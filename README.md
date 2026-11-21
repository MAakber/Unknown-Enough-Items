<div align="center">

<img src="https://socialify.git.ci/MAakber/Unknown-Enough-Items/image?description=1&font=Inter&forks=1&issues=1&language=1&name=1&owner=1&pattern=Plus&pulls=1&stargazers=1&theme=Auto" alt="Unknown Enough Items social preview">

# UEI - Unknown Enough Items

**A JEI-style item, liquid, and recipe helper for `Casualties Unknown Demo`.**
**一个用于 `Casualties Unknown Demo` 的 JEI 风格物品、液体与配方查询辅助插件。**

![Version](https://img.shields.io/badge/version-1.2.4-blue)
![BepInEx](https://img.shields.io/badge/BepInEx-plugin-f6c343)
![Game](https://img.shields.io/badge/game-Casualties%20Unknown%20Demo-2f2f2f)
![UI](https://img.shields.io/badge/UI-%E4%B8%AD%E6%96%87%20%7C%20English-orange)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

> [!WARNING]
> UEI is an early WIP mod. Its original test baseline was `Casualties Unknown Demo v6.1`. The current full-game/CUCoreLib integration is a test candidate; see the linked validation records before treating it as a verified release.
>
> UEI 目前仍是早期开发版本，原始测试基线为 `Casualties Unknown Demo v6.1`。当前正式版 / CUCoreLib 整合为待实测候选，已验证范围与限制见下方记录。

`1.2.4` 本地整合保留 RshLib / SexMod，并优先使用 CUCoreLib 的公开接口读取物品、图标、联机角色和生成物品。
Together 客户端的原版物品请求由服务器处理；模组物品需由房主生成，客户端会显示限制提示。
“已请求”仅表示提交了命令，不代表服务器已成功执行。`cuspawn` 尚未启用为自动后备，需单独验证权限、作弊规则与同步行为。
审查依据、贡献归属、验证范围见 [PR #3 / #4 整合记录](docs/pr-3-4-integration.md)。
CUCoreLib 专项验证见 [公开接口与测试计划](docs/cucore-public-api-test-plan.md)；测试插件位于仓库之外，不随 UEI 发布。

## 目录 / Contents

- [简介 / Overview](#简介--overview)
- [功能 / Features](#功能--features)
- [安装 / Install](#安装--install)
- [编译方法 / Compile Method](#编译方法--compile-method)
- [配置 / Configuration](#配置--configuration)
- [使用说明 / Usage](#使用说明--usage)
- [作者 / Author](#作者--author)
- [许可证 / License](#许可证--license)

## 简介 / Overview

UEI, short for **Unknown Enough Items**, is a BepInEx plugin inspired by Minecraft JEI. It adds an in-game helper panel beside the original inventory/radial backpack UI, letting you browse items, liquids, and recipe relationships without replacing the game's native crafting system.

UEI 是 **Unknown Enough Items** 的缩写，设计灵感来自 Minecraft JEI。它会在原版物品栏 / 径向背包界面旁显示一个辅助面板，用于查询物品、液体和配方关系，但不会替代原版制作系统。

The mod reads runtime game data directly from:

本插件直接读取游戏运行时数据：

- `Item.GlobalItems`
- `Liquids.Registry`
- `Recipes.recipes`

It does not bundle game assets, game assemblies, BepInEx, Harmony, Unity assemblies, or TextMeshPro assemblies.

本项目不包含游戏资源、游戏程序集、BepInEx、Harmony、Unity 程序集或 TextMeshPro 程序集；这些仅作为本地构建和运行依赖。

## 功能 / Features

| 功能                                  | Feature                                                                 |                                                |
| ------------------------------------- | ----------------------------------------------------------------------- | ---------------------------------------------- |
| 在原版右键背包界面旁显示 UEI 面板。   | Shows the UEI panel beside the native right-click inventory UI.         | ![1779439656940](image/README/1779439656940.png) |
| 列出全部运行时物品和液体。            | Lists all runtime items and liquids.                                    | ![1779439722660](image/README/1779439722660.png) |
| 支持搜索、分类筛选和收藏置顶。        | Supports search, category filtering, and favorite pinning.              | ![1779439878022](image/README/1779439878022.png) |
| 鼠标悬停复用游戏原生 tooltip 风格。   | Reuses the game's native tooltip style on hover.                        | ![1779439913966](image/README/1779439913966.png) |
| 左键查看来源配方，右键查看用途配方。  | Left-click opens source recipes, right-click opens usage recipes.       | ![1779440030782](image/README/1779440030782.png) |
| 配方材料和结果可点击跳转。            | Recipe ingredients and results can jump to related UEI entries.         | -                                              |
| 泛材料会显示可用物品 / 液体候选列表。 | Generic ingredients show matching item/liquid candidates.               | ![1779440090021](image/README/1779440090021.png) |
| 可跳转到原版制作面板并选中对应配方。  | Can jump to the original crafting panel and select the matching recipe. | -                                              |
| 适配多人联机模组 KrokMP。             | Adapts to the KrokMP multiplayer mod.                                   | ![1779439469003](image/README/1779439469003.png) |
| 支持中文和 English。                  | Supports Chinese and English.                                           | -                                              |

## 安装 / Install

1. Install BepInEx for `Casualties Unknown Demo`.
2. Copy the compiled DLL to:

```text
BepInEx/plugins/UEI/UEI.build.dll
```

3. Launch the game.
4. Open the native inventory/radial backpack UI. UEI appears beside it when the UI state is supported.

步骤：

1. 先为 `Casualties Unknown Demo` 安装 BepInEx。
2. 将编译好的 DLL 放到：

```text
BepInEx/plugins/UEI/UEI.build.dll
```

3. 启动游戏。
4. 打开原版物品栏 / 径向背包界面；当界面状态支持时，UEI 会显示在旁边。

## 编译方法 / Compile Method

GitHub Actions 自动运行回归检查，并为本仓库提交生成可下载的编译包。
维护者推送与源码版本一致的 `vX.Y.Z` 标签后，会自动构建并发布 GitHub Release。
发布规则、构建引用与维护方法见 [GitHub 自动化说明](docs/github-automation.md)。

GitHub Actions checks pull requests, builds trusted repository commits, and
publishes releases for matching version tags. See the automation guide above.

Regular players should use the release DLL once releases are published.

普通玩家后续可以直接下载 Release 页面中的 DLL，不需要自己编译。

If you need to compile from source manually, prepare:

如果需要从源码手动编译，请准备：

- A local `Casualties Unknown Demo` game folder with BepInEx installed.
- .NET SDK.
- This repository's source code.

Release builds use the version declared in `Plugin.cs`/`UEI.csproj`. Update those files before creating a new release build.

Release 构建会使用 `Plugin.cs`/`UEI.csproj` 中声明的版本号。创建新版本前请先更新这些文件。

Recommended layout:

推荐目录结构：

```text
Casualties Unknown Demo/
├─ BepInEx/
├─ CasualtiesUnknown_Data/
└─ Tools/
   └─ UEI/
      ├─ UEI.csproj
      └─ *.cs
```

Compile from `Tools/UEI`:

在 `Tools/UEI` 中编译：

```powershell
cd "D:\SteamLibrary\steamapps\common\Casualties Unknown Demo\Tools\UEI"
dotnet build UEI.csproj -c Release
```

The project file references assemblies from the local game folder and writes the plugin DLL to:

项目文件会引用本地游戏目录中的程序集，并把插件 DLL 输出到：

```text
BepInEx/plugins/UEI/UEI.build.dll
```

If the source folder is not placed under `GameRoot/Tools/UEI`, pass the game directory explicitly:

如果源码没有放在 `游戏目录/Tools/UEI` 下，请显式传入游戏目录：

```powershell
dotnet build UEI.csproj -c Release /p:GameRoot="D:\SteamLibrary\steamapps\common\Casualties Unknown Demo"
```

The expected runtime file is:

```text
BepInEx/plugins/UEI/UEI.build.dll
```

If the game is running, Windows may lock the deployed DLL. Close the game before replacing it.

如果游戏正在运行，Windows 可能会锁定已部署的 DLL。请关闭游戏后再替换文件。

## 配置 / Configuration

BepInEx writes UEI configuration to:

BepInEx 会把 UEI 配置写入：

```text
BepInEx/config/casualtiesunknown.uei.cfg
```

| 配置项 / Setting              | 说明 / Description                                                                                                           |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `General/Language`          | `auto`、`zh` 或 `en`。 / `auto`, `zh`, or `en`.                                                                  |
| `General/PanelPosition`     | 面板位置：`left` 或 `right`。 / Panel position: `left` or `right`.                                                   |
| `General/PanelScale`        | 面板比例，当前设置页会在 85%、100%、115%、130% 之间循环。 / Panel scale, currently cycles between 85%, 100%, 115%, and 130%. |
| `Favorites/FavoriteEntries` | 收藏条目，格式为 `item:<id>` / `liquid:<id>`。 / Persisted favorite entries as `item:<id>` / `liquid:<id>`.          |
| `Cheat/Enabled`             | 开关，默认关闭。 / give-item toggle, disabled by default.                                                                    |

## 使用说明 / Usage

- Open the game's native right-click inventory/radial backpack UI to show UEI.
- Type in the search box to filter entries.
- Use category arrows to switch between `All`, `Favorites`, item categories, and `Liquid`.
- Click the small star button on an entry to favorite it.
- Left-click an entry to view source recipes.
- Right-click an entry to view usage recipes.
- Use the recipe panel arrows to page through recipes.
- Click recipe materials/results to jump through the recipe chain.
- Use the jump button in the detail panel to open the original crafting panel.
- Use the gear button beside the search box to open UEI settings.

说明：

- 打开游戏原版右键物品栏 / 径向背包界面后，UEI 会自动显示。
- 在搜索框中输入关键词可筛选条目。
- 使用分类箭头切换 `全部`、`收藏`、物品分类和 `液体`。
- 点击条目角落的星标按钮可收藏。
- 左键条目查看来源配方。
- 右键条目查看用途配方。
- 使用配方面板箭头翻页。
- 点击配方材料 / 结果可在配方链中跳转。
- 点击详情页跳转按钮可打开原版制作面板并选中配方。
- 点击搜索框旁的齿轮按钮可打开 UEI 设置。

## 作者 / Author

**Aakber (小叶子)**

## 许可证 / License

This repository is licensed under the [MIT License](LICENSE).

本仓库源码使用 [MIT License](LICENSE) 授权。

The license only covers UEI source code in this repository. It does not grant any rights to `Casualties Unknown Demo`, game assets, game assemblies, BepInEx, Harmony, Unity, TextMeshPro, or any other third-party dependency.

该许可证仅适用于本仓库中的 UEI 源码，不包含 `Casualties Unknown Demo` 游戏本体、游戏资源、游戏程序集、BepInEx、Harmony、Unity、TextMeshPro 或其他第三方依赖。
