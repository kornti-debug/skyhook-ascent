# Game Concept Decision

Status: Decision Made  
Working title: **Skyhook Ascent**

## Selected concept

An endless third-person 3D vertical platformer inside a hollow cylindrical tower. The player outruns a rising hazard by running, jumping, and firing a gravity-affected grappling hook at marked anchors. Procedurally assembled tower sections create a different upward route for each run.

## Why this concept was selected

- It builds directly on the third-person 3D work covered in class.
- Movement, jumping, grappling, and momentum provide a strong playable core.
- The grappling hook creates a focused technical feature for the video explanation.
- Procedural chunk assembly supports the optional procedural-generation bonus without requiring arbitrary unverified platform placement.
- Rising water creates pressure and turns a fall or slow route into a meaningful consequence.
- Short safe/risky branches add decisions without multiplying the generator's complexity.
- Simple geometry can still look coherent through lighting, fog, materials, particles, and readable anchor design.

## Locked design decisions

- Third-person 3D presentation
- One endless upward run
- Interior of a hollow cylindrical tower
- Platforms distributed around the inner wall and central shaft
- Route appears to spiral because generated chunks rotate around the vertical axis
- One guaranteed route through every chunk
- Occasional short branches that rejoin before the chunk exit
- Safer jump route versus faster, harder grapple route
- Fixed visible grapple anchors rather than grappling arbitrary surfaces
- Grapple projectile follows an arc and requires aiming above distant targets
- Momentum-preserving swing and release
- Rising water/void as the primary hazard and timer
- Height as the primary score
- Seeded procedural assembly from handcrafted, validated chunk prefabs

## Parked alternatives

- Signal Bloom: graph-routing puzzle with environmental restoration
- Polarity Forge: attraction/repulsion physics puzzle
- VR Signal Technician: room-scale circuit repair puzzle

These are no longer active options for this assignment.

## Explicit non-goals

- Combat or enemies
- Free grappling on every surface
- Large authored campaign
- Several permanent routes through the whole tower
- Full rope-climbing or Zelda-style stopped-rope rotation
- Inventory, equipment, or permanent progression
- Roguelike upgrade system before the complete base loop works

