# Objective and Journal System Design

## Purpose

Create a data-driven in-game **Journal** that acts primarily as a guided quest log. It tracks active and completed quests, their objectives and progress, permanently discovered lore notes, the currently tracked quest, and related player-facing UI feedback.

The system must not require custom quest code for individual quests. Story content is authored in YAML, while scene components explicitly apply reusable journal actions.

## Current Project Integration

The project already has these relevant foundations:

- `GameSession` persists between scenes and owns `GameState` and `SaveManager`.
- `GameState` currently persists flags and checkpoints.
- Dialogue YAML can read and set flags.
- `InteractionOption` can set flags and invoke Unity events.
- `InteractionTrigger` currently handles proximity for interactables only; it does not execute automatic area actions.
- The project uses the Input System, with a `Player` action map.

The Journal system is separate from dialogue flags. It may be updated by scene triggers now, and by interactions later through the same Journal Action list.

## Player Experience

### Journal contents

The full-screen Journal has three tabs:

- **Active**: active main and side quests.
- **Completed**: compact archive of completed quests; selecting one opens its details.
- **Notes**: permanently discovered lore notes in chronological discovery order.

The Active tab sorts quests as follows:

1. Main quests before side quests.
2. Newest acquired first inside each quest type.

Completed quests are sorted newest completed first.

### Quest presentation

Every quest displays:

- Title.
- Summary.
- Status.
- Current objective text.
- Current objective location, when defined.
- Current objective giver/source, when defined.
- Objective progress, when applicable.
- Checklist of objectives in authored story order.
- Tracked state.
- Completion summary in the Completed tab.

Completed objectives use a checkmark and strikethrough. Their authored text does not change after completion.

Future objectives appear as `???` until they are activated. Objectives can be active in parallel, but the Journal and HUD use a single current/primary objective: the first incomplete active objective in YAML order.

### Quest tracking and HUD

- The player can select any active quest as tracked using a **Track** button.
- Once a quest exists, there must be a selected tracked quest.
- Starting a new quest does not replace the currently tracked quest.
- When the tracked quest completes, the newest remaining main quest becomes tracked.
- The tracked quest HUD displays only one objective: the current/primary objective.
- The HUD displays quest title, current objective, location, numeric progress, and main/side label.
- The HUD is in the top-right corner.
- `H` toggles HUD visibility.
- The HUD is hidden before the initial quest exists.

### Journal controls and pausing

- `J` opens and closes the Journal.
- `Escape` closes the Journal.
- The Journal is a full-screen page.
- Opening the Journal blocks player movement, look, and interactions.
- Opening the Journal does **not** pause global time with `Time.timeScale`.

### Notes

Notes are lore items, such as letters, pictures, and records. They are not associated with quests.

- Notes preserve exact authored text.
- Notes can be text-only, image-only, or contain both text and an image.
- A discovered note opens immediately in a full-screen reader and blocks player controls.
- Viewing the note once marks it as read.
- Closing a note returns directly to gameplay.
- The Notes tab remains chronological by discovery order.
- The note reader has on-screen Previous and Next buttons.
- Notes with text and an image show them side-by-side.
- Images preserve their native aspect ratio within the reading area.

### Feedback

Quest changes queue toasts for 2-3 seconds. The common format is:

`Journal Updated: [Info]`

Examples:

- `Journal Updated: Restore the Harvest Feast`
- `Journal Updated: Find the bakery`
- `Journal Updated: Gather wildflowers (3/10)`
- `Journal Updated: Found the bakery`
- `Journal Updated: Restore the Harvest Feast complete`

Notes do not display a toast because they open directly.

Quest updates use one shared journal-update sound for now. Toasts use distinct placeholder icons for quest updates, objective updates, and quest completion.

## Runtime Components

### JournalDatabase

`JournalDatabase` loads all quest and note definitions automatically at startup.

Content locations:

- `Assets/Resources/Journal/Quests/`
- `Assets/Resources/Journal/Notes/`

Each quest YAML file may declare multiple quests. Each note YAML file may declare multiple notes. Quest and note content remain in separate files/folders.

The database validates content and logs then skips invalid entries. Validation includes:

- Duplicate quest IDs.
- Duplicate note IDs.
- Duplicate objective IDs within a quest.
- Invalid stable IDs.
- Invalid references from actions to quests or objectives.
- Missing Resources image paths.
- More or fewer than one `autoStart` quest.

There is exactly one quest with `autoStart: true`.

### JournalState

`JournalState` is a persistent sibling component to `GameState`, owned by `GameSession`.

It stores runtime Journal state only; dialogue flags remain in `GameState`.

JournalState records:

- Which quests have started.
- Which quests have completed.
- Quest acquisition order.
- Quest completion order.
- Objective activation state.
- Objective completion state.
- Objective numeric progress.
- The tracked quest ID.
- Which notes were discovered.
- Note discovery order.
- Note read state.
- Used one-time `JournalAreaTrigger` IDs.

If a saved quest or note definition no longer exists in YAML, its saved state is hidden and a warning is logged. The save remains usable.

### JournalAreaTrigger

`JournalAreaTrigger` is a new automatic on-enter component. It remains independent from the existing `InteractionTrigger`.

Each JournalAreaTrigger has:

- A manually authored, permanent lowercase snake-case trigger ID.
- A list of Journal Actions.
- One-time-per-save behavior.

On player entry, it runs its actions and always records its trigger ID as used. Re-entering the volume does nothing after that.

Duplicate trigger IDs do not require cross-scene validation at this stage, but IDs must be authored carefully and kept stable after release so save data remains valid.

### Journal Actions

Journal Actions are reusable serialized data. A trigger may contain several actions, and valid actions continue to execute if another action is invalid. Invalid actions warn in development builds while presenting no player-facing error.

Supported action types:

- `StartQuest`
- `ActivateObjective`
- `CompleteObjective`
- `AddObjectiveProgress`
- `SetObjectiveProgress`
- `CompleteQuest`
- `SetTrackedQuest`
- `DiscoverNote`

Actions target stable quest, objective, and note IDs rather than display text.

The same action list is intended to be added to `InteractionOption` later. This lets an interaction set flags, invoke Unity events, and update Journal state in a single interaction without hard-coded quest behavior.

## Quest Rules

### Starting and completion

- Quest starts are explicit. A Journal Action starts the quest.
- One starter quest is defined in YAML with `autoStart: true`.
- The starter quest starts only for a brand-new game, not when loading an existing save or migrating an old save.
- Starting the same quest again does nothing.
- A quest can be completed automatically when all required objectives complete.
- A final "turn in" conversation is modeled as the final required objective; completing that objective completes the quest.
- A trigger can update several quests in one activation.

### Objectives

- An objective is always required for quest completion.
- Objectives use authored story order.
- An objective may be inactive/future, active, or completed.
- Future objectives are displayed as `???`.
- A trigger can activate several objectives at once.
- A trigger can complete an objective directly, including as an alternate solution.
- A quest can have several active objectives simultaneously.
- The current/primary objective is the first incomplete active objective in YAML order.
- The player cannot manually choose the primary objective in this version.

### Numeric progress

- An objective may optionally declare `progressTarget`.
- Numeric objectives display `0/target` before the first update.
- `AddObjectiveProgress` changes the current amount by a value.
- `SetObjectiveProgress` sets the current amount exactly.
- Reaching the progress target automatically completes the objective.

## YAML Definitions

All visible text supports TextMeshPro rich text.

All stable IDs use lowercase snake case, for example `restore_harvest_feast`, `find_bayker`, and `torn_ritual_letter`.

### Quest YAML example

```yaml
quests:
  - id: restore_harvest_feast
    type: main
    autoStart: false
    title: Restore the Harvest Feast
    summary: Help the townsfolk prepare for the Harvest.
    completionSummary: The Harvest Feast was restored.
    objectives:
      - id: find_bayker
        text: Find Bayker at the bakery.
        location: Old Bakery
        giver: Inspekta
        initiallyActive: true

      - id: gather_wildflowers
        text: Gather wildflowers.
        location: Milldread Woods
        giver: Bayker
        progressTarget: 10
        initiallyActive: false

      - id: return_to_bayker
        text: Return to Bayker.
        location: Old Bakery
        giver: Bayker
        initiallyActive: false
```

`type` is `main` or `side`. `location` and `giver` are optional per objective. `progressTarget` is optional. `initiallyActive` controls objectives available when the quest first starts; later objectives are activated through Journal Actions.

### Note YAML example

```yaml
notes:
  - id: torn_ritual_letter
    title: Torn Ritual Letter
    bodyText: |
      Dear Saul,
      The harvest must proceed...
    imagePath: Journal/Notes/torn_ritual_letter
    displayMode: sideBySide
```

`bodyText` and `imagePath` are each optional, but at least one must be provided. `imagePath` is a Resources path without a file extension. `displayMode` supports the side-by-side text-and-image presentation.

## Save Behavior

The current save data must be expanded and versioned to include JournalState data.

Persist:

- Active and completed quest IDs.
- Quest acquisition and completion order.
- Per-objective active/completed state.
- Per-objective numeric progress.
- Tracked quest ID.
- Discovered note IDs.
- Note discovery order.
- Note read state.
- Used JournalAreaTrigger IDs.

Existing version-1 saves retain current flags and checkpoint data. They load with empty Journal state and do not receive the YAML auto-start quest. A genuinely new game resets JournalState and then starts the single auto-start quest.

## UI Scope

Use placeholder panels, text, and icons for the first implementation. No final paper/notebook visual treatment is required yet.

### Active tab

- List item: title, current objective, status.
- Detail view: title, summary, status, current location, current giver/source, checklist, numeric progress, Track button.

### Completed tab

- Compact list of completed quest titles.
- Selecting a quest opens its full details, including checklist history and completion summary.

### Notes tab

- Notes remain in chronological discovery order.
- Selecting a note opens its full content.
- The full note reader uses Previous and Next on-screen controls.

## Implementation Workflow

1. Add Journal definition models, YAML parsing, Resources loading, and validation.
2. Add JournalState to GameSession and save/load Journal state through SaveManager.
3. Add Journal Actions and JournalAreaTrigger.
4. Add Journal Actions to InteractionOption when the interaction work begins.
5. Add `OpenJournal` (`J`) and `ToggleObjectiveHud` (`H`) input actions to the existing Player map.
6. Add Journal full-screen UI, note reader, Active/Completed/Notes tabs, Track button, and player-control blocking.
7. Add top-right objective HUD, queued toasts, placeholder icons, and shared update sound.
8. Add representative quest/note YAML content and scene triggers.
9. Validate fresh game startup, existing-save migration, scene transitions, trigger persistence, objective progress, tracking fallback, note reading, and missing-definition handling.

## Architecture Planning Plan (Developer-Owned)

Use this checklist to make and document your own architecture decisions before implementation. It intentionally does not prescribe components, APIs, data structures, or ownership boundaries.

### 1. Establish constraints and goals

1. Re-read the Purpose, Player Experience, Quest Rules, Save Behavior, and UI Scope sections.
2. List the non-negotiable player-facing requirements.
3. List existing-project constraints that affect the work, including save data, scene loading, the Input System, dialogue YAML, and Resources.
4. Define the first-release success criteria and the future features that must remain practical to add.

### 2. Map responsibilities

1. List all required responsibilities: content loading, validation, runtime progression, persistence, action execution, scene behavior, input, UI, audio, and feedback.
2. Decide what will own each responsibility and document the reason.
3. Identify the operations other game code must request, such as starting quests, changing progress, discovering notes, tracking quests, and opening the Journal.
4. Decide which code is allowed to request state changes and which code must not mutate Journal state directly.
5. Define how state changes reach UI, HUD, audio, and toast feedback.

### 3. Define authored data

1. List every required and optional field for quests, objectives, notes, images, and actions.
2. Define stable-ID rules and every field that references another ID.
3. Write the exact YAML schema, including defaults and invalid combinations.
4. Define how an authored objective becomes active, completed, visible, and current.
5. Draft representative content before writing parsers: a main quest, side quest, numeric objective, parallel objectives, text-only note, image-only note, and combined note.

### 4. Define runtime state

1. Separate immutable authored definitions from per-save runtime facts.
2. List every fact that must persist for quests, objectives, notes, tracking, ordering, and one-time triggers.
3. Define valid runtime states and transitions for quests, objectives, notes, and triggers.
4. Define invalid transitions and their intended warning/no-op behavior.
5. Define how acquisition/completion/discovery ordering is represented.
6. Define how the primary objective is determined from authored order and runtime state.

### 5. Plan save/load and migration

1. Define the exact serialized fields needed for runtime state.
2. Decide the save-version and migration strategy.
3. Write expected behavior for a new game, existing version-1 save, current save, malformed save, and a save containing removed content.
4. Decide the ordering of Journal load/save relative to checkpoints and scene transitions.
5. Create migration test cases before changing save code.

### 6. Plan loading and validation

1. Define when and how YAML files are discovered.
2. Decide where validation runs: startup, editor, development builds, or a combination.
3. List every validation rule and classify it as an error, warning, or skipped entry.
4. Define diagnostics that include source file, entry ID, and invalid field/reference.
5. Decide what content remains available if some definitions fail validation.
6. Prepare intentionally invalid content for validation tests.

### 7. Plan actions and scene behavior

1. Define the payload and preconditions for every action type.
2. Decide execution order and partial-failure behavior for multi-action triggers.
3. Define idempotency for repeated starts, completions, discoveries, and trigger entries.
4. Define trigger identity and the rule for recording one-time use.
5. Define the boundary between automatic area behavior and player-initiated interaction behavior.
6. Define how interactions will use the same action workflow later without duplicating logic.

### 8. Plan UI and input states

1. Sketch Active, Completed, Notes, quest detail, note reader, HUD, and toast states.
2. Define UI selection behavior and its reset rules.
3. Define every input's behavior during gameplay, Journal, and note-reader states.
4. Define player-control blocking and confirm it does not alter unrelated global time behavior.
5. Define refresh behavior after each Journal state change.
6. Define toast queue behavior and fallbacks for missing UI, image, icon, or audio references.

### 9. Define interfaces and errors

1. Write the public API surface before implementing it.
2. Decide which operations return results and what information callers receive on failure.
3. Define development-build versus release-build diagnostics.
4. Decide how callers handle invalid IDs, duplicate discoveries, inactive objectives, and missing definitions.
5. Confirm callers cannot directly alter internal mutable Journal state.

### 10. Create a test matrix

1. Cover every quest/objective transition and every action type.
2. Cover parallel objectives, numeric progress at zero/target/over-target, direct completion, and automatic quest completion.
3. Cover tracking behavior, note discovery/read/order, trigger reuse, scene reload, save/load, migration, and missing content.
4. Cover Journal open/close, HUD toggle, note navigation, and player-control blocking.
5. Write manual play-test scenarios using representative authored content.

### 11. Review before implementation

1. Confirm every requirement maps to authored data, runtime state, an action, UI behavior, validation, or an explicit out-of-scope decision.
2. Review the proposed architecture against the current codebase and record every integration point that will change.
3. Identify dependencies and decide the implementation order.
4. Confirm save migration behavior before changing persistence code.
5. Walk one representative quest and note from new game through save/load, then convert the reviewed plan into implementation tasks.
## Explicitly Out of Scope for This Version

- Hints.
- Quest failure, abandonment, expiration, or lockout states.
- Player-written notes.
- Quest filters.
- Map pins.
- Automatic/inferred quest starts.
- Notes associated with quests.
- Final Journal art direction.
- Global `Time.timeScale` pausing.

