# Assignment-Sized Architecture

Status: Current Reference

## Decision

Use the blueprint as a menu of patterns, not as a starter framework. This assignment rewards a clear, working, explainable project; architecture is valuable only when it makes the one vertical slice easier to understand and verify.

## Adopt now

- Root documentation outside the Unity project
- Project-owned assets under `Assets/Game`
- Clear suffixes such as `Config`, `Rules`, `Controller`, and `View`
- Input System and URP already present in the template
- ScriptableObject configuration for values that are genuinely tuned
- Plain C# for the most important deterministic rule
- MonoBehaviours for Unity lifecycle, scene references, input, physics, and presentation
- One EditMode test assembly for important rules
- Fixed seeds and diagnostics for procedural generation
- Unity MCP as editor-only development tooling

## Defer until a feature proves the need

- Separate runtime entity objects
- Factories, pooling, and registries
- Multiple runtime assembly definitions
- A Boot scene and persistent application scope
- A formal ordered game loop
- UI Toolkit instead of simple uGUI
- A second content definition/catalog domain

## Do not add for this assignment

- VContainer or another DI container
- UniTask solely for architecture
- Wwise or another middleware pipeline
- A custom Content Workbench, Config Hub, Doctor, or verification dashboard
- A large service/interface layer
- Many generic base classes
- Multiple scenes before the gameplay loop works
- A full visual-test framework

## Initial Unity layout

Create folders only as the first assets need them:

```text
Assets/
`-- Game/
    |-- Art/
    |-- Materials/
    |-- Prefabs/
    |-- Scenes/
    |-- Scripts/
    |   |-- Core/
    |   |-- Gameplay/
    |   `-- Presentation/
    |-- Settings/
    `-- Tests/
        `-- EditMode/
```

Empty folder trees are not progress; folders should arrive with their first owned asset.

## Dependency direction

```text
Plain C# rules and state
          ^
          |
Gameplay coordination
          ^
          |
Unity input, views, prefabs, scene, UI, effects
```

The important rule is that the tested decision logic does not need a scene. There is no need to force every mutable entity out of MonoBehaviour for a project of this size.

## Expected first code seams

For Skyhook Ascent, a likely small structure is:

```text
PlayerController.cs        Movement, grounded jump, and player physics
ThirdPersonCameraController.cs  Orbit, follow smoothing, and wall avoidance
GrappleController.cs       Single-hook lifecycle, rope feedback, zip pull, and release
GrappleProjectile.cs       Ballistic flight, anchor collision, attachment, and return
GrappleAnchor.cs           Valid target marker and presentation reference
TowerChunk.cs              Entry, exit, bounds, difficulty, and metadata
TowerGenerator.cs          Seeded placement, streaming window, validation, cleanup, and fallback
ChunkSelectionRules.cs     Deterministic selection rules for EditMode tests
ChunkPlacementRules.cs     Turn-angle and protected-corridor geometry rules
TowerStreamingRules.cs     Append, recycling, and deterministic stage-seed rules
RisingHazard.cs            Stage-based hazard speed and player contact
RunController.cs           Run state, score, death, and restart
RunConfig.cs               Tuneable movement, grapple, hazard, and generation values
HudController.cs           Height, warnings, seed, and run-end presentation
```

`SkyhookAscent.Gameplay.asmdef` owns the runtime scripts. The small
`SkyhookAscent.Tests.EditMode` assembly references it and tests only the plain
chunk-selection and streaming rules. Thirteen prefab assets live under
`Assets/Game/Prefabs/TowerChunks`; the authored `Course` remains a scene-owned
fallback rather than becoming hidden generator data.

`FinishGoal` owns landing detection only for the authored fallback. Generated
play is endless. `RunController` owns loss presentation and the restart input
bridge, while `TowerGenerator` alone owns run seeds, transactional stage
generation, transition palettes, and below-flood per-chunk cleanup.

Every chunk serializes an incoming direction, outgoing direction, and traversal
clearance width captured from its real prefab geometry. Placement permits
forward and quarter-turn progress but rejects reverse turns over 100 degrees.
Future chunk colliders are also sampled against all protected traversal
corridors already placed. This separates physical overlap from functional route
clearance: two chunks may not intersect yet still be rejected if one blocks an
earlier jump or grapple approach.

These are seams, not a requirement to create every file immediately. Begin with
`PlayerController`, `GrappleController`, and one handcrafted test scene.

## Why the full blueprint is overkill

The blueprint describes a project with hundreds of scripts, several paid packages, extensive editor tooling, DI, multiple runtime systems, and many tests. Reproducing that architecture would spend the assignment's time budget on infrastructure the game does not need and would make the video harder to explain.

The transferable lesson is its discipline: clear ownership, deterministic rules, explicit lifecycle, diagnostics, and verification. We keep those properties at a much smaller scale.
