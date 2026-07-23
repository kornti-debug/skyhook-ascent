# Game Design

Status: Current Reference  
Working title: **Skyhook Ascent**

## One-sentence pitch

An endless 3D vertical platformer where the player outruns a rising flood by jumping through a procedurally assembled tower and using a gravity-affected grappling hook to swing across larger gaps.

## Player experience

The player should understand "go up before the water reaches you" within 15 seconds. Running, jumping, firing the hook, swinging, and releasing should feel responsive enough that failure feels caused by a readable decision or execution mistake.

A typical early run should last 2-5 minutes. Better players can continue until the increasing difficulty and hazard speed overwhelm them.

## Design pillars

1. **Satisfying momentum:** movement, grapple attachment, swing, and release form one continuous flow.
2. **Readable risk:** the player can see anchors, platforms, route choices, and the rising hazard.
3. **Meaningful short choices:** safe jump routes cost time; difficult grapple routes gain height quickly.
4. **Controlled randomness:** runs vary, but every required route is valid with base abilities.

## Environment and route structure

The level is the interior of a hollow cylindrical tower:

- Platforms attach to the inner wall or project into the central shaft.
- Grapple anchors hang above gaps or attach to structural beams.
- Chunks rotate around the vertical axis, creating an upward spiral without requiring one continuous staircase.
- The central shaft provides space for visible swings and dramatic falls.
- The camera follows the player inside the tower rather than showing the entire structure.

Every chunk has one entrance and one exit. Most chunks have one readable route. Selected chunks contain a short branch:

- **Safe route:** more platforms and easier jumps, but slower.
- **Risk route:** fewer platforms and a demanding grapple swing, but faster.
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
- Only objects on the grapple-anchor layer can be attached
- A missed hook returns or reloads quickly
- One active grapple at a time
- Rope length is established on attachment
- Gravity and current velocity create the swing
- Movement input applies limited tangential swing assistance
- Releasing preserves velocity
- Grapple can be fired from the ground or in the air

Not included in the MVP:

- Attaching to arbitrary surfaces
- Reeling the rope in and out
- Climbing the rope
- Completely stopping, rotating, and restarting a swing
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

Persistent high scores are optional.

## MVP acceptance criteria

- Player can walk, run, jump, and control direction in the air.
- Grapple projectile visibly arcs and attaches only to valid anchors.
- Attached player can swing and release with preserved momentum.
- One handcrafted course supports a complete start-climb-fail-restart loop.
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
- Stronger swing assistance
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

- Grapple feel may consume more tuning time than expected.
- A real projectile can miss thin targets at speed; collision handling must be robust.
- Camera collision inside the cylindrical tower needs early testing.
- Generated chunk rotation can create visual intersections even when traversal remains valid.
- Rising hazard speed must pressure the player without making safe routes pointless.
