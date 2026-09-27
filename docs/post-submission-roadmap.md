# Post-Submission Development Plan

Status: both route-fork variants play-tested; straight alternative parked for refit
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
`GenerateNextTower` path produced a valid stage, and all 14 EditMode tests pass.
Manual Play Mode testing has also confirmed that the mirrored variant is
traversable; the baseline variant was tested earlier. Comparing relative route
readability, grapple difficulty, and time saved remains the next step.

The parked straight draft is a starting point for study, not a template to
promote unchanged. Do not widen the radius just to accommodate it; if future
playtests show that bounded layouts cannot create enough variety, revisit the
tower shape and its wall/placement rules as a separate feature.

**Exit condition:** manual play confirms the two handed layouts read
differently, the risky route clearly saves time, both routes remain reachable,
and generated combinations remain dependable across several seeds.

### P3 - Add one moving grapple anchor

After a few stationary route shapes are reliable, make an optional risk route
use one moving anchor. Keep its motion periodic and readable, with a clear
travel boundary and timing that a player can learn. The shot remains a normal
gravity-affected projectile; the target moves, but aim assist does not snap the
shot to it.

The current grapple code reads an anchor's live attachment position during
attachment and pulling, which is a useful starting point. The generator's
clearance checks currently reason about the anchor's authored position, though.
Before relying on moving anchors in generated play, validate the full swept
area of the anchor and keep its motion away from blocked projectile corridors
and unsafe landing positions.

**Exit condition:** the anchor is readable, miss retrieval still works while
the player moves, successful zips land safely, and fixed-seed tests cover its
placement envelope.

### P4 - Try one extra movement feature

Prototype one feature at a time. The strongest first candidate is a trampoline
or spring platform in a dedicated chunk: it naturally supports upward motion
and gives a distinct movement beat. A moving platform or one timed local hazard
could be tested instead if it better complements the route-choice prototype.

Do not combine a trampoline, moving platforms, multiple traps, and new surface
physics in the first pass. Each changes timing or reachability and should earn
its place through play testing.

**Exit condition:** the feature adds a readable decision or satisfying movement
moment without undermining the jump-and-grapple rhythm.

### P5 - Reassess run upgrades and enemies

If runs still need more variation after route chunks are fun, try a very small
temporary upgrade choice at a stage boundary. Prefer upgrades that change
options without invalidating generated jumps, such as faster hook recovery or a
single emergency recovery. Test jump-height and grapple-range changes carefully
because they can bypass intended challenges.

If an enemy is still desirable, begin with one telegraphed flying obstacle that
disrupts or bumps the player. Add weapons only if combat becomes a deliberate
new design pillar; they require their own aiming, feedback, balance, and enemy
behavior work and can draw attention away from grapple traversal.

**Exit condition:** each added system supports the ascent and makes a run
meaningfully more engaging. Otherwise remove it from the active plan.

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
- Moving platforms
- Optional collectibles on difficult routes
- A short-lived jetpack or defensive effect
- Surface-specific movement such as slippery ice
- More stage themes and matching environmental behavior
- A tower that widens by stage, with its shell and placement radius expanding
  together
- Persistent high scores, menus, music, and additional game modes

## Recommended next increment

Keep both bounded forks in the pool and compare the baseline and mirrored
versions in Play Mode. Focus on whether the safe lane feels slower,
whether the mirrored risky lane is clear and aimable, and whether both merge
cleanly into the next chunk. Recheck fixed seeds `10001`, `10002`, `10004`, and
`10006`; also watch how the generator handles seeds that cannot assemble with
the expanded pool. Do not promote the parked long straight draft or widen the
tower yet. Once the static route variants are fun and reliable, try one moving
anchor and validate its full motion path before adding other mechanics or final
art.
