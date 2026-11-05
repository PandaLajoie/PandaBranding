# PandaBranding

[English](README.md)

Un plugin Jellyfin qui remplace, dans l'interface web :

- l'**icône d'onglet** du navigateur (favicon) ;
- l'**icône d'écran d'accueil** (raccourci sur un téléphone, application web installée) ;
- le **nom de l'application** « Jellyfin » (titre de l'onglet pendant le chargement, nom de l'application web
  installée).

Les fichiers de Jellyfin ne sont pas modifiés : le plugin change `index.html` et `manifest.json` au moment où ils sont
envoyés, grâce au plugin [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation).
Les changements survivent aux mises à jour de Jellyfin.

## Prérequis

- Jellyfin 12.4.
- Le plugin **File Transformation**, installé et actif.

## Installation

1. Installez **File Transformation** (dépôt `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`).
2. *Tableau de bord → Extensions → Dépôts → +* : ajoutez
   `https://raw.githubusercontent.com/PandaLajoie/PandaBranding/main/manifest.json`
3. *Catalogue → PandaBranding → Installer*, puis redémarrez Jellyfin.

## Réglages

*Tableau de bord → Extensions → PandaBranding* :

- **Icône** : la tête de panda fournie avec le plugin, ou votre propre PNG carré (512 × 512 conseillé, 1 Mo au plus).
- **Nom** : le nom affiché à la place de « Jellyfin ». Une fois la page chargée, Jellyfin affiche lui-même le nom du
  serveur dans l'onglet.

Les navigateurs gardent longtemps les icônes d'onglet : après un changement, rechargez la page (Ctrl+F5) ou fermez et
rouvrez l'onglet.

## Limites

- Navigateurs seulement. Les applications qui embarquent leur propre copie de l'interface (Jellyfin for Tizen,
  Android TV, Swiftfin…) ne sont pas touchées.
- Le logo du menu du tableau de bord ne change pas (il fait partie du code de l'interface, pas de ces deux fichiers).

## Compiler

```
docker run --rm -v "${PWD}\Jellyfin.Plugin.PandaBranding:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet publish -c Release -o /src/bin/publish
```

Installation manuelle : copiez `Jellyfin.Plugin.PandaBranding.dll`, `meta.json` et `images/logo.png` dans un
dossier `PandaBranding_0.1.0.0` du dossier `plugins` de Jellyfin, puis redémarrez Jellyfin. Publier une version :
`scripts/release.ps1 -Version 0.1.1 -Changelog "…"`.

## Licence

GPL-2.0, voir [LICENSE](LICENSE).
