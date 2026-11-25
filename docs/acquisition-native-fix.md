# Native acquisition sources — 1.2.7

Scope: the installed full game and Gale profile `UEI-Test`. The user performs
all in-game testing. Preserve the existing input, performance, localization,
delivery and optional-mod changes. No commit or publication is requested.

## Checkpoint

- Baseline source, staged/unstaged diffs and the installed UEI 1.2.6 DLL are
  backed up under `artifacts/acquisition-native-2026-11-22/baseline/`.
- Baseline DLL SHA-256:
  `1A0458BC8DC33289E77574E469A9D0A19C401C1CE3EC9E23458659F991A62F04`.
- Full-game build reference copies were checked against the current installed
  game and Gale BepInEx core. The preceding regression run passed 1,203 assertions.
- Investigation verified `BuildingEntity` drop fields, the actual
  `ItemLootPool.pool`, trader filtering, filled-container contents, canonical
  resource references, and Unity asynchronous resource-loading signatures.
- Implementation complete: resource metadata IO runs on a worker; each main
  thread frame processes at most one completed prefab and starts one asynchronous
  request. No bulk resource load, instantiation or game command is used.
- Source descriptors preserve canonical resource paths and child variants.
  The reverse index uses kind plus ID, weighted categories and actual loot-pool
  membership. Fixed and always drops retain their probability/count/condition
  semantics. Trader eligibility matches the shipped code, including its separate
  weapon/ammunition inventory path. Liquids identify filled containers and their
  acquisition routes; default amounts are not promises of actual spawn amounts.
- New source data refreshes acquisition/detail text without rebuilding recipes,
  sorting the grid or invalidating the query cache. Registry content is checked
  once per second; same-count replacements and acquisition field edits update
  sources, while entry/category replacements also invalidate catalog metadata.
  Locale and recipe changes reuse prefab descriptors. Loading, missing metadata
  and transient registry failures have explicit states; failed snapshots retain
  the last complete reverse index and recover without rescanning prefabs.
- Initial implementation passed full-game compilation with zero warnings/errors,
  actual native/Unity/optional-mod API checks and all 23 package automation tests.
  The regression coordinator now also covers bounded scheduling, late pool
  initialization, same-count changes and transient failure recovery. Its warmed
  .NET/API-double sample allocates zero bytes for 1,000 ticks; this does not
  establish Unity allocations or frame times.
- Current full-game metadata resolves 561 canonical GameObject resource paths
  with no unavailable external files. No Unity assets or scenes were loaded by
  this filesystem verification.
- Final staged full-game build: zero warnings/errors. Regression passed 1,377
  assertions; all 23 automation tests, version parity/release-note gate and diff
  formatting checks passed. Native/Unity/CUCoreLib/Together API checks passed.
  The compiled trader-rule arrays match literals in the current shipped game.
- The final input transpiler check still routes all 28 shipped-game key reads,
  retaining 381 unrelated instructions and 51 branch target labels. Nine earlier
  input/query/favorite/interop/policy helpers and regression files were verified
  unchanged against the pre-task source backup.
- Installed UEI 1.2.7 in Gale `UEI-Test` after verifying no game process was
  running. Build/installed SHA-256 match; metadata scanning found one UEI plugin
  GUID and the independent `UEI.TestFixture.dll` hash is unchanged. No commit or
  publication was made. All game execution and manual validation remain with
  the user.

## Verified output and installation

Installed DLL:
`%APPDATA%\com.kesomannen.gale\casualties-unknown\profiles\UEI-Test\BepInEx\plugins\UEI\UEI\UEI.dll`.

Version: `1.2.7`. SHA-256:
`ACA81A789BD694EC28B30DD85E6DFDB8B11340205956FA9B77420A00178A423A`.

The staged DLL, build/regression/API/input/package logs, source and reference
hashes, and installation metadata are under
`artifacts/acquisition-native-2026-11-22/final-stage/`.
`native-fixture.json` records 129 canonical root-building configurations resolved
from resource references, separately from all 361 serialized building records.
The runtime also reads child entities through Unity APIs; no claim is made that
the offline root fixture covers every child or that Unity loading was exercised.
`Read-NativeFixture.ps1` retains the validated local-data reader. These local
game/reference artifacts are ignored by Git and excluded from release sources.

## Reproduce

Run from the source repository, using the checked full-game reference copies:

```powershell
$taskRoot = Join-Path $PWD 'artifacts\acquisition-native-2026-11-22'
$taskReferences = Join-Path $PWD 'artifacts\input-performance-2026-11-22\references'
$taskGameAssembly = '<游戏目录>\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll'
$taskProfile = Join-Path $env:APPDATA 'com.kesomannen.gale\casualties-unknown\profiles\UEI-Test'
$taskTogether = Join-Path $taskProfile 'BepInEx\plugins\CasualtiesTogether-CasualtiesTogether\CasualtiesTogether-CasualtiesTogether'

dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD" `
  '<游戏目录>\CasualtiesUnknown_Data'
python -m unittest discover -s tests/automation -p 'test_*.py'
python scripts/ci.py version
dotnet build UEI.csproj -c Release `
  "-p:GameRoot=$taskReferences" `
  "-p:OutputPath=$taskRoot\final-stage\" `
  "-p:BaseIntermediateOutputPath=$taskRoot\obj-final\" `
  "-p:IntermediateOutputPath=$taskRoot\obj-final\" `
  "-p:MSBuildProjectExtensionsPath=$taskRoot\obj-final\"
./tests/Verify-ApiContracts.ps1 `
  -CecilPath "$taskProfile\BepInEx\core\Mono.Cecil.dll" `
  -CuCoreLibPath "$taskTogether\CUCoreLib.dll" `
  -TogetherPath "$taskTogether\Together\CasualtiesMP.dll" `
  -PluginPath "$taskRoot\final-stage\UEI.build.dll" `
  -GameAssemblyPath $taskGameAssembly
dotnet run --project "$PWD\artifacts\input-performance-2026-11-22\patch-check\PatchCheck.csproj" -c Release -- `
  "$taskRoot\final-stage\UEI.build.dll" $taskGameAssembly "$taskProfile\BepInEx\core"
git diff --check
```

Rebuilding may change output hashes. Recheck the saved output and manifest before
using `Install-VerifiedUei.ps1`; it verifies the backup, output and installed
hashes, unique plugin identity, independent fixture and closed-game state.

## Data coverage

The game has no acquisition field on `ItemInfo`. UEI reads drop information from
`BuildingEntity`, the current `ItemLootPool.pool`, trader selection rules and
`LiquidItemInfo.defaultContents`, rather than copying a Wiki drop table.
`UeiI18n.BuildingText` reads the live game building locale; interface wording
continues to follow UEI's independent Chinese/English setting.

Verified canonical crate configuration in this installed full game:

| Resource | Random draws | Category weights |
| --- | ---: | --- |
| `foodbox` | 1 | food ×2, water |
| `medcrate` | 1 | medical ×5, drug ×2, water |
| `dropcapsule` | 2 | medical ×3, food ×2, water ×2, utility ×2, container ×2, drug ×2, tool |
| `lifepodchest` | 2 | medical ×3, food ×2, water ×2, utility ×2, container ×2, drug ×2, tool, trash |
| `containercrate` | 1 | utility ×2, container ×2, tool, medical |

Medical crates also roll human blood bags at 0.05 × 0.67, with condition 0.5–1.
Life-pod chests roll a blueprint at chance 1, condition 1. Repeated categories
and pool members remain weights; two random draws do not guarantee any one
specific item. Empty/missing pool slots prevent reporting a precise probability.

Terrain generation, special scripts, events and all custom mod sources are not
fully covered. Explicit acquisition annotations remain labelled supplements,
including their special conditions. Crafting recipes stay below the acquisition
card. A completed empty result means no known acquisition info, not unobtainable.

Wiki MediaWiki/Bucket APIs were verified during investigation, but this change
does not send runtime HTTP requests or redistribute Wiki content. Unsupported
resource formats retain registry/description information and disclose incomplete
coverage. The metadata reader supports the installed Unity serialized format 22
with disabled type trees and validates file/object/path bounds.

## 哥哥的游戏内测试

1. 从 Gale `UEI-Test` 启动正式版，确认 BepInEx 加载 UEI `1.2.7`。
2. 使用游戏官方英文文本查看柠檬水罐和绷带。来源不应依赖“获取：”标注；
   首次扫描未完成时应提示继续读取，完成后提示消失。关闭物品栏时也继续读取。
3. 柠檬水罐应包含医疗箱、食物箱、空投舱、维生舱箱子和商人随机库存来源；
   箱子来源应注明随机抽取与该物品的概率，不应把随机抽取次数当成必得数量。
4. 在液体分类查看柠檬水：应说明柠檬水罐的默认内容为 750 mL，并列出罐子的
   获取途径。它应与同 ID 的罐子条目分开，实际生成液量可能随状态变化。
5. 检查绷带的箱子/交易来源与制作配方可同时显示；人类血袋的医疗箱固定
   判定为 3.35%、耐久 50%–100%。嵌套实体来源应说明所在预制件，不合并变体。
6. 切换游戏语言、UEI 中英界面及存档，确认名称和“描述补充”条件正确；
   来源缓存仍应复用。日志的资源队列与扫描完成记录每个游戏进程只出现一次。
7. 搜索、翻页、收藏、查看用途与长来源卡片滚动仍应正常。首次扫描过程中
   以及完成后的面板静止/连续搜索场景，由哥哥观察帧时间与卡顿。

No in-game operation, scene validation, FPS measurement or multiplayer session
was performed by the agent.

## Recovery

Source backups contain all pre-existing tracked and untracked source files.
Restore only this task's edits when necessary; do not overwrite earlier dirty
work wholesale. Build into a separate artifact directory using the full-game
references, never the legacy `build.ps1` Demo path.

Installed DLL:
`%APPDATA%\com.kesomannen.gale\casualties-unknown\profiles\UEI-Test\BepInEx\plugins\UEI\UEI\UEI.dll`.

To revert the installed build, close the game and copy
`artifacts/acquisition-native-2026-11-22/baseline/Gale-UEI-1.2.6-1A0458BC.dll.bak`
over that DLL. Keep backups outside the plugins folder and retain one active
plugin with GUID `casualtiesunknown.uei`. Preserve `UEI.TestFixture.dll`.

No game process has been launched, and no FPS/runtime validation is claimed.
