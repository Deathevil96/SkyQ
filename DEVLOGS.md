# SkyQueue dev logs and profiling

All of this is **compiled out of release builds** (`[Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]`), so it costs nothing in production. Use it when optimising.

## Reading the logs
- Editor: Console, filter `[SkyQueue]`.
- Phone (development build): `adb logcat -s Unity | findstr SkyQueue`.
- Timed steps print milliseconds; a step over its budget is logged as a **warning** with `(over N ms budget)`.
- `GameConfig > Debug > devLogVerbose` adds high-frequency lines (every tap, every boarding row, every emoji).

| Tag | What it tells you |
|---|---|
| `[boot]` | Screen/safe area/frame/FOV, then the cost of each start-up step (config + camera, track, environment build, helicopter, HUD, audio) and the start-up total. |
| `[save]` | Save load time and every save write (writes are batched: at most one per frame). Backup recovery is reported. |
| `[level]` | Teardown, level generation (size, planes, colours, passengers, bands), view spawning, total load time, WON / FAILED with time, moves and ring rows. |
| `[gen]` | Generator needed extra seeds, or placed fewer planes than the spec (board too dense), or had to shrink a spec. |
| `[plane]` | Every launch (size, colour, seats, stand kind) and every plane that fills up (ring rows, passengers left). |
| `[route]` | For each launch route: clear (lowest screen point vs the floor line) or a warning naming the booster it passes over. Toggle: `logRouteChecks`. |
| `[model]` | Why a level was lost (board jammed vs ring full with nothing able to board). |
| `[booster]` / `[vip]` | Booster requests, executions (owned use vs coins paid), helicopter airlift timing. |
| `[event]` | Analytics events (JSON lines; also appended to `persistentDataPath/analytics.jsonl`). |
| `[perf]` | Every 5 s: fps, avg / p95 / worst frame ms, managed heap and its change, GC collections, spikes, passengers drawn, walkers. Single frames over 40 ms are logged as `spike` immediately. |

## Debug menu (cheats)
Editor and development builds only (`GameConfig > Debug > debugMenu`; stripped from release builds).
- Open: **DBG** button in the bottom-left corner (works with touch on the phone), or **F1** in the editor.
- +1000 coins · +5 of every booster · unlock all boosters (this session) · win / lose the level · previous / next level · +10 levels (next world) · go to level N · reset save (tap twice; reloads the scene).
- While the panel is open, board taps are ignored. Every action logs a `[debug]` line.

## Profiler markers
Open the Unity Profiler (also works attached to the phone) and look for:
`SkyQueue.Model.Tick`, `SkyQueue.Ring.Tick`, `SkyQueue.Model.FailCheck`, `SkyQueue.Passengers.Draw`, `SkyQueue.Input.Tap`, `SkyQueue.Level.Load`, `SkyQueue.Level.Generate`, `SkyQueue.Save`.

## Baseline (editor, 2026-11-08)
- Start-up before first level: ~62 ms (HUD build ~32 ms is the largest step).
- Level load: 9-18 ms including a 45-plane 14x14 Super Hard board.
- Gameplay: 0 GC collections over a 15 s session of level 20 with 300 passengers drawn.
- Phone numbers: still to be measured (see BACKLOG "Device").
