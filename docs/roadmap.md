# 40-Hour Roadmap

Status: Current Plan  
Project: **Skyhook Ascent**

## Time budget

| Phase | Budget | Exit condition |
| --- | ---: | --- |
| Concept and design lock | 2 h | Pitch, mechanics, route model, MVP, and cuts are documented |
| Repository and MCP foundation | 3 h | Source control, package setup, Unity connection, clean compile |
| Player movement and camera | 4 h | Walk, run, jump, air control, and camera work in a test scene |
| Grapple vertical slice | 8 h | Projectile arc, one-hook retrieval, automatic zip, release, and one landing feel good |
| Run loop and rising hazard | 4 h | Start, climb, water death, height score, and restart work |
| Procedural tower chunks | 6 h | Fixed-seed chunk assembly produces validated playable stages |
| Aesthetics and feedback | 4 h | Tower readability, anchors, rope, lighting, fog, particles, and UI are coherent |
| Tests, build, and fixes | 3 h | Clean compile, focused tests, packaged build, known limitations |
| Video planning and recording | 6 h | 15-25 minute video and both submission artifacts are verified |
| **Total** | **40 h** | |

## Gates

### Gate 1: concept locked - hour 2

- Vertical inner-tower setting
- Ballistic projectile and automatic zip grapple
- Rising hazard
- Braided chunk routes
- Procedural chunk assembly
- Upgrades explicitly classified as stretch

### Gate 2: movement prototype - hour 9

- Player movement and camera are responsive
- Grounding and jumping are reliable
- Movement test platforms are playable

### Gate 3: grapple proof - hour 17

- Hook visibly follows an arc
- Only valid anchors attach
- A miss visibly returns before another shot becomes available
- A valid hit starts a fast, collision-aware pull
- Arrival releases automatically over the intended platform
- Player can complete one jump-shoot-zip-land sequence repeatedly

If this gate slips by more than two hours, simplify the hook before adding procedural generation: enlarge anchors, shorten miss recovery, and reduce zip acceleration tuning.

### Gate 4: complete run - hour 21

- Rising hazard creates pressure
- Falling into it ends the run
- Height score and restart work
- A generated starter tower produces the complete gameplay loop

### Gate 5: procedural bonus candidate - hour 27

- At least four validated chunks
- Same seed produces the same sequence
- Chunks align and do not visibly overlap
- Required routes work with base abilities
- A 24-chunk generated window is valid and playable before streaming is enabled
- Endless generation appends ahead of the player and removes only chunks below
  the flood
- Each roughly 80-100 m transition introduces a readable visual stage and faster flood

### Gate 6: submission candidate - hour 34

- Visual states and anchors are readable
- Build runs outside the Editor
- Console is clean of project errors
- Known limitations are written
- No new systems are added

### Gate 7: delivered - hour 40

- Recording test passed
- Final video is within 15-25 minutes
- Gameplay segment is at most 7 minutes
- Project ZIP excludes `Library`
- Both upload artifacts/links were tested

## First implementation sequence

The referenceable task breakdown and status live in
[`tasks.md`](tasks.md). The implementation order is:

1. Create the movement playground.
2. Finish and verify movement before grappling.
3. Prove one satisfying grapple sequence.
4. Complete the start-climb-fail-restart run.
5. Add deterministic procedural chunks with fixed-seed diagnostics.
6. Convert the generator into a bounded endless streaming window.
7. Add stage transitions and height-based material palettes.
8. Improve readability and aesthetics.
9. Freeze features, build, test, and record.

## Work log

| Date | Duration | Work completed | Next risk |
| --- | ---: | --- | --- |
| | | | |

## Scope-cut order

1. Upgrade/roguelike system
2. Collectibles
3. Persistent high score
4. Audio
5. Crumbling platforms
6. Additional chunk variants
7. Safe/risk branches, retaining single-route chunks

Never cut responsive movement, grapple, rising hazard, deterministic procedural assembly, death/score/restart, build verification, or explanation preparation.
