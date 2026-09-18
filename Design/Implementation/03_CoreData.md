# Phase 3 — Core Data (S06–S10)

Design references: sections 1, 2, 3, "Cross-cutting" (world scale, asset locations, data format version).

---

## S06 — Navigation Settings asset

**Goal:** the project-wide settings asset: world scale, road types with stable IDs, view channels, format version.

**Depends on:** S01.

**Files:**

1. `Runtime/Core/RoadType.cs` (new, `[Serializable]` class, public)
   - Serialized: `id` (int), `name` (string), `speedMetersPerSecond` (float), `widthMeters` (float), `editorColor` (Color). Public read-only properties for each; internal setters via methods (`SetName`, `SetSpeed`, `SetWidth`, `SetColor`).
2. `Runtime/Core/NavigationSettings.cs` (new, `ScriptableObject`, public)
   - `[CreateAssetMenu]` is **not** used (created by tools).
   - Serialized: `formatVersion` (int, current = 1), `unitsPerMeter` (float, default 1), `roadTypes` (`List<RoadType>`), `nextRoadTypeId` (int), `viewChannelNames` (string[8]), `version` (int, increments on every change that affects bakes: road type speed/width changes, add/remove type).
   - Constants: `CurrentFormatVersion = 1`, `ChannelCount = 8`.
   - Default content (method `ResetToDefaults()`), all values from the design "Default values → Road types": Highway 110 km/h 20 m red, Main 60 km/h 14 m orange, Secondary 50 km/h 10 m yellow, Local 30 km/h 7 m light gray. IDs 1..4, `nextRoadTypeId` = 5. Channel names: `Minimap`, `Full map`, `Custom 1` … `Custom 6`.
   - Methods: `AddRoadType(string name)` → new type with the next ID (speed 50 km/h, width 10 m, white), `FindRoadType(int id)` (returns null if missing), `GetRoadTypeIndex(int id)`, `MoveRoadType(int fromIndex, int toIndex)` (does **not** change IDs, does **not** bump `version`), `RemoveRoadType(int id)` (refuses to remove the last type; returns bool), `SetRoadTypeSpeed/Width(int id, float value)` (bump `version`).
   - `ChannelMask` helpers: `GetChannelName(int index)`.
3. `Runtime/Core/SpeedUnits.cs` (new, plain class) — instance methods `KmhToMetersPerSecond`, `MphToMetersPerSecond`, `MetersPerSecondToKmh`, `MetersPerSecondToMph`.

**Tests:** `EditMode/NavigationSettingsTests.cs` (create with `ScriptableObject.CreateInstance`, destroy in TearDown)
- `ResetToDefaults_CreatesFourTypesWithIds1To4`
- `ResetToDefaults_HighwaySpeedIs110KmhInMetersPerSecond` (≈ 30.556)
- `AddRoadType_UsesNextIdAndIncrementsCounter`
- `RemoveRoadType_LastType_ReturnsFalse`
- `RemoveRoadType_ThenAdd_NeverReusesId`
- `MoveRoadType_KeepsIds_DoesNotBumpVersion`
- `SetRoadTypeSpeed_BumpsVersion`
- `ResetToDefaults_ChannelNamesMatchDesign`
- `SpeedUnitsTests` (separate class): round-trips km/h and mph.

---

## S07 — Coordinate conversions and floating origin shift

**Goal:** the math between World, True and Map spaces (README section 6), and the shift sources.

**Depends on:** S06.

**Files:**

1. `Runtime/Core/WorldConverter.cs` (new, plain class, public)
   - Holds `unitsPerMeter` and the current `shift` (Vector3, world units).
   - `WorldToTrue(Vector3 world)` = `(world − shift) / unitsPerMeter`.
   - `TrueToWorld(Vector3 truePos)` = `truePos * unitsPerMeter + shift`.
   - `WorldDirectionToTrue(Vector3 dir)` = direction unchanged (normalized), `WorldDistanceToTrue(float d)` = `d / unitsPerMeter`.
   - `SetShift(Vector3 shift)`, `SetUnitsPerMeter(float value)` (value must be > 0, otherwise keep old and log error).
2. `Runtime/Core/MapFrame.cs` (new, plain class, public)
   - Built from the rectangle: `center` (True, Vector3; only X/Z used), `size` (Vector2 meters: width along the rectangle's local X, height along local Z), `rotationY` (degrees).
   - Map origin = bottom-left corner; Map +X = rectangle local +X; Map +Y = rectangle local +Z ("map up" = north).
   - `TrueToMap(Vector3 truePos)` → `Vector2`; `MapToTrue(Vector2 mapPos, float y)` → `Vector3`.
   - `TrueDirectionToMap(Vector3 dir)` → normalized `Vector2`; `MapDirectionToTrue(Vector2 dir)`.
   - `MapToNormalized(Vector2 mapPos)` → 0–1 over the image; `ContainsMap(Vector2 mapPos)`.
   - `HeadingToMapAngle(Vector3 trueDir)` → degrees, 0 = map up, clockwise positive.
3. `Runtime/Core/ShiftSource.cs` (new, enum): `Rectangle`, `Manual`.
4. `Runtime/Core/OriginShiftTracker.cs` (new, plain class)
   - Mode `Rectangle`: `UpdateFromRectangle(Vector3 currentWorldPosition, Vector3 storedEditTimeWorldPosition)` → shift = current − stored.
   - Mode `Manual`: `AddManualDelta(Vector3 delta)` accumulates; the total is kept when the map changes (`ResetForNewMap()` resets nothing in Manual mode, and in Rectangle mode just waits for the next update).
   - Property `Shift`.

**Tests:** `EditMode/WorldConverterTests.cs`, `EditMode/MapFrameTests.cs`, `EditMode/OriginShiftTrackerTests.cs`
- `WorldToTrue_WithShiftAndScale_RemovesBoth` (unitsPerMeter 100, shift (1000,0,0): world (1100,0,200) → true (1,0,2))
- `TrueToWorld_IsInverseOfWorldToTrue`
- `SetUnitsPerMeter_Zero_KeepsOldValue`
- `TrueToMap_NoRotation_CornerIsOrigin` (center (50,0,50), size (100,100): true (0,0,0) → map (0,0); (100,0,100) → (100,100))
- `TrueToMap_Rotated90_SwapsAxesCorrectly` (define expected values explicitly in the test)
- `MapToTrue_IsInverseOfTrueToMap_Rotated37Degrees`
- `HeadingToMapAngle_NorthIsZero_EastIs90` (with rotation 0: dir (0,0,1) → 0, (1,0,0) → 90)
- `HeadingToMapAngle_RectangleRotated_IsRelativeToMapUp`
- `Rectangle_ShiftIsCurrentMinusStored`
- `Manual_DeltasAccumulate_AndSurviveResetForNewMap`

---

## S08 — Road network runtime data and builder

**Goal:** the runtime road asset (without the grid yet) and the builder that produces it from simple input. The editor bake (S18) and all tests use this builder.

**Depends on:** S06.

**Files:**

1. `Runtime/Data/RoadRecord.cs` (new, `[Serializable]` struct, internal fields serialized, public read-only properties):
   `Id`, `TypeId`, `OneWay` (bool; allowed direction = from start to end), `StartIntersection`, `EndIntersection` (indices into the intersection array), `FirstPoint`, `PointCount` (into the shared point arrays), `Length` (m), `Speed` (m/s, final: override or type), `Width` (m, final).
2. `Runtime/Data/IntersectionRecord.cs` (new, struct): `Id`, `Position` (True Vector3), `FirstLink`, `LinkCount` (into the link array).
3. `Runtime/Data/RoadNetworkData.cs` (new, `ScriptableObject`, public) — the **Road Network runtime asset**.
   - Serialized arrays: `roads` (RoadRecord[]), `intersections` (IntersectionRecord[]), `links` (int[] — road indices per intersection), `points` (Vector3[] True), `pointDistances` (float[] cumulative distance within each road, starting at 0 for each road).
   - Serialized: `formatVersion`, `settings` (NavigationSettings reference), `sourceVersion` (int: the authoring version it was baked from), `settingsVersion` (int: the settings version it was baked with), `maxSpeed` (m/s over all roads).
   - Read-only accessors: `RoadCount`, `GetRoad(int)`, `IntersectionCount`, `GetIntersection(int)`, `GetLink(int)`, `GetPoint(int)`, `GetPointDistance(int)`, `Settings`, `MaxSpeed`, versions.
   - Internal method `SetData(...)` used by the builder.
   - Helper: `GetOtherEnd(int roadIndex, int intersectionIndex)`; `IsDeadEnd(int intersectionIndex)` (LinkCount == 1).
4. `Runtime/Data/RoadNetworkBuildInput.cs` (new, plain classes): `BuildRoad` (Id, TypeId, OneWay, SpeedOverride (float, ≤ 0 = none), WidthOverride (≤ 0 = none), StartIntersectionId, EndIntersectionId, `List<Vector3> Points` True), `BuildIntersection` (Id, Position), and `RoadNetworkBuildInput` (lists of both).
5. `Runtime/Data/RoadNetworkBuilder.cs` (new, plain class, internal)
   - `Build(RoadNetworkBuildInput input, NavigationSettings settings, RoadNetworkData target)`:
     - Maps IDs to compact indices (in input order).
     - Computes per-road cumulative distances using **X/Z only** (height ignored) and `Length`.
     - Final speed/width = override if > 0, else the road type's value; if the type ID is missing, use the first type and log a warning.
     - Builds links per intersection.
     - Computes `maxSpeed`.
     - Skips a road whose intersection IDs don't exist (log error) — never throws.

**Test helper:** `Assets/Tests/NavigationSystem/Shared/TestNetworks.cs` (new, public class in `Gley.NavigationSystem.Tests`) — instance methods that create small `RoadNetworkBuildInput`s used by many later tests:
- `Line(int roadCount, float roadLength)` — straight chain along +X.
- `Square(float side)` — 4 intersections, 4 two-way roads.
- `Grid(int cellsX, int cellsY, float cell)` — two-way grid.
- `WithOneWay(...)` helpers as needed later (add them in the step that needs them).
- `BuildNetwork(RoadNetworkBuildInput input)` — creates settings (defaults) + data via the builder and returns the data (caller destroys).

**Tests:** `EditMode/RoadNetworkBuilderTests.cs`
- `Build_Line_ComputesLengthsIgnoringHeight` (points with different Y)
- `Build_Line_CumulativeDistancesStartAtZeroPerRoad`
- `Build_Square_EachIntersectionHasTwoLinks`
- `Build_SpeedOverride_WinsOverType`
- `Build_MissingType_FallsBackToFirstType`
- `Build_MissingIntersection_SkipsRoadWithoutThrowing`
- `Build_MaxSpeed_IncludesOverrides`
- `IsDeadEnd_LineEnds_True`

---

## S09 — Road spatial grid

**Goal:** fast "nearest road point" queries (design: grid 50 m, lookups for snapping and when tracking is lost).

**Depends on:** S08.

**Files:**

1. `Runtime/Data/RoadGrid.cs` (new, `[Serializable]` class stored inside `RoadNetworkData`)
   - Serialized: `cellSize` (default 50 m), `origin` (min X/Z), `cellsX`, `cellsZ`, `cellStart` (int[]), `cellCount` (int[]), `entries` (int[] — each entry = a **segment**: global point index `i` meaning segment from point `i` to `i+1` of the same road; plus a parallel `entryRoad` int[] for the road index).
   - Built from all road segments: each segment is added to every cell its X/Z bounding box overlaps.
2. `RoadNetworkBuilder` (change): builds the grid at the end, with `cellSize` from a new build parameter (default 50).
3. `Runtime/Data/RoadPoint.cs` (new, struct, public): `RoadIndex`, `SegmentIndex` (global point index of the segment start), `DistanceAlong` (m from road start), `Position` (True Vector3, Y interpolated), `Distance` (m from the query point, X/Z), `Tangent` (True, normalized, X/Z, pointing from road start to end).
4. `Runtime/Data/RoadQuery.cs` (new, plain class, internal) — constructed with a `RoadNetworkData`. No allocations per query.
   - `FindNearest(Vector3 truePos, float maxDistance, out RoadPoint result)` → bool. Checks all cells overlapping the circle, projects onto each segment (X/Z), keeps the closest. Ties: lower road index wins.
   - `ProjectOnRoad(int roadIndex, Vector3 truePos, out RoadPoint result)` — exact projection onto one road (all its segments).

**Tests:** `EditMode/RoadQueryTests.cs`
- `FindNearest_PointOnRoad_DistanceZero`
- `FindNearest_BeyondMaxDistance_ReturnsFalse`
- `FindNearest_BetweenTwoRoads_PicksCloser`
- `FindNearest_SegmentCrossesManyCells_StillFound` (long diagonal road, query near its middle)
- `FindNearest_DistanceAlongIsCorrect`
- `ProjectOnRoad_ClampsToRoadEnds`
- `Grid_EveryCellEntryReferencesValidSegment`

---

## S10 — Map asset and data format versions

**Goal:** the Map asset (rectangle + image + road network) and the shared format-version field.

**Depends on:** S07, S08.

**Files:**

1. `Runtime/Data/MapImageState.cs` (new enum): `None`, `Captured`, `Custom`, `Outdated`.
2. `Runtime/Data/MapData.cs` (new, `ScriptableObject`, public) — the **Map asset**.
   - Serialized: `formatVersion`, `rectangleCenter` (True Vector3; Y = overlay height), `rectangleSize` (Vector2 m), `rectangleRotationY` (deg), `editTimeWorldPosition` (Vector3 world units, for the Rectangle shift source), `image` (Texture2D), `imageState` (MapImageState), `locked` (bool), `roadNetwork` (RoadNetworkData), `outsideMapColor` (Color, default dark gray until a capture sets it).
   - Properties for all; internal setters (used by editor code).
   - `CreateFrame()` → a new `MapFrame` (only called at map activation, not per frame).
   - `CurrentFormatVersion = 1`.
3. `Runtime/Core/IFormatVersioned.cs` (new interface): `int FormatVersion { get; }`, `int CurrentFormatVersion { get; }`. Implemented by `NavigationSettings`, `RoadNetworkData`, `MapData` (and later `RouteStyle`).

**Tests:** `EditMode/MapDataTests.cs`
- `CreateFrame_UsesRectangleValues`
- `NewAsset_FormatVersionIsCurrent` (for MapData, RoadNetworkData, NavigationSettings)
- `NewAsset_ImageStateIsNone_NotLocked`
