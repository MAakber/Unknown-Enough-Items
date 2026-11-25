# Entities and liquid delivery

The Entities category uses the existing UEI list, search, favorites and clicks.
Entities also appear in All. There are no new buttons or configuration options;
cheat mode remains the existing opt-in switch. Version stays at 1.2.10.

The native acquisition scanner passes each canonical root prefab to
`UeiEntities` before filtering source drops. A root `BuildingEntity` without an
`Item` becomes an entity descriptor, including merchants with no item drops and
indestructible entities. Nested components are acquisition contexts rather than
independently spawnable entries. Particle effects, UI prefabs and other resources
without those gameplay components are excluded. Descriptors retain resource
paths, locale IDs, copied text and sprites, never prefab references.

Entity keys use `entity:<resource path>`. This keeps merchants with the same
building locale ID separate (`trader1`, `trader2`, `trader3`) and prevents ID
collisions with items/liquids, recipe matches or favorites. Names and descriptions
use `Locale.GetBuilding`; discovery updates the catalog in bounded batches without
rebuilding existing item recipe links or reading prefabs again. Outside cheat
mode, entity details show their description.

Cheat clicks dispatch by entry type:

| Entry | Single player / offline | Together host or authorized client |
| --- | --- | --- |
| Item | Existing factory and pickup behavior | Existing give/custom-item policy |
| Entity | Registered native `spawn` action | Native `spawn` via authenticated admin command |
| Liquid | Fill and give native temporary bottle | UEI liquid command executes on server for its caller |

Cheat entity selection enters `UeiEntityPlacement` and closes the native radial
inventory through its existing `radialOpen` flag. Reopening inventory preserves
the category, search, page and selected entry. The selecting gesture must finish;
only a subsequent left-button press confirms one spawn at
`Camera.main.ScreenToWorldPoint(Input.mousePosition)`, using its x/y coordinates.
The preview is a translucent world-space copy of visible sprite layers, including
root scale, nested local poses, flipping, tiled/sliced size, colors/materials and
sorting groups. The existing prefab scan stores only sprite/material assets and
copied transform data, without retaining GameObject/Transform/renderer references.
Trader neutral head/eye sprites reuse the verified native arrays. The preview
creates only Transform/SpriteRenderer/SortingGroup components; no gameplay prefab,
collider, AI, trade inventory or network entity is instantiated. It is a static
appearance rather than live animation/physics. Missing or failed visuals show a
text hint; placement still works. There is no crosshair or floating-icon fallback.
Graphic UI raycasts, out-of-window points and non-finite coordinates cannot
confirm placement; physics raycasts do not block it. There is no additional
snapping or fixed player offset.

Right-click or Escape/the configured pause key cancels, consuming cancellation
through release. The inventory binding cancels and opens inventory; mouse-bound
inventory navigation uses the same native flag while keeping that click captured.
Left/right mouse buttons retain their placement/cancellation roles. World pointer
input is blocked through confirmation/cancellation release; keyboard movement
remains available. Native attacks, dragging, inventory/world actions and the
held-item block ghost cannot act on placement clicks. The controller runs before
inventory visibility checks, with one confirmation sample per frame and a
reentrant-dispatch guard. Focus loss, death, scene/body/camera/canvas changes,
pause, minigames, console/crafting/trade/medical UI and changed cheat settings,
permissions or network session cancel pending placement. Confirmation/cancellation
hides the ghost; renderer/material objects are reused for the same appearance and
disposed on replacement/runtime teardown. The ghost has its own foreground sorting
group and cannot intercept pointer/physics interactions. Invalid world positions
hide the ghost, while UI-obstructed valid positions dim it.

Local failures remain pending until another independent click. Network submission
ends one-shot placement and reports a request without automatic retries or local
instantiation. Both routes use the selected position with an explicit invariant
coordinate argument and amount one. Native spawn handles entity creation and
`BuildingEntity.blockPlacedOn`; it also retains installed command wrappers. A
`TraderScript.Start` prefix supplies an activity range only when no range was
provided, before Together publishes the new merchant. Natural generation/mod
ranges are preserved. Client instances do not initialize authoritative ranges.

Entity descriptions pass the original string directly to TextMeshPro with
rich-text rendering enabled. Only the entity-description card opts in;
acquisition notes retain plain-text behavior. There is no semantic parser, tag
removal, fixed section layout, delimiter cleanup or number/probability rewriting.
Font, shared font material, sprite asset and control-character mode come from the
current native tooltip description where available. Rendering is configured
before `GetPreferredValues` measures the text. The card uses the remaining
detail-panel height and scrolls longer contents. Tooltips still receive the
unchanged original description.

UI entries without a usable icon use a single cached, point-filtered black/magenta
checkerboard. The actual catalog icon remains null, so a UI placeholder never
becomes a world-ghost sprite or changes acquisition data. Empty/loading grid
cells stay blank; unresolved source nodes without a catalog entry retain their
text. Entity icon selection can reuse a visible sprite captured after an empty
first renderer, and trader neutral head sprites use their native arrays.

The inspected native `grabberplant` resource has one LineRenderer and no
SpriteRenderer. `GrabberPlant` drives `IKHandle`, whose rendering updates line
positions. Its gray block in the old UI was therefore a missing static-icon
placeholder, not the plant's appearance or proof that the prefab failed to load.

Liquids use the game's existing `craftingbottle` and
`WaterContainerItem.AddLiquid`, filled to its capacity (1000 mL in the inspected
game). Native default contents are cleared first. The bottle expires after 300
seconds, matching the native recipe path. Inventory delivery is checked; a full
inventory or pickup exception leaves a filled bottle nearby with a different
status. Invalid resources/capacities are cleaned up.

Together liquid requests use `uei_liquid <liquid id>` through the same existing
authenticated command transport as give. The registered server handler checks
host cheat permission, validates the liquid, resolves the public
`Together.Con.GetCommandCallerPlayer().body`, and uses the public
`CasualtiesTogetherUtils.Util_BodyExtensions.ForceAutoPickUpItem` helper. It never
falls back to the host camera for an unknown caller. The host needs this UEI
build too. The void transport reports request submission, not confirmed delivery.

Validation links the real catalog, models and spawn helpers to API doubles, plus
checks against the installed game/CUCore/Together metadata. These checks cover
catalog identity/localization, recipe separation, incremental discovery, native
command arguments, selecting/confirming/releasing gestures, camera projection,
cancellation/lifecycle cleanup, one-shot/reentrant dispatch, liquid contents/
lifetime, failed pickup, permissions, caller targeting and merchant ranges.
Metadata checks new input hooks, selected-position forwarding and native
rich-text/font/sprite/rendered-height contracts. They do not establish actual Unity rendering,
trading, timed cleanup or host/client synchronization.

In-game acceptance:

1. Find all three merchant variants under Entities and All. Check search,
   favorites, Chinese/English text and existing item/liquid recipe browsing.
2. With cheats off, check original description colors/sizes/sprites/newlines,
   long or unusual mod descriptions, and scrolling. With cheats on, select a
   merchant and confirm selection creates none. Move/zoom, then left-click a
   chosen world point: check that the ghost matches the entity size, origin,
   layers and flipping, and verify one spawn there, no attack/drop/ordinary block
   placement, and no extra spawns while holding. Check right/Escape/inventory
   cancellation, keyboard/mouse inventory bindings, focus/menu/death/disconnect
   cleanup, preserved list state, and trade/movement near the chosen point.
3. Receive liquids in a temporary bottle, check its volume and identity, drink or
   transfer it, and verify expiration after five minutes. Repeat with no inventory
   room and verify the bottle remains nearby.
4. With both sides updated, test host and authorized client generation, ownership,
   trading, bottle contents, cleanup and reconnect. With `sv_cheats` disabled or
   client admin permission absent, verify no objects/inventory changes occur.

The initial entity/liquid baseline is under `artifacts/entity-spawn-2026-11-23/`.
Placement/rendering baselines, prepared build, hashes, recovery patch and
installation status are under `artifacts/entity-placement-richtext-2026-11-23/`.
The subsequent entity-shaped ghost preview is recorded under
`artifacts/entity-ghost-2026-11-23/`.
Do not stop a running game or overwrite a plugin that no longer matches the
corresponding task's baseline.
