# CUCoreLib 公开接口与专项测试计划

日期：2026-11-21。候选版本：UEI `1.2.4`；基线：已安装的 `1.2.3`。

## 已完成的适配

- 优先绑定公开 `ItemRegistry.TryGetIcon(string, out Sprite)`、
  `TryGetCustomInfo(string, out CustomItemInfo)` 和 `TryGetItemInfo(string, out ItemInfo)`。
- 单机/房主端的 CUCoreLib 物品使用公开
  `CustomInstantiate.InstantiateReturn(string, Vector3, Quaternion, float?)`。
  RshLib/SexMod 的兼容路径保留；没有引入 CUCoreLib 的强制程序集依赖。
- 使用公开 `MultiplayerApi.IsAvailable/IsRunning/IsClient/IsServer` 读取活动角色；
  未初始化的桥接回退到原生判断，矛盾状态拒绝发放。
- 方法绑定只接受公开类型上的公开静态方法，缓存成功与失败，程序集加载时刷新。
- 移除对内部 `LocaleLoader` 的反射。游戏公开 `Locale` API 已包含 CUCoreLib 的
  overlay 与只读拦截；缺失名称/说明再读已注册的公开 ItemInfo 数据。
  不调用会修改 required/network locale 数据的 `LocaleRegistry.Get`。
  ItemInfo API 会记录查询失败，部分情况下还会注入条目，因此不能拿任意翻译键直接查询它。

核实依据：CUCoreLib 官方源码 `jimmyking9999999/CUCoreLib`，检查提交
`cb5181e0d2e24ab1dc09ba9282000c624ff41d52` 的相关文件，并用实际安装的
CUCoreLib `1.1` / Together `5.5.4` DLL 元数据交叉核对公开性及精确签名。
源码检查与元数据检查不能代替游戏运行测试。

## cuspawn 的边界

目前仅列为受控实测候选，**未接入 UEI 自动发放**。

- 原生 `give` 只走 Resources；无法生成当前这类 CUCoreLib 自定义物品。
- `cuspawn <id> <position> <condition> <count>` 使用 CUCore 的自定义生成器，
  但在世界中生成物品，不是直接放入某位玩家的背包。
- Together 会验证远程管理员，并会把位置参数 `player` 改写为命令调用者。
  需要验证物品确实出现在请求客户端附近、两端都能看到、能拾取并能重连保存。
- 当前检查到的 cuspawn 实现没有 give 的同款 `sv_cheats` 检查，而且在读取显式位置前
  会访问 Camera.main，因此还不能宣称支持独立服务器或满足相同的作弊规则。
- 如果 sv_cheats=false 时 cuspawn 仍生成物品，判定其不具备直接作为 UEI 后备命令的条件。
  不通过客户端本地生成、伪造成功提示或未验证的自建网络协议绕过这些限制。

## 测试环境和存档

- 专用 Gale 档案：`UEI-Test`。**独立 Gale 档案本身不会隔离 Unity 存档。**
- 临时测试插件的源码、DLL、测试包均放在 UEI 仓库外，不提交或放入 UEI 发布包。
- 测试插件只能在名为 UEI-Test 的档案中注册物品；使用公开 CUCoreLib 注册接口创建
  `uei_fixture.red_token` 与 `uei_fixture.blue_token`，带不同的程序绘制图标与中英文文本。
- 两物品使用 `category=nospawn`、`SpawnFrequency=0`、空 DropPool、零 WorldSpawnPerChunk。
  运行日志还须验证 `registered=2; lootEntries=0`。
- 测试插件单独重定向 Together 的公开存档路径与原版 SaveSystem 存档入口，目标限定为
  `UEI-Test/UEI-Test-Saves`。安装钩子失败时，不注册测试物品。
  路径单测只证明路径约束，**还必须先在主菜单核实运行日志中的隔离成功标记**。
- 房主/客户端使用同一份测试插件、UEI 和依赖，核对 DLL 哈希。
  只新建测试存档；不要打开任何常用存档，也不要提前删除仍用于测试存档的插件。

## 双端验证矩阵（待实测，不预填通过）

准备状态：Gale 的 UEI-Test 已创建；四个本地测试包已安装并启用，版本分别为
BepInEx 5.4.2305、Together 5.5.4、UEI 1.2.4、测试插件 0.1.0。
安装 DLL 的哈希与构建产物一致；Default 中的 UEI 1.2.3 哈希保持不变。
安装校验和测试包清单保存在仓库外测试目录中，未将测试插件加入本仓库。
UEI 编译、公开 API 元数据校验、回归检查与测试插件路径单测已通过；下表仍等待实际双端运行。

| 项目 | 操作与验收 | 状态 |
|---|---|---|
| 启动与隔离 | 日志有 TEST_SAVE_ISOLATION_READY，路径在 UEI-Test 内；未见测试插件钩子错误 | 隔离钩子返回路径已确认；实际写盘/读档待测 |
| 注册与掉落池 | 日志 registered=2、lootEntries=0；目录从基线 375 增至 377 项 | 日志确认；截图可见两个条目 |
| 本地显示/持有 | 用户截图显示两个测试物品均已被角色持有，未见 CUCore 工厂异常 | 已观察；截图不单独证明领取入口 |
| 图标/本地化 | 红方块/蓝菱形显示不同；游戏中切换中英文，名称和说明随之变化 | 中文名称和图标已观察；英文待测 |
| 房主发放 | sv_cheats=true，UEI 左键测试物品；核实背包实际得到、另一端也可见 | 待测 |
| 管理员客户端 | UEI 当前应明确提示需房主生成，不本地生成，不伪称已发放 | 待测 |
| give 失败及候选 | 管理员客户端先确认 give 不支持该资源，再手工测试 cuspawn，核对生成位置、两端可见与拾取 | 待测 |
| 非管理员客户端 | UEI 权限提示明确；服务器拒绝其远程生成命令，物品数量不增加 | 待测 |
| sv_cheats=false | 记录 UEI/原生命令提示；检查 cuspawn 是否仍生成。若生成，候选方案不得上线 | 待测 |
| 重连与读档 | 两端保持插件安装，保存测试运行后退出/重连，核实物品 ID、数量、图标和属性 | 待测 |

2026-11-21 首轮运行证据：UEI 1.2.4、测试插件 0.1.0、游戏 1.0.2。
日志中的 `network=False` 表明本轮尚未建立实际联机会话，不能将本地持有物品写成房主/客户端发放通过。
本轮没有测试 save.sv 写盘或重连。截图里的 Together 单人提示也不证明客户端连接成功。
日志还包含 Together 的 MovementCourse.Start 缺失补丁和 EvidenceCache.Arm IL 编译异常；
相同异常已出现在原 Default 日志中，早于 UEI 加载，不能归因于本次测试插件或新 UI 改动。

普通房主/客户端环境中的受控候选命令（不是独立服务器支持承诺）：

```text
give @m uei_fixture.red_token
cuspawn uei_fixture.red_token player 1 1
```

必须开启服务器管理员命令转发；cuspawn 之后需要检查世界中的物品并手动拾取。
不要仅凭“已请求”判断成功，也不要将上述手工测试写成 UEI 已支持自动回退。

## 清理与合并门槛

完成双端测试后先关闭两端游戏，再删除专用测试档案及它内部的测试存档，最后移除仓库外的
临时插件/测试包。保留验证记录和必要的错误日志，不修改 Default 或常用存档。
测试尚未完成时保持插件与测试存档配套，不提前清理，以便检查重连和读档。

当前不以“可编译”替代上述实测结果；PR #3/#4 的既有贡献和问题归属详见整合记录。
若 cuspawn 不满足权限/同步要求，可以保留客户端限制并单独推进经过验证的公开接口适配。
