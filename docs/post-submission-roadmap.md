# Post-Submission Development Plan

Status: route variants and moving traversal work; the first three-choice
stage-boundary perk slice is implemented and awaits Play Mode testing
Project: **Skyhook Ascent**

This plan covers optional development after the university assignment was
submitted. The submitted build remains the playable baseline. Items below are
ideas and proposed work, not claims about features already implemented.

## Product direction

Keep the game's identity focused on ascending a procedural tower under pressure
from the rising flood. The main skill should remain choosing a route, moving
cleanly, and landing gravity-affected grapple shots. New systems should make
those decisions more interesting rather than replace the grapple with a
separate combat game.

### Current decisions

- Keep the active tower envelope and cylindrical silhouette for now. A cone
  that widens with height is deferred because it would require coordinated
  changes to chunk placement limits, bounds validation, and the stage shell.
- Aim for a quick, readable “3D Doodle Jump” rhythm: varied jump layouts, with
  grapples creating optional shortcuts and harder route choices.
- Add variety first through a small library of hand-authored layouts that fit
  the current envelope, including mirrored or compact straight-looking forks.
  Do not make every chunk larger or let chunks extend arbitrarily across the
  tower.
- `Chunk_RouteFork` and its mirrored variant remain active. The straight fork
  draft is parked in
  `final_assignment/Assets/Game/Prefabs/TowerChunks/Ideas/` and is not in the
  generator pool. Revisit it only after refitting its footprint and refreshing
  its placement metadata.

### Design goals

1. Create moments where the player chooses between a safe, slower route and a
   difficult, faster route.
2. Make optional grapple challenges more expressive through timing and moving
   targets while preserving ballistic aiming.
3. Keep generated routes readable, fair, and completable using the base player
   abilities.
4. Add content in small playable slices and check whether each change improves
   replay interest before expanding it.

### Current baseline

- The game has one main ascending route through a stream of generated chunks.
- Players can sometimes skip ahead by making a more difficult long-range shot
  at an anchor. This is an emergent shortcut, not a designed branch.
- The flood accelerates as the player reaches higher stages.
- Chunks are authored prefabs assembled, rotated, validated, streamed ahead, and
  removed after they are safely below the flood.
- The grapple projectile follows a gravity-affected arc, attaches only to
  anchors, pulls the player automatically, and returns after a miss.

### Out of scope for the next small phase

- Bow or gun combat and a broad enemy roster
- Permanent character progression or a large roguelike upgrade catalogue
- Multiplayer, online leaderboards, or a large collection of game modes
- Changing all biome materials into different movement physics at once

These can be reconsidered after the traversal additions prove fun.

## Proposed development order

### P0 - Capture the current feel baseline

Before changing movement, record a short run and note what feels good or weak:
movement responsiveness, jump timing, grapple aiming, zip arrival, camera
readability, flood pressure, and run length. Use the same observations when
checking later prototypes. Avoid broad tuning changes without a specific play
test problem to solve.

**Exit condition:** a short baseline note exists, including the current seed or
build used for comparison and the top one or two feel issues to address.

### P1 - Validate one route-choice chunk

**Implementation status:** `Chunk_RouteFork` is included in the
runtime chunk pool as one deliberately larger route-choice module among the
existing short chunks. Its safe lane has eight sequential jump platforms. The
shortcut has two ballistic grapple targets, each centered 3.2 m above its
landing, followed by one jump to the shared exit. Both lanes use the same entry
and rejoin on the same exit platform.

The first enlarged layout exceeded the generator's 12 m exit-radius limit, so
the curved design keeps its routes close to the tower's spiral. At the
revision covered by the earlier geometry checks, bounds and traversal
clearances were validated, a Play Mode generation check with seed `10001` built
all 24 chunks and aligned both neighboring entry/exit markers, and all 14
EditMode tests passed. These checks verify generation and authored geometry,
not traversal fairness. Manual Play Mode testing confirmed that the baseline
layout works and feels good. For repeatable
future checks, select `ProceduralTower`, turn off `randomizeInitialSeed`, set
`seed` to `10001`, and press Play. Restore `randomizeInitialSeed` afterward for
normal randomized runs.

The baseline route-fork chunk has a shared entry and shared exit:

- **Safe route:** eight clear jumps with generous landing room.
- **Risk route:** two grapple zips that bypass several safe jumps.
- **Merge:** both routes return to the same exit so the following generated
  chunk remains connected.

The copied straight alternative is kept as an idea, not runtime content. Its
current entry-to-exit horizontal span is about 20.8 m, and its saved bounds and
entry-direction metadata still reflect the circular prefab it was copied from.
Before promotion, refit it to the current tower envelope, recapture bounds and
traversal metadata, assign a unique `chunkId`, then verify it with fixed and
varied seeds. The generator now references the baseline and mirrored forks;
the straight draft remains excluded.

The fork is hand-authored inside one existing `TowerChunk` prefab. The current
generator treats a chunk as one entry-to-exit building block, so both internal
routes can rejoin without replacing the generator. The safer and faster routes
are optional; neither is mandatory for progression.

**Play-test questions:** Can players see the choice before committing? Can a
miss recover onto the safe route? Does the risky route actually save enough
time to justify its difficulty? Does the flood make the choice matter without
making the safe route feel like a trap?

**Exit condition:** both routes are completable with base abilities, the merge
works, and several runs show players making understandable route choices.

### P2 - Build a few bounded route variants

**Implementation status:** `Chunk_RouteFork_Mirrored` now complements
`Chunk_RouteFork` in the generator pool. It reflects the complete
layout across local X, moving the risk lane to the opposite side while keeping
the same centered entry and shared exit. It has a unique `route-fork-mirrored`
ID. Both forks' derived bounds and entry directions were refreshed from their
current rendered geometry; in the mirrored prefab, both anchors still line up
with their landing platforms and sit 3.2 m above the walkable surface.

Fixed-seed Play Mode generation built full 24-chunk stages for seeds `10001`,
`10002`, `10004`, and `10006` with the mirrored variant selected; repeating
`10001` produced the same sequence. Seed `10003` was rejected at chunk 10 with
the expanded pool, though it succeeds with the original pool, so keep an eye
on generation pressure when testing combinations. The normal randomized
`GenerateNextTower` path produced a valid stage, and the original 14 EditMode
tests passed at this stage.
Manual Play Mode testing has confirmed that both variants are traversable and
neither blocks the player. Detailed comparison of relative route readability,
grapple difficulty, and time saved can wait for later balance feedback.

The parked straight draft is a starting point for study, not a template to
promote unchanged. Do not widen the radius just to accommodate it; if future
playtests show that bounded layouts cannot create enough variety, revisit the
tower shape and its wall/placement rules as a separate feature.

**Exit condition:** both handed layouts remain reachable and generated
combinations are dependable across several seeds. The player has now manually
tested both routes and reported that neither blocks traversal. Detailed timing
and balance comparison can wait for later play feedback.

### P3 - Add one moving grapple anchor

**Implementation status:** `RiskAnchor_01` in
`Chunk_RouteFork_Mirrored` now moves smoothly ±0.4 m along the landing
platform's local X axis on a 3.4-second cycle. It uses a kinematic,
interpolated Rigidbody; the projectile remains gravity-affected and receives no
aim assistance. The existing zip reads the anchor's live attachment position,
so the player is pulled toward where the target actually is.

Generation validates that the full horizontal movement sweep stays inside the
assigned landing footprint, includes the sweep in the chunk bounds, and
protects five sampled projectile paths plus the swept anchor capsule from
neighboring traversal clearances. The other grapple anchors remain stationary.
All 21 EditMode tests pass. Fixed seed `10001` generated a full 24-chunk stage
including the moving-anchor fork with no placement-clearance rejections.

The player has confirmed the anchor motion is noticeable and makes the shot a
little harder. Miss retrieval, zip landing, and nearby-chunk interaction still
need a focused Play Mode test.

**Exit condition:** manual play confirms the anchor is readable, miss
retrieval still works while the player moves, successful zips land safely, and
the moving sweep does not obstruct either route or adjacent chunks.

### P4 - Add one mid-route moving platform

**Implementation status:** `SafeJump_03`, the third middle landing on the safe
lane, now moves in both `Chunk_RouteFork` variants. It oscillates ±0.55 m along
chunk-local X on a smooth 4.2-second cycle using a kinematic, interpolated
Rigidbody. The fork entry, exit, and grapple landings remain stationary. In the
mirrored variant, the moving platform is on the safe route and the moving hook
target remains on the separate risk route.

The complete platform sweep is added to the chunk bounds. Before accepting a
placement, the generator checks it against sibling colliders and protected
traversal corridors from neighboring chunks; the chunk is rejected if the
sweep intrudes. Fixed seed `10001` generated all 24 chunks, including the
mirrored fork, with no direction or clearance rejections. Four quarter-turn
placement checks pass, and all 21 EditMode tests pass.

The player controller now identifies upward contacts with a moving platform
and applies movement relative to the platform's calculated velocity, allowing
the player to ride it without parenting the Rigidbody. Unity compiled the fix
without errors and all 21 EditMode tests passed. The player confirmed that
grounded carry works. The agreed takeoff behavior preserves the platform's
full horizontal velocity, so the player drifts with it when jumping; the
resulting jump feel still needs a focused Play Mode check.

**Play-test questions:** Does the player stay carried smoothly while standing
still and while moving? Can they jump onto and off it cleanly? Is the sweep
slow and small enough to read, and does it leave adjacent platforms and grapple
shots unobstructed?

**Exit condition:** the moving platform adds a readable timing beat without
making the safe lane unreliable or blocking neighboring chunks.

### P5 - Add a stage-boundary perk choice

**Implementation status:** landing on each round stage-transition platform
pauses the game and requires one choice from three fixed, stackable perks:

- **Quick Recall:** missed-hook return speed increases by 35% of base speed per
  pick. It does not alter aim, range, or successful zip speed.
- **Climber's Pace:** ground walking/running speed increases by 10% per pick.
  The air movement target and air acceleration remain at their baseline values.
- **Light Feet:** jump height increases by 10% per pick; air steering is
  unchanged.

The player can choose with the three on-screen buttons or number keys 1–3. The
flood and physics pause during the choice. Perk stacks reset on `R`. The HUD
shows current stacks. Fixed offers keep the first test focused on whether each
effect and the mandatory choice feel useful. Generated routes must remain
completable with baseline stats; upgrades can enable optional skips.

Unity compiles without project errors, and all 27 EditMode tests completed
with no failures. Manual Play Mode testing confirmed that landing opens the
choice, the game pauses until selection, perks stack, and `R` starts a fresh
run. Perk balance can still be revisited after more playtesting.

**Exit condition:** the menu appears exactly once on landing at every biome
transition, all three choices apply and stack correctly, the flood resumes
after selection, and restart restores base stats. The choice should feel like a
useful build decision without making normal traversal controls surprising.

## When to polish

Use two polish passes instead of waiting until the end or polishing every new
idea to final quality:

### Small feel pass now

The submitted prototype already has a coherent playable loop and readable
presentation. Keep its proven movement and grapple feel as a baseline. Add only
small feedback improvements that help test the next mechanic: clear target
motion, an understandable launch/hit sound, a distinct successful grapple cue,
and readable landing feedback. Temporary meshes and simple materials are enough
for the route-choice experiment.

### Full presentation pass after the new slice is fun

Once the route fork and one special challenge work in repeated play tests,
polish that complete slice before generating many more variants. This is the
right point to decide on a character silhouette and minimal movement/zip
animations, cohesive chunk meshes, sound for jumping/landing/grappling/flood
warnings, and any additional camera or impact feedback. Then build more chunks
using the established visual and audio language.

Keep collision shapes simple and let meshes communicate the route. Avoid
buying a large asset pack before choosing a visual direction; the current
project-owned materials can remain as a coherent style while the new gameplay
is evaluated.

## Working loop for each feature

1. State the player decision or feeling the feature should create.
2. Build one greybox example.
3. Test it in several runs, including misses and falls.
4. Check it with fixed seeds and add a rule test for any pure placement rule.
5. Keep it only if it improves the run and can be explained simply.
6. Polish its feedback, then use it as the pattern for more content.

## Parked ideas

These ideas remain available, but are not commitments for the next slice:

- More chunk shapes and arrangements
- Collapsing platforms, timed vents, wind, or other local hazards
- Additional moving-platform variants and movement paths
- Optional collectibles on difficult routes
- A short-lived jetpack or defensive effect
- A trajectory/impact preview while aiming, especially for moving grapple
  anchors; treat it as a powerful upgrade rather than baseline assistance
- Pickups that add tools such as a jetpack or bounce boots; defer tool switching
  until the traversal loop demonstrates a need for it
- Enemies and weapons; keep the current identity focused on climbing and
  avoiding hazards until combat has a clear purpose
- Surface-specific movement such as slippery ice
- More stage themes and matching environmental behavior
- A tower that widens by stage, with its shell and placement radius expanding
  together
- Persistent high scores, menus, music, and additional game modes

## Recommended next increment

The player has confirmed that landing opens the perk choice, gameplay pauses
until selection, and picks stack across transitions. In a focused follow-up
run, check the individual feel of Quick Recall, Climber's Pace, and Light Feet,
then press `R` to confirm the menu and perk effects reset. Keep the trajectory
preview, item drops, enemies, extra platform physics, the parked long straight
draft, and tower-radius changes out of this test.
