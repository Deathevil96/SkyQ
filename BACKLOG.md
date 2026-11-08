# SkyQueue backlog

Ideas and requests not scheduled yet. Owner-requested items are marked **(owner)**.

## Done (kept for reference)
- **(owner)** Emoji reactions on long-waiting rows: implemented 2026-11-08 (`EmojiReactions`, settings in `GameConfig > emoji`). Uses placeholder text faces (">:(", "zZ", "?!") because the built-in font has no colour emoji; swap in emoji sprites with the art pass.

## Device (deferred by owner)
- **(owner: later)** Phone pass on the S23 Ultra: install the latest dev APK, check fps / frame time / heap (`[SkyQueue][perf]` logs) on dense 14x14-15x15 levels, the VIP helicopter, the ribbon track, booster panels, haptics and sound. See `DEVLOGS.md`.

## Art / audio
Done 2026-11-08 (owner's art in `Assets/SkyQueue/Art`):
- Plane models 16/24/40 (`Art/3D/*_textured.glb`, glTFast package) tinted per colour via grey paint textures (`Art/Textures/planeNN_paint.png`); wingspan capped at `planeArt.maxWingCells`.
- Emoji reactions use the sticker sprites (`Art/Sprites/Emoji`) and appear on rows waiting in the FEED lines (owner: never on the ring).
- Booster icons on the HUD buttons (`Art/Sprites/Boosters`).
- Garden + asphalt textures (`Art/Textures`, world-tiled, seamless).
- Passenger characters A-D (`Art/3D/Characters/*_texture_*`), mixed per row; outfits recoloured per gameplay colour into `Art/Textures/Passengers/pax_{arch}_{colour}.png` (skin/trim kept). Re-run `Tools/art/recolor_passengers.py` if the palette or characters change.
- VIP helicopter model (`Art/3D/Vehicle`); main rotor split off in code at start-up so it spins.
- Stand pads plain / VIP / locked (`Art/3D/*parking_pad*`): shapes only, coloured per world (rim + fill split at the auto-detected inner floor); a bought "+" swaps to the plain pad. Known: faint diagonal crease on the plain pad mesh.
- Desert signal smoke: `Art/2D/Cartoon Smoke Puff Animation Grid.png` -> 16 alpha frames (`Tools/art/unmatte_flipbook.py`), looping flipbook at `extraWorlds[0].ambientPx`.
- Desert world textures: sand, dirt road, concrete strip (`Art/Textures/desert_*`), assigned to `GameConfig > worlds > extraWorlds[0]`.
- Source sheets are processed by `Tools/art/*.py` (flood-fill background removal, seamless tiling, paint/outfit recolour); re-run if the owner replaces a sheet.

Still to do:
- UI kit sheet (`Art/2D/Glossy Casual Game UI Kit.png`): buttons, panels, coin, toggles, banners need slicing into 9-sliced sprites.
- VFX sheet: background haze must be cleaned before use (smoke, confetti, sparkle, dust).
- Extra-slot icon could replace the "+" text on purchasable stands.
- Desert props W4-W9 (control tower, hangar, windsock, cacti, rocks, rescue camp); cacti replace the green placeholder blobs.
- Garden prop models; real sound clips for every `Cue`.

## Balancing
- Difficulty tuning of the 30 authored levels and the procedural bands (bot-based tuning was dropped at the owner's request; tune by playtesting, or with a cheaper offline tool later).

## Track
- Smoother feed-to-ring hand-off on the right merge (accepted as-is for now: ring is clockwise so the left feed fills faster).
