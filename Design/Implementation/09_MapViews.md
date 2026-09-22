# Phase 9 — Map Views (S42–S45)

Design references: section 1 (zoom, Fit/Fill, outside map color), section 10 "Map rendering in uGUI", section 13 "Minimap view", "Cross-cutting" (update order, time), "Default values → Map views".

All view MonoBehaviours use `[DefaultExecutionOrder(100)]` (after the Manager) and do their work in `LateUpdate` → `UpdateMapViewVisuals(Time.unscaledDeltaTime)`. UI motion uses **unscaled** time.

---

## S42 — Map View core

**Goal:** the generic view: container in map meters, image, outside color, zoom, conversions, channels.

**Depends on:** S38, S05.

**Files:**

1. `Runtime/UI/MapView.cs` (MonoBehaviour, public, `[RequireComponent(typeof(RectTransform))]`)
   - Serialized: `manager` (auto-find once if empty), `viewport` (RectTransform — the masked area; default = own RectTransform), `channelMask` (int bitmask, default Minimap + Full map bits as set per prefab), `zoomMeters` (meters visible across the viewport **width**, default 300), `minZoomMeters` (max zoom-in, default 50). (The `routeStyle` field is added in S43, when `RouteStyle` exists.)
   - Builds its hierarchy once (on first enable): `Background` (Image, outside map color, fills viewport, behind, **`raycastTarget = true`**: it receives the pointer events for the input adapter and the minimap tap; every other image in the view has `raycastTarget = false`), `Content` (RectTransform, pivot bottom-left, the **container**), `Content/MapImage` (RawImage, size = rectangle size in meters, anchored bottom-left at (0,0)), `Content/Route` (RouteLineRenderer, S43), `Markers` layer (sibling after Content, S47).
   - **Container transform:** local scale = `canvasUnitsPerMeter = viewportWidth / zoomMeters`; rotation around the view **center point** (a map position); position so that the center point lands at the viewport's pivot point (the Follow Car behavior sets a different pivot for the car offset).
   - State: `CenterMap` (Vector2 map meters), `RotationDegrees` (map rotation, 0 = map up), `ZoomMeters`. Setters: `SetCenter`, `SetRotation`, `SetZoomMeters` (clamped between `minZoomMeters` and the max zoom-out passed by the behavior).
   - Conversions (public): `ScreenToWorld(Vector2 screenPoint)` → World (via `RectTransformUtility.ScreenPointToLocalPointInRectangle` with the canvas event camera, then container local = Map meters → True → World), `WorldToScreen(Vector3 world)`; internal `MapToViewport(Vector2 map)` / `ViewportToMap(Vector2 local)`.
   - On `MapChanged`: assign the new image (`RawImage.texture`), size, outside map color; clear route display.
   - Property `CanvasUnitsPerMeter`.
2. `Runtime/UI/MapViewMath.cs` (plain, internal) — the pure math used above: `ComputeContainerPose(Vector2 centerMap, float rotation, float unitsPerMeter, Vector2 pivotInViewport)` → position/rotation/scale; `ViewportToMap`, `MapToViewport`. All conversions go through this class (tested).

**Tests:**
- `EditMode/MapViewMathTests.cs`: `MapToViewport_Center_IsPivot`, `ViewportToMap_IsInverse_Rotated37`, `Scale_300mAcross600UnitsViewport_Is2`.
- `PlayMode/MapViewTests.cs` (canvas + view + Manager with a test map): `Background_IsRaycastTarget_OthersNot`, `MapChanged_ImageAssigned_SizeMatchesRectangle`, `SetZoomMeters_ClampsToMin`, `ScreenToWorld_WorldToScreen_RoundTrip`, `Background_UsesOutsideMapColor`.

---

## S43 — Route display in views

**Goal:** active route, preview route, trimming, faded mode, dotted piece and styles (design section 10).

**Depends on:** S42.

**Files:**

1. `Runtime/UI/RouteStyle.cs` (`ScriptableObject`, implements `IFormatVersioned`): for **active**, **preview** and **faded**: line color, outline color; `halfWidth` (canvas units: minimap default 3 = 6 wide, full map 4 = 8 wide), `outlineWidth` (default 1.5), `dashLength` (6), `gapLength` (4), `drivenMode` (enum `Removed`/`Faded`, default Removed), `lineShader` (Shader: the `RouteLine` shader; this reference is what keeps the shader in builds; assigned by the S53 builder). Defaults from the design: active blue / dark blue; preview light blue semi-transparent / gray; faded gray 50%, no outline. Two assets ship later (S53): `MinimapRouteStyle`, `FullMapRouteStyle`.
2. `Runtime/UI/RouteLineData.cs` (plain, internal): converts a `Route` into the three lists for the renderer: map-space points (`Route.GetTruePoints` → `MapFrame.TrueToMap`), cumulative distances (same distances as the route's progress), dashed flags; **appends the dotted piece** from the route end to `Route.Destination` when the destination is farther than 1 m from the road point. Reused lists.
3. `MapView` (change):
   - Add the serialized `routeStyle` (RouteStyle) field.
   - Two `RouteLineRenderer`s under Content: **Active** and **Preview** (preview only shown when the view shows previews — a serialized `showPreview` bool: full map true, minimap false).
   - Rebuild a renderer's line only when its route changes (`NavigationStarted`, `Rerouted`, `PreviewReady`, cleared on stop/arrive/cancel/fail).
   - Every frame (only properties, no rebuild): `SetCanvasUnitsPerMeter(CanvasUnitsPerMeter)` and, for the active line, `SetTrimDistance(manager.TrimDistance)`.
   - Style from `routeStyle` (`Removed` → trim mode 0; `Faded` → trim mode 1 with the faded color); `SetShader(routeStyle.lineShader)` on both renderers.

**Tests:** `EditMode/RouteLineDataTests.cs`: `Convert_AppendsDottedPiece_WhenDestinationOffRoad`, `Convert_NoDottedPiece_WhenOnRoad`, `Convert_DistancesMatchRouteLength`. `PlayMode/MapViewRouteTests.cs`: `NavigationStarted_ActiveLineBuilt`, `Driving_TrimUpdates_NoMeshRebuild` (check `MeshBuildCount` stays 1), `Preview_ShownOnlyWhenShowPreview`, `Stop_ClearsLine`.

**Manual checks** (sandbox with a drawn and baked road network, a temporary full-screen MapView on a canvas, start navigation with a small dev script or the Test Runner): the route follows the roads, shrinks while driving, the dotted piece appears for an off-road destination.

---

## S44 — Follow Car behavior (minimap)

**Goal:** heading-up/north-up, car offset, speed zoom, edge clamp, marker slide, outside-map pin (design section 13).

**Depends on:** S43.

**Files:**

1. `Runtime/UI/MinimapMath.cs` (plain, internal) — pure math, fully tested:
   - `SpeedZoom(float speed, float minMeters, float maxMeters, float slowSpeed, float fastSpeed)` → meters across (linear between 20 km/h→150 m and 100 km/h→500 m by default).
   - `MaxZoomToFitMap(Vector2 mapSize, bool round, Vector2 viewportSize)` → the largest meters-across that keeps the view inside the map (round: viewport diameter ≤ smaller map side; rectangle: its diagonal ≤ smaller map side).
   - `ClampCenter(Vector2 desiredCenter, Vector2 mapSize, float viewHalfExtentMeters, bool round, float rotation, Vector2 viewHalfSizeMeters)` → clamped center. Round: keep a circle of radius `viewHalfExtent` inside the map. Rectangle: keep the **rotated** rectangle's corners inside the map (compute the rotated rectangle's axis-aligned half extents).
   - `SmoothAngle(float current, float target, float smoothTime, ref float velocity, float deltaTime)` (wraps −180..180; `Mathf.SmoothDampAngle`).
2. `Runtime/UI/MapViewFollowCar.cs` (MonoBehaviour, requires `MapView`)
   - Serialized (design defaults): `rotationMode` (`HeadingUp`/`NorthUp`), `carOffsetFromBottom` (0.3), `rotationSmoothing` (0.25 s), `speedZoom` (bool true), speed zoom values, `round` (bool, true for the round minimap).
   - Each frame (unscaled time for smoothing):
     - Target rotation: heading-up → the **matched road's direction** (`NavigationManager.RoadHeading`, oriented toward the nose) while `HasRoadHeading`; otherwise the car's **nose** map angle with a dead zone (`noseDeadZoneDegrees`, default 3°). Never the movement direction. North-up → 0. Smoothly damped: `rotationSmoothing` (0.25 s) normally, `turnSmoothing` (0.8 s) from a target jump > `turnAngleThreshold` (20°, e.g. a road change) until within 2° of the target. (Changed 2026-09-22, user request: see Decisions.)
     - Zoom: speed zoom (using the Manager's speed) smoothed, capped by `MaxZoomToFitMap`.
     - Desired center: the car's map position shifted so the car sits at 30% from the bottom (heading-up); centered in north-up.
     - Clamp the center (`ClampCenter`). The player marker is **not** clamped: it's drawn at its true map position, so it slides away from its spot at the edges automatically.
     - **Car outside the map:** the player marker is pinned to the view edge (S47 draws markers; this behavior sets a flag the marker layer uses).
   - `SetRotationMode`, `ToggleRotationMode` (public).

**Tests:** `EditMode/MinimapMathTests.cs`: `SpeedZoom_Slow_150`, `SpeedZoom_Fast_500`, `SpeedZoom_Between_Linear`, `MaxZoomToFitMap_RoundUsesDiameter`, `ClampCenter_Round_InsideMap_Unchanged`, `ClampCenter_Round_NearEdge_Clamped`, `ClampCenter_Rect_Rotated45_UsesDiagonalExtents`, `SmoothAngle_WrapsAround180`. `PlayMode/MapViewFollowCarTests.cs`: `HeadingUp_Reversing_RotationUnchanged`, `HeadingUp_WeavingOnRoad_RotationFollowsRoad`, `HeadingUp_OffRoad_FollowsNose`, `HeadingUp_OffRoad_SmallNoseChange_IgnoredByDeadZone`, `HeadingUp_LargeHeadingChange_UsesTurnSmoothing`, `NorthUp_RotationZero`, `NearMapEdge_CenterClamped`.

**Manual checks:** drive to the city edge: the minimap stops moving and the car marker slides toward the edge; reversing doesn't spin the map.

---

## S45 — Minimap shape, compass, tap to open the full map

**Goal:** round/rect masks, compass button, minimap tap (design sections 12–13).

**Depends on:** S44.

**Files:**

1. `Runtime/UI/MinimapShape.cs` (MonoBehaviour on the minimap's viewport): enum `Rectangle` / `Sprite`; applies a `RectMask2D` (rectangle) or `Mask` + `Image` with the given sprite (round sprite by default). Switching removes the other component. Editor-time via `OnValidate` too.
2. `Runtime/UI/CompassButton.cs` (MonoBehaviour + `Button`): rotates its icon to point at map north (uses the view's rotation); click → `MapViewFollowCar.ToggleRotationMode()`.
3. `Runtime/UI/MinimapTapToOpen.cs` (MonoBehaviour, `IPointerClickHandler` on the minimap viewport): setting `OpenFullMap` / `Nothing`; serialized reference to the full map `MapViewInteractive` (S49) — until S49 exists, reference a `GameObject` and call `SetActive(true)`; **S49 replaces this with `Open()`**.

**Tests:** `PlayMode/MinimapShapeTests.cs`: `Rectangle_AddsRectMask2D_RemovesMask`, `Sprite_AddsMask_RemovesRectMask2D`. `PlayMode/CompassButtonTests.cs`: `Click_TogglesRotationMode`.
