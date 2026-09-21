# Implementation Progress

Mark a step `[x]` and add the date **only after the user confirms** its tests and manual checks passed.

`[HARD]` = recommended Claude Opus 5, or Claude Sonnet 5 at xhigh effort. The agent stops and asks before starting these.

## Phase 1 — Foundation
- [x] S01 Assemblies and test setup (2026-09-18)
- [x] S02 Dev sandbox city and test car (2026-09-18)

## Phase 2 — Rendering spike
- [x] S03 Route line mesh builder (2026-09-18)
- [x] S04 [HARD] Route line shader, graphic and Route Line Lab (2026-09-21)
- [x] S05 Route line chunks (2026-09-21)

## Phase 3 — Core data
- [x] S06 Navigation Settings asset (2026-09-21)
- [x] S07 Coordinate conversions and floating origin shift (2026-09-21)
- [x] S08 Road network runtime data and builder (2026-09-21)
- [x] S09 Road spatial grid (2026-09-21)
- [x] S10 Map asset and data format versions (2026-09-21)

## Phase 4 — Pathfinding
- [x] S11 Route types and snapping (2026-09-21)
- [x] S12 [HARD] A* search (2026-09-21)
- [x] S13 [HARD] Mid-road start and destination (2026-09-21)
- [x] S14 Zero garbage and performance on a large network (2026-09-21)

## Phase 5 — Authoring data and bake
- [ ] S15 Authoring asset
- [ ] S16 Curves, adaptive sampling and ground probe
- [ ] S17 Edit operations
- [ ] S18 Bake and outdated detection
- [ ] S19 Validation
- [ ] S20 Format versions and migrations
- [ ] S21 Importer interface and source replacement

## Phase 6 — Editor tools
- [ ] S22 Map scene object, rectangle handles, sync and overlay
- [ ] S23 Navigation window shell and asset locations
- [ ] S24 Scene view road drawing
- [ ] S25 [HARD] Draw mode
- [ ] S26 [HARD] Edit and Connect modes
- [ ] S27 Validate and Bake modes, Play-mode warning, build check
- [ ] S28 Project settings panel
- [ ] S29 Large network editor check (decision point: chunking)

## Phase 7 — Map image capture
- [ ] S30 Capture planner
- [ ] S31 [HARD] Capture execution, pipeline adapters and import settings
- [ ] S32 Custom images: ratio lock, guidance and template export

## Phase 8 — Runtime navigation
- [ ] S33 Vehicle motion
- [ ] S34 [HARD] Road matching
- [ ] S35 Navigation session
- [ ] S36 Reroute rules
- [ ] S37 [HARD] Navigation Manager: core loop
- [ ] S38 [HARD] Navigation Manager: API, preview flow, events and command queue
- [ ] S39 Formatter and text adapters
- [ ] S40 Navigation Events component
- [ ] S41 Floating origin misconfiguration warning

## Phase 9 — Map views
- [ ] S42 Map View core
- [ ] S43 Route display in views
- [ ] S44 Follow Car behavior
- [ ] S45 Minimap shape, compass, tap to open

## Phase 10 — Markers
- [ ] S46 Marker component, registry and grid
- [ ] S47 Marker layer in views
- [ ] S48 Off-screen arrows

## Phase 11 — Full map interaction
- [ ] S49 Interactive behavior
- [ ] S50 Pointer adapter and gestures
- [ ] S51 Tapping, preview panel and buttons
- [ ] S52 Crosshair mode and the Input System adapter

## Phase 12 — Setup and finish
- [ ] S53 Default prefabs, placeholder art, safe area, default assets
- [ ] S54 Setup window
- [ ] S55 Enter Play Mode Options audit
- [ ] S56 Performance pass

## Implementation notes

Added by the implementing agent when a step passes: anything later steps need to know that differs from the plan or isn't obvious from the code. Format: `Sxx: <note>`.

- S01: `Runtime/AssemblyInfo.cs` and `Editor/AssemblyInfo.cs` had no `using System.Runtime.CompilerServices;` line before this step; it was added along with the `InternalsVisibleTo` attributes.
- S02: In `Dev.Editor`, `System.Random` must be fully qualified (`new System.Random(...)`) — a bare `Random` is ambiguous with `UnityEngine.Random` once both `using System;` and `using UnityEngine;` are present. `SandboxSceneBuilderTests` uses `EditorSceneManager.NewScene(..., NewSceneMode.Single)`, not `Additive` as in the plan — `Additive` throws `InvalidOperationException` in the Test Runner because the active untitled scene isn't saved. `SandboxSceneBuilder` exposes `RoadObjects` (`IReadOnlyList<GameObject>`) and `ExpectedRoadObjectCount` (const, = 18) for tests/later steps to check road-segment output; no layer named `Road` exists yet so road segments currently sit on layer `Default`.
- S03: `RouteLineMeshBuilder.Build` groups vertex pairs internally (one pair per "join point": a plain point, or two pairs for a bevel or a dashed-flag change) and emits a quad between every consecutive pair of groups — this is how the plan's separate bevel/dashed-duplicate rules and the "two triangles per quad" rule combine. When coincident points are skipped (closer than 0.0001 m), the kept segment's dashed flag is taken from the last original segment leading into the next kept point (the plan doesn't specify this interaction).
- S06: `NavigationSettings.AddRoadType` and `RemoveRoadType` bump `version` too (not just `SetRoadTypeSpeed`/`Width`) — the field's own doc comment and the design's cross-cutting section both say add/remove affects bakes. `MoveRoadType` is the only mutator that does not bump it. No `SetUnitsPerMeter` or channel-name setter methods exist yet; editor code should edit those serialized fields directly via `SerializedProperty` until a settings panel (S28) needs otherwise.
- S07: `WorldConverter` defaults to `unitsPerMeter = 1` on construction (not specified in the plan; nothing wires it from `NavigationSettings` yet). `MapFrame.TrueToMap`/`MapToTrue` use the rotation convention: `localX = relX*cos - relZ*sin`, `localZ = relX*sin + relZ*cos` (Unity's standard Y-axis rotation matrix), verified against all worked examples in the plan; later steps needing map rotation math should reuse `MapFrame` rather than re-deriving it. Testing a `CustomLogger.LogError` call (e.g. `WorldConverter.SetUnitsPerMeter`'s guard) needs `UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true` around the call, since `LogError` has no `[Conditional]` gate and would otherwise fail the test — no prior test in the repo needed this pattern.
- S05: `RouteLineRenderer` only forwards style (colors/widths) to a newly pooled `RouteLineGraphic` once `SetStyle` has been called at least once on the renderer (`_hasStyle` flag) — otherwise a graphic created for a longer route would get zeroed-out C# default colors/widths instead of `RouteLineGraphic`'s own built-in defaults. Trim distance and canvas-units-per-meter are always forwarded to new graphics since their 0/1 defaults already match. `SetShader` is only forwarded to graphics (existing or newly created) when a non-null shader was actually set on the renderer, so pooled graphics fall back to `RouteLineGraphic`'s own `Shader.Find` default. Pooled chunk graphics are disabled via `gameObject.SetActive(false)`, never destroyed, and get a full-stretch `RectTransform` (anchors 0,0–1,1, zero offsets) so every chunk shares the same local origin.
- S09: `RoadGrid.GetCellRange(minX, minZ, maxX, maxZ, ...)` is the shared, public helper that turns a world-space bounding box into a clamped cell-coordinate range (min and max clamped independently to `[0, count-1]` on each axis, since clamping is monotonic and preserves `min <= max`); both `RoadGrid.Build` (segment bounding boxes) and `RoadQuery.FindNearest` (query circle) use it, and later grid consumers should reuse it too rather than re-deriving the clamp math (an earlier version that clamped only one side per bound produced an out-of-range cell index when a query fell entirely outside the grid on one axis). `RoadGrid.DefaultCellSize` (= 50f) is the one place the 50 m default lives; `RoadNetworkBuilder.Build`'s new `cellSize` parameter defaults to it.
- S10: `CurrentFormatVersion` stays a `const` on `MapData`, `RoadNetworkData` and `NavigationSettings`; `IFormatVersioned.CurrentFormatVersion` is implemented explicitly (`int IFormatVersioned.CurrentFormatVersion`) on each so it coexists with the const of the same name — access it through an `IFormatVersioned` reference, not the concrete type. Added `MapData.SetFormatVersion` (not in the plan's file list) since migrations (S20) will need it.
- S11: `RouteSnapper` is constructed with a `RoadNetworkData` (builds its own internal `RoadQuery`), not a pre-built `RoadQuery` — simpler for both `Pathfinder` and tests to construct. `Route`'s Pathfinder-filled properties (`Network`, `Failure`, `Start`, `End`, `Destination`, `Length`, `Eta`, `Success`, `ArrivedImmediately`) use `internal set` — S12/S13 set them directly rather than through mutator methods; `Network` is `internal`. `RoutePreferences.SetPreference` removes a road type's entry from its parallel lists when set back to `Normal` (keeps them compact instead of accumulating `Normal` entries). `Route.GetTruePoints` uses one general "cut at any distance" interpolation path for every segment (no separate full-road-vs-partial branch) — later steps needing route polylines should call it as-is rather than re-deriving the interpolation.
- S12: `Pathfinder` is built from reusable pieces S13 should extend rather than rewrite: `BeginSearch(prefs)` (stamp, per-road `costPerMeter`, `minCostPerMeter`), `AddStartState(state, g)` (initial states; parent −1), `EvaluateFinish(intersection, state, g)` (finish candidates against `destinationRoad`/`destinationDistance`/`destinationPoint`), `RunSearch()`, `WriteResult(route)`. Extra internal test entry `FindRouteFromRoad(startRoad, forward, toIntersection, prefs, result)` (approved plan change): a single fixed-direction start state, its road included as the first segment — the U-turn/dead-end tests need it because with an intersection start U-turns are never on a shortest path. Finish rules: a finish piece of length 0 is always legal and appends no segment (so an intersection destination is reached via any arriving road); non-zero finish pieces respect one-way and the U-turn rule. The start intersection is evaluated as a finish with g = 0 (from == to → success, no segments). `Route.Start`/`End` are not filled yet (S13). Added `RoutePreferences.GetSmallestMultiplier()` (internal; min of 1 and every configured type's multiplier) for the heuristic.
- S13: `Pathfinder` now owns a `RouteSnapper` (created in its constructor). `FindRoute` fills `Route.Start`/`End` as soon as both snaps succeed (also on `NoPath`); `Destination` is always `request.To`. The finish candidate stores `bestFinishFrom` (0/Length for intersection arrivals, the start distance for the direct same-road piece), and the path's first state is cut at `startDistance` when `hasStartPoint` is set. Zero-length start pieces (start snapped exactly onto an intersection) are not emitted as segments. The heuristic targets the snapped destination position.
- S14: `Pathfinder.FindRoute` needed no changes — its arrays are all preallocated in the constructor and reused per call, `Route.Clear`/`AddSegment` reuse the internal list, and `RoadRecord`/`RoadPoint` are structs, so it was already allocation-free. `TestCityGenerator.Generate` builds a jittered grid (cell 100 m, ±20 m jitter) plus diagonal roads (~12% of cells) sized from `approximateRoadCount` via a fixed roads-per-cell estimate; for 5000 it produced 5176 roads on the measured run, well inside 4500–5500. Measured average on the dev machine: 1.225 ms/request over 200 pairs on a 5176-road network (target ≤ 2 ms; design's 5 ms low-end-phone target has much more headroom).
- S04: `RouteLineGraphic` must carry `[RequireComponent(typeof(CanvasRenderer))]` — in uGUI 1.0 the base `Graphic` doesn't require it, and without it every rebuild is silently skipped (applies to any future custom `Graphic`). Per the S04 decision, the shader widens in canvas space: hidden property `_CanvasOffsetMatrix` (float4 = 2x2 graphic-to-batch-canvas rotation, times the root/batch canvas scale ratio) is computed by `RouteLineGraphic.UpdateRouteLineVisuals(deltaTime)`, driven by `Canvas.willRenderCanvases`, and set on the materials only when it changes; `_CanvasUnitsPerMeter` is used only for dashes. Internal `CanvasOffsetMatrix` property exposed for tests. The graphic also adds TexCoord1/2 to the root canvas when nested. `raycastTarget` is set false once in `Awake` (hidden serialized `defaultsApplied` flag). The material instance is `HideAndDontSave` and is recreated on enable from the serialized shader. PlayMode tests: a freshly created Overlay canvas rebuilds child graphics once while it settles, so build-count tests use `[UnitySetUp]` with two settle frames, compare against a baseline, and wait with `yield return null` + `Canvas.ForceUpdateCanvases()`.

## Decisions

Decisions the user makes during implementation (decision points S04, S29, S56, or answers to plan questions), with the date. Format: `YYYY-MM-DD Sxx: <decision>`.

- 2026-09-21 S04: Route line shader widens the line in canvas space using a per-graphic `_CanvasOffsetMatrix` updated on `Canvas.willRenderCanvases` (option B), instead of offsetting in graphic-local meters. Same performance as the sub-canvas-only approach and keeps the "one canvas, no sub-canvases" fallback viable. Nested-canvas clipping passed manual check 2, so the fallback is not needed; S05 keeps `useOwnCanvas = true`.
