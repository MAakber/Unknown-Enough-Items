# UEI integration regressions

Run from the repository root with .NET SDK 10:

```powershell
dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD"
```

The executable links the production `UeiModInterop.cs`, `UeiCheatPolicy.cs`, and
`UeiI18n.cs` files. It deliberately loads a separate fixture assembly only after
testing absent mods. Fixtures include ambiguous overloads, a registry initialized
after assembly load, registry replacement, and a mutating locale API that must
never be called. No test packages or game binaries are bundled.

Coverage:

- Positive/negative type-cache behavior, including 1,000 repeated absent lookups.
- Assembly-load invalidation and live item registration.
- Current CUCoreLib and GitLab SexMod method signatures; RshLib icons and `$` IDs.
- Public CUCore item-info/factory binding, nullable condition and current network roles.
- Rejecting internal API types and nonpublic methods.
- Item/liquid translation fallback and native translation precedence, without
  internal LocaleLoader calls or passing missing locale keys to the item registry.
- Together client/host/offline/unknown permission and give-routing matrix.
- Source guards for runtime recovery hooks, visibility before deduplication,
  destroyed UI handling, cheat-click detail behavior, and `x.x.x` version parity.

`Verify-ApiContracts.ps1` additionally reads actual installed DLL metadata using
Mono.Cecil (from the local BepInEx installation), without loading the game:

```powershell
./tests/Verify-ApiContracts.ps1 `
  -CecilPath '<BepInEx core>/Mono.Cecil.dll' `
  -CuCoreLibPath '<Together package>/CUCoreLib.dll' `
  -TogetherPath '<Together package>/Together/CasualtiesMP.dll' `
  -PluginPath '<build output>/UEI.build.dll'
```

These checks establish reflection binding and policy behavior, plus structural
invariants. They do not simulate Unity object lifetime, Harmony hook execution,
UI layout, server acknowledgement, or multiplayer item synchronization. Those
need the game and a host/client session; use the checklist in
`docs/pr-3-4-integration.md`.

The temporary in-game test plugin is intentionally outside this repository and
all UEI release packages. Its save-isolation path checks are separate from these
Unity-free regressions. Neither suite establishes actual network delivery or
runtime save-hook behavior.
