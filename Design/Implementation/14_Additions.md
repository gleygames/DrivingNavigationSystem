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
