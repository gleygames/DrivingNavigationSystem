# Implementation Progress

Mark a step `[x]` and add the date **only after the user confirms** its tests and manual checks passed.

`[HARD]` = recommended Claude Opus 5, or Claude Sonnet 5 at xhigh effort. The agent stops and asks before starting these.

## Phase 1 — Foundation
- [ ] S01 Assemblies and test setup
- [ ] S02 Dev sandbox city and test car

## Phase 2 — Rendering spike
- [ ] S03 Route line mesh builder
- [ ] S04 [HARD] Route line shader, graphic and Route Line Lab
- [ ] S05 Route line chunks

## Phase 3 — Core data
- [ ] S06 Navigation Settings asset
- [ ] S07 Coordinate conversions and floating origin shift
- [ ] S08 Road network runtime data and builder
- [ ] S09 Road spatial grid
- [ ] S10 Map asset and data format versions

## Phase 4 — Pathfinding
- [ ] S11 Route types and snapping
- [ ] S12 [HARD] A* search
- [ ] S13 [HARD] Mid-road start and destination
- [ ] S14 Zero garbage and performance on a large network

## Phase 5 — Authoring data and bake
- [ ] S15 Authoring asset
- [ ] S16 Curves, adaptive sampling and ground probe
- [ ] S17 Edit operations
- [ ] S18 Bake and outdated detection
- [ ] S19 Validation
- [ ] S20 Format versions and migrations
- [ ] S21 Importer interface and source replacement

## Phase 6 — Editor tools
- [ ] S22 Map scene object, rectangle handles, sync and overlay
- [ ] S23 Navigation window shell and asset locations
- [ ] S24 Scene view road drawing
- [ ] S25 [HARD] Draw mode
- [ ] S26 [HARD] Edit and Connect modes
- [ ] S27 Validate and Bake modes, Play-mode warning, build check
- [ ] S28 Project settings panel
- [ ] S29 Large network editor check (decision point: chunking)

## Phase 7 — Map image capture
- [ ] S30 Capture planner
- [ ] S31 [HARD] Capture execution, pipeline adapters and import settings
- [ ] S32 Custom images: ratio lock, guidance and template export

## Phase 8 — Runtime navigation
- [ ] S33 Vehicle motion
- [ ] S34 [HARD] Road matching
- [ ] S35 Navigation session
- [ ] S36 Reroute rules
- [ ] S37 [HARD] Navigation Manager: core loop
- [ ] S38 [HARD] Navigation Manager: API, preview flow, events and command queue
- [ ] S39 Formatter and text adapters
- [ ] S40 Navigation Events component
- [ ] S41 Floating origin misconfiguration warning

## Phase 9 — Map views
- [ ] S42 Map View core
- [ ] S43 Route display in views
- [ ] S44 Follow Car behavior
- [ ] S45 Minimap shape, compass, tap to open

## Phase 10 — Markers
- [ ] S46 Marker component, registry and grid
- [ ] S47 Marker layer in views
- [ ] S48 Off-screen arrows

## Phase 11 — Full map interaction
- [ ] S49 Interactive behavior
- [ ] S50 Pointer adapter and gestures
- [ ] S51 Tapping, preview panel and buttons
- [ ] S52 Crosshair mode and the Input System adapter

## Phase 12 — Setup and finish
- [ ] S53 Default prefabs, placeholder art, safe area, default assets
- [ ] S54 Setup window
- [ ] S55 Enter Play Mode Options audit
- [ ] S56 Performance pass

## Implementation notes

Added by the implementing agent when a step passes: anything later steps need to know that differs from the plan or isn't obvious from the code. Format: `Sxx: <note>`.

- (none yet)

## Decisions

Decisions the user makes during implementation (decision points S04, S29, S56, or answers to plan questions), with the date. Format: `YYYY-MM-DD Sxx: <decision>`.

- (none yet)
