# PandaBranding

[Français](README.fr.md)

A Jellyfin plugin that replaces, in the web interface:

- the **browser tab icon** (favicon);
- the **home screen icon** (phone shortcut, installed web app);
- the **app name** "Jellyfin" (tab title while the page loads, name of the installed web app).

Jellyfin's files are not modified: the plugin changes `index.html` and `manifest.json` as they are sent, through the
[File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) plugin. Changes survive
Jellyfin updates.

## Requirements

- Jellyfin 12.4.
- The **File Transformation** plugin, installed and active.

## Installation

1. Install **File Transformation** (repository `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`).
2. *Dashboard → Plugins → Repositories → +*: add
   `https://raw.githubusercontent.com/PandaLajoie/PandaBranding/main/manifest.json`
3. *Catalog → PandaBranding → Install*, then restart Jellyfin.

## Settings

*Dashboard → Plugins → PandaBranding*. Nothing is replaced until you turn it on:

- **Icon**: the panda head included with the plugin, or your own square PNG (512 × 512 recommended, 1 MB at most).
- **Name**: the name shown instead of "Jellyfin". Once a page has loaded, Jellyfin itself shows the server name in the
  tab.

Browsers keep tab icons for a long time: after a change, reload the page (Ctrl+F5) or close and reopen the tab.

## Limits

- Web browsers only. Apps that bundle their own copy of the interface (Jellyfin for Tizen, Android TV, Swiftfin…) are
  not affected.
- The dashboard's side menu logo is not changed (it is part of the interface code, not of these two files).

## Building

```
docker run --rm -v "${PWD}\Jellyfin.Plugin.PandaBranding:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet publish -c Release -o /src/bin/publish
```

Manual install: copy `Jellyfin.Plugin.PandaBranding.dll`, `meta.json` and `images/logo.png` into a
`PandaBranding_0.1.1.0` folder in Jellyfin's `plugins` folder, then restart Jellyfin. Release:
`scripts/release.ps1 -Version 0.1.1 -Changelog "…"`.

## License

GPL-2.0, see [LICENSE](LICENSE).
