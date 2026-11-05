using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.PandaBranding.Configuration;

namespace Jellyfin.Plugin.PandaBranding;

/// <summary>
/// Ce que File Transformation passe à nos fonctions : le texte actuel du fichier demandé (champ JSON « contents »).
/// </summary>
public class TransformRequest
{
    public string? Contents { get; set; }
}

/// <summary>
/// Icônes et réécriture des fichiers de jellyfin-web. Les méthodes <c>Transform*</c> sont appelées par File
/// Transformation par réflexion (statiques, un paramètre, renvoient le texte modifié) : elles ne doivent jamais lever
/// d'exception et rendent le fichier d'origine en cas de problème.
/// </summary>
public static class Branding
{
    /// <summary>Tailles fournies avec le plugin (fichiers Resources/icon-{taille}.png).</summary>
    public static readonly int[] Sizes = [32, 180, 192, 512];

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly ConcurrentDictionary<int, byte[]> EmbeddedIcons = new();

    // Adresses relatives à /web/ (index.html et manifest.json y sont servis) : marche aussi avec une « URL de base ».
    private const string IconPath = "../PandaBranding/Icon";

    private static readonly Regex IconLinks = new(
        @"<link\b[^>]*\brel=""(?:shortcut icon|icon)""[^>]*>\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AppleIcon = new(
        @"(<link\b[^>]*\brel=""apple-touch-icon""[^>]*\bhref="")[^""]*("")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Title = new(@"<title>[^<]*</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AppNameMeta = new(
        @"(<meta\b[^>]*\bname=""(?:application-name|apple-mobile-web-app-title)""[^>]*\bcontent="")[^""]*("")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>
    /// L'icône choisie par l'administrateur, ou null (PNG absent ou invalide).
    /// </summary>
    public static byte[]? CustomIcon(PluginConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.CustomIconPng))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(config.CustomIconPng);
            return IsPng(bytes) ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static bool IsPng(byte[] bytes) => bytes.Length > PngSignature.Length && bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);

    /// <summary>
    /// Icône à servir pour une taille demandée : l'icône personnalisée (le navigateur la met à l'échelle) ou la tête de
    /// panda de la taille la plus proche.
    /// </summary>
    public static byte[] GetIcon(int size, bool builtIn = false)
    {
        var custom = builtIn ? null : CustomIcon(Config);
        if (custom != null)
        {
            return custom;
        }

        var best = Sizes.OrderBy(s => s < size ? int.MaxValue - s : s - size).First();
        return EmbeddedIcons.GetOrAdd(best, s =>
        {
            using var stream = typeof(Branding).Assembly.GetManifestResourceStream($"{typeof(Branding).Namespace}.Resources.icon-{s}.png")
                ?? throw new InvalidOperationException($"Icon {s} missing");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        });
    }

    /// <summary>
    /// Marque de version des adresses d'icône : change quand l'icône change, pour que les navigateurs (qui gardent
    /// très longtemps les icônes d'onglet) la rechargent.
    /// </summary>
    public static string IconVersion()
    {
        var custom = CustomIcon(Config);
        if (custom == null)
        {
            return typeof(Branding).Assembly.GetName().Version?.ToString() ?? "0";
        }

        return Convert.ToHexString(SHA256.HashData(custom), 0, 6).ToLowerInvariant();
    }

    private static string IconUrl(int size, string version) => $"{IconPath}?size={size}&v={version}";

    public static string TransformIndex(TransformRequest request)
    {
        var html = request?.Contents ?? string.Empty;
        try
        {
            return PatchIndex(html, Config);
        }
        catch (Exception)
        {
            return html;
        }
    }

    public static string TransformManifest(TransformRequest request)
    {
        var json = request?.Contents ?? string.Empty;
        try
        {
            return PatchManifest(json, Config);
        }
        catch (Exception)
        {
            return json;
        }
    }

    public static string PatchIndex(string html, PluginConfiguration config)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        if (config.ReplaceIcons)
        {
            var v = WebUtility.HtmlEncode(IconVersion());
            html = IconLinks.Replace(html, string.Empty);
            var links = $"<link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"{IconUrl(32, v).Replace("&", "&amp;", StringComparison.Ordinal)}\">"
                + $"<link rel=\"icon\" type=\"image/png\" sizes=\"192x192\" href=\"{IconUrl(192, v).Replace("&", "&amp;", StringComparison.Ordinal)}\">";
            var head = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            html = head >= 0 ? html.Insert(head, links) : links + html;
            html = AppleIcon.Replace(html, m => m.Groups[1].Value + IconUrl(180, v).Replace("&", "&amp;", StringComparison.Ordinal) + m.Groups[2].Value);
        }

        var name = config.AppName?.Trim();
        if (config.ReplaceName && !string.IsNullOrEmpty(name))
        {
            var encoded = WebUtility.HtmlEncode(name);
            html = Title.Replace(html, $"<title>{encoded}</title>", 1);
            html = AppNameMeta.Replace(html, m => m.Groups[1].Value + encoded + m.Groups[2].Value);
        }

        return html;
    }

    public static string PatchManifest(string json, PluginConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(json) || JsonNode.Parse(json) is not JsonObject manifest)
        {
            return json;
        }

        var name = config.AppName?.Trim();
        if (config.ReplaceName && !string.IsNullOrEmpty(name))
        {
            manifest["name"] = name;
            manifest["short_name"] = name;
        }

        if (config.ReplaceIcons)
        {
            var v = IconVersion();
            var icons = new JsonArray();
            foreach (var size in new[] { 192, 512 })
            {
                icons.Add(new JsonObject
                {
                    ["sizes"] = $"{size}x{size}",
                    ["src"] = IconUrl(size, v),
                    ["type"] = "image/png",
                    ["purpose"] = "any"
                });
            }

            manifest["icons"] = icons;
        }

        return manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
