# ART_ASSET_PLAN — SkyQueue: Desert Rescue

Status (2026-11-08): plane models, emoji stickers, booster icons and the garden/asphalt textures are in the game (see `BACKLOG.md` "Art / audio"). The rest below is still placeholder. Copyable version of every prompt: the "SkyQueue Art Pack" artifact page.

Worlds rotate every 10 levels (`GameConfig > worlds`): **Blossom Garden** (world 0) and **Desert Rescue** (world 1: stranded passengers waiting at a remote desert airstrip are flown out by the planes). Shared assets (planes, passengers, UI, helicopter) are used in every world; §10d lists the desert-only pieces.

Art direction target: the broad look of the supplied *Bus Fever Party* reference (bright, saturated, chunky, toy-like, soft shadows, polished casual-mobile 3D) with **original** airplanes, passengers and environment. Do not reproduce its branded characters, vehicles, UI or text.

## 0. Conventions used by every prompt (read once)

| Topic | Rule |
|---|---|
| Units | 1 Unity unit = 1 metre. Models are authored at a convenient size and scaled by the importer/prefab, see per-asset notes. |
| Axes | **+Z = forward/nose**, +Y = up, +X = right. Pivot on the ground plane (y = 0), centred under the footprint. |
| Colour | Colour-coded assets (planes, passengers) are authored in **neutral light grey/white with a separate "paint" material slot** so the 8 gameplay colours are material swaps, never separate meshes. |
| Gameplay colour | The gameplay colour must stay the dominant readable cue; secondary details (stripes, hats) stay small and desaturated. |
| Camera | Fixed perspective, ~74° pitch looking down. Everything must read from **above**: top view and a bit of side. Detail below the camera angle (belly, underside) is wasted. |
| Text/branding | None. No logos, airline names, numbers, watermarks, baked UI. |
| Format | glTF/GLB or FBX, Y-up, metres, triangulated, one mesh + 1–2 materials, textures ≤ 1024 (planes/passengers) or ≤ 2048 (environment). |
| Meshy settings | Meshy's menu names and options change between versions. Choose the closest of: *text-to-3D, stylized/cartoon art style, low-poly or game-ready topology, PBR off or minimal, T-pose/A-pose for characters*. Names are intentionally not quoted here. Check the target polygon count, then retopologise in Blender if it is over budget (a Blender MCP is available in this workspace). |

### Compact reusable template
```
Original stylized casual-mobile 3D {OBJECT}, chunky rounded toy-like shapes, smooth glossy clean materials,
bright saturated {COLOUR}, soft bevels, simple readable silhouette from above, single object, centred,
no ground, no scene, no text, no logos, no watermark, game-ready low-poly mesh with clean topology.
Orientation: nose toward +Z, upright +Y. Style: polished casual mobile game, soft toy plastic.
Negative: photorealistic, realistic airline livery, text, logo, thin antennas, tiny parts, floating parts,
noisy texture, dirt, scratches, gritty, sharp edges, multiple objects, background, shadow plane.
```

## 1. Prioritised asset list

| # | Asset | In-game use | Priority |
|---|---|---|---|
| 1 | Airplane — Small (2:1) | 16-seat plane; jam board + stand | P0 |
| 2 | Airplane — Medium (3:1) | 24-seat plane | P0 |
| 3 | Airplane — Large (4:1) | 40-seat plane | P0 |
| 4 | Passenger archetypes A–D (4 silhouettes) | loop crowd, boarding walkers | P0 |
| 5 | Loop track textures (road + rim; shape stays procedural) | passenger loop | P1 |
| 6 | Boarding stand pieces (empty, VIP, locked "+") | the stand row | P1 |
| 7 | Runway / taxi strip | middle band | P1 |
| 8 | VIP helicopter (body + 2 rotors + hook) | VIP booster airlift | P1 |
| 9 | Airport garden dressing (trees, lantern, fountain, props) | background | P2 |
| 10 | Booster icons (VIP, Arrange, Jumble, Extra Slot + locked state) | bottom bar | P2 |
| 11 | UI kit (buttons, panels, badges, coin) | HUD | P2 (2D, see §9) |
| 12 | Emoji reaction sprites ×6 | waiting rows | P2 (§10b) |
| 13 | VFX sprites (smoke puff, confetti, sparkle) | takeoff / full | P3 |
| 14 | Sound clips ×11 (one per `Cue`) | all events | P2 (§10c) |

## 2. Airplane base (one prompt per size; shared style)

Footprint on the board is `length × 1` cells (2/3/4 × 1). The model is authored so that **fuselage length : wingspan ≈ footprint ratio**, i.e. long axis = 1.0 and wingspan ≈ 0.5 / 0.33 / 0.25 of it. Wings must never exceed one cell of width or planes overlap neighbours.

**2a. Small plane (16 seats, 2:1)**
```
Original stylized casual-mobile 3D toy airplane, small chubby propeller-style commuter plane, short stubby fuselage,
big rounded nose, short wide wings, tiny tail fin, chunky toy-plastic look, smooth glossy surface, bright light-grey
body with a separate paint area on the fuselage and wings, readable silhouette from above, nose pointing forward,
clear front and back, single object, game-ready low-poly mesh, clean topology, no text, no logo, no airline branding.
Length-to-wingspan about 2 to 1.
Negative: realistic, photorealistic, airline livery, text, logo, thin antenna, tiny parts, floating parts, gritty, dirt,
multiple objects, ground, background.
```
**2b. Medium plane (24 seats, 3:1)** — same prompt, replace the description with *"medium regional jet, longer fuselage, slim rounded wings, small engines under the wings"*, length-to-wingspan about 3 to 1.

**2c. Large plane (40 seats, 4:1)** — *"large airliner proportions but toy-like and chunky, long fuselage, rounded nose, two engine pods, narrow swept wings kept within one body-length quarter"*, length-to-wingspan about 4 to 1.

Specs: ≤ 5 000 tris each; 2 materials (`Body_Paint` colour-swappable, `Trim` neutral); a flat white roof area on the top of the fuselage reserved for the arrow decal (the arrow stays a separate quad mesh so direction is always crisp). Variants: 8 gameplay colours via `Body_Paint` swap (MaterialPropertyBlock or material instances). Direction is encoded by the model's forward axis; the importer applies no rotation.

## 3. Passengers — 4 archetype silhouettes

One character per model, full body, grounded neutral pose (standing, arms slightly out), limbs clearly separated, big round head, stubby torso — they are seen from above at ~35 px, so **head + shoulders silhouette is what matters**. Body is the gameplay colour; skin/hair/hat/accessories are small and desaturated.

| ID | Silhouette idea | Distinguishing feature |
|---|---|---|
| A | Round "bean" body, big head | plain beanie |
| B | Tall slim body | cap with brim |
| C | Wide chubby body | backpack |
| D | Small kid body | pigtails / bobble hat |

```
Original stylized casual-mobile 3D character, {ARCHETYPE DESCRIPTION}, chunky toy-like proportions, huge round head, stubby
body, simple mitten hands, soft rounded shapes, smooth clean materials, single solid colour outfit (paint area), small
neutral accessory, standing neutral pose, arms slightly away from the body, full body, one character, no scene, no
background, readable silhouette from above, game-ready very low-poly mesh with clean topology, no text, no logo.
Negative: realistic, scary, detailed face, tiny fingers, thin accessories, noisy texture, multiple characters, background.
```
Specs: ≤ 600 tris, 1 material with a vertex-colour or 2-colour split (paint vs neutral), no skeleton needed (the crowd bobs by transform; the boarding walk is a hop). Instancing: passengers are drawn with GPU instancing, **one material per gameplay colour**; keep ≤ 4 archetype meshes → 4 instanced draws × 8 colours.
**Crowd spacing is fixed** (rows of 4, 0.30 world-unit spacing): archetypes must fit the same ~0.3 × 0.3 footprint so variety never changes gameplay layout.

## 4. Passenger colour variants

Eight colours, defined in `GameConfig.palette` (index = colour id): orange, yellow, blue, green, red, pink, purple, brown. Variants are **material swaps** on the paint slot (no new meshes). Accessibility: add a small secondary pattern per colour later (dot/stripe on the hat or backpack) — a texture-mask in the paint material, not geometry.

## 5. Loop track (road, rim)

The track is a smooth rounded-triangle ring plus two feed lanes and an exit neck, drawn as procedural ribbon meshes from `TrackDefinition.asset`. **Keep the meshes procedural** (the shape is tuned data) and only supply textures/materials. Ribbon UVs run along the road (U = distance, V = across), so the road texture must tile along one axis.

```
Seamless tileable top-down texture of a clean grey asphalt-style road for a casual mobile game, soft stylized painted look,
subtle lighter edge, no cracks, no markings, no text, no perspective, flat orthographic, 1024x1024.
```
```
Seamless tileable top-down pink garden ground for a cherry-blossom themed casual mobile game, soft stylized painted look,
a few scattered petals, no text, flat orthographic, 1024x1024.
```
**Transparent / background-free output needed:** petals overlay decal (PNG with alpha). Everything else is opaque.

## 6. Boarding stands (3 pieces)

Flat rounded-rectangle pads, one cell-row wide, skewed like the reference row.
```
Original stylized casual-mobile 3D flat parking-stand pad for a toy airplane, rounded rectangle slab with a thin raised rim,
dark grey inset surface, lighter rim, viewed from above, single object, game-ready low-poly, no text.
Variants: (1) plain grey rim, (2) VIP with a gold rim and a small crown mark, (3) locked with a green rim and a plus sign.
```
Specs: ≤ 300 tris, 1 material each. The plus and crown are separate small meshes/decals so text rendering is not baked in.

## 7. Runway / taxi strip and airport dressing

```
Original stylized casual-mobile 3D asphalt taxi strip piece for a toy airport, wide flat grey band with soft painted lane
dashes, slightly raised kerb on one edge, top view, tileable along one axis, no text, no logo, low-poly.
```
Garden dressing (separate prompts, one object each, ≤ 1 500 tris, transparent-free 3D): cherry-blossom tree, stone lantern, small fountain/well, bush, bench, sign post (blank). Placed at the screen edges only; never over the loop or board.

## 7b. VIP helicopter

Flies in, hovers, lowers a cable hook, lifts a plane from the board and sets it on the VIP stand. Seen from above at roughly plane size.
```
Original stylized casual-mobile 3D toy rescue helicopter, chubby rounded cabin, big bubble cockpit window, short tail boom
with a small tail rotor, two landing skids, a winch hook point under the belly, bright orange body with white trim, chunky
toy-plastic look, smooth glossy, readable silhouette from above, single object, game-ready low-poly, no text, no logo.
Negative: realistic, military, weapons, thin details, text, livery, multiple objects, background.
```
Specs: ≤ 4 000 tris. **Main rotor and tail rotor must be separate meshes** (they spin in code; pivot at the hub), plus a separate small hook mesh (the cable is drawn by code). Pivot at the cabin centre. Values for scale/heights/speeds live in `GameConfig > helicopter`.

## 8. Booster icons (VIP, Arrange, Jumble, Extra Slot)

The reference shows small 3D toy icons on blue buttons. Generate as **3D models rendered to icons** (one object each, transparent PNG export from a turntable render at 512×512):
```
Original stylized casual-mobile 3D icon object: {VIP: a small orange toy helicopter carrying a tiny plane on a hook |
Arrange: a small toy airplane with a neat row of tiny people beside it | Jumble: a rainbow-segmented ball with swirl
arrows | Extra Slot: a parking pad with a big green plus}, chunky toy-like, bright saturated colours, smooth glossy, single
object, soft bevels, centred, no text, no logo, game-ready low-poly.
```
Also needed: a **padlock badge** (transparent PNG) for locked boosters (the game greys the button and shows "Lv N" text), and a small count bubble background. Future boosters: Undo = curved arrow badge, Hint = lightbulb, Bomb = round toy bomb with fuse. **Output needed: transparent PNG icons**, plus the 3D source.

## 9. UI art (2D — separate from the 3D prompts)

Not generated by Meshy. Produce as vector/2D: rounded blue button (normal/pressed/disabled), green primary button, round restart and pause buttons with icon, level label plate, coin icon + coin pill, result panel (win / fail), buy-booster panel, booster-unlock intro panel, pause panel with 3 toggle switches (sound, vibration, reduce motion; on/off states), seat-counter badge (currently a coloured quad), difficulty banners (HARD / SUPER HARD ribbons), toast pill. Slice as 9-sliced sprites; the HUD already reads layout from `HudLayout`. All text is rendered by the game (no baked words).

## 10. VFX sprites (transparent PNG)

Smoke puff (takeoff and plane launch), confetti burst (plane full), sparkle (coin), dust ring (landing in a stand). 256×256 sprite sheets, premultiplied alpha, additive or alpha-blend particle materials.

## 10b. Emoji reaction sprites (transparent PNG)

Pop above rows that have circled too long (currently text faces `>:(`, `zZ`, `?!`, `!!`, `:(`, `...`; pool and timing in `GameConfig > emoji`). Six original cartoon emoji faces, 256×256 each, thick dark outline, readable at ~40 px:
```
Original cartoon emoji face sticker, round yellow face, thick dark outline, flat bright colours, {angry with steam puffs |
sleepy with "zz" bubble | confused with question mark | shocked with exclamation marks | sad with a tear | bored with
half-closed eyes}, centred, transparent background, no text other than the symbol, no logo.
```
Must be original designs (not copies of any platform's emoji set).

## 10c. Sound clips

One clip per `Cue` (`GameConfig > sound > clips`, same order). Short, punchy, casual-mobile style, mono OGG/WAV 44.1 kHz, normalised to about −3 dBFS, no long tails:

| Cue | Sound | Length |
|---|---|---|
| Button | soft UI pop | < 0.1 s |
| Launch | light jet whoosh / engine rev | 0.4 s |
| Blocked | dull bonk | 0.2 s |
| Board | tiny footstep blip (plays per row, keep it subtle) | < 0.1 s |
| PlaneFull | bright chime | 0.4 s |
| Depart | takeoff whoosh | 0.8 s |
| Win | short jingle | 1.5 s |
| Fail | soft descending tones | 1.2 s |
| Booster | magic sparkle | 0.5 s |
| Coins | coin clinks | 0.6 s |
| Unlock | fanfare sting | 1 s |

Also optional: a looping light background music track (1–2 min, seamless loop).

## 10d. Desert Rescue world (world 1)

Until these arrive the desert uses plain sand / dirt / dusty-strip colours (`EnvironmentTheme.Desert()`). Assign the textures in `GameConfig > worlds > extraWorlds[0]` (gardenTexture = sand, asphaltTexture = dirt road; the strip shares the asphalt slot with `runwayTint` today).

| Gate | Asset | Kind | Prompt (short form; full text on the art page) |
|---|---|---|---|
| W1 | Sand ground texture | 2D tileable 1024 | warm golden desert sand, gentle wind ripples, a few pebbles and dry grass tufts, flat orthographic, seamless |
| W2 | Packed-dirt road texture | 2D tileable 1024 | packed reddish-brown dirt road, subtle cracks, faint tyre marks, lighter dusty edges, seamless |
| W3 | Dusty airstrip texture | 2D tileable 1024 | sun-bleached concrete apron, faded panel joints, drifting sand, oil stains, no markings, seamless |
| W4 | Control tower | 3D ≤3k tris | small desert airfield tower, sandy stucco base, round glass cab, white roof, tiny red beacon |
| W5 | Small hangar | 3D ≤2.5k | curved corrugated orange roof, sandy walls, half-open door |
| W6 | Windsock | 3D | orange/white striped sock on a short pole; sock a separate mesh so it can sway |
| W7 | Cacti ×3 | 3D ≤1.5k each | saguaro with two arms / barrel cactus with pink flower / prickly pear; no thin spines |
| W8 | Desert rocks ×3 | 3D ≤800 each | sandstone boulder / stacked red rocks / rock cluster with dry bush |
| W9 | Rescue camp set | 3D ≤1.2k each | white tent with orange stripe, water tank, supply crates, camp chair + umbrella; **no red cross symbol** |
| W10 | Signal smoke | 2D sprite sheet | soft orange rising signal smoke, 4×4 frames, real transparency |

All prompts follow §0 conventions (original designs, no text/logos, +Z forward, pivot at ground centre).

**2D backgrounds (all sprite sheets / icons / VFX):** image generators fake transparency with a painted checkerboard. Every 2D prompt therefore ends with *"on a perfectly flat solid magenta (#FF00FF) background: no checkerboard, no gradient, no glow or shadow on the background"*, and pieces are separated by empty space. `Tools/art/cut_sprites.py` keys magenta out (`magenta()` predicate). A real alpha PNG is even better when the tool supports it.

## 11. Unity integration notes

- **Import:** Scale factor 1, Y-up, no animation, generate colliders OFF (no physics is used; the grid is the truth), read/write OFF, mesh compression Medium, normals imported, **pivot baked at ground centre**.
- **Materials:** URP Lit, 1 shared template per type; colour swaps by `MaterialPropertyBlock` (planes) or per-colour material instances (instanced passengers). GPU instancing ON for passengers. Avoid alpha-blended materials on the board.
- **Plane footprint scaling:** `PlaneArtSettings.lengthFill` maps the model length to the footprint; check `bodyWidth`/`wingSpan` so a plane never exceeds one cell of width. Replace the placeholder by swapping `PlaneView.BuildModel` with prefab instantiation (one prefab per `PlaneSize`).
- **LOD / batching:** planes ≤ 5 k tris × 60 on screen is fine on mid-range phones; passengers are instanced (≈250 rows×4 per frame). No LODs needed at this camera distance; static-batch the environment.
- **Shadows:** soft real-time shadow for planes only; passengers cast none (the instanced path disables it for performance).
- **Validation in the target camera:** run the game at the reference aspect, enable the overlay (`O` in Play mode), capture and compare against `Screenshot … Level 15` — check silhouette size, colour dominance, arrow readability, crowd density at row spacing 0.30.

## 12. Deliverable checklist for the artist / Meshy session

1. 3 plane meshes + paint materials (colour-swappable).
2. 4 passenger archetype meshes.
3. Stand pads ×3, runway piece, track textures (optional).
4. Helicopter (body + separate main rotor, tail rotor, hook).
5. Garden props ×6.
6. Booster icon renders (transparent PNG) ×4 + padlock badge.
7. UI sprite sheet (9-sliced), including buy / intro / pause-toggle panels.
8. Emoji reaction sprites ×6.
9. VFX sprites ×4.
10. Sound clips ×11 (+ optional music loop).

Until these exist the placeholders remain and are clearly procedural (see `Art.cs`, `PlaneView.BuildModel`).
