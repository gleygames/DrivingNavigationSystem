# Phase 14 — Additions (S65–)

Features added after the original plan and the prefab front door, one self-contained step each. Read `00_README.md` (rules, workflow, report format) and the phase 13 "Shared rules" (how settings classes, plain classes and root inspectors are written) before any step here.

The package is **not released**: no migration code. An old scene loads a new serialized field with its field-initializer default.

---

## S65 — Full map tap target: markers only

**Goal:** a full map setting so that only taps on "can be destination" markers start a preview or a route; a tap anywhere else (road or empty map) does nothing. Use case: predefined destinations such as gas station icons. Design: section 12 "Tap target" (decision 2026-10-06), root inspector layout (full map), defaults table.

**Depends on:** S64.

**Background:** today `MapViewInteractive.TapAt` (`Runtime/UI/MapViewInteractive.cs`) first asks `MarkerLayer.FindNearestDestinationMarker(screenPoint, settings.MarkerTapRadius, ...)`; when no marker is in range it falls back to the map point under the tap. Both the pointer path (`GestureTracker` → `Tap` → `TapAt`) and the crosshair path (`ConfirmAtCrosshair` → `TapAt(Vector2.zero)`) and user input calling `TapAt` go through this one method, so the whole feature is one early return there plus the setting.

**Files:**

1. `Runtime/UI/FullMapTapTarget.cs` (new): `public enum FullMapTapTarget { MapAndMarkers, MarkersOnly }`, namespace `Gley.NavigationSystem`, same style as `CrosshairMode.cs`. `MapAndMarkers` must be the first value (0), so it is the default for anything serialized before this step.
2. `Runtime/UI/FullMapInteractionSettings.cs` (change), following "How every settings class is written":
   - Field `[SerializeField] private FullMapTapTarget tapTarget = FullMapTapTarget.MapAndMarkers;` placed directly after `confirmStep`.
   - `public FullMapTapTarget TapTarget { get { return tapTarget; } }` (after `ConfirmStep`).
   - `internal void SetTapTarget(FullMapTapTarget value)` (after `SetConfirmStep`).
3. `Runtime/UI/MapViewInteractive.cs` (change), `TapAt` only:
   - Keep the marker lookup exactly as it is.
   - In the `else` branch (no destination marker found): if `settings.TapTarget == FullMapTapTarget.MarkersOnly`, `return;` before computing the map point. Nothing else changes: no preview is started, moved or cancelled, no event is raised, the active route is untouched.
   - Write it with braces and `if`, no ternary (C# conventions).
4. `Editor/Inspectors/NavigationFullMapEditor.cs` (change)
   - Add `"interactionSettings.tapTarget"` to the visible paths array directly after `"interactionSettings.confirmStep"`.
   - Draw it in the **Interaction** header directly after `confirmStep`.
   - When its value is `MarkersOnly` (`property.enumValueIndex == (int)FullMapTapTarget.MarkersOnly`), show under it `EditorGUILayout.HelpBox("Only markers with Can Be Destination on start a route. Tap radius: Advanced > Marker Tap Radius.", MessageType.Info)`.
   - `markerTapRadius` stays under **Advanced**.
5. Tests
   - `PlayMode/TapAndPreviewTests.cs` (change): add the tests below. Reuse the existing `SetUp`, `CreateMarker`, `ComputeViewportPoint` and `WaitFrames` helpers; set the target with `rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly)` at the start of each test. Use `yield return WaitFrames(3)` after creating a marker, as the existing marker tests do (the marker layer needs frames to see it). Do not change the existing tests.
     - `MarkersOnly_TapOnEmptyMap_NothingHappens`: subscribe `PreviewReady` and `PreviewFailed` (each sets a bool); `TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)))` with no marker; `yield return null`; both bools false, `manager.HasPreview` false, `manager.HasActiveRoute` false.
     - `MarkersOnly_TapNearDestinationMarker_PreviewsMarker`: like `TapNearDestinationMarker_UsesMarkerPosition_ReportsMarker` (marker at 60, tap at 62); the reported marker is the created one and `manager.HasPreview` is true.
     - `MarkersOnly_TapNearNonDestinationMarker_NothingHappens`: `CreateMarker(new Vector3(60f, 0f, 0f), false)`, wait, tap at 60; `HasPreview` false.
     - `MarkersOnly_TapOnEmptyMap_KeepsExistingPreview`: marker at 60, wait, tap at 62 → preview; remember `manager.Markers.GetEntry(manager.PreviewMarkerIndex).TruePosition`; tap at `new Vector3(100f, 0f, 0f)` (on the road and inside the view, 40 m = 80 canvas units from the marker, outside the 40-unit tap radius); `yield return null`; `HasPreview` still true and the preview pin position is unchanged (`Vector3.Distance < 0.01f`).
     - `MarkersOnly_ConfirmStepOff_TapOnEmptyMap_NoNavigation`: `SetConfirmStep(false)`; subscribe `NavigationStarted`; tap at 60 with no marker; not started, `HasActiveRoute` false.
     - `MarkersOnly_ConfirmAtCrosshair_NoMarker_NothingHappens`: `interactive.SetCrosshairMode(true)`; `interactive.ConfirmAtCrosshair()`; `yield return null`; `HasPreview` false.
     - `MapAndMarkers_TapOnEmptyMap_StillPreviews`: `SetTapTarget(FullMapTapTarget.MapAndMarkers)` explicitly, tap at 60 with no marker; `HasPreview` true (guards the default path against the new early return).
   - `EditMode/FullMapInteractionSettingsTests.cs` (new): `TapTarget_DefaultIsMapAndMarkers`: `new FullMapInteractionSettings().TapTarget == FullMapTapTarget.MapAndMarkers`; `SetTapTarget_ChangesValue`: set `MarkersOnly`, read it back.
   - `EditMode/RootInspectorTests.cs`: no change. `FullMapEditor_AllPathsExist` and `FullMapEditor_EverySettingIsShown` must pass with the new field (they fail if step 4 is missed).

**Not in this step:** an event or feedback for a missed tap, a runtime-public setter, any minimap change.

**Tests:**
- EditMode: `FullMapInteractionSettingsTests` (2), `RootInspectorTests` (all existing), everything else green.
- PlayMode: `TapAndPreviewTests` (9 existing + 7 new), everything else green.

**Manual checks:**
1. Select the full map root: **Tap Target** shows under Confirm Step in Interaction, value Map And Markers. Set it to Markers Only: the info box appears. Set it back: the box disappears. Ctrl+Z works.
2. Full Sandbox: add a `MapMarker` to a GameObject next to a road, **Can Be Destination** on (empty prefab = default marker). Set the full map's Tap Target to Markers Only. Play, open the full map:
   - tap a road away from the marker: no pin, no preview panel;
   - tap the marker icon: preview to the marker; Confirm starts navigation;
   - while navigating, tap a road: the route keeps running, nothing changes;
   - double-tap still zooms; drag still pans.
3. Set Tap Target back to Map And Markers, Play: tapping a road previews a route again (as before).
4. If a gamepad is connected: in crosshair mode, confirm with the crosshair over empty map does nothing; over the marker it previews the marker.

---

## Marker tap and visual binding (S66–S73)

Design: section 11 "Marker tap action", "Selection", "Destination marker link", "Visual binding", "Labels", "Selected look", "Runtime data changes" (all decisions 2026-10-07); section 12 "Tap target" (a missed tap clears the selection); section 14 events and API tables; default values table.

Shared facts for S66–S73:
- Selection lives in the **Manager**. The full map only asks for it (tap → `SelectMarker` / `ClearSelection`).
- Selection changes go through the **command queue** like every other Manager call (`RunOrQueue`), so a call from inside an event handler is applied after the current events.
- Marker visuals are pooled **per view and per prefab** in `MarkerLayer`. A visual instance is "bound" while it shows one entry and "unbound" when it goes back to the pool.
- The package is not released: rename or remove fields freely, no migration.

---

## S66 — Default marker prefab reaches the marker layer

**Goal:** a `MapMarker` with an empty Prefab shows the shipped `DefaultMarker.prefab`. Today it shows nothing: `MarkerLayer.defaultMarkerPrefab` is never assigned (see the S53 note). Design: section 11 "Two kinds of marker entries" (four prefab slots in Navigation Settings).

**Depends on:** S65.

**Files:**

1. `Runtime/Core/NavigationRuntimeSettings.cs` (change): `[SerializeField] private GameObject defaultMarkerPrefab;` directly after `previewPinPrefab`; `public GameObject DefaultMarkerPrefab { get { return defaultMarkerPrefab; } }` after `PreviewPinPrefab`; `internal void SetDefaultMarkerPrefab(GameObject value)` after `SetPreviewPinPrefab`. `ResetTuningToDefaults` keeps it (it keeps references).
2. `Runtime/UI/MarkerLayer.cs` (change): remove the `defaultMarkerPrefab` field and `SetDefaultMarkerPrefab` (nothing calls it). In `AcquireInstance(prefab)`: when `prefab` is null use `view.Manager.RuntimeSettings.DefaultMarkerPrefab` (the Manager is never null there; `UpdateMarkerLayerVisuals` returns early without one). Still null → return null as today.
3. `Editor/Inspectors/NavigationManagerEditor.cs` (change): add `"runtime.defaultMarkerPrefab"` to `projectPaths` directly after `"runtime.previewPinPrefab"`.
4. `Editor/Setup/NavigationSetupWindow.cs` (change): new field `defaultMarkerPrefabPath` next to `previewPinPrefabPath`, set in `ResolvePaths` to `prefabFolder + "/DefaultMarker.prefab"`; in `EnsureSettingsDefaults` add `changed |= AssignPrefabIfMissing(serializedObject, "runtime.defaultMarkerPrefab", defaultMarkerPrefabPath);` after the preview pin line.
5. `Assets/Tests/NavigationSystem/Dev/Editor/DevUiInstaller.cs` (change): `AssignAssetIfMissing(serializedObject, "runtime.defaultMarkerPrefab", LoadPrefab("DefaultMarker"));` after the preview pin line.
6. Tests
   - `EditMode/NavigationRuntimeSettingsTests.cs`: `Defaults_MatchDesign` also asserts `DefaultMarkerPrefab` is null; `ResetTuningToDefaults_ResetsNumbers_KeepsReferences` also sets and checks `DefaultMarkerPrefab`.
   - `PlayMode/MarkerLayerTests.cs`: add a helper `CreateObjectMarkerWithoutPrefab(Vector3 position)` (like `CreateObjectMarker` but `SetPrefab(null)`). New `EmptyPrefab_UsesSettingsDefaultMarker`: `settings.Runtime.SetDefaultMarkerPrefab(markerTemplate)`, view set up as in `MarkerInView_Shown`, marker in view, wait 3 frames → `GetActiveInstance(index)` not null. New `EmptyPrefab_NoDefault_ShowsNothing`: same without the default → null, no error logged.
   - `EditMode/RootInspectorTests.cs`: no change; the Manager project-paths test must pass (it fails if file 3 is missed).

**Not in this step:** any change to the default marker prefab itself.

**Tests:** EditMode `NavigationRuntimeSettingsTests`, `RootInspectorTests`; PlayMode `MarkerLayerTests` (+2). Everything else green.

**Manual checks:**
1. Manager > Project-wide foldout (or the Navigation Settings asset): **Default Marker Prefab** is listed under Preview Pin Prefab. Open the Setup window: the slot gets `DefaultMarker` if it was empty.
2. Full Sandbox: add a `MapMarker` to a GameObject near a road, Prefab empty. Play: the default marker icon shows on the minimap and the full map.

---

## S67 — Marker tap action and selection

**Goal:** replace the "Can be destination" checkbox with a **Tap action** (None / Select / Destination) and add the Manager's selected marker: `SelectedMarker`, `MarkerSelected`, `MarkerDeselected`, `ClearSelection()`. A tap on a Select marker only selects it; a tap on a Destination marker selects it and then previews (or starts) the route exactly as today. A tap that hits no marker clears the selection. Design: section 11 "Marker tap action", "Selection"; section 12 "Tap target"; section 14 tables.

**Depends on:** S66.

**Files:**

1. `Runtime/Markers/MarkerTapAction.cs` (new): `public enum MarkerTapAction { None, Select, Destination }` (`None` first = default), same style as `MarkerRotationMode.cs`.
2. `Runtime/Markers/MapMarker.cs` (change): replace `canBeDestination` / `CanBeDestination` / `SetCanBeDestination` with `[SerializeField] private MarkerTapAction tapAction = MarkerTapAction.None;`, `public MarkerTapAction TapAction { get { return tapAction; } }`, `internal void SetTapAction(MarkerTapAction value)`, in the same positions.
3. `Runtime/Markers/MarkerEntry.cs` and `MarkerRegistry.cs` (change): `CanBeDestination` → `public MarkerTapAction TapAction { get; set; }`. `AddObject` copies `marker.TapAction`; `AddPoint` and `EnsurePlayer` set `MarkerTapAction.None`.
4. `Runtime/UI/MarkerLayer.cs` (change): rename `FindNearestDestinationMarker` → `FindNearestTappableMarker(Vector2 viewportPoint, float radius, out MapMarker marker, out Vector3 truePosition, out MarkerTapAction tapAction)`. Set `tapAction = MarkerTapAction.None` at the start; skip entries with `Marker == null` or `TapAction == MarkerTapAction.None`; otherwise unchanged (closest within radius wins); return the winner's `TapAction`.
5. `Runtime/Core/NavigationCommandType.cs` (change): append `SelectMarker`, `ClearSelection` at the end.
6. `Runtime/Navigation/NavigationManager.cs` (change):
   - Field `private MapMarker selectedMarker;` next to `previewMarker`. Property `public MapMarker SelectedMarker { get { return selectedMarker; } }` after `HasPreview`.
   - Events `public event Action<MapMarker> MarkerSelected;` and `public event Action<MapMarker> MarkerDeselected;` after `BackInsideMap`.
   - After `RemoveMarker`: `internal void SelectMarker(MapMarker marker)` → `RunOrQueue(new NavigationCommand(NavigationCommandType.SelectMarker, marker))`; `public void ClearSelection()` → `RunOrQueue(new NavigationCommand(NavigationCommandType.ClearSelection, null))`.
   - `ExecuteCommand`: two new cases calling `ExecuteSelectMarker(command.Marker)` and `ExecuteClearSelection()`.
   - `ExecuteSelectMarker(marker)`: null → `ExecuteClearSelection()` and return. Same as `selectedMarker` → return (no events). Otherwise `ExecuteClearSelection()` first (it does nothing when none is selected), then `selectedMarker = marker; RaiseMarkerSelected(marker);`.
   - `ExecuteClearSelection()`: nothing selected → return. Else keep the old one in a local, set the field to null **before** raising, then `RaiseMarkerDeselected(old)`.
   - `ExecuteRemoveMarker(marker)`: after `markers.RemoveObject(marker)`, if `marker == selectedMarker` → `ExecuteClearSelection()`. (Disabled or destroyed markers already call `RemoveMarker` from `MapMarker.OnDisable`.)
   - `RaiseMarkerSelected` / `RaiseMarkerDeselected`: same shape as `RaisePreviewCanceled` (BeginDispatch / try / finally).
7. `Runtime/UI/MapViewInteractive.cs` (change):
   - `TapAt`: look up with `FindNearestTappableMarker(...)`. Marker found → `activeManager.SelectMarker(marker);` then if `tapAction != MarkerTapAction.Destination` → `return;`; otherwise compute `worldPoint` from the marker position as today. No marker → `activeManager.ClearSelection();` first, then the existing `MarkersOnly` early return, then the map point as today.
   - `Disable()`: call `manager.ClearSelection()` before `CancelPreview()` (closing the full map clears the selection).
8. `Runtime/Navigation/NavigationEvents.cs` (change): `[Serializable] public class MapMarkerUnityEvent : UnityEvent<MapMarker> { }` next to the other event classes; fields `onMarkerSelected` and `onMarkerDeselected` after `onBackInsideMap`; properties `MarkerSelected` / `MarkerDeselected`; forwarders; subscribe in `OnEnable` and unsubscribe in `OnDisable` like the others.
9. `Editor/Inspectors/NavigationFullMapEditor.cs` (change): the Markers-only help box text becomes `"Only markers with Tap Action Select or Destination react to taps. Tap radius: Advanced > Marker Tap Radius."`.
10. Tests. Every existing marker helper switches from `bool canBeDestination` to `MarkerTapAction tapAction` (`true` → `Destination`, `false` → `None`).
   - `PlayMode/TapAndPreviewTests.cs` and `PlayMode/CrosshairTests.cs`: update `CreateMarker` and its callers; existing tests keep their names and must still pass.
   - `PlayMode/TapAndPreviewTests.cs` new tests. Subscribe `MarkerSelected` / `MarkerDeselected` with handler methods that append `"Selected:" + name` / `"Deselected:" + name` to a `List<string> events` (reset in `SetUp`); `HandlePreviewReady` also appends `"PreviewReady"`.
     - `TapSelectMarker_SelectsWithoutPreview`: Select marker at 60, wait 3, tap at 62 → `SelectedMarker` is it, `HasPreview` false.
     - `TapDestinationMarker_SelectsThenPreviews`: events are `Selected`, then `PreviewReady`; `HasPreview` true.
     - `TapOtherMarker_DeselectsThenSelects`: Select markers A at 60 and B at 200 (both in view), tap A then B → `Selected:A`, `Deselected:A`, `Selected:B`.
     - `TapSameMarkerTwice_OneSelectedEvent`.
     - `TapEmptyMap_ClearsSelectionAndPreviews` (Map and markers).
     - `MarkersOnly_TapEmptyMap_ClearsSelectionKeepsPreview`: tap a Destination marker (preview + selected), then tap at 100 (outside the radius) → `SelectedMarker` null, `HasPreview` still true.
     - `DisableSelectedMarker_Deselects`: select, `SetActive(false)` on the marker object, `yield return null` → deselected.
     - `CloseFullMap_ClearsSelection`: select, `rig.Root.SetActive(false)` → `SelectedMarker` null.
     - `ClearSelection_NothingSelected_NoEvent`.
     - `ClearSelectionInsideHandler_IsQueued`: in the `PreviewReady` handler call `manager.ClearSelection()`; tap a Destination marker → events `Selected`, `PreviewReady`, `Deselected`; `SelectedMarker` null.
   - `PlayMode/NavigationEventsTests.cs`: one new test that the `MarkerSelected` / `MarkerDeselected` UnityEvents fire with the marker (follow the file's existing pattern; call `manager.SelectMarker` / `ClearSelection` directly).

**Not in this step:** Cancel/Confirm deselecting (S68); any visual change (S69+); Display name (S71).

**Tests:** PlayMode `TapAndPreviewTests` (all existing + 10), `CrosshairTests`, `NavigationEventsTests` (+1); EditMode `RootInspectorTests`. Everything else green.

**Manual checks:**
1. Select a `MapMarker`: a **Tap Action** dropdown (None / Select / Destination) replaces Can Be Destination.
2. Full Sandbox: add a `NavigationEvents` component and wire **On Marker Selected** / **On Marker Deselected** to something visible (e.g. `GameObject.SetActive` on a test cube). Two markers with empty Prefab: one Select, one Destination. Play, open the full map:
   - tap the Select marker: selected, no route preview;
   - tap the Destination marker: the first is deselected, the second selected, the preview appears;
   - tap an empty road: deselected, the preview moves to the road;
   - set Tap Target to Markers Only and tap empty map: deselected, the preview stays;
   - close the full map: deselected.

---

## S68 — Destination marker link: Cancel and Confirm deselect

**Goal:** when the current preview came from the selected Destination marker, Cancel and Confirm both clear the selection. In instant mode (Confirm step off) the marker is selected, navigation starts, and the selection is cleared in the same frame. A failed preview or a failed start keeps the selection. Design: section 11 "Destination marker link".

**Depends on:** S67.

**Files:**

1. `Runtime/Navigation/NavigationManager.cs` (change):
   - New `private void ClearSelectionIfPreviewMarker()`: if `previewMarker != null && previewMarker == selectedMarker` → `ExecuteClearSelection()`.
   - `ExecuteCancelPreview`: after `RaisePreviewCanceled()`, call `ClearSelectionIfPreviewMarker()`, then `previewMarker = null`.
   - `ExecuteConfirmPreview`: on success, after `BeginNavigationFromScratch(...)`, call `ClearSelectionIfPreviewMarker()`, then `previewMarker = null`. On failure (`RouteFailed`) change nothing.
   - `DeactivateMap`: in the `hasPreview` block, after `RaisePreviewCanceled()`, call `ClearSelectionIfPreviewMarker()` and set `previewMarker = null`.
   - `ExecuteStartNavigation(worldPoint, marker)`: on success, after `BeginNavigationFromScratch(...)` (this also covers the arrived-immediately case), if `marker != null && marker == selectedMarker` → `ExecuteClearSelection()`. On failure keep the selection.
   - Order everywhere: the route event first (`PreviewCanceled` / `NavigationStarted` / `Arrived`), then `MarkerDeselected`.
2. Tests: `PlayMode/TapAndPreviewTests.cs` new tests (extend the S67 `events` list with `"PreviewCanceled"`, `"NavigationStarted"`, `"PreviewFailed"`):
   - `DestinationMarker_Cancel_Deselects`: tap a Destination marker, `manager.CancelPreview()` → `…, PreviewCanceled, Deselected`; `SelectedMarker` null.
   - `DestinationMarker_Confirm_Deselects`: … `manager.ConfirmPreview()` → `…, NavigationStarted, Deselected`.
   - `DestinationMarker_ConfirmStepOff_SelectStartDeselect`: Confirm step off, tap → `Selected, NavigationStarted, Deselected`.
   - `SelectMarker_CodePreview_CancelKeepsSelection`: tap a Select marker, then `manager.PreviewDestination(point)` from code (selection stays), `CancelPreview()` → still selected (that preview did not come from the marker).
   - `DestinationMarker_PreviewFails_KeepsSelection`: Destination marker far from every road (e.g. `(150, 0, 150)`, beyond the 50 m destination snap) → `PreviewFailed`, `SelectedMarker` still the marker.

**Not in this step:** visuals.

**Tests:** PlayMode `TapAndPreviewTests` (+5). Everything else green.

**Manual checks:** the S67 sandbox setup. Tap a Destination marker → Cancel: deselected. Tap it again → Confirm: navigation starts, deselected. Turn Confirm Step off and tap it: navigation starts; the selected object flashes on and off in the same frame (nothing stays selected).

---

## S69 — Marker visual binding: `IMapMarkerVisual`

**Goal:** a component on a marker prefab learns which marker its pooled instance shows: `Bind(marker, view)` when the instance is taken for a marker, `Unbind()` when it goes back. Also fix index reuse: today, if a marker is removed and another is added in the same frame, the new marker reuses the entry index and keeps the old instance (wrong prefab, and now a wrong binding). Design: section 11 "Visual binding".

**Depends on:** S68.

**Files:**

1. `Runtime/Markers/IMapMarkerVisual.cs` (new): `public interface IMapMarkerVisual { void Bind(MapMarker marker, MapView view); void Unbind(); }`. `marker` is null for point markers (destination, preview pin) and for the player.
2. `Runtime/Markers/MarkerEntry.cs` (change): `public int Generation { get; set; }`.
3. `Runtime/Markers/MarkerRegistry.cs` (change): `AcquireIndex` increments the entry's `Generation` for both a reused and a new entry, so a reused index never repeats a generation.
4. `Runtime/UI/MarkerLayer.cs` (change):
   - `private readonly Dictionary<GameObject, IMapMarkerVisual[]> visualsOf` and `private readonly Dictionary<int, int> activeGenerations`.
   - In `AcquireInstance`, after `AcquireFromPool`: if the instance is not in `visualsOf`, add `instance.GetComponentsInChildren<IMapMarkerVisual>(true)` (once per instance). Arrow instances are not touched.
   - In the per-entry loop, before the `activeInstances.TryGetValue`: if the index is active but `activeGenerations[index] != entry.Generation` → `ReleaseInstance(index)`, so a fresh instance is acquired for the new entry.
   - When an instance is added to `activeInstances`: `activeGenerations[index] = entry.Generation`, then `Bind(entry.Marker, view)` on each of its visuals.
   - `ReleaseInstance(index)`: before `ReturnToPool`, `Unbind()` each visual of the instance; remove `activeGenerations[index]`.
   - No per-frame allocation, no per-frame `GetComponent`.
5. Tests
   - `PlayMode/FakeMarkerVisual.cs` (new, test assembly): `MonoBehaviour, IMapMarkerVisual`; public get-only `BindCount`, `UnbindCount`, `BoundMarker`, `BoundView`, `IsBound`.
   - `PlayMode/MarkerLayerTests.cs` new tests (add `FakeMarkerVisual` to `markerTemplate` before creating markers):
     - `MarkerEntersView_BoundToMarkerAndView`: the instance's fake has `BindCount == 1`, `BoundMarker == marker`, `BoundView == view`.
     - `MarkerLeavesView_Unbound`: move the (non-static) marker far away, wait → `UnbindCount == 1`, `IsBound` false.
     - `PooledInstanceReused_RebindsToNewMarker`: A in view; then A out and B in → the reused instance is bound to B (`BindCount == 2`, `BoundMarker == B`).
     - `IndexReusedSameFrame_NewInstanceForNewMarker`: two templates, each with a fake. A (template A) in view, wait 3. In one frame disable A and create B (template B) at the same position; assert B got A's old index (guard), wait 3 → the active instance's fake has `BoundMarker == B`, and A's old instance has `UnbindCount == 1`.
     - `PointMarker_BoundWithNullMarker`: `settings.Runtime.SetDestinationMarkerPrefab(markerTemplate)`; start navigation to a road point inside the view → the destination instance is bound with `BoundMarker == null`.

**Not in this step:** selection visuals (S70), labels (S72).

**Tests:** PlayMode `MarkerLayerTests` (+5), `OffScreenArrowTests`. Everything else green.

**Manual checks:** no visible change. Optional: Perf scene (**Tools > Gley > Navigation Dev > Create Perf Scene**) for one phase cycle; the Profiler shows no new GC.Alloc under `Gley.Nav.MarkerLayer` after warm-up.

---

## S70 — Selected marker look: `IMapMarkerSelectable`

**Goal:** the selected marker's visual is told it is selected and is drawn above the other markers; the default marker prefab scales ×1.25 while selected. Design: section 11 "Selected look", default values table.

**Depends on:** S69.

**Files:**

1. `Runtime/Markers/IMapMarkerSelectable.cs` (new): `public interface IMapMarkerSelectable { void SetSelected(bool selected); }`.
2. `Runtime/Markers/MarkerSelectionScale.cs` (new): `public class MarkerSelectionScale : MonoBehaviour, IMapMarkerSelectable`; `[SerializeField] private float selectedScale = 1.25f;` + `SelectedScale` property + `internal SetSelectedScale`. `SetSelected(true)` → `transform.localScale = Vector3.one * selectedScale`; `false` → `Vector3.one` (if/else).
3. `Runtime/UI/MarkerLayer.cs` (change):
   - `private readonly Dictionary<GameObject, IMapMarkerSelectable[]> selectablesOf`, filled together with `visualsOf`.
   - `private MapMarker shownSelection;`. Each `UpdateMarkerLayerVisuals` reads `manager.SelectedMarker`; when it differs from `shownSelection` (`ReferenceEquals`, skips Unity's null check), call `SetSelected` with the new state on every active instance whose entry `Marker` is the old or the new selection, then store it.
   - After binding a new instance: `SetSelected(entry.Marker != null && ReferenceEquals(entry.Marker, manager.SelectedMarker))` on its selectables (this also resets reused pool instances).
   - After the positioning loop: if the selected marker has an active instance that is not the last sibling (`GetSiblingIndex() != transform.childCount - 1`) → `SetAsLastSibling()`.
4. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change): the `DefaultMarker` prefab gets a `MarkerSelectionScale` on its root (separate `CreateDefaultMarkerPrefab()` that calls the shared code, or a parameter); the other three marker prefabs stay unchanged.
5. Tests
   - `PlayMode/FakeMarkerVisual.cs`: also implements `IMapMarkerSelectable`; public `IsSelected`, `SetSelectedCount`.
   - `PlayMode/MarkerLayerTests.cs`: `SelectMarker_VisualSelected` (`manager.SelectMarker(marker)`, wait 1 → `IsSelected`); `ClearSelection_VisualUnselected`; `SelectOther_OldUnselectedNewSelected`; `SelectedMarker_IsLastSibling` (three markers in view, select the first one created, wait → its instance has the highest sibling index of the three); `ReusedInstance_StartsUnselected` (select A, A leaves the view, B enters and reuses the instance → B's fake `IsSelected` false).
   - `EditMode/MarkerSelectionScaleTests.cs` (new): default 1.25; `SetSelected(true)` → scale 1.25; `SetSelected(false)` → 1.
   - `EditMode/DefaultPrefabsTests.cs`: `DefaultMarker_HasSelectionScale`.

**Not in this step:** labels.

**Tests:** PlayMode `MarkerLayerTests` (+5); EditMode `MarkerSelectionScaleTests` (3), `DefaultPrefabsTests` (+1). Everything else green.

**Manual checks:** run **Tools > Gley > Navigation Dev > Build Default Prefabs**. Full Sandbox, two Select markers close together, Prefab empty. Play, open the full map, tap one: it grows and draws on top of the other. Tap empty map: it shrinks back.

---

## S71 — Display name and visual refresh

**Goal:** `MapMarker` gets an optional **Display name** and a way to refresh its visuals at runtime. Setting `DisplayName` or calling `RefreshVisuals()` re-binds the marker's active visuals in every view on their next update (several changes in one frame = one re-bind). Design: section 11 "Map Marker" (Display name), "Runtime data changes".

**Depends on:** S70.

**Files:**

1. `Runtime/Markers/MapMarker.cs` (change):
   - `[SerializeField] private string displayName = "";` directly after `prefab`.
   - `public string DisplayName { get { return displayName; } set { displayName = value; RefreshVisuals(); } }`.
   - `public void RefreshVisuals()`: if `cachedManager != null` → `cachedManager.RefreshMarkerVisuals(this)`.
   - `private void OnValidate()`: if `Application.isPlaying` → `RefreshVisuals()` (Inspector edits in Play mode show at once).
2. `Runtime/Markers/MarkerEntry.cs` (change): `public int VisualVersion { get; set; }`.
3. `Runtime/Markers/MarkerRegistry.cs` (change): `public void MarkVisualsChanged(MapMarker marker)`: index from `markerToIndex`; found → `entries[index].VisualVersion++`.
4. `Runtime/Navigation/NavigationManager.cs` (change): `internal event Action<MapMarker> MarkerVisualsChanged;` and `internal void RefreshMarkerVisuals(MapMarker marker)` → `markers.MarkVisualsChanged(marker)`, then raise `MarkerVisualsChanged(marker)` (BeginDispatch / try / finally). Not queued: it changes no navigation state.
5. `Runtime/UI/MarkerLayer.cs` (change): `private readonly Dictionary<int, int> activeVisualVersions`. Store `entry.VisualVersion` when binding. Each frame, for an already active instance whose stored version differs: `Unbind()` its visuals, then `Bind(...)` and `SetSelected(...)` exactly as for a new instance, and store the new version. Same instance, no pool change. Remove the key in `ReleaseInstance`. Put the bind + select code in one private method used by both paths.
6. Tests
   - `PlayMode/MarkerLayerTests.cs`: `RefreshVisuals_RebindsSameInstance` (fake `BindCount` 1 → 2, same instance object); `DisplayNameSet_Rebinds`; `ThreeChangesOneFrame_OneRebind` (set `DisplayName` three times in one frame, wait 1 → `BindCount == 2`); `RefreshVisuals_DisabledMarker_NoError` (call it on a disabled marker: nothing happens, no exception).
   - `PlayMode/MapMarkerTests.cs`: `DisplayName_DefaultEmpty`.

**Not in this step:** anything that shows the name (S72, S73).

**Tests:** PlayMode `MarkerLayerTests` (+4), `MapMarkerTests` (+1). Everything else green.

**Manual checks:** select a `MapMarker`: a **Display Name** text field under Prefab. No visible change in Play mode yet.

---

## S72 — Marker labels

**Goal:** a built-in `MarkerLabel` component shows the marker's Display name next to its icon. The default marker prefab includes it. Per-view **Show marker labels**: full map on, minimap off. Design: section 11 "Labels", root inspector layouts, default values table.

**Depends on:** S71.

**Files:**

1. `Runtime/UI/MapViewSettings.cs` (change): `[SerializeField] private bool showMarkerLabels = true;` after `showArrowDistance`; `ShowMarkerLabels` property; `internal SetShowMarkerLabels`. The constructor `(int channelMask, bool showPreview)` becomes `(int channelMask, bool showPreview, bool showMarkerLabels)`; the parameterless constructor keeps `true`.
2. `Runtime/UI/NavigationMinimap.cs` → `new MapViewSettings(MapViewSettings.MinimapChannelBit, false, false)`; `Runtime/UI/NavigationFullMap.cs` → `new MapViewSettings(MapViewSettings.FullMapChannelBit, true, true)`. `EditMode/MapViewSettingsTests.Constructor_SetsChannelAndPreview` passes the new argument.
3. `Runtime/UI/MapView.cs` (change): `public bool ShowMarkerLabels { get { return settings.ShowMarkerLabels; } }` next to `ShowArrowDistance`.
4. `Runtime/UI/MarkerLayer.cs` (change): `private bool shownMarkerLabels` + `private bool hasShownMarkerLabels`. When `view.ShowMarkerLabels` differs from the stored value, re-bind every active instance (the S71 re-bind method), then store it. This keeps "never copy a setting" true for a runtime toggle.
5. `Runtime/Markers/MarkerLabel.cs` (new): `[DefaultExecutionOrder(101)] public class MarkerLabel : MonoBehaviour, IMapMarkerVisual`.
   - `[SerializeField] private Component text;` + `Text` property + `internal SetText(Component value)`.
   - `private readonly NavigationTextOutput textOutput = new NavigationTextOutput();`, `private readonly StringBuilder scratch = new StringBuilder(32);`, `private Transform layerTransform;`.
   - `Bind(marker, view)`: `text == null` → return. Visible = `marker != null && view.ShowMarkerLabels && !string.IsNullOrEmpty(marker.DisplayName)`. Show or hide with `((Behaviour)text).enabled` — never `SetActive`, because the text may sit on the marker root. When visible: `scratch.Length = 0; scratch.Append(marker.DisplayName); textOutput.Write(text, view.TextWriter, scratch);`. Then `layerTransform = transform.parent;` and `enabled = visible && marker.RotationMode != MarkerRotationMode.Upright`.
   - `LateUpdate()` (runs only while enabled, i.e. a visible label on a rotating marker): `text.transform.rotation = layerTransform.rotation;`. Order 101 runs after the map roots (99) have placed the markers this frame.
   - `Unbind()`: `text == null` → return; hide the text; `enabled = false`.
6. `Editor/Inspectors/NavigationMinimapEditor.cs` and `NavigationFullMapEditor.cs` (change): add `"viewSettings.showMarkerLabels"` to `visiblePaths` after `"viewSettings.showArrowDistance"`; draw it under a new header **Markers** placed right before **Off-screen arrows**.
7. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change): the default marker prefab gets a child `Label` (reuse `CreateTmpText`: anchor (0.5, 0), position (0, -14), size (160, 24), font 14, white; then set its text to `""`) and a `MarkerLabel` on the root with `SetText(label)`.
8. Tests
   - `EditMode/MapViewSettingsTests.cs`: `ShowMarkerLabels_DefaultTrue`; the constructor test covers `false`.
   - `PlayMode/MarkerLabelTests.cs` (new; copy the `MarkerLayerTests` setup; the template gets a child with a legacy `UnityEngine.UI.Text` and a `MarkerLabel` pointing at it. `NavigationTextOutput` writes legacy `Text` directly, so `text.text` can be asserted):
     - `NameSet_ShowsName`; `EmptyName_TextDisabled`; `ViewLabelsOff_TextDisabled`; `ToggleViewLabelsAtRuntime_Rebinds` (off → on shows the name without re-creating the marker); `Rename_UpdatesText`; `PointMarker_NoLabel`; `FollowHeadingMarker_LabelStaysUpright` (marker object rotated 90° around Y → the label's `transform.rotation` equals the layer's rotation within 0.01°); `UprightMarker_LabelComponentDisabled`.
   - `EditMode/DefaultPrefabsTests.cs`: `DefaultMarker_HasLabelWithText` (MarkerLabel present, `Text` assigned to a TMP component).
   - `EditMode/RootInspectorTests.cs`: no change; must pass.

**Not in this step:** label overlap handling (deferred in the design); labels on off-screen arrows.

**Tests:** EditMode `MapViewSettingsTests`, `DefaultPrefabsTests`, `RootInspectorTests`; PlayMode `MarkerLabelTests` (8). Everything else green.

**Manual checks:**
1. **Build Default Prefabs**. Both roots show **Markers > Show Marker Labels**: on for the full map, off for the minimap.
2. Full Sandbox: two markers with empty Prefab, one with Display Name "Acme", one with none. Play: the full map shows "Acme" under the first icon and nothing under the second; the minimap shows no labels.
3. In Play mode change the Display Name in the Inspector: the label updates. Untick Show Marker Labels on the full map root: labels disappear; tick it: they return.
4. Give a moving marker (e.g. with `DevMarkerMover`) Rotation Mode Follow Heading and a name: the icon turns, the label stays upright.

---

## S73 — Default info panel

**Goal:** the full map gets an **Info panel** slot group (panel, title text, Close button). It shows when a marker with a Display name is selected and hides on deselect; Close clears the selection; it follows a rename of the selected marker. Empty slots = off. Design: section 11 "Marker tap action" and the full map root inspector layout (Info panel).

**Depends on:** S72.

**Files:**

1. `Runtime/UI/InfoPanelSlots.cs` (new): settings class written like `PreviewPanelSlots`: `panelRoot` (GameObject), `titleText` (Component), `closeButton` (Button); properties `PanelRoot`, `TitleText`, `CloseButton`; internal setters.
2. `Runtime/UI/InfoPanel.cs` (new): `internal class`, same structure as `PreviewPanel` (constructor `(InfoPanelSlots slots, MapViewSettings viewSettings)`, `Enable(manager)`, `Disable()`).
   - `Enable`: subscribe `MarkerSelected`, `MarkerDeselected`, `MarkerVisualsChanged`; add the Close listener (→ `manager.ClearSelection()`); then `Refresh(manager.SelectedMarker)`. No Manager → hide.
   - `Refresh(marker)`: visible = `marker != null && !string.IsNullOrEmpty(marker.DisplayName)`; `PanelRoot.SetActive(visible)` (null-checked); when visible write the name to `TitleText` through `NavigationTextOutput` with `viewSettings.TextWriter` (reused `StringBuilder`).
   - Handlers: selected → `Refresh(marker)`; deselected → `Refresh(null)`; visuals changed → if `ReferenceEquals(marker, manager.SelectedMarker)` → `Refresh(marker)`.
   - `Disable`: unsubscribe, remove the listener, hide the panel.
3. `Runtime/UI/NavigationFullMap.cs` (change): `[SerializeField] private InfoPanelSlots infoPanel = new InfoPanelSlots();` after `previewPanel`; `public InfoPanelSlots InfoPanelSlots { get { return infoPanel; } }`; create `InfoPanel` in `EnsureParts`; `Enable(cachedManager)` right after the preview panel; `Disable()` right before it.
4. `Editor/Inspectors/NavigationFullMapEditor.cs` (change): `"infoPanel.panelRoot"`, `"infoPanel.titleText"`, `"infoPanel.closeButton"` in `visiblePaths` after the preview panel paths; header **Info panel** after **Preview panel**; `DrawTextSlotWarning("infoPanel.titleText")`.
5. `Assets/Tests/NavigationSystem/Dev/Editor/DefaultPrefabBuilder.cs` (change): `CreateInfoPanel(viewport, fullMap)` after `CreatePreviewPanel`. `InfoPanel` object: anchor/pivot (0.5, 1), position (0, -20), size (360, 56), panel sprite sliced, color (0, 0, 0, 0.75). `TitleText`: TMP, left-aligned area, size (290, 40), font 20, white. `CloseButton`: `CreateButton(..., "X", ...)`, size (44, 40), at the right. Assign the three slots; the panel starts inactive.
6. Tests
   - `PlayMode/InfoPanelTests.cs` (new; copy the `TapAndPreviewTests` setup; assign slots with `rig.FullMap.InfoPanelSlots.Set...`, then `rig.Rebind()`; title slot = legacy `UnityEngine.UI.Text`): `SelectNamedMarker_PanelShowsName`; `SelectUnnamedMarker_PanelHidden`; `Deselect_PanelHidden`; `CloseButton_ClearsSelection` (`button.onClick.Invoke()`); `RenameSelected_TitleUpdates`; `RenameOther_TitleUnchanged`; `ReopenFullMap_PanelHidden` (select, close, open → hidden, because closing cleared the selection); `EmptySlots_NoErrors`.
   - `EditMode/DefaultPrefabsTests.cs`: `FullMapPrefab_InfoPanelSlotsAssigned` (all three set, panel inactive).
   - `EditMode/RootInspectorTests.cs`: no change; must pass.

**Not in this step:** more fields on the panel (subtitle, icon). Users who need them build their own panel from `MarkerSelected` / `MarkerDeselected`.

**Tests:** PlayMode `InfoPanelTests` (8); EditMode `DefaultPrefabsTests` (+1), `RootInspectorTests`. Everything else green.

**Manual checks:**
1. **Build Default Prefabs**, then reinstall the UI in the Full Sandbox (Setup window or the Dev installer). The full map root shows an **Info panel** group with three slots filled.
2. Play, open the full map. Tap a Select marker named "Acme": the top panel shows "Acme". Tap X: the panel hides and the marker shrinks back. Tap an unnamed Select marker: no panel.
3. Tap a Destination marker named "Fuel": the info panel and the preview panel show together; Cancel hides both.
4. While "Acme" is selected, rename it in the Inspector: the panel title follows.
