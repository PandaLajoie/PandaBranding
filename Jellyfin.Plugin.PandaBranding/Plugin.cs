using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.PandaBranding.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PandaBranding;

/// <summary>
/// Plugin « PandaBranding » : icône d'onglet, icône d'écran d'accueil et nom de l'application dans l'interface web.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "PandaBranding";

    public override Guid Id => Guid.Parse("8b77f51d-826d-490b-b126-26ea3a45ad7c");

    public override string Description =>
        "Browser tab icon, home screen icon and app name for the web interface (requires File Transformation). / "
        + "Icône d'onglet, icône d'écran d'accueil et nom de l'application dans l'interface web (nécessite File Transformation).";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
