# Phase 8 — Runtime Navigation (S33–S41)

Design references: section 2 (floating origin, misconfiguration warning), section 7 "Vehicle tracking", section 8 "Route management and rerouting", section 12 (preview flow), section 14 (events, failures, API, startup), "Cross-cutting" (update order, time, units, one tracked car), "Default values → Routing and tracking".

Tracking and routing logic are **plain classes** (EditMode tests with scripted positions). The Manager (S37+) wires them together (PlayMode tests).

All values below marked "default" come from the design's default tables and must be exposed as settings on the Manager (serialized fields with those defaults).

---

## S33 — Vehicle motion (speed and headings)

**Goal:** speed, movement direction, nose direction, stopped and teleport detection.

**Depends on:** S07.

**Files:** `Runtime/Tracking/VehicleMotion.cs` (plain, internal)

- `Reset(Vector3 truePos, Vector3 trueNose)`: clears history; `HasMovedOnce = false`.
- `UpdateVehicleMotionLogic(Vector3 truePos, Vector3 trueNose, float deltaTime)`: if `deltaTime <= 0` → do nothing.
  - `displacement` = X/Z distance from the last position. `Teleported = displacement > teleportDistance` (default 50 m). On teleport: `Reset` to the new position and keep `Teleported = true` for this update.
  - `Speed = displacement / deltaTime` (m/s).
  - `IsStopped = Speed < stoppedSpeed` (default 0.1).
  - **Nose heading:** `trueNose` flattened to X/Z; if its length < 0.01 (car flipped/vertical) keep the last valid nose. (The yaw offset is applied by the Manager before calling.)
  - **Movement heading:** if `Speed >= minHeadingSpeed` (default 1 m/s): normalized displacement, `HasMovedOnce = true`. Otherwise keep the last one. **Before the first movement** (`HasMovedOnce == false`): movement heading = nose heading.
  - `IsReversing = dot(movementHeading, noseHeading) < 0` (info only).

**Tests:** `EditMode/VehicleMotionTests.cs`
- `Speed_IsDisplacementOverTime`
- `DeltaTimeZero_NoChange`
- `Stopped_BelowThreshold`
- `Teleport_Over50m_FlagsAndResets`
- `MovementHeading_BeforeFirstMove_IsNose`
- `MovementHeading_SlowMovement_KeepsLast`
- `NoseHeading_Vertical_KeepsLastValid`
- `Reversing_MovementOppositeNose_True`

---

## S34 — Road matching

**Difficulty: HARD** (map matching: forks, continuity, hysteresis, lost search). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** which road the car is on, with forks, continuity, off-road and "lost" handling (design section 7).

**Depends on:** S33, S09.

**Files:**

1. `Runtime/Data/RoadQuery.cs` (change): add `FindAllWithin(Vector3 truePos, float radius, List<RoadPoint> output)` — the nearest point **per road** within the radius (no duplicates per road), no allocations.
2. `Runtime/Tracking/RoadMatcher.cs` (plain, internal). Public read state: `IsOnRoad`, `RoadIndex`, `DistanceAlong`, `SnappedPosition` (True), `MovingForward` (car moves from road start toward end), per-update flags `EnteredRoad`, `LeftRoad`, `ChangedRoad`.
   - Thresholds per road: `leave = road.Width / 2 + leaveMargin` (default margin 3 m), `back = road.Width / 2`.
   - **Score** of a candidate road point: `lateralDistance + 10 * (1 − |dot(movementHeading, tangent)|)`. Lower is better.
   - `UpdateRoadMatchingLogic(Vector3 truePos, Vector3 movementHeading, bool stopped, bool teleported)`:
     1. Clear per-update flags. If `stopped` and not `teleported` → return.
     2. If `teleported` → become **lost**.
     3. **On road:** `ProjectOnRoad(RoadIndex)`.
        - `lateral > leave` → `LeftRoad`, become lost, and run the lost search immediately (the car may have moved onto an adjacent road).
        - **Reached the end** (projection is at the road end in the direction of travel and the car is past it): candidates = roads linked at that intersection except the current one (include the current one only at a dead end). Project onto each; keep those with `lateral <= leave`. None → lost. One → switch (`ChangedRoad`). Several → **fork state** (below).
        - Otherwise update `DistanceAlong`, `SnappedPosition`, `MovingForward = dot(movementHeading, tangent) >= 0`.
     4. **Fork state:** keep up to 3 candidates; each update re-project all; the current road = best score (for the marker). Resolved when only one candidate still has `lateral <= its Width / 2` → it becomes the road. All candidates above `leave` → lost.
     5. **Continuity check** (distance based, not time): every 10 m driven while on road, run `FindAllWithin(truePos, 20 m)`. If a road **not connected** to the current one scores better by more than 2 m, accumulate distance driven while that stays true; after **10 m** (default "switch to unconnected road") → switch (`ChangedRoad`). Reset the accumulator when it's no longer better.
     6. **Lost search:** `FindAllWithin(truePos, 30 m)`; pick the best score among candidates with `lateral <= back` → `EnteredRoad`. Otherwise stay off-road (`IsOnRoad = false`).
   - `Reset()` → lost.

**Tests:** `EditMode/RoadMatcherTests.cs` (scripted positions along test networks; add fork/parallel/bridge builders to `TestNetworks`)
- `DriveAlongRoad_StaysOnRoad_DistanceIncreases`
- `DriveAcrossIntersection_SwitchesToStraightRoad`
- `Stopped_NoChange`
- `LeaveSideways_BeyondLeaveThreshold_LeftRoadThenLost`
- `ReturnToRoad_WithinBackThreshold_EnteredRoad`
- `Hysteresis_BetweenBackAndLeave_NoFlicker` (oscillate at width/2 + 1 m: stays on road)
- `ParallelOneWays_HeadingPicksCorrectRoad`
- `SmallAngleFork_KeepsCandidates_ResolvesWhenSeparated`
- `BridgeCrossing_StaysOnBridgeRoad` (two unconnected roads crossing in X/Z)
- `WrongInitialMatch_UnconnectedBetterFor10m_Switches`
- `Teleport_BecomesLost_ThenFindsNewRoad`
- `FindAllWithin_OnePointPerRoad`

---

## S35 — Navigation session (progress and arrival)

**Goal:** following an active route: progress, trim distance, remaining distance, ETA, arrival (design section 8).

**Depends on:** S34, S13.

**Files:** `Runtime/Navigation/NavigationSession.cs` (plain, internal)

- `Start(Route route)`: precompute per segment its start distance along the route. `ProgressDistance = 0`, `CurrentSegment = 0`.
- `UpdateNavigationSessionLogic(RoadMatcher matcher, float drivenDistance)`:
  - **Off-road:** progress, remaining distance and ETA **freeze**. `OffRoad = true`.
  - **On road:**
    - Road = current segment's road and moving in the segment's direction → progress = segment start + |DistanceAlong − segment.From| (clamped to the segment).
    - Road = one of the next segments (look ahead while the skipped length is < 30 m) with matching direction → advance `CurrentSegment` to it.
    - Road = current segment's road but moving **opposite** → `WrongWayDistance += drivenDistance`; otherwise `WrongWayDistance = 0`.
    - Road not found in the current/look-ahead segments → `WrongTurn = true`.
  - **Arrival:** on the last segment and (passed the destination distance in the travel direction **or** remaining ≤ `arrivalDistance`, default 10 m) → `Arrived = true`. Off-road destination: the same rule applies to the route end (the dotted piece doesn't count).
  - `RemainingDistance = route.Length − ProgressDistance`. `Eta` = remaining part of the current segment / its road speed + full remaining segments' length / speed.
  - `TrimDistance = ProgressDistance` (used by the route line).
- `JumpTo(RoadMatcher matcher)`: after a teleport that landed on a route road: find the segment with that road and matching direction closest to the current progress; set progress there (can go backward).
- `Stop()`.

**Tests:** `EditMode/NavigationSessionTests.cs`
- `DriveRoute_ProgressIncreases_RemainingDecreases`
- `NextSegment_Advances`
- `ShortSegmentSkipped_LookAheadAdvances`
- `OppositeDirection_AccumulatesWrongWayDistance`
- `UnknownRoad_WrongTurn`
- `OffRoad_Freezes`
- `ArrivalPassingPoint_Arrived`, `ArrivalWithinDistance_Arrived`
- `Eta_UsesRoadSpeeds`
- `JumpTo_BackwardOnRoute_ProgressDecreases`

---

## S36 — Reroute rules

**Goal:** when to reroute (design table in section 8, cooldown, bypasses).

**Depends on:** S35.

**Files:** `Runtime/Navigation/RerouteReason.cs` (enum: `None`, `WrongTurn`, `TurnedAround`, `BackOnRoad`, `CarChanged`, `PreferencesChanged`, `Teleported`) and `Runtime/Navigation/RerouteDecider.cs` (plain, internal).

- `DistanceSinceLastReroute` accumulates driven distance; `OnRerouted()` resets it.
- `Decide(NavigationSession session, RoadMatcher matcher, bool teleported)` → `RerouteReason`:
  - `teleported` and the matched road is **not** on the route → `Teleported` (**ignores cooldown**). On the route → `None` (the Manager calls `JumpTo`).
  - Cooldown active (`DistanceSinceLastReroute < rerouteCooldown`, default 20 m) → `None` for the rest.
  - `session.WrongTurn` → `WrongTurn`.
  - `session.WrongWayDistance >= turnedAroundDistance` (default 30 m) → `TurnedAround`.
  - `matcher.EnteredRoad` after being off-road, and the road isn't on the route (current/look-ahead) → `BackOnRoad`.
- `CarChanged` and `PreferencesChanged` are decided by the Manager directly (always ignore the cooldown).

**Tests:** `EditMode/RerouteDeciderTests.cs`
- `WrongTurn_AfterCooldown_Reroutes`, `WrongTurn_DuringCooldown_Waits`
- `TurnedAround_Under30m_NoReroute`, `TurnedAround_30m_Reroutes`
- `OffRoadBackOnRouteRoad_NoReroute`, `OffRoadBackOnOtherRoad_BackOnRoad`
- `Teleport_OffRoute_IgnoresCooldown`, `Teleport_OnRoute_None`

---

## S37 — Navigation Manager: core loop

**Difficulty: HARD** (Manager lifecycle, map registration, floating origin, update order). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** the Manager component: settings, car, map registration and automatic choice, floating origin, update order, time handling, read-only properties. (API and events come in S38.)

**Depends on:** S36, S22.

**Files:**

1. `Runtime/Navigation/NavigationManager.cs` (MonoBehaviour, public, `[DefaultExecutionOrder(-100)]`)
   - Serialized (with design defaults): `settings` (NavigationSettings), `explicitMap` (NavigationMap, optional), `car` (Transform), `carYawOffset` (float), `startManually` (bool), `shiftSource` (ShiftSource), `routeMode`, `uTurnRule`, `avoidMultiplier` (5), `preferMultiplier` (0.7), `startSnapDistance` (200), `destinationSnapDistance` (50), `arrivalDistance` (10), `turnedAroundDistance` (30), `rerouteCooldown` (20), `minHeadingSpeed` (1), `stoppedSpeed` (0.1), `teleportDistance` (50), `leaveMargin` (3).
   - `Start()`: if not `startManually` → `Initialize()`. `Initialize()` (public, idempotent).
   - **Map registration:** `RegisterMap(NavigationMap map)` / `UnregisterMap(NavigationMap map)` (internal). **Automatic choice** (design "Automatic map choice"): explicit reference or `SetMap` wins; otherwise, with no active map and exactly one registered → activate it; several and none chosen → `LogWarning` "several maps loaded, call SetMap" (once per change); active map unregistered → stop navigation (S38 fires the event), clear, and apply the rule again.
   - **Map activation** (the only place allowed to allocate): `MapFrame`, `RoadQuery`, `Pathfinder`, `RoadMatcher`, `NavigationSession`, `RerouteDecider`, routes, lists. If the map has no road network → treat as no map (`NoMap`).
   - **Floating origin:** `OriginShiftTracker` with the Manager's `shiftSource`. Rectangle mode reads the **active** map object's Transform position vs. the map asset's `editTimeWorldPosition` each update. If the map object's Y rotation differs from the asset's `rectangleRotationY` by more than 0.01 degrees -> `LogWarning` once per map activation ("The map object was rotated at runtime; floating origin systems should only move it."). `OnOriginShifted(Vector3 delta)` (public) works only in Manual mode (otherwise `LogWarning`).
   - **Car:** reading uses `car.rotation * Quaternion.Euler(0, carYawOffset, 0) * Vector3.forward` for the nose.
   - `LateUpdate` → `UpdateNavigationLogic(Time.deltaTime)`: if not initialized or `deltaTime <= 0` → return. Order: `UpdateShiftLogic` → `UpdateCarLogic(deltaTime)` (World → True via `WorldConverter`, `VehicleMotion`) → `UpdateTrackingLogic` (`RoadMatcher`) → `UpdateRouteLogic` (session, reroute decisions — actual rerouting is wired in S38) → `UpdateOutsideMapLogic`.
   - Read-only properties: `ActiveMap`, `Car`, `IsInitialized`, `IsOffRoad`, `IsOutsideMap`, `Speed` (m/s, from `VehicleMotion`), `CurrentRoadId` (−1 when none), `CarTruePosition` (internal), `CarMapPosition` (internal), `NoseHeading`, `MovementHeading` (internal), `Converter` (internal `WorldConverter`), `Frame` (internal `MapFrame`).
2. `Runtime/Data/NavigationMap.cs` (change): on `OnEnable` find the Manager (serialized reference if set, otherwise one `FindAnyObjectByType<NavigationManager>()`, cached) and `RegisterMap(this)`; `OnDisable` → `UnregisterMap(this)`. If no Manager exists yet, do nothing.
3. **Enable-order safety:** a map can be enabled before the Manager exists (scene load order, objects created in code). So in `Initialize()` the Manager also registers every enabled `NavigationMap` already in the loaded scenes (`FindObjectsByType<NavigationMap>(FindObjectsSortMode.None)`); `RegisterMap` ignores duplicates.

**Tests:** `PlayMode/NavigationManagerCoreTests.cs` (build everything in code: settings via `CreateInstance` + `ResetToDefaults`, a `RoadNetworkData` from `TestNetworks`, a `MapData` pointing to it, GameObjects for the map, the Manager and the car; destroy all in TearDown)
- `SingleMap_ActivatedAutomatically`
- `TwoMaps_NoneChosen_LogsWarning_NoActiveMap` (use `LogAssert.Expect`)
- `ExplicitMap_WinsOverAutomatic`
- `MapCreatedBeforeManager_RegisteredOnInitialize`
- `ActiveMapDisabled_OtherSingleMapActivated`
- `StartManually_NotInitializedUntilInitializeCalled`
- `CarMovesAlongRoad_CurrentRoadIdMatches`
- `CarMovesAway_IsOffRoadTrue`
- `YawOffset90_NoseHeadingRotated`
- `RectangleShift_MapObjectMoved_TruePositionUnchanged` (move both the car and the map object by (1000,0,0) in one frame: tracking stays on the same road, no teleport)
- `ManualShift_OnOriginShifted_Accumulates`
- `TimeScaleZero_NoLogicUpdate`
- `RectangleRotatedAtRuntime_LogsWarningOnce`
- `Speed_MatchesCarMovement`
- `CarOutsideRectangle_IsOutsideMapTrue`

---

## S38 — Navigation Manager: API, preview flow, events and command queue

**Difficulty: HARD** (full API behavior table, events, command queue). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** the complete public API and events from design section 14.

**Depends on:** S37.

**Files:**

1. `Runtime/Navigation/StopReason.cs` (enum `StopCalled`, `MapChanged`, `CarRemoved`).
2. `Runtime/Navigation/NavigationRouteRequest.cs` (public class, **World** space, for `RequestRoute`): `From`, `To`, `HasHeading`, `Heading`, `Preferences` (optional; null = the Manager's current preferences).
3. `NavigationManager` (change) — methods (all public):
   - `SetMap(NavigationMap map)`, `SetCar(Transform car, float yawOffset = 0)`, `PreviewDestination(Vector3 worldPoint)`, `StartNavigation(Vector3 worldPoint)`, `ConfirmPreview()`, `CancelPreview()`, `StopNavigation()`, `SetRouteMode(RouteMode)`, `SetRoadTypePreference(int typeId, RoadTypePreference)`, `SetUTurnRule(UTurnRule)`, `RequestRoute(NavigationRouteRequest request, Action<Route> callback)`.
   - Events (C# `event Action<...>`): `MapChanged(NavigationMap)`, `CarChanged(Transform)`, `PreviewReady(Route, MapMarker)` (marker is null until S51 wires tapping markers; create `Runtime/Markers/MapMarker.cs` now as an **empty public MonoBehaviour** — S46 fills it in), `PreviewFailed(FailureReason)`, `PreviewCanceled()`, `NavigationStarted(Route)`, `Rerouted(Route, RerouteReason)`, `RouteFailed(FailureReason)`, `Arrived()`, `NavigationStopped(StopReason)`, `OffRoad()`, `BackOnRoad()`, `OutsideMap()`, `BackInsideMap()`.
   - Properties: `ActiveRoute`, `PreviewRoute`, `RemainingDistance`, `Eta`, `HasActiveRoute`, `HasPreview`, `TrimDistance`.
   - **Route objects** are reused (active, preview, scratch); swap references instead of allocating. `RequestRoute` creates a **new** `Route` for the caller (it's not per frame).
   - **Requests from the car:** From = car True position, heading = movement heading (HasHeading true), preferences = current.
4. **Behavior rules** (implement exactly):

| Call / situation | Behavior |
|---|---|
| `PreviewDestination` | No map → `PreviewFailed(NoMap)`; no car → `PreviewFailed(NoCar)`; failure → `PreviewFailed(reason)`, active route untouched; success → store preview destination, `PreviewReady`. A new preview replaces the old one. |
| Preview while driving | Recompute the preview whenever the matcher reports `ChangedRoad` or `EnteredRoad`. Failure → `PreviewFailed`. |
| `ConfirmPreview` | Recompute from the current car state; success → becomes the active route, preview cleared, `NavigationStarted`; failure → `RouteFailed`, current navigation kept. |
| `CancelPreview` | Clear preview, `PreviewCanceled` (only if a preview existed). |
| `StartNavigation` | Like confirm without a preview: success → `NavigationStarted`; failure → `RouteFailed`, current navigation kept. |
| `StopNavigation` | If active: clear, `NavigationStopped(StopCalled)`. |
| Reroute (from `RerouteDecider`) | Recompute from the car; success → `Rerouted(route, reason)`; failure → `RouteFailed(reason)` and navigation **stops**. |
| Teleport on the route | `session.JumpTo`, no event. |
| Arrival | Clear route, `Arrived`. |
| `ArrivedImmediately` result | `Arrived` right away (no `NavigationStarted`). |
| `SetMap` | Active navigation → `NavigationStopped(MapChanged)`; preview → `PreviewCanceled`; then `MapChanged`. |
| `SetCar(newCar)` during navigation | Reset motion/matcher, recompute from the new car: `Rerouted(route, CarChanged)`; failure → `RouteFailed`, stop. Open preview recomputed. Then `CarChanged`. |
| `SetCar(null)` | Active navigation → `NavigationStopped(CarRemoved)`; `CarChanged(null)`. |
| Preference setters | Mark dirty; at the **end of this frame's update** (once, even for several calls) recompute the active route (`Rerouted(route, PreferencesChanged)`, ignores cooldown) and the preview. Outside Play/when no active route: just store. |
| Off-road / outside map transitions | Fire `OffRoad` / `BackOnRoad` / `OutsideMap` / `BackInsideMap` once per transition. |

5. **Command queue** (design "API calls from inside event handlers"): `Runtime/Core/CommandQueue.cs` (plain). The Manager tracks a **dispatch depth** (incremented around every event invocation). A state-changing API call made while depth > 0 is **queued** (a preallocated list of small command structs: type + world point / enum / reference). When the outermost dispatch ends, process the queue in order; events raised while processing may queue more; allow at most **3 rounds**, then `LogError` and drop the rest. Calls at depth 0 run immediately. `RequestRoute` always runs immediately.
6. Unsubscribe-safety: events are plain C# events; the Manager never holds references to subscribers beyond the event fields.

**Tests:** `PlayMode/NavigationManagerApiTests.cs` — one test per table row, plus:
- `EventHandler_CallsStartNavigationInsideArrived_AppliedAfterDispatch` (next route active in the same frame, `NavigationStarted` fired after `Arrived`)
- `EventHandlerLoop_StopsAfter3Rounds_LogsError` (a handler that re-queues forever)
- `PreferencesChangedTwiceInOneFrame_OneReroute`
- `RequestRoute_DoesNotChangeActiveRoute`
- `RequestRoute_GetPoints_AreWorldPositions` (with a shifted map)
- `DriveToDestination_ArrivedFires` (move the car along the route over frames)
- `WrongTurn_ReroutedWithReason`

---

## S39 — Formatter and text adapters

**Goal:** units and text without per-frame garbage (design: units, text, TextMeshPro isolation).

**Depends on:** S37 (it changes `NavigationManager`).

**Files:**

1. `Runtime/UI/NavigationFormatter.cs` (public abstract `ScriptableObject`): `abstract void FormatDistance(float meters, StringBuilder output)`, `abstract void FormatDuration(float seconds, StringBuilder output)`. Both **append** to `output`.
2. `Runtime/UI/DefaultNavigationFormatter.cs` (`ScriptableObject`, `[CreateAssetMenu(menuName = "Gley/Navigation System/Default Formatter")]`)
   - `unitSystem` enum: `ProjectSetting` (uses `NavigationSettings.imperialUnits`; needs a `settings` reference), `Metric`, `Imperial`.
   - Metric: < 1000 m → rounded to 10 m, "850 m"; < 10 km → one decimal "1.2 km"; ≥ 10 km → "12 km".
   - Imperial: < 0.1 mi → feet rounded to 50 ft, "500 ft"; < 10 mi → one decimal "1.2 mi"; else "12 mi".
   - Duration: < 60 s → "< 1 min"; < 60 min → "N min"; else "H h M min".
   - Decimal point is always "." (culture independent).
   - **No allocations:** append digits manually (a private `AppendInteger` that writes digits into the `StringBuilder` without `int.ToString()`).
3. `Runtime/UI/NavigationTextTarget.cs` (public abstract MonoBehaviour): `abstract void SetText(StringBuilder text)`.
4. `Runtime/UI/LegacyTextTarget.cs`: wraps `UnityEngine.UI.Text` (`text = sb.ToString()` — allocates only when called; callers only call when the value changed).
5. `Runtime.TMP/Gley.NavigationSystem.TMP.asmdef`: references `Gley.NavigationSystem`, `Unity.TextMeshPro`; `versionDefines`: `{ "name": "com.unity.textmeshpro", "expression": "", "define": "GLEY_NAV_TMP" }` and `{ "name": "com.unity.ugui", "expression": "2.0.0", "define": "GLEY_NAV_TMP" }`; `defineConstraints: ["GLEY_NAV_TMP"]`.
   `Runtime.TMP/TmpTextTarget.cs`: wraps `TMP_Text`, uses `SetText(StringBuilder)` (no garbage).
6. `NavigationManager` (change): serialized `formatter` (NavigationFormatter) + `SetFormatter(NavigationFormatter)` + property `Formatter`. **Fallback:** if no formatter is assigned at `Initialize` (or `SetFormatter(null)` is called), create one `DefaultNavigationFormatter` with `ScriptableObject.CreateInstance` (unit system ProjectSetting, using the Manager's settings) and destroy it in `OnDestroy`. `Formatter` is never null after `Initialize`.
7. Test asmdef `Gley.NavigationSystem.Tests.Editor` (change): add references `Gley.NavigationSystem.TMP`, `Unity.TextMeshPro`.

**Tests:** `EditMode/DefaultNavigationFormatterTests.cs`, `EditMode/TmpTextTargetTests.cs`
- Distances: 0 → "0 m", 847 → "850 m", 1234 → "1.2 km", 12345 → "12 km"; imperial 30 m → "100 ft", 5000 m → "3.1 mi".
- Durations: 30 → "< 1 min", 300 → "5 min", 3900 → "1 h 5 min".
- `ProjectSetting_ReadsImperialFromSettings`
- `FormatDistance_NoGarbage` (`Is.Not.AllocatingGCMemory()` with a reused StringBuilder with enough capacity)
- `TmpTextTarget_SetText_UpdatesText`
- `PlayMode/NavigationManagerFormatterTests.cs`: `NoFormatterAssigned_DefaultCreated`, `SetFormatterNull_FallsBackToDefault`

---

## S40 — Navigation Events component (Inspector wiring)

**Goal:** UnityEvents for designers (design "Inspector events").

**Depends on:** S38.

**Files:** `Runtime/Navigation/NavigationEvents.cs` (MonoBehaviour): serialized `manager` reference (auto-find once if empty). One `UnityEvent` per Manager event: no arguments, except `UnityEvent<FailureReason>` for `PreviewFailed`/`RouteFailed`, `UnityEvent<StopReason>` for `NavigationStopped`, `UnityEvent<RerouteReason>` for `Rerouted`. Subscribes in `OnEnable`, unsubscribes in `OnDisable`, forwards with method groups (no lambdas).

**Tests:** `PlayMode/NavigationEventsTests.cs`
- `Arrived_ForwardedToUnityEvent`
- `RouteFailed_ForwardsReason`
- `Disabled_NoLongerForwards`

---

## S41 — Floating origin misconfiguration warning

**Goal:** the design's once-per-session warning.

**Depends on:** S37.

**Files:** `NavigationManager` (change): when `VehicleMotion.Teleported` is true, the car was on a road just before, and after the lost search there is **no road within `startSnapDistance`** or the car is **outside the map rectangle** → `CustomLogger.LogWarning("The car jumped far away from all roads. If you use a floating origin system, make sure it moves the map object, or set Shift source to Manual.")` — only the first time for this Manager instance (instance flag).

**Tests:** `PlayMode/MisconfigurationWarningTests.cs`
- `CarShiftedButMapNot_LogsWarningOnce` (move only the car by 5000 m twice: `LogAssert.Expect` once, then `LogAssert.NoUnexpectedReceived()`)
- `NormalTeleportToRoad_NoWarning`
