# Implementation Task Board

Status: Current Plan  
Project: **Skyhook Ascent**

This is the day-to-day execution order. The broader time budget and scope gates
remain in [`roadmap.md`](roadmap.md).

## Working rule

Work on one small playable increment at a time. A task is complete only when:

1. Unity has finished compiling.
2. The Console has no project errors.
3. The changed behavior has been checked in Play Mode.
4. Any deterministic rule has a focused EditMode test.
5. The result and known limitation can be explained in plain language.

Status values:

- **Done** - verified and complete
- **Next** - the next task to implement
- **Planned** - in the committed MVP
- **Stretch** - attempted only after the submission candidate works

## M0 - Foundation

Goal: a reproducible project that is ready for feature work.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M0.1 | Create the outer Git/documentation root and nested Unity project | done | Done | Unity-generated folders and local agent files are ignored |
| M0.2 | Review the assignment PDF and lock the game concept and scope | done | Done | Checklist, game design, architecture, roadmap, and video plan agree |
| M0.3 | Install and connect Unity MCP | done | Done | Codex reads the correct live project and the Console is clean |
| M0.4 | Create the initial source-control baseline | 0.5 h | Done | One coherent initial commit exists before gameplay implementation |

Architecture used here: repository ownership, documentation outside `Assets`,
project assets under `Assets/Game`, pinned versions, and clean source control.

## M1 - Movement Playground

Goal: a small handcrafted scene where basic movement already feels dependable.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M1.1 | Create `Assets/Game` as needed, a gameplay scene, player capsule, camera, light, floor, and three-platform course | 0.75 h | Done | Scene opens directly and contains a readable test route |
| M1.2 | Create a minimal gameplay input map for move, look, run, jump, grapple, and restart | 0.5 h | Done | Keyboard and mouse inputs are visible and named clearly |
| M1.3 | Implement camera-relative walk/run, acceleration, braking, grounded jump, and limited air control | 1.5 h | Done | Player can traverse the course consistently without unstable grounding |
| M1.4 | Implement the third-person follow camera and basic wall avoidance | 0.75 h | Done | Camera does not clip badly during the test route |
| M1.5 | Tune and verify the complete movement course | 0.5 h | Done | Ten consecutive jump attempts behave consistently; Console is clean |

**Milestone exit:** walking, running, jumping, air control, and the camera are
playable before any grappling code exists.

Architecture used here: ordinary MonoBehaviours for input, physics, references,
and presentation. Tuneable values begin as focused serialized fields; a config
asset is introduced only if sharing or repeated tuning makes it useful.

## M2 - Grapple Proof

Goal: one satisfying and repeatable jump-grapple-swing-release-land sequence.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M2.1 | Add a clearly marked grapple-anchor component/layer and one large fixed target | 0.75 h | Done | Only the intended target is considered valid |
| M2.2 | Add crosshair aiming and a visible gravity-affected hook projectile | 1.5 h | Done | The projectile follows an arc and misses cleanly |
| M2.3 | Implement attachment, one active rope, rope visualization, and quick miss recovery | 2.0 h | Next | Valid hits attach; invalid hits return without locking input |
| M2.4 | Implement swing physics and limited tangential input assistance | 1.5 h | Planned | The player can deliberately build useful swing motion |
| M2.5 | Preserve momentum on release and support ground/air firing | 1.0 h | Planned | Releasing at different points produces understandable trajectories |
| M2.6 | Build and tune one mandatory grapple gap | 1.25 h | Planned | The complete sequence succeeds repeatedly with base values |

**Milestone exit:** the central mechanic is fun enough to justify the rest of
the game. If this exceeds its time box, enlarge anchors, simplify rope behavior,
and shorten miss recovery before adding more features.

Architecture used here: separate components only for real ownership
(`GrappleController`, projectile, and anchor). No general ability framework,
factory, service layer, or event bus.

## M3 - Complete Authored Run

Goal: a short non-procedural course with the full start-climb-fail-restart loop.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M3.1 | Extend the handcrafted course with jumps, one recovery platform, and two grapple sections | 0.75 h | Planned | The route demonstrates all core movement |
| M3.2 | Add the rising hazard and player contact/death | 0.75 h | Planned | The hazard applies pressure and ends the run reliably |
| M3.3 | Add run state, height score, session best, seed display, and restart | 1.25 h | Planned | One button cleanly resets player, hazard, score, and course state |
| M3.4 | Add minimal HUD and run-end panel | 0.5 h | Planned | Gameplay and failure state are understandable without explanation |
| M3.5 | Play-test and balance a two-to-five-minute authored run | 0.75 h | Planned | Full loop works repeatedly before procedural work begins |

**Milestone exit:** this is the safe fallback submission version. Procedural
generation must never be allowed to break this working loop.

## M4 - Deterministic Procedural Tower

Goal: earn the procedural-generation bonus with a small system that is
reproducible, testable, and explainable.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M4.1 | Define `TowerChunk` entry, exit, bounds, difficulty, weight, and traversal metadata | 0.5 h | Planned | Metadata is visible and understandable in the Inspector |
| M4.2 | Convert four validated authored sections into chunk prefabs | 1.25 h | Planned | Every chunk is completable with base movement/grapple values |
| M4.3 | Implement plain-C# seeded chunk-selection rules | 1.0 h | Planned | Same seed gives the same sequence; focused EditMode tests pass |
| M4.4 | Align and rotate chunk prefabs from exit to entry | 1.0 h | Planned | A fixed seed builds a continuous ascending route |
| M4.5 | Add overlap rejection, recent-history limits, and diagnostic rejection reasons | 1.0 h | Planned | Bad candidates are rejected visibly without hiding the reason |
| M4.6 | Stream chunks above the player and remove chunks below the hazard | 0.75 h | Planned | A run continues without manual scene changes or unbounded growth |
| M4.7 | Preserve the authored fallback and verify at least three fixed seeds | 0.5 h | Planned | Each chosen seed remains playable and reproducible |

**Milestone exit:** at least four chunks assemble deterministically, required
routes work with base abilities, and generated chunks do not visibly overlap.

Architecture used here: this is where the blueprint's plain-C# rules, fixed
seeds, diagnostics, prefab ownership, and EditMode tests become valuable.
A small chunk-creation owner is acceptable; DI, catalogs, pooling, and a custom
editor dashboard are still unnecessary.

## M5 - Readability and Aesthetics

Goal: replace prototype ambiguity with a coherent tower presentation.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M5.1 | Establish a simple tower material/color palette and structural shell | 1.0 h | Planned | Platforms, walls, hazard, and background are visually distinct |
| M5.2 | Make grapple anchors readable by shape and emission, not color alone | 0.75 h | Planned | Valid anchors are identifiable during motion |
| M5.3 | Improve rope/projectile/hit/release feedback | 0.75 h | Planned | Hook state is understandable without additional HUD text |
| M5.4 | Tune lighting, fog, hazard warning, and restrained particles | 1.0 h | Planned | The route remains readable from the gameplay camera |
| M5.5 | Polish HUD and optional minimal audio if time remains | 0.5 h | Planned | UI is legible; audio is never required for understanding |

No imported asset or audio is added without recording its license/source.

## M6 - Submission Candidate

Goal: freeze features and prove the project can be graded reliably.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M6.1 | Run focused EditMode tests and manual Play Mode checks | 0.75 h | Planned | Rules pass and the complete loop works from a clean open |
| M6.2 | Test camera, input, grapple misses, falling recovery, death, and restart edge cases | 1.0 h | Planned | No known blocker remains hidden |
| M6.3 | Create and run a standalone Windows build | 0.75 h | Planned | Build launches and completes a full run outside the Editor |
| M6.4 | Record honest known limitations and freeze scope | 0.5 h | Planned | `game-design.md` matches the actual build |
| M6.5 | Prepare the 15-25 minute recording and both upload artifacts | 6.0 h | Planned | Gameplay is at most 7 minutes; project ZIP excludes `Library` |

## Stretch backlog

Attempt only after M6.1-M6.4 pass:

1. Crumbling platform
2. Persistent high score
3. Safe/risky branch inside selected chunks
4. Two-choice temporary upgrade
5. Optional collectible

No stretch task may make a required generated route depend on an upgrade.

## Immediate next action

Implement **M2.3 only**. Attach the existing projectile to valid anchors, show
one rope, and make invalid hits recover immediately. Swing forces remain in
M2.4 so attachment can be verified independently.
