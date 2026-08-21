# EbCRD Final Assignment

A small Unity game for the final assignment in Creative Computing and Engine-Based Cross Reality Development.

The Unity project lives in [`final_assignment/`](final_assignment/). Planning and submission documentation stays at the repository root so it is readable without opening Unity and does not create Unity `.meta` files.

## Current status

- The feature set is frozen as a submission candidate.
- **Skyhook Ascent** is a playable endless vertical 3D platformer with responsive
  movement, a ballistic skill-shot zip grapple, deterministic procedural stages,
  an accelerating flood hazard, restart/death flow, and a compact HUD.
- The presentation uses project-owned procedural textures, URP materials, a
  cylindrical tower shell, animated water, fog, lighting, and visual grapple and
  danger feedback. No third-party art or audio is required.
- Unity compiles without project errors and all 14 focused EditMode tests pass.
- A Windows x64 build (`Skyhook Ascent` 1.0.0) has been built and its complete
  move/jump/grapple/miss/death/restart loop has been manually verified.
- Next task: **M6.5**, record the explanatory video and prepare the final project ZIP.

## Start here

1. [`docs/assignment-checklist.md`](docs/assignment-checklist.md) - authoritative requirements and conflicts
2. [`docs/game-concepts.md`](docs/game-concepts.md) - concept decision record
3. [`docs/game-design.md`](docs/game-design.md) - living design document
4. [`docs/architecture.md`](docs/architecture.md) - what we keep from the blueprint
5. [`docs/roadmap.md`](docs/roadmap.md) - 40-hour plan and scope gates
6. [`docs/tasks.md`](docs/tasks.md) - actionable milestone task board
7. [`docs/video-plan.md`](docs/video-plan.md) - final recording structure
8. [`docs/mcp-setup.md`](docs/mcp-setup.md) - Unity/Codex connection record

## Reference material

- [`EbCRD.pdf`](EbCRD.pdf) - authoritative assignment brief
- [`assignment_description.txt`](assignment_description.txt) - older text description
- [`unity-project-architecture-blueprint.md`](unity-project-architecture-blueprint.md) - architecture reference

## Repository layout

```text
.
|-- docs/                  Project planning and submission preparation
|-- final_assignment/      Unity project
|-- EbCRD.pdf              Assignment brief
|-- README.md              Project entry point
`-- AGENTS.md              Local-only agent guidance (ignored by Git)
```

## Unity project

- Editor: Unity `6000.3.6f1`
- Render pipeline: URP `17.3.0`
- Input: Input System `1.18.0`
- Project-owned assets live under `Assets/Game/`
- Unity MCP is development tooling only and must never become a runtime dependency

## Controls

- `WASD` - move
- `Left Shift` - run
- `Space` - jump
- Mouse - aim
- Left mouse button - fire the grapple
- `R` - restart with a newly generated tower

The local Windows build is generated under `final_assignment/Builds/Windows/`.
Build output is intentionally ignored by Git; the repository contains the Unity
source project and documentation.

Before submitting a ZIP, close Unity and remove the Unity project's `Library` folder from the copy being uploaded.
