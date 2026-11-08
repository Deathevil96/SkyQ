# SkyQueue: Desert Rescue — project notes for Claude Code

Unity **6000.3.17f1**, URP, portrait, Android first. Airplane version of a Bus-Fever-style parking/boarding puzzle:
stranded passengers at a remote desert airstrip are flown out by the planes. Spec: `SkyQueue_Bus_Fever_Party_Reference_GDD.md`
(its reference-fidelity directive overrides the rest). Reference: Level-15 screenshot + screen recording in the repo root.

## Owner rules (always)
- **Check Play mode first.** If the editor is in Play mode, change nothing (edits are lost) — tell the owner.
- **Never close / restart the Unity editor** and never run heavy main-thread jobs. **No bot-play tests or difficulty-tuning runs** (they froze the editor). Use the fast `LevelValidator`.
- No hardcoding: gameplay, layout, timing, colours live in config/data. Industry-standard, optimised code (no per-frame allocations, GPU instancing, pooling).
- Work autonomously when asked; report honestly what was and was not verified.
- **Back up the owner's save before any Play-mode test** and restore it afterwards (`persistentDataPath/skyqueue_save.json` → `.pretest`). Tests must never leave progress in the real save.
- Art prompts must spell out delivery details: 2D sheets "on a perfectly flat solid magenta (#FF00FF) background, no checkerboard, no gradient, no glow", pieces separated; 3D: which parts must be separate meshes, orientation, pivot.

## Decisions (do not re-litigate)
- Stands: 1 VIP + 4 plain + 3 buyable "+" (8). Tall pads tilted 12° (top leans left), visible gap; parked planes keep board size (scale 1 when the large plane fits).
- Parked planes **swell** as passengers board (owner loves it): deterministic `feel.boardGrowMax` (+35% when full). Keep it.
- Loop: two feed lanes → clockwise rounded-triangle ring (fixed-slot conveyor); level starts with the ring empty; rows of 4 move as a unit but board one passenger at a time. Feed rows wait at a hold line.
- Plane exits: straight out, side rails inside the real screen edges, invisible floor line above the booster bar.
- VIP booster = helicopter airlift (planes never fly over the board).
- Emoji reactions appear on rows waiting in the **feed lines**, never on the ring.
- Worlds rotate every 10 levels (`GameConfig > worlds`): Blossom Garden (levels 1–10, 21–30…) and Desert Rescue (11–20…).
- Product name "SkyQueue: Desert Rescue"; Android id still `com.skyqueue.prototype`. Renaming the product moves the editor save folder.

## Code layout
- `Assets/SkyQueue/Scripts/Core` — pure model (asmdef SkyQueue.Core): board, ring, rules, levels (30 authored + procedural to 1000), validator, economy/save, boosters, DevLog.
- `Assets/SkyQueue/Scripts/View` — views, layouts, HUD, config (asmdef SkyQueue.View). `GameController` orchestrates.
- `Assets/SkyQueue/Tests/EditMode` — fast unit tests only (209, ~1.5 s).
- Config: the `SkyQueue` GameObject's `GameConfig` in `Assets/Scenes/SampleScene.unity` + `Assets/SkyQueue/Config/TrackDefinition.asset`. **Saved scene/asset values override code defaults — update both.**
- Art: `Assets/SkyQueue/Art` (owner's 2D sheets, 3D glb via glTFast). Processed outputs in `Art/Sprites`, `Art/Textures`. Processing scripts: `Tools/art/*.py` (Python 3 + Pillow + numpy + scipy; paths are repo-relative).
- Docs: `BACKLOG.md` (what's done / open), `DEVLOGS.md` (log tags, profiler markers, **debug menu**), `ART_ASSET_PLAN.md` (prompts). Art prompt page (artifact): https://claude.ai/artifact/8qaMEbLUcwhDgow5YmWHHr

## Tooling notes
- Unity MCP: `execute_code` (CodeDom C# 6: prefix types with `SkyQueue.`, use `UnityEngine.Object`); Play mode does not advance while the editor is unfocused → `EditorApplication.isPaused = true` + `Time.captureDeltaTime` + `EditorApplication.Step()`. Screenshots: `ScreenCapture.CaptureScreenshot("Screenshots/x.png")` (git-ignored folder). Never leave preview objects in the scene (destroy in `finally`).
- First use of a new shader in the editor shows cyan placeholders until async compile finishes — step more frames before judging.
- Debug menu (editor + dev builds): **DBG** button bottom-left or **F1** — coins, boosters, unlock, win/lose, level jumps, reset save.
- Test phone: Samsung S23 Ultra (SM-S918B). Dev APK builds to `Builds/Android/` (git-ignored).

## Status (2026-11-08) and next steps
- Done: GDD phases 1–4 (gameplay, 1000 levels, economy, save, boosters, sound placeholders, haptics, settings, dev logs, analytics), world rotation, debug menu; art wired in: planes, passengers (4 archetypes × 8 colours), helicopter (rotor split in code), stand pads, emoji, booster icons, garden/asphalt + desert sand/dirt/concrete textures, desert signal smoke.
- Exhaust puffs behind planes taxiing to the stands (`ExhaustPuffs`, `GameConfig > exhaust`, frames `Art/Sprites/Exhaust`): verified in Play mode 2026-11-09 (trail visible, fades out via `startAlpha`/`fadeStart`, pool empties after parking).
- Open: desert props W4–W9 (tower, hangar, windsock, cacti to replace green blobs, rocks, rescue camp); UI kit + VFX sheets slicing; sound clips; phone pass on the S23; difficulty tuning by playtest.
- Owner is generating more assets; when they say "added assets", list new files under `Assets/SkyQueue/Art`, inspect, wire in, verify with a Play-mode screenshot.

## Mac setup reminders
- Open the project with Unity Hub (6000.3.17f1), let it import (Library is not in git), then reconnect the Unity MCP (`/mcp`).
- Editor save on macOS: `~/Library/Application Support/DefaultCompany/SkyQueue_ Desert Rescue/skyqueue_save.json` (the Windows save is not in git; use the debug menu to add coins / jump levels).
- Android build on Mac needs the Android module installed in Unity Hub.
