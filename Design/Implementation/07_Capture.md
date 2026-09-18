# Phase 7 — Map Image Capture (S30–S32)

Design references: section 9 "Map picture (capture tool)", "Map rectangle" (custom image, exact ratio, guidance), "Technical risks" (max image size), "Cross-cutting" (asset locations), "Default values → Editor, capture".

---

## S30 — Capture planner (pure logic)

**Goal:** all capture math, testable without rendering.

**Depends on:** S07.

**Files:** `Editor/Capture/CapturePlanner.cs` (plain class) and small result types.

1. `PlanImageSize(Vector2 rectSizeMeters, int longerSidePixels)` → `ImageSizePlan { int widthPx, int heightPx, float metersPerPixel, Vector2 adjustedRectSize }`:
   - `longerSidePixels` rounded **up** to a multiple of 4.
   - `metersPerPixel = longerSideMeters / longerSidePixels`.
   - Shorter side: `px = ceil((shorterMeters / metersPerPixel) / 4) * 4`; adjusted shorter meters = `px * metersPerPixel` (only grows, by less than 4 pixels' worth). The center stays the same (the caller applies the new size around the center).
2. `PlanPieces(ImageSizePlan plan, float pieceSizeMeters, int overlapPx)` → list of `CapturePiece { RectInt outputRect (pixels in the final image, no overlap), RectInt renderRect (with overlap, clipped to image), Vector2 centerMap (Map meters), float orthoSize (half height in meters), int renderWidthPx, int renderHeightPx }`:
   - Piece pixel size = `round(pieceSizeMeters / metersPerPixel)`, clamped to at most 4096 − 2·overlap.
   - Pieces tile the image left-to-right, bottom-to-top; the last row/column may be smaller.
   - A map smaller than one piece → exactly 1 piece.
3. `PlanCameraDepth(float minY, float maxY)` → `CameraDepthPlan { float cameraY, float near, float far }`: `cameraY = maxY + 10`, `near = 0.1`, `far = cameraY − minY + 10` (meters; the executor converts to world units).
4. `AverageEdgeColor(Color32[] pixels, int width, int height)` → Color: average of all pixels on the 4 borders.

**Tests:** `EditMode/CapturePlannerTests.cs`
- `PlanImageSize_Square_BothSidesEqualMultipleOf4`
- `PlanImageSize_1000x733_2048_Gives2048x1504_SquarePixels` (compute and assert exact expected values: mpp = 1000/2048; 733/mpp = 1501.18 → 1504; adjusted height = 1504 · mpp)
- `PlanImageSize_RoundsLongerSideUpToMultipleOf4`
- `PlanImageSize_OnlyGrows_LessThan4Pixels`
- `PlanPieces_SmallMap_OnePiece`
- `PlanPieces_OutputRectsCoverImageWithoutOverlap`
- `PlanPieces_RenderRectsIncludeOverlap_ClippedToImage`
- `PlanPieces_NoPieceLargerThan4096`
- `PlanCameraDepth_CoversMinToMax`
- `AverageEdgeColor_UniformBorder_ReturnsThatColor`

---

## S31 — Capture execution, pipeline adapters and import settings

**Difficulty: HARD** (multi-pipeline rendering, stitching, import settings). Follow the HARD step rule in `00_README.md` section 1 before writing any code.

**Goal:** render the pieces, stitch, save the PNG with the right import settings, lock the rectangle.

**Depends on:** S30, S23.

**Files:**

1. `Editor/Capture/CaptureSettings.cs` (`[Serializable]`, stored in `NavigationEditorPrefs` as JSON): `longerSidePixels` (2048), `layers` (LayerMask, Everything), `disableFog` (true), `disablePostEffects` (true), `fillColor` (dark gray), `useOwnCamera` (bool), `ownCamera` (not persisted; picked in UI), `hdrpExposure` (float EV, default 13).
2. `Editor/Capture/ICapturePipelineAdapter.cs` (public interface): `bool IsActive();` (checks `GraphicsSettings.currentRenderPipeline` type name), `void Prepare(Camera camera, CaptureSettings settings);`, `void Restore();`, `bool TryGetDefaultExposure(out float ev);` (Built-in/URP return false). `CaptureSettings` is `public` (used by the adapter assemblies).
3. `Editor/Capture/BuiltInCaptureAdapter.cs`: active when no render pipeline asset; `Prepare`/`Restore` do nothing.
4. `Editor.URP/Gley.NavigationSystem.Editor.URP.asmdef` (new): references `Gley.NavigationSystem.Editor`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`; `includePlatforms: ["Editor"]`; `versionDefines`: `{ "name": "com.unity.render-pipelines.universal", "expression": "", "define": "GLEY_NAV_URP" }`; `defineConstraints: ["GLEY_NAV_URP"]`.
   `Editor.URP/UrpCaptureAdapter.cs`: sets `UniversalAdditionalCameraData.renderPostProcessing = !disablePostEffects` on the capture camera.
5. `Editor.HDRP/Gley.NavigationSystem.Editor.HDRP.asmdef` (new): same pattern with `com.unity.render-pipelines.high-definition` → `GLEY_NAV_HDRP`, references `Unity.RenderPipelines.HighDefinition.Runtime`, `Unity.RenderPipelines.Core.Runtime`.
   `Editor.HDRP/HdrpCaptureAdapter.cs`: `TryGetDefaultExposure`: looks at the scene's global volumes' `Exposure` override; if its mode is **Fixed**, returns its value (design: start from the scene's exposure when it's fixed). `Prepare`: creates a temporary global `Volume` (highest priority, on a temporary GameObject with `HideFlags.HideAndDontSave`) with an `Exposure` override in **Fixed** mode set to `hdrpExposure`; when `disablePostEffects`, also disables other post overrides through that volume. Removes everything in `Restore`. **This project has no HDRP: the code only needs to compile when HDRP is present; the user verifies it later in an HDRP project.**
6. `Editor/Capture/MapCaptureExecutor.cs` (plain class)
   - **Units:** the planner works in meters. Convert to world units with `unitsPerMeter` for everything given to Unity (camera position, `orthographicSize`, near/far), and convert renderer bounds from world units to meters before `PlanCameraDepth`.
   - Finds the active adapter with `TypeCache.GetTypesDerivedFrom<ICapturePipelineAdapter>()` (instantiate each, pick the first `IsActive()`; Built-in as fallback). No static registry.
   - Scene bounds: all `Renderer`s (`Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)`) on the included layers whose bounds overlap the rectangle in X/Z → min/max Y → `PlanCameraDepth`.
   - Camera: a temporary camera (`HideFlags.HideAndDontSave`); with "use own camera": `tempCamera.CopyFrom(ownCamera)` first. Then always set: orthographic, rotation `(90, rectangleRotationY, 0)`, `orthographicSize`, position per piece, near/far, `cullingMask = layers`, `clearFlags = SolidColor`, `backgroundColor = fillColor`, `aspect` = piece aspect.
   - Fog: remember `RenderSettings.fog`, set false if `disableFog`, restore in `finally`.
   - Per piece: `RenderTexture` (renderRect size), `camera.targetTexture = rt`, `camera.Render()`, `ReadPixels` into a reused piece `Texture2D`, copy the **outputRect** part into the final `Texture2D` (RGB24).
     - **Risk:** if the user reports black or empty captures in URP, switch the render call to `RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt })`.
   - Always destroy temporaries and restore state in `finally`.
   - Save: `EncodeToPNG` → `<Name>_MapImage.png` in the map folder (overwrite if it exists — same file, references kept). `AssetDatabase.ImportAsset`.
   - Update `MapData`: rectangle size = the adjusted size (center kept), `image`, `imageState = Captured`, `locked = true`, `outsideMapColor = AverageEdgeColor`. `SetDirty`, save.
   - `CapturePreview(...)`: same pipeline at 512 px longer side, returns a `Texture2D` (not saved).
7. `Editor/Capture/MapImageImportSettings.cs` (plain class)
   - `ApplyIfNew(string assetPath, bool isNewFile, int longerSidePixels)`: only when the PNG was **created** now (not on recapture): `textureType Default`, `npotScale None` (**important**: otherwise Unity rescales to a power of two), `mipmapEnabled true`, `wrapMode Clamp`, `sRGBTexture true`, `isReadable false`, `maxTextureSize` = smallest valid size ≥ longer side (max 16384), compression Normal; platform overrides `Android` and `iPhone`: `overridden = true`, `maxTextureSize = 4096`. `SaveAndReimport()`.
   - `GetWarnings(string assetPath, List<string> output)`: size not multiple of 4, `npotScale != None`, mipmaps off, mobile max size > 4096 → one message each (used for custom images).
8. `Editor/Capture/MapImagePanel.cs` (IMGUI drawing class, reused later by the Setup window): shows capture settings, a warning when resolution > 4096 ("mobile builds will use a reduced version"), **HDRP-only** exposure field (shown when the active adapter is HDRP; its initial value comes from `TryGetDefaultExposure` if that returns true, else 13, and it stops following that once the user edits the field), Preview button + preview image, **Capture** button (confirm dialog when it will overwrite), the lock state and the "Change area" button. For now, show it in the Navigation window under a "Map Image" foldout.

**Tests:** `EditMode/MapImageImportSettingsTests.cs` (create a small PNG in the Temp folder)
- `ApplyIfNew_New_SetsNpotNoneMipmapsClamp`
- `ApplyIfNew_New_MobileOverrides4096`
- `ApplyIfNew_NotNew_DoesNotChangeUserSettings`
- `GetWarnings_BadSettings_ReturnsMessages`
- `EditMode/MapCaptureExecutorTests.cs`: `Capture_EmptyScene_ProducesFillColorImage_AndLocksMap` (tiny 64 px capture in a temp scene; the image is all fill color; MapData locked, state Captured)

**Manual checks (sandbox):**
1. Preview, then Capture at 2048: the saved PNG shows the city top-down with shadows; no visible seams between pieces (try piece size 100 m to force many pieces).
2. Recapture: same file, references still OK.
3. Exclude the building layer (if you put buildings on their own layer): roofs disappear.
4. The rectangle is locked after capture; "Change area" unlocks it and shows "outdated".

---

## S32 — Custom images: ratio lock, guidance and template export

**Goal:** assigning an artist's image (design: custom image, guidance for artists).

**Depends on:** S31.

**Files:**

1. `Editor/Capture/CustomImageAssigner.cs` (plain class)
   - `Assign(MapData data, Texture2D image)`: `imageState = Custom`; rectangle height = width × (imageHeight / imageWidth), center kept; `locked = false` (custom images are aligned by moving the rectangle; the ratio is locked by `ResizeFromCorner(keepRatio)` from S22).
   - `GetGuidance(MapData data)` → `ImageGuidance { float ratio, string ratioText ("1000 × 733 m → 1.364 : 1"), recommended sizes for 2048 and 4096 on the longer side (from `PlanImageSize`) with meters per pixel }`.
2. `Editor/Capture/TemplateExporter.cs` (plain class)
   - `Export(MapData data, RoadNetworkData roads, ImageSizePlan size, string path)`: creates a `Texture2D` of that size; background = the current image scaled to fit (if any) else plain mid-gray; draws every road as a 3-pixel-wide dark line (CPU line drawing between consecutive points, converted True → Map → pixels); saves `<Name>_MapTemplate.png` in the map folder. No import settings changes.
   - Internal helper `MapToPixel(Vector2 map, ImageSizePlan size)` (tested).
3. `MapImagePanel` (change): "Assign custom image" object field (→ `Assign` + import warnings from `GetWarnings`), the guidance box, a size dropdown and **Export template** button.

**Tests:** `EditMode/CustomImageAssignerTests.cs`, `EditMode/TemplateExporterTests.cs`
- `Assign_SetsCustomState_AdjustsHeightToImageRatio_KeepsCenter`
- `GetGuidance_1000x733_RatioText`
- `GetGuidance_RecommendedSizesAreMultiplesOf4`
- `MapToPixel_Corners` (map (0,0) → pixel (0,0); map size → (width−1, height−1))
- `Export_DrawsRoadPixels` (one straight road across the middle: the middle row has dark pixels; corners stay background)

**Manual checks:** export a template, paint on it in any editor, assign it: the overlay lines up with the roads; resizing the rectangle keeps the image ratio.
