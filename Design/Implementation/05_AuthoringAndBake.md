# Phase 5 — Authoring Data and Bake (S15–S21)

All code in this phase is in the **Editor** assembly (`Gley.NavigationSystem.Editor`, folder `Editor/Authoring/`), except where noted. It is pure logic: no windows, no Scene view code, **no `Undo` calls** (the tools in Phase 6 wrap operations in Undo). This keeps it testable in EditMode.

Design references: section 3 "Road network data model", section 4 "Authoring tool", section 5 "Import adapters", "Cross-cutting" (bake, data format version).

---

## S15 — Authoring asset

**Goal:** the editor-only asset that stores what the user draws.

**Depends on:** S10.

**Files:**

1. `Editor/Authoring/AuthoringKeyPoint.cs` (`[Serializable]` class): `position` (True Vector3), `inHandle`, `outHandle` (Vector3 offsets from `position`), `manualHandles` (bool; false = handles are computed automatically).
2. `Editor/Authoring/AuthoringRoad.cs` (`[Serializable]` class): `id`, `typeId`, `oneWay`, `speedOverride` (≤ 0 = none), `widthOverride` (≤ 0 = none), `sourceTag` (string, empty = hand-drawn), `modifiedAfterImport` (bool), `startIntersectionId`, `endIntersectionId`, `keyPoints` (`List<AuthoringKeyPoint>`, at least 2; first = start intersection position, last = end intersection position), `points` (`List<Vector3>` generated dense shape, True), `groundMissIndices` (`List<int>`).
3. `Editor/Authoring/AuthoringIntersection.cs` (`[Serializable]` class): `id`, `position` (True).
4. `Editor/Authoring/RoadNetworkAuthoring.cs` (`ScriptableObject`) — the **Road Network authoring asset**.
   - Serialized: `formatVersion` (current 1), `runtimeAsset` (RoadNetworkData), `settings` (NavigationSettings), `mapAsset` (MapData, for validation), `gridCellSize` (float, 50), `nextRoadId`, `nextIntersectionId` (start at 1), `version` (int), `roads`, `intersections`.
   - Implements `IFormatVersioned`.
   - Methods: `FindRoad(int id)`, `FindIntersection(int id)`, `GetRoadsAtIntersection(int id, List<AuthoringRoad> output)`, `NewRoadId()`, `NewIntersectionId()` (never reuse), `MarkChanged()` (increments `version`), `SetGridCellSize(float)` (bumps version).
5. `Editor/Authoring/RoadBrush.cs` (`[Serializable]` class): `typeId`, `oneWay`, `speedOverride`, `widthOverride`. Used for new roads.

**Tests:** `EditMode/RoadNetworkAuthoringTests.cs`
- `NewRoadId_NeverReusesAfterRemoval`
- `MarkChanged_IncrementsVersion`
- `GetRoadsAtIntersection_ReturnsConnectedRoads`
- `SetGridCellSize_BumpsVersion`

---

## S16 — Curves, adaptive sampling and ground probe

**Goal:** turn key points + handles into the dense shape points (design: adaptive spacing 10 cm / 20 m; bridge rule +2 m / 5 m).

**Depends on:** S15.

**Files:**

1. `Editor/Authoring/RoadCurve.cs` (plain class)
   - The road between key points `k` and `k+1` is a cubic Bézier: `P0 = k.position`, `P1 = k.position + k.outHandle`, `P2 = (k+1).position + (k+1).inHandle`, `P3 = (k+1).position`.
   - `Evaluate(AuthoringRoad road, int segment, float t)` → True Vector3 (includes Y).
   - `UpdateAutoHandles(AuthoringRoad road)`: for every key point with `manualHandles == false`: direction = `normalize(next − previous)` (first/last point: toward the neighbor), `outHandle = direction * distance(to next) / 3`, `inHandle = −direction * distance(to previous) / 3`. First point has no `inHandle` (zero), last point no `outHandle`.
   - `Split(AuthoringRoad road, int segment, float t, ...)` helper using de Casteljau so a split keeps the exact shape (used by S17).
2. `Editor/Authoring/IGroundProbe.cs` (interface): `bool Probe(Vector3 truePos, out float trueY)`.
3. `Editor/Authoring/PhysicsGroundProbe.cs` — implements the bridge rule: ray from `(x, y + 2 m, z)` straight down, max distance `5 m` (the heights are the curve's own interpolated heights). Converts True → World with `unitsPerMeter` (shift is 0 at edit time) before calling `Physics.Raycast`, and back. Uses a layer mask (the "Road layers" setting) and `QueryTriggerInteraction.Ignore`.
4. `Editor/Authoring/FlatGroundProbe.cs` — returns a fixed Y (tests).
5. `Editor/Authoring/RoadSampler.cs` (plain class)
   - `Sample(AuthoringRoad road, IGroundProbe probe, float maxDeviation, float maxSpacing)` fills `road.points` and `road.groundMissIndices` (clears them first).
   - Per Bézier segment, recursive subdivision over `t`: split `[t0, t1]` when the curve midpoint is farther than `maxDeviation` (X/Z) from the chord midpoint, **or** the chord (X/Z) is longer than `maxSpacing`. Max depth 16. Use an explicit stack (list reused), not recursion.
   - No duplicate points between segments.
   - Each output point: X/Z from the curve; Y from the probe (starting from the curve's Y); on miss, keep the curve Y and add the index to `groundMissIndices`.

**Tests:** `EditMode/RoadCurveTests.cs`, `EditMode/RoadSamplerTests.cs`
- `Evaluate_StraightHandles_IsLinear`
- `UpdateAutoHandles_SkipsManualHandles`
- `Split_KeepsShape_PointsMatchOriginalCurve` (sample 20 points before and after the split, compare)
- `Sample_StraightRoad_OnlyAddsPointsForMaxSpacing` (100 m straight → 6 points with max spacing 20 m)
- `Sample_Curve_DeviationUnderTolerance` (check every chord midpoint vs curve ≤ 0.1 m)
- `Sample_NoDuplicatePointsBetweenSegments`
- `Sample_ProbeMiss_RecordsIndex_KeepsCurveHeight` (fake probe that misses for x > 50)
- PlayMode is not needed. For `PhysicsGroundProbe`: EditMode test `Probe_BelowBridge_HitsRoadNotBridge` — create two BoxColliders (road at y=0, bridge deck at y=6) in a temporary scene, probe at y=0.2 → hits ~0.1 (road top), not 6. Call `Physics.SyncTransforms()` after creating colliders.

---

## S17 — Edit operations

**Goal:** every editing operation from the design table, as testable logic.

**Depends on:** S16.

**Files:** `Editor/Authoring/RoadEditOperations.cs` (plain class). Constructed with `(RoadNetworkAuthoring asset, IGroundProbe probe, float maxDeviation, float maxSpacing)`. **Every operation**: changes the data, re-samples affected roads (`UpdateAutoHandles` + `Sample`), sets `modifiedAfterImport = true` on affected roads that have a `sourceTag`, calls `asset.MarkChanged()`, and returns a result (bool or new ID).

Operations:

| Method | Rules |
|---|---|
| `CreateRoad(List<Vector3> keyPoints, RoadBrush brush, int startIntersectionId, int endIntersectionId)` | IDs = 0 means "create a new intersection at that end". Requires ≥ 2 key points. Returns the new road ID. |
| `ExtendRoad(int roadId, bool atEnd, Vector3 newKeyPoint)` | Only from a dead end (the end intersection has only this road). Moves that end intersection to the new point and inserts the old end as an inner key point. |
| `InsertKeyPoint(int roadId, int segment, float t)` | Inserts at the curve position (exact split of the Bézier). |
| `DeleteKeyPoint(int roadId, int index)` | Only inner key points; road keeps ≥ 2. |
| `MoveKeyPoint(int roadId, int index, Vector3 position)` | Inner key points only (ends move via `MoveIntersection`). |
| `MoveHandle(int roadId, int index, bool isOut, Vector3 offset)` | Sets `manualHandles = true` for that key point. |
| `MoveIntersection(int id, Vector3 position)` | Moves the first/last key point of every connected road. |
| `SplitRoad(int roadId, int segment, float t)` | New intersection at the split. The **first half keeps the old ID**, the second half gets a new ID. Properties (type, one-way, overrides, source tag) copied. Returns the new intersection ID. |
| `MergeAtIntersection(int intersectionId)` | Only when exactly 2 roads meet there, both have equal type/one-way/overrides/source tag, and (for one-way) directions continue (one ends at `X`, the other starts at `X`). The **lower ID** survives; the intersection is removed. Returns bool. |
| `DeleteRoad(int roadId)` | Removes the road; removes intersections that end up with no roads. |
| `ConnectIntersections(int fromId, int intoId)` | All roads of `fromId` now use `intoId` (their end key points move to its position); `fromId` is removed. |
| `ConnectToRoadMiddle(int intersectionId, int roadId, int segment, float t)` | `SplitRoad`, then `ConnectIntersections(intersectionId, newIntersection)`. |
| `Disconnect(int intersectionId)` | Every road except the first gets its own new intersection at the same position. |
| `Flip(int roadId)` | Reverses key points (swapping in/out handles) and swaps start/end intersections. |
| `SetProperties(List<int> roadIds, RoadBrush values, bool setType, bool setOneWay, bool setSpeed, bool setWidth)` | Multi-edit: only the flagged properties change. |

**Tests:** `EditMode/RoadEditOperationsTests.cs` (FlatGroundProbe) — at least one test per operation, including:
- `CreateRoad_NewIntersectionsAtBothEnds`
- `CreateRoad_ReusesGivenIntersection`
- `ExtendRoad_FromConnectedEnd_Fails`
- `DeleteKeyPoint_EndPoint_Fails`, `DeleteKeyPoint_WouldLeaveOnePoint_Fails`
- `MoveIntersection_MovesAllConnectedRoadEnds`
- `SplitRoad_FirstHalfKeepsId_SecondGetsNewId`
- `SplitRoad_ShapeUnchanged`
- `Merge_DifferentTypes_Fails`, `Merge_OneWayOpposite_Fails`, `Merge_LowerIdSurvives`
- `DeleteRoad_RemovesOrphanIntersections`
- `ConnectToRoadMiddle_CreatesTJunction` (3 roads at the new intersection)
- `Disconnect_EachRoadGetsOwnIntersection`
- `Flip_SwapsDirection_AndIntersections`
- `SetProperties_OnlyFlaggedPropertiesChange`
- `AnyOperation_OnImportedRoad_SetsModifiedAfterImport`
- `AnyOperation_BumpsVersion`

---

## S18 — Bake and outdated detection

**Goal:** authoring → runtime asset, and knowing when the bake is outdated.

**Depends on:** S17, S09.

**Files:**

1. `Editor/Authoring/RoadBaker.cs` (plain class)
   - `Bake(RoadNetworkAuthoring authoring)`:
     - Converts to `RoadNetworkBuildInput` (roads with fewer than 2 points are skipped with a warning).
     - Runs `RoadNetworkBuilder.Build` with `authoring.gridCellSize` into `authoring.runtimeAsset` (if null: create it next to the authoring asset as `<Name>_RoadsRuntime.asset` — naming from design "Asset locations"; replace `_RoadsAuthoring` in the file name).
     - Sets on the runtime asset: `sourceVersion = authoring.version`, `settingsVersion = settings.version`, `formatVersion = current`, `settings` reference.
     - `EditorUtility.SetDirty(runtime)` and `AssetDatabase.SaveAssets()`.
2. `Editor/Authoring/BakeStatus.cs` (plain class) — `IsOutdated(RoadNetworkAuthoring authoring)` → true if the runtime asset is missing, or `sourceVersion != authoring.version`, or `settingsVersion != settings.version`, or its format version isn't current.

**Tests:** `EditMode/RoadBakerTests.cs` (assets in the Temp folder, deleted in TearDown)
- `Bake_CreatesRuntimeAssetNextToAuthoring`
- `Bake_RuntimeHasSameRoadCountAndIds`
- `Bake_SetsVersions_NotOutdated`
- `EditAfterBake_IsOutdated`
- `SettingsSpeedChangeAfterBake_IsOutdated`
- `MoveRoadTypeAfterBake_NotOutdated` (reordering types doesn't matter)
- `Bake_SkipsRoadWithOnePoint`

---

## S19 — Validation

**Goal:** the design's cheap and full checks.

**Depends on:** S18.

**Files:**

1. `Editor/Authoring/ValidationIssue.cs`: `Severity` (Warning/Error), `Kind` (enum: `NearMiss`, `Duplicate`, `Island`, `OneWayTrap`, `OutsideMap`, `GroundMiss`), `RoadId`, `IntersectionId`, `Position` (True), `Message`.
2. `Editor/Authoring/RoadValidator.cs` (plain class; all distances in meters, from settings/defaults)
   - `RunCheapChecks(RoadNetworkAuthoring asset, List<ValidationIssue> output)`:
     - **NearMiss:** a dead-end intersection within 2 m (X/Z) of another intersection or of another road's points, not connected to it.
     - **Duplicate:** two roads between the same two intersections (either direction) whose points are all within 1 m of the other road.
   - `RunFullChecks(RoadNetworkAuthoring asset, MapData map, List<ValidationIssue> output)`: cheap checks plus:
     - **Island:** connected components of the undirected intersection graph; every component except the largest is reported (one issue per component, at its first intersection).
     - **OneWayTrap:** strongly connected components of the directed intersection graph (edges: road start→end always, end→start if two-way; U-turns are ignored, i.e. strict). Within the largest undirected component, report every SCC that is not the largest SCC and has **no edge leaving** it (you can enter but not leave) or **no edge entering** it (you can leave but never enter). Implement Tarjan **iteratively** (no recursion).
     - **OutsideMap:** any road point outside the map rectangle (`map` may be null → skip).
     - **GroundMiss:** one warning per road with `groundMissIndices`.
   - Near misses, islands and traps are **Warnings**. Nothing is an Error in v1 (the design allows intentional islands).

**Tests:** `EditMode/RoadValidatorTests.cs`
- `NearMiss_EndsCloseButNotConnected_Reported`, `NearMiss_Connected_NotReported`
- `Duplicate_SameEndpointsSameShape_Reported`, `Duplicate_SameEndpointsDifferentShape_NotReported`
- `Island_TwoComponents_ReportsSmallerOne`
- `OneWayTrap_DeadEndOneWay_Reported`, `OneWayTrap_OneWayLoop_NotReported`
- `OutsideMap_RoadBeyondRectangle_Reported`
- `GroundMiss_Reported`
- `FullChecks_LargeGeneratedNetwork_NoStackOverflow` (use `TestCityGenerator` 5000 converted to authoring data)

**Helper (new):** `Assets/Tests/NavigationSystem/Dev/Editor/AuthoringFromBuildInput.cs` - converts a `RoadNetworkBuildInput` into a `RoadNetworkAuthoring` (each road's points become key points with auto handles; intersections copied; IDs kept; ID counters set past the highest ID). Used by this test and by S29.

---

## S20 — Format versions and migrations

**Goal:** the migration framework (v1 has no real migrations yet).

**Depends on:** S15.

**Files:**

1. `Editor/Setup/IFormatMigration.cs` (interface): `Type AssetType { get; }`, `int FromVersion { get; }`, `void Migrate(UnityEngine.Object asset)` (migrates from `FromVersion` to `FromVersion + 1` and sets the new version on the asset).
2. `Editor/Setup/FormatMigrator.cs` (plain class) — constructed with a list of migrations (empty in v1; tests pass fakes).
   - `MigrateIfNeeded(UnityEngine.Object asset)` → result enum `UpToDate`, `Migrated`, `NewerThanCode`, `MissingStep`:
     - Not `IFormatVersioned` → `UpToDate`.
     - Version > current → `LogError` once ("created by a newer version"), return `NewerThanCode`, **do not modify**.
     - Version < current → apply steps in order; each missing step → `LogError`, stop, return `MissingStep`. After success: `SetDirty`, one-line `Log`.
   - `RoadNetworkData` (runtime asset) is never migrated: older version → it simply reads as "Bake outdated" (S18 already checks the format version).
3. Internal setter for `formatVersion` on the versioned assets (needed by migrations and tests).

**Tests:** `EditMode/FormatMigratorTests.cs`
- `UpToDate_NoChange`
- `Older_AppliesStepsInOrder` (two fake migrations 1→2→3 with current = 3 via a fake versioned asset type defined in the test assembly)
- `MissingStep_ReturnsMissingStep_StopsAtGap`
- `Newer_NotModified_ReturnsNewerThanCode`

---

## S21 — Importer interface and source replacement

**Goal:** the public importer interface and re-import that replaces only one source's roads (the Traffic System mapping itself is **deferred** by design).

**Depends on:** S17.

**Files:**

1. `Editor/Authoring/IRoadImporter.cs` (public interface): `string SourceTag { get; }`, `string DisplayName { get; }`, `bool IsAvailable();`, `void Import(RoadImportContext context);`
2. `Editor/Authoring/RoadImportContext.cs` (public class): wraps `RoadEditOperations`; `AddRoad(List<Vector3> keyPoints, RoadBrush brush)` creates roads tagged with the importer's `SourceTag`; `FindOrCreateIntersection(Vector3 position, float mergeDistance)` so imported roads connect.
3. `Editor/Authoring/RoadImportRunner.cs` (plain class)
   - `CountModifiedImportedRoads(RoadNetworkAuthoring asset, string sourceTag)` → int (the tool uses it for the warning dialog).
   - `Run(IRoadImporter importer, RoadNetworkAuthoring asset)`: deletes all roads with that source tag (and orphan intersections), then calls `importer.Import`. Hand-drawn roads and other sources are untouched. Imported roads start with `modifiedAfterImport = false`.

**Tests:** `EditMode/RoadImportRunnerTests.cs` (a `FakeImporter` in the test assembly that adds 3 roads)
- `Run_AddsRoadsWithSourceTag`
- `Run_Twice_ReplacesOnlyThatSource` (hand-drawn road survives, count stays 3 + hand-drawn)
- `Run_OtherSourceUntouched`
- `CountModifiedImportedRoads_AfterEdit_ReturnsOne`
- `Run_ImportedRoadsConnectThroughSharedIntersection`
