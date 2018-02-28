# Dust
Dust is a GPU-based particle simulation and rendering system for Unity. All simulation is done in world space.

### Usage
Add the DustParticleSystem component to a Game Object and press play.    

### Requirements
* \>=Unity 2017.3
* GPU with compute shader support

### Features
* Inherit velocity from either a parent Rigidboy or Transform node
* Emission shapes:
    - Sphere
    - Mesh renderer
* Color by life and by velocity gradients
* 2D, 3D, 4D animated noise
* Mass, momentum, and lifespan random value range
* Cast- and self-shadowing

### To Do
* Rotation over lifetime + by speed
* Velocity over lifetime
* Size over lifetime
* Emission shapes:
    - Cone
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