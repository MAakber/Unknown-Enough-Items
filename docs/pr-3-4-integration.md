# PR #3 / #4 integration review — 1.2.4

## Baseline and attribution

- Date: 2026-11-21.
- Integration baseline: `origin/main` at
  `6f1bafdfa7c00cc8723a1915e6c63958c2ff241b`, including merged PR #2 (RshLib).
- PR #3 reviewed head: `5c08864ead23940152f824818abe7afeb457e1d2`.
- PR #4 reviewed head: `fbee9aa6f5d406b10bfc59ef527d6850fd8cc4e4`.
- PR #3 is an ancestor of PR #4; its changes do not need applying twice.
- The CUCoreLib/SexMod support and UI/catalog optimization proposals came from
  contributor `dfgger433` in PR #3/#4. This integration retains those features,
  with the corrections below, alongside previously developed local fixes.
- Both PRs remain open. This work does not publish comments, push commits, merge,
  or close either PR. This file records the local integration, not a release.

## Corrections applied

1. **RshLib retained.** Synced main before integration; icons, custom-item
   detection, `$` ID handling, recipe resolution and mod-aware `Utils.Create`
   remain available. Registry discovery caches missing types but reads the live
   registry field, so a late initialization or replacement is not lost.
2. **SexMod retained.** The earlier rejection based on its obsolete GitHub source
   was incorrect. The current GitLab repository is
   `https://gitlab.com/foxythethird/ScavPrototypeSexMod`, inspected at
   `be6f4b363a073081348dd155300c3dd4e74f5e59`. Its
   `ScavPrototypeSexMod.Registers.ItemRegistry` supplies `IsRegistered(string)`,
   `GetIcon(string)` and `CreateInstance(string, Vector2, float)`. Bind these exact
   signatures; retain the mod-aware factory and fallback, without `GetTypes()`.
3. **Optional discovery caches both hits and misses.** A shared exact-name cache
   refreshes on assembly load. CUCoreLib and SexMod method bindings share this
   invalidation, without rescanning every assembly/type for every vanilla item.
4. **Translation lookup uses public APIs.** Read the game's Locale APIs (including
   CUCore's overlays) first, then registered public ItemInfo data. Do not reflect
   into the internal `LocaleLoader` type. Never call
   `LocaleRegistry.Get`: a missing key can call `Require` and alter required
   network locale data even with a null fallback. CUCore merges liquid overlays
   into the game's other dictionary. Check registration before ItemInfo lookup
   so missing locale keys do not trigger warnings or item injection.
5. **Together permissions use the server-admin grant on clients.**
   `CanCheat()`/`AllowClientCheatCommands` describe local unsynchronized cheats;
   they are not admin-command authorization. Do not require the client's stale
   replica of `sv_cheats` for enabling the UI. The server still validates its
   authoritative rules when it receives a command. Hosts require `sv_cheats`;
   an unreadable network state denies access. Legacy KrokMP checks are retained.
6. **Item routing distinguishes resources from custom factories.** Together
   clients submit vanilla `give @m <id>` through the authenticated native admin
   command API. A registered mod item instead reports that the host must spawn
   it; it is neither instantiated locally nor sent to native `give`, which only
   loads Resources. Hosts/offline players use mod-aware `Utils.Create` for custom
   items; CUCore-owned items instead use its public `CustomInstantiate.InstantiateReturn`
   first. Retain the SexMod factory fallback. A custom override of a vanilla ID is
   also blocked on clients to avoid issuing the wrong item.
7. **Dispatch is not acknowledgement.** The void Together API reports a request,
   not completed item delivery or healing, including on hosts. Only successful
   local item factory paths use the completed item status. No new network protocol
   or authority bypass was introduced.
8. **Recovery hooks retained.** Keep `GlobalDark.Update`, PlayerCamera hooks and
   the standalone probe/runtime. The old logs show that game menu initialization
   destroys UEI runtime objects. Visibility is evaluated at each hook, with only
   duplicate visible catalog/grid work skipped within the frame. Remove costly
   per-frame diagnostic snapshots; retain the explicit debug text function.
   Skip/recreate destroyed UI objects instead of dereferencing them on transitions.
9. **UI behavior retained/fixed.** Cheat left-click gives or reports its request
   status without opening recipe detail. Toggling cheat mode clears prior detail;
   right-click recipe usage, settings and one-click revive remain. Loading views
   are initialized once, inactive hides avoid redundant animation updates, and
   pointer corner buffers/front ordering avoid repeated allocations/mutations.
10. **Catalog refresh.** Check recipe discovery flags at most once per second
    while counts/INT/language stay unchanged; count both flags in one pass.
    Rebuild immediately when the game language or structural counts change.
11. **Version 1.2.4.** Use a fresh matching `x.x.x` plugin/project version. The
    installed 1.2.3 baseline remains distinct from these public-API changes;
    earlier local code and PR #4 also used 1.2.2 for different contents.
12. **CUCore first.** Prefer its public item registry/icon/factory APIs, and use
    `MultiplayerApi` for active network roles with native compatibility fallback.
    Missing/uninitialized bridges are not treated as proof of offline mode.
    Contradictory role flags deny access. See `cucore-public-api-test-plan.md`.

## Findings that predate these PRs

The vanilla console eligibility check, the old local item-spawning path, and
left-click opening item detail were already in main before PR #3/#4. They must
not be attributed to these contributions in eventual review feedback. Together
adds an authority/synchronization constraint that the integrated paths now handle.

## Validation and remaining runtime checks

Completed locally:

- Release build against the full game's managed assemblies and BepInEx 5.4.2305:
  0 warnings, 0 errors.
- Linked production interop/i18n/policy regression harness, including negative
  cache repetition, late assembly/registry initialization, ambiguous overloads,
  read-only locale misses, and host/client routing. Source guards cover recovery,
  UI lifetime checks, click behavior and version parity. See `tests/README.md`.
- Actual Gale CUCoreLib/Together API signatures checked against installed DLL
  metadata; plugin BepInEx metadata and optional-reference boundaries checked.
- `git diff --check`.

The previous 1.2.3 game log confirms plugin loading, runtime destruction/recovery
through GlobalDark, and catalog display. It injected zero CUCore custom items,
so it cannot establish custom-item delivery. The user's baseline feedback was
positive; the following new-build checks still require their scoped live session:

- New-build main-menu → game → main-menu recovery and UI recreation in Unity.
- New-build host/client vanilla item delivery and revive synchronization.
- Client custom-item warning with no request or client-local spawn; host custom
  creation with each supported mod installed on both peers.
- Late locale switching and UI interactions/layout in the running game.

These need an actual session before treating 1.2.4 as a verified public release
or resolving the PRs as fully runtime-tested. In particular, preserving a host
factory path does not prove that every optional mod synchronizes its item types.
