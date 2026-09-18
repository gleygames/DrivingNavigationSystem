# Phase 4 — Pathfinding (S11–S14)

Design references: section 6 "Pathfinding", section 14 "Failure reasons", "Default values → Routing and tracking".

All pathfinding works in **True** meters. Conversion from World happens in the Manager (Phase 8).

---

## S11 — Route types and snapping

**Goal:** the request/result types and snapping of the start and destination to roads.

**Depends on:** S09.

**Files:**

1. `Runtime/Pathfinding/RouteMode.cs` — enum `Shortest`, `Fastest`.
2. `Runtime/Pathfinding/RoadTypePreference.cs` — enum `Normal`, `Avoid`, `Prefer`.
3. `Runtime/Pathfinding/UTurnRule.cs` — enum `Never`, `AtIntersections`, `Anywhere`.
4. `Runtime/Pathfinding/FailureReason.cs` — enum `None`, `NoMap`, `NoCar`, `NoRoadNearStart`, `NoRoadNearDestination`, `NoPath`.
5. `Runtime/Pathfinding/RoutePreferences.cs` (public class)
   - `Mode`, `UTurn`, `AvoidMultiplier` (default 5), `PreferMultiplier` (default 0.7).
   - Per road type preference stored in two parallel reused lists (type IDs, preferences). `SetPreference(int typeId, RoadTypePreference value)`, `GetPreference(int typeId)` (missing = Normal), `GetMultiplier(int typeId)`.
   - `CopyFrom(RoutePreferences other)` (no allocation when list capacity is enough).
6. `Runtime/Pathfinding/RouteRequest.cs` (public class, reusable)
   - `From` (True Vector3), `HasHeading`, `Heading` (True direction), `To` (True Vector3), `Preferences` (RoutePreferences), `StartSnapDistance` (default 200), `DestinationSnapDistance` (default 50), `ArrivalDistance` (default 10).
   - `Set(Vector3 from, Vector3 to)`, `SetHeading(Vector3 heading)`, `ClearHeading()`.
7. `Runtime/Pathfinding/RouteSegment.cs` (public struct): `RoadIndex`, `RoadId`, `Forward` (bool), `FromDistance`, `ToDistance` (along the road, meters; for `Forward == false`, From > To).
8. `Runtime/Pathfinding/Route.cs` (public class, reusable — has `Clear()`)
   - `Success`, `Failure` (FailureReason), `ArrivedImmediately` (bool), `Start` (RoadPoint), `End` (RoadPoint), `Destination` (True Vector3 — the originally requested point, used for the dotted line), `Segments` (read-only list view), `Length` (m), `Eta` (s).
   - `GetTruePoints(List<Vector3> output)`: fills the polyline from the segments using the network's shape points (partial first/last roads cut at their distances; no duplicate points at joins). Needs the `RoadNetworkData` reference stored on the route when it's filled.
   - `GetPoints(List<Vector3> output)` (public API): same as `GetTruePoints` but converted to **World** through a `WorldConverter` reference set by the Manager. If none is set, returns True points.
9. `Runtime/Pathfinding/RouteSnapper.cs` (plain, internal) — uses `RoadQuery`.
   - `SnapStart(RouteRequest request, out RoadPoint point)` → `FailureReason` (`NoRoadNearStart` beyond `StartSnapDistance`).
   - `SnapDestination(RouteRequest request, out RoadPoint point)` → `FailureReason` (`NoRoadNearDestination` beyond `DestinationSnapDistance`).

**Tests:** `EditMode/RoutePreferencesTests.cs`, `EditMode/RouteSnapperTests.cs`
- `GetMultiplier_Default_IsOne`, `GetMultiplier_Avoid_IsFive`, `GetMultiplier_Prefer_IsPointSeven`
- `CopyFrom_CopiesAllValues`
- `SnapStart_Within200m_Succeeds`, `SnapStart_Beyond200m_NoRoadNearStart`
- `SnapDestination_Beyond50m_NoRoadNearDestination`
- `SnapStart_UsesStartDistance_NotDestinationDistance` (point 120 m from road: start OK, destination fails)

---

## S12 — A* search

**Difficulty: HARD** (edge-based A* with one-way, U-turn and dead-end rules). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** the core search with road costs, preferences, one-way roads and U-turn rules. Start and destination are at **intersections** in this step; mid-road points come in S13.

**Depends on:** S11.

**Files:** `Runtime/Pathfinding/Pathfinder.cs` (plain, internal). Constructed with a `RoadNetworkData`; allocates all its arrays once, sized for that network.

**Algorithm (edge-based A\*):** follow exactly.

- **State** = a road traversed in one direction: `state = roadIndex * 2 + dir`, `dir` 0 = forward (start → end), 1 = backward. The state's cost `g` = cost to reach the **end intersection of that traversal** (`End` for forward, `Start` for backward).
- **Arrays** (size `2 * RoadCount`, reused): `g` (float), `parent` (int, −1 = none), `visitStamp` (int; compare with a per-search counter instead of clearing arrays), `closed` stamp.
- **Open list:** a binary min-heap over `f = g + h`, implemented with pre-allocated arrays (initial capacity `4 * RoadCount`, grows by doubling only if needed). Lazy deletion: skip popped states whose `g` is worse than the stored one.
- **Cost per meter** of road `r`:
  - Shortest: `multiplier(type)`.
  - Fastest: `multiplier(type) / r.Speed`.
- **Heuristic** `h(state)` = straight X/Z distance from the traversal's end intersection to the destination point × `minCostPerMeter`, where
  - Shortest: `minCostPerMeter = smallest multiplier in use` (0.7 if any type is Prefer, else 1).
  - Fastest: `minCostPerMeter = smallest multiplier in use / network.MaxSpeed`.
- **Successors** of a state that ends at intersection `X`: for each road `r2` linked at `X`:
  - Entering from `X`: if `r2.StartIntersection == X` → `dir2 = 0`; if `r2.EndIntersection == X` → `dir2 = 1` (a loop road with both ends at `X` gives both).
  - Skip `dir2 == 1` if `r2.OneWay`.
  - **U-turn check:** if `r2 == r` and `dir2 != dir` (going back the way we came): allowed only if `UTurn` is `AtIntersections` or `Anywhere`, **or** `X` is a dead end (`LinkCount == 1`).
  - New `g = g + r2.Length * costPerMeter(r2)`.
- **Goal handling:** the destination is given as a road point `(destRoad, e)`. Whenever a state reaches an intersection `X` where `destRoad` is linked, compute the **finish candidate**: entering `destRoad` with `dir2` as above (respecting one-way), partial cost `e` (dir 0) or `Length − e` (dir 1), times its cost per meter. Keep the best candidate (cost + which state + which dir).
- **Stop** when the heap is empty or the smallest `f` in the heap ≥ best candidate cost.
- **Result:** rebuild segments by following `parent` from the best candidate's state back to the start, reverse, append the final partial segment.

**In this step:** start = "at intersection `S`" is modeled as initial states for every road linked at `S` (respecting one-way), each with `g = Length × costPerMeter`. Destination = intersection `D` is modeled as `(any linked road, distance 0 or Length)`. Provide an internal test entry point `FindRouteBetweenIntersections(int from, int to, RoutePreferences prefs, Route result)`.

**Tests:** `EditMode/PathfinderTests.cs` (use `TestNetworks`; add helper builders as needed)
- `Line_FromFirstToLast_UsesAllRoadsForward`
- `Square_OppositeCorner_ShortestHasLength2Sides`
- `OneWay_AgainstDirection_TakesLongWayAround`
- `OneWay_OnlyPathAgainst_ReturnsNoPath`
- `Disconnected_ReturnsNoPath`
- `Fastest_PrefersFasterLongerRoad` (two parallel routes: short slow Local vs longer Highway)
- `Shortest_IgnoresSpeed`
- `Avoid_Highway_TakesOtherRoadIfNotMuchLonger`
- `Avoid_Highway_StillUsedWhenOnlyPath` (avoid ≠ forbid)
- `Prefer_Type_ChangesChoice`
- `UTurnNever_AtIntersection_NotUsed` (a network where a U-turn at an intersection would be shortest)
- `UTurnAtIntersections_AllowsIt`
- `DeadEnd_AlwaysAllowsTurnAround`
- `Eta_IsLengthOverSpeedWithoutMultipliers`
- `Result_MatchesDijkstra_OnRandomGrid` (fixed seed 42: 50 random pairs on a 10×10 grid with random one-ways; compare total cost with a simple Dijkstra written inside the test class)

---

## S13 — Mid-road start and destination

**Difficulty: HARD** (mid-road start/end pieces, heading rules, same-road cases). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** real requests: start and destination anywhere on roads, optional heading, same-road cases, "arrived immediately".

**Depends on:** S12.

**Files:** `Runtime/Pathfinding/Pathfinder.cs` (change) — public-to-assembly entry point `FindRoute(RouteRequest request, Route result)`:

1. `result.Clear()`. Snap start and destination (`RouteSnapper`); on failure set `Failure` and return.
2. **Arrived immediately:** if the X/Z distance between `request.From` and the snapped destination position ≤ `ArrivalDistance` → `Success = true`, `ArrivedImmediately = true`, no segments. Return.
3. **Initial states** from the start point `(r0, d)` (the road is split only for this request; the network is never changed):
   - Forward piece (toward `End`): allowed always. Cost `(Length − d) × cpm`.
   - Backward piece (toward `Start`): allowed unless `r0.OneWay`. Cost `d × cpm`.
   - **Heading given** and `UTurn != Anywhere`: keep only the piece whose direction matches the heading (`dot(heading, tangent) >= 0` → forward). If that piece isn't allowed (driving the wrong way on a one-way road), keep the allowed piece instead.
4. **Destination** `(destRoad, e)` as in S12's goal handling.
5. **Same road** (`destRoad == r0`): add a direct finish candidate before the search: forward allowed and `e >= d` → cost `(e − d) × cpm`; backward allowed (and heading rule allows it) and `e <= d` → cost `(d − e) × cpm`. A* still runs and may find a cheaper way (it won't, but the rule must hold).
6. Fill `Start`, `End`, `Destination = request.To`, segments (first and last are partial), `Length`, `Eta`, `Success`.

**Tests:** `EditMode/PathfinderMidRoadTests.cs`
- `MidRoad_ToMidRoad_FirstAndLastSegmentsArePartial`
- `SameRoad_DestinationAhead_SingleDirectSegment`
- `SameRoad_DestinationBehind_UTurnNever_LoopsAround`
- `SameRoad_DestinationBehind_UTurnAnywhere_GoesBackDirectly`
- `Heading_Backward_UTurnNever_StartsBackward`
- `NoHeading_EitherDirectionAllowed_PicksShorter`
- `OneWay_WrongWayHeading_StartsInLegalDirection`
- `ArrivedImmediately_WithinArrivalDistance`
- `NoRoadNearStart_FailureReason`, `NoRoadNearDestination_FailureReason`
- `GetTruePoints_NoDuplicatePointsAtJoins`
- `GetTruePoints_PartialRoadsCutAtDistances`
- `GetPoints_WithConverter_ReturnsWorldPositions` (unitsPerMeter 2, shift (100,0,0))

---

## S14 — Zero garbage and performance on a large network

**Goal:** verify the design targets for route requests (design: "Performance targets").

**Depends on:** S13.

**Files:**

1. `Assets/Tests/NavigationSystem/Shared/TestCityGenerator.cs` (new) — `Generate(int approximateRoadCount, int seed)` returns a `RoadNetworkBuildInput`: a jittered grid with ~10% one-way roads, some diagonal roads, all four default road types, curved roads made of 5–15 points. Must produce ~5,000 roads for `approximateRoadCount = 5000`.
2. `Pathfinder` (change only if needed) — make sure `FindRoute` allocates nothing after the first call (reuse everything; `Route` reuses its segment list).

**Tests:** `EditMode/PathfinderPerformanceTests.cs`
- `FindRoute_5000Roads_AllocatesNoGarbageAfterWarmup` — warm up with 3 calls, then `Assert.That(new TestDelegate(Run100Routes), Is.Not.AllocatingGCMemory())` (from `UnityEngine.TestTools.Constraints`; `using Is = UnityEngine.TestTools.Constraints.Is;`).
- `FindRoute_5000Roads_AverageUnder2Milliseconds` — 200 random pairs (seed 7), measure with `System.Diagnostics.Stopwatch`, log the average with `CustomLogger.Log`, assert average < 2 ms (the dev machine is much faster than the 5 ms low-end-phone target).
- `TestCityGenerator_5000_ProducesBetween4500And5500Roads`

**Report** the measured average in your step report.
