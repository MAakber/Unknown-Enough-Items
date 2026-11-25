# Input and performance fixes — 1.2.6

Scope: current full game and Gale profile `UEI-Test`. In-game testing belongs to
the user. Keep the existing acquisition, localization, inventory-delivery and
optional-mod changes in the working tree.

Baseline installed DLL: `1.2.5`, SHA-256
`09E22518B1675EFFDBB90116A28BD97B0DBDA7EA439A680791C89991055DD7DD`.
Baseline source/DLL backups and full-game reference copies are in
`artifacts/input-performance-2026-11-22/baseline/` and `references/`.
Baseline regression harness: 1,155 assertions passed.

## Checkpoint

- Input implementation complete: route KeyCode reads inside the original
  `PlayerCamera.HandleInput`, preserve movement/navigation on hover, clear latched
  movement during text editing, capture UEI clicks through release, and guard the
  actual attack/start-drag/release-drag methods found in the full-game assembly.
- Input stage: 1,173 regression assertions passed; full-game build has zero
  warnings/errors. Compiled transpiler check routes all 28 shipped-game key
  reads, preserving 381 unrelated instructions and 51 branch target labels.
  Synthetic exception metadata and string-overload checks also passed.
- The offline checker uses Cecil and .NET Framework 4.8. Gale's Harmony depends
  on an Exception remoting helper absent from .NET 10; the first .NET 10 checker
  attempts failed and were replaced, without changing the game or Harmony.
- Performance implementation complete: reuse 60 grid cells/icons, bind clicks
  to the current entry, update an already-hovered tooltip, and keep empty/loading
  cells inert. Sort only when the source/catalog/favorites change; reuse the
  current filtered list and merge search edits into the next visible update.
- Pointer detection uses existing rectangles/projected corner bounds, without
  allocating PointerEventData or a RaycastAll results list. Animation progress
  stops advancing at its target and identical alpha/interaction/position writes
  are skipped. Visibility, settings, layout and canvas-scale changes still apply,
  including changes within the same frame.
- Final full-game build: zero warnings/errors. Final harness: 1,203 assertions
  passed. The isolated helper allocated zero bytes for 1,000 repeated warmed
  queries on .NET; this does not establish allocation or FPS behavior in Unity.
- All 23 automation/package tests, the version/release-note gate, the compiled
  full-game input signatures, and the installed CUCoreLib/Together API contracts
  passed. The final DLL's transpiler check also passed.
- Installed UEI `1.2.6` in Gale `UEI-Test`; installed/build SHA-256 match. Metadata
  scanning found exactly one plugin with GUID `casualtiesunknown.uei`. The
  separate `UEI.TestFixture.dll` belongs to another plugin and was retained.
- Code/build/install work is complete. In-game input, layout, frame-time and
  multiplayer tests remain with the user. No commit or publication was made.

## Installed build and recovery

Installed DLL:
`C:\Users\10178\AppData\Roaming\com.kesomannen.gale\casualties-unknown\profiles\UEI-Test\BepInEx\plugins\UEI\UEI\UEI.dll`

Version: `1.2.6`. SHA-256:
`1A0458BC8DC33289E77574E469A9D0A19C401C1CE3EC9E23458659F991A62F04`.

Verified output and metadata/API/transpiler records:
`artifacts/input-performance-2026-11-22/final-stage/`.
`verification.json` records source/reference hashes and the dirty working tree;
this is a local working build rather than a published release.

Old DLL backup:
`artifacts/input-performance-2026-11-22/baseline/Gale-UEI-1.2.5-09E22518.dll.bak`.
To restore, close the game and copy this backup over the installed DLL above.
Keep the backup outside the plugin directory and retain a single active UEI DLL.

The installer first checks the old/build/backup hashes and game process state,
then replaces the DLL via a same-directory staged file. Its first filename-based
duplicate check mistakenly included the independent test fixture; the final
check uses BepInPlugin GUIDs. PowerShell requires `[NullString]::Value` for the
null backup-path argument to `File.Replace`. Both failed installation attempts
left the original installed DLL unchanged; the corrected replacement succeeded.

## Manual tests

- Hover list/detail/settings while moving, changing direction, releasing keys,
  jumping, toggling inventory and pausing.
- Click/hold/release UEI buttons with the pointer entering/leaving the panel;
  check that no attack, world pickup, drop or inventory close leaks through.
- Search while already moving/jumping; verify motion stops, typing stays in the
  search field, and Escape/Tab can dismiss search through the native game logic.
- Drag an existing inventory item onto UEI and release; the item should stay
  where it was and the drag ghost should disappear.
- Rapidly search/page/favorite items, switch recipes and languages,
  resize/reposition panels, and traverse menu → game → menu.

No Unity execution, frame-time measurement or live multiplayer verification has
been performed by the agent.

## 哥哥的游戏内测试顺序

1. 从 Gale 的 `UEI-Test` 启动正式版，确认 BepInEx 日志加载的是 UEI `1.2.6`。
2. 打开物品栏，鼠标放在列表、详情和设置上，测试移动、松开方向键、跳跃，以及绑定的物品栏和暂停按键。
3. 已经移动或跳跃时点进搜索框：角色应停止接收操作，输入内容不应触发游戏动作；退出搜索后再测试移动。
4. 在 UEI 内按住鼠标，移到世界后再松开，检查是否误攻击、拾取或丢弃；再从物品栏拖物品到 UEI 上释放，检查拖拽影像消失、物品仍在原处。
5. 连续搜索、翻页和收藏，保持鼠标不动，核对图标、提示框与左/右键操作是否对应当前格子的物品；无结果和空格子应无物品操作。
6. 调整左右位置、比例和语言，往返主菜单与存档；观察动画结束后的面板是否正常响应。性能对比请使用同一存档、同一模组组合，分别观察面板静止和连续搜索/翻页时的卡顿。

## Reproduce the offline checks

Run in the repository directory. All build references below are copies of the
current full-game managed assemblies and this Gale profile's BepInEx core.

```powershell
$taskRoot = Join-Path $PWD 'artifacts\input-performance-2026-11-22'
dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD"
python -m unittest discover -s tests/automation -p 'test_*.py'
python scripts/ci.py version
dotnet build UEI.csproj -c Release `
  "-p:GameRoot=$taskRoot\references" `
  "-p:OutputPath=$taskRoot\final-stage\" `
  "-p:BaseIntermediateOutputPath=$taskRoot\obj-final\" `
  "-p:IntermediateOutputPath=$taskRoot\obj-final\" `
  "-p:MSBuildProjectExtensionsPath=$taskRoot\obj-final\"
dotnet run --project "$taskRoot\patch-check\PatchCheck.csproj" -c Release -- `
  "$taskRoot\final-stage\UEI.build.dll" `
  'D:\SteamLibrary\steamapps\common\Casualties Unknown\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll' `
  'C:\Users\10178\AppData\Roaming\com.kesomannen.gale\casualties-unknown\profiles\UEI-Test\BepInEx\core'
```

Run `tests/Verify-ApiContracts.ps1` as described in `tests/README.md`, passing
the current full-game assembly and Gale CUCoreLib/Together paths. Rebuilding
replaces the local output; check its hash again before installing any later edit.
