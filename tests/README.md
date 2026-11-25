# UEI integration regressions

Run from the repository root with .NET SDK 10:

```powershell
dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD"
```

The executable links the production `UeiModInterop.cs`, `UeiCheatPolicy.cs`,
`UeiI18n.cs`, `UeiAcquisition.cs`, `UeiAcquisitionIndex.cs`, `UeiAcquisitionCards.cs`,
`UeiAcquisitionSupplement.cs`, `UeiResourceIndex.cs`,
`UeiNativeAcquisition.cs`, `UeiInput.cs`, `UeiFavorites.cs`,
`UeiCatalogView.cs`, and `UeiEntryInteraction.cs` files from `src/`. It deliberately loads a separate fixture
assembly only after testing absent mods. Fixtures include ambiguous overloads, a registry initialized
after assembly load, registry replacement, and a mutating locale API that must
never be called. No test packages or game binaries are bundled.

Coverage:

- The production catalog/models now run in regressions: entity/category/All/favorite
  identity, native resource paths, merchant variants, live localization, late
  discovery without recipe rebuilding, and item/entity recipe separation.
- Native spawn arguments and routing, temporary bottle fill/capacity/300-second
  cleanup, failed pickup recovery, authenticated caller targeting, host cheat
  rechecks, client/unknown rejection and preserved natural trader ranges.
  These use API doubles, not Unity lifetime or multiplayer simulation.
- The production entity placement controller runs with controllable camera,
  input, scene and UI-raycast boundaries. Tests cover selection without spawning,
  next-press confirmation, complete gesture capture, projected native coordinates,
  UI/invalid-position rejection, held/reentrant/failed confirmations, keyboard/
  mouse inventory bindings, pause-key cancellation, lifecycle/modal/permission
  cleanup and a single network submission without local spawning. These do not
  simulate Unity frame ordering, physics or network delivery.
- Entity ghost regressions link the production appearance collector and renderer:
  multipart/hidden layers, nested pivots and mirrored/nonuniform scale, native
  trader neutral sprites, source immutability/no prefab retention, world origins,
  tiled size/ordering, isolated material copies and alpha, reuse, hiding and cleanup.
  Renderer boundaries are doubles; they do not prove GPU shader output or live poses.

- Positive/negative type-cache behavior, including 1,000 repeated absent lookups.
- Assembly-load invalidation and live item registration.
- Current CUCoreLib and GitLab SexMod method signatures; RshLib icons and `$` IDs.
- Public CUCore item-info/factory binding, nullable condition and current network roles.
- Rejecting internal API types and nonpublic methods.
- Item/liquid translation fallback and native translation precedence, without
  internal LocaleLoader calls or passing missing locale keys to the item registry.
- Together client/host/offline/unknown permission and give-routing matrix.
- Acquisition annotations from Chinese and English descriptions, including the
  screenshot format, split/nested color tags, combined acquisition/crafting
  labels, next-line and multi-line values, line breaks, duplicate lines, and long text. Ordinary source mentions,
  crafting/effect lines, and missing descriptions must not become acquisition data.
- Chinese/English acquisition headings and separate source/usage empty states.
- Description supplements remove only complete source names covered by this
  entry's cards, including the verified medical-crate alias and actual liquid
  container chains. Unknown sources, qualified names, quoted/bracketed slashes,
  malformed lists and following-line conditions keep their context. Source
  additions/removals and UI/game-language combinations update the remainder;
  source guards check cached tab visibility, content and selection fallback.
- Native source weights, repeated independent/guaranteed drops, condition ranges,
  indestructible entities, separate nested variants, actual trader-pool/value
  eligibility, special weapon stock, typed filled-container sources, unchanged
  cache reuse, registry/source invalidation and recoverable snapshot failures.
- Compact acquisition cards retain independent probabilities, random-pool
  at-least-once chances, condition ranges and default liquid volumes. Container
  chains use typed routes and stable identities across UI/game-language changes;
  source sprites reuse the existing scan without another resource read.
- Bounded Unity format-22 metadata reads, canonical GameObject references,
  ignored embedded presets, missing external files, path bounds and malformed
  or unsupported files. An optional second argument reads current game metadata:
  `dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD" '<full game>/CasualtiesUnknown_Data'`.
- The production acquisition coordinator runs against controllable API-boundary
  doubles: one request per frame across recovery hooks, no premature asset reads,
  source children, late loot-pool readiness, same-count pool/item/liquid updates,
  transient failure recovery and no prefab rereads. These doubles do not simulate
  Unity deserialization, prefab lifetime or actual loading/frame performance.
- Hover movement and navigation, search focus, latched movement/jump cleanup,
  mouse capture through release and immediate re-click, and drag cancellation.
- Cached catalog queries, existing sort/filter behavior, same-count favorite
  replacements, catalog/locale revisions, and replacement/empty catalogs.
- Reused cells dispatch clicks to the current entry and refresh an already-hovered
  tooltip; empty/loading cells do not retain actions or hover state.
- Source guards for runtime recovery hooks, visibility before deduplication,
  destroyed UI handling, cheat-click detail behavior, and `x.x.x` version parity.

`Verify-ApiContracts.ps1` additionally reads actual installed DLL metadata using
Mono.Cecil (from the local BepInEx installation), without loading the game:

```powershell
./tests/Verify-ApiContracts.ps1 `
  -CecilPath '<BepInEx core>/Mono.Cecil.dll' `
  -CuCoreLibPath '<Together package>/CUCoreLib.dll' `
  -TogetherPath '<Together package>/Together/CasualtiesMP.dll' `
  -PluginPath '<build output>/UEI.build.dll' `
  -GameAssemblyPath '<full game>/CasualtiesUnknown_Data/Managed/Assembly-CSharp.dll'
```

These checks establish reflection binding and policy behavior, plus structural
invariants. They do not simulate Unity object lifetime, Harmony hook execution,
UI layout, server acknowledgement, or multiplayer item synchronization. Those
need the game and a host/client session; use the checklist in
`docs/pr-3-4-integration.md`.

The optional `GameAssemblyPath` also checks the compiled input patches against
the current full game's private method signatures. A separate local transpiler
checker and the manual input/performance checklist are documented in
`docs/input-performance-fix.md`. The allocation sample in the regression output
measures the isolated helper on .NET; it is not a Unity frame-time measurement.

The current full-game API check also verifies native acquisition fields,
`Locale.GetBuilding`, `Item.GetItem`, Unity asynchronous/child-component APIs,
and the compiled trader selection arrays against literals in the shipped trader
methods. Native acquisition validation and manual tests are recorded in
`docs/acquisition-native-fix.md`.

Placement contracts additionally check the new world-action/block-ghost patch
targets, native scene/camera/pause/minigame/UI fields, and direct selected-position
forwarding through both dispatch routes. Description rendering checks use actual
TMP font/sprite/control-character properties and verify rich-text setup before
height measurement, with only the entity-description card opting in. Actual
glyphs/colors/scrolling and cursor alignment still require the game checklist
in `docs/entity-spawning.md`.

The local give path now verifies that the newly spawned object is actually in
the player's inventory after native pickup. An item left in the world gets a
separate nearby-spawn message; factory null returns are logged as failures.
This Unity delivery behavior still needs in-game verification.

The temporary in-game test plugin is intentionally outside this repository and
all UEI release packages. Its save-isolation path checks are separate from these
Unity-free regressions. Neither suite establishes actual network delivery or
runtime save-hook behavior.
