# Implementation Plan — Read This First

This folder tells you (the implementing agent) exactly how to build the Driving Navigation System, one small step at a time.

- The **design** is in `Design/NavigationSystemDesign.md`. It is the source of truth for *what* to build.
- This folder is the source of truth for *how* and *in which order*.
- Progress is tracked in `Design/Implementation/PROGRESS.md`.

## 1. How you work (mandatory)

Each step is done in its **own conversation**. The user starts it with: *"Read Design/Implementation/00_README.md, then do step Sxx."* You have no memory of earlier steps: everything you need is in this folder and in the code.

1. **One step per conversation.** Do only the step the user named. Never start the next step, even if this one was quick.
2. **Before writing code**:
   - Read this README fully.
   - Read `PROGRESS.md` fully, **including "Implementation notes" and "Decisions"** — they describe how earlier steps were really built.
   - Check that every step under "Depends on" is marked done. If not, stop and tell the user.
   - Read the phase file that contains your step, fully, and the design sections the step references.
   - **Read the existing code you will use or change.** Every file marked "(change)", and the public/internal members of classes from earlier steps that your step calls. Use the names and signatures **as they exist in the code**, not as you remember them from the plan. If they differ from the plan, follow the code and mention it in your report.
   - Load the `csharp-code-conventions` skill (via the Skill tool) before writing or editing any C# file.
2b. **HARD steps.** Steps marked `**Difficulty: HARD**` (also tagged `[HARD]` in `PROGRESS.md`) are where cheaper models most often go wrong. After reading (rule 2) and **before writing any code**, reply with only:
   `HARD STEP: Sxx — <one line why>. Recommended: Claude Opus 5, or Claude Sonnet 5 at xhigh effort. Reply "continue" to proceed with the current setup.`
   Then wait. Proceed only after the user replies (they may switch the model or effort first). Do not ask again in the same conversation.
   **Escalation on any step:** when the loop guard (rule 6) triggers, end your message with the same recommendation: `Recommended: retry this step in a fresh conversation with Claude Opus 5, or Sonnet 5 at xhigh effort.`
3. **Write the code and the tests listed in the step.** Nothing more. Do not add features, settings or files that the step does not ask for. Only change files from other steps when your step says so or when it is required to compile; list every such change in your report.
4. **You cannot run Unity.** The user runs the tests from the Unity Test Runner and does manual checks. Write code that compiles on the first try: double-check namespaces, `using` lines, asmdef references, `InternalsVisibleTo` and member names.
5. **End with the report** (section 9). Then stop and wait.
6. **When the user reports compile errors or test failures**, fix only what they show, then report again (same format, only the changed files).
   - **Loop guard:** if the same error or the same failing test comes back after two fix attempts, **stop**. Tell the user it is looping, what you tried, and what you think the real cause is. Do not keep patching.
7. **When the user says the step passed**:
   - Mark it done in `PROGRESS.md` (`[ ]` → `[x]` + date).
   - Add a short entry under **"Implementation notes"** in `PROGRESS.md` if anything a later step needs to know differs from the plan or isn't obvious from the code (a renamed member, an extra helper, a workaround, a Unity quirk you hit). Keep it to a few lines; write "none" only if truly nothing.
8. **Committing — only when the user asks** (section 3 has the two repositories):
   - Check `git -C Assets/Gley/DrivingNavigationSystem status`. **If the submodule is in "detached HEAD" state, stop and ask the user which branch to use** (commits made on a detached HEAD get lost).
   - Commit the package changes **inside the submodule first**, then in the outer repository commit the tests/dev/docs changes **and the updated submodule pointer**.
   - Message format for both: `Sxx: <step title>`.
   - Never push unless the user asks.
9. **Decision points** (S04, S29, S56, or whenever the plan says "stop and ask"): start your report's first line with **`DECISION NEEDED:`** and a one-line question. Do not implement any option until the user answers. The user records the answer under "Decisions" in `PROGRESS.md` (you may write it there when asked).
10. **If something in the design or this plan is unclear, contradicts itself, or seems wrong: stop and ask the user.** Do not guess, do not redesign. Start the report with **`PLAN QUESTION:`**.

## 2. Project facts

- Unity **2022.3** (minimum supported), render pipeline in this project: **URP 14**.
- Packages present: Test Framework 1.1.33, TextMeshPro 3.0.7, uGUI. **Input System is NOT installed.**
- Gley `Common` package is available at `Assets/Gley/Common` (assemblies `Gley.Common`, `Gley.Common.Editor`). Useful: `Gley.Common.CustomLogger` (logging), `Gley.Common.Editor.WindowLoader`, `ISettingsWindowProperties`, `IVersion`, `EditorUtilities`, `PreprocessorDirective`.

## 3. Repositories and folders

There are **two git repositories**:

| What | Path | Repository |
|---|---|---|
| The shipped package (runtime + editor code, shaders, prefabs, art) | `Assets/Gley/DrivingNavigationSystem/` | **Submodule** (its own repo) |
| Tests, dev tools, design docs | everything else (`Assets/Tests/NavigationSystem/`, `Design/`) | Outer project repo |

Package folder layout (create folders as steps need them):

```
Assets/Gley/DrivingNavigationSystem/
  Runtime/
    Core/          settings, coordinates, logging helpers, command queue
    Data/          map asset, road network runtime asset, builder, grid
    Pathfinding/   requests, route result, A*, snapping
    Tracking/      vehicle tracker, map matching
    Navigation/    navigation session, reroute rules, manager
    Markers/       marker component, registry, grid
    UI/            map view, behaviors, route line, marker layer, adapters, formatter
    Shaders/       RouteLine.shader
  Runtime.TMP/     Gley.NavigationSystem.TMP assembly
  Runtime.InputSystem/  Gley.NavigationSystem.InputSystem assembly
  Editor/
    Authoring/     authoring asset, curves, sampling, edit operations, bake, validation
    Tools/         navigation window, scene drawing, modes
    Capture/       capture planner and executor
    Setup/         setup window, build check, migrations
  Editor.URP/      URP-specific capture adapter assembly
  Editor.HDRP/     HDRP-specific capture adapter assembly
  Prefabs/
  Art/             placeholder sprites
```

Test and dev layout (outer repo, **never shipped**):

```
Assets/Tests/NavigationSystem/
  EditMode/        Gley.NavigationSystem.Tests.Editor  (EditMode tests)
  PlayMode/        Gley.NavigationSystem.Tests.Runtime (PlayMode tests)
  Shared/          Gley.NavigationSystem.Tests.Shared  (test helpers: network generator, fakes)
  Dev/             Gley.NavigationSystem.Dev           (dev-only runtime scripts: test car)
  Dev/Editor/      Gley.NavigationSystem.Dev.Editor    (dev-only menus: sandbox builders, labs)
```

## 4. Namespaces

- Runtime: `Gley.NavigationSystem` (all runtime code, all subfolders use this same namespace).
- Editor: `Gley.NavigationSystem.Editor`.
- TMP: `Gley.NavigationSystem.TMP`. Input System: `Gley.NavigationSystem.InputSystem`.
- Tests: `Gley.NavigationSystem.Tests`. Dev: `Gley.NavigationSystem.Dev`.

## 5. Code rules (summary — the `csharp-code-conventions` skill is the full version)

- **No comments** of any kind (no `//`, no XML docs).
- **Always braces** for every `if/else/for/foreach/while/using`.
- **No ternary** `?:` — use `if/else`.
- **No LINQ, no lambdas.** Use `for`/`foreach` loops. Use method groups for delegates (e.g. `Assert.Throws<ArgumentException>(CallThatThrows)`).
- **Properties, not public fields.** Unity-serialized data: `[SerializeField] private` field + public property.
- **No static classes or static methods in new code.** Use instance helper classes. **Exception:** entry points Unity forces to be static (`[MenuItem]`, `[InitializeOnLoad]` static constructor, `[InitializeOnEnterPlayMode]`, `[RuntimeInitializeOnLoadMethod]`, `[DidReloadScripts]`). Keep those one line that forwards to an instance.
- **Per-tick methods** are named `Update<Domain><Phase>(float deltaTime)`, e.g. `UpdateTrackingLogic(deltaTime)`, `UpdateMapViewVisuals(deltaTime)`. Unity's `LateUpdate` only forwards into them.
- **Member order** and field lanes as in the skill.
- **No mutable static state** in runtime code (domain reload may be off).
- Unsubscribe every event you subscribe to, in `OnDisable` / `OnDestroy`.
- **Zero garbage per frame** in runtime per-frame code: no `new` of classes/arrays/lists, no string building, no boxing in `Update*` paths. Pre-allocate and reuse.
- Logging only through `Gley.Common.CustomLogger` (`Log`, `LogWarning`, `LogError`).
- Pure logic goes into **plain C# classes** (not MonoBehaviours) so it can be tested in EditMode. MonoBehaviours stay thin: they hold references, forward Unity callbacks and call plain classes.
- **One `MonoBehaviour` or `ScriptableObject` per file, and the file name must equal the class name** (Unity requires this, otherwise components/assets can't be created or loaded).
- **Never create or edit `.meta` files.** Unity generates them when the user switches back to the editor.
- New package files go under `Assets/Gley/DrivingNavigationSystem/` (the submodule); tests and dev tools under `Assets/Tests/NavigationSystem/`.
- **Visibility across assemblies:** a type or member used from another assembly must be `public` if it is part of the user-facing API; otherwise keep it `internal` and make sure the using assembly is in the `InternalsVisibleTo` lists from S01 (runtime -> TMP, InputSystem, Editor, tests, Dev, Dev.Editor; editor -> Editor.URP, Editor.HDRP, Tests.Editor, Dev.Editor). If a needed assembly is missing from a list, add it.

## 6. Units and coordinate spaces (use these names in code)

| Space | Meaning | Type |
|---|---|---|
| **World** | Unity scene coordinates, in the user's units, current floating-origin position | `Vector3` |
| **True** | Meters, floating-origin shift removed: `true = (world − shift) / unitsPerMeter` | `Vector3` (only X/Z used for logic; Y kept for display/AI points) |
| **Map** | Meters inside the map rectangle, X right / Y up, origin at the rectangle's bottom-left corner, rotated with the rectangle | `Vector2` |
| **Canvas** | uGUI canvas units | `Vector2` |

Rules:
- All stored asset data (road points, rectangle position, intersections) is in **True** meters.
- Convert World → True the moment a position enters the system; convert True → World only when returning data to the user.
- Speeds are stored in **meters per second**. Distances in **meters**. Time in **seconds**.

## 7. Tests

- Framework: Unity Test Framework (NUnit). The user runs them via **Window > General > Test Runner** (EditMode and PlayMode tabs).
- **EditMode tests** (`Assets/Tests/NavigationSystem/EditMode`): pure logic, editor code, asset creation. Fast. Most tests go here.
- **PlayMode tests** (`Assets/Tests/NavigationSystem/PlayMode`): MonoBehaviour behavior over frames (`[UnityTest]` + `yield return null`). Build GameObjects in code; **do not use scene files**. Destroy everything you create in `[TearDown]`.
- Test class name = `<ClassUnderTest>Tests`. Test method name = `<Method>_<Situation>_<Expected>`, e.g. `FindRoute_OneWayAgainstDirection_ReturnsNoPath`.
- Every test must be deterministic (no random without a fixed seed, no real time).
- Tests that create assets on disk use a temp folder `Assets/Tests/NavigationSystem/Temp/` and delete it in `[TearDown]`.
- Floating-point asserts use a tolerance (`Assert.AreEqual(expected, actual, 0.001f)`).
- Test code follows the same code rules (section 5).

## 8. Manual checks

Some steps have **manual checks** (visual or editor-tool behavior). Write them in your report as a short numbered list the user can follow in Unity, with the exact menu path / scene / action and what they should see.

Dev-only tools (sandbox scenes, labs, generators) live in the `Dev` assemblies under `Assets/Tests/NavigationSystem/Dev` and are opened from the menu **Tools > Gley > Navigation Dev > ...**. They build scenes procedurally; never hand-write `.unity` YAML.

## 9. End-of-step report (always this format)

```
## Step Sxx — <title> — ready for testing

(If needed, the first line instead is `DECISION NEEDED: ...` or `PLAN QUESTION: ...`.)

Files created/changed:
- <path> (new/changed)

Tests to run:
- EditMode: <TestClass> (<n> tests)
- PlayMode: <TestClass> (<n> tests)

Manual checks:
1. ...

Changes to files from other steps:
- <path>: <why> (or "none")

Deviations from the plan:
- <what and why> (or "none")

Notes / questions:
- <anything unclear, or "none">
```

## 10. Phase files

| File | Steps |
|---|---|
| `01_Foundation.md` | S01–S02 |
| `02_RenderingSpike.md` | S03–S05 |
| `03_CoreData.md` | S06–S10 |
| `04_Pathfinding.md` | S11–S14 |
| `05_AuthoringAndBake.md` | S15–S21 |
| `06_EditorTools.md` | S22–S29 |
| `07_Capture.md` | S30–S32 |
| `08_RuntimeNavigation.md` | S33–S41 |
| `09_MapViews.md` | S42–S45 |
| `10_Markers.md` | S46–S48 |
| `11_Interaction.md` | S49–S52 |
| `12_SetupAndFinish.md` | S53–S56, S56b |
