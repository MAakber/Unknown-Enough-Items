# UEI — Unknown Enough Items @VERSION@

Casualties: Unknown 的物品、液体与配方查询插件，界面支持简体中文和 English。

An item, liquid and recipe browser for Casualties: Unknown, with a Chinese and English UI.

中文说明在前，English version follows below.

## 中文

UEI 的灵感来自 Minecraft 的 JEI。打开原版物品栏时，它会在旁边显示一个查询面板，帮你查物品和液体的获取途径、制作配方与用途。原版制作系统保持不变。

### 功能

- 浏览游戏运行时的全部物品和液体，包括受支持模组添加的条目。
- 按名称搜索、按分类筛选，收藏的条目排在最前。
- 左键物品或液体查看获取途径和制作配方，右键查看用途。
- 获取途径和制作配方使用图标卡片，默认每页 2 行，可在设置中改为 1–4 行；液体显示来源、容器和液体的关系，特殊描述条件保留在「描述补充」。
- 点击配方里的材料或产物继续查询；悬停泛材料可查看符合条件的候选，数量较多时只显示一部分。
- 一键打开原版制作面板中的对应配方，或直接收藏这条配方。
- 可调整界面语言、面板位置、大小和每页卡片行数。
- 实体分类收录商人、生物和场景物件，也可在「全部」中搜索和收藏；点击可查看原文描述。
- 可选作弊功能：领取物品和液体、生成实体、一键复活。默认关闭，受游戏和服务器权限限制。

### 安装

- **Gale**：在 Casualties: Unknown 的模组列表中搜索 UEI 并安装，BepInEx 会自动一起安装。也可以把下载的 ZIP 直接导入 Gale。
- **手动**：先安装 BepInEx 5，再把 ZIP 中的 `BepInEx` 文件夹合并到游戏目录。
- 升级前请先关闭游戏，并确认只保留一份 UEI。

### 使用

1. 进入游戏，打开原版右键物品栏，UEI 面板会出现在旁边。
2. 用搜索框和分类箭头查找条目，点击星标收藏。没有可用图标的条目显示黑紫棋盘格。
3. 普通浏览时，左键物品或液体打开来源页，用上方按钮切换获取途径、制作配方和描述补充；右键打开用途页。实体左右键都显示原文描述，保留颜色、字号和图标，长内容可滚动。点击容器、材料或产物可以继续查。
4. 翻页：点击箭头，或在面板上滚动滚轮、按 PageUp / PageDown。
5. 点击搜索框旁的齿轮，可调整语言、位置、比例、每页卡片行数（1–4 行）和作弊开关。

开启作弊后，右键仍查看详情，左键改为：物品领取 1 个；液体领取装满该液体的临时瓶（默认 1000 mL，5 分钟后消失），背包放不下时瓶子留在脚边；实体进入定位，背包自动收起，移动鼠标选择落点，再次左键生成 1 个。右键或 Esc 取消定位，背包键取消并打开背包。

定位时显示半透明的静态 Sprite 预览，保留实际尺寸、原点、翻转和可见贴图层，不包含动画、线条等渲染方式；预览只是本地画面，确认前不会创建可交互实体。抓缠草这类没有可用预览的实体只显示文字提示，仍可生成。

### 兼容性与已知限制

- Together、CUCoreLib、RshLib、SexMod 都是可选适配，不安装也能正常使用。
- Together 联机：房主需开启 `sv_cheats`；客户端需要服务器授予的管理员命令权限，服务器执行时仍会检查 `sv_cheats`，所以能打开开关不代表请求一定成功。客户端的领取和生成都通过服务器命令提交，液体领取需要房主也安装同一版本的 UEI，模组物品需由房主生成。双端同步、存档与重连尚未完整实测。
- 获取途径读取当前游戏的实体掉落字段、实际掉落池、商人随机库存规则和液体容器内容，无需中文补丁或联网；描述标注保留为补充。首次读取分批进行，完成前来源可能继续补充。
- 随机来源注明抽取与概率条件。地形生成、脚本事件及全部模组来源尚未覆盖；读取失败会说明信息可能不完整，缺少信息不代表无法获得。
- 本包只包含 UEI，不含游戏文件、BepInEx 或其他模组。

### 链接与致谢

- 源码与发布：[GitHub](https://github.com/MAakber/Unknown-Enough-Items)
- 问题反馈：[GitHub Issues](https://github.com/MAakber/Unknown-Enough-Items/issues)
- 各版本的更新内容见 Changelog 页签。

作者：**Aakber（小叶子）**。感谢 **dfgger433** 的兼容性贡献。UEI 使用 MIT 许可证，许可证文件随包附带。

## English

Inspired by Minecraft's JEI, UEI shows a lookup panel beside the game's inventory, so you can check how items and liquids are crafted, what they are used for and, where available, where to find them. The game's own crafting system is left untouched.

### Features

- Browse every item and liquid loaded at runtime, including entries added by supported mods.
- Find merchants, creatures and world objects under Entities or All, using the same search and favorites; click one to read its original description.
- Search by name and filter by category; favorites are listed first.
- Left-click an item or liquid for its acquisition info and crafting recipes; right-click for its uses.
- Acquisition and crafting use icon cards, two rows per page by default and 1–4 rows in settings. Liquid cards show their source, container and liquid; special description conditions remain under Notes.
- Click an ingredient or result to look it up; hover a generic ingredient to see matching candidates (long lists show only some of them).
- Open the game's crafting panel at the current recipe, or pin that recipe.
- Change the UI language, panel position, scale and cards per page.
- Optional cheats: give yourself items or liquids, spawn entities, or revive. Off by default and subject to game and server permissions.

### Install

- **Gale**: search for UEI in the Casualties: Unknown mod list and install it; BepInEx is installed automatically. You can also import a downloaded ZIP into Gale.
- **Manual**: install BepInEx 5 first, then merge the ZIP's `BepInEx` folder into the game folder.
- Close the game before updating, and keep only one copy of UEI.

### Usage

1. In game, open the native right-click inventory; the UEI panel appears beside it.
2. Find entries with the search box and category arrows; click the star on an entry to favorite it. Entries without a usable icon show a black/magenta checkerboard.
3. During normal browsing, left-click an item or liquid for its Source page, then switch between Acquisition, Crafting recipes and Notes above the cards; right-click for its Uses page. Entities show their original description on either click, keeping colors, sizes and sprites, and long text scrolls. Click containers, ingredients or results to keep exploring.
4. To change pages, click the arrows, or use the mouse wheel or PageUp / PageDown over the panel.
5. Click the gear next to the search box to change the language, position, scale, cards per page (1–4 rows) and cheat setting.

With cheats on, right-click still opens details, and left-click depends on the entry: an item gives you one; a liquid gives you a full temporary bottle (1000 mL by default, disappears after five minutes), left at your feet if your inventory is full; an entity starts placement, closing the inventory so you can choose a spot with the mouse, and a second left-click spawns one there. Right-click or Esc cancels placement; the inventory key cancels and opens the inventory.

Placement shows a translucent static-sprite preview with the entity's actual size, origin, flipping and visible sprite layers, but without animation, lines or other renderers. The preview is only a local image; no interactive entity exists until you confirm. Entities with no usable preview, such as the Tentacle-plant, show a text hint instead and can still be spawned.

### Compatibility and known issues

- Support for Together, CUCoreLib, RshLib and SexMod is optional; UEI works without them.
- Together multiplayer: the host needs `sv_cheats` enabled. Clients need admin-command permission from the server, and the server still checks `sv_cheats` when it runs a request, so an enabled switch does not guarantee success. Clients give and spawn through server commands; liquid delivery needs the same UEI version on the host, and mod items must be spawned by the host. Synchronization between players, saving and reconnecting have not been fully tested.
- Acquisition info uses current building drops, actual loot pools, random trader-stock rules and filled-container contents. No Chinese patch or network connection is required; description annotations remain supplements. The first scan runs in batches and may add more sources as it finishes.
- Random sources include draw/probability conditions. Terrain generation, scripted events and all mod sources are not fully covered. Failed reads disclose incomplete coverage; missing information does not mean an entry is unobtainable.
- This package contains UEI only: no game files, BepInEx or other mods.

### Links and credits

- Source code and releases: [GitHub](https://github.com/MAakber/Unknown-Enough-Items)
- Bug reports: [GitHub Issues](https://github.com/MAakber/Unknown-Enough-Items/issues)
- See the Changelog tab for what's new in each version.

Author: **Aakber (小叶子)**. Thanks to **dfgger433** for compatibility contributions. UEI is licensed under the MIT License included in this package.
