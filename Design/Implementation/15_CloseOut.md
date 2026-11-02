# S74 — Close-out

**Depends on:** every other step (S01–S73).

**Goal:** leave `Design/NavigationSystemDesign.md` describing exactly what was built, then delete this plan.

The plan is scaffolding: once the asset is built, these files describe what was planned, not what exists, and a later agent would read them as instructions. The design document stays as the only description of the asset.

**Work:**

1. Read `PROGRESS.md`. If any other step is not ticked, stop and list them. Do nothing else.
2. For each entry under *Decisions*, and each *Implementation note* that changes behavior, a setting, a default or a rule: make sure the design document says it. Drop notes that only mattered to a later step.
3. Read the design document against the code (section 3 of `00_README.md` says where it is), section by section: names, defaults, menus, data paths, API members, behavior rules.
   - The code is the agreed result → fix the document.
   - The code might be wrong → stop with `DECISION NEEDED:`. Never change code in this step.
4. Set the document's `Status:` line to `Status: Implemented (<date>).`, remove done items from its open/deferred list, and remove every reference to `Design/Implementation/`, `PROGRESS.md` or step numbers, since they will no longer exist.
5. Delete `Design/Implementation/` (and its `.meta`, if it has one). Git history keeps the plan.

**Report:** instead of tests, list the design-document changes (section → what changed), every mismatch found and how it was resolved, and confirm what was deleted.

**Done when:** the user has reviewed the design-document changes. Commit only when asked: `Close out implementation plan`.
