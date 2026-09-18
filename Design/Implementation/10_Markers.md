# Phase 10 — Markers (S46–S48)

Design references: section 11 "Markers and icons", section 13 (off-screen arrows, car outside the map), "Default values → Map views".

---

## S46 — Marker component, registry and grid

**Goal:** object markers, point markers, the Manager's marker list and the 100 m marker grid.

**Depends on:** S38.

**Files:**

1. `Runtime/Markers/MarkerRotationMode.cs` — enum `Upright`, `FollowHeading`, `FollowMap`.
2. `Runtime/Markers/MapMarker.cs` (MonoBehaviour, public — replaces the empty class from S38)
   - Serialized: `manager` (optional), `prefab` (GameObject UI prefab; null = the marker layer's default), `rotationMode` (Upright), `channelMask` (default: Minimap + Full map bits), `isStatic` (false), `canBeDestination` (false), `showOffScreenArrow` (false).
   - `OnEnable`: find the Manager (reference, else one cached `FindAnyObjectByType`) → `manager.AddMarker(this)`; if no Manager exists yet, do nothing. `OnDisable`: `RemoveMarker(this)`.
   - **Enable-order safety:** in `Initialize()` the Manager also adds every enabled `MapMarker` already in the loaded scenes (`FindObjectsByType<MapMarker>(FindObjectsSortMode.None)`); `AddMarker` ignores duplicates.
3. `Runtime/Markers/MarkerEntry.cs` (internal class, reused): kind (`Object`/`Point`), `Marker` (MapMarker or null), `Transform`, `TruePosition`, `TrueHeading`, `Prefab`, `RotationMode`, `ChannelMask`, `IsStatic`, `CanBeDestination`, `ShowArrow`, `IsPlayer`, `Alive`, `GridCell`.
4. `Runtime/Markers/MarkerGrid.cs` (plain, internal): 100 m cells over True X/Z (unbounded: cell key = `(int x, int z)` packed into a `long`; a `Dictionary<long, List<int>>` whose lists are reused and never freed). `Add`, `Remove`, `Move(entryIndex, oldCell, newCell)`, `QueryArea(Vector2 minXZ, Vector2 maxXZ, List<int> output)`.
5. `Runtime/Markers/MarkerRegistry.cs` (plain, internal): entries in a list with a free-index stack (indices stay stable; removed entries are marked not alive and reused later). `AddObject(MapMarker)`, `RemoveObject(MapMarker)`, `AddPoint(Vector3 truePos, GameObject prefab, int channelMask, bool showArrow)` → index, `RemovePoint(int index)`, `SetPointPosition(int index, Vector3 truePos)`, `UpdateMarkerRegistryLogic(WorldConverter converter)` (reads every alive **non-static** object marker's Transform: position → True, forward flattened → heading; moves grid cells when changed; static markers are read **once** when added; the **player** entry is skipped here), `SetPlayer(Vector3 truePos, Vector3 trueHeading)` (the Manager writes the player's position and **nose heading with the yaw offset** each frame), `QueryVisible(Vector2 minXZ, Vector2 maxXZ, int channelMask, List<int> output)` (grid query + channel filter + always includes alive entries with `ShowArrow`), `GetEntry(int index)`.
6. `NavigationManager` (change):
   - `AddMarker(MapMarker)` / `RemoveMarker(MapMarker)` (public, follow the command-queue rule).
   - Prefab slots (serialized): `playerMarkerPrefab`, `destinationMarkerPrefab`, `previewPinPrefab`.
   - Internal markers: the **player** (object entry on the car Transform, `IsPlayer = true`, heading = the nose with yaw offset, `FollowHeading`, both channels; retargeted by `SetCar`); the **destination** (point marker at `Route.Destination`, both channels, `ShowArrow = true`; added on `NavigationStarted`, moved on reroute, removed on stop/arrival/fail-stop); the **preview pin** (point marker, **Full map channel only**; added on `PreviewReady`, removed on cancel/confirm/fail).
   - `LateUpdate` order gets `UpdateMarkerRegistryLogic` after tracking.
   - Internal accessor `Markers` (the registry) for the views.

**Tests:** `EditMode/MarkerGridTests.cs`, `EditMode/MarkerRegistryTests.cs`, `PlayMode/MapMarkerTests.cs`
- `QueryArea_ReturnsOnlyInside`, `Move_ChangesCell`
- `AddRemove_ReusesIndex`, `StaticMarker_ReadOnce_NotUpdated`, `MovingMarker_GridCellUpdated`
- `QueryVisible_FiltersChannels`, `QueryVisible_IncludesArrowMarkersOutsideArea`
- `MapMarker_EnableDisable_RegistersAndUnregisters`
- `MarkerCreatedBeforeManager_AddedOnInitialize`
- `NavigationStarted_DestinationPointAdded`, `Arrived_DestinationRemoved`
- `PreviewPin_OnlyFullMapChannel`
- `SetCar_PlayerMarkerRetargeted`
- `PlayerHeading_UsesNoseWithYawOffset` (yaw offset 90: player heading differs from transform.forward)
- `UpdateMarkerRegistryLogic_500MovingMarkers_NoGarbage` (EditMode, `Is.Not.AllocatingGCMemory()`)

---

## S47 — Marker layer in views

**Goal:** showing markers in every view with culling, pooling, constant size and rotation modes.

**Depends on:** S46, S44.

**Files:**

1. `Runtime/UI/MarkerLayer.cs` (MonoBehaviour, created by `MapView` as a sibling **above** `Content`, inside the viewport/mask, with its **own nested Canvas** (no override sorting) — the S04 lab verified this; if S04 used the fallback, no nested canvas).
   - Serialized: `defaultMarkerPrefab`, `cullMarginFraction` (0.1).
   - Each frame (`UpdateMarkerLayerVisuals`):
     1. Viewport corners → Map → True; X/Z bounding box grown by 10% of the view size → `QueryVisible` with the view's channel mask.
     2. Entries that became invisible → return their UI instance to the pool (`SetActive(false)`).
     3. Visible entries: get/keep a UI instance from the pool (**one pool per prefab**, `Dictionary<GameObject, List<GameObject>>`, never destroyed while enabled), set `anchoredPosition = MapToViewport(map position)`.
     4. Rotation: `Upright` → 0; `FollowHeading` → view rotation minus the heading's map angle; `FollowMap` → the view's map rotation.
     5. **Player marker while the car is outside the map** (flag from `MapViewFollowCar`): clamp its position to the viewport edge with `OffScreenArrowMath.ComputeEdgePoint`, still rotated by its heading.
   - No per-frame allocations after warm-up; the "previously visible" set uses reused lists and a per-entry frame stamp.
2. `MapView` (change): creates the `MarkerLayer`; serialized `edgeShape` (Circle for the minimap prefab, Rectangle for the full map), `edgeInset` (8 canvas units).
3. `Runtime/UI/EdgeShape.cs` — enum `Rectangle`, `Circle`.
4. `Runtime/UI/OffScreenArrowMath.cs` (plain, internal): `ComputeEdgePoint(Vector2 targetInViewport, Vector2 viewportHalfSize, EdgeShape shape, float inset, out Vector2 edgePoint, out float angleDegrees)` → bool (false when the target is inside the area). Circle: radius = `min(halfSize) − inset`. Rectangle: half size − inset, intersection of the ray from the center with the rectangle. Angle: 0 = up, clockwise positive.

**Tests:** `EditMode/OffScreenArrowMathTests.cs`: `Inside_ReturnsFalse`, `Circle_TargetRight_EdgeAtRadiusMinusInset_Angle90`, `Rect_TargetDiagonal_OnRectangleEdge`. `PlayMode/MarkerLayerTests.cs`
- `MarkerInView_Shown`, `MarkerOutOfView_Hidden`
- `MarkerLeavesAndReturns_InstanceReused` (pool: instance count stays 1)
- `ChannelNotInView_NotShown`
- `Upright_NoRotation`, `FollowHeading_RotatesWithHeading`
- `ConstantSize_ZoomChange_SizeUnchanged`
- `PlayerOutsideMap_PinnedToEdge`

**Manual checks:** in the sandbox, place a few `MapMarker`s (one moving: attach to a second dev car or animate it); they show on the minimap, keep their size while zooming, and the player arrow rotates with the car.

---

## S48 — Off-screen arrows

**Goal:** arrows at the view edge for markers with "Show off-screen arrow" (design: destination on by default; any view; edge rectangle/circle + inset; optional distance label). Uses `OffScreenArrowMath` from S47.

**Depends on:** S47, S39.

**Files:**

1. `MapView` (change): serialized `showOffScreenArrows` (true), `arrowPrefab`, `showArrowDistance` (true).
2. `MarkerLayer` (change): for visible-set entries with `ShowArrow` whose position is outside the area → hide the marker, show a pooled arrow at the edge point, rotated to point at the target. If the arrow prefab has a `NavigationTextTarget` and `showArrowDistance` is on: distance = X/Z distance from the car to the marker; format with the Manager's formatter into a reused `StringBuilder` and call `SetText` **only when the value shown changes** (compare with the last value rounded to the formatter's step: 10 m metric).

**Tests:** `PlayMode/OffScreenArrowTests.cs`: `DestinationOutOfView_ArrowShown`, `DestinationInView_NoArrow`, `ShowArrowsOff_NoArrow`, `DistanceLabel_UpdatesOnlyWhenValueChanges`.

**Manual checks:** set a far destination: the minimap shows an arrow on its round edge pointing at it with a distance label; on the full map, pan away from the destination: an arrow appears at the screen edge.
