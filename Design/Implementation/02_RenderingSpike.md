# Phase 2 — Rendering Spike (S03–S05)

These steps build the **real** route line code early, because it carries the biggest technical risks (design: "Technical risks and prototype plan"). The input is a plain list of points, so it doesn't depend on roads or routes yet.

Design references: section 10 "Map rendering in uGUI", "Technical risks".

---

## S03 — Route line mesh builder (pure logic)

**Goal:** turn a polyline into mesh vertices whose width is applied later by the shader.

**Depends on:** S01.

**Files:**

1. `Runtime/UI/RouteLineMeshBuilder.cs` (new, plain class, `internal`)
   - Method: `Build(List<Vector2> points, List<float> distances, List<bool> dashed, List<UIVertex> outVertices, List<int> outIndices)`
     - `points`: centerline in **Map meters**. `distances`: cumulative distance along the route at each point (meters). `dashed[i]`: the segment from point `i` to `i+1` is dashed.
     - Clears and fills the output lists (they are reused by the caller — never allocate new lists inside).
   - **Vertex layout** (every vertex):
     - `position` = the centerline point `(x, y, 0)`. The shader moves it sideways.
     - `uv0` = `(distanceAlong, side, 0, 0)`, `side` = −1 (left) or +1 (right).
     - `uv1` = `(offset.x, offset.y, 0, 0)`: the direction and length to move this vertex sideways **per meter of half-width**.
     - `uv2` = `(dashedFlag, 0, 0, 0)`, 1 = dashed, 0 = solid.
     - `color` = white.
   - **Rules:**
     - Skip consecutive points closer than 0.0001 m. Fewer than 2 remaining points → output stays empty.
     - Normal of a segment with direction `d` = `(−d.y, d.x)` (left side). Left vertex gets `+normal`, side −1; right vertex gets `−normal`, side +1.
     - **First/last point**: one vertex pair using that segment's normal (flat end).
     - **Inner point** with incoming normal `n0` and outgoing normal `n1`: `miter = normalize(n0 + n1)`, `scale = 1 / dot(miter, n0)`.
       - If `scale <= 2` (**miter limit**, constant `MiterLimit = 2f`): one vertex pair with offset `miter * scale`.
       - Else (sharp corner, e.g. U-turn): **bevel** — two vertex pairs at the same point, the first with `n0`, the second with `n1`, connected by a quad.
       - If `n0 + n1` is almost zero (exact 180° turn): use the bevel rule.
     - Where the dashed flag changes at a point, emit a second vertex pair at that point with the new flag (so a vertex never mixes flags).
     - Two triangles per quad between consecutive vertex pairs: `(l0, r0, l1)`, `(r0, r1, l1)`.

**Tests:** `Assets/Tests/NavigationSystem/EditMode/RouteLineMeshBuilderTests.cs`
- `Build_TwoPoints_Creates4Vertices6Indices`
- `Build_OnePoint_CreatesNothing`
- `Build_DuplicatePoints_AreSkipped`
- `Build_StraightLine_OffsetsArePerpendicularUnitLength` (points (0,0),(10,0): left offset (0,1), right (0,−1))
- `Build_RightAngleCorner_UsesMiterWithScaleSqrt2` (corner offset length ≈ 1.4142)
- `Build_UTurn_UsesBevelTwoPairsAtCorner`
- `Build_Distances_AreCopiedToUv0X`
- `Build_DashedFlagChange_DuplicatesPairAtPoint`
- `Build_ReusedLists_AreClearedFirst`

**Done when:** all tests pass.

---

## S04 — Route line shader, graphic and the Route Line Lab

**Difficulty: HARD** (custom UI shader, material copies under masks, shader channels). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** draw the line with constant on-screen width, outline, trimming and dashes, **without rebuilding the mesh** when zoom or trim changes, and verify it works inside masks, nested canvases and rotated containers.

**Depends on:** S03.

**Files:**

1. `Runtime/Shaders/RouteLine.shader` (new) — name `Gley/NavigationSystem/RouteLine`.
   - **Start from Unity's `UI/Default` shader source for 2022.3** (Unity built-in shaders download, file `DefaultResourcesExtra/UI/UI-Default.shader`). Keep unchanged: the `Stencil` block and stencil properties, `ColorMask`, `_ClipRect` + `UNITY_UI_CLIP_RECT`, `UNITY_UI_ALPHACLIP`, `ZTest [unity_GUIZTestMode]`, blending, tags.
   - Add properties: `_Color` (line), `_OutlineColor`, `_FadedColor`, `_HalfWidth` (canvas units), `_OutlineWidth` (canvas units), `_CanvasUnitsPerMeter`, `_TrimDistance` (meters), `_TrimMode` (0 = remove, 1 = fade), `_DashLength`, `_GapLength` (canvas units).
   - Vertex input must read `texcoord0` (float4), `texcoord1` (float4), `texcoord2` (float4).
   - **Vertex:** `halfMeters = (_HalfWidth + _OutlineWidth) / _CanvasUnitsPerMeter`; `objectPos.xy += texcoord1.xy * halfMeters`; then the normal UI transform. Pass `distance = texcoord0.x`, `side = texcoord0.y`, `dashed = texcoord2.x` to the fragment.
   - **Fragment:**
     - `inner = _HalfWidth / (_HalfWidth + _OutlineWidth)`; color = `_OutlineColor` if `abs(side) > inner`, else `_Color`.
     - If `distance < _TrimDistance`: `_TrimMode` 0 → `discard`; 1 → color = `_FadedColor` (no outline).
     - If `dashed > 0.5`: `along = distance * _CanvasUnitsPerMeter`; if `fmod(along, _DashLength + _GapLength) > _DashLength` → `discard`.
     - Then the standard clip-rect and alpha-clip code.
2. `Runtime/UI/RouteLineGraphic.cs` (new, `MaskableGraphic`, public)
   - Holds a `RouteLineMeshBuilder` and reused vertex/index lists.
   - `SetLine(List<Vector2> points, List<float> distances, List<bool> dashed)`: copies into its own reused lists, calls `SetVerticesDirty()`.
   - `SetStyle(Color line, Color outline, Color faded, float halfWidth, float outlineWidth, float dashLength, float gapLength, int trimMode)`: stores values, applies to materials.
   - `SetTrimDistance(float meters)` and `SetCanvasUnitsPerMeter(float value)`: store and **apply to materials only** (never `SetVerticesDirty`).
   - Material handling:
     - Serialized `Shader lineShader` + `SetShader(Shader)`. If null on enable: `Shader.Find("Gley/NavigationSystem/RouteLine")` (works in the editor and in tests; in builds the shader is kept because `RouteStyle` references it, S43). If still null: `LogError` and draw nothing.
     - On enable, create one material instance from the shader and assign it to `material`.
     - **Apply property values to both `material` and `materialForRendering`**. Override `GetModifiedMaterial(Material baseMaterial)`: call the base, then copy all our current property values onto the returned material. (Masks create a copy of the material; this keeps the copy in sync.)
     - Destroy the material instance in `OnDestroy`.
   - On enable: `canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2` (otherwise uv1/uv2 never reach the shader).
   - `OnPopulateMesh(VertexHelper vh)`: `vh.Clear()`, run the builder, `vh.AddUIVertexStream(vertices, indices)`. Increment an internal counter `MeshBuildCount` (property, used by tests and the lab).
   - `raycastTarget` = false by default.
3. `Assets/Tests/NavigationSystem/Dev/DevRouteLineLabController.cs` (new) — `OnGUI` sliders: zoom (canvas units per meter 0.2–5), container rotation (0–360°), trim distance (0–route length), outline width (0–6), trim mode toggle. Shows `MeshBuildCount` of each graphic. Applies values to all lab panels.
4. `Assets/Tests/NavigationSystem/Dev/Editor/RouteLineLabBuilder.cs` (new) — menu **Tools > Gley > Navigation Dev > Create Route Line Lab**. Scene saved at `Assets/Tests/NavigationSystem/Dev/Scenes/RouteLineLab.unity`:
   - Screen Space Overlay canvas with CanvasScaler (scale with screen size, 1920×1080, match 0.5) and an EventSystem.
   - **Panel A:** RectMask2D (square). **Panel B:** `Mask` with a circle sprite (`AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")`). **Panel C:** a nested `Canvas` (no override sorting) inside a `Mask` like B. **Panel D:** a World Space canvas placed in front of the camera.
   - The builder assigns the shader with `AssetDatabase.LoadAssetAtPath<Shader>("Assets/Gley/DrivingNavigationSystem/Runtime/Shaders/RouteLine.shader")`.
   - Each panel contains a "container" RectTransform (the map content) with a `RouteLineGraphic` child showing a sample route: zigzag, a right angle, a U-turn and a dashed last segment. Total length ~600 m.
   - The lab controller object.

**Tests:** `Assets/Tests/NavigationSystem/PlayMode/RouteLineGraphicTests.cs`
- `SetLine_BuildsMeshOnce` (canvas + graphic created in code, wait one frame, `MeshBuildCount == 1`)
- `SetTrimDistance_DoesNotRebuildMesh`
- `SetCanvasUnitsPerMeter_DoesNotRebuildMesh`
- `OnEnable_AddsTexCoord1And2ShaderChannels`

**Manual checks** (in the Route Line Lab, Play mode):
1. All four panels show the line with outline; width stays the same on screen while changing zoom.
2. Rotating the container: the line stays clipped inside each mask (square and circle), including the nested canvas panel.
3. Trim slider: the start of the line disappears (remove) / turns gray (fade). `MeshBuildCount` does not change while moving zoom, rotation or trim.
4. The dashed tail shows dashes of constant on-screen length at all zooms.
5. The U-turn corner shows a bevel, no long spike.
6. Optional (user, later, in other projects): repeat in a Built-in and an HDRP project.

**If manual check 2 fails** (clipping broken in nested canvas): stop and report. The design fallback is "one canvas, no sub-canvases".

**Done when:** tests pass and manual checks 1–5 pass.

---

## S05 — Route line chunks

**Goal:** routes longer than one mesh can hold are split into chunks; fully driven chunks are disabled.

**Depends on:** S04.

**Files:**

1. `Runtime/UI/RouteLineChunker.cs` (new, plain, internal)
   - Constant `MaxVerticesPerChunk = 16000`. Estimate 4 vertices per point (worst case) → `maxPointsPerChunk = 4000`.
   - `Split(List<Vector2> points, List<float> distances, List<bool> dashed, List<RouteLineChunk> outChunks)` where `RouteLineChunk` holds start index, count, start distance, end distance. Consecutive chunks **share their boundary point** so the line is continuous.
2. `Runtime/UI/RouteLineRenderer.cs` (new, MonoBehaviour, public)
   - Manages a pool of `RouteLineGraphic` children (created on demand, reused, never destroyed while enabled).
   - **Own sub-canvas** (design section 10): on first enable adds a `Canvas` component (no override sorting) to its own GameObject, unless the serialized `useOwnCanvas` is false. Default true; set it to false only if S04 recorded the "no sub-canvases" fallback in `PROGRESS.md`.
   - `SetShader(Shader)`: stored and forwarded to all current and future graphics.
   - `SetLine(...)`: runs the chunker, gives each chunk's slice to one graphic, disables unused graphics.
   - `SetStyle`, `SetTrimDistance`, `SetCanvasUnitsPerMeter`: forwards to all active graphics.
   - `SetTrimDistance`: in remove mode, disables graphics whose chunk end distance ≤ trim distance; re-enables them if trim goes back.

**Tests:**
- EditMode `RouteLineChunkerTests`: `Split_SmallRoute_OneChunk`, `Split_10000Points_ThreeChunks`, `Split_ChunksShareBoundaryPoint`, `Split_ChunkDistancesAreCorrect`.
- PlayMode `RouteLineRendererTests`: `Enable_AddsOwnCanvas`, `SetLine_LongRoute_CreatesMultipleGraphics`, `SetTrimDistance_PastChunkEnd_DisablesChunk`, `SetTrimDistance_Back_ReenablesChunk`, `SetLine_ShorterRoute_DisablesExtraGraphics`.

**Done when:** all tests pass.
