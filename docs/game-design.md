# Game Design

Status: Current Reference  
Working title: **Skyhook Ascent**

## One-sentence pitch

An endless 3D vertical platformer where the player outruns an accelerating flood by jumping through a seeded procedural tower and landing gravity-affected hook shots that zip them upward for a high score.

## Player experience

The player should understand "go up before the water reaches you" within 15 seconds. Running, jumping, landing a ballistic hook shot, and being pulled to the next platform should feel responsive enough that failure feels caused by a readable decision or execution mistake.

A typical run should last several minutes and end when the accelerating flood catches the player. Skilled players climb farther by moving cleanly and taking difficult grapple shortcuts.

## Design pillars

1. **Fast vertical flow:** movement, a skillful hook shot, and the automatic zip form one continuous climb.
2. **Readable risk:** the player can see anchors, platforms, route choices, and the rising hazard.
3. **Meaningful short choices:** safe jump routes cost time; difficult grapple routes gain height quickly.
4. **Controlled randomness:** runs vary, but every required route is valid with base abilities.

## Environment and route structure

The level is the interior of a hollow cylindrical tower:

- Platforms attach to the inner wall or project into the central shaft.
- Grapple anchors sit in clear space above their intended landing platforms.
  One anchor in the mirrored risk fork now oscillates a short distance while
  remaining inside its own landing footprint.
- The third safe-route landing (`SafeJump_03`) moves in both fork variants. It
  slides along chunk-local X, separate from the risk route's moving grapple
  target; chunk entry, exit, and zip landings stay fixed.
- Chunks rotate around the vertical axis, creating an upward spiral without requiring one continuous staircase.
- The central shaft provides space for readable projectile arcs, fast zip lines, and dramatic falls.
- The camera follows the player inside the tower rather than showing the entire structure.

Every chunk has one entry and one exit so the procedural generator can connect
chunks as single building blocks. Most chunks keep one primary route. The
post-submission working build adds one authored route-choice chunk whose two
internal routes rejoin at the same exit:

- **Safe route:** more platforms and easier jumps, but slower.
- **Risk route:** fewer platforms and a demanding ballistic grapple challenge,
  but faster.

The safe lane uses eight ordinary platform landings and takes longer. The risk
lane uses two skill-shot anchors, each centered above its own landing platform,
then one final jump to the shared exit. It is deliberately a larger chunk than
the library's short jump and grapple chunks, but its curved layout stays within
the tower's placement limits. Keep both sizes in the pool; do not expand every
chunk just to add variety. Mandatory grapple chunks remain useful so the core
mechanic cannot be ignored.

## Core run loop

1. Start a run with a displayed seed.
2. Climb through generated tower chunks.
3. Use jumps and grapple choices, including an optional safe-versus-fast route
   in the authored route-choice chunk.
4. The hazard rises and gradually accelerates.
5. Generate more chunks ahead as the player climbs.
6. Recycle chunks that are safely below the flood.
7. Contact with the hazard ends the run.
8. Show maximum height; `R` returns the player to the ground and starts a fresh
   tower with a new displayed seed.

## Player mechanics

### Movement

- Camera-relative walking and running
- Ground acceleration and braking
- Limited air control
- Fixed-height grounded jump with a deliberately fast rise and fall
- Momentum retained when leaving platforms
- Close third-person orbit camera with basic wall avoidance
- **Current local experiment:** the player can pass upward through a chunk
  platform from below, then land on its top while falling. A prototype route
  fork adds a trampoline pad beside the ordinary jump route; its bounce can
  carry the player through an overhead platform. Grapple anchors and side
  contacts stay solid. Keep the collision and bounce changes only if a route
  play-test shows that jumps and landings still feel clear.

The first prototype may use a capsule and primitives. Character animation is not required for the MVP.

### Grappling hook

- Fired as a visible projectile from the player/camera aim direction
- Projectile is affected by gravity, so distant anchors require aiming above them
- No target snapping or ballistic compensation; objects under the crosshair do not alter the launch
- Only objects on the grapple-anchor layer can be attached
- The hook has a fixed maximum travel range
- Every anchor has a camera-facing emissive diamond around its original core.
  Anchors inside the base 22-metre straight-line range brighten and pulse;
  distant anchors remain smaller and subdued. This is a range/readability cue,
  not a promise that the ballistic arc is clear and not an aiming aid.
- A missed hook visibly returns to the player's current position before another shot is available, even while the player moves or falls
- There is exactly one active hook: firing is blocked while it is flying, returning, or pulling
- A valid hit automatically pulls the player toward the anchor at a capped speed
- The hook releases automatically near the anchor, allowing the player to fall onto the platform below it
- The rope and projectile communicate their state without extra HUD text:
  orange while outbound, narrow magenta/red while a miss is being retrieved,
  and thicker pulsing cyan while attached and pulling. A valid hit briefly
  expands/flashes the anchor, and release produces a second white flash.
- The rope shortens naturally as the distance closes; it is not a simulated pendulum
- Grapple can be fired from the ground or in the air
- In `Chunk_RouteFork_Mirrored`, the first risky-route anchor moves ±0.4 m
  along its landing's local X axis on a 3.4-second cycle. This changes target
  timing only; the shot remains ballistic and receives no aim assistance.

Not included in the MVP:

- Attaching to arbitrary surfaces
- Reeling the rope in and out
- Climbing the rope
- Swinging or pendulum physics
- Manual rope-length control
- Multiple simultaneous hooks

### Failure and recovery

- Falling to a lower platform is allowed and can produce a recovery.
- The rising hazard prevents unlimited retries below the current height.
- The flood is a circular surface fitted just inside the 20-metre-radius tower
  shell. A lightweight project-owned URP shader adds animated wave displacement,
  moving highlights, depth color, and restrained transparency without changing
  the gameplay collision height.
- The masonry starting platform shares the flood and tower shell's circular
  20-metre footprint. Its surface remains at world height zero so the player
  spawn and first movement beat are unchanged.
- Touching the hazard ends the run.
- There are no enemies in the MVP.

## Procedural generation

The generator assembles handcrafted tower-chunk prefabs rather than placing arbitrary individual platforms.

Each current chunk provides:

- Entry transform
- Exit transform
- Bounds for overlap checks
- Difficulty
- Selection weight
- Traversal category
- Traversal and clearance information used by placement validation

Future route-choice chunks can keep their safe and risky paths inside the
authored prefab and share its entry/exit. Additional generator metadata should
be added only if specific branch-selection rules require it.

Generation rules:

1. Choose a chunk compatible with the current difficulty and recent history.
   The exact prefab cannot repeat inside the recent-history window; traversal
   categories may repeat twice but not three times while alternatives exist.
2. Align its entry with the previous chunk's exit.
3. Rotate it around the tower axis.
4. Reject overlapping placements.
5. Reject a candidate whose incoming direction turns more than 100 degrees
   against the previous chunk's outgoing direction.
6. Protect each accepted jump/grapple traversal corridor and reject future
   solid colliders that enter it, even when the chunk bounds do not overlap.
7. Synchronize moved colliders before validation and protect the final landing
   approach of the immediately previous chunk from raised platform walls.
8. Build an initial playable window when the run starts, then append validated
   chunks before the player approaches the current top.
9. Keep approximately 100 metres of generated route visible above the player.
10. Remove chunks only after they are below the flood and can no longer be
   recovered onto.
11. Use one numeric run seed for reproducibility and diagnostics.
12. If initial generation exhausts its attempts, report the failure and let `R`
    retry with new seeds. A failed replacement attempt during an active run keeps
    the current valid tower intact.
13. Include moving elements' full sweeps in chunk bounds and validate them
    against sibling colliders and protected traversal clearances. Keep moving
    anchors within their assigned landing footprints and moving platforms clear
    of neighboring routes.

The current implementation starts with one validated 24-chunk stage and streams
another complete stage before the player approaches the generated top. Each
stage is built transactionally from the displayed run seed: an invalid candidate
is discarded without disturbing the playable tower. Individual chunks are
recycled once their highest point is safely below the flood; empty stage roots
are then removed.

### Height stages

- Each generated section begins a new visual stage after roughly 80-100 metres
  of climbing; the exact height varies with the selected chunks.
- A slightly oversized round landing platform clearly separates stages and
  provides a short pacing reset without spanning or blocking the tower shaft.
  It is a mandatory zip transition: the landing top is approximately 4.9
  metres above and 8 metres forward from the previous exit, with a centered
  anchor approximately 8.4 metres above the entry. This keeps the broad landing
  safely above earlier jump surfaces while remaining inside the base grapple
  range. Its placement must preserve the projectile corridor and player-sized
  headroom around the previous route.
- Platforms cycle through masonry, ice, and overgrown-wood material palettes.
- Each stage creates a matching 16-panel tower shell at a 20-metre radius. The
  shell has no colliders or shadows, so it communicates the cylindrical tower
  without changing traversal, blocking grapple shots, or adding unnecessary
  physics work. Adjacent shells are trimmed to a shared seam with a small gap,
  preventing different stage materials from overlapping and flickering. Each
  shell is removed together with its submerged stage.
- The presentation uses project-owned procedural tile textures against a
  dark-blue background. Cool directional light, tri-light ambient color, and
  subtle exponential fog separate the route from distant geometry without
  imported art assets or third-party licensing requirements.
- The flood speed increases at each stage boundary and may also rise smoothly
  within a stage. Prototype tuning starts at 0.65 m/s, adds 0.20 m/s per stage
  plus up to 0.10 m/s within the current stage, and caps the total additional
  speed at 2.0 m/s.
- When the flood comes within six metres of its contact boundary, a pulsing
  screen-edge frame and a numeric `FLOOD CLOSE` clearance warning appear. The
  frame grows stronger and changes from cyan toward orange/red inside the
  final 2.5 metres, while the normal view remains clear at safe distances.
- Stage geometry and materials must not change the reachability rules.

Every chunk must be tested with base movement and grapple values. Upgrades may make routes easier or unlock optional shortcuts, but required progression never depends on an upgrade.

At each biome transition, landing on the round zip platform pauses gameplay and
requires the player to choose one of three run-long perks. The starter choices
are **Quick Recall** (+35% missed-hook return speed per pick), **Climber's Pace**
(+10% ground walk/run speed per pick), and **Light Feet** (+10% jump height per
pick). Perks stack and reset when a new tower starts. Climber's Pace does not
raise the in-air movement target or air acceleration; Light Feet changes jump
height but not air steering. Every generated route must remain completable with
base stats, while stacked perks may make optional shortcuts possible.

The first choice menu is fixed rather than randomized so its three effects can
be tested cleanly. Longer grapple range and an aiming trajectory preview for
moving anchors are later candidates, not part of this slice.

### Initial chunk set

1. Start/warm-up platform
2. Basic jump sequence
3. Mandatory grapple across the shaft
4. Jump sections with optional long-range grapple shortcuts
5. Recovery/rest section
6. Raised round zip-transition landing between generated stages

Post-submission working build: `Chunk_RouteFork` and its mirrored
`Chunk_RouteFork_Mirrored` variant are in the generated chunk pool. The
baseline version has been play-tested and reported to work well; the mirrored
variant has also been tested in Play Mode and reported to work. Both offer an
eight-platform safe route and a faster route with two grapple zips; each anchor
starts over its landing, and both routes jump to one shared exit. The
mirrored copy puts the risky lane on the opposite side while preserving the
same entry and exit. The pair's derived bounds and entry-direction metadata
have been recaptured from their current geometry. These larger variants remain
alongside the short chunks so pacing can alternate between compact and extended
layouts. In fixed-seed generation checks, seeds `10001`, `10002`, `10004`, and
`10006` built full 24-chunk stages that included the mirrored fork. Seed
`10003` was rejected at chunk 10 with the expanded pool, while the original
pool accepted it; a randomized `GenerateNextTower` check succeeded. All 21
EditMode tests now pass. Manual traversal of both variants works; systematic
comparison of traversal time, readability, and balance remains open. The
first risky anchor in the mirrored variant now moves ±0.4 m over its landing;
the second anchor and the baseline fork remain stationary. Both routes have
been manually tested without blocking traversal. The generator includes the
mover's swept bounds and clearance paths in placement validation.
Both fork variants now also move `SafeJump_03`, the third middle platform on the
safe route, ±0.55 m along chunk-local X on a 4.2-second smooth cycle. Each
platform is a kinematic Rigidbody. Its swept bounds are included in chunk
placement validation, and the sweep must stay clear of sibling colliders and
neighboring traversal corridors. The mirrored fork's moving grapple anchor is
on its separate risk route. The player is carried while grounded and inherits
the platform's full horizontal velocity on jump; this keeps motion continuous
at takeoff instead of forcing a world-vertical launch.
These post-submission additions are not part of the submitted build. A copied
straight-looking alternative is parked in
`final_assignment/Assets/Game/Prefabs/TowerChunks/Ideas/` and is not in the
generator pool until its footprint and placement metadata are refit. The
current cylindrical envelope stays unchanged; widening the tower by stage is
deferred. The moving-anchor implementation still needs hands-on testing for
aim timing, misses, zip arrival, and interaction with nearby chunks; details
and staging are in
[`post-submission-roadmap.md`](post-submission-roadmap.md).

### Reusable chunk library

The final scene contains no prebuilt course. Runtime generation uses a fixed
start prefab, a raised anchored stage-transition prefab, and reusable jump,
grapple, mixed, mirrored, precision, quick-turn, and recovery variants. Every
chunk stores `Entry` and `Exit` transforms plus selection and traversal metadata.
Seed `104729` remains a useful deterministic regression seed. Normal play uses a
fresh run seed and continues beyond the initial 24-chunk stage.

## Difficulty progression

Difficulty can increase through:

- Faster hazard rise
- Narrower landing platforms
- Larger but still validated gaps
- More frequent mandatory grapple chunks
- More difficult safe/risk choices
- Later introduction of crumbling platforms

Do not change all variables simultaneously. Height bands should use explicit, testable configurations.

## Scoring and UI

Required HUD:

- Current height
- Best height for the session
- Hazard proximity or clear visual warning
- Grapple readiness/return state if it is not obvious from animation
- Current flood speed
- Current stage, reproducible run seed, and the `R` restart shortcut

The first run opens with an eight-second, automatically fading objective card;
restarted runs show it for five seconds:
`CLIMB. ESCAPE THE FLOOD.` It explains that the player must reach higher
platforms before the water catches them and shows the movement, jump, aim, and
grapple controls. It does not pause or delay the run.

Required run-end UI:

- Maximum height
- Seed
- Restart

The final HUD uses one compact top-left information card, a temporary centered
objective card, the proximity warning, and a centered run-end panel. Audio and
a separate menu remain optional scope cuts.

Generated play is endless and has no finish state. Flood contact ends the run.
`R` cancels any active grapple, creates a fresh seeded tower, and returns the
player, hazard, score, and grapple state to the start. The camera follows the
reset player while preserving the player's current aim orientation.

In the Unity Editor only, `T` toggles collision-free debug flight for streaming
and geometry inspection. Use `WASD` to move, `Space`/`E` to rise,
`Left Ctrl`/`Q` to descend, and `Left Shift` to boost. The component is inert in
player builds and is not part of the normal game rules.

Persistent high scores are optional.

## MVP acceptance criteria

- Player can walk, run, jump, and control direction in the air.
- Grapple projectile visibly arcs and attaches only to valid anchors.
- A valid hit automatically zips the player to the anchor and releases near it.
- A miss reaches its range or an invalid surface, returns visibly, and only then restores grapple readiness.
- The generated tower supports a complete start-climb-fail-restart loop.
- Generated play streams additional stages before the player reaches the top.
- Rising hazard reliably ends the run.
- At least four chunk prefabs assemble from a fixed seed.
- Repeating a seed produces the same chunk sequence.
- Every required chunk route is completable using base abilities.
- Generated chunks do not visibly overlap.
- Submerged stages are removed only after the flood makes recovery impossible.
- `R` always returns the player to the start with a new displayed seed and tower.
- Project compiles without project errors.

## Assignment-era stretch features

This list records the submission scope decisions. The active continuation
priorities are in [`post-submission-roadmap.md`](post-submission-roadmap.md).

In priority order:

1. Crumbling platforms
2. Persistent high score
3. Two-choice temporary upgrades at safe milestones
4. Optional collectibles on risky routes
5. Additional visual tower theme
6. Additional chunk variants

Possible upgrades must not be required by generation:

- Longer grapple range
- Faster hook recovery
- Faster zip pull
- Better air control
- Slightly higher jump
- One recovery from a fatal fall
- Temporary hazard slowdown

## Assignment-era scope cuts

These cuts describe the submitted university build. Post-submission work may
revisit small run-only features when they improve the core climbing loop; the
active continuation scope is tracked in [`post-submission-roadmap.md`](post-submission-roadmap.md).

Cut in this order:

1. Upgrade system
2. Collectibles
3. Persistent high score
4. Crumbling platforms
5. Additional chunk variants
6. Branching inside chunks

Never cut the responsive base movement, grapple, rising hazard, deterministic chunk assembly, run-end screen, or restart loop.

## Accessibility and comfort

- Grapple anchors differ by shape/emission as well as color.
- The diamond frame and pulse make nearby valid anchors readable without
  changing the crosshair or snapping the shot.
- Camera motion should not automatically roll with the player.
- Camera shake must remain subtle and optional if implemented.
- Hazard warnings must be visible without relying on audio.

## Known limitations and residual risks

- Generation uses structural overlap, headroom, and sampled grapple-corridor
  validation rather than simulating every possible ballistic shot. Stress tests
  and manual traversal passed, but a rare awkward route may still be possible;
  the displayed seed makes it reproducible.
- Distant visible anchors can sometimes be hit to skip intermediate platforms.
  This is accepted as an intentional high-risk, time-saving skill shot.
- The moving anchor is currently limited to one target in the mirrored fork.
  Its generator footprint and clearance envelope are tested, and the player
  confirmed the motion makes aiming slightly harder. Miss retrieval, zip
  landing, and nearby-chunk interaction still need manual play.
- The moving platform appears only in the two fork variants. Its generated
  sweep passes conservative clearance checks. The player controller now tracks
  grounded contact and preserves velocity relative to the moving platform so
  the player is carried while standing on it. The chosen jump behavior inherits
  its full horizontal motion. The player confirmed carry in Play Mode; jump
  trajectory, edge behavior, and interaction with neighboring chunks still
  need a hands-on check.
- Perks are selected on landing on each stage-transition platform and reset on
  a new run. The pure stacking rules are EditMode-tested; the player confirmed
  the transition choice pauses gameplay and that repeated picks stack. Perk
  balance and reset-on-`R` still merit a focused Play Mode spot-check.
- Submerged chunks are destroyed rather than pooled. This is sufficient for the
  assignment scale but creates more allocations than a production pooling system.
- The best-height value lasts only for the current application session.
- The graded target is keyboard and mouse on Windows. Gamepad, other operating
  systems, unusual aspect ratios, and other hardware have not been fully tested.
- Audio, a main menu, character animation, collectibles, and persistent
  progression remain deliberate scope cuts. The current run-only perk choice
  is intentionally small; extra tools, weapons, and combat are not included.
  The complete loop communicates required state through motion, shape, text,
  and visual feedback without those systems.
