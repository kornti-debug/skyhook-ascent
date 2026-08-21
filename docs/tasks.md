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

Goal: one satisfying and repeatable jump-shoot-zip-land sequence.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M2.1 | Add a clearly marked grapple-anchor component/layer and one large fixed target | 0.75 h | Done | Only the intended target is considered valid |
| M2.2 | Add crosshair aiming and a visible gravity-affected hook projectile | 1.5 h | Done | The projectile follows an arc and misses cleanly |
| M2.3 | Implement one active hook, rope visualization, maximum range, and movement-independent miss retrieval | 2.0 h | Done | A moving or falling player cannot interrupt retrieval; a second shot is blocked until it completes |
| M2.4 | Implement automatic collision-aware zip pull with speed, acceleration, and timeout | 1.5 h | Done | A valid hit pulls reliably without teleporting or trapping the player |
| M2.5 | Add automatic arrival release and support ground/air firing | 1.0 h | Done | Reaching an anchor releases predictably over its landing platform |
| M2.6 | Build and tune one mandatory grapple gap | 1.25 h | Done | The complete sequence succeeds repeatedly with base values |

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
| M3.1 | Extend the handcrafted course with jumps, one recovery platform, and two grapple sections | 0.75 h | Done | The route demonstrates all core movement |
| M3.2 | Add the rising hazard and player contact/death | 0.75 h | Done | The hazard applies pressure and ends the run reliably |
| M3.3 | Add run state, height score, session best, seed display, finish, and restart | 1.25 h | Done | One button cleanly resets player, hazard, score, and course state |
| M3.4 | Add minimal HUD and run-end panel | 0.5 h | Done | Gameplay and failure state are understandable without explanation |
| M3.5 | Play-test and balance the authored run | 0.75 h | Done | Full loop works repeatedly before procedural work begins |

**Milestone exit:** this is the safe fallback submission version. Procedural
generation must never be allowed to break this working loop.

## M4 - Deterministic Procedural Tower

Goal: earn the procedural-generation bonus with a small system that is
reproducible, testable, and explainable.

| ID | Task | Estimate | Status | Verification |
| --- | --- | ---: | --- | --- |
| M4.1 | Define `TowerChunk` entry, exit, bounds, difficulty, weight, and traversal metadata | 0.5 h | Done | Metadata is visible and understandable in the Inspector |
| M4.2 | Convert four validated sections and create eight derived variants | 1.25 h | Done | Twelve prefab shapes provide jump, zip, mixed, precision, mirrored, and recovery rhythms |
| M4.3 | Implement plain-C# seeded chunk-selection and pacing rules | 1.0 h | Done | Five focused EditMode tests pass; a category may repeat twice but not three times when alternatives exist |
| M4.4 | Align and rotate chunk prefabs from exit to entry | 1.0 h | Done | Seed `104729` builds a connected 24-chunk route ending near 97 m |
| M4.5 | Add overlap, reverse-turn, protected-corridor, adjacent-approach, history, and fallback validation | 1.0 h | Done | Moved colliders synchronize before checks; seed `81189325` rejects the blocking zigzag/recovery pair and streams ten valid stages |
| M4.6 | Add landing-based finish, generated-run restart, and longer hazard progression | 0.75 h | Done | The finite regression goal works; `R` resets the player and creates a fresh tower |
| M4.7 | Stress-test random generated runs and replacement-course cleanup | 0.5 h | Done | Repeated runs keep one valid 24-chunk course, one matching seed, and no fallback flashes |
| M4.8 | Convert generation to a bounded endless window that appends ahead of the player | 2.0 h | Done | Ten-stage and ten-run stress checks appended valid stages without interrupting the active tower |
| M4.9 | Remove generated chunks only after they are safely below the flood | 0.75 h | Done | Cleanup removes each submerged chunk and deletes a stage root once it becomes empty |
| M4.10 | Add a round transition landing and rotating stage material palettes | 2.0 h | Done | The landing has a flat mesh collider, a validated 1.25 m upward jump, and cannot obstruct jump/headroom above any traversal platform or any grapple corridor |
| M4.11 | Increase flood speed by generated stage and remove the normal finite finish | 1.0 h | Done | Generated runs end through flood contact; flood starts at 0.65 m/s and scales visibly with each stage |
| M4.12 | Add editor-only debug flight for streaming inspection | 0.5 h | Done | F3 toggles collision-free flight with vertical movement and a speed boost; player builds remain unaffected |
| M4.13 | Replace the low stage-boundary jump with a mandatory zip transition | 0.75 h | Done | Eight appended stress stages produced eight raised, anchored transitions and 216 valid chunks; the centered target remains inside base grapple range |

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
| M5.1 | Establish a simple tower material/color palette and structural shell | 1.0 h | Done | Three textured stage palettes cycle with matching collider-free wall shells; the hazard and background remain visually distinct |
| M5.2 | Make grapple anchors readable by shape and emission, not color alone | 0.75 h | Done | Every anchor has a camera-facing emissive diamond; in-range anchors pulse while the original hit collider remains unchanged |
| M5.3 | Improve rope/projectile/hit/release feedback | 0.75 h | Done | Outbound, retrieval, pull, hit, and release have distinct color, width, pulse, trail, and anchor-flash cues without HUD text |
| M5.4 | Tune lighting, fog, hazard warning, and restrained particles | 1.0 h | Done | Lighting/fog preserve route contrast; a pulsing edge frame and numeric clearance warn inside six metres without particles |
| M5.5 | Polish HUD and optional minimal audio if time remains | 0.5 h | Planned | UI is legible; audio is never required for understanding |

No imported asset or audio is added without recording its license/source.

The completed visual-foundation pass uses project-owned procedural textures and
URP materials for masonry, ice, and overgrown-wood stages. Each generated stage
also receives a matching 16-panel, 20-metre-radius collider-free tower shell; the
shell is recycled with its stage. The start floor, deep-blue background,
restrained ambient light, and distance fog keep the route readable without
third-party assets. Flood proximity now adds a non-color-only edge and distance
warning only inside its six-metre danger zone; particles remain intentionally
omitted because the warning is already clear.
The flood now matches the circular shell and uses a lightweight animated URP
water shader. Consecutive stage shells meet at measured non-overlapping seams,
removing the material flicker seen during biome transitions.
The start floor now uses the same centered circular footprint while preserving
its original surface and player spawn heights.

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
4. Two-choice temporary upgrade/roguelike choice
5. Optional collectible

No stretch task may make a required generated route depend on an upgrade.

## Immediate next action

Play one normal stage and confirm that the flood warning appears late enough to
preserve a clear view but early enough to prompt an escape. Then complete
**M5.5** with a focused HUD readability pass; skip audio unless the otherwise
finished build clearly needs it. Keep the current capsule player and defer
imported character, animation, footsteps, and a menu until submission checks
are complete.
