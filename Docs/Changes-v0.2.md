# Oilpuz 0.2 — casual mala and liquid film

## Appearance

All five Mala images were redrawn with the built-in image generator using explicit style-transfer prompts: smooth silhouettes, illustrated shading, simplified food details and a warm apricot/cream/jade palette. Alpha and existing Unity sprite GUIDs were retained. Prompts and source-output mapping are in `Art/Casual-v02-prompts.md`.

The oil now has a translucent, nearly uniform film interior, a narrow meniscus and directional edge glints. Removed broad convex shading that made it look like gelatin. A wider density kernel smooths the visible hexagonal particle footprint.

Stretched surviving links contribute a narrow capsule to the density field, keeping the thin neck visibly continuous until the simulation actually splits. No capsules are drawn across severed links or different groups.

## Motion

Material keeps its identity at the grabbed point, but neighbouring particles can exchange connections. Link rest lengths relax over time rather than storing the original shape indefinitely. Local viscosity and reduced feedback from positional corrections dissipate rebound, while weak capillary cohesion settles the outline. Slow movement still pulls a trailing region; fast excessive neck strain can split it.

Material counts, merging, obstacle collision, gate size rules and stage data are preserved. This is a game-oriented viscous particle approximation, not a full physically calibrated oil/water solver.

## Delivery

Android package: `com.kimjunho.oilpuz`, version 0.2.0 (2), ARM64, Android 6/API 23 or newer, OpenGL ES 3. APK uses the development machine's debug signing key for sideload testing; this is not a store-signed release.

Unity caches, logs, builds, APK/AAB files and signing keys are excluded from Git. APKs are distributed as GitHub release attachments rather than source history.
