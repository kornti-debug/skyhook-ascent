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
- Grapple anchors are centered in clear space above their intended landing platforms.
- Chunks rotate around the vertical axis, creating an upward spiral without requiring one continuous staircase.
- The central shaft provides space for readable projectile arcs, fast zip lines, and dramatic falls.
- The camera follows the player inside the tower rather than showing the entire structure.

Every chunk has one entrance and one exit. Most chunks have one readable route. Selected chunks contain a short branch:

- **Safe route:** more platforms and easier jumps, but slower.
- **Risk route:** fewer platforms and a demanding ballistic grapple shot, but faster.
- Both routes rejoin at the chunk exit.

Mandatory grapple chunks ensure the main mechanic cannot be ignored.

## Core run loop

1. Start a run with a displayed seed.
2. Climb through generated tower chunks.
3. Choose safe or risky routes when offered.
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
- The rope is visual feedback and shortens naturally as the distance closes; it is not a simulated pendulum
- Grapple can be fired from the ground or in the air

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

Each chunk provides:

- Entry transform
- Exit transform
- Bounds for overlap checks
- Difficulty
- Selection weight
- Traversal category
- Optional route-choice metadata

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
12. Keep the authored course active if initial generation cannot complete safely.

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
- Stage geometry and materials must not change the reachability rules.

Every chunk must be tested with base movement and grapple values. Upgrades may make routes easier or unlock optional shortcuts, but required progression never depends on an upgrade.

### Initial chunk set

1. Start/warm-up platform
2. Basic jump sequence
3. Mandatory grapple across the shaft
4. Safe jumps versus grapple shortcut
5. Recovery/rest section
6. Raised round zip-transition landing between generated stages

### Authored chunk prototype

Before prefab conversion or procedural assembly, `Gameplay.unity` contains a
manually arranged clockwise spiral made from four named chunk groups:

1. `Chunk_00_Warmup`: three increasingly high jump platforms.
2. `Chunk_01_Zip`: one mandatory ballistic shot to an anchor centered above a
   wide landing platform.
3. `Chunk_02_Jumps`: two more platforms continuing around the tower axis.
4. `Chunk_03_MixedRecovery`: a second zip landing followed by a normal jump and
   a wider recovery/exit platform.

Each group has an `Entry` and `Exit` transform showing how chunks connect. The
four validated groups are preserved in the scene as the fallback and converted
to reusable prefabs. Derived jump, grapple, mixed, mirrored, precision, and
recovery variants bring the current asset set to 12 shapes: one fixed warm-up
plus 11 reusable choices. Seed `104729` remains a useful deterministic
regression seed. Normal play uses a fresh run seed and continues beyond the
initial 24-chunk stage.

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

Required run-end UI:

- Maximum height
- Seed
- Restart

The authored fallback still contains a round goal platform for regression
testing, but generated play is endless and has no normal finish. Flood contact
ends the run. `R` cancels any active grapple, creates a fresh seeded tower, and
returns the player, hazard, score, grapple, and camera state to the start.

In the Unity Editor only, `F3` toggles collision-free debug flight for streaming
and geometry inspection. Use `WASD` to move, `Space`/`E` to rise,
`Left Ctrl`/`Q` to descend, and `Left Shift` to boost. The component is inert in
player builds and is not part of the normal game rules.

Persistent high scores are optional.

## MVP acceptance criteria

- Player can walk, run, jump, and control direction in the air.
- Grapple projectile visibly arcs and attaches only to valid anchors.
- A valid hit automatically zips the player to the anchor and releases near it.
- A miss reaches its range or an invalid surface, returns visibly, and only then restores grapple readiness.
- One handcrafted fallback course supports a complete start-climb-fail-restart loop.
- Generated play streams additional stages before the player reaches the top.
- Rising hazard reliably ends the run.
- At least four chunk prefabs assemble from a fixed seed.
- Repeating a seed produces the same chunk sequence.
- Every required chunk route is completable using base abilities.
- Generated chunks do not visibly overlap.
- Submerged stages are removed only after the flood makes recovery impossible.
- `R` always returns the player to the start with a new displayed seed and tower.
- Project compiles without project errors.

## Stretch features

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

## Scope cuts

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

## Known risks

- Zip speed, arrival distance, and anchor placement may consume more tuning time than expected.
- A real projectile can miss thin targets at speed; collision handling must be robust.
- A blocked path to an anchor must time out without trapping the player in the pulling state.
- Camera collision inside the cylindrical tower needs early testing.
- Generated chunk rotation can create visual intersections even when traversal remains valid.
- Rising hazard speed must pressure the player without making safe routes pointless.
- Structural generation is automated, but each accepted seed still needs a human traversal play-test.
- The six derived variants reuse validated primitives, but their changed gaps
  and rhythm require manual base-ability validation before they are considered
  submission-safe.
- Clearance validation uses a conservative sampled corridor rather than an
  exact simulation of every possible ballistic aiming arc. It prevents known
  platform obstructions but does not replace manual traversal testing.
- Cleanup destroys submerged chunks instead of pooling them. This is adequate
  for the assignment but creates more runtime allocations than a production pool.
- The transition landing, three stage palettes, and recycled structural tower
  shell have a coherent color, lighting, and fog pass. Their final traversal
  readability still needs a human play-test from the normal gameplay camera.
- The raised zip transition passed an automated eight-stage generation stress
  run, but its ballistic aim and landing feel still need a normal player
  traversal check before the submission build is frozen.
- Roguelike upgrades are not implemented and remain outside the current MVP.
