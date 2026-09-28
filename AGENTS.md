# Oilpuz working rules

- Active project root: `D:/oilPuzzle/Oilpuz`. Execute all project commands with this working directory.
- Read `CONTEXT.md` and `Docs/GameDesign.md` before changing game behavior.
- Unity version: 6000.1.9f1. Keep the existing URP 2D and Input System packages.
- Current direction: portrait mobile oil-merging puzzle, mala-tang first, replaceable soup skins later.
- Drag the material at the actual touched point. Never implement input by snapping or moving only a blob's center.
- Keep simulation, rendering, skin data, stage definitions and UI separate.
- AI-generated raster images must be persisted inside this project and listed with their prompts in `Docs/Art`.
- Preserve the user's original SampleScene and existing project assets. Create game content in `Assets/Oilpuz`.
- Do not add timers, lives, payments, ads or an economy without a request.
- Future work should continue from this Unity project; the desktop web prototype is a reference only.
