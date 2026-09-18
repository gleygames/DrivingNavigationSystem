# Phase 1 — Foundation (S01–S02)

---

## S01 — Assemblies and test setup

**Goal:** all assemblies reference each other correctly, tests can see internal code, and one smoke test runs in each test mode.

**Depends on:** nothing.

**Files:**

1. `Assets/Gley/DrivingNavigationSystem/Runtime/Gley.NavigationSystem.asmdef` (change)
   - `references`: `["Gley.Common", "UnityEngine.UI"]` (uGUI types such as `MaskableGraphic`, `RawImage`, `Mask` live in `UnityEngine.UI`)
2. `Assets/Gley/DrivingNavigationSystem/Editor/Gley.NavigationSystem.Editor.asmdef` (change)
   - `references`: `["Gley.NavigationSystem", "Gley.Common", "Gley.Common.Editor"]`
3. `Assets/Gley/DrivingNavigationSystem/Runtime/AssemblyInfo.cs` (change)
   - Keep the existing class. Add at the top (after `using System.Runtime.CompilerServices;`):
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Editor")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Tests.Editor")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Tests.Runtime")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Tests.Shared")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.TMP")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.InputSystem")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Dev")]`
     - `[assembly: InternalsVisibleTo("Gley.NavigationSystem.Dev.Editor")]`
4. `Assets/Gley/DrivingNavigationSystem/Editor/AssemblyInfo.cs` (change)
   - Add `InternalsVisibleTo` for `Gley.NavigationSystem.Tests.Editor`, `Gley.NavigationSystem.Dev.Editor`, `Gley.NavigationSystem.Editor.URP`, `Gley.NavigationSystem.Editor.HDRP`.
5. `Assets/Tests/NavigationSystem/Shared/Gley.NavigationSystem.Tests.Shared.asmdef` (new)
   - `references`: `["Gley.NavigationSystem"]`, `defineConstraints`: `["UNITY_INCLUDE_TESTS"]`, `includePlatforms`: `[]`, `autoReferenced`: false.
6. `Assets/Tests/NavigationSystem/EditMode/Gley.NavigationSystem.Tests.Editor.asmdef` (new)
   - `references`: `["Gley.NavigationSystem", "Gley.NavigationSystem.Editor", "Gley.NavigationSystem.Tests.Shared", "Gley.NavigationSystem.Dev", "Gley.NavigationSystem.Dev.Editor", "Gley.Common", "UnityEngine.UI", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`
   - `includePlatforms`: `["Editor"]`, `overrideReferences`: true, `precompiledReferences`: `["nunit.framework.dll"]`, `defineConstraints`: `["UNITY_INCLUDE_TESTS"]`, `autoReferenced`: false.
7. `Assets/Tests/NavigationSystem/PlayMode/Gley.NavigationSystem.Tests.Runtime.asmdef` (new)
   - `references`: `["Gley.NavigationSystem", "Gley.NavigationSystem.Tests.Shared", "Gley.Common", "UnityEngine.UI", "UnityEngine.TestRunner"]`
   - `includePlatforms`: `[]`, `overrideReferences`: true, `precompiledReferences`: `["nunit.framework.dll"]`, `defineConstraints`: `["UNITY_INCLUDE_TESTS"]`, `autoReferenced`: false.
8. `Assets/Tests/NavigationSystem/Dev/Gley.NavigationSystem.Dev.asmdef` (new)
   - `references`: `["Gley.NavigationSystem", "UnityEngine.UI"]`, `autoReferenced`: false.
9. `Assets/Tests/NavigationSystem/Dev/Editor/Gley.NavigationSystem.Dev.Editor.asmdef` (new)
   - `references`: `["Gley.NavigationSystem", "Gley.NavigationSystem.Editor", "Gley.NavigationSystem.Dev", "Gley.NavigationSystem.Tests.Shared", "UnityEngine.UI"]`, `includePlatforms`: `["Editor"]`, `autoReferenced`: false. (S53 adds the TMP references.)
   - Note: Tests.Shared has the `UNITY_INCLUDE_TESTS` constraint; this is defined in the editor, so the reference works in the editor.
10. `Assets/Tests/NavigationSystem/EditMode/SmokeTests.cs` (new) — one `[Test]` that asserts `true`.
11. `Assets/Tests/NavigationSystem/PlayMode/SmokePlayTests.cs` (new) — one `[UnityTest]` that yields one frame and asserts `true`.

**Tests:** `SmokeTests` (1), `SmokePlayTests` (1).

**Manual checks:**
1. Unity compiles with no errors.
2. Test Runner shows both smoke tests in the right tabs and they pass.

**Done when:** both smoke tests pass and there are no compile errors.

---

## S02 — Dev sandbox city and test car

**Goal:** a procedurally built sandbox scene with a small grid city and a drivable car, for manual checks in later steps.

**Depends on:** S01.

**Files:**

1. `Assets/Tests/NavigationSystem/Dev/DevCarController.cs` (new, MonoBehaviour)
   - Kinematic keyboard car: W/S = accelerate/brake-reverse, A/D = steer, Space = hard stop, R = teleport 300 m forward (to test teleports later).
   - Uses the legacy `Input` class inside `#if ENABLE_LEGACY_INPUT_MANAGER` (the project uses the old Input Manager).
   - Max speed 30 m/s, reverse max 8 m/s. Moves the Transform directly in `Update` (no Rigidbody). Keeps the car on Y = 0.5.
   - Public properties: `Speed` (m/s, signed).
2. `Assets/Tests/NavigationSystem/Dev/DevFollowCamera.cs` (new) — camera follows the car from behind/above, smoothly.
3. `Assets/Tests/NavigationSystem/Dev/Editor/SandboxSceneBuilder.cs` (new)
   - Menu: **Tools > Gley > Navigation Dev > Create Sandbox Scene** (static menu method that forwards to an instance).
   - Creates a new scene saved at `Assets/Tests/NavigationSystem/Dev/Scenes/Sandbox.unity`:
     - Ground plane 1000 × 1000 m with a collider (layer Default).
     - A grid of roads: 6 × 6 blocks, block size 100 m, roads 12 m wide, as flat boxes (height 0.2 m) with BoxColliders on a layer named `Road` if it exists, else Default.
     - Buildings: one box per block (random heights with fixed seed 1234), with colliders.
     - One diagonal road (to test curves later) and one bridge: a raised road deck at Y = 6 crossing one grid road, with ramps.
     - A car: a 2 × 1 × 4 m box with `DevCarController`, placed on a road.
     - Main camera with `DevFollowCamera`, a directional light.
4. `Assets/Tests/NavigationSystem/EditMode/SandboxSceneBuilderTests.cs` (new)
   - `BuildCityObjects_DefaultSettings_CreatesExpectedRoadCount` — call the builder's object-creation method on a temporary empty scene (use `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)` and close it in TearDown) and assert the number of road objects is (6+1)·2 grid lines + 1 diagonal + bridge parts as defined in your builder (assert the exact number your builder defines as a constant).

**Manual checks:**
1. Run the menu item. The scene opens with the city, the bridge and the car.
2. Press Play. Drive with WASD, reverse with S, stop with Space, R teleports forward.

**Done when:** the test passes and the car drives in the sandbox.
