<!-- version: 1.2.10 -->

缺少可用图标的条目改用黑紫棋盘格标识，空格子保持空白。实体图标会先复用已有可见贴图，避免第一个组件无图时误显示缺图；占位图仅用于界面，不替代生成物外观。

Missing icons now use a black/magenta checkerboard while empty cells stay empty. Entity icons first reuse an available visible sprite instead of stopping at an empty renderer. The placeholder is UI-only and never substitutes for an entity's world appearance.

实体鼠标定位改用半透明的实体外观预览，保留实际尺寸、原点、可见贴图层及翻转，替换落点十字和悬浮图标。预览只创建本地绘图组件，确认后才生成可交互实体。

Mouse placement now previews the translucent entity appearance at its actual scale and origin, including visible sprite layers and flipping. It replaces the crosshair/floating icon with local visual-only geometry; confirmation still creates the interactive entity.

新增「实体」分类，商人、生物和场景物件也可在「全部」中查找和收藏。作弊模式下点击实体进入鼠标定位，再次左键在所选位置生成 1 个，右键或 Esc 取消；物品照常领取，液体装入原生临时瓶交给玩家（默认 1000 mL，5 分钟后消失）。实体描述直接渲染原文富文本，按可用空间显示并滚动，不拆分或改写内容。联机请求由服务器执行，液体领取需要房主同步更新 UEI。

Added an Entities category, also included in All and favorites. Cheat entity clicks now select a mouse-placement target; the next left click spawns one there, while right-click or Esc cancels. Original entity rich text is rendered without splitting or rewriting its contents, with space-aware scrolling. Items retain their give behavior, and liquids use a full temporary bottle (1000 mL by default, expires after five minutes). Multiplayer liquid delivery requires the updated UEI on the host. No new buttons or settings.

本版合并同一对象 ID、掉落规则相同的获取途径卡片，减少独立与嵌套来源的重复展示。合并后的主标题使用对象名称，原始来源及上下文保留在悬浮说明中；概率、抽取次数和产物数量不会相加。

合并使用未舍入的概率、次数、耐久、获取动作和液体容器及体积；不同对象 ID、不同规则和信息不完整的来源仍分开显示。来源补齐使用稳定的卡片标识，新增相同来源形态不会改变已有分组的顺序。图标可从同组已扫描来源中取得，不增加资源加载。

保留「每页卡片行数」设置，可选择 1–4 行，默认 2 行并自动保存；获取途径、制作配方和用途共用该设置。调整后保留当前浏览位置，卡片内容按可用高度等比缩小以适应面板。

原生获取途径继续使用制作配方风格的图标卡片，上方按钮可切换获取途径、制作配方和描述补充。

## 获取途径

- 卡片左侧展示来源，右侧展示物品；液体展示来源、容器和液体的关系，容器图标可点击继续查询。
- 概率、抽取次数和耐久直接显示在卡片上，完整条件保留在悬浮说明；随机抽取次数不显示为产物数量，液量仍注明默认值。
- 复用制作卡片的背景、像素边框和图标尺寸，来源实体图标复用首次扫描读取的数据，不增加预制件加载。
- 获取与制作的页码分别保留，来源补齐保留当前卡片锚点；特殊描述在独立的补充卡片中完整保留并可滚动阅读。
- 读取当前预制件的实体掉落字段与实际掉落池，反查箱子及其他实体来源；注明随机抽取、独立掉落概率和耐久条件，区分预制件与嵌套变体。
- 商人随机库存遵循游戏的掉落池、价值和不可交易限制，特定商人的额外武器、弹药库存单独显示。
- 液体从容器默认内容推导来源，并显示容器的获取途径；物品与液体即使 ID 相同也分别索引，默认液量不当作实际生成量保证。
- 首次读取使用后台资源索引与主线程分批异步加载，完成后缓存复用。新增来源只刷新详情，不重建配方、排序或搜索列表；加载和失败状态均有提示。
- 描述中的“获取：”、`Acquisition:` 等内容作为补充保留，包含特殊条件。制作配方仍单独显示。

## 保留的输入与界面性能修复

- 鼠标悬停 UEI 时保留原版移动、跳跃、物品栏和暂停按键；搜索输入时阻止游戏按键并清除残留移动与跳跃状态。
- UEI 内按下的鼠标按钮持续拦截到释放，避免移出面板后误触攻击、拾取或丢弃。搜索可通过绑定的 Escape、Tab 或功能键退出。
- 列表固定复用 60 个格子与图标；重新绑定时更新点击目标和悬停提示，空格子不会继续操作旧物品。
- 按目录与收藏变化重排，缓存当前分类与搜索结果；连续搜索输入合并到界面更新时处理。
- 指针检测使用面板边界，去掉额外的全界面射线检测；动画结束后停止重复写入界面属性，位置、比例与设置变化仍可更新。
- 保留获取途径显示、中文/英文说明和现有物品领取逻辑。

## 兼容范围

- 沿用 1.2.4 的游戏正式版与 Together、CUCoreLib、RshLib、SexMod 兼容逻辑。
- 地形生成、脚本事件及全部模组自定义来源尚未覆盖；缺少信息不代表物品无法获得。
- Together 客户端的模组物品仍需房主生成；自动 cuspawn 尚未启用。
- 双端同步、权限边界以及保存/重连仍需进一步实机验证。
- 新卡片的实际排版、交互和帧时间仍需游戏内验收；编译与离线回归不模拟 Unity 界面。

## 安装

从 Gale 更新已发布版本，或导入对应 Release ZIP；本地工作构建的具体安装位置和恢复方法见修复记录。
手动安装时先安装 BepInEx 5，再将 UEI.dll 放入 BepInEx/plugins/UEI/。
升级前关闭游戏，并只保留一份 UEI DLL。

Version 1.2.10 merges acquisition cards for the same source object ID when
their exact drop rules match. The main title uses the source name, while
tooltips retain all original source contexts. Probabilities, rolls and output
quantities are not added. Different IDs, methods, probabilities, conditions,
filled containers, volumes and incomplete rules remain separate. Stable group
identities and ordering preserve the current page as matching variants arrive.
Source icons reuse already scanned variants without additional resource loads.

Cards per page remains available in UEI settings. Choose 1–4 card rows for
acquisition, crafting and uses, with 2 rows by default. The choice is saved,
your browsing position is retained, and card contents scale uniformly to fit.
Acquisition routes retain their recipe-style icon cards.
Switch between acquisition, crafting and description
notes above the cards. Liquid routes show their source, filled container and
liquid; container icons can be clicked to continue browsing. Cards retain
probability, roll and condition information, while full conditions remain in
tooltips. Random draws are not output counts, and liquid amounts are still
defaults. Source sprites reuse the existing scan without additional prefab
loads. Acquisition and crafting pages are preserved separately.

Native sources still derive from current building drops, weighted loot pools,
trader rules and filled-container contents, without a translation patch or
network requests. Description annotations remain supplements. Bounded async
loading caches sources; new sources refresh detail text without rebuilding
recipes or the search list. Terrain, scripted and custom sources are not fully
covered, and in-game validation remains pending. The retained input fixes
preserve movement and navigation while hovering UEI, isolate text
input, and captures UEI mouse presses through release. The list reuses 60 cells,
refreshes their current click/tooltip bindings, caches catalog queries and stops
redundant raycasts and settled-animation writes. Acquisition display and existing
item-delivery behavior are retained. Full-game compilation and offline checks
passed; the new layout's in-game interaction and frame-time verification are pending. Custom mod items
on Together clients still require host spawning; automatic cuspawn is disabled,
and two-peer/save/reconnect validation is pending.
