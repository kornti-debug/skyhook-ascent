# Final Video and Submission Guide

Status: Ready to rehearse and record

Target length: **19-21 minutes**

Hard limits from `EbCRD.pdf`: **15-25 minutes total** and **no more than 7
minutes of gameplay**.

The video is the main evidence that the project is understood. Explain systems
in your own words and show how they cooperate; do not read every line of code.

## Submission requirements

### Upload 1 - video

- Record the screen and make sure the voice is clearly understandable.
- Record and play back a short audio/screen test immediately before the final take.
- Keep the complete video between 15 and 25 minutes.
- Show gameplay and mechanics first, for no more than 7 minutes.
- Use the second part to explain the important Unity setup and C# implementation.
- Upload the video, a ZIP containing it, or a `.txt` file containing a stable
  viewing/download link. Do not use a temporary transfer link.
- If using a link, test it in a private/incognito browser window.

### Upload 2 - Unity project

- Upload a project ZIP, or a `.txt` file containing a stable project-ZIP link.
- Close Unity before packaging.
- Exclude `Library` as explicitly required by the brief.
- Also exclude `Temp`, `Logs`, `obj`, `Builds`, `.vs`, local captures, and other
  generated files. The portable project needs `Assets`, `Packages`, and
  `ProjectSettings`.
- Download the final upload/link once and confirm the project opens.

The PDF names July 6 and September 21, but its metadata is from 2025. Confirm the
applicable year and exact deadline in the current eCampus course.

## Recording timeline

| Time | Segment | Screen | Main point |
| --- | --- | --- | --- |
| 00:00-00:30 | Introduction | Standalone game | Name, pitch, objective, controls |
| 00:30-06:00 | Gameplay | Standalone game | Full mechanic loop, under the 7-minute limit |
| 06:00-07:30 | Unity structure | Unity hierarchy and Project window | Clear ownership and project organization |
| 07:30-09:15 | Input, movement, camera | Input Actions, Player Inspector, `PlayerController` | Responsive Rigidbody movement and third-person control |
| 09:15-13:15 | Grapple implementation | Player Inspector, projectile prefab, grapple scripts | Ballistic skill shot, state flow, return, zip, release |
| 13:15-17:15 | Procedural tower | Chunk prefab, generator and rule scripts | Deterministic selection, placement validation, streaming |
| 17:15-18:30 | Run loop and presentation | Run/hazard scripts and materials | Flood pressure, restart, HUD and visual feedback |
| 18:30-19:30 | Tests, limitations, summary | Test Runner and final game view | Evidence, honest scope, result |
| **Total** |  |  | **19:30** |

## Part 1 - gameplay script (00:00-06:00)

### 00:00-00:30 - introduction

Suggested wording:

> This is Skyhook Ascent, an endless third-person vertical platformer. The goal
> is to climb a procedurally generated tower before the rising flood catches the
> player. The main mechanic is a gravity-affected zip grapple: it has no target
> snapping, so I have to aim above an anchor, hit it with the real projectile,
> and then the player is pulled to it.

Briefly name the controls: `WASD`, `Left Shift`, `Space`, mouse aim, left mouse
button to grapple, and `R` for a new tower.

### 00:30-06:00 - demonstration order

1. Point out the objective card and the HUD values: current/best height, stage,
   flood speed, seed, and restart key.
2. Demonstrate walking, running, one normal jump, air steering, and the camera.
3. Intentionally miss an anchor. Show that the hook reaches a surface/range,
   returns to the moving player, and blocks a second shot until recovered.
4. Hit an anchor by aiming above it. Show the ballistic arc, zip pull, automatic
   release, and landing.
5. Mention that a distant anchor can be attempted as a risky time-saving skip.
6. Reach one raised zip stage transition if the run permits. Point out the new
   material theme and that the displayed flood speed increases with progress.
7. Let the flood catch the player or deliberately fall back into it. Show the
   result panel.
8. Press `R`. Point out the reset position, new seed, new layout, reset flood,
   and retained session best.

Do not spend time climbing as high as possible. The purpose is to demonstrate
the mechanic loop, not player skill.

## Part 2 - Unity and code explanation

### 06:00-07:30 - scene and project structure

Open `Assets/Game` and the `Gameplay` hierarchy.

Explain the seven root objects:

- `Main Camera` owns orbit/follow behavior and the crosshair.
- `Environment` owns the start floor, rising flood, and finite fallback goal.
- `Course` is a handcrafted safety/fallback course.
- `Player` owns Rigidbody movement, grapple coordination, and editor-only flight.
- `GameLoop` owns run state, scoring, death, restart, and HUD.
- `ProceduralTower` owns generation and the generated hierarchy.
- `Directional Light` provides the main scene lighting.

Then show these folders:

- `Prefabs/TowerChunks`: reusable authored pieces with entry/exit metadata.
- `Scripts/Gameplay`: small components with specific ownership.
- `Tests/EditMode`: tests for deterministic rules without requiring Play Mode.
- `Materials`, `Textures`, and `Shaders`: project-owned presentation assets.

Useful explanation:

> I kept the architecture assignment-sized. MonoBehaviours own Unity input,
> physics, scene references, and presentation. Deterministic decisions such as
> chunk selection and streaming conditions are plain C# rules, which makes them
> easier to explain and test.

### 07:30-09:15 - input, movement, and camera

Show `GameplayInput.inputactions`, the `Player` Inspector, then these parts of
[`PlayerController.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/PlayerController.cs):

- Lines 77-112: Input System actions are read in `Update`.
- Lines 114-124: physics work runs in `FixedUpdate`.
- Lines 166-193: camera-relative target velocity and separate ground/air
  acceleration.
- Lines 195-233: buffered jump plus stronger upward/falling gravity.
- Lines 252-276: ground detection from collision-normal angle.

Explain it like this:

> Input is sampled every rendered frame, while Rigidbody velocity changes happen
> in the fixed physics step. Movement converts input through the camera's flat
> forward/right axes. `MoveTowards` gives acceleration and stronger braking
> instead of instantly setting speed. The jump has a short input buffer and
> coyote time. The jump velocity comes from the target jump height, and stronger
> gravity while rising and falling makes it feel fast rather than floaty.

For the camera, show the `Main Camera` Inspector and only briefly mention
[`ThirdPersonCameraController.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/ThirdPersonCameraController.cs):

- Lines 96-114: free yaw/pitch aiming.
- Lines 135-165: camera position pitch smoothly separates from aim pitch when
  aiming steeply upward.
- Lines 167-196: a sphere cast shortens camera distance near walls.

### 09:15-13:15 - grapple, the main feature

Show the grapple values on the `Player`, then the `HookProjectile` prefab.

#### 1. Firing and aiming

Open [`GrappleController.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/GrappleController.cs),
lines 152-185.

> A ray through the center crosshair defines a direction at a fixed zeroing
> distance, but it does not raycast for a target and does not correct the shot.
> The projectile is spawned near the player and receives one launch velocity.
> Unity gravity then creates the arc, so the player must learn the correct aim.

Point out `CanFire`: only one projectile can exist at a time.

#### 2. Projectile state machine

Open [`GrappleProjectile.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/GrappleProjectile.cs):

- Lines 8-15: `Idle`, `Outbound`, `Returning`, `Attached`, `Finished`.
- Lines 99-110: travelled distance/range check.
- Lines 139-156: anchor collision attaches; other collision returns.
- Lines 158-199: launch velocity, continuous collision, and ignored player
  colliders.
- Lines 214-230: attachment disables projectile physics and notifies the owner.
- Lines 232-294: return state smoothly travels back to the player's current hook
  origin, even while the player moves.

Explain why the states matter:

> The state machine prevents conflicting behavior. Outbound uses normal physics;
> returning and attached are kinematic. A miss does not instantly reset the
> weapon, which creates tension and makes the single-hook rule visible.

#### 3. Zip pull and release

Return to `GrappleController.cs`:

- Lines 106-131: accelerate the player's velocity toward the anchor.
- Lines 208-222: valid hit disables gravity and normal movement during the pull.
- Lines 239-268: arrival or timeout restores gravity and completes the hook.
- Lines 133-149 and 323-348: LineRenderer rope and state-specific feedback.

> The controller coordinates player movement; the projectile only owns its own
> flight/collision states. During a zip, regular movement is suspended, gravity
> is disabled, and velocity approaches the pull velocity. Arrival distance and a
> timeout guarantee that the state ends instead of trapping the player.

### 13:15-17:15 - procedural generation and bonus feature

Show one chunk prefab and its `TowerChunk` component. Explain its metadata:

- stable ID, difficulty, weight, and traversal category;
- entry/exit transforms used for connection;
- bounds used for physical overlap;
- protected traversal/headroom corridors used to prevent playable-path blocking.

Then show the `ProceduralTower` Inspector and
[`TowerGenerator.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/TowerGenerator.cs):

1. Lines 27-54: seed, 24 chunks per stage, streaming distance, shell, radius,
   turn limit, and attempt limits are Inspector-controlled data.
2. Lines 217-283: a restart searches for a valid new runtime seed and keeps the
   current playable course if replacement generation fails.
3. Lines 349-461: each stage uses `System.Random(stageSeed)`, places a mandatory
   start/transition chunk, then builds a sequence from reusable candidates.
4. Lines 532-583: candidate selection is separated from physical placement.
5. Lines 585-700: entry-to-exit alignment plus rotation, transition height,
   turn, clearance, tower-radius, and overlap validation.
6. Lines 134-154 and 775-831: append ahead of the player and remove chunks only
   after the flood has made them unreachable.

Open [`ChunkSelectionRules.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/ChunkSelectionRules.cs),
lines 28-99:

> Candidates above the current difficulty are filtered out. Recent IDs are
> avoided, and after two chunks of one traversal category the algorithm prefers
> another category. A weighted random roll selects among valid candidates. If a
> strict filter leaves no option, constraints are relaxed in a controlled order
> so generation can continue.

Open [`ChunkPlacementRules.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/ChunkPlacementRules.cs),
lines 24-62, and explain:

> Direction checks reject near-reverse turns. Clearance corridors are sampled
> with a radius against candidate bounds. This was added because non-overlapping
> platforms could still block the jump or ballistic path needed to reach the
> next anchor.

Open [`TowerStreamingRules.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/TowerStreamingRules.cs):

> These plain functions decide when to append, when flooded content is safe to
> remove, and how a stage seed is deterministically derived from the run seed,
> stage index, and retry number.

State the procedural-generation claim carefully:

> The content pieces are authored prefabs, while their selection, orientation,
> connection, validation, stage themes, streaming, and seed sequence are decided
> at runtime by code. The displayed run seed makes a layout reproducible.

### 17:15-18:30 - run loop, flood, HUD, and aesthetics

Open [`RunController.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/RunController.cs):

- Lines 125-161: track highest run height, report stage progress, calculate
  flood clearance, and end the run on contact.
- Lines 171-232: cancel/reset grapple and player state, generate a replacement
  course transactionally, teleport to the start, reset physics, and restart the
  hazard.
- Lines 260-525: HUD, intro, result panel, and escalating visual flood warning.

Open [`RisingHazard.cs`](../final_assignment/Assets/Game/Scripts/Gameplay/RisingHazard.cs),
lines 36-49 and 82-100:

> The flood begins at 0.65 metres per second. Its speed increases from the
> highest reached stage and progress inside that stage, with a cap. The speed
> never becomes easier by falling down, because only highest progress is stored.

Briefly show the three stage materials, collider-free shell, and animated water
shader. Explain that audio was intentionally omitted because it is optional and
the required states already have visual feedback.

### 18:30-19:30 - tests, limitations, and conclusion

Open the Unity Test Runner. All **14 EditMode tests** passed on the final project.
Show [`ChunkSelectionRulesTests.cs`](../final_assignment/Assets/Game/Tests/EditMode/ChunkSelectionRulesTests.cs)
and name representative checks:

- same seed gives the same sequence; different seeds vary;
- difficulty and repetition rules;
- reverse-turn rejection;
- blocked grapple corridor and headroom rejection;
- append/recycle rules and deterministic stage seeds;
- flood-warning strength.

Be honest about limitations:

- validation samples intended traversal corridors but does not simulate every
  possible ballistic shot;
- submerged chunks are destroyed rather than pooled;
- best height is session-only;
- Windows keyboard/mouse is the tested submission target;
- no menu, audio, animated character, or upgrade system was added because the
  assignment favors a small, explainable game.

Suggested closing:

> The final result is one complete and tested loop: move and jump, make a
> ballistic grapple shot, climb a deterministic but newly seeded endless tower,
> escape an accelerating flood, fail, and restart into a new run. The main thing
> I learned was how physics components, scene-owned controllers, prefabs, and
> plain testable generation rules can be kept separate while still working as
> one game.

## Tabs and windows to prepare before recording

Open these in this order so there is no searching during the take:

1. Standalone `SkyhookAscent.exe`
2. Unity `Gameplay` scene and Game view
3. Project window at `Assets/Game`
4. `GameplayInput.inputactions`
5. `PlayerController.cs`
6. `GrappleController.cs`
7. `GrappleProjectile.cs`
8. `TowerGenerator.cs`
9. `ChunkSelectionRules.cs`
10. `ChunkPlacementRules.cs`
11. `TowerStreamingRules.cs`
12. `RunController.cs`
13. `RisingHazard.cs`
14. `ChunkSelectionRulesTests.cs`
15. Unity Test Runner

Increase the code/editor font size enough for a 1080p recording. Collapse the
Inspector components that are not being discussed.

## Final pre-recording checklist

- [ ] Confirm the correct deadline and upload slots in eCampus
- [ ] Choose one calm demo run; rehearse the eight gameplay actions once
- [ ] Prepare the tabs/windows listed above
- [ ] Increase Unity and code font size
- [ ] Close unrelated windows and hide personal information
- [ ] Disable notifications
- [ ] Record a 20-30 second test with screen and voice
- [ ] Play the test file back and confirm voice clarity and readable code
- [ ] Keep a clock visible off-screen and end gameplay by 06:00
- [ ] Record the final video in one take if convenient; corrections are allowed
- [ ] Check final duration and the first-part duration

## Explanation pattern

For each important system, answer these five questions:

1. What player/design problem does it solve?
2. Which GameObject and script own it?
3. What data is configured in the Inspector?
4. What state or algorithm changes at runtime?
5. How did you verify it, and what is its honest limitation?
