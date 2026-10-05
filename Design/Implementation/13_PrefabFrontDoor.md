# Phase 13 — Prefab Front Door (S58–S63)

Design references: section 14 "Prefab front door" (decision 2026-10-05) and "Cross-cutting" → "One set of default assets", "Text".

Added after S57; not in the original plan. The package is **not released**, so nothing here needs migration code: old components are deleted, prefabs are rebuilt by the builder.

---

## Shared rules for this phase (read before any step)

### Where we are going (state after S63)

Each UI prefab has **exactly one Gley MonoBehaviour**, on its root. Everything else is a plain C# class created and run by that root.

```
NavigationMinimap (root)  [NavigationMinimap]
  Viewport                [Image + Mask, or RectMask2D]   ← driven by the root, no Gley script
  MinimapFrame            [Image]
  CompassButton           [Button]
    Icon                  [Image]
      N                   [TMP text]

NavigationFullMap (root, inactive = closed)  [NavigationFullMap]
  Viewport                [no components except RectTransform]
    Crosshair             [Image]
    NavigationControlsUi
      CloseButton / CenterButton / StopButton   [Image + Button]
    PreviewPanel          [Image]
      DistanceText / EtaText                    [TMP text, no adapter]
      ConfirmButton / CancelButton              [Image + Button]
```

`MapView` still creates its own runtime children under `Viewport` (`Background`, `Content`, `Markers` with `MarkerLayer` and `RouteLineRenderer` components). Those are created at runtime, never saved in the prefab, and stay as they are.

| Root | Runs these plain classes, in this order every `LateUpdate` |
|---|---|
| `NavigationMinimap` | `MapViewFollowCar` → `MapView` → `MinimapCompass` (+ `MinimapShape` on enable / on validate) |
| `NavigationFullMap` | `MapViewInteractive` → `MapView` → `NavigationControls` (+ `PointerInputAdapter` in `Update`, `PreviewPanel` is event driven) |

### Exact serialized field names (the builder, the inspectors and the tests use these strings)

`NavigationMinimap`: `manager`, `viewport`, `viewSettings`, `followSettings`, `shapeSettings`, `tapAction`, `fullMap`, `compassButton`, `compassIcon`.

`NavigationFullMap`: `manager`, `viewport`, `viewSettings`, `interactionSettings`, `crosshairImage`, `previewPanel`, `buttons`.

| Settings class | Fields (default) |
|---|---|
| `MapViewSettings` | `routeStyle` (null), `arrowPrefab` (null), `textWriter` (null, added in S62), `minZoomMeters` (50), `edgeInset` (8), `channelMask` (set by constructor), `showPreview` (set by constructor), `showOffScreenArrows` (true), `showArrowDistance` (true) |
| `MinimapFollowSettings` | `rotationMode` (HeadingUp), `carOffsetFromBottom` (0.3), `rotationSmoothing` (0.25), `turnSmoothing` (0.8), `turnAngleThreshold` (20), `noseDeadZoneDegrees` (3), `speedZoom` (true), `speedZoomMinMeters` (150), `speedZoomMaxMeters` (500), `speedZoomSlowSpeed` (5.5556), `speedZoomFastSpeed` (27.7778), `zoomSmoothing` (0.5), `fixedZoomMeters` (300) |
| `MinimapShapeSettings` | `shapeKind` (Sprite), `sprite` (null), `spriteOutline` (EdgeShape.Circle) |
| `FullMapInteractionSettings` | `zoomOutMode` (Fit), `crosshairMode` (Auto), `openZoomMeters` (1000), `mouseWheelStep` (1.25), `doubleTapStep` (2), `markerTapRadius` (40), `confirmStep` (true), `builtInPointerInput` (true), `fling` (true), `doubleTapZoom` (true) |
| `PreviewPanelSlots` | `panelRoot` (GameObject), `distanceText`, `etaText`, `confirmButton` (Button), `cancelButton` (Button) |
| `FullMapButtons` | `stopButton`, `centerButton`, `closeButton` (all Button) |

### How every settings class is written

- `[System.Serializable] public class Xxx` (not a MonoBehaviour, not a ScriptableObject). One class per file, file name = class name.
- Every field is `[SerializeField] private` with the default from the table as a field initializer.
- Every field has a **public get-only property** with the field name in PascalCase (`speedZoomMinMeters` → `SpeedZoomMinMeters`), and an **internal setter method** `SetSpeedZoomMinMeters(float value)`. Tests, the builder and the roots use the setters.
- The root declares it with a field initializer, e.g. `[SerializeField] private MinimapFollowSettings followSettings = new MinimapFollowSettings();`, so it exists even for a component added from code.

### How every plain class is written

- `internal class` (only `MapView` and `MapViewInteractive` are `public`, because users reach them through `minimap.View` and `fullMap.Interactive`).
- No Unity messages (`OnEnable`, `LateUpdate`, ...). The root calls `Enable(...)`, `Disable()` and `Update<Domain><Phase>(deltaTime)` methods instead.
- Everything it needs comes in through the **constructor** (references) or through `Enable(...)` (the Manager).
- **Never copy a setting into a field.** Always read it from the settings object at the moment you need it (`settings.ZoomSmoothing`), so a change made in the inspector at runtime works right away.
- Unity statics such as `Object.Destroy`, `Object.FindAnyObjectByType` and `Time.unscaledTime` are fine to call. Our own code stays non-static.

### Other rules

- **Deleting a file:** delete both `X.cs` and `X.cs.meta`. (Deleting a `.meta` is allowed; creating or editing one is not.)
- **Prefabs are only made by the builder** (`Tools > Gley > Navigation Dev > Build Default Prefabs`). Never edit prefab YAML. From S58 on, the builder writes straight into the shipped folder `Assets/Gley/DrivingNavigationSystem/Graphics/`.
- The EditMode test `DefaultPrefabsTests` rebuilds the prefabs in its `[OneTimeSetUp]`. That is intended.
- `Runtime.InputSystem/GamepadInputAdapter.cs` is **not compiled in this project** (the Input System package is not installed). When a step changes it, re-read your change twice: Unity will not tell you about mistakes there.
- Tests that enable a map without a Navigation Manager must `LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"))` **once per error that will be logged**, before the object is enabled. Each step below says how many errors to expect.
- Test helpers (`MinimapTestRig`, `FullMapTestRig`, `TestMapViewHost`) live in `Assets/Tests/NavigationSystem/PlayMode/` (assembly `Gley.NavigationSystem.Tests.Runtime`, which can see `internal` runtime members).

---

## S58 — One set of default assets in `Graphics/`

**Goal:** the builder, the art generator, the dev scene builders and the tests all use `Assets/Gley/DrivingNavigationSystem/Graphics/` (the folder the setup window already uses); the duplicate `Prefabs/` and `Art/` folders are deleted. Design: "One set of default assets".

**Depends on:** S57.

**Background:** today `DefaultPrefabBuilder` writes `Prefabs/` + `Art/`, while the setup window gives users `Graphics/Prefabs`, `Graphics/Textures`, `Graphics/Presets`. They are separate copies with different GUIDs. The `.png` files in both folders are byte-identical.

**Files:**

1. `Assets/Tests/NavigationSystem/Dev/Editor/DevUiInstaller.cs` (new, namespace `Gley.NavigationSystem.Dev`, plain class)
   - Constants (public `const string`):
     - `GraphicsFolder = "Assets/Gley/DrivingNavigationSystem/Graphics"`
     - `PrefabFolder = GraphicsFolder + "/Prefabs"`
     - `TextureFolder = GraphicsFolder + "/Textures"`
     - `PresetFolder = GraphicsFolder + "/Presets"`
   - `public GameObject Minimap { get; private set; }` and `public GameObject FullMap { get; private set; }`: the two instances made by `InstallDefaultUi`.
   - `public GameObject LoadPrefab(string prefabName)` → `AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab")`.
   - `public void AssignDefaultManagerAssets(NavigationManager manager)`: through a `SerializedObject` on the manager, set `formatter` = `DefaultNavigationFormatter` at `PresetFolder + "/DefaultFormatter.asset"`, and `playerMarkerPrefab`, `destinationMarkerPrefab`, `previewPinPrefab` = `LoadPrefab("PlayerMarker")`, `LoadPrefab("DestinationMarker")`, `LoadPrefab("PreviewPin")`. Then `ApplyModifiedPropertiesWithoutUndo()`.
   - `public bool InstallDefaultUi(Transform canvas)`:
     - Load `NavigationMinimap` and `NavigationFullMap`. If either is null: `CustomLogger.LogError("DevUiInstaller: default prefabs are missing. Run Tools > Gley > Navigation Dev > Build Default Prefabs.")` and return false.
     - `Minimap = (GameObject)PrefabUtility.InstantiatePrefab(minimapPrefab, canvas)`, same for `FullMap`.
     - Link the minimap tap to the full map exactly as `PerfSceneBuilder.CreateUi` does today (`MinimapTapToOpen` found with `GetComponentInChildren<MinimapTapToOpen>(true)`, `MapViewInteractive` with `GetComponentInChildren<MapViewInteractive>(true)`, then `tapToOpen.SetFullMap(interactive)` when both are found). S59 and S60 change only this linking code.
     - Return true.
2. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change)
   - Delete its `PrefabFolder` and `ArtFolder` constants. Prefabs go to `DevUiInstaller.PrefabFolder`; sprites load from `DevUiInstaller.TextureFolder`; `CreateRouteStyle` and `CreateDefaultFormatter` use `DevUiInstaller.PresetFolder`.
   - `BuildDefaultPrefabs` calls `EnsureFolderExists` for the prefab folder **and** the preset folder.
   - Nothing else changes in this step.
3. `Assets/Tests/NavigationSystem/Dev/Editor/PlaceholderArtGenerator.cs` (change)
   - `ArtFolder` → delete the constant; use `DevUiInstaller.TextureFolder`.
   - In `GenerateSprite`, right after building the path: `if (File.Exists(path)) { return; }`, **before** anything is drawn or written. Reason: final art will use the same file names; the generator (and the EditMode test that calls it) must never overwrite an existing sprite.
4. `Assets/Tests/NavigationSystem/Dev/Editor/FullSandboxBuilder.cs` (change)
   - The sandbox now uses the real default prefabs instead of building its own UI in code.
   - `CreateFullSandbox`: after `CreateManager(...)`:
     ```
     DevUiInstaller installer = new DevUiInstaller();
     installer.AssignDefaultManagerAssets(manager);
     GameObject canvasObject = CreateCanvasWithEventSystem();
     installer.InstallDefaultUi(canvasObject.transform);
     MapViewFollowCar minimap = null;
     if (installer.Minimap != null) { minimap = installer.Minimap.GetComponentInChildren<MapViewFollowCar>(true); }
     ```
     then `tester.Configure(manager, minimap)` as today.
   - Delete `CreateUi`, `CreateFullMap`, `CreatePreviewPanel`, `CreateNavigationControls`, `CreateText`, `CreateTextTarget`, `CreateButton` and the constants `MinimapSizeCanvasUnits` / `MinimapMarginCanvasUnits` if nothing else uses them (search the Dev folders first; keep a constant if another file uses it). Keep `CreateOrReuseMap`, `CaptureMapImage`, `CreateManager`, `CreateCanvasWithEventSystem` (other builders call them). Remove `using` lines that become unused.
5. `Assets/Tests/NavigationSystem/Dev/DevMinimapCarMarker.cs` (delete, with its `.meta`): it was only used by the old code-built sandbox minimap; the default prefabs show the real player marker.
6. `Assets/Tests/NavigationSystem/Dev/Editor/PerfSceneBuilder.cs` (change)
   - Delete its `PrefabFolder` constant and `LoadPrefab`; use a `DevUiInstaller` instance for `LoadPrefab` (markers) and `AssignDefaultManagerAssets` (replace the four manual `FindProperty` lines for formatter and marker prefabs in `CreateManager`).
   - `CreateUi`: keep creating the canvas and EventSystem, then `installer.InstallDefaultUi(canvasObject.transform)`; `minimapRoot = installer.Minimap`; return `installer.FullMap.GetComponentInChildren<MapViewInteractive>(true)` (null-check `installer.FullMap`).
7. `Assets/Tests/NavigationSystem/Dev/Editor/RuntimeUiSceneBuilder.cs` (change): delete `PrefabFolder` and `AssignMarkerPrefabs`; use `new DevUiInstaller().AssignDefaultManagerAssets(manager)`; select `installer.LoadPrefab("NavigationMinimap")`.
8. `Assets/Tests/NavigationSystem/EditMode/DefaultPrefabsTests.cs` (change)
   - `PrefabFolder` → use `DevUiInstaller.PrefabFolder`; `ArtFolder` → `DevUiInstaller.TextureFolder`; route styles load from `DevUiInstaller.PresetFolder`. Delete the local constants.
   - Add `[Test] DefaultFormatter_ExistsInPresets` → asset at `DevUiInstaller.PresetFolder + "/DefaultFormatter.asset"` is not null.
9. Delete the folders `Assets/Gley/DrivingNavigationSystem/Prefabs/` and `Assets/Gley/DrivingNavigationSystem/Art/` **with all their contents and their folder `.meta` files** (`Prefabs.meta`, `Art.meta`). Before deleting, search all `.cs` files for `DrivingNavigationSystem/Prefabs` and `DrivingNavigationSystem/Art`; after your changes there must be no hit.
10. `Design/Implementation/00_README.md` (change): in section 3's package layout, replace the lines `Prefabs/` and `Art/             placeholder sprites` with:
    ```
      Graphics/
        Prefabs/       default UI and marker prefabs (built by DefaultPrefabBuilder)
        Textures/      default sprites (placeholder until final art, same file names)
        Presets/       route styles, default formatter, text writer
    ```

**Tests:**
- EditMode `DefaultPrefabsTests` (all existing tests + `DefaultFormatter_ExistsInPresets`), now reading `Graphics/`.
- Everything else must stay green (run all EditMode and PlayMode tests).

**Manual checks:**
1. `Tools > Gley > Navigation Dev > Build Default Prefabs`: no errors; `Graphics/Prefabs`, `Graphics/Presets` are updated; no `Prefabs/` or `Art/` folder appears again.
2. `Tools > Gley > Navigation Dev > Generate Placeholder Art`: no file in `Graphics/Textures` changes (check git status: no modified `.png`).
3. `Tools > Gley > Navigation Dev > Create Full Sandbox (everything)`, Play: the minimap (bottom-left) follows the car with the default player arrow; tap it → full map opens; tap a destination, Confirm → route on both maps; Close works.
4. `Tools > Gley > Navigation Dev > Create Perf Scene`: builds without errors.
5. `RuntimeUiTest.unity` referenced the deleted `Prefabs/` copies: rebuild it with `Tools > Gley > Navigation Dev > Create Runtime UI Test Scene`.

**Done when:** no code or scene points at `Prefabs/` or `Art/`, and the sandbox runs on the shipped prefabs.

---

## S59 — Minimap root: `NavigationMinimap`

**Difficulty: HARD** (many files change together; Unity component life cycle; edit-time shape application).

**Goal:** the minimap prefab is driven by one root component, `NavigationMinimap`, which owns all minimap settings and runs Follow Car, Shape, Compass and the tap as plain classes. Design: "Prefab front door".

**Depends on:** S58.

**Interim state (until S61):** `MapView` **stays a MonoBehaviour on the `Viewport` child** in this step. The root finds it with `viewport.GetComponent<MapView>()` and runs **before** it (`[DefaultExecutionOrder(99)]`, `MapView` is 100), exactly as `MapViewFollowCar` did. The full map is untouched in this step (`MapViewInteractive` is still a component). S61 turns `MapView` into a plain class.

**Files:**

1. `Runtime/UI/MinimapFollowSettings.cs` (new, public, see "How every settings class is written")
   - Fields, defaults, properties and setters from the table in the shared rules.
   - Plus `internal void ToggleRotationMode()`: HeadingUp ↔ NorthUp.
2. `Runtime/UI/MinimapShapeSettings.cs` (new, public)
   - Fields `shapeKind`, `sprite`, `spriteOutline` with properties `ShapeKind`, `Sprite`, `SpriteOutline` and setters `SetShapeKind`, `SetSprite`, `SetSpriteOutline`.
   - Computed property `public EdgeShape Outline`: `EdgeShape.Rectangle` when `shapeKind == MinimapShapeKind.Rectangle`, otherwise `spriteOutline` (write it with `if/else`, no ternary).
   - Why: the outline is the **single source** for both the off-screen arrow edge (`MapView.EdgeShape`) and the round clamp in Follow Car (old `MapViewFollowCar.round`). A rectangle minimap is always a rectangle; a sprite minimap says whether its sprite is round (Circle, default) or boxy (Rectangle).
3. `Runtime/UI/MapViewFollowCar.cs` (change → plain `internal class`)
   - Remove `: MonoBehaviour`, `[DefaultExecutionOrder]`, `[RequireComponent]`, `using` lines that become unused.
   - Remove all `[SerializeField]` setting fields, `round`, `RotationMode`, `SetRotationMode`, `ToggleRotationMode` and all `internal Set...` methods (they move to the settings classes / the root).
   - Constructor `internal MapViewFollowCar(MapView view, MinimapFollowSettings settings, MinimapShapeSettings shapeSettings)`: store the three references.
   - `internal void Enable()`: `view.SetFollowCar(this); needsSnap = true;` (was `OnEnable`, without the `GetComponent`).
   - `internal void Disable()`: `if (view.FollowCar == this) { view.SetFollowCar(null); }`.
   - `UpdateFollowCarVisuals(float deltaTime)` keeps its body; every old field read becomes a settings read (`rotationMode` → `settings.RotationMode`, `zoomSmoothing` → `settings.ZoomSmoothing`, ...). `round` → `shapeSettings.Outline == EdgeShape.Circle` (compute it into a local at the start of the method and pass it on).
   - Keep the runtime state fields (`currentZoom`, `zoomVelocity`, `rotationVelocity`, `lastHeadingRotation`, `previousTargetRotation`, `needsSnap`, `inTurn`, `lastMap`, `lastCar`), the `ProfilerMarker` and `internal bool PinPlayerToEdge { get; private set; }`.
   - Delete `LateUpdate`, `OnEnable`, `OnDisable`.
4. `Runtime/UI/MinimapShape.cs` (change → plain `internal class`)
   - Constructor `internal MinimapShape(RectTransform viewport, MinimapShapeSettings settings)`.
   - `internal void Apply()`: if `viewport == null` return. Rectangle kind → `ApplyRectangle()`, else `ApplySprite()`.
   - `ApplyRectangle()`: remove the viewport's `Mask`, **then** its `Image`; add a `RectMask2D` if missing. (The `Viewport` belongs to the root, so the `Image` is always ours; the old `ownsImageComponent` flag is gone.)
   - `ApplySprite()`: remove `RectMask2D`; get or add `Image`; `image.sprite = settings.Sprite`; `image.raycastTarget = true` (taps on the map must hit something); get or add `Mask`; `mask.showMaskGraphic = false`.
   - `RemoveComponent(Component component)`: null → return; `Application.isPlaying` → `Object.Destroy(component)`, else `Object.DestroyImmediate(component)`.
   - Delete `OnEnable`, `OnValidate`, `DelayedApplyShape`, `SetShapeKind`, `SetSprite`, `ShapeKind`, `ownsImageComponent`, the attributes.
5. `Runtime/UI/MinimapCompass.cs` (new, plain `internal class`; replaces `CompassButton`)
   - Constructor `internal MinimapCompass(Button button, RectTransform icon, MapView view, MinimapFollowSettings followSettings)`.
   - `internal void Enable()`: if `button != null`: when `icon == null` use `(RectTransform)button.transform` as the icon; `button.transform.SetAsLastSibling()` (keeps it drawn above the viewport and the frame, see S45 note); `button.onClick.AddListener(HandleClick)`.
   - `internal void UpdateCompassVisuals()`: if `icon == null` return; `icon.localRotation = Quaternion.Euler(0f, 0f, view.RotationDegrees)`.
   - `internal void Disable()`: if `button != null`, `RemoveListener(HandleClick)`.
   - `private void HandleClick()`: `followSettings.ToggleRotationMode()`.
6. `Runtime/UI/CompassButton.cs` and `Runtime/UI/MinimapTapToOpen.cs` (delete, with `.meta`).
7. `Runtime/UI/NavigationMinimap.cs` (new, `public class NavigationMinimap : MonoBehaviour, IPointerClickHandler`, attributes `[DefaultExecutionOrder(99)]`, `[RequireComponent(typeof(RectTransform))]`)
   - Serialized fields (exact names): `viewport` (RectTransform), `followSettings` (= new), `shapeSettings` (= new), `tapAction` (= `MinimapTapAction.OpenFullMap`), `fullMap` (**`MapViewInteractive` in this step**; S60 changes the type), `compassButton` (Button), `compassIcon` (RectTransform).
   - Private: `MapView view`, `MapViewFollowCar followCar`, `MinimapShape shape`, `MinimapCompass compass`, `bool partsEnabled`.
   - Public API: `MapView View`, `MinimapFollowSettings FollowSettings`, `MinimapShapeSettings ShapeSettings`, `MinimapRotationMode RotationMode` (→ `followSettings.RotationMode`), `MinimapTapAction TapAction`, `void SetRotationMode(MinimapRotationMode value)`, `void ToggleRotationMode()`, `void SetTapAction(MinimapTapAction value)`, `void SetFullMap(MapViewInteractive value)`.
   - Internal: `RectTransform Viewport` (getter), `SetViewport(RectTransform value)`, `SetCompass(Button button, RectTransform icon)`, `MapViewFollowCar FollowCar` (getter), `void ApplyShape()`.
   - `private bool EnsureParts()`:
     - if `view != null` return true.
     - if `viewport == null`: `CustomLogger.LogError("NavigationMinimap on '" + name + "': Viewport is not assigned.", this)`; return false.
     - `view = viewport.GetComponent<MapView>()`; null → `LogError("NavigationMinimap on '" + name + "': no MapView on the Viewport.", this)`; return false. *(S61 replaces this line.)*
     - `followCar = new MapViewFollowCar(view, followSettings, shapeSettings)`; `if (shape == null) { shape = new MinimapShape(viewport, shapeSettings); }`; `compass = new MinimapCompass(compassButton, compassIcon, view, followSettings)`; return true.
   - `ApplyShape()`: if `viewport == null` return; `if (shape == null) { shape = new MinimapShape(viewport, shapeSettings); }`; `shape.Apply()`.
   - `OnEnable`: `if (!EnsureParts()) { return; }` → `ApplyShape()` → `view.SetEdgeShape(shapeSettings.Outline)` → `followCar.Enable()` → `compass.Enable()` → `partsEnabled = true`.
   - `LateUpdate` → `UpdateMinimapVisuals(Time.unscaledDeltaTime)`.
   - `public void UpdateMinimapVisuals(float deltaTime)`: `if (!partsEnabled) return;` → `view.SetEdgeShape(shapeSettings.Outline)` (one enum write per frame keeps the arrow edge in sync with runtime changes) → `followCar.UpdateFollowCarVisuals(deltaTime)` → `compass.UpdateCompassVisuals()`.
   - `OnDisable`: `if (!partsEnabled) return;` → `compass.Disable()` → `followCar.Disable()` → `partsEnabled = false`.
   - `public void OnPointerClick(PointerEventData eventData)`: if `tapAction != MinimapTapAction.OpenFullMap` return; if `fullMap == null`, `fullMap = FindAnyObjectByType<MapViewInteractive>(FindObjectsInactive.Include)`; still null → `CustomLogger.LogError("NavigationMinimap on '" + name + "': no full map found in the loaded scenes.", this)` and return; `fullMap.Open()`. (uGUI passes a click on the map up to this root; the compass `Button` keeps its own clicks.)
   - `OnValidate` (wrap the body in `#if UNITY_EDITOR`): if `Application.isPlaying` return; `UnityEditor.EditorApplication.delayCall += DelayedApplyShape;`. `DelayedApplyShape` (also inside `#if UNITY_EDITOR`): `if (this == null) return;` then `ApplyShape()`. (Same pattern the old `MinimapShape` used.)
8. `Editor/Setup/NavigationSetupWindow.cs` (change)
   - `DrawStep4Ui`: `minimapPresent = FindAnyObjectByType<NavigationMinimap>(FindObjectsInactive.Include) != null`.
   - `AddMinimapAndFullMap`: replace the `MinimapTapToOpen` lookup with `NavigationMinimap minimap = minimapInstance.GetComponent<NavigationMinimap>()` and `minimap.SetFullMap(interactive)` when both exist. Leave the full map and gamepad code as is.
9. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change, `CreateMinimapPrefab` and `CreateCompassButton`)
   - The root gets `NavigationMinimap minimap = root.AddComponent<NavigationMinimap>()`. The `Viewport` child gets only `MapView` (route style + channel mask + show preview + arrow prefab, as today; **delete** the `view.SetEdgeShape(EdgeShape.Circle)` line).
   - Do not add `MapViewFollowCar`, `MinimapShape`, `MinimapTapToOpen`, `CompassButton`.
   - `minimap.SetViewport(viewportRect)`; `minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Sprite)`; `minimap.ShapeSettings.SetSprite(LoadSprite("MinimapMask"))`; `minimap.ShapeSettings.SetSpriteOutline(EdgeShape.Circle)`; `minimap.ApplyShape()` (adds Image + Mask to the viewport in the saved prefab).
   - `CreateCompassButton(Transform parent)` now returns the `Button`; the caller does `minimap.SetCompass(button, iconRect)` (return the icon too, e.g. through an `out RectTransform icon` parameter).
   - Order inside `CreateMinimapPrefab`: viewport → MapView → frame → compass → root component setup → `ApplyShape()` → save.
10. `Assets/Tests/NavigationSystem/Dev/Editor/DevUiInstaller.cs` (change): link with `installer.Minimap.GetComponent<NavigationMinimap>().SetFullMap(interactive)` (null-check both).
11. `Assets/Tests/NavigationSystem/Dev/DevNavigationTester.cs` (change): field and `Configure` parameter type `MapViewFollowCar` → `NavigationMinimap` (it uses `ToggleRotationMode()` and `RotationMode`, which exist on the root).
12. `Assets/Tests/NavigationSystem/Dev/Editor/FullSandboxBuilder.cs` (change): `minimap = installer.Minimap.GetComponent<NavigationMinimap>()`.
13. `Assets/Tests/NavigationSystem/PlayMode/MinimapTestRig.cs` (new, `internal class MinimapTestRig`)
    - Constructor `MinimapTestRig(Transform parent)`:
      - `Root = new GameObject("Minimap", typeof(RectTransform))`, parent it (`SetParent(parent, false)`), **`Root.SetActive(false)`** right away, stretch it (anchors 0→1, offsets 0).
      - `Viewport` child `new GameObject("Viewport", typeof(RectTransform))`, stretched (anchors 0→1, offsets 0, pivot stays 0.5/0.5).
      - `Viewport.gameObject.AddComponent<MapView>()` *(interim; S61 deletes this line)*.
      - `Minimap = Root.AddComponent<NavigationMinimap>()`; `Minimap.SetViewport(viewportRect)`.
    - Properties: `GameObject Root`, `RectTransform RootRect`, `RectTransform Viewport`, `NavigationMinimap Minimap`, `MapView View` (→ `Minimap.View`; null until activated).
    - `void Activate()` → `Root.SetActive(true)`.
    - `Button AddCompass()`: child `"Compass"` under `Root` with `Image` + `Button`; `Minimap.SetCompass(button, (RectTransform)button.transform)`; returns the button. Call before `Activate()`.
14. `Assets/Tests/NavigationSystem/PlayMode/MapViewFollowCarTests.cs` (change)
    - In `SetUp`: replace the `viewObject` creation and the `AddComponent<MapView>` / `AddComponent<MapViewFollowCar>` lines with `rig = new MinimapTestRig(canvasObject.transform);`. Replace each `followCar.SetXxx(v)` with `rig.Minimap.FollowSettings.SetXxx(v)`; **delete** `followCar.SetRound(true)` (the default shape is a round sprite). Then `rig.Activate();` and keep the two `yield return null`. After activation set `view = rig.View`.
    - In the tests: `followCar.SetRotationMode(x)` → `rig.Minimap.SetRotationMode(x)`; `followCar.SetNoseDeadZoneDegrees` / `SetTurnSmoothing` → `rig.Minimap.FollowSettings....`.
    - Keep every assertion and number unchanged.
15. `Assets/Tests/NavigationSystem/PlayMode/CompassButtonTests.cs` (delete, with `.meta`) → replaced by `NavigationMinimapCompassTests.cs` (new). No Manager in these tests: each test that activates the rig first calls `LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"))` once (the `MapView` component logs it).
    - `CompassClick_TogglesRotationMode`: rig + `AddCompass()`, expect error, `Activate()`; `RotationMode` is HeadingUp; `button.onClick.Invoke()`; `RotationMode` is NorthUp.
    - `NoCompass_EnablesWithoutOtherErrors`: rig without compass, expect error, `Activate()`, `LogAssert.NoUnexpectedReceived()`.
    - `[UnityTest] CompassIcon_FollowsViewRotation`: rig + `AddCompass()`, expect error, `Activate()`, `yield return null`; `rig.View.SetRotation(30f)`; `yield return null`; icon `localEulerAngles.z` ≈ 30 (tolerance 0.01). (Follow Car does nothing without a Manager, so the rotation stays.)
16. `Assets/Tests/NavigationSystem/PlayMode/MinimapShapeTests.cs` (delete, with `.meta`) → replaced by `NavigationMinimapShapeTests.cs` (new). The shape tests call `rig.Minimap.ApplyShape()` **without activating** the rig (no Manager error), then `yield return null` (Destroy is deferred in Play mode).
    - `[UnityTest] Rectangle_AddsRectMask2D_RemovesMaskAndImage`: kind Sprite → apply → yield; kind Rectangle → apply → yield; viewport has `RectMask2D`, no `Mask`, no `Image`.
    - `[UnityTest] Sprite_AddsImageAndMask_RemovesRectMask2D`: kind Rectangle → apply → yield; set a sprite (`Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.one * 0.5f)`, destroy texture and sprite in TearDown), kind Sprite → apply → yield; viewport has `Image` with that sprite and `raycastTarget == true`, `Mask` with `showMaskGraphic == false`, no `RectMask2D`.
    - `[UnityTest] ViewEdgeShape_FollowsShapeSettings`: expect the Manager error, `Activate()`, yield; kind Rectangle → yield → `rig.View.EdgeShape == EdgeShape.Rectangle`; kind Sprite + outline Circle → yield → `EdgeShape.Circle`.
17. `Assets/Tests/NavigationSystem/PlayMode/MinimapTapToOpenTests.cs` (delete, with `.meta`) → replaced by `NavigationMinimapTapTests.cs` (new). Use a `MinimapTestRig` (not activated: `OnPointerClick` can be called directly). Keep the old helper `CreateInactiveFullMap(name)` (bare inactive GameObject + `AddComponent<MapViewInteractive>()`; `MapView` is added by `RequireComponent`) and `ExpectNoManagerErrorOnOpen()` (regex `"MapView on '.*': no NavigationManager found"`).
    - `Tap_FullMapUnassigned_FindsInactiveFullMap_Opens`, `Tap_NoFullMapInScene_LogsError` (regex `"NavigationMinimap on '.*': no full map"`), `Tap_FullMapAssigned_UsesAssigned` (via `rig.Minimap.SetFullMap`): same steps and asserts as the old tests, calling `rig.Minimap.OnPointerClick(new PointerEventData(null))`.
    - `Tap_ActionNothing_DoesNothing`: `SetTapAction(MinimapTapAction.Nothing)`, one inactive full map; click; full map still inactive; `LogAssert.NoUnexpectedReceived()`.
18. `Assets/Tests/NavigationSystem/EditMode/MinimapShapeSettingsTests.cs` (new)
    - `Outline_RectangleKind_IsRectangle` (even with `spriteOutline` = Circle).
    - `Outline_SpriteKind_UsesSpriteOutline` (Circle → Circle, Rectangle → Rectangle).
19. `Assets/Tests/NavigationSystem/EditMode/MinimapFollowSettingsTests.cs` (new)
    - `Defaults_MatchDesign` (HeadingUp, 0.3, speed zoom on, 150 / 500, fixed 300).
    - `ToggleRotationMode_SwitchesBothWays`.
20. `Assets/Tests/NavigationSystem/EditMode/DefaultPrefabsTests.cs` (change) `MinimapPrefab_HasRequiredComponents`: the root has `NavigationMinimap`; the viewport has `MapView`; through a `SerializedObject` on the root, `viewport` and `compassButton` are not null and `shapeSettings.sprite` is not null; the viewport has `Image` + `Mask`. Remove the asserts for the deleted components.

**Tests:**
- PlayMode: `MapViewFollowCarTests` (7, unchanged asserts), `NavigationMinimapCompassTests` (3), `NavigationMinimapShapeTests` (3), `NavigationMinimapTapTests` (4).
- EditMode: `MinimapShapeSettingsTests` (2), `MinimapFollowSettingsTests` (2), `DefaultPrefabsTests`.
- All other tests stay green.

**Manual checks:**
1. Build Default Prefabs. Open `Graphics/Prefabs/NavigationMinimap.prefab`: the root shows **Navigation Minimap**; `Viewport` shows `MapView`, `Image`, `Mask` and nothing else of ours; `CompassButton` has only `Button`.
2. Select the prefab root, change Shape Kind to Rectangle: `Viewport` loses Image + Mask and gets RectMask2D. Back to Sprite: Image + Mask return. Don't save that change.
3. Create Full Sandbox, Play: minimap follows the car, heading-up; the compass rotates and a click switches to north-up; tapping the map opens the full map; off-screen arrows sit on the round edge.
4. In Play mode, set Shape Kind = Rectangle on the root: the off-screen arrows move to the square edge.

---

## S60 — Full map root: `NavigationFullMap`

**Difficulty: HARD** (pointer events moved to the root, open/close moved to the root, many files).

**Goal:** the full map prefab is driven by one root component, `NavigationFullMap`, which owns all full map settings and slots and runs Interactive, pointer input, Preview Panel and Navigation Controls as plain classes. Adds the fling and double-tap zoom switches. Design: "Prefab front door", "Root inspector layout (full map)".

**Depends on:** S59.

**Interim state (until S61):** `MapView` is still a MonoBehaviour on `Viewport`; the root gets it with `viewport.GetComponent<MapView>()` and runs before it (order 99). Because `MapView` still searches for its own Manager, a full map enabled without a Manager logs **two** errors in this step (MapView's and the root's).

**Files:**

1. `Runtime/UI/FullMapInteractionSettings.cs` (new, public): fields from the table (`zoomOutMode`, `crosshairMode`, `openZoomMeters`, `mouseWheelStep`, `doubleTapStep`, `markerTapRadius`, `confirmStep`, `builtInPointerInput`, `fling`, `doubleTapZoom`), properties, internal setters.
2. `Runtime/UI/PreviewPanelSlots.cs` (new, public): `panelRoot` (GameObject), `distanceText` and `etaText` (**`NavigationTextTarget` in this step**; S62 changes them to `Component`), `confirmButton`, `cancelButton` (Button). Properties + internal setters.
3. `Runtime/UI/FullMapButtons.cs` (new, public): `stopButton`, `centerButton`, `closeButton`. Properties + internal setters.
4. `Runtime/UI/MapViewInteractive.cs` (change → plain **`public class MapViewInteractive : IMapGestureTarget`**)
   - Remove `MonoBehaviour`, attributes, `[SerializeField]` setting fields, the `Opened` / `Closed` events, `Open`, `Close`, `Toggle`, and the getters / internal setters for settings (`ZoomOutMode`, `CrosshairMode`, `OpenZoomMeters`, `MouseWheelStep`, `DoubleTapStep`, `MarkerTapRadius`, `ConfirmStep`, `SetZoomOutMode`, `SetCrosshairMode(CrosshairMode)`, `SetCrosshairImage`, `SetOpenZoomMeters`, `SetMouseWheelStep`, `SetDoubleTapStep`, `SetMarkerTapRadius`, `SetConfirmStep`).
   - Constructor `internal MapViewInteractive(NavigationFullMap owner, MapView view, FullMapInteractionSettings settings, Image crosshairImage)`.
   - `internal void Enable()` = old `OnEnable` body without `GetComponent` (`isFollowingCar = true; needsSnap = true; crosshairLogic.Mode = settings.CrosshairMode; ApplyCrosshairVisible(crosshairLogic.IsCrosshairActive);`).
   - `internal void Disable()` = old `OnDisable` **without** raising `Closed` (the root raises it): `if (manager != null) { manager.CancelPreview(); }`.
   - `internal void UpdateInteractiveMapLogic(float deltaTime)`: same body; where it raised `Opened`, call `owner.NotifyOpened()`; read settings from `settings`.
   - Keep public: `ZoomMeters`, `IsFollowingCar`, `IsCrosshairActive`, `Pan`, `Zoom`, `Tap`, `TapAt`, `SetZoomMeters`, `CenterOnCar`, `SetCrosshairMode(bool active)`, `ConfirmAtCrosshair`, `PanByStick`, `ZoomBySpeed`. Keep `internal NotifyPointerInput()`.
   - (A public class may implement the internal interface `IMapGestureTarget`; its methods stay public.)
5. `Runtime/UI/PointerInputAdapter.cs` (change → plain `internal class`)
   - Constructor `internal PointerInputAdapter(MapViewInteractive target, RectTransform viewport, FullMapInteractionSettings settings)`; creates `tracker = new GestureTracker(target)` in the constructor.
   - `internal GestureTracker Tracker` (getter, for tests).
   - `internal void ApplySettings()`: `tracker.MouseWheelStep = settings.MouseWheelStep; tracker.DoubleTapStep = settings.DoubleTapStep; tracker.DoubleTapEnabled = settings.DoubleTapZoom; tracker.FlingEnabled = settings.Fling;`.
   - `internal void HandlePointerDown(PointerEventData e)`, `HandleBeginDrag`, `HandleDrag`, `HandlePointerUp`, `HandleScroll`: same bodies as the old `OnPointerDown` / `OnBeginDrag` / `OnDrag` / `OnPointerUp` / `OnScroll`, each starting with `ApplySettings()`. `ToViewportLocal` uses the `viewport` passed in. (`OnEndDrag` / `OnPointerClick` were empty: no methods needed.)
   - `internal void UpdatePointerInputLogic(float time, float deltaTime)`: `ApplySettings(); tracker.UpdateGestureLogic(time, deltaTime);`.
6. `Runtime/UI/PreviewPanel.cs` (change → plain `internal class`)
   - Constructor `internal PreviewPanel(PreviewPanelSlots slots)`.
   - `internal void Enable(NavigationManager manager)`: same as the old `OnEnable` but takes the Manager instead of finding it, and **does not log** (the root logs). `manager` may be null: then still add the button listeners and hide the panel.
   - `internal void Disable()`: same as old `OnDisable`.
   - Read slots through `slots.PanelRoot`, `slots.DistanceText`, ... every time (never cache them).
   - Delete `FindManager`, the `Set...` methods, the `manager` field.
7. `Runtime/UI/NavigationControls.cs` (change → plain `internal class`)
   - Constructor `internal NavigationControls(NavigationFullMap owner, MapViewInteractive interactive, FullMapButtons buttons)`.
   - `internal void Enable(NavigationManager manager)` / `internal void Disable()` / `internal void UpdateNavigationControlsVisuals()`: same as before, without finding or logging. Close button → `owner.Close()`.
   - Delete `FindManager`, `GetComponentInParent`, the `Set...` methods.
8. `Runtime/UI/NavigationFullMap.cs` (new, `public class NavigationFullMap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler`, attributes `[DefaultExecutionOrder(99)]`, `[RequireComponent(typeof(RectTransform))]`)
   - Serialized fields (exact names): `manager` (NavigationManager), `viewport` (RectTransform), `interactionSettings` (= new), `crosshairImage` (Image), `previewPanel` (`PreviewPanelSlots`, = new), `buttons` (`FullMapButtons`, = new).
   - Private: `NavigationManager cachedManager`, `MapView view`, `MapViewInteractive interactive`, `PointerInputAdapter pointerInput`, `PreviewPanel panel`, `NavigationControls controls`, `bool partsEnabled`.
   - Public: `event Action Opened`, `event Action Closed`, `MapView View`, `MapViewInteractive Interactive`, `FullMapInteractionSettings InteractionSettings`, `PreviewPanelSlots PreviewPanelSlots`, `FullMapButtons Buttons`, `void Open()` (`gameObject.SetActive(true)`), `void Close()` (`gameObject.SetActive(false)`), `void Toggle()`, `void CenterOnCar()` (`if (interactive != null) { interactive.CenterOnCar(); }`).
   - Internal: `RectTransform Viewport` (getter), `SetManager`, `SetViewport`, `SetCrosshairImage`, `void NotifyOpened()` (`if (Opened != null) { Opened(); }`).
   - `private bool EnsureParts()`: like the minimap's (same two error messages with `NavigationFullMap`), `view = viewport.GetComponent<MapView>()` *(S61 replaces this line)*, then `interactive = new MapViewInteractive(this, view, interactionSettings, crosshairImage)`, `pointerInput = new PointerInputAdapter(interactive, viewport, interactionSettings)`, `panel = new PreviewPanel(previewPanel)`, `controls = new NavigationControls(this, interactive, buttons)`.
   - `private NavigationManager FindManager()`: `manager` if set, else `cachedManager` if not null, else `FindAnyObjectByType<NavigationManager>()`.
   - `OnEnable`: `if (!EnsureParts()) return;` → `cachedManager = FindManager()` → if null: `CustomLogger.LogError("NavigationFullMap on '" + name + "': no NavigationManager found. Load the UI after the Navigation Manager.", this)` (continue anyway, like the old components did) → `interactive.Enable()` → `panel.Enable(cachedManager)` → `controls.Enable(cachedManager)` → `partsEnabled = true`.
   - `Update` → `UpdateFullMapInputLogic(Time.unscaledTime, Time.unscaledDeltaTime)`: if `partsEnabled && interactionSettings.BuiltInPointerInput` → `pointerInput.UpdatePointerInputLogic(time, deltaTime)`.
   - `LateUpdate` → `UpdateFullMapVisuals(Time.unscaledDeltaTime)`: `if (!partsEnabled) return;` → `interactive.UpdateInteractiveMapLogic(deltaTime)` → `controls.UpdateNavigationControlsVisuals()`.
   - `OnDisable`: `if (!partsEnabled) return;` → `controls.Disable()` → `panel.Disable()` → `interactive.Disable()` → `partsEnabled = false` → `cachedManager = null` → `if (Closed != null) { Closed(); }`.
   - Pointer handlers (`OnPointerDown`, `OnBeginDrag`, `OnDrag`, `OnEndDrag`, `OnPointerUp`, `OnScroll`): `if (!partsEnabled || !interactionSettings.BuiltInPointerInput) return;` then forward to the matching `pointerInput.HandleXxx(eventData)`. `OnEndDrag` forwards nothing (keep it: uGUI needs the interface for drags to start on this object). Behaviour must match the old adapter on `Viewport` (buttons and the panel are children of `Viewport`, so the same events arrive as before).
9. `Runtime/UI/NavigationMinimap.cs` (change): `fullMap` field, `SetFullMap` parameter and the `FindAnyObjectByType` in `OnPointerClick` use `NavigationFullMap` instead of `MapViewInteractive`.
10. `Runtime.InputSystem/GamepadInputAdapter.cs` (change, **not compiled here: double-check**): `target` field type → `NavigationFullMap`. In `OnEnable`, first line: `if (target == null) { target = GetComponent<NavigationFullMap>(); }`. Calls: `target.Interactive.PanByStick(...)`, `target.Interactive.ZoomBySpeed(...)`, `target.Interactive.ConfirmAtCrosshair()`, `target.CenterOnCar()`, `target.Close()`. Guard `target.Interactive == null` (full map not enabled yet) in `UpdateGamepadInputLogic` and `HandleConfirmPerformed`.
11. `Editor/Setup/NavigationSetupWindow.cs` (change): `fullMapPresent` uses `NavigationFullMap`; in `AddMinimapAndFullMap`, `NavigationFullMap fullMap = fullMapInstance.GetComponent<NavigationFullMap>()`, link `minimap.SetFullMap(fullMap)`, `AddGamepadAdapter(fullMapInstance)`; in `AddGamepadAdapter`, `target` = `fullMapObject.GetComponent<NavigationFullMap>()`.
12. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change, `CreateFullMapPrefab`, `CreatePreviewPanel`, `CreateNavigationControls`)
    - Root: `NavigationFullMap fullMap = root.AddComponent<NavigationFullMap>()`; `fullMap.SetViewport(viewportRect)`; `Viewport` keeps only `MapView` (as today). No `MapViewInteractive`, `PointerInputAdapter`, `PreviewPanel`, `NavigationControls` components.
    - Crosshair → `fullMap.SetCrosshairImage(crosshairImage)`.
    - Panel → `fullMap.PreviewPanelSlots.SetPanelRoot(...)`, `SetDistanceText(...)`, `SetEtaText(...)`, `SetConfirmButton(...)`, `SetCancelButton(...)`. The `TmpTextTarget` objects stay in this step.
    - Controls → `fullMap.Buttons.SetStopButton(...)`, `SetCenterButton(...)`, `SetCloseButton(...)`.
    - **The root is saved inactive** (`root.SetActive(false)` just before `SaveAsPrefabAsset`); the viewport stays active. (Before, the viewport was saved inactive.)
13. `Assets/Tests/NavigationSystem/Dev/Editor/DevUiInstaller.cs` (change): link `NavigationMinimap.SetFullMap(installer.FullMap.GetComponent<NavigationFullMap>())`.
14. Dev scripts: `PerfSceneBuilder.cs` (`CreateUi` returns `NavigationFullMap`: `installer.FullMap.GetComponent<NavigationFullMap>()`), `DevPerfOverlay.cs` and `DevPerfPhaseCycler.cs` (field / `Configure` parameter type `MapViewInteractive` → `NavigationFullMap`; in the cycler `fullMap.GetComponent<MapView>()` → `fullMap.View`; `IsFullMapOpen` keeps `fullMap.isActiveAndEnabled`).
15. `Assets/Tests/NavigationSystem/PlayMode/FullMapTestRig.cs` (new, `internal class FullMapTestRig`): same shape as `MinimapTestRig` — `Root` (inactive right away, **not** stretched: tests set `RootRect` anchors/size themselves), stretched `Viewport` child with `AddComponent<MapView>()` *(interim; S61 deletes this line)*, `FullMap = Root.AddComponent<NavigationFullMap>()`, `FullMap.SetViewport(...)`. Properties `Root`, `RootRect`, `Viewport`, `FullMap`, `View` (→ `FullMap.View`), `Interactive` (→ `FullMap.Interactive`). `void Activate()`. `void Rebind()` → `Root.SetActive(false); Root.SetActive(true);` (used after assigning slots to an already active full map). Helper `Button AddButton(string name)` (child of `Viewport` with `Image` + `Button`) and `GameObject AddChild(string name)`.
16. `Assets/Tests/NavigationSystem/PlayMode/MapViewInteractiveTests.cs`, `CrosshairTests.cs`, `TapAndPreviewTests.cs` (change)
    - `SetUp`: replace the `viewObject` creation with `rig = new FullMapTestRig(canvasObject.transform)`; apply the **same anchor / pivot / size lines** to `rig.RootRect`; keep `viewObject = rig.Root` if the file uses `viewObject` elsewhere. Replace `AddComponent<MapView>` / `AddComponent<MapViewInteractive>` with nothing; `interactive.SetOpenZoomMeters(x)` → `rig.FullMap.InteractionSettings.SetOpenZoomMeters(x)`; then `rig.Activate()`; then `view = rig.View; interactive = rig.Interactive;`.
    - `interactive.Close()` → `rig.FullMap.Close()`; `interactive.SetConfirmStep(false)` → `rig.FullMap.InteractionSettings.SetConfirmStep(false)`.
    - `TapAndPreviewTests` panel / button tests: build the panel root and buttons under `rig.Viewport`, assign them through `rig.FullMap.PreviewPanelSlots.SetXxx(...)` / `rig.FullMap.Buttons.SetXxx(...)`, then `rig.Rebind()` (the old tests used a disabled component; same idea). Delete the `CreateDisabledComponent` helper if unused.
    - Keep every assertion and number unchanged.
17. `Assets/Tests/NavigationSystem/PlayMode/NavigationMinimapTapTests.cs` (change): `CreateInactiveFullMap` builds a `FullMapTestRig` (not activated) and returns `rig.FullMap`; opening it without a Manager logs **two** errors → `ExpectNoManagerErrorOnOpen()` calls `LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"))` **twice**. Track the rig roots in `createdObjects`. Assertions use `fullMap.gameObject.activeSelf`.
18. `Assets/Tests/NavigationSystem/PlayMode/MapViewNoManagerTests.cs` (change): delete `NavigationControls_EnabledWithoutManager_LogsError` and `PreviewPanel_EnabledWithoutManager_LogsError`; add `NavigationFullMap_EnabledWithoutManager_LogsError`: `FullMapTestRig`, expect the generic regex twice, `Activate()`, `LogAssert.NoUnexpectedReceived()`. Keep the `MapView` and `NavigationEvents` tests.
19. `Assets/Tests/NavigationSystem/EditMode/PointerInputAdapterTests.cs` (new): build `new FullMapInteractionSettings()` and `new PointerInputAdapter(null, null, settings)` (the constructor only stores references).
    - `ApplySettings_FlingOff_TrackerFlingDisabled`, `ApplySettings_DoubleTapZoomOff_TrackerDoubleTapDisabled`, `ApplySettings_Defaults_BothEnabled`, `ApplySettings_Steps_Copied` (mouse wheel 1.5, double-tap 3 → tracker values).
20. `Assets/Tests/NavigationSystem/EditMode/DefaultPrefabsTests.cs` (change) `FullMapPrefab_HasRequiredComponents_AndIsInactive`: root has `NavigationFullMap` and `prefab.activeSelf == false`; viewport has `MapView`; via `SerializedObject`: `viewport`, `crosshairImage`, `previewPanel.panelRoot`, `previewPanel.distanceText`, `previewPanel.confirmButton`, `buttons.stopButton`, `buttons.closeButton` are not null.

**Tests:**
- PlayMode: `MapViewInteractiveTests` (6), `CrosshairTests` (3), `TapAndPreviewTests` (9), `NavigationMinimapTapTests` (4), `MapViewNoManagerTests` (3).
- EditMode: `PointerInputAdapterTests` (4), `DefaultPrefabsTests`.
- Existing `GestureTrackerTests` cover what fling-off / double-tap-off do.

**Manual checks:**
1. Build Default Prefabs. `NavigationFullMap.prefab`: the root is inactive and shows **Navigation Full Map**; `Viewport` shows only `MapView` of ours; buttons and panel have no Gley scripts except `TmpTextTarget` on the two texts (removed in S62).
2. Create Full Sandbox, Play: open the full map from the minimap; drag pans, flings; wheel / pinch zooms; double-tap zooms in; tap → preview panel with distance / ETA; Confirm starts; Stop / Center / Close work; closing cancels a preview.
3. In Play mode on the full map root set Fling off: a quick swipe stops dead. Double-Tap Zoom off: a single tap previews at once (no short wait) and double-tap no longer zooms.
4. Set Built-in Pointer Input off: the map ignores mouse / touch; buttons still work.

---

## S61 — Map View becomes a plain class

**Difficulty: HARD** (central class used by everything; many test files).

**Goal:** `MapView` is a plain class owned by the roots; its settings move to `MapViewSettings` on each root; the roots find the Manager and log the missing-Manager error. After this step each minimap prefab has exactly one Gley script. Design: "Prefab front door", "UI loaded at runtime".

**Depends on:** S60.

**Files:**

1. `Runtime/UI/MapViewSettings.cs` (new, public)
   - Constants: `public const int MinimapChannelBit = 1 << 0;` and `public const int FullMapChannelBit = 1 << 1;` (move them from `MapView`).
   - Fields from the table **except `textWriter`** (S62 adds it).
   - Constructors: `public MapViewSettings()` → `channelMask = MinimapChannelBit | FullMapChannelBit; showPreview = true;` and `public MapViewSettings(int channelMask, bool showPreview)`.
   - Properties + internal setters as usual.
2. `Runtime/UI/MapView.cs` (change → plain **`public class MapView`**)
   - Remove `MonoBehaviour`, `[DefaultExecutionOrder]`, `[RequireComponent]`, the channel constants (now in `MapViewSettings`).
   - Remove the serialized fields `manager`, `routeStyle`, `arrowPrefab`, `minZoomMeters`, `edgeInset`, `channelMask`, `showPreview`, `showOffScreenArrows`, `showArrowDistance`. `edgeShape` becomes a plain `private EdgeShape edgeShape = EdgeShape.Rectangle;` (runtime, set by the owner). `zoomMeters` becomes a plain `private float zoomMeters = 300f;` (runtime state).
   - New private fields: `readonly MonoBehaviour host`, `readonly MapViewSettings settings`. `viewport` stays a private field.
   - Constructor `internal MapView(MonoBehaviour host, RectTransform viewport, MapViewSettings settings)`.
   - Property changes: `EdgeInset` → `settings.EdgeInset`; `ChannelMask` → `settings.ChannelMask`; `ShowOffScreenArrows` → `settings.ShowOffScreenArrows`; `ShowArrowDistance` → `settings.ShowArrowDistance`; `ArrowPrefab` → `settings.ArrowPrefab`. New `internal MapViewSettings Settings`.
   - `SetZoomMeters` clamps with `settings.MinZoomMeters`. `ApplyRouteStyle` reads `settings.RouteStyle`. `ShowCurrentRoutes` / `HandlePreviewReady` read `settings.ShowPreview`.
   - Delete `SetShowPreview`, `SetChannelMask`, `SetEdgeInset`, `SetArrowPrefab`, `SetShowOffScreenArrows`, `SetShowArrowDistance` (callers use the settings setters). Keep `SetEdgeShape`, `SetFollowCar`, `SetCenter`, `SetRotation`, `SetZoomMeters`.
   - `OnEnable` → `internal void Enable(NavigationManager manager)`: `BuildHierarchyIfNeeded();` then `if (manager == null) { return; }` (the owner already logged), then the same binding code as today (`cachedManager = manager; hasManager = true;` subscriptions, `HandleMapChanged`, `ShowCurrentRoutes`).
   - `OnDisable` → `internal void Disable()` (same body). Delete `LateUpdate` and `FindManager`.
   - `BuildHierarchyIfNeeded`: `if (viewport == null) { viewport = host.GetComponent<RectTransform>(); }`.
   - Logging: `name` → `host.name`, context `this` → `host`. The destroyed-Manager message keeps the text `"MapView on '" + host.name + "': the NavigationManager was destroyed. The UI must not outlive the Manager."`.
   - `MarkerLayer` and `RouteLineRenderer` are still created as runtime child GameObjects; `markerLayer.SetView(this)` is unchanged.
3. `Runtime/UI/NavigationMinimap.cs` (change)
   - New serialized fields: `manager` (NavigationManager) and `viewSettings` = `new MapViewSettings(MapViewSettings.MinimapChannelBit, false)`. New private `NavigationManager cachedManager`.
   - Public `MapViewSettings ViewSettings`; internal `SetManager(NavigationManager value)`.
   - `EnsureParts`: replace the `GetComponent<MapView>()` line and its error with `view = new MapView(this, viewport, viewSettings);`.
   - `FindManager()` exactly like the full map's.
   - `OnEnable`: after `EnsureParts`: `cachedManager = FindManager()`; null → `CustomLogger.LogError("NavigationMinimap on '" + name + "': no NavigationManager found. Load the UI after the Navigation Manager.", this)`; then `view.Enable(cachedManager)` (called even when null, so the view hierarchy exists, as before) → `ApplyShape()` → `view.SetEdgeShape(...)` → `followCar.Enable()` → `compass.Enable()` → `partsEnabled = true`.
   - `UpdateMinimapVisuals`: edge shape → `followCar.UpdateFollowCarVisuals(deltaTime)` → **`view.UpdateMapViewVisuals(deltaTime)`** → `compass.UpdateCompassVisuals()`.
   - `OnDisable`: `compass.Disable()` → `followCar.Disable()` → **`view.Disable()`** → `partsEnabled = false` → `cachedManager = null`.
   - Keep `[DefaultExecutionOrder(99)]` (anything after the Manager's -100 works).
4. `Runtime/UI/NavigationFullMap.cs` (change)
   - New serialized field `viewSettings` = `new MapViewSettings(MapViewSettings.FullMapChannelBit, true)`; public `ViewSettings`.
   - `EnsureParts`: `view = new MapView(this, viewport, viewSettings);` (replaces the `GetComponent` line).
   - `OnEnable`: after the Manager lookup / error: **`view.Enable(cachedManager)`** before `interactive.Enable()`.
   - `UpdateFullMapVisuals`: `interactive.UpdateInteractiveMapLogic` → **`view.UpdateMapViewVisuals(deltaTime)`** → `controls.UpdateNavigationControlsVisuals()`.
   - `OnDisable`: after `interactive.Disable()`: **`view.Disable()`**.
5. `Editor/Setup/NavigationSetupWindow.cs` (change) `DrawBlurInfo`: replace the `MapView` search with `FindObjectsByType<NavigationMinimap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)` and the same for `NavigationFullMap`. When both arrays are empty → the two default lines (unchanged). Otherwise one line per root: skip a root whose `Viewport` is null; `DrawBlurLine(root.name, root.Viewport.rect.width, root.ViewSettings.MinZoomMeters, metersPerPixel)`. Delete `ReadMinZoomMeters`.
6. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change): no `MapView` component anywhere. Minimap: `minimap.ViewSettings.SetRouteStyle(routeStyle)`, `minimap.ViewSettings.SetArrowPrefab(offScreenArrow)` (channel mask and show preview come from the constructor). Full map: the same with its route style. Delete the builder's `MinimapChannelBit` / `FullMapChannelBit` constants if unused.
7. `Assets/Tests/NavigationSystem/Dev/DevPerfPhaseCycler.cs` (change): `GetMinimapView()` → return the `NavigationMinimap` (`minimapRoot.GetComponent<NavigationMinimap>()`); `ApplyToView(MapView view, MapViewSettings viewSettings, bool arrows, bool routeLine)` uses `viewSettings.SetShowOffScreenArrows(arrows)`; call it with `(minimap.View, minimap.ViewSettings, ...)` and `(fullMap.View, fullMap.ViewSettings, ...)`; null-check the roots **and** `View` (null until the root was enabled once).
8. `Assets/ToDelete/MapViewManualTest.cs` and `Assets/ToDelete/TempNavTrigger.cs` (delete, with `.meta`): scratch scripts that use `MapView` as a component and would no longer compile. Leave the rest of `Assets/ToDelete/` alone.
9. `Assets/Tests/NavigationSystem/PlayMode/TestMapViewHost.cs` (new, test-only `public class TestMapViewHost : MonoBehaviour`, `[RequireComponent(typeof(RectTransform))]`): hosts a bare `MapView` with no behaviour, for the tests of `MapView` itself.
   - `private readonly MapViewSettings settings = new MapViewSettings();` (not serialized), `private NavigationManager manager;`, `private MapView view;`.
   - `public MapView View`, `public MapViewSettings Settings`, `public void SetManager(NavigationManager value)`.
   - `OnEnable`: `if (view == null) { view = new MapView(this, GetComponent<RectTransform>(), settings); }`; find the Manager (`manager`, else `FindAnyObjectByType<NavigationManager>()`); none → `CustomLogger.LogError("TestMapViewHost on '" + name + "': no NavigationManager found.", this)`; `view.Enable(found)`.
   - `LateUpdate` → `view.UpdateMapViewVisuals(Time.unscaledDeltaTime)`. `OnDisable` → `view.Disable()`.
10. Test updates — `MapView` itself (`MapViewTests`, `MapViewRouteTests`, `MapViewLayerTests`, `MapViewManagerLostTests`, `MarkerLayerTests`, `OffScreenArrowTests`):
    - `x.AddComponent<MapView>()` → `TestMapViewHost host = x.AddComponent<TestMapViewHost>();` and use `host.View` where the `MapView` was used (keep field names like `view`, `lateView`, `previewHiddenView`, `mapView`).
    - `view.SetChannelMask(v)` → `host.Settings.SetChannelMask(v)`; `SetShowPreview` / `SetArrowPrefab` / `SetShowOffScreenArrows` / `SetShowArrowDistance` / `SetEdgeInset` → the same on `host.Settings`. `view.SetEdgeShape(...)` stays on the view. Keep a field for each host the test needs.
    - `MapViewRouteTests`: the preview-hidden view must have `showPreview` off **before** it is enabled: create its GameObject inactive, add the host, `host.Settings.SetShowPreview(false)`, then activate.
    - Error regexes: `"no NavigationManager found"` stays valid (the host logs it). `"MapView on '.*': the NavigationManager was destroyed"` stays valid (the MapView logs it with the host's name).
    - Keep every assertion and number unchanged.
11. Rigs: `MinimapTestRig` and `FullMapTestRig` — delete the `AddComponent<MapView>()` line.
12. Error counts drop from two to one for the full map: in `NavigationMinimapTapTests.ExpectNoManagerErrorOnOpen` expect the generic regex **once**. `MapViewNoManagerTests`:
    - delete `MapView_EnabledWithoutManager_LogsError`;
    - add `NavigationMinimap_EnabledWithoutManager_LogsError` (`MinimapTestRig`, regex `"NavigationMinimap on '.*': no NavigationManager found"`, `Activate()`, `NoUnexpectedReceived`);
    - `NavigationFullMap_EnabledWithoutManager_LogsError` now expects regex `"NavigationFullMap on '.*': no NavigationManager found"` once;
    - keep `NavigationEvents_...`.
    The minimap compass / shape / tap tests already expect exactly one error: unchanged.
13. `Assets/Tests/NavigationSystem/EditMode/DefaultPrefabsTests.cs` (change): remove the `MapView` asserts; add `MinimapPrefab_HasOneGleyScript`: count the components from `prefab.GetComponentsInChildren<MonoBehaviour>(true)` whose `GetType().Namespace` starts with `"Gley.NavigationSystem"` → exactly 1. (The full map still has the two `TmpTextTarget`s until S62.) Add `MinimapPrefab_ViewSettings_ChannelAndArrowSet`: via `SerializedObject`, `viewSettings.channelMask == 1`, `viewSettings.showPreview == false`, `viewSettings.arrowPrefab` and `viewSettings.routeStyle` not null; full map: `viewSettings.channelMask == 2`, `showPreview == true`.
14. `Assets/Tests/NavigationSystem/EditMode/MapViewSettingsTests.cs` (new): `DefaultConstructor_BothChannels_ShowsPreview`, `Constructor_SetsChannelAndPreview`, `Defaults_MinZoom50_EdgeInset8_ArrowsOn`.

**Tests:**
- PlayMode: `MapViewTests`, `MapViewRouteTests`, `MapViewLayerTests`, `MapViewManagerLostTests`, `MarkerLayerTests`, `OffScreenArrowTests` (all existing tests, unchanged asserts), `MapViewNoManagerTests` (3), `NavigationMinimapTapTests` (4), and everything from S59 / S60 again.
- EditMode: `MapViewSettingsTests` (3), `DefaultPrefabsTests`, `StaticStateTests`.

**Manual checks:**
1. Build Default Prefabs. `NavigationMinimap.prefab`: `Viewport` has only `Image` + `Mask`. The root inspector shows **View Settings** with route style and arrow prefab set.
2. Create Full Sandbox, Play: everything from S59 / S60 manual checks still works (minimap follow, compass, tap to open, full map pan / zoom / tap / confirm / stop / center / close, route on both maps, off-screen arrows with distance).
3. Create Runtime UI Test Scene and repeat S57 manual checks 1–4 (prefabs instantiated after Play, missing Manager → one error, destroyed Manager → one error).
4. Setup window step 2: the blur lines list the minimap and full map by name.

---

## S62 — Text writer instead of text adapter scripts

**Goal:** text slots take the text component itself (TMP or legacy `Text`); TMP is written through a `TmpTextWriter` asset; the adapter scripts (`NavigationTextTarget`, `TmpTextTarget`, `LegacyTextTarget`) are deleted. After this step both prefabs have exactly one Gley script. Design: "Text slots take the text itself", "Text".

**Depends on:** S61.

**Files:**

1. `Runtime/UI/NavigationTextWriter.cs` (new): `public abstract class NavigationTextWriter : ScriptableObject` with
   - `public abstract bool CanWrite(Component target);`
   - `public abstract void Write(Component target, StringBuilder text);`
   - `public abstract Component FindText(GameObject root);` (first writable text in `root` or its children, inactive included; null if none).
2. `Runtime/UI/NavigationTextOutput.cs` (new, plain `internal class`): the one place that knows legacy `Text` and the writer.
   - `internal bool CanWrite(Component target, NavigationTextWriter writer)`: null → false; `target is UnityEngine.UI.Text` → true; else `writer != null && writer.CanWrite(target)`.
   - `internal void Write(Component target, NavigationTextWriter writer, StringBuilder text)`: null target → return; `Text legacy = target as Text;` not null → `legacy.text = text.ToString();` return; else `if (writer != null) { writer.Write(target, text); }`.
   - `internal Component FindText(GameObject root, NavigationTextWriter writer)`: `Text legacy = root.GetComponentInChildren<Text>(true)`; not null → return it; `writer != null` → `return writer.FindText(root)`; else null.
3. `Runtime.TMP/TmpTextWriter.cs` (new, namespace `Gley.NavigationSystem.TMP`): `[CreateAssetMenu(fileName = "TmpTextWriter", menuName = "Gley/Navigation System/TMP Text Writer")] public class TmpTextWriter : NavigationTextWriter`
   - `CanWrite`: `target is TMP_Text`.
   - `Write`: `TMP_Text tmp = target as TMP_Text; if (tmp != null) { tmp.SetText(text); }` (the `StringBuilder` overload: no allocation).
   - `FindText`: `root.GetComponentInChildren<TMP_Text>(true)`.
4. `Runtime/UI/MapViewSettings.cs` (change): add field `textWriter` (`NavigationTextWriter`), property `TextWriter`, setter `SetTextWriter`.
5. `Runtime/UI/MapView.cs` (change): add `internal NavigationTextWriter TextWriter { get { return settings.TextWriter; } }`.
6. `Runtime/UI/PreviewPanelSlots.cs` (change): `distanceText` and `etaText` become `Component`; setters take `Component`.
7. `Runtime/UI/PreviewPanel.cs` (change): constructor `internal PreviewPanel(PreviewPanelSlots slots, MapViewSettings viewSettings)`; a `private readonly NavigationTextOutput textOutput = new NavigationTextOutput();`; write with `textOutput.Write(slots.DistanceText, viewSettings.TextWriter, distanceScratch)` (same for ETA).
8. `Runtime/UI/NavigationFullMap.cs` (change): `panel = new PreviewPanel(previewPanel, viewSettings);`.
9. `Runtime/UI/MarkerLayer.cs` (change): the arrow distance label.
   - `arrowTextTargets` becomes `Dictionary<GameObject, Component>`; add `private readonly NavigationTextOutput textOutput = new NavigationTextOutput();`.
   - Where it called `arrow.GetComponentInChildren<NavigationTextTarget>(...)` → `textOutput.FindText(arrow, view.TextWriter)` (lookup only on first use of each arrow instance, as today — never per frame).
   - Where it called `textTarget.SetText(distanceScratch)` → `textOutput.Write(textTarget, view.TextWriter, distanceScratch)`. Keep the null checks.
10. Delete `Runtime/UI/NavigationTextTarget.cs`, `Runtime/UI/LegacyTextTarget.cs`, `Runtime.TMP/TmpTextTarget.cs` (with `.meta`). Search the whole `Assets/` folder for `TextTarget` afterwards: only these deleted names may have matched.
11. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change)
    - New `TmpTextWriter CreateTextWriter()`: load `DevUiInstaller.PresetFolder + "/TmpTextWriter.asset"`; if missing, `ScriptableObject.CreateInstance<TmpTextWriter>()` + `AssetDatabase.CreateAsset`. Call it in `BuildDefaultPrefabs` before the prefabs.
    - Rename `CreateTmpTextTarget` → `CreateTmpText`, returning the `TextMeshProUGUI` (no adapter component).
    - Off-screen arrow: `CreateTmpText(... "DistanceLabel" ...)` (plain TMP label).
    - Both roots: `ViewSettings.SetTextWriter(writer)`. Full map panel: `SetDistanceText(distanceTmp)`, `SetEtaText(etaTmp)`.
12. Tests
    - `EditMode/TmpTextTargetTests.cs` (delete, with `.meta`) → `EditMode/TmpTextWriterTests.cs` (new): `CanWrite_TmpText_True`, `CanWrite_Image_False`, `Write_UpdatesText` ("1.2 km"), `FindText_FindsInactiveChild`. Create the writer with `ScriptableObject.CreateInstance<TmpTextWriter>()` and destroy it in TearDown.
    - `EditMode/NavigationTextOutputTests.cs` (new): `Write_LegacyText_NoWriter_Updates`, `Write_UnknownComponent_NoWriter_DoesNothing` (an `Image`; no exception), `Write_Null_DoesNothing`, `CanWrite_LegacyText_True`, `CanWrite_Image_NoWriter_False`, `FindText_LegacyChild_Found`.
    - `EditMode/StaticStateTests.cs` (change): `typeof(TMP.TmpTextTarget)` → `typeof(TMP.TmpTextWriter)`.
    - `EditMode/DefaultPrefabsTests.cs` (change): `MarkerPrefabs_Exist` checks the arrow has a `TMPro.TMP_Text` child instead of `NavigationTextTarget`; add `FullMapPrefab_HasOneGleyScript` (same counting as the minimap test); add `TextWriterAsset_ExistsAndIsAssigned` (asset exists in Presets; both roots' `viewSettings.textWriter` reference it).
    - `PlayMode/FakeLabel.cs` (new, test-only `public class FakeLabel : MonoBehaviour`): `public int WriteCount { get; private set; }`, `internal void RecordWrite()` (adds 1).
    - `PlayMode/FakeTextWriter.cs` (new, test-only `public class FakeTextWriter : NavigationTextWriter`, own file because it is a ScriptableObject): `CanWrite` → `target is FakeLabel`; `Write` → `FakeLabel label = target as FakeLabel; if (label != null) { label.RecordWrite(); }`; `FindText` → `root.GetComponentInChildren<FakeLabel>(true)`.
    - `PlayMode/OffScreenArrowTests.cs` (change): delete the nested `FakeTextTarget`. In `SetUp` create `textWriter = ScriptableObject.CreateInstance<FakeTextWriter>()` (destroy it in `TearDown`) and set `host.Settings.SetTextWriter(textWriter)` right after the host is added. Every `AddComponent<FakeTextTarget>()` → `AddComponent<FakeLabel>()`; every `GetComponent...<FakeTextTarget>` → `...<FakeLabel>`; `SetTextCallCount` → `WriteCount`. Keep every assertion and number unchanged.
    - Search `Assets/Tests` for `TextTarget` afterwards: no match may remain.

**Tests:**
- EditMode: `TmpTextWriterTests` (4), `NavigationTextOutputTests` (6), `StaticStateTests`, `DefaultPrefabsTests`.
- PlayMode: `OffScreenArrowTests` (existing tests, unchanged asserts), `TapAndPreviewTests` and the rest stay green.

**Manual checks:**
1. Build Default Prefabs: `Graphics/Presets/TmpTextWriter.asset` exists. In `NavigationFullMap.prefab`, `DistanceText` / `EtaText` have only `TextMeshProUGUI`; `OffScreenArrow.prefab`'s label has only `TextMeshProUGUI`.
2. Create Full Sandbox, Play: preview panel shows distance and ETA; off-screen arrows on both maps show their distance.
3. On a copy of the full map in a scene, put a legacy `Text` (UI > Legacy > Text) into the Distance Text slot: it shows the distance.

---

## S63 — Root inspectors

**Goal:** a first-time user sees the important settings at the top of each root, slots in their own groups and fine-tuning collapsed under **Advanced**. Design: "Root inspector layout (minimap)" and "(full map)".

**Depends on:** S62.

**Files:**

1. `Editor/Inspectors/RootInspectorDrawer.cs` (new, plain `internal class` in `Gley.NavigationSystem.Editor`): small helpers so both editors look the same.
   - `internal void DrawHeader(string title)`: `EditorGUILayout.Space()` + bold label.
   - `internal void DrawProperty(SerializedObject serializedObject, string path)`: `EditorGUILayout.PropertyField(serializedObject.FindProperty(path))`; if the property is null, show `EditorGUILayout.HelpBox("Missing property: " + path, MessageType.Error)` instead (a typo then shows up immediately).
   - `internal void DrawProperties(SerializedObject serializedObject, string[] paths)`: loop over `DrawProperty`.
   - `internal bool DrawAdvancedFoldout(bool open)`: `EditorGUILayout.Space()` + `EditorGUILayout.Foldout(open, "Advanced", true)`; returns the new state.
2. `Editor/Inspectors/NavigationMinimapEditor.cs` (new, `[CustomEditor(typeof(NavigationMinimap))] public class NavigationMinimapEditor : UnityEditor.Editor`)
   - Keep the property paths in two private instance fields `private readonly string[] visiblePaths` and `private readonly string[] advancedPaths` (not static), exposed as `internal string[] VisiblePaths` and `internal string[] AdvancedPaths` so the test can check them. The drawing code may use the path strings directly; the arrays must contain the same strings.
   - Layout, in this order (`serializedObject.Update()` first, `ApplyModifiedProperties()` last):
     - `manager`.
     - Header **Shape**: `shapeSettings.shapeKind`; when the kind is Sprite: `shapeSettings.sprite`, `shapeSettings.spriteOutline`.
     - Header **Rotation**: `followSettings.rotationMode`, `followSettings.carOffsetFromBottom`.
     - Header **Zoom**: `followSettings.speedZoom`; when on: `followSettings.speedZoomMinMeters`, `followSettings.speedZoomMaxMeters`; when off: `followSettings.fixedZoomMeters`.
     - Header **Tap**: `tapAction`; when OpenFullMap: `fullMap` (with a small grey label "Empty = found automatically" under it).
     - Header **Off-screen arrows**: `viewSettings.showOffScreenArrows`; when on: `viewSettings.showArrowDistance`.
     - Header **Compass**: `compassButton`, `compassIcon`.
     - **Advanced** foldout (closed by default, state kept in a private bool field of the editor): `followSettings.rotationSmoothing`, `followSettings.turnSmoothing`, `followSettings.turnAngleThreshold`, `followSettings.noseDeadZoneDegrees`, `followSettings.zoomSmoothing`, `followSettings.speedZoomSlowSpeed`, `followSettings.speedZoomFastSpeed`, `viewSettings.minZoomMeters`, `viewSettings.edgeInset`, `viewSettings.routeStyle`, `viewSettings.arrowPrefab`, `viewSettings.textWriter`, `viewport`, `viewSettings.showPreview`, `viewSettings.channelMask`.
   - `VisiblePaths` lists every non-Advanced path above (all of them, including the conditional ones); `AdvancedPaths` lists the Advanced ones.
   - Shape changes are applied by the root's `OnValidate` (nothing to do here).
3. `Editor/Inspectors/NavigationFullMapEditor.cs` (new, `[CustomEditor(typeof(NavigationFullMap))]`)
   - `manager`.
   - Header **Interaction**: `interactionSettings.builtInPointerInput`, `interactionSettings.confirmStep`, `interactionSettings.openZoomMeters`, `interactionSettings.zoomOutMode`, `interactionSettings.crosshairMode`.
   - Header **Gestures**: `interactionSettings.fling`, `interactionSettings.doubleTapZoom`.
   - Header **Off-screen arrows**: `viewSettings.showOffScreenArrows`; when on: `viewSettings.showArrowDistance`.
   - Header **Preview panel**: `previewPanel.panelRoot`, `previewPanel.distanceText`, `previewPanel.etaText`, `previewPanel.confirmButton`, `previewPanel.cancelButton`. Under each text slot: if it is assigned and `new NavigationTextOutput().CanWrite(component, writer)` is false (writer = `viewSettings.textWriter`'s object reference cast to `NavigationTextWriter`), show `HelpBox("This component can't show text. Assign a TextMeshPro or legacy Text, or set Advanced > Text Writer.", MessageType.Error)`. Keep one `NavigationTextOutput` instance as a field of the editor.
   - Header **Buttons**: `buttons.stopButton`, `buttons.centerButton`, `buttons.closeButton`.
   - Header **Crosshair**: `crosshairImage`.
   - **Advanced**: `interactionSettings.mouseWheelStep`, `interactionSettings.doubleTapStep`, `interactionSettings.markerTapRadius`, `viewSettings.minZoomMeters`, `viewSettings.edgeInset`, `viewSettings.routeStyle`, `viewSettings.arrowPrefab`, `viewSettings.textWriter`, `viewport`, `viewSettings.channelMask`, `viewSettings.showPreview`.
   - `VisiblePaths` / `AdvancedPaths` as for the minimap.
4. Tests: `EditMode/RootInspectorTests.cs` (new)
   - `MinimapEditor_AllPathsExist`: create a GameObject with `NavigationMinimap`, `UnityEditor.Editor.CreateEditor(component)` cast to `NavigationMinimapEditor`; for every path in `VisiblePaths` and `AdvancedPaths`, `editor.serializedObject.FindProperty(path)` is not null (message = the path). Destroy the editor (`Object.DestroyImmediate(editor)`) and the GameObject in TearDown.
   - `FullMapEditor_AllPathsExist`: same for the full map.
   - `MinimapEditor_EverySettingIsShown`: every serialized field of the root and of its settings objects appears in exactly one of the two lists. Get them with a `SerializedProperty` iterator over `serializedObject` (`GetIterator()`, `NextVisible(true)`), skipping `m_Script` and properties whose `propertyType` is `Generic` (the settings containers themselves).
   - `FullMapEditor_EverySettingIsShown`: same.

**Tests:** EditMode `RootInspectorTests` (4).

**Manual checks:**
1. Select the minimap root in a scene: Manager, Shape, Rotation, Zoom, Tap, Off-screen arrows, Compass are visible; Advanced is closed. Switch Shape Kind to Rectangle: Sprite and Sprite Outline disappear. Turn Speed Zoom off: min / max disappear, Fixed Zoom Meters appears.
2. Select the full map root: Interaction, Gestures, Off-screen arrows, Preview panel, Buttons, Crosshair visible; Advanced closed.
3. Drag an `Image` into Distance Text: the red error box appears. Drag the TMP text back: the box disappears.
4. Undo / redo of an inspector change works (Ctrl+Z).
