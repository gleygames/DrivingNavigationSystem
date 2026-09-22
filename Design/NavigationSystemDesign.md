# Driving Navigation System — Design

Status: first design pass + four review passes complete (2026-09-18). Decisions may change after implementation and live tests.

## Product scope

- A scaled map of the user's city in the UI (full map) and a dashboard minimap that follows the car, Google Maps style.
- Shortest/fastest path between two points on a road network, drawn on the map.
- Standalone. Must work in any scene, with cities built any way. Gley Traffic System integration is a bonus.
- v1 navigation features: rerouting, tap/click to set a destination, pan + zoom on the full map.
- No runtime road changes in v1.
- Cities that exist only at runtime (procedural / spawned) are not supported in v1: capture and road drawing work in edit mode only.
- No multi-level roads: bridges are treated as normal roads (2D only).
- Mobile first, but runs everywhere: all render pipelines, uGUI (default Unity UI), Unity 2022.3 minimum.

---

## 1. Coordinate spaces and scale

- The map is a flat top-down projection of world X/Z. Height (Y) is ignored.
- **Map area** = one rectangle on world X/Z. The user can rotate it around Y (default 0°), so diagonal cities fit tightly.
- **Internal map unit = meters**, measured from the map area corner, in the rotated frame. Independent of image resolution.
- The image uses a meters → 0–1 conversion only when drawn.
- **Zoom = meters visible across the view** (e.g. "minimap shows 300 m"). Same result on every screen and Canvas Scaler setup.
  - Max zoom out (full map): setting **Fit** (default — whole map visible, outside-map-color bands where the screen shape differs) or **Fill** (map fills the screen, never shows outside, pan to see the rest). **Outside map color** (map view setting) default = average color of the captured image edges; user can change it.
  - Max zoom out (minimap): automatically capped so the view always fits inside the map (circle diameter / rotated rectangle diagonal).
  - Max zoom in: user setting, with an optional warning when the image gets blurry.

## 2. Floating origin

- We **do not** move the world. We only support the user's own floating origin system.
- All our data is stored in stable "true" (edit-time) coordinates: `true position = current world position − total shift`.
- **Shift source** — a **Navigation Manager** setting (floating origin belongs to the game, not to a map); never both, to avoid counting twice:
  - **Rectangle** (default): the shift is read from the **active** map's scene object; the rectangle stores its **edit-time position** (updated whenever edited in the editor). At runtime `shift = current position − stored position`. Works across `SetMap` (each map object carries its own stored position). A runtime rotation change of the rectangle logs a warning.
  - **Manual**: the user calls `OnOriginShifted(delta)`; the Manager accumulates the deltas. The total belongs to the world and is kept across `SetMap`.
- Not "position at game start" — a game can start already shifted (e.g. loading a save far away).
- **World scale**: a "units per meter" setting in the project-wide Navigation Settings (default 1). Applied at the same boundary as the floating origin shift, so inside the system (runtime and editor tools: widths, speeds, snap distances, raycast offsets, capture piece size, zoom) everything is in real meters and all defaults stay correct. Values returned to users are converted back to their units.
- **Conversion rule**: inside the system everything is in true coordinates. Convert only at the boundary:
  - **In**: car position, marker positions, world points passed to the API.
  - **Out**: anything returned to the user (route points for AI, destination world position, …).
- Movement direction, speed and teleport detection use true positions, so a shift never looks like a teleport.
- Tracking runs in LateUpdate, after the user's system has shifted things that frame.
- **Misconfiguration warning** (editor / development builds, once per session): a teleport that lands the car far from every road (beyond the start snap distance) and/or outside the map rectangle, right after being on a road, logs: "The car jumped far away from all roads. If you use a floating origin system, make sure it moves the map object, or set Shift source to Manual." No automatic correction.

### Map asset and map scene object

- **Map asset** (ScriptableObject): rectangle (size, Y rotation, edit-time position), image reference, road network reference. Nothing else. The image, rectangle and roads describe the same area, so they live together: every scene using the map gets the exact same alignment.
- **Map scene object**: references the Map asset. It shows the rectangle handles (editing writes into the asset) and is the floating origin anchor.
- `SetMap(mapSceneObject)` switches the active map. Any active navigation stops (`NavigationStopped(MapChanged)`) and any preview is canceled (`PreviewCanceled`), because the routes belong to the old map's roads.
- **Automatic map choice**: map objects register with the Manager when their scene loads and unregister when it unloads (they find the Manager like markers do). If there is no active map and exactly one map object is loaded, it becomes active (`MapChanged`). Several loaded and none chosen → warning "several maps loaded, call SetMap". Active map's scene unloads → navigation stops (`NavigationStopped(MapChanged)`), views clear, and the same rule may pick the one remaining map. An explicit Manager reference or `SetMap` always wins.
- **Scene object ↔ asset sync**: unlocked → Unity's Move/Rotate tools write into the asset (same as our handles); locked (after capture) → Move/Rotate are reverted with a message ("Map area is locked, use Change area"). On scene open the object snaps to the asset. Object scale is ignored and kept at 1. Runtime Transform changes (floating origin) are never written back.

### Map rectangle

- **One rectangle**: position, Y rotation, width/height. The editor draws 4 corner handles; dragging one resizes the rectangle (it can never skew).
- The map image is previewed semi-transparent over the world in the Scene view, to align it with the real roads. The overlay is drawn at the map scene object's height (height is free to change, never locked; the lock covers only X/Z position and rotation). An "Always on top" toggle (default on) draws it over everything.
- The image always lies on one flat plane (Y ignored).
- **Custom image**: the rectangle's ratio locks to the image ratio (move / rotate / uniform scale only). The rectangle is free only in capture mode (no image yet).
- **Guidance for custom images** (setup step 2): shows the required ratio (e.g. "1000 × 733 m → 1.364 : 1"), recommended pixel sizes (multiples of 4, square pixels) with m/pixel, and an **Export template** button that saves a PNG at the chosen size with the roads drawn as lines (over the capture if any, else a plain background) for artists to paint over.
- **Captured image**: after capture the rectangle is **locked**. A "Change area" button unlocks it and marks the image **outdated**; a "Recapture" warning stays until the user recaptures.
- **Exact ratio on capture**: the pixel size is chosen first (multiple of 4), then the rectangle grows very slightly (at most a few pixels' worth, keeping its center) so pixels stay square and the image never drifts from the roads.
- Roads outside the rectangle: a **warning** to the user, no restriction.

## 3. Road network data model

- **One line per road** (centerline), not per lane.
- Each road is **one-way or two-way**. Internally a two-way road is two directed directions.
- **No turn restrictions.** Users model forbidden turns by drawing separate one-way roads.
- **U-turns**: never / at intersections / anywhere. Default: never. Dead ends always allow turning around. A Navigation Manager setting (changeable at runtime, like route mode) and an optional route request field.
- **Roads connect only at their ends.** Joining the middle of a road splits it (done by the editor automatically).
- **Two kinds of data:**
  - **Graph** — intersections, connections, length. Used only for pathfinding. Small and fast.
  - **Shape** — dense points (polyline) per road, following the real road. Used for drawing the route and tracking the car.
- **Road types**: one **project-wide** user-editable list (settings asset) shared by all maps, 4 defaults (e.g. Highway / Main / Secondary / Local). Each type has a default speed, default width and an editor debug color. Route preferences ("avoid Highway") therefore mean the same on every map.
- **Road type IDs**: each type has a stable ID (counter, never reused). Roads store the type ID, not the list position, so renaming/reordering types is safe.
- **Each road**: type + optional speed override + optional width override.
- **Deleting a road type in use**: a dialog shows how many roads (across all maps; all authoring assets are scanned) use it and asks which type to move them to; roads are reassigned, then the type is deleted. The last remaining type cannot be deleted.
- **Road types at runtime**: bake copies each road's final speed and width (type value or override) plus its type ID into the runtime asset (no lookups at runtime). The runtime road asset references the road types asset so game code can pass a type ("avoid Highway"). Changing a type's speed/width marks every bake using it as outdated (the version stamp includes the types asset version).
- Saved in **two ScriptableObject assets**:
  - **Road Network authoring asset** (editor only): key points, curve handles, road properties. The tools and importers edit this. It references the runtime asset, never the other way, so it is never pulled into builds. The editor finds it by searching for the authoring asset that references a given runtime asset.
  - **Road Network runtime asset**: dense shape points + baked data, written by **Bake**. Referenced by the Map asset. Reusable across scenes.
- **Stable IDs**: each road and intersection has a unique integer ID from a counter in the authoring asset; never reused. On a split, one half keeps the old ID, the other gets a new one. Bake maps IDs to compact array indices in the runtime asset (direct array access at runtime).
- A mismatch check warns when the data doesn't fit the scene (e.g. roads outside the rectangle).
- Road names: not in v1.

## 4. Authoring tool

- **Editor home**: a Gley window with modes (Draw / Edit / Connect / Validate / Bake), like Traffic System, reusing `Common` window code. The Scene view is used for clicking; settings live in the window. (A Scene view overlay may come later.)
- Roads are drawn in the **Scene view**. Points are placed by **raycast on scene colliders** (car environments always have road colliders).
  - "Road layers" mask (default Everything). Triggers are ignored.
  - Point height is kept for editor display only. Map and routing ignore it.
- **Drawing**: click key points, drag handles to bend curves. The tool generates the dense shape points by raycasting. Spacing is adaptive: a point is added where the line would deviate from the curve by more than X cm (default 10 cm), with a max distance between points (default 20 m) so roads follow hills. Key points and handles are kept for later editing. (May be adjusted later.)
- **Bridges / roads under bridges**: key points use the Scene view mouse ray (user views from the side to click under a bridge). Generated points raycast from just above the curve's interpolated height (+2 m) down a short distance (5 m), so they never hit a bridge above. No hit → use interpolated height + validation warning.
- **Editing operations**:

| Operation | How |
|---|---|
| Draw a new road | Click key points; ends auto-snap |
| Extend a road | Start drawing from its free end |
| Select | Click; Ctrl+click adds; box select by dragging on empty space |
| Move key point / curve handle | Drag |
| Insert key point | Double-click on the road |
| Delete key point | Select + Delete (a road keeps at least 2) |
| Delete roads | Select + Delete |
| Split road manually | At a chosen point (creates an intersection) |
| Merge two roads | When only those two meet at an intersection and properties match (one keeps its ID) |
| Flip direction / edit properties | Buttons / properties panel |
| Move intersection | Drag; all connected road ends follow |
| Connect / Disconnect intersection | Snapping / Disconnect separates all connected roads |

- **Connecting**: auto-snap to road ends and road middles (middle = split). The snap target is highlighted before clicking. Hold a key to disable snapping. A Disconnect action fixes mistakes.
- **Properties**: brush settings apply to new roads; any road(s) can be selected (multi-select) and edited later.
- One-way direction = drawing direction, with a **Flip** button.
- **Validation**:
  - Live, cheap checks: near misses (ends close but not connected), duplicates.
  - Validate button, full check: islands, one-way traps, roads outside the map area. Clickable list jumps to the problem.
  - No automatic check on save.
  - **Build check**: problems (validation errors, outdated bake) are summarized in the console and the build continues. Setting "block build on problems" (default off) for strict teams.
- Unity Undo/Redo supported.

### Scene view drawing performance

- Draw only what is inside the Scene view (same approach as Traffic System).
- Detail depends on camera distance: far = road lines only, close = arrows, points, colors.
- Interactive handles only on the selected road(s).
- Per-element draw toggles:

| Element | Default |
|---|---|
| Road lines | On |
| Direction arrows | On |
| Road type colors | On |
| Key points | On |
| Dense shape points | Off |
| Intersections / connections | On |
| Validation highlights | On |
| Map rectangle | On |
| Map image overlay | Off |

- Filter by road type (e.g. show only Highways).
- A cached single mesh for all roads only if live tests on big cities still lag.

## 5. Import adapters

- Public **importer interface**. v1 ships only the **Gley Traffic System** importer. EasyRoads3D, Unity Splines, etc. come later.
- Import = **one-time copy**. Each road remembers its source.
- Re-import replaces only the roads from that source. Hand-drawn roads are kept. Manual edits to imported roads are lost (with a warning).
- Imported data goes through normal validation.
- **Deferred**: how Traffic System (and other tools) data maps to our roads. Decide after the built-in editor works and after live tests.

## 6. Pathfinding

- **A\*** on the graph.
- **Route request inputs**: From (world point, required, snapped), From heading (optional; if given the route starts in that direction following the U-turn rule, else either direction), To (world point, required, snapped), Mode (optional, default from Manager setting), Road type preferences (optional, default from Manager setting), U-turn rule (optional, default from Manager setting). Player navigation uses the same request with car position + heading filled in.
- **Route result** (snapshot, never changes while driving): success / failure reason (see the failure reason list in section 14); snapped start and end world points; ordered road list (road ID, travel direction, start/end distance along each road — first and last usually partial); total length (m) and ETA (s); points along the route in world space incl. height, filled into a caller-provided list (no garbage). Positions converted out to the current world when read.
- **Points in the middle of roads**: per request, the snapped start/end temporarily split their roads into partial pieces (forward to next intersection, backward to previous). A* runs, then the temporary points are discarded; road data is never changed. One-way roads allow only the legal piece; a given heading allows only the piece in that direction (unless U-turns = anywhere); no heading allows both.
- **Start and destination on the same road**: add a direct piece between them if the direction allows it (A* still decides if it is best). Destination behind without allowed U-turn → normal A* loop. Start within the arrival distance of the destination → no route, "arrived" fires immediately.
- Pathfinding is a **general service**: any code can request a route (AI cars, missions, quest UI). Player navigation is just one user of it.
- **Route request** (runtime, no rebuild): mode **Shortest** (distance) or **Fastest** (length ÷ speed), plus per road type **normal / avoid / prefer**. Cost is computed at request time.
- **Preference multipliers** on road cost: normal ×1, **avoid ×5**, **prefer ×0.7** (both settings). Avoid is not forbid: if there is no other way the road is still used, so preferences never make a route fail.
- **A\* estimate** (must never exceed the real cost): Shortest = straight distance × smallest multiplier in use; Fastest = straight distance ÷ highest speed of any road in the network (including per-road overrides; baked) × smallest multiplier in use. Computed once per request.
- **Start**: the car's position and direction on the road (from tracking). A separate, larger **start snap distance** (default 200 m) lets a car in a parking lot or field start navigation: the route begins at the nearest road point; beyond it → `NoRoadNearStart`. No dotted line from the car to the route start; once the car reaches a road, normal tracking/rerouting takes over.
- **Destination**: the clicked point snapped to the nearest road point, within a max snap distance. Beyond it: rejected (`NoRoadNearDestination`). The marker stays where the user clicked, with a dotted line from the route end.
- Runs **synchronously on the main thread** in v1, behind a callback-style "route ready" API so it can move to multi-frame later without breaking user code.
- **No route** → fails clearly with a reason (`PreviewFailed` for previews, `RouteFailed` for navigation/reroutes, or the request callback for general requests). No partial routes.

## 7. Vehicle tracking (map matching)

- Input: the car's Transform, plus a **yaw offset** (degrees around Y) for models whose forward is not +Z. Setup step 5 draws an arrow gizmo on the car showing the nose direction used; quick buttons 0 / 90 / 180 / 270 + free value. `SetCar(transform, yawOffset)` for cars spawned from code. The offset affects only the nose direction, not the movement direction.
- **Two direction sources**:

| Used for | Direction source |
|---|---|
| Road matching (which way on the road, turned-around reroute) | **Movement** direction above a small speed; below it, keep the last known one |
| Minimap rotation (heading-up) and player arrow | Car's **nose** (transform forward flattened to X/Z); if the car is flipped/vertical, keep the last valid one |

  Reversing does not spin the minimap; matching still knows the car moves backward.
  Before the car has moved above the min speed once (game start, after `SetCar`, after a teleport), the movement direction falls back to the car's nose, so the first route starts the way the car faces.
- **Road matching**: score by distance + heading match, with **continuity** (prefer the current road and roads connected to it; switch to an unconnected road only if clearly better for a short time). Handles intersections, parallel one-way roads and 2D bridge crossings.
- **Off-road**: within a max distance the marker snaps to the road; beyond it the car is **off-road** (real position shown, event fires). The route is kept while off-road.
- **Update strategy (event-driven, no timers)**:
  - Every frame: only a cheap projection onto the current road segment (smooth marker; detects leaving the road or reaching its end). Skipped when the car is stopped.
  - Reaching a road end → choose among **connected** roads (from the graph, no grid).
  - **Grid search only when lost**: start, teleport, off-road, left the road sideways.
  - At small-angle forks, keep 2–3 candidates until one clearly wins.
  - Two thresholds, based on road width: "left the road" > "back on the road" (no flicker).

## 8. Route management and rerouting

| Case | Reroute when |
|---|---|
| Wrong turn | Immediately, once matched to the new road |
| Turned around | After driving X meters the wrong way (setting) |
| Off-road | When back on a road that is not on the route |

- A new route always starts from the car's current position and direction.
- **Preference changes during navigation** (route mode, road type preferences, U-turn rule): recalculate right away from the car (`Rerouted(route, PreferencesChanged)`), open preview too. Explicit action → ignores the reroute cooldown. Several changes in one frame → one recalculation at the end of that frame's update.
- **Teleport during navigation** (jump > teleport distance): always bypasses the cooldown. New road not on the route → reroute right away (`Rerouted(route, Teleported)`); landed on the route → no reroute, progress jumps to the new position (the trim follows, forward or backward).
- **Reroute cooldown (distance-based)**: after a reroute, the next one can only happen after the car has driven at least X meters (setting, default 20 m). Arrival and `StopNavigation()` are never delayed.
- **Arrival**: the car passes the destination point on the final road, or is within a small distance while on the final road. "Arrived" event fires, the route is cleared. Off-road destination: arrival at the route end.
- **Driven part of the route**: setting — removed (default, the line shrinks) or faded color.
- Route info in v1: **remaining distance** and **ETA**. Next-turn info: not in v1.
- **ETA** = time remaining in seconds (a duration, not a clock time; shown e.g. "5 min" by the formatter). Computed from the remaining route with road speeds (type or override), not the player's actual speed. Updated with progress; exposed as a property. Games with an in-game clock can convert it to an arrival time.
- **While off-road**, remaining distance and ETA freeze at the last on-road value until the car is back on a road (the UI can check the off-road property and show "--").

## 9. Map picture (capture tool)

- The map picture is a **top-down screenshot** of the map area, captured in the editor and saved as **one image**. The user can edit it or replace it with their own design, as long as it matches the real roads.
- Tiles (splitting the image) are a possible later upgrade for big cities.
- **Capture settings**: resolution, layers to include (e.g. exclude trees, vehicles), disable fog/post-effects, **capture fill color** (fills pixels where the camera sees nothing; default dark gray), preview before saving. Advanced: "use my own camera".
- Capture is **always done in pieces** (a small map = 1 piece), with small overlap, stitched into the one saved image. Avoids missing shadows, low LODs, missing grass and max render size limits.
- **Capture camera** (automatic): orthographic, pointing straight down, rotated to the rectangle. Height above the highest object in the area (renderer bounds on included layers) + margin; clip range down past the lowest object. Same height/clip for every piece. With "use my own camera", the user's camera settings (lighting, effects) are kept but position, rotation and size are always ours.
- **HDRP exposure**: captures always use fixed exposure (auto-exposure would give each piece a different brightness → seams). "Capture exposure (EV)" setting shown only in HDRP; starts from the scene's exposure if already fixed, else a daylight default; tuned with the preview. Applied via a temporary override on the capture camera only (scene and volumes untouched). Hidden in Built-in/URP.
- **Import settings** set automatically for our captures: compressed, mipmaps on, size multiple of 4 (the user can change them). Custom images only get **warnings**, never silent changes.

## 10. Map rendering in uGUI

- **One generic "map view"** component: image + route + markers inside one container. Add-on behaviors:
  - **Follow Car** — used by the minimap.
  - **Interactive** — used by the full map.
  - Users can build other views (mission briefing, menu map).
- **One "map content" container**, laid out in map meters. Pan / zoom / rotate = transform this one container. A mask on the parent clips the view.
- **Route line**: one custom UI Graphic building a **single mesh** (1 draw call), in its own nested sub-canvas.
  - Each vertex stores its distance along the route; a simple UI shader hides the driven part. Trimming = change one value, no mesh rebuild.
  - **Constant on-screen width in canvas units** (not pixels; scaled by the project's Canvas Scaler like all UI). Setting, separate for full map and minimap. Applied in the shader from a per-vertex side direction. No rebuild on zoom.
  - **Outline** in the same mesh: the mesh is drawn slightly wider and the shader paints the outer edge in the outline color (uses the per-vertex side value). One mesh, one draw call. Outline width 0 = off.
  - **Route Style asset** (ScriptableObject) referenced by each Map View (minimap and full map each get their own by default; holds the per-view widths). Defaults: active = blue line, dark blue outline; preview = light blue semi-transparent, gray outline; driven part when "faded" = gray 50% transparent, no outline. Line width and outline width per style.
  - **Dotted line to an off-road destination**: the last piece of the same mesh (straight, from route end to the clicked point). Its vertices carry a "dashed" flag; the shader cuts gaps from the distance along the route. Dash/gap lengths in canvas units (constant at every zoom). Uses the style colors; trimmed like the rest.
  - **Chunks**: the line is built in chunks of at most 16,000 vertices (UI mesh limit ~65,000); normal routes need one chunk. Chunks share the material (usually batched into one draw call). Fully driven chunks are disabled.
  - **Corners**: miter joins with a limit angle; sharper corners (e.g. U-turns) get a bevel.
  - **No drive-side offset**: the line always runs on the road centerline. Out-and-back routes overlap on the same road; the trimmed driven part keeps this readable.

## 11. Markers and icons

- A separate **marker layer** above the map container (inside the mask). Markers are placed each frame by converting map position → view. Constant screen size.
- Per-marker rotation: **upright** / **follow heading** / **follow map**. "Follow heading" uses the marker object's forward flattened to X/Z (flipped/vertical → keep last valid); static markers read it once; point markers (destination, preview pin) are always upright.
- Generic **Map Marker** component on any object: icon, rotation mode, which views show it, **static** checkbox (static = world → map position computed once; placement in the view still updates when the view moves).
- **View channels** (like Unity layers): a short list of named channels (e.g. Minimap, Full map, Custom 1–6) in the project-wide settings asset. Each Map View and each marker has a channel mask; a view shows a marker when they share a channel (bitmask check). New markers default to Minimap + Full map.
- **Two kinds of marker entries** in the Manager's list: **object markers** (Map Marker component on a GameObject) and **point markers** (a world point, no GameObject, created by the Manager).
  - **Destination** and **preview pin** are point markers at the clicked point (dotted line to the road).
  - **Player marker**: an object marker the Manager creates internally for the car; nothing is added to the user's car; `SetCar` retargets it.
  - Three prefab slots on the Manager (player, destination, preview pin), each with a default; the preview pin looks different (e.g. hollow vs filled).
  - During a preview the full map shows both the active destination and the preview pin; the minimap shows only the active destination.
- **Finding the Manager**: use the marker's reference if set, otherwise one cached scene search on enable. `AddMarker` / `RemoveMarker` API for full control. Markers unregister themselves when disabled/destroyed.
- **Moving markers**: the Manager reads every non-static marker's position each frame in one LateUpdate loop and updates its grid cell if changed (cheap). UI updates happen only for markers inside the view. Static markers are skipped.
- Only markers inside the view (+ margin) are shown and updated (grid lookup). Marker layer has its own sub-canvas. Markers with an off-screen arrow are never culled.
- **Off-screen arrow** is a general marker option ("Show off-screen arrow"): on for the destination, off for other markers by default. Works in any view (per-view setting, default on), e.g. on the full map when panned away from the destination. Uses the view's edge setting (rectangle/circle + inset) and optional distance label. Keep arrow markers few.
- **Markers as destinations**: "Can be destination" checkbox (default off). Tapping such a marker starts the preview at the marker's position (snapped normally); markers win over the map point when a tap hits both; the gamepad crosshair snaps to them when close; an event reports which marker was chosen. **Tap targeting**: hit radius 40 canvas units (setting); only "Can be destination" markers are tappable; with several in range the closest wins; the crosshair uses the same radius and rule.
- Marker visual = **UI prefab** (default prefab = one Image). Pooled when entering/leaving the view.

## 12. Full map interaction

- **Map actions API** (independent of input): the Map View actions (`Pan`, `Zoom`, `TapAt`, `CenterOnCar`, … — see section 14).
- **Input adapters** call the actions:
  - **Built-in pointer adapter** (on by default): mouse, touch, pinch via uGUI events. Works with both input systems.
  - **User's own input**: disable ours (or keep it) and call the actions from their own input class and key mappings.
  - **Optional gamepad/keyboard adapter** for the **new Input System** only, with default remappable bindings. Compiled only when the package is installed.
- **Center crosshair mode** for gamepad/keyboard: the stick pans the map under a fixed crosshair, confirm sets the destination. Setting on the full map: **Auto** (default — the last-used kind of input decides: crosshair actions turn it on, pointer actions turn it off) / Always / Never.
- **Fling** (map glides after a quick swipe, then slows) and **double-tap zoom** (one step in, around the tap): both settings, default on. Implemented through the same actions (`Pan`, `Zoom`); fling uses unscaled time. With double-tap on, a single tap waits briefly before starting the preview.
- **Tap = preview**: pin + route + distance/ETA. Navigation starts on **confirm** (default confirm button provided). Setting to skip confirm.
- **Preview updates**: recalculated whenever the car moves onto a new road (tracking event), and always recalculated on confirm.
- **Active route during a preview** keeps running (minimap shows it, rerouting works). The full map draws both: active route in normal style, preview in a distinct style. Confirm replaces the active route.
- **Cancel preview**: Cancel button next to Confirm (default UI); tapping elsewhere moves the preview; closing the full map discards the preview.
- **Stop navigation**: `StopNavigation()` API + default Stop button on the full map while a route is active. Arrival clears the route automatically.
- The full map **does not rotate** (pan + zoom only). A Rotate action can be added later. Panning stops at the map edges.
- **Open / close**: turning the full map's GameObject on/off; convenience `Open()` / `Close()` / `Toggle()` and `Opened` / `Closed` events on the full map view. The full map prefab has a Close button. Pausing the game while open is the game's choice (listen to the events).
- **Minimap tap** opens the full map by default (setting: open full map / nothing; the setup window links them). The compass is a separate button (toggles rotation mode).
- **On open**: always centered on the car at a default zoom (setting).
- **While open**: follows the car until the user pans; then stays put, and a "Center on car" button resumes following. Zooming does not stop following (while following, zoom pivots on the car).

## 13. Minimap view

- **North** = the map's "up" (the rectangle's forward direction), so the full map and a north-up minimap always match. Games needing world north keep the rectangle at 0°.
- **Rotation**: heading-up (default) or north-up, switchable at runtime (e.g. tap the compass). Compass shown. Smooth, damped rotation.
  - **Heading-up follows the road, not the car**: while the car is matched to a road, "up" is the matched road's direction at the car's point (like Google Maps), so weaving inside the lane doesn't rotate the map; curves still rotate it smoothly. Of the road's two directions, the one closer to the car's nose is used (reversing never spins the map; a real U-turn flips it, with hysteresis so driving across a road doesn't flicker).
  - **Off road / lost / no road data**: falls back to the car's nose heading with a dead zone (default 3°): smaller nose changes are ignored.
- **Car position**: offset setting (default ~30% from the bottom) in heading-up. North-up always centers.
- **Map edge clamp**: the view never shows outside the map. When the view cannot move further, the player marker slides from its usual spot toward the edge, and returns smoothly when the car drives away. Round minimap clamps by radius (rotation-independent); rectangular heading-up minimap clamps by its rotated corners (recomputed each frame).
- **Car outside the map area**: the player marker is pinned at the view edge (still showing the car's direction) and an "outside map" event fires. Tracking, routing and rerouting keep working normally.
- **Zoom**: speed-based (min/max meters across mapped to speeds, smoothed). Off = fixed zoom.
- **Shape**: rectangle → RectMask2D (no extra cost); any other shape → stencil Mask with a user sprite (round sprite included).
- **Off-screen arrows** (see Markers): point straight toward the marker. Placed on the edge via an edge setting (rectangle/circle + inset). Optional distance label.

## 14. Public API and setup

### Component references

```
Navigation Manager (scene)
 ├─► Map scene object ──► Map asset ──► Image
 │                                  └─► Road Network runtime asset
 ├─► Car (Transform)
 └─► Formatter (units / text)

Map View (UI) ──► Navigation Manager
 ├─ Follow Car behavior      (minimap)
 └─ Interactive behavior     (full map)
       ▲
 Input adapter ──► Map View

Map Marker (on any object) ──► Navigation Manager
```

- Everything points toward the Manager, never back. The Manager raises events and answers queries. It keeps a **marker list** (data only) that views query.
- Views and markers read from the Manager (active map, route, car state, events).
- `SetMap` → Manager fires an event → views swap image and rebuild.

- **Setup window** (Gley settings-window style), step by step, each step showing done / missing / warnings:
  1. Map area (creates the rectangle object)
  2. Map image (capture or assign)
  3. Roads (drawing tools, validate)
  4. UI (adds minimap and full map prefabs to a canvas)
  5. Car (assign the Transform)
  - It creates normal components and prefabs, so manual setup is still possible.
  - **Canvas**: pick an existing one or create a new one (Screen Space Overlay, Canvas Scaler "scale with screen size", 1920×1080 reference, match 0.5).
  - **EventSystem**: if none exists, create one with the module matching Active Input Handling (new Input System, or Both with the package installed → `InputSystemUIInputModule`; old Input Manager → `StandaloneInputModule`). An existing EventSystem is never modified; a mismatched module shows a warning.
- **Runtime API**: one scene **Navigation Manager** component with instance methods and C# events, accessed through a serialized reference (the setup window wires it). **No static API.**
### Events (Navigation Manager)

All positions are converted out to the current world.

| Group | Event | When |
|---|---|---|
| Setup | `MapChanged(map)` | After `SetMap` |
| | `CarChanged(car)` | After `SetCar` |
| Preview | `PreviewReady(route, marker?)` | Preview calculated/updated; `marker` set if a marker was tapped |
| | `PreviewFailed(reason)` | Any failure reason (see list below) |
| | `PreviewCanceled` | Cancel button or full map closed |
| Navigation | `NavigationStarted(route)` | Confirm (or instant mode) |
| | `Rerouted(route, reason)` | Wrong turn / turned around / back on road / car changed / preferences changed / teleported |
| | `RouteFailed(reason)` | `StartNavigation` failed (current navigation kept) or a reroute failed (navigation stops) |
| | `Arrived` | Destination reached, route cleared |
| | `NavigationStopped(reason)` | Reason: `StopCalled` (API / Stop button), `MapChanged`, `CarRemoved` |
| Tracking | `OffRoad` / `BackOnRoad` | Crossing the off-road thresholds |
| | `OutsideMap` / `BackInsideMap` | Leaving/entering the map rectangle |

- **Failure reasons** (one list, used everywhere): `NoMap` (no active map or road data missing), `NoCar` (player navigation without a car), `NoRoadNearStart`, `NoRoadNearDestination`, `NoPath`.
- **Where failures are reported**: `PreviewDestination` / tap → `PreviewFailed(reason)`, active route untouched. `StartNavigation` → `RouteFailed(reason)`, current navigation (if any) keeps running. Reroute → `RouteFailed(reason)`, navigation stops. `RequestRoute` → failure reason in the result callback.
- **API calls from inside event handlers** (e.g. `Arrived` → `StartNavigation(nextStop)`): queued and applied in order right after the Manager finishes firing that frame's events (same frame). Calls from normal code apply immediately. `RequestRoute` never changes Manager state and always runs immediately. Events raised while applying queued calls are delivered normally; the queue is processed at most a few rounds per frame, extra rounds are logged as an error (no infinite loops).
- **Inspector events**: the Manager exposes C# events only. An optional **Navigation Events** component forwards every event as a UnityEvent for Inspector wiring (no cost when not added).
- Remaining distance, ETA, current road and off-road state are **properties**, not events (no per-frame events).
- General pathfinding requests use their own per-request "route ready" callback, not these events.

### Navigation Manager API

| Group | Method | Notes |
|---|---|---|
| Setup | `SetMap(mapObject)` | |
| | `SetCar(transform, yawOffset = 0)` | |
| | `Initialize()` | Only when "start manually" is on |
| | `OnOriginShifted(delta)` | Only when shift source = Manual |
| Destination | `PreviewDestination(worldPoint)` | Same as a tap, from code |
| | `StartNavigation(worldPoint)` | Straight to navigation, no preview |
| | `ConfirmPreview()` / `CancelPreview()` | Used by the default buttons |
| | `StopNavigation()` | |
| Preferences | `SetRouteMode(mode)` | Shortest / Fastest |
| | `SetRoadTypePreference(type, pref)` | Normal / avoid / prefer |
| | `SetUTurnRule(rule)` | Never / at intersections / anywhere |
| Pathfinding | `RequestRoute(request, callback)` | General service |
| Markers | `AddMarker(marker)` / `RemoveMarker(marker)` | |
| Formatting | `SetFormatter(formatter)` | Units and text |

Read-only properties: active map, car, active route, preview route, remaining distance, ETA, current road, off-road, outside map.

### Map View actions

| Group | Action | Notes |
|---|---|---|
| Navigation | `Pan(screenDelta)` | Stops car-following |
| | `Zoom(amount, screenPivot)` | While following, pivots on the car |
| | `SetZoomMeters(meters)` | Direct zoom (e.g. fixed minimap zoom) |
| | `CenterOnCar()` | Resumes following |
| Selection | `TapAt(screenPoint)` | Marker first, else map point → preview |
| | `SetCrosshairMode(on)` | Gamepad/keyboard |
| | `ConfirmAtCrosshair()` | Like a tap at the crosshair |
| Minimap (Follow Car) | `SetRotationMode(mode)` / `ToggleRotationMode()` | Heading-up / north-up; toggle = compass tap |
| Conversions | `ScreenToWorld(point)` / `WorldToScreen(point)` | For custom UI on top of the map |

Properties: following car, current zoom (meters). A full map view that gets disabled (closed) cancels any open preview.

- **Startup**: automatic on scene start by default; "start manually" option. `SetCar(transform)` to switch or spawn cars at any time. During navigation, `SetCar` keeps the destination and recalculates from the new car immediately (`Rerouted(route, CarChanged)`; an open preview is recalculated too; failure → `RouteFailed`, navigation stops). `SetCar(null)` stops navigation (`NavigationStopped(CarRemoved)`). While no car is set, the map works for viewing and navigation waits.

## Cross-cutting

- **Design principle: build as much as possible in the editor**, so load time is not affected.
- **Asset locations**:
  - User data never goes inside the package folder (`Assets/Gley/DrivingNavigationSystem`), so Store updates can't overwrite it.
  - One folder per map, chosen in setup step 1 (default `Assets/NavigationData/<SceneName>/`): `<Name>_Map.asset`, `<Name>_RoadsAuthoring.asset`, `<Name>_RoadsRuntime.asset`, `<Name>_MapImage.png`.
  - Captured image = **PNG** (lossless, editable anywhere; Unity compresses it for builds). Recapture overwrites the same file (references kept). Automatic import settings are applied only when the file is first created, so the user's later changes are kept.
  - Project-wide settings (road types, view channels): one `Assets/NavigationData/NavigationSettings.asset`, created on first use, found by search in the editor; several found → warning, first one used.
- **Data format version**: every asset we create (Map, Roads Authoring, Roads Runtime, Navigation Settings, Route Style) stores a format version (starts at 1). Editor: older assets run migration steps in order (1→2→3…), get saved, one-line log. Runtime road assets just become "Bake outdated" (no conversion code). Newer than the code understands (downgrade) → clear error, asset untouched. Migrations never run in builds.
- **Bake**: graph, road lengths, base travel times (length ÷ speed) and the grid are baked into the asset by a **manual Bake button** (no automatic rebuilds). Route preferences stay runtime multipliers.
  - Version stamp: each road edit bumps a counter; the bake stores the counter it was built from.
  - Outdated bake → "Bake outdated" in inspector and setup window; warning when entering Play mode; build check (see validation).
  - At runtime an outdated bake is **used as is** and an error is logged. Runtime code must never crash on mismatched data (e.g. skip missing roads).

- **Update order**: (1) Navigation Manager in LateUpdate runs fixed internal steps named `Update<Domain><Phase>(deltaTime)`: read car + apply floating origin → tracking → route progress / rerouting → fire events. (2) Map Views in LateUpdate, forced after the Manager via `[DefaultExecutionOrder]`: follow/clamp → container transform → route trim value → markers. The Manager never calls views; order comes from the attribute.
- **Time**: logic (tracking, speed, rerouting, ETA) uses scaled game time and skips the frame when deltaTime is 0. UI motion (damped rotation, smooth zoom, marker return from edge, pan/pinch) uses unscaled real time, so the full map stays responsive while paused.

- **Default art**: player arrow, destination pin (filled), preview pin (hollow), off-screen arrow, round minimap mask, frame, compass (N), crosshair, buttons (Confirm, Cancel, Stop, Center on car, Close), info panel background (9-slice). Style: flat, minimal, white shapes with dark outline (readable on any map). Placeholder sprites during development; final art before release with the same file names (no prefab changes). No third-party icon packs (licensing).
- **Safe area**: a small Safe Area component on the root of the default UI prefabs fits them to `Screen.safeArea` (notches, rounded corners). Event-based: updates when the canvas size changes (rotation/resolution), no per-frame checks. Removable for users with their own system. Screen Space canvases only (World Space ignores it).
- **Text**: components write text through a small **text adapter**; the core runtime has **no TextMeshPro reference** (projects without TMP still compile). A TextMeshPro adapter lives in `Gley.NavigationSystem.TMP`, compiled only when TMP exists (`com.unity.textmeshpro` in 2022.3, or `com.unity.ugui` 2.0+ in Unity 6). The core includes a legacy Text adapter. Default prefabs use TextMeshPro; if it's missing, the setup window says so. All text goes through the formatter.
- **Units**: the API always uses meters and seconds. All UI text goes through a **replaceable formatter**: a ScriptableObject base class assigned in the Manager's Inspector; a shipped **Default Formatter** asset has a metric/imperial setting; users subclass it for localization/custom formats; `SetFormatter` swaps it at runtime. Editor speeds are entered in the chosen unit (km/h or mph) and stored in m/s.
- **Multiple maps**: one active map at a time, switchable at runtime with `SetMap(map)`. Streamed worlds: put the map and manager in the persistent scene, with one rectangle over the whole world. No routing across maps. **Memory**: every map object in a loaded scene loads its image, so use one map object per scene; for multiple maps put each map object in its own small additive scene (load → `SetMap`, unload frees the image).
- **Mobile performance** is checked in every area.
- **Enter Play Mode Options (domain reload off) supported**: no mutable static state in runtime code (pools, caches, marker list, queue all live on instances); event subscriptions always removed in `OnDisable` / `OnDestroy`; editor static state reset on entering Play mode; every feature tested with domain reload off and on.
- **Logging**: all messages go through `Common/CustomLogger`. `Log` / `LogWarning` run only in the editor and development builds (stripped from release); `LogError` always runs. Editor-time issues (bake outdated, validation, image settings) are warnings; real runtime problems (outdated bake in use, event queue overflow, mismatched data) are errors.
- **One tracked car** in v1: one Navigation Manager per scene, one car (`SetCar` switches it). Online multiplayer works naturally (each client tracks its local car). Local split-screen not supported in v1. AI cars use only `RequestRoute`.

## Technical risks and prototype plan

The first implementation step is a small **internal prototype** (not shipped) that checks these risks in the editor / on desktop. Device testing is outside the development phase.

- **Nested sub-canvases inside masks**: test both mask types + route/marker sub-canvases + rotated content + the custom route shader. Fallback if clipping fails: one canvas, no sub-canvases (route still never rebuilds thanks to shader trimming; measure marker cost).

- **Custom route shader**: built on Unity's UI-Default shader source (2022.3 built-in shaders), keeping its stencil (Mask), clip-rect (RectMask2D) and alpha-clip support; only the trim and width expansion are added. Test in Built-in, URP, HDRP and all canvas modes (Overlay, Camera, World Space).

- **Undo on large road assets**: a drag is recorded once (at drag start) = one undo step. Test with a large generated network (~5,000 roads). Fallback if slow: split authoring data into chunks by area as **separate files** (so Undo records only the edited chunk, and team members editing different areas don't get merge conflicts).

- **Team workflows**: v1 keeps one authoring file per map. Unity text serialization lets edits to different roads often merge in git; two people editing the same map at the same time can conflict. Separate chunk files come only if the Undo test forces chunking.

- **Max image size on mobile**: our auto import settings set Android/iOS max size **4096**, desktop/consoles full resolution (one image, per-platform size). The capture tool warns above 4096 ("mobile builds will use a reduced version"). Custom images: warning only. Tiles (deferred) remain the long-term answer.

- **Assemblies for optional parts**:

| Assembly | Contains | Compiles when |
|---|---|---|
| `Gley.NavigationSystem` | All runtime code | Always |
| `Gley.NavigationSystem.Editor` | Tools, setup window, bake | Always (editor only) |
| `Gley.NavigationSystem.InputSystem` | Gamepad/keyboard adapter | Input System package installed (asmdef version define) |
| `Gley.NavigationSystem.TMP` | TextMeshPro text adapter | `com.unity.textmeshpro` present, or `com.unity.ugui` 2.0+ (asmdef version defines) |
| `Gley.NavigationSystem.TrafficImporter` | Traffic System importer | `GLEY_TRAFFIC_SYSTEM` define set (editor only); the setup window detects Traffic System and sets/removes it via `Common/PreprocessorDirective` |

## Performance targets

Goals for a low-end phone with a test city of 5,000 roads. During development they are checked with the Unity Profiler on the dev machine; choosing test devices is outside the development phase.

| Area | Target |
|---|---|
| CPU per frame, normal driving (Manager + minimap) | ≤ 0.5 ms |
| CPU per frame, full map open + 100 visible markers | ≤ 1 ms |
| One route request / reroute | ≤ 5 ms |
| GC allocation per frame (steady state) | 0 bytes |
| Draw calls minimap / full map | ≤ 6 / ≤ 10 |
| Runtime road data in memory | ≤ 5 MB |
| Map image at 4096, compressed + mipmaps | ≈ 10–20 MB (format dependent) |
| Map activation (scene start / `SetMap`), excluding asset loading | ≤ 10 ms |
| Editor: one edit operation | ≤ 50 ms |
| Editor: Scene view redraw | ≤ 16 ms |

## Default values

Starting points; tune after live tests.

### Routing and tracking

| Setting | Default |
|---|---|
| Route mode | Shortest |
| U-turns | Never |
| Avoid / prefer multipliers | ×5 / ×0.7 |
| Destination max snap distance | 50 m |
| Start max snap distance | 200 m |
| Arrival distance | 10 m |
| Turned-around reroute | 30 m driven the wrong way |
| Reroute cooldown | 20 m driven |
| Movement-direction min speed | 1 m/s |
| "Stopped" speed (skip tracking) | 0.1 m/s |
| "Left the road" threshold | half road width + 3 m |
| "Back on road" threshold | half road width |
| Teleport jump | 50 m in one frame |
| Fork resolved | car > half road width from all other candidates |
| Switch to unconnected road | clearly better for 10 m driven |

### Map views

| Setting | Default |
|---|---|
| Minimap rotation | Heading-up |
| Minimap car position | 30% from bottom |
| Minimap speed zoom | 150 m across at ≤ 20 km/h → 500 m at ≥ 100 km/h (capped to fit inside the map) |
| Minimap rotation smoothing | 0.25 s |
| Minimap heading-up source | Matched road direction; nose heading off road |
| Minimap off-road heading dead zone | 3° |
| Minimap road-change turn smoothing | 0.8 s (target jumps > 20°, e.g. turning onto another road) |
| Minimap shape | Round |
| Minimap route line width | 6 canvas units |
| Full map zoom-out | Fit |
| Full map zoom on open | 1000 m across |
| Max zoom-in (both) | 50 m across |
| Full map route line width | 8 canvas units |
| Confirm step | On |
| Driven part of route | Removed |
| Marker culling margin | 10% of view size |
| "Can be destination" | Off |
| Marker tap / crosshair snap radius | 40 canvas units |
| Double-tap wait (single tap delay) | 250 ms |
| Fling slowdown | speed halves every 0.15 s |
| Fling minimum speed | 500 canvas units/s |
| Mouse wheel zoom step | ×1.25 per notch |
| Double-tap zoom step | ×2 in |
| Gamepad zoom (trigger held) | ×2 per second |
| Gamepad pan speed (full stick) | 50% of view width per second |
| Off-screen arrow edge inset | 8 canvas units |
| Off-screen arrow distance label | On |
| Outside map color | Average of captured image edges |

### Editor, capture and project

| Setting | Default |
|---|---|
| Road layers mask | Everything |
| Shape point deviation / max distance | 10 cm / 20 m |
| Connect snap distance | 3 m |
| Key to disable snapping | Shift |
| Near-miss check distance | 2 m |
| Duplicate check tolerance | 1 m |
| Block build on problems | Off |
| Capture resolution | 2048 on the longer side |
| Capture piece size | 200 m |
| Capture piece overlap | 16 px |
| Capture fog / post-effects | Off / Off |
| Capture fill color | Dark gray |
| Road grid cell size (baked; change = bake outdated) | 50 m |
| Marker grid cell size (runtime) | 100 m |
| Units | Metric |
| Shift source | Rectangle |
| Startup | Automatic |

### Road types

| Type | Speed | Width | Editor color |
|---|---|---|---|
| Highway | 110 km/h | 20 m | Red |
| Main | 60 km/h | 14 m | Orange |
| Secondary | 50 km/h | 10 m | Yellow |
| Local | 30 km/h | 7 m | Light gray |

## Open / deferred

- How Traffic System and other tools' roads map to our roads (after the built-in editor works).
- Image tiles for big cities.
- Road names.
- Next-turn info (and turn-by-turn).
- Cached Scene view mesh (only if needed).
- Multi-frame pathfinding (only if needed).
- Capture and road drawing in Play mode (runtime-generated cities).
- On-demand image loading (Addressables) for multiple maps.
