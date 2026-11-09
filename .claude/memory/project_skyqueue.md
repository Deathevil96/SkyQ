---
name: project-skyqueue
description: SkyQueue = Unity 6 URP portrait airplane traffic puzzle cloned from Bus Fever Party; key design decisions and layout of the code
metadata:
  node_type: memory
  type: project
  originSessionId: 4656ef84-2165-4f44-9ea2-21d8b7e672e8
  modified: 2026-11-08T14:29:37.062Z
---

Unity 6000.3.17f1, URP, Android first. Spec: `SkyQueue_Bus_Fever_Party_Reference_GDD.md` (reference-fidelity directive overrides it). Reference: Level-15 screenshot + gameplay recording in the repo root.

Decisions made with the owner (do not re-litigate):
- 4 plain stands open, 1 VIP (locked), 3 purchasable "+" (8 total).
- Loop = two top feed lanes merging into a clockwise ring; rows of 4 same-colour passengers move/merge as one unit, but board the plane one passenger at a time (staggered walkers).
- Ring = fixed-slot conveyor; level starts with the ring EMPTY and rows streaming in from both feeds; waiting feed rows stand at a hold line upstream of the merge (no clash with the ring). Right merge filling slower is accepted.
- Track shape = smooth rounded triangle ring (apex up, flat bottom with exit neck to the runway), wide road; drawn as ribbons, values in `TrackDefinition.asset`.
- Plane exits: original route (straight out, side rails, up to stands) but rails sit inside the real screen edges and an invisible floor line keeps planes off the booster bar. Owner rejected the "invisible square" routing.
- Loss: ring full + no ring row matches a parked plane + no helpful launch (or board jammed).
- Stands: tall pads tilted 12° (top leans left, like the reference); parked planes must NOT shrink (scale 1 whenever the large plane fits the pad; only 12x12-and-bigger-cell boards scale slightly). Values in `GameConfig` stand fields. Pads have a visible gap (pitch 98 px).
- Parked planes SWELL as passengers board (owner loves it, "looks funny"): deterministic, `feel.boardGrowMax` (0.35 = +35% when full) + per-passenger pop. Keep it; don't "fix" it.
- Game name: "SkyQueue: Desert Rescue" (owner pick; PlayerSettings.productName). Story: stranded passengers at a remote desert airstrip flown out by the planes. Android id still com.skyqueue.prototype. Renaming productName moves the EDITOR save folder (LocalLow/DefaultCompany/<productName>); copy saves over if it changes again.
- Worlds rotate every 10 levels (`GameConfig > worlds`): world 0 Blossom Garden (`theme`), world 1 Desert Rescue (placeholder colours until desert art W1-W10 arrives). Owner chose rotation over replacing the garden.
- Emoji reactions belong on the FEED lines (rows waiting to merge), never on the ring.
- Owner art lives in `Assets/SkyQueue/Art` (2D sheets, 3D glb planes); wired-in status tracked in `BACKLOG.md` "Art / audio".
- Capacities 16/24/40; counter under a stand plane counts down remaining seats.
- Owner does NOT want bot-play tests or difficulty tuning runs (they froze the editor); use the fast `LevelValidator` instead.

Backlog of owner ideas: `BACKLOG.md` in the repo root. Dev log tags / profiler markers documented in `DEVLOGS.md`.

Status 2026-11-08: GDD Phases 1-3 implemented (30 authored + procedural to 1000 levels, economy, save, boosters VIP=helicopter airlift / Arrange / Jumble / buy "+" stands, unlock popups, sound placeholders, haptics, settings, emoji reactions); Phase 4 dev logs + debug analytics done. Owner deferred phone tests; difficulty tuning shelved; art/audio assets pending.

Paused 2026-11-08 (owner: "looks good, will come back later"). Resume point:
- Last state: 178 EditMode tests green, console clean, dev APK built at `Builds/Android/SkyQueue-dev.apk`.
- Owner's save was restored to level 4 / 60 coins after my Play-mode tests changed it; never leave test progress in the real save (back it up first).
- Level test pass 2026-11-08 (owner away): all 1000 levels pass the validator, are deterministic, generate in <27 ms; 48 sampled levels load in Play mode in <9 ms with no exceptions. Fixed: `LevelData.Solution` was "reverse placement order" (illegal for 698 levels); now the verified greedy `GameModel.SolveOrder`, checked by the validator. 209 EditMode tests green. Save backup kept at `persistentDataPath/skyqueue_save.json.pretest`.
- Open items, in likely order: phone pass on the S23 (deferred by owner), art/audio assets (emoji sprites, sound clips, models, UI art), difficulty balancing by playtest (no bot runs), right-merge smoothness (accepted as-is). Owner said they'll report bugs as they play.
- On return: check Play mode first, read `BACKLOG.md` + `DEVLOGS.md`, ask what to pick up rather than starting new features.

2026-11-08 evening: owner moved to a Mac. Everything pushed to GitHub (Deathevil96/SkyQ, main). `CLAUDE.md` in the repo root carries the project rules/decisions/status for the Mac session (this memory dir does not travel). Exhaust puffs were wired but not visually verified at hand-off.

Code layout: `Assets/SkyQueue/Scripts/Core` (pure model, asmdef SkyQueue.Core), `.../View` (views, layouts, HUD, config; asmdef SkyQueue.View), `Tests/EditMode` (fast unit tests only). Config lives on the `SkyQueue` GameObject (`GameConfig`) + `Assets/SkyQueue/Config/TrackDefinition.asset`; saved scene/asset values override code defaults, so update both.

**Why:** the owner wants an exact Bus Fever Party recreation with planes and judges by look/feel in Play mode.
**How to apply:** keep gameplay values in config/data, check Play mode is off before changing anything, verify visually with a Play-mode screenshot.
