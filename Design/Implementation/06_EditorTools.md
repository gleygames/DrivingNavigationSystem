# Phase 6 — Editor Tools (S22–S29)

Design references: section 2 "Map asset and map scene object", "Map rectangle", section 4 "Authoring tool" (editor home, operations, validation, Scene view drawing), "Cross-cutting" (asset locations, bake), "Default values → Editor".

General rules for this phase:
- UI code calls the Phase 5 logic classes; it never edits authoring data directly.
- **Undo:** call `Undo.RecordObject(authoringAsset, "<Action name>")` **once** before each operation. For drags, record once when the drag **starts** (when `GUIUtility.hotControl` becomes the handle's control), not on every mouse move.
- Tool preferences (draw toggles, road layers, snap distance, brush) are stored in `EditorPrefs` with the prefix `Gley.Navigation.`, through one small class `NavigationEditorPrefs`.
- Every step that has UI also has a pure-logic helper that is unit tested.

---

## S22 — Map scene object, rectangle handles, sync and overlay

**Goal:** the `NavigationMap` scene object and its editor behavior.

**Depends on:** S10, S07.

**Files:**

1. `Runtime/Data/NavigationMap.cs` (runtime MonoBehaviour, public): `[SerializeField] MapData mapData` + property. Nothing else in this step (registration with the Manager comes in S37).
2. `Editor/Tools/MapRectangleSync.cs` (plain class)
   - `SnapObjectToAsset(Transform t, MapData data, float unitsPerMeter)`: position X/Z = `rectangleCenter * unitsPerMeter` (Y left as is), rotation = `(0, rectangleRotationY, 0)`, scale = 1.
   - `ApplyTransformChange(Transform t, MapData data, float unitsPerMeter)` → result enum `NoChange`, `Written`, `Reverted`:
     - Unlocked: write X/Z and rotation Y into the asset; always write Y into `rectangleCenter.y` (overlay height); store `editTimeWorldPosition = t.position`; force scale 1.
     - Locked: revert X/Z and rotation Y to the asset values (keep the object's Y and write it to `rectangleCenter.y`); return `Reverted`.
   - `ResizeFromCorner(MapData data, int corner, Vector2 newCornerMap, bool keepRatio)`: the opposite corner stays fixed; with `keepRatio` (custom image) the size keeps `width/height`.
3. `Editor/Tools/NavigationMapEditor.cs` (`[CustomEditor(typeof(NavigationMap))]`)
   - Inspector: MapData field, rectangle size/rotation (read-only when locked), lock state, "Change area" button (locked + captured image → unlock, set image state `Outdated`, confirm dialog first).
   - `OnSceneGUI`: 4 corner handles (hidden when locked), resizing via `ResizeFromCorner` (keepRatio when image state is `Custom`). Detects Transform changes and calls `ApplyTransformChange`; on `Reverted` shows `SceneView.lastActiveSceneView.ShowNotification("Map area is locked, use Change area")`.
4. `Editor/Tools/MapSceneHooks.cs` — `[InitializeOnLoad]` entry point (static constructor forwarding to an instance) subscribing to `EditorSceneManager.sceneOpened`: snaps every `NavigationMap` in the opened scene to its asset.
5. `Editor/Tools/NavigationEditorPrefs.cs` (new here, extended in S23) - typed get/set over `EditorPrefs` (prefix `Gley.Navigation.`). In this step only: `ShowMapOverlay` (default false), `OverlayAlwaysOnTop` (default true).
6. `Editor/Tools/MapOverlayDrawer.cs` — draws the map image as a semi-transparent textured quad over the rectangle at the object's height, in `SceneView.duringSceneGui`. "Always on top" (default on) = a material with `ZTest Always`; off = normal depth test. Uses one editor material created from a small unlit transparent shader `Editor/Tools/MapOverlay.shader` (editor-only asset). Toggle states come from `NavigationEditorPrefs` (overlay default **off**, always-on-top default **on**).

**Tests:** `EditMode/MapRectangleSyncTests.cs`
- `SnapObjectToAsset_SetsXZRotationScale_KeepsY`
- `ApplyTransformChange_Unlocked_WritesAsset_AndEditTimePosition`
- `ApplyTransformChange_Locked_RevertsXZAndRotation_KeepsY`
- `ApplyTransformChange_ForcesScaleOne`
- `ResizeFromCorner_OppositeCornerFixed`
- `ResizeFromCorner_KeepRatio_PreservesAspect`
- `SnapObjectToAsset_UnitsPerMeter2_ScalesPosition`

**Manual checks:**
1. Add a `NavigationMap` to the sandbox, assign a new MapData asset. Drag corners: the rectangle resizes, the opposite corner stays.
2. Move/rotate the object with Unity's tools: the asset follows.
3. Set `locked` true in the Debug inspector: moving X/Z reverts with the notification; moving Y works.

---

## S23 — Navigation window shell and asset locations

**Goal:** the Gley window with modes, and creation/finding of all assets in the right folders.

**Depends on:** S22, S18.

**Files:**

1. `Editor/Tools/NavigationEditorPrefs.cs` (change, created in S22) — add typed get/set for all remaining tool preferences (draw toggles table from the design, road layers mask, snap distance 3 m, max deviation 10 cm, max spacing 20 m, "Always on top", last mode, brush values).
2. `Editor/Tools/NavigationAssetLocator.cs` (plain class)
   - `GetDefaultMapFolder(string sceneName)` → `Assets/NavigationData/<SceneName>`.
   - `CreateMapAssets(string folder, string name)` → creates `<Name>_Map.asset`, `<Name>_RoadsAuthoring.asset`, `<Name>_RoadsRuntime.asset` (empty runtime asset), links them (authoring → runtime, authoring → map, map → runtime), creates folders if needed.
   - `FindOrCreateSettings()` → searches `t:NavigationSettings`; none → create `Assets/NavigationData/NavigationSettings.asset` with `ResetToDefaults()`; several → `LogWarning` once, use the first (sorted by path).
   - `FindAuthoringFor(RoadNetworkData runtime)` → searches `t:RoadNetworkAuthoring` whose `runtimeAsset == runtime`.
   - Never writes inside `Assets/Gley/DrivingNavigationSystem`.
3. `Editor/Tools/NavigationWindowProperties.cs` (implements `Gley.Common.Editor.ISettingsWindowProperties`) and `Editor/Tools/NavigationVersion.cs` (implements `IVersion`, version "0.1.0").
4. `Editor/Tools/NavigationWindow.cs` (`EditorWindow`, opened with `WindowLoader.LoadWindow<NavigationWindow>`; menu **Tools > Gley > Navigation System > Road Editor**)
   - Target: the selected `NavigationMap`, else the first one in open scenes. Shows "No map in scene — use the Setup window" when none.
   - Toolbar: **Draw / Edit / Connect / Validate / Bake**. Each mode is its own class implementing `IRoadEditorMode` (`OnEnter`, `OnExit`, `OnWindowGUI`, `OnSceneGUI(SceneView)`); modes are filled in by later steps (empty panels now).
   - Subscribes to `SceneView.duringSceneGui` while open; unsubscribes on close.
   - Header shows: map name, road count, bake status (from `BakeStatus`).
   - On open and when the target map changes: `FormatMigrator.MigrateIfNeeded` on the settings asset, the Map asset, the authoring asset and all `t:RouteStyle` assets.

**Tests:** `EditMode/NavigationAssetLocatorTests.cs` (Temp folder instead of `Assets/NavigationData` — make the root folder a constructor parameter)
- `GetDefaultMapFolder_UsesSceneName`
- `CreateMapAssets_CreatesThreeLinkedAssets`
- `FindOrCreateSettings_NoneExists_CreatesWithDefaults`
- `FindOrCreateSettings_Exists_ReturnsExisting`
- `FindAuthoringFor_ReturnsMatchingAsset`

**Manual checks:** open the window from the menu; switching modes works; the header shows the map.

---

## S24 — Scene view road drawing

**Goal:** draw the network fast (design: only inside the view, detail by distance, toggles, type filter).

**Depends on:** S23.

**Files:**

1. `Editor/Tools/RoadDrawPlanner.cs` (plain class)
   - Caches per road: X/Z bounds (as a `Bounds` with a height range) — rebuilt when `authoring.version` changes.
   - `Plan(Plane[] frustum, Vector3 cameraPosition, List<int> visibleRoads, List<bool> detailed)`: visible = bounds intersect frustum (`GeometryUtility.TestPlanesAABB`); detailed = distance from camera to bounds < `detailDistance` (default 150 m × unitsPerMeter).
   - Honors the road-type filter (set of type IDs; empty = all).
2. `Editor/Tools/RoadSceneDrawer.cs` — draws with `Handles`: road lines (`Handles.DrawAAPolyLine` with a reused `Vector3[]` buffer), type colors or one plain color, direction arrows every 20 m on detailed roads (one-way: one direction; two-way: none), key points, intersections (discs), validation highlights (red discs from the last validation result), map rectangle outline. Each element obeys its `NavigationEditorPrefs` toggle (defaults from the design table). Positions converted True → World with `unitsPerMeter`.
3. Window: a "View" foldout with the toggles and the type filter.

**Tests:** `EditMode/RoadDrawPlannerTests.cs`
- `Plan_RoadOutsideFrustum_NotVisible`
- `Plan_NearRoad_Detailed_FarRoad_NotDetailed`
- `Plan_TypeFilter_ExcludesOtherTypes`
- `Plan_BoundsRebuiltWhenVersionChanges`

**Manual checks:** with a few roads, toggles hide/show each element; zooming out removes arrows and points.

---

## S25 — Draw mode

**Difficulty: HARD** (Scene view tool input, raycasts, snapping, Undo). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** drawing roads with raycasts, snapping and extending (design: editing operations table, connecting, bridges).

**Depends on:** S24.

**Files:**

1. `Editor/Tools/SnapFinder.cs` (plain class)
   - `FindSnap(Vector3 truePos, float snapDistance, RoadNetworkAuthoring asset, int ignoreRoadId, out SnapTarget target)`: an **intersection** within the distance wins; otherwise the nearest **road point** within the distance (returns road ID, segment, `t`); otherwise none.
2. `Editor/Tools/DrawMode.cs` (implements `IRoadEditorMode`)
   - `HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive))` so clicks go to the tool.
   - Mouse ray: `HandleUtility.GUIPointToWorldRay` + `Physics.Raycast` with the road layers mask, `QueryTriggerInteraction.Ignore`. Convert hit → True.
   - Click adds a key point. Before each click, `SnapFinder` runs (unless **Shift** is held); the snap target is highlighted (yellow disc) while hovering.
   - The first click on a **dead-end** intersection of an existing road → extend mode (`ExtendRoad` per click).
   - While drawing, a preview of the curve is drawn (sample with the flat probe for speed; the real sample happens on finish).
   - Finish: **Enter** or double-click. Cancel: **Esc**. Right-click removes the last key point.
   - On finish: `Undo.RecordObject`, `CreateRoad` with the brush; connect ends to their snap targets (`ConnectIntersections` / `ConnectToRoadMiddle`), then run cheap validation.
   - Window panel: brush (type dropdown, one-way, speed/width overrides), road layers mask, snap distance.

**Tests:** `EditMode/SnapFinderTests.cs`
- `FindSnap_IntersectionWithinDistance_PrefersIntersection`
- `FindSnap_RoadMiddle_ReturnsSegmentAndT`
- `FindSnap_NothingClose_ReturnsFalse`
- `FindSnap_IgnoresGivenRoad`

**Manual checks (sandbox):**
1. Draw a road along a grid street: it follows the ground; the start snaps to an existing intersection (highlight visible).
2. Hold Shift: no snapping.
3. End on the middle of a road: it becomes a T-junction.
4. Start from a dead end: the road extends.
5. Draw a road under the bridge (click under it from a side view): generated points stay on the lower road.
6. Ctrl+Z undoes a whole road in one step.

---

## S26 — Edit and Connect modes

**Difficulty: HARD** (selection, handles, box select, Undo per drag). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** selecting and changing roads (design: editing operations table).

**Depends on:** S25.

**Files:**

1. `Editor/Tools/RoadSelection.cs` (plain class): selected road IDs, selected key point `(roadId, index)`, selected intersection ID. `Click(SelectionHit hit, bool additive)` (`SelectionHit` = what was clicked: road ID, key point, or intersection ID), `BoxSelect(Rect guiRect, List<int> roadIds, List<Vector2> roadGuiPoints, List<int> roadGuiPointStarts, bool additive)` (the mode fills the GUI-space points of each road; selects roads with any point inside the rect), `Clear()`.
2. `Editor/Tools/EditMode.cs`
   - Click selects a road / key point / intersection (nearest within 10 GUI pixels via `HandleUtility.DistanceToPolyLine` and `HandleUtility.WorldToGUIPoint`). **Ctrl+click** adds. Dragging on empty space draws a box and selects roads with any point inside.
   - Handles: key points and intersections (`Handles.FreeMoveHandle`; the new position is re-raycast to the ground), curve handles of the selected key point. Undo recorded once at drag start.
   - **Double-click** on a road inserts a key point (`InsertKeyPoint` at the nearest curve position).
   - **Delete** key: deletes the selected key point (if inner) or the selected roads.
   - Buttons (window): Split (at the last clicked road position), Merge (selected intersection), Disconnect (selected intersection), Flip (selected roads).
   - Properties panel: multi-edit of type / one-way / speed override / width override with mixed-value display; uses `SetProperties` flags.
3. `Editor/Tools/ConnectMode.cs`: click an intersection, then another intersection or a road → `ConnectIntersections` / `ConnectToRoadMiddle`. Esc cancels.

**Tests:** `EditMode/RoadSelectionTests.cs`
- `Click_Additive_AddsToSelection`, `Click_NotAdditive_Replaces`
- `BoxSelect_SelectsRoadsWithPointInside`
- `Clear_EmptiesEverything`

**Manual checks:** every operation in the design table works in the sandbox, and each is one Undo step.

---

## S27 — Validate and Bake modes, Play-mode warning, build check

**Goal:** validation UI, bake button and the warnings around outdated bakes (design: validation, bake, build check).

**Depends on:** S26, S19, S18.

**Files:**

1. `Editor/Tools/ValidateMode.cs`: "Validate" button → `RunFullChecks`; a list of issues; clicking one frames the Scene view on it (`SceneView.lastActiveSceneView.Frame(bounds)`) and highlights it. Cheap checks run automatically after every edit operation (in Draw/Edit/Connect), and their issues show in the list too.
2. `Editor/Tools/BakeMode.cs`: bake status text, **Bake** button (`RoadBaker`), grid cell size field, and an **Import** foldout:
   - `Editor/Authoring/RoadImporterDiscovery.cs` (plain class): finds all `IRoadImporter` types with `TypeCache.GetTypesDerivedFrom<IRoadImporter>()`, instantiates them, keeps those whose `IsAvailable()` is true.
   - One button per available importer. Before running, if roads with that source tag exist, a confirm dialog: "Re-import replaces N roads from <DisplayName>. M of them were edited by hand and those edits will be lost." (`CountModifiedImportedRoads`). Then `Undo.RecordObject`, `RoadImportRunner.Run`, cheap validation.
   - No importers available (v1 without the Traffic System mapping) -> the foldout shows "No importers available".
3. `Editor/Setup/PlayModeBakeCheck.cs` — `[InitializeOnLoad]` forwarder; on `EditorApplication.playModeStateChanged == ExitingEditMode`: for every `NavigationMap` in open scenes, if its bake is outdated → `CustomLogger.LogWarning("Bake outdated for map X ...")`.
4. `Runtime/Core/NavigationSettings.cs` (change): add serialized `blockBuildOnProblems` (bool, default false).
5. `Editor/Setup/BuildCheck.cs`
   - Plain logic `BuildCheckEvaluator.Evaluate(List<RoadNetworkAuthoring> assets, NavigationSettings settings, List<string> messages)` → `bool shouldBlock`: collects outdated bakes and full-validation issues (counts per map).
   - `NavigationBuildPreprocessor : IPreprocessBuildWithReport` (callbackOrder 0): finds all authoring assets, evaluates, logs one summary warning; if `shouldBlock` → `throw new BuildFailedException(summary)`.

**Tests:** `EditMode/BuildCheckEvaluatorTests.cs`
- `Evaluate_AllGood_NoMessages_NotBlock`
- `Evaluate_OutdatedBake_Message_NotBlockByDefault`
- `Evaluate_Problems_BlockSettingOn_Blocks`
- `Evaluate_ValidationIssues_CountedPerMap`
- `EditMode/RoadImporterDiscoveryTests.cs`: `Discover_FindsAvailableFakeImporter`, `Discover_SkipsUnavailableImporter` (two fake importers in the test assembly)

**Manual checks:** validate lists near misses and islands in the sandbox; clicking frames them; Bake clears "Bake outdated"; editing then pressing Play shows the warning.

---

## S28 — Project settings panel (road types, channels, scale, units)

**Goal:** editing the project-wide settings, including deleting a road type in use (design: deleting a road type in use).

**Depends on:** S23.

**Files:**

1. `Runtime/Core/NavigationSettings.cs` (change): add serialized `imperialUnits` (bool, default false) — the project's display unit for the editor and the default formatter.
2. `Editor/Tools/RoadTypeDeletion.cs` (plain class)
   - `CountUsage(int typeId, List<RoadNetworkAuthoring> assets)` → int (across all maps).
   - `Reassign(int fromTypeId, int toTypeId, List<RoadNetworkAuthoring> assets)`: changes the type on every road using it, marks each asset changed (so bakes become outdated).
3. `Editor/Tools/SettingsPanel.cs` (a foldout in the Navigation window):
   - Road types list: name, speed (shown/entered in km/h or mph per `imperialUnits`, stored m/s), width, color; Add; reorder (up/down buttons → `MoveRoadType`); Delete → if used: dialog "N roads in M maps use X. Move them to: [dropdown]" → `Reassign` then `RemoveRoadType`; the last type has no delete button.
   - View channel names (8 text fields).
   - Units per meter (with a warning box: "Change this before authoring maps; existing maps won't match").
   - Imperial units toggle. Block build on problems toggle.
   - All changes use `Undo.RecordObject(settings, ...)`.

**Tests:** `EditMode/RoadTypeDeletionTests.cs`
- `CountUsage_AcrossTwoAssets`
- `Reassign_ChangesAllRoads_AndMarksAssetsChanged`
- `Reassign_OtherTypesUntouched`

---

## S29 — Large network editor check (Undo and redraw)

**Goal:** measure editor performance against the targets (edit ≤ 50 ms, Scene view redraw ≤ 16 ms) and decide about chunking (design: technical risk "Undo on large road assets").

**Depends on:** S27.

**Files:**

1. `Assets/Tests/NavigationSystem/Dev/Editor/LargeNetworkGenerator.cs` — menu **Tools > Gley > Navigation Dev > Generate 5k Road Map**: creates map assets in `Assets/NavigationData/Dev5k/` and fills the authoring asset from `TestCityGenerator.Generate(5000, 1)` (roads as key points = their generated points with auto handles; use the flat probe), then bakes. Adds a `NavigationMap` for it to the open scene.
2. Navigation window (change): a small "Diagnostics" line showing the duration of the **last operation** (Stopwatch around each operation including re-sample and cheap validation) and the **last Scene view draw** duration.

**Tests:** none new (manual measurement step).

**Manual checks:**
1. Generate the 5k map. Move a key point, split a road, undo, redo. Note the operation times and Undo delay.
2. Orbit the Scene view zoomed out and zoomed in; note the draw time.

**Report** the numbers. **Stop and ask the user** whether chunking is needed (design fallback: separate chunk files by area). Do not implement chunking in this step.
