# Oilpuz — persistent conversation context

## Active workspace

`D:/oilPuzzle/Oilpuz` is the active working root from 2026-09-29 onward. The prior web prototype lives at `C:/Users/Junho/Desktop/새 폴더 (5)` and is not the active product.

## Agreed product

- Mobile portrait, top-down 2D puzzle with dimensional-looking soup and oil.
- Inspiration: slowly collecting floating oil into one mass on soup. Fast dragging tears it.
- User enjoyed the harder web version: narrow gates, a destination circle, split budgets, and late-stage currents.
- Stage 5 needs clear teaching: transport small drops through the gate before combining; zero splits allowed.
- First skin is **마라탕**. Future 짬뽕 / 라멘 skins must be data-driven, without rewriting game rules.
- Essential new requirement: the point grabbed by the finger moves first; the oil stretches locally and the rear follows. Center-following circles do not meet this requirement.

## Requested work order

1. Write a game design document and generate a prompted target screenshot.
2. Inspect that screenshot, then identify required images and other assets.
3. Generate the identified raster assets with recorded prompts.
4. Import and wire assets into the Unity project.

Proceed with reasonable assumptions. Target 9:16, Korean UI, touch and mouse, URP 2D. No need to interrupt for routine design choices. This pass includes a playable vertical slice to review the imported art and grabbed-point oil behavior; a full mobile release is outside this pass.

## Implementation guardrails

- Use particle material with persistent particle identity, local grab weights and constraints; distinguish rendered fluid from gameplay groups.
- Conserve material through merge/split. Use connection/neck tension for splitting rather than only moving a whole circle.
- Oil, goal indicators and UI glyphs are dynamic code/shader output. Food, bowl and table are prompted raster assets.
- Do not bake interactable oil, goals, UI text or movable food into the soup background.
- Record actual checks and limitations in `Docs/Validation.md`.

## Status — first Unity pass implemented

- Unity 6000.1.9f1, URP 17.1.0, Input System 1.14.0. Original SampleScene preserved.
- Requested steps 1–4 completed: written design, generated target screen, asset breakdown, five prompted raster assets, Unity import and scene integration.
- Entry scene: `Assets/Oilpuz/Scenes/MalaPuzzle.unity`. Open this scene and Play in a portrait Game view.
- Playable C# particle model: 120 Hz fixed simulation, persistent local material attachment, trailing deformation, contact merging, tension-based neck tearing, area conservation, food collisions, split budgets and goal settling.
- First generated Mala skin + five starter StageDefinition assets. New stages are adapted to the particle model, not exact copies of the web stages.
- `SoupSkin` holds imagery/palette independently of stage rules; ramen/jjamppong are extension points, not implemented skins.
- 30 editor validation checks passed; Windows development build created at `Builds/Windows/Oilpuz.exe`.
- Actual camera-render captures: `Docs/Art/ActualGame-Stage01.png`, `ActualGame-LocalDrag.png`, `ActualGame-Stage05.png`.
- Renderer orientation was fixed for Direct3D/Metal versus OpenGL. Material density is also explicitly assigned to the surface material for correct texel-size sampling.
- Local drag was tightened after visual inspection: the leading material tracks the finger while the rear is pulled by soft constraints. A material-step bound prevents flicks teleporting through ingredients.
- Remaining: mobile device profiling/builds, full human playthrough of all new stages, later fluid-feel and difficulty polish. See `Docs/Validation.md`.

## Important development notes

- 2026-09-29 v0.2: user requested less jelly-like oil, prompted casual art replacement, Android APK, Unity gitignore and Git commit/link. User then explicitly requested computer shutdown after completing delivery.
- Current remote is `https://github.com/kimjunho403/OilPuzzle.git` on main (public). Source belongs at this project root. APK belongs in GitHub release v0.2.0, excluded from Git history.
- Oil neighbours now relax/reconnect, positional feedback and rebound are damped, and rendering uses a thin translucent film/meniscus. Material identity and local grab are preserved. Still a game approximation.
- All five Mala rasters were regenerated as casual illustrations; prompts in `Docs/Art/Casual-v02-prompts.md`. Sprite GUIDs retained.
- Windows player rebuilt with input fix and v0.2 changes. 30 simulation checks and fluid relaxation checks passed. New screenshot `Docs/Art/ActualGame-Casual-v02.png`.
- Android target: package `com.kimjunho.oilpuz`, version 0.2.0 (2), IL2CPP ARM64, minimum API 23, target 35, OpenGL ES 3. Development-key signed sideload APK. See `Docs/Android-validation.txt` for build/package result. No connected Android device, so no actual device performance or touch verification.
- `RefreshCasualAndValidate` updates the existing scene without regenerating stage definitions. Prefer it over Setup when preserving stage edits.
- Final v0.2 Android build and signature/package checks succeeded (49,761,370 bytes). Final Windows rebuild and simulation checks passed. A narrow density capsule for each stretched surviving link fixes the beaded/disconnected-looking neck; final drag capture inspected. Build/report details in `Docs/Android-validation.txt`.

- 2026-09-29 input fix: custom UICard/UIRing now require CanvasRenderer; missing renderers in MalaPuzzle were repaired and saved. They previously caused GraphicRaycaster exceptions that blocked mouse input. Original direct-handler demos did not cover this.
- New Editor menu `Oilpuz > Repair and validate UI input`: edit mode repairs existing UI; Play mode validates real raycasts and queues a temporary virtual Mouse through the Input System for grab/drag/release. Both checks passed. Native UI clicks also verified oil hit detection, stage selection and restart. Windows exe is still the pre-fix build.

- `Oilpuz > Build or refresh mala scene` regenerates default data and the authored scene. Do not use it blindly after hand-editing levels.
- The authored scene has a static EXR density preview for edit mode; Play reconstructs a live renderer.
- Runtime screenshot switches are QA-only: `--oil-capture <absolute.png>`, `--oil-stage <zero-based index>`, `--oil-drag-demo`. Normal play uses real delta time; the scripted drag capture uses a fixed demo clock for reproducibility.
- ScreenCapture from a hidden Windows player produced black backbuffer images, so QA captures use a direct URP camera render. This is an actual render, not a generated concept.
- Previous desktop web folder contains a short AGENTS.md pointer back here so future game work uses this root.
