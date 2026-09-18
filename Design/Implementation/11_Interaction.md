# Phase 11 — Full Map Interaction (S49–S52)

Design references: section 1 (Fit/Fill zoom-out), section 12 "Full map interaction", section 11 (markers as destinations, tap targeting), section 14 (Map View actions), "Default values → Map views".

All gesture timing uses **unscaled** time, passed in as a parameter so the logic is testable.

---

## S49 — Interactive behavior: actions, open/close, following

**Goal:** the full map's actions API and its open/follow rules.

**Depends on:** S47.

**Files:**

1. `Runtime/UI/FullMapMath.cs` (plain, internal)
   - `MaxZoomMeters(Vector2 mapSize, Vector2 viewportSize, bool fit)`: meters across the viewport width at maximum zoom-out. Fit: the whole map is visible → `max(mapWidth, mapHeight * viewportAspect)`. Fill: the map covers the viewport → `min(mapWidth, mapHeight * viewportAspect)`.
   - `ClampCenter(Vector2 center, Vector2 mapSize, Vector2 viewHalfSizeMeters)`: per axis: if the view is larger than the map on that axis → the map's center on that axis; else keep the view inside the map. (The full map never rotates.)
   - `ZoomAroundPivot(Vector2 center, Vector2 pivotMap, float oldZoom, float newZoom)` → new center that keeps `pivotMap` under the same screen point.
2. `Runtime/UI/MapViewInteractive.cs` (MonoBehaviour, requires `MapView`)
   - Serialized (design defaults): `zoomOutMode` (`Fit`/`Fill`, Fit), `openZoomMeters` (1000), `mouseWheelStep` (1.25), `doubleTapStep` (2), `confirmStep` (true), `showPreview` true on the view.
   - Actions (public, the design's Map View actions table): `Pan(Vector2 screenDelta)` (stops following), `Zoom(float factor, Vector2 screenPivot)` (factor > 1 zooms in; while following the pivot is the car), `SetZoomMeters(float)`, `CenterOnCar()` (following on), `TapAt(Vector2 screenPoint)` (S51), `SetCrosshairMode(bool)` / `ConfirmAtCrosshair()` (S52).
   - Rotation is always 0. Center always clamped with `FullMapMath.ClampCenter`; zoom clamped between the view's `minZoomMeters` and `MaxZoomMeters`.
   - **Following:** while on, the center = the car's map position each frame.
   - **Open/close:** `Open()` / `Close()` / `Toggle()` = `gameObject.SetActive`. Events `Opened` / `Closed` (C#). `OnEnable` → center on the car, zoom = `openZoomMeters`, following on, `Opened`. `OnDisable` → `manager.CancelPreview()`, `Closed`.
   - Properties: `IsFollowingCar`, `ZoomMeters`.
3. `Runtime/UI/MinimapTapToOpen.cs` (change): reference a `MapViewInteractive` and call `Open()` (replaces the GameObject reference from S45).

**Tests:** `EditMode/FullMapMathTests.cs`: `MaxZoom_Fit_WideScreenSquareMap_UsesHeight`, `MaxZoom_Fill_WideScreenSquareMap_UsesWidth`, `ClampCenter_ViewLargerThanMap_Centers`, `ZoomAroundPivot_PivotStaysFixed`. `PlayMode/MapViewInteractiveTests.cs`: `Open_CenteredOnCar_OpenZoom`, `Pan_StopsFollowing`, `Zoom_WhileFollowing_KeepsCarCentered`, `CenterOnCar_ResumesFollowing`, `Close_CancelsPreview`, `Pan_CannotLeaveMap`.

---

## S50 — Pointer adapter and gestures

**Goal:** mouse and touch through uGUI events: drag, pinch, wheel, tap, double-tap, fling (design: built-in pointer adapter, fling and double-tap defaults).

**Depends on:** S49.

**Files:**

1. `Runtime/UI/GestureTracker.cs` (plain, internal) — no Unity event types inside, only positions, IDs and times, so it's fully testable.
   - Input methods: `PointerDown(int id, Vector2 pos, float time)`, `PointerMove(int id, Vector2 pos, float time)`, `PointerUp(int id, Vector2 pos, float time, bool wasClick)`, `Scroll(float notches, Vector2 pos)`, `UpdateGestureLogic(float time, float deltaTime)`.
   - Output: it calls methods on an `IMapGestureTarget` interface (`Pan(Vector2 delta)`, `Zoom(float factor, Vector2 pivot)`, `Tap(Vector2 pos)`) implemented by `MapViewInteractive`.
   - Rules:
     - One pointer moving → `Pan(delta)`.
     - Two pointers → pinch: `Zoom(currentDistance / previousDistance, midpoint)` plus `Pan(midpoint delta)`.
     - Scroll → `Zoom(mouseWheelStep ^ notches, pos)`.
     - Tap = pointer up with `wasClick` (uGUI's click: no drag beyond the EventSystem's drag threshold). With double-tap **on**: the first tap waits **250 ms**; a second tap within 250 ms and within 40 canvas units → `Zoom(doubleTapStep, pos)` and no tap; otherwise after 250 ms → `Tap(pos)`. With double-tap **off**: `Tap` immediately.
     - Fling: on release after a one-pointer drag, velocity = average of the last 0.1 s of movement (canvas units/s). If ≥ **500** → keep calling `Pan(velocity * dt)` each update while `velocity *= 0.5 ^ (dt / 0.15)`; stop below 20. Any new pointer down stops the fling.
   - Settings: `doubleTapEnabled` (true), `flingEnabled` (true), wait 0.25 s, fling min speed 500, half-life 0.15 s, double-tap radius 40.
2. `Runtime/UI/PointerInputAdapter.cs` (MonoBehaviour on the full map viewport; implements `IPointerDownHandler`, `IPointerUpHandler`, `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IScrollHandler`, `IPointerClickHandler`): converts `PointerEventData` into `GestureTracker` calls (screen positions converted to viewport-local canvas units), passes `Time.unscaledTime`/`unscaledDeltaTime`, and tells `MapViewInteractive` that **pointer** input was used (for crosshair Auto mode, S52). Can be disabled/removed by users who wire their own input.

**Tests:** `EditMode/GestureTrackerTests.cs` (a fake `IMapGestureTarget` that records calls)
- `SingleDrag_Pans`
- `Pinch_ZoomsAroundMidpoint`
- `Scroll_OneNotch_Zoom1_25`
- `Tap_DoubleTapOn_WaitsThenTaps`
- `DoubleTap_ZoomsIn_NoTap`
- `DoubleTapOff_TapsImmediately`
- `FastRelease_Flings_ThenStops`
- `SlowRelease_NoFling`
- `NewPointerDown_StopsFling`

**Manual checks:** in the sandbox with the full map open: drag, mouse wheel, fling (swipe fast); on a touch device or the Device Simulator: pinch and double-tap.

---

## S51 — Tapping, preview panel and buttons

**Goal:** tap → marker or map point → preview; the default preview UI (design: tap = preview, confirm/cancel/stop/center buttons, skip-confirm setting, tap targeting).

**Depends on:** S50, S48.

**Files:**

1. `NavigationManager` (change): internal overload `PreviewDestination(Vector3 worldPoint, MapMarker marker)` so `PreviewReady` reports the tapped marker; the same overload for `StartNavigation`.
2. `MapViewInteractive.TapAt(Vector2 screenPoint)`:
   - Check markers first: among the marker layer's **currently shown** entries with `CanBeDestination`, the closest within **40 canvas units** (setting) wins → preview at the marker's world position with that marker.
   - Else: `ScreenToWorld` → preview at that point.
   - `confirmStep == false` → `StartNavigation` instead of `PreviewDestination`.
3. `Runtime/UI/PreviewPanel.cs` (MonoBehaviour in the full map prefab): shown on `PreviewReady`: distance and ETA of the preview route via the formatter into two `NavigationTextTarget`s (set once per `PreviewReady`); **Confirm** → `ConfirmPreview()`; **Cancel** → `CancelPreview()`. Hidden on `PreviewCanceled`, `NavigationStarted`, `PreviewFailed`. (No failure message UI in v1: games can listen to `PreviewFailed`.)
4. `Runtime/UI/NavigationControls.cs` (MonoBehaviour in the full map prefab): **Stop** button visible while `HasActiveRoute` (→ `StopNavigation()`); **Center on car** visible while not following (→ `CenterOnCar()`); **Close** (→ `Close()`). Visibility updated from events and the following flag, not polled every frame where an event exists.

**Tests:** `PlayMode/TapAndPreviewTests.cs`
- `TapOnMap_PreviewReady`
- `TapNearDestinationMarker_UsesMarkerPosition_ReportsMarker`
- `TapNearNonDestinationMarker_UsesMapPoint`
- `TwoMarkersInRadius_ClosestWins`
- `ConfirmStepOff_StartsNavigationDirectly`
- `PreviewPanel_ShowsOnReady_HidesOnCancel`
- `ConfirmButton_StartsNavigation`
- `StopButton_VisibleOnlyWithActiveRoute`
- `CenterButton_VisibleOnlyWhenNotFollowing`

---

## S52 — Crosshair mode and the Input System adapter

**Goal:** gamepad/keyboard support (design: center crosshair, Auto/Always/Never, optional adapter for the new Input System only, default bindings, zoom/pan speeds).

**Depends on:** S51.

**Files:**

1. `Runtime/UI/CrosshairModeLogic.cs` (plain, internal): setting `Auto`/`Always`/`Never`; `NotifyPointerInput()`, `NotifyCrosshairInput()`; property `IsCrosshairActive` (Auto: the last kind of input decides; starts inactive).
2. `MapViewInteractive` (change): serialized `crosshairMode` (Auto), a crosshair `Image` at the viewport center shown while active; `SetCrosshairMode(bool)` (forces on/off in Auto until the next input); `ConfirmAtCrosshair()` = `TapAt(viewport center)` (the marker radius rule gives the "snap to markers" behavior); `PanByStick(Vector2 stick, float deltaTime)` (full tilt = 50% of the view width per second) and `ZoomBySpeed(float axis, float deltaTime)` (full axis = ×2 per second); both call `NotifyCrosshairInput()`. The pointer adapter calls `NotifyPointerInput()`.
3. `Runtime.InputSystem/Gley.NavigationSystem.InputSystem.asmdef`: references `Gley.NavigationSystem`, `Unity.InputSystem`; `versionDefines`: `{ "name": "com.unity.inputsystem", "expression": "", "define": "GLEY_NAV_INPUTSYSTEM" }`; `defineConstraints: ["GLEY_NAV_INPUTSYSTEM"]`.
4. `Runtime.InputSystem/NavigationMapControls.inputactions` (JSON asset) with one action map `Map`: `Pan` (Vector2: left stick, WASD, arrow keys), `Zoom` (Axis: right trigger +, left trigger −, E +, Q −), `Confirm` (button south, Enter), `Cancel` (button east, Escape), `CenterOnCar` (button north, C), `Close` (start button, M).
5. `Runtime.InputSystem/GamepadInputAdapter.cs` (MonoBehaviour): serialized `InputActionAsset` (default: the file above; users can assign a remapped copy), `MapViewInteractive` target. Enables the map in `OnEnable`, disables in `OnDisable`. Each frame: `Pan` → `PanByStick`, `Zoom` → `ZoomBySpeed`; `Confirm` → `ConfirmPreview()` if a preview is open, else `ConfirmAtCrosshair()`; `Cancel` → `CancelPreview()`; `CenterOnCar`; `Close`. Subscriptions via method groups; unsubscribe on disable.
6. Setup/prefab note: the gamepad adapter is **not** on the default prefab (it would not exist without the package); the Setup window (S54) adds it when the Input System package is installed.

**Tests:** `EditMode/CrosshairModeLogicTests.cs`: `Auto_CrosshairInput_Activates`, `Auto_PointerInput_Deactivates`, `Always_IgnoresPointer`, `Never_IgnoresCrosshair`. `PlayMode/CrosshairTests.cs`: `ConfirmAtCrosshair_PreviewsAtCenter`, `ConfirmAtCrosshair_NearDestinationMarker_SnapsToMarker`, `PanByStick_FullTilt_HalfViewPerSecond`.

**Manual checks:** this project has no Input System package. The Input System assembly is simply not compiled here. **To test it, the user installs the Input System package** (Package Manager) in this project or a copy, sets Active Input Handling to Both, and checks: stick pans under the crosshair, triggers zoom, south button confirms, the crosshair hides when the mouse is used (Auto).
