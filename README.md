# Dust
Dust is a GPU-based particle simulation and rendering system for Unity. All simulation is done in world space.

### Usage
Add the DustParticleSystem component to a Game Object and press play.   
The two values for mass, momentum, and lifespan properties set the minimum and maximum range for a random value assigned when the particle is spawned.   

### Requirements
* \>=Unity 2017.3
* GPU with compute shader support

### To Do
* Rotation over lifetime + by speed
* Velocity over lifetime
* Size over lifetime
* Emission types:
    - Sphere
    - Disk
    - Line
    - Mesh
    - Skinned mesh renderer
* Rendering modes:
    - Sprite
    - Trails
    - Instanced
* Optimization:
    - Precompute random fields
    - Precompute noise if not animated (and at lower resolution?)
* Depth buffer collision
* Dynamic kernel size
* Switch to Renderer.SetPropertyBlock for setting uniforms