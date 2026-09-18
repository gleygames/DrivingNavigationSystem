# Phase 12 — Setup and Finish (S53–S56)

Design references: section 14 "Setup window", "Cross-cutting" (default art, text, safe area, Enter Play Mode Options, logging, performance targets), "Technical risks" (assemblies).

---

## S53 — Default prefabs, placeholder art, safe area, default assets

**Goal:** ready-to-use UI prefabs and assets, built by scripts (no hand-written prefab YAML).

**Depends on:** S52.

**Files:**

0. `Assets/Tests/NavigationSystem/Dev/Editor/Gley.NavigationSystem.Dev.Editor.asmdef` (change): add references `Gley.NavigationSystem.TMP` and `Unity.TextMeshPro`.
1. `Runtime/UI/SafeAreaFitter.cs` (MonoBehaviour): fits its RectTransform anchors to `Screen.safeArea` in `OnEnable` and `OnRectTransformDimensionsChange` (event-based, no per-frame checks). Does nothing on World Space canvases.
2. `Assets/Tests/NavigationSystem/Dev/Editor/PlaceholderArtGenerator.cs` — menu **Tools > Gley > Navigation Dev > Generate Placeholder Art**. Draws simple white shapes with a dark outline into PNGs (CPU drawing into `Texture2D`) and saves them in `Assets/Gley/DrivingNavigationSystem/Art/` with these **final file names** (final art will replace them with the same names): `PlayerArrow.png`, `DestinationPin.png`, `PreviewPin.png` (hollow), `OffScreenArrow.png`, `MinimapMask.png` (filled circle), `MinimapFrame.png` (ring), `Compass.png` (arrow; the "N" is a TMP text in the prefab), `Crosshair.png`, `ButtonBackground.png` and `PanelBackground.png` (rounded rectangles, 9-slice borders set), `DefaultMarker.png` (circle). Import all as `Sprite (2D and UI)`.
3. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` — menu **Tools > Gley > Navigation Dev > Build Default Prefabs**. Builds GameObjects in code and saves with `PrefabUtility.SaveAsPrefabAsset` into `Assets/Gley/DrivingNavigationSystem/Prefabs/`:
   - Assets: `MinimapRouteStyle.asset`, `FullMapRouteStyle.asset` (design widths 6 / 8 canvas units, `lineShader` = the RouteLine shader), `DefaultFormatter.asset` (unit system ProjectSetting).
   - Marker prefabs: `PlayerMarker`, `DestinationMarker`, `PreviewPin`, `DefaultMarker`, `OffScreenArrow` (with a `TmpTextTarget` distance label).
   - `NavigationMinimap.prefab`: root with `SafeAreaFitter` (full-screen stretch), a 300×300 round viewport anchored bottom-left with `MapView` (channel Minimap, `showPreview` false, edge Circle), `MapViewFollowCar` (round true), `MinimapShape` (Sprite, `MinimapMask`), `MinimapFrame` image, `CompassButton`, `MinimapTapToOpen`.
   - `NavigationFullMap.prefab` (inactive by default): root with `SafeAreaFitter`, full-screen viewport with `MapView` (channel Full map, `showPreview` true, edge Rectangle), `MapViewInteractive`, `PointerInputAdapter`, crosshair image, `PreviewPanel` (distance + ETA TMP texts, Confirm, Cancel), `NavigationControls` (Stop, Center on car, Close).
   - All TMP texts use `TmpTextTarget`.
4. The builder assigns the default prefab slots/styles on the prefabs' components (the Manager's marker prefab slots are assigned by the Setup window in S54).

**Tests:** `EditMode/DefaultPrefabsTests.cs` (load with `AssetDatabase.LoadAssetAtPath`)
- `MinimapPrefab_HasRequiredComponents` (MapView, MapViewFollowCar, MinimapShape, CompassButton, MinimapTapToOpen, SafeAreaFitter)
- `FullMapPrefab_HasRequiredComponents_AndIsInactive`
- `MarkerPrefabs_Exist`
- `RouteStyles_WidthsMatchDesign`
- `RouteStyles_ReferenceRouteLineShader`
- `ArtSprites_ExistWithFinalNames`
- `PlayMode/SafeAreaFitterTests.cs`: `WorldSpaceCanvas_NotChanged`

**Manual checks:** run both menus; open the prefabs; drop both in the sandbox canvas manually with a Manager: the minimap follows the car; tapping it opens the full map.

---

## S54 — Setup window

**Goal:** the 5-step setup window (design section 14 "Setup window" + related decisions).

**Depends on:** S53, S32, S27.

**Files:**

1. `Editor/Setup/SetupStatus.cs` (enum `Done`, `Missing`, `Warning`) and `Editor/Setup/SetupStatusEvaluator.cs` (plain class, **all decisions here, tested**):
   - Step 1 Map area: map object + asset exist → Done; else Missing.
   - Step 2 Map image: `None` → Missing; `Outdated` → Warning; image import warnings → Warning; else Done.
   - Step 3 Roads: no roads → Missing; bake outdated or validation issues → Warning; else Done.
   - Step 4 UI: minimap + full map in the scene → Done; EventSystem module mismatch or TMP missing → Warning.
   - Step 5 Car: car assigned → Done.
   - `ChooseInputModule(bool newInputSystemEnabled, bool inputSystemPackagePresent)` → `InputSystemUI` or `Standalone` (design rule).
   - `IsTrafficSystemPresent(List<string> assemblyNames)` → any name starting with `Gley.TrafficSystem`.
   - `BlurFactor(float viewportWidthCanvas, float minZoomMeters, float metersPerPixel)` = how many canvas units one image pixel covers at max zoom-in: `(viewportWidthCanvas / minZoomMeters) * metersPerPixel`. `IsBlurry(float factor)` = `factor > 2`.
2. `Editor/Setup/NavigationSetupWindow.cs` (Gley window via `WindowLoader`; menu **Tools > Gley > Navigation System > Setup**)
   - On open: `FormatMigrator.MigrateIfNeeded` on the settings and all map, authoring and `RouteStyle` assets found; Traffic System detection → `PreprocessorDirective.AddToCurrent("GLEY_TRAFFIC_SYSTEM", !present)` (from `Gley.Common.Editor`; assembly names from `CompilationPipeline.GetAssemblies()`).
   - **Step 1:** "Create map" → folder field (default `NavigationAssetLocator.GetDefaultMapFolder`), name → `CreateMapAssets`, a `NavigationMap` GameObject, and a `NavigationManager` GameObject if none (settings from `FindOrCreateSettings`, `DefaultFormatter`, marker prefab slots). Initial rectangle = the X/Z bounds of all scene renderers (the user adjusts it).
   - **Step 2:** embeds `MapImagePanel` (capture / custom image / guidance / template). Below it, the **blur info** (design: "optional warning when the image gets blurry"): for each `MapView` in the scene (or, before step 4, the default minimap 300 and full map 1920 canvas-unit widths with min zoom 50 m): "At max zoom-in, 1 image pixel = N screen pixels", with a warning icon when `IsBlurry`. This line does not change the step's status.
   - **Step 3:** road count, bake status, validation summary; buttons "Open Road Editor", "Validate", "Bake".
   - **Step 4:** Canvas picker or "Create Canvas" (Screen Space Overlay, scale with screen size 1920×1080, match 0.5); EventSystem: create with the chosen module if none (`InputSystemUIInputModule` added by type name `UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem` via `System.Type.GetType`; use `#if ENABLE_INPUT_SYSTEM` / `ENABLE_LEGACY_INPUT_MANAGER` to know what is enabled); an existing EventSystem is never modified (warning on mismatch). "Add minimap and full map" → instantiates both prefabs under the canvas, links `MinimapTapToOpen` to the full map. If the Input System package is present, adds `GamepadInputAdapter` to the full map (by type name). TMP missing → message.
   - **Step 5:** car object field, yaw offset field with quick buttons 0 / 90 / 180 / 270, and an **arrow gizmo** in the Scene view (while the window is open) showing the nose direction that will be used.
   - Each step shows its status icon + message; everything created is normal components and prefabs.

**Tests:** `EditMode/SetupStatusEvaluatorTests.cs` — one test per status rule, plus `ChooseInputModule_*` (4 combinations), `IsTrafficSystemPresent_*` (present / absent), `BlurFactor_Computes`, `IsBlurry_Above2`.

**Manual checks (fresh copy of the sandbox scene):** go through steps 1–5 from nothing to a working minimap + full map, drive, tap a destination, confirm, arrive.

---

## S55 — Enter Play Mode Options audit

**Goal:** everything works with domain reload off (design requirement).

**Depends on:** S54.

**Files:**

1. `EditMode/StaticStateTests.cs`: reflection over the types of the `Gley.NavigationSystem`, `Gley.NavigationSystem.TMP` assemblies: **no static fields** that are not `const` or `readonly` (report every offender in the failure message). Editor assembly: same check, except fields explicitly listed in an allow-list inside the test (each entry must have a reset in an `[InitializeOnEnterPlayMode]` method).
2. Fix any offenders found.

**Tests:** `StaticStateTests` (2).

**Manual checks:**
1. Project Settings > Editor > Enter Play Mode Options: enabled, Reload Domain **off**, Reload Scene **off**.
2. In the sandbox: Play, drive, navigate, stop. Play again: no duplicate events (e.g. `Arrived` logged once), no errors, markers not doubled.
3. Run all PlayMode tests with these settings.

---

## S56 — Performance pass

**Goal:** measure against the design's performance targets on the 5,000-road city.

**Depends on:** S55, S29.

**Files:**

1. Profiler markers: add `Unity.Profiling.ProfilerMarker` **instance** fields and `Begin/End` (or `Auto()`) around: Manager logic, tracking, marker registry update, each view's visuals, marker layer, route line updates. Names start with `Gley.Nav.`.
2. `Assets/Tests/NavigationSystem/Dev/DevAutoDriver.cs`: drives the car along the active route's points at 15 m/s (no physics).
3. `Assets/Tests/NavigationSystem/Dev/DevPerfOverlay.cs`: `OnGUI` showing, via `ProfilerRecorder`: "GC Allocated In Frame", the `Gley.Nav.*` markers' time, and draw call count (`ProfilerRecorder` "Draw Calls Count"). 
4. `Assets/Tests/NavigationSystem/Dev/Editor/PerfSceneBuilder.cs`: menu **Tools > Gley > Navigation Dev > Create Perf Scene**: uses the 5k map from S29, adds the Manager, both prefabs, 100 static + 20 moving markers, the car with `DevAutoDriver`, the overlay, and starts navigation to a far destination on Play.

**Tests:** none new.

**Manual checks:** in the perf scene (Play, full map closed, then open), fill in this table in the report:

| Target | Goal | Measured |
|---|---|---|
| CPU per frame, driving (Manager + minimap) | ≤ 0.5 ms (phone) | |
| CPU per frame, full map + 100 markers | ≤ 1 ms (phone) | |
| GC per frame, steady state | 0 B | |
| Draw calls minimap / full map | ≤ 6 / ≤ 10 | |
| Route request (from S14) | ≤ 5 ms (phone) | |

The dev PC is much faster than a low-end phone; report the numbers as measured. **Do not optimize anything in this step** — list what exceeds its goal and ask the user how to proceed.
