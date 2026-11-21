# UEI — Unknown Enough Items @VERSION@

Casualties Unknown 的物品、液体与配方查询插件，支持中文和 English。
An item, liquid and recipe browser for Casualties Unknown.

UEI adds a JEI-style helper panel beside the game's native inventory. Browse
runtime items, liquids and recipes without replacing the original crafting UI.

UEI 会在原版物品栏 / 径向背包旁显示辅助面板，用于查询物品、液体与配方关系。

## 功能 / Features

### English

- Browse runtime items and liquids, including supported mod entries.
- Search, filter by category, and pin favorites.
- Left-click an entry to view source recipes; right-click to view its uses.
- Follow recipe chains by clicking ingredients and results.
- View matching candidates for generic ingredients.
- Jump to the original crafting panel and select the related recipe.
- Use Chinese or English, with adjustable panel position and scale.
- Optional cheat controls for item requests and revival, subject to game and server permissions.

### 中文

- 浏览运行时物品和液体，并读取受支持的第三方模组条目。
- 支持搜索、分类筛选和收藏置顶。
- 普通查询模式下，左键查看来源配方，右键查看用途配方。
- 点击配方材料或结果，可继续查询相关配方；泛材料会显示候选条目。
- 可跳转到原版制作面板并选中对应配方。
- 支持中文和 English，可调整面板位置与比例。
- 提供可选的物品领取与复活操作，遵循游戏和服务器权限。

## 安装 / Install

- 先安装 BepInEx 5；它是必需依赖。
- Gale 用户可从 Casualties: Unknown 的模组列表安装 UEI，也可直接导入这个 ZIP。
- 手动安装时，把 ZIP 中的 `BepInEx` 文件夹合并到游戏目录。
- 如果已有旧版 UEI DLL，请先关闭游戏，并只保留一份 UEI 插件，避免重复加载。

Install UEI from the Casualties: Unknown mod list in Gale; the required BepInEx
dependency is installed by the mod manager. Alternatively, import this ZIP into
Gale. For manual installation, install BepInEx 5 first, then merge the ZIP's
`BepInEx` folder into the game directory. Close the game before replacing an
older UEI DLL; keep only one copy of the plugin.

## 使用 / Usage

### English

1. Launch the game with mods enabled and open the native right-click inventory.
2. Use the search box and category arrows to find an entry; click its star to favorite it.
3. Left-click for source recipes, or right-click for usage recipes.
4. Click recipe ingredients/results to follow the recipe chain, or use the jump button to open crafting.
5. Open the gear button beside search to change language, layout, or optional cheat settings.

### 中文

1. 通过 Gale 启动模组游戏，打开原版右键物品栏后即可看到 UEI。
2. 使用搜索框与分类箭头查找条目，点击星标收藏。
3. 左键查看来源配方，右键查看用途配方；点击配方材料 / 结果可继续跳转。
4. 通过详情页的跳转按钮打开原版制作面板。
5. 点击搜索框旁的齿轮，调整语言、布局和可选作弊设置。

Cheat mode is disabled by default. When enabled and permitted, left-click is
used for item requests rather than opening source recipes; right-click still
shows usage recipes.

作弊模式默认关闭。启用且具备权限时，左键用于领取 / 请求物品，右键仍可查看用途。

## 兼容范围 / Compatibility

Version 1.2.4 includes full-game compatibility changes. Local runtime checks have
been performed; the multiplayer checks listed below remain pending.

1.2.4 包含游戏正式版适配，并已进行本地运行检查；下述联机项目仍待实测。

Together、CUCoreLib、RshLib 和 SexMod 都是可选适配，并非必需依赖。
Together 客户端的原版物品领取通过服务器命令处理；自定义模组物品仍需由房主生成。
自动 `cuspawn` 尚未启用；双端同步与保存/重连仍需实机验证。

Together, CUCoreLib, RshLib and SexMod are optional. Vanilla item requests on
Together clients go through the server. Custom mod items must currently be
spawned by the host. Automatic `cuspawn` is not enabled. Two-peer synchronization
and save/reconnect validation remain pending.

本包只包含 UEI，不包含游戏程序集、BepInEx、联机模组或临时测试插件。
This package contains UEI only, not game assemblies, BepInEx, multiplayer mods
or the disposable in-game test fixture.

## @VERSION@ 更新 / What's new

- New square cave-glow UEI icon for Thunderstore and mod managers.
- Updated installation documentation and automatically packaged changelog.
- Gameplay behavior and the multiplayer limitations described above remain those of 1.2.4.

- 更新为 1:1 洞穴荧光图标，用于 Thunderstore 与模组管理器。
- 完善安装说明，自动打包时附带 CHANGELOG.md。
- 功能沿用 1.2.4，上方联机限制仍适用。

## 1.2.4 适配摘要 / Compatibility changes in 1.2.4

- Full-game and Together compatibility changes, with optional CUCoreLib, RshLib and SexMod integration.
- Added a revival button to the cheat controls.
- Fixed cheat permission checks and unintended recipe-panel opening when requesting items.
- Improved catalog refresh, optional-mod detection caching, localization lookup and UI recovery.
- See the Changelog tab for the version's full release notes.

- 适配游戏正式版与 Together，补充 CUCoreLib、RshLib、SexMod 的可选兼容。
- 作弊操作栏新增复活按钮，修复权限判断与左键领取时意外打开配方的问题。
- 优化目录刷新、模组检测缓存、本地化查询与界面恢复。
- 完整更新内容见 Changelog 页签。

## 源码与反馈 / Source and support

- [Source code and releases](https://github.com/MAakber/Unknown-Enough-Items)
- [Report a bug](https://github.com/MAakber/Unknown-Enough-Items/issues)

Author: **Aakber (小叶子)**. Thanks to **dfgger433** for the compatibility contributions.
UEI is licensed under the MIT License included in this package.
