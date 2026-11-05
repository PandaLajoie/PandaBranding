using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PandaBranding;

/// <summary>
/// Au démarrage du serveur, demande au plugin File Transformation de passer index.html et manifest.json par nos
/// fonctions. File Transformation est chargé dans un autre contexte que nous : on ne peut pas le référencer, on
/// l'appelle par réflexion (méthode publique <c>PluginInterface.RegisterTransformation(JObject)</c>).
/// </summary>
public class FileTransformationRegistration : IHostedService
{
    // Identifiants fixes : si File Transformation reçoit deux fois le même, il ignore le doublon.
    //
    // Motifs : File Transformation range les transformations par texte exact du motif et, pour un fichier, n'exécute
    // qu'UNE liste (la correspondance exacte d'abord). On prend donc le nom simple « index.html », comme les autres
    // plugins, pour partager leur liste ; un motif différent (ex. « ^index\.html$ ») serait ignoré dès qu'un autre
    // plugin enregistre « index.html ».
    private static readonly Guid IndexId = Guid.Parse("0f1d4b64-2b5e-4f0a-9d7e-5c1a3e9b7d21");
    private static readonly Guid ManifestId = Guid.Parse("6a2c9e17-8d43-4b6f-a1e5-3f7b0c2d9e48");

    private readonly ILogger<FileTransformationRegistration> _logger;
    private readonly CancellationTokenSource _stopping = new();

    public FileTransformationRegistration(ILogger<FileTransformationRegistration> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // En arrière-plan : File Transformation n'est peut-être pas encore prêt à cet instant.
        _ = Task.Run(() => RegisterWithRetryAsync(_stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping.Cancel();
        return Task.CompletedTask;
    }

    private async Task RegisterWithRetryAsync(CancellationToken token)
    {
        string? lastError = null;
        for (var attempt = 1; attempt <= 30 && !token.IsCancellationRequested; attempt++)
        {
            try
            {
                var register = FindRegisterMethod();
                if (register == null)
                {
                    lastError = "File Transformation plugin not found";
                }
                else
                {
                    Register(register, IndexId, "index.html", nameof(Branding.TransformIndex));
                    Register(register, ManifestId, "manifest.json", nameof(Branding.TransformManifest));
                    _logger.LogInformation("PandaBranding: registered with File Transformation (index.html, manifest.json)");
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex is TargetInvocationException { InnerException: { } inner } ? inner.Message : ex.Message;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        _logger.LogWarning(
            "PandaBranding: could not register with File Transformation ({Error}). Icons and name are unchanged. Install File Transformation and restart Jellyfin.",
            lastError);
    }

    private static MethodInfo? FindRegisterMethod()
    {
        var assembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.FileTransformation");
        return assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?.GetMethod("RegisterTransformation");
    }

    private static void Register(MethodInfo register, Guid id, string pattern, string callbackMethod)
    {
        var json = JsonSerializer.Serialize(new
        {
            id,
            fileNamePattern = pattern,
            callbackAssembly = typeof(Branding).Assembly.FullName,
            callbackClass = typeof(Branding).FullName,
            callbackMethod
        });

        // Le paramètre est un JObject (Newtonsoft) de leur contexte : on le fabrique avec leur propre JObject.Parse.
        var payloadType = register.GetParameters()[0].ParameterType;
        var parse = payloadType.GetMethod("Parse", [typeof(string)])
            ?? throw new InvalidOperationException("JObject.Parse not found");
        var payload = parse.Invoke(null, [json]);
        register.Invoke(null, [payload]);
    }
}
