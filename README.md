# EbCRD Final Assignment

A small Unity game for the final assignment in Creative Computing and Engine-Based Cross Reality Development.

The Unity project lives in [`final_assignment/`](final_assignment/). Planning and submission documentation stays at the repository root so it is readable without opening Unity and does not create Unity `.meta` files.

## Current status

- Assignment requirements reviewed.
- Unity 6 URP project created with the Input System.
- Game concept locked: a procedural vertical 3D grappling platformer (working title: **Skyhook Ascent**).
- Lightweight architecture selected; the reusable blueprint will not be copied wholesale.
- MCP for Unity is connected and verified as editor-only development tooling.
- Initial GitHub baseline created and pushed.
- Movement playground scene created and verified.
- Gameplay input map created and verified.
- Next task: **M1.3**, player movement and grounded jumping.

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
- Main project assets will live under `Assets/Game/`
- Unity MCP is development tooling only and must never become a runtime dependency

Before submitting a ZIP, close Unity and remove the Unity project's `Library` folder from the copy being uploaded.
