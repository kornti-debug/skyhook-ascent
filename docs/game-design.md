# Game Design

Status: Current Reference  
Working title: **Skyhook Ascent**

## One-sentence pitch

An endless 3D vertical platformer where the player outruns a rising flood by jumping through a procedurally assembled tower and landing gravity-affected hook shots that zip them upward.

## Player experience

The player should understand "go up before the water reaches you" within 15 seconds. Running, jumping, landing a ballistic hook shot, and being pulled to the next platform should feel responsive enough that failure feels caused by a readable decision or execution mistake.

A typical early run should last 2-5 minutes. Better players can continue until the increasing difficulty and hazard speed overwhelm them.

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
5. New chunks generate above the player.
6. Chunks below the hazard are removed or recycled.
7. Contact with the hazard ends the run.
8. Show maximum height and restart with a new or repeated seed.

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
2. Align its entry with the previous chunk's exit.
3. Rotate it around the tower axis.
4. Reject overlapping placements.
5. Keep several chunks generated above the player.
6. Remove or pool chunks below the hazard.
7. Use a numeric seed for reproducibility.

Every chunk must be tested with base movement and grapple values. Upgrades may make routes easier or unlock optional shortcuts, but required progression never depends on an upgrade.

### Initial chunk set

1. Start/warm-up platform
2. Basic jump sequence
3. Mandatory grapple across the shaft
4. Safe jumps versus grapple shortcut
5. Recovery/rest section
6. Finish/debug section used before endless streaming is enabled

### Authored chunk prototype

Before prefab conversion or procedural assembly, `Gameplay.unity` contains a
manually arranged clockwise spiral made from four named chunk groups:

1. `Chunk_00_Warmup`: three increasingly high jump platforms.
2. `Chunk_01_Zip`: one mandatory ballistic shot to an anchor centered above a
   wide landing platform.
3. `Chunk_02_Jumps`: two more platforms continuing around the tower axis.
4. `Chunk_03_MixedRecovery`: a second zip landing followed by a normal jump and
   a wider recovery/exit platform.

Each group has an `Entry` and `Exit` transform showing how chunks will connect.
The scene objects remain authored prototypes until every transition is
play-tested. Only validated groups become reusable chunk prefabs.

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

Required run-end UI:

- Maximum height
- Seed
- Restart

The authored fallback course ends at a visible goal marker on the final recovery
platform. Reaching it produces a `Tower Cleared` result; contact with the rising
flood produces a failure result. Both states freeze movement and accept `R` to
restart the same course. Endless continuation replaces this finish only after
procedural streaming is proven.

Persistent high scores are optional.

## MVP acceptance criteria

- Player can walk, run, jump, and control direction in the air.
- Grapple projectile visibly arcs and attaches only to valid anchors.
- A valid hit automatically zips the player to the anchor and releases near it.
- A miss reaches its range or an invalid surface, returns visibly, and only then restores grapple readiness.
- One handcrafted course supports a complete start-climb-fail-restart loop.
- The authored course has a visible finish and supports a win-restart loop.
- Rising hazard reliably ends the run.
- At least four chunk prefabs assemble from a fixed seed.
- Repeating a seed produces the same chunk sequence.
- Every required chunk route is completable using base abilities.
- Generated chunks do not visibly overlap.
- The run can continue without manual scene changes.
- Project compiles without project errors.

## Stretch features

In priority order:

1. Crumbling platforms
2. Persistent high score
3. Two-choice temporary upgrades at safe milestones
4. Optional collectibles on risky routes
5. Additional visual tower theme

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
- Crosshair and anchor highlighting make valid targets readable.
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
