# REFERENCE_BREAKDOWN — Bus Fever Party → SkyQueue

Sources: `Screenshot 2026-11-08 at 4.32.42 AM.png` (1320x2868, Level 15, annotated "Exactly same / but planes") and `ScreenRecording_11-08-2026 05-10-11_1.MP4` (45 s, 60 fps). Reviewed as 1.5 s and 0.25 s contact sheets plus an empty-track still; **not** frame-by-frame. Items marked **UNKNOWN** are unconfirmed. A copy of the screenshot lives at `Assets/SkyQueue/Reference/ref_level15.png` for the in-editor overlay (press `O` in Play mode).

## 1. Screen regions (reference px 921 x 2000, y down)
| Region | y range | Content |
|---|---|---|
| Loop area | 0 – 620 | Pink garden. Two feed lanes enter from the top edge, widen, and merge into a **ring around a small triangular island**; a big heart-shaped island sits between the lanes ("Level N" label inside). Exit gap at the bottom of the ring (x 375–545). Undo (top-left), Pause (top-right). |
| Runway / stands | 620 – 885 | Asphalt with a notch where the loop exits. One row of 8 stands: gold **VIP** (locked), **4 plain**, **3 green "+"** (purchasable). |
| Board | 905 – 1695 | Lavender floor, dense vehicle jam in a square rotated ~15–20°. |
| Booster bar | 1720 – 1900 | VIP / Arrange / Jumble buttons with "+" badges. |

Decision (owner): 4 default stands, 1 VIP, remaining purchasable (matches the reference).

## 2. Camera
- Perspective, high near-top-down tilt (vehicle side faces visible). Exact pitch/FOV **UNKNOWN**; calibrated by overlay (`GameConfig.camPitch/camFov/camHeight/camZ`).
- Fixed: never moves, rotates or zooms during play.
- Implementation: the reference composition is a "frame" fitted to the screen's safe area; the camera derives its FOV per screen so the frame keeps the calibrated scale (see `ScreenMap`). Extra screen area shows scenery.

## 3. Board / vehicles
- Rotated square grid; arrows along vehicle long axes. About 60 vehicles at Level 15; grid size **UNKNOWN** (estimated ~12 x 12 cells of ~52 px).
- Three footprints (car / minibus / bus) → plane Small/Medium/Large = 16/24/40 seats (owner's rule). Lengths 2/3/4 cells is an assumption.
- Colours: orange, yellow, blue, green, red, pink, purple, brown.

## 4. Passenger loop (confirmed from the video)
- Passengers stand in **solid-colour rows, 4 wide** (lanes are 4 columns wide). A row moves, merges and boards **as one unit**.
- Two feed queues (left and right) run in from above the screen ("GO!" markers at their entry) and merge into the ring when there is room.
- The ring circulates around the small island; the exit gap is at its bottom. A row leaves the ring only if a matching vehicle is waiting at a stand; otherwise it keeps circling.
- When a row boards, the crowd closes up behind it and the feeds push forward.
- Direction of circulation is **inferred** as clockwise (arch → right side down → bottom leftwards → left side up). Not confirmed frame-by-frame.
- **Loss (owner's description):** stands full of planes whose colours are not on the ring, ring full, no legal launch → game over.

## 5. Interaction & sequence
1. Level start banner ("SUPER HARD" at Level 15), "GO!" markers on the feeds.
2. Tap a vehicle with a clear path → it drives out along its arrow, off the board, then to the first free stand (white dust trail).
3. Number under a stand vehicle **counts down remaining seats** (e.g. 40 → 32 → 18 → 8); confetti when full; vehicle leaves to the right with a puff; stand frees.
4. Blocked-tap feedback, win/fail screens, booster use: **UNKNOWN** (not in the recording).

## 6. Mapping to the airplane concept
Buses/cars → airplanes (3 sizes). Boosters: the reference has VIP / Arrange / Jumble; the design doc lists Undo/Hint/Shuffle/Extra Slot/Bomb — **needs the owner's decision before the booster phase**.

## 7. Open items
1. Frame-by-frame pass for loop speed/direction, vehicle exit routes, and blocked-tap behaviour.
2. Camera pitch/FOV overlay tuning against the screenshot.
3. Board density (~60 planes in the reference vs 5–40 in Phase 1 levels).
