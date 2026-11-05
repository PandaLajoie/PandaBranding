using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PandaBranding.Configuration;

/// <summary>
/// Réglages du plugin. Par défaut, rien n'est remplacé : l'administrateur active ce qu'il veut (plugin public).
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether l'icône d'onglet et l'icône d'écran d'accueil sont remplacées.
    /// </summary>
    public bool ReplaceIcons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether le nom de l'application (titre de l'onglet au chargement, nom de
    /// l'application installée) est remplacé.
    /// </summary>
    public bool ReplaceName { get; set; }

    /// <summary>
    /// Gets or sets le nom affiché à la place de « Jellyfin ».
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets une icône PNG choisie par l'administrateur, en base64. Vide : la tête de panda fournie avec le
    /// plugin.
    /// </summary>
    public string CustomIconPng { get; set; } = string.Empty;
}
