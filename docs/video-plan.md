# Video Plan

Status: Ready to record

Target 19-21 minutes. The first gameplay section must remain below 7 minutes.

| Segment | Target time | Content |
| --- | ---: | --- |
| Introduction | 0:30 | Name, one-sentence pitch, controls |
| Gameplay | 5:30 | One run, movement and jumps, grapple hit and miss, optional anchor skip, stage transition, accelerating flood, death, and a new seeded restart |
| Project structure | 2:00 | Scene hierarchy, player/anchor/chunk prefabs, config |
| Grapple implementation | 4:00 | Projectile arc, single-hook flight/return states, valid attachment, automatic pull, and arrival release |
| Procedural tower | 3:00 | Chunk metadata, seed, selection, placement, validation, fallback |
| Unity features and presentation | 2:30 | Input, Rigidbody/collision, LineRenderer, procedural textures/materials, tower shell, water shader, lighting/fog, feedback, HUD, and Inspector setup |
| Tests and limitations | 1:30 | 14 EditMode tests, seed diagnostics, standalone build verification, and honest scope cuts |
| Summary | 0:30 | What was learned and where the build/project is |
| **Total** | **19:30** | |

## Recording preparation

- [ ] Prepare a stable demo seed/run
- [ ] Prepare the exact scripts and Inspector objects to open
- [ ] Increase Editor/code font size for the recording
- [ ] Close unrelated windows and hide personal information
- [ ] Disable notifications
- [ ] Record and play back a short voice/screen test
- [ ] Keep bullet notes visible on a second screen or printed
- [x] Start from a clean project open and confirm the build works

## Explanation rule

For every important feature, explain:

1. What problem it solves
2. Which Unity object/script owns it
3. What data goes in
4. What decision or state change occurs
5. How the result reaches the player
6. How it was tested and what its limitations are
