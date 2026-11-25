<div align="center">

<img src="packaging/icon-D-1024.png" width="192" height="192" alt="UEI icon">

# UEI - Unknown Enough Items

**An item, liquid and recipe browser for *Casualties: Unknown***

[![Release](https://img.shields.io/github/v/release/MAakber/Unknown-Enough-Items)](https://github.com/MAakber/Unknown-Enough-Items/releases)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-UEI-23a8e0)](https://thunderstore.io/c/casualties-unknown/p/MAakber/UEI/)
![BepInEx 5](https://img.shields.io/badge/BepInEx-5-f6c343)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

[简体中文](README.md) · **English**

</div>

UEI (Unknown Enough Items) is inspired by Minecraft's JEI. When you open the game's inventory, it shows a lookup panel alongside it: search for any item or liquid, see how it is crafted and what it is used for, and follow the recipe chain from there. The game's own crafting system is left untouched.

![The UEI panel to the left of the game's inventory](image/README/1779439656940.png)

> [!NOTE]
> UEI is still in early development. Since 1.2.4 it supports the full release of the game, and that version passed local runtime checks. Features added since then, such as acquisition cards, entity spawning and liquid delivery, are still awaiting in-game acceptance testing, and multiplayer has not been fully tested; see [Compatibility and known issues](#compatibility-and-known-issues).

[Features](#features) · [Install](#install) · [Usage](#usage) · [Configuration](#configuration) · [Compatibility](#compatibility-and-known-issues) · [Building](#building-from-source) · [Development notes](#development-notes) · [Author and license](#author-and-license)

## Features

- **Browse** every item and liquid loaded at runtime, including entries added by supported mods. UEI reads this data from the game itself rather than shipping its own item list, so it follows your game version and installed mods.
- **Find** entries by name or category; favorites are listed first.
- **Entities**: merchants, creatures and world objects have their own category, and also appear in All with the same search and favorites. Click one to read its original description.
- **Sources and uses**: left-click an item or liquid for its acquisition info and crafting recipes; right-click for the recipes that use it.
- **Recipe chains**: click an ingredient or result to look it up. Hover a generic ingredient (such as "any rippable item") to see matching items or liquids; long lists show the first 18 and how many more there are.
- **Game integration**: open the game's crafting panel at the recipe you are viewing, or pin that recipe.
- **Native look**: hovering an entry shows the game's own tooltip style.
- **Adjustable**: change the UI language (Chinese / English), panel position, scale and card rows per page in settings.
- **Optional cheats**: give yourself items or liquids, spawn entities, or revive. Off by default and subject to game and server permissions.

> **About acquisition info:** UEI derives sources from current building drops, actual loot pools, trader rules and filled-container contents. This works with official English text and requires no translation patch or network connection. Labelled description text is kept as a supplement, with its special conditions intact.

Standalone and nested variants of the same source are combined when their drop rules match exactly. Tooltips retain the source contexts. Probabilities and output quantities are not added together; different or incomplete rules stay separate.

### Screenshots

These screenshots were taken in an early demo build with the Chinese UI, so details may differ from the current version.

| What it shows | Screenshot |
| --- | --- |
| All runtime items and liquids | ![Item list](image/README/1779439722660.png) |
| Search, category filter and pinned favorites | ![Search and filter](image/README/1779439878022.png) |
| Native-style tooltip on hover | ![Tooltip](image/README/1779439913966.png) |
| Source page (left-click) and Uses page (right-click) | ![Source and Uses pages](image/README/1779440030782.png) |
| Matching candidates for a generic ingredient | ![Generic ingredient candidates](image/README/1779440090021.png) |
| The cheat toggle follows multiplayer permissions (left: client without access; right: host with cheats allowed) | ![Multiplayer permissions](image/README/1779439469003.png) |

## Install

### Gale (recommended)

In Gale, select Casualties: Unknown, search for **UEI** and install it. The required BepInEx is installed automatically. You can also download the ZIP from the [Thunderstore page](https://thunderstore.io/c/casualties-unknown/p/MAakber/UEI/) and import it into Gale.

### Manual

1. Install BepInEx 5 for the game.
2. Download `UEI-x.y.z.zip` from [GitHub Releases](https://github.com/MAakber/Unknown-Enough-Items/releases) and merge its `BepInEx` folder into the game folder. Alternatively, download only `UEI.dll` and place it at `BepInEx/plugins/UEI/UEI.dll`.
3. Launch the game.

### Updating

Close the game first (Windows locks the DLL while the game is running) and keep only one copy of UEI. A locally built `UEI.build.dll` and the released `UEI.dll` are the same plugin, so do not install both.

## Usage

### Opening and finding entries

1. In game, open the native right-click inventory (the radial backpack). The UEI panel appears beside it, and hides while the crafting, trade or medical panel or the console is open.
2. Find entries with the search box and the category arrows (`All`, `Favorites`, item categories, `Liquid`, `Entities`). Click the star on an entry to favorite it.

### Viewing details

The table shows what clicks do during normal browsing, with cheats off. With cheats on, left-click gives or spawns instead; see [Cheat mode](#cheat-mode).

| Entry | Left-click | Right-click |
| --- | --- | --- |
| Item or liquid | Source page | Uses page |
| Entity | Entity description | Entity description |

- **Source page**: switch between Acquisition and Crafting recipes with the buttons above the cards. When the description carries acquisition labels, a Notes section also appears, where long text can be scrolled. Acquisition and crafting both use icon cards, two rows per page by default and 1–4 rows in settings. Liquid cards show the source, filled container and liquid; click a container to keep browsing.
- **Uses page**: lists every recipe that uses the entry. The `SRC` / `USE` button at the top left of the detail panel switches between the Source and Uses pages.
- **Entity description**: renders the original rich text, keeping colors, sizes, line breaks and sprites, without splitting or rewriting it. The text uses as much of the remaining detail-panel space as it needs and scrolls when longer. Entities appear gradually as the first resource scan progresses.
- **Missing-icon marker**: entries without a usable icon show a black/magenta checkerboard, so a missing icon is not mistaken for the object's appearance; empty cells stay empty. Entities reuse an available visible sprite first. Some entities are drawn another way: the Tentacle-plant, for example, is drawn with lines and has no static sprite, so its marker does not mean it cannot be spawned.

### Recipes and paging

- Click an ingredient or result in a recipe to look it up.
- The jump button in the detail panel opens the game's crafting panel at the current recipe, and the `*` button on a recipe card pins or unpins that recipe.
- To change pages, click the arrows, or hover over the list or the recipes and use the mouse wheel or PageUp / PageDown.

### Settings

Click the gear next to the search box to change the language, panel position, scale, cards per page and cheat setting. The cards-per-page button cycles through 1–4 rows, shared by acquisition, crafting and uses; denser pages scale card contents down to fit the panel. Your row choice is saved automatically.

### Cheat mode

Cheat mode is off by default. You can turn it on in settings, but only when you have permission:

| Situation | Requirement |
| --- | --- |
| Single player, or Together not installed | Always available |
| Together host | `sv_cheats` must be enabled |
| Together client | Admin-command permission from the server; the server still checks `sv_cheats` when it runs the request |

Being able to turn the switch on as a client does not guarantee that every request succeeds: the server carries out gives and spawns, and only what actually arrives in game counts.

While cheats are on, right-click still opens details, and left-click acts by entry type:

| Entry | Left-click |
| --- | --- |
| Item | Gives you one |
| Liquid | Gives you a native temporary bottle filled with it (1000 mL by default, disappears after five minutes); it stays at your feet if your inventory is full |
| Entity | Starts placement: the inventory closes, you move the mouse to choose a spot, and a second left-click spawns one there through the native `spawn` command |

During placement:

- A translucent preview shows the entity at its actual size, origin, orientation and visible sprite layers. The preview is only a local image; no interactive entity exists until you confirm.
- The preview uses static sprites only, without animation, lines or other renderers. Entities with no usable preview, such as the Tentacle-plant, show the text hint "No visible preview" instead and can still be spawned.
- Right-click or Esc cancels; the inventory key cancels and opens the inventory. After one spawn, normal controls return, and reopening UEI keeps your category, search and page.

These actions use the existing entries, with no extra buttons or settings. A revive button also appears next to the search box, fully healing and reviving your character.

## Configuration

BepInEx stores the settings in `BepInEx/config/casualtiesunknown.uei.cfg`. Language, position, scale, card rows per page and the cheat toggle can also be changed in the in-game settings.

| Setting | Default | Description |
| --- | --- | --- |
| `General/Language` | `auto` | UI language: `auto` (follows the game, falling back to the system language), `zh` or `en` |
| `General/PanelPosition` | `left` | Panel position: `left` or `right` |
| `General/PanelScale` | `1.0` | Panel scale; the in-game setting ranges from 85% to 130% |
| `General/DetailRowsPerPage` | `2` | 1–4 card rows per page for acquisition, crafting and uses; contents scale uniformly to the available height |
| `Favorites/FavoriteEntries` | empty | Favorite entries as `item:<id>`, `liquid:<id>` or `entity:<resource path>`, separated by `\|` |
| `Cheat/Enabled` | `false` | Cheat mode toggle |

## Compatibility and known issues

- UEI was first built for Casualties Unknown Demo v6.1 and has supported the full release of the game since 1.2.4.
- Support for Together (multiplayer), CUCoreLib, RshLib and SexMod is optional; UEI works without them. When they are installed, UEI lists the items they add and checks permissions according to your multiplayer role.
- In Together multiplayer:
  - Clients give items or liquids and spawn entities through server commands, which the server carries out. "Give requested" or "Spawn requested from server" only means the command was sent, not that the server carried it out.
  - Liquid delivery relies on UEI running on the host, so the host needs the same UEI version installed.
  - Clients cannot request mod items; the host has to spawn them.
  - Synchronization between players, saving and reconnecting have not been fully tested.
- Acquisition info primarily uses current building drop fields, actual loot pools, random trader-stock rules and default liquid contents. Official English descriptions need no annotations. Labels such as `获取：`, `Acquisition:` and `Obtained from:` are retained under "Description supplement".
- The first prefab scan runs in bounded asynchronous batches. More sources may appear while it finishes; search, paging, language and recipe changes reuse the cache. Failed reads disclose incomplete coverage. "No acquisition info" means no source is currently known, rather than that the entry is unobtainable.
- Terrain generation, scripted events and all custom mod sources are not fully covered. Crafting recipes remain separate, and description annotations can supplement special routes. See [the native acquisition record](docs/acquisition-native-fix.md) for validation and manual tests.
- Release packages contain UEI only: no game files, BepInEx, Harmony, Unity / TextMeshPro assemblies or other mods.

## Building from source

Players should install a release; there is no need to build UEI yourself.

### Automated builds

GitHub Actions checks pull requests and builds commits to this repository. Pushing a `vX.Y.Z` tag that matches the source version builds and publishes a GitHub Release. CI compiles against encrypted reference assemblies, so the repository contains no plain game assemblies. See the [automation guide](docs/github-automation.md) (in Chinese).

Plugin source files are in `src/`, regression tests in `tests/`, and build and release scripts in `scripts/`. `UEI.csproj` stays at the repository root; run the commands below from there.

### Manual build

You need a game folder with BepInEx installed, the .NET SDK (CI uses .NET 10) and this repository.

The recommended layout puts the source in the game folder's `Tools/UEI`:

```text
<game folder>/
├─ BepInEx/
├─ CasualtiesUnknown_Data/
└─ Tools/
   └─ UEI/    ← this repository
```

From `Tools/UEI`, run:

```powershell
dotnet build UEI.csproj -c Release
```

If the source is somewhere else, pass the game folder with `GameRoot`:

```powershell
dotnet build UEI.csproj -c Release /p:GameRoot="<game folder>"
```

The build writes `BepInEx/plugins/UEI/UEI.build.dll` straight into the game folder. Close the game first, because Windows locks the file while the game is running.

Before a release, set the same `x.y.z` version in `UEI.csproj` and `src/Plugin.cs`, and update the version marker at the top of `packaging/RELEASE-NOTES.md`. See [tests/README.md](tests/README.md) for running the regression tests.

## Development notes

Design notes and validation scope by version:

- [Input and performance fixes](docs/input-performance-fix.md) (1.2.6)
- [Native acquisition sources](docs/acquisition-native-fix.md) (1.2.7): source coverage and the in-game test list
- [Acquisition icon cards](docs/acquisition-card-layout.md) (1.2.8, in Chinese)
- [Card rows per page](docs/detail-rows-setting.md) (1.2.9, in Chinese)
- [Source card merging](docs/acquisition-card-merge.md) (1.2.10, in Chinese)
- [Entities and liquid delivery](docs/entity-spawning.md): spawning flow, multiplayer routing and in-game acceptance list
- [PR #3 / #4 integration review](docs/pr-3-4-integration.md): review basis, attribution and validation scope
- [CUCoreLib public API and test plan](docs/cucore-public-api-test-plan.md) (in Chinese)
- [GitHub automation guide](docs/github-automation.md) (in Chinese)
- [Regression tests](tests/README.md)

## Author and license

Author: **Aakber (小叶子)**. Thanks to **dfgger433** for the CUCoreLib / SexMod compatibility and UI improvements contributed in PR #3 / #4.

The source code in this repository is licensed under the [MIT License](LICENSE). The license covers only the UEI source code here and grants no rights to *Casualties: Unknown*, its assets or assemblies, BepInEx, Harmony, Unity, TextMeshPro or any other third-party dependency.
