using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyQueue
{
    /// <summary>
    /// Scene entry point. Orchestrates level lifecycle and wires the authoritative <see cref="GameModel"/> to its views
    /// (planes, passengers, HUD). Holds no puzzle rules: it forwards input as commands and mirrors model events.
    /// </summary>
    [RequireComponent(typeof(GameConfig))]
    public sealed class GameController : MonoBehaviour, DebugMenu.IHost
    {
        enum Phase { Playing, Paused, Won, Failed }

        GameConfig cfg;
        ScreenMap map;
        TrackLayout track;
        SlotLayout slots;
        HudView hud;
        TapInput taps;
        Transform planesRoot;
        SaveStore store;
        Progress progress;
        BoosterFlow boosters;
        HelicopterView helicopter;
        AudioService audioService;
        EmojiReactions emojis;
        readonly Queue<BoosterKind> introQueue = new Queue<BoosterKind>();
        float levelStartTime;
        bool saveDirty;
        Transform environment;
        EnvironmentTheme activeTheme;   // world currently built (worlds rotate by level)
        ExhaustPuffs exhaust;

        // per-level state
        GameModel model;
        BoardLayout board;
        PassengerRenderer passengers;
        float slotPlaneScale;
        int levelId = 1;
        Phase phase = Phase.Playing;
        readonly Dictionary<int, PlaneView> views = new Dictionary<int, PlaneView>();
        readonly List<PlaneView> awaitingDeparture = new List<PlaneView>();   // full planes waiting for their last walkers

        void Start()
        {
            long boot = DevLog.Stamp(), t = boot;
            Application.targetFrameRate = 60;
            cfg = GetComponent<GameConfig>();
            DevLog.Verbose = cfg.devLogVerbose;

            if (!ApplyConfig()) { enabled = false; return; }
            Art.Configure(cfg.litTemplate, cfg.unlitTemplate, cfg.primitives);
            map = new ScreenMap(FindOrCreateMainCamera(), cfg.theme.cameraBackground);
            map.Apply(cfg);
            DevLog.Info("boot", "screen " + Screen.width + "x" + Screen.height + " safe " + Screen.safeArea + " frame " + map.FramePx + " fov " + map.Cam.fieldOfView.ToString("0.0"));
            DevLog.Took("boot", "config + camera", t); t = DevLog.Stamp();
            track = new TrackLayout(map, cfg.track);
            slots = new SlotLayout(map, cfg);
            DevLog.Took("boot", "track + stand layout (ring " + track.Ring.Length.ToString("0.0") + " u, " + track.Ring.Count + " pts)", t); t = DevLog.Stamp();
            planesRoot = new GameObject("Planes").transform;
            helicopter = HelicopterView.Create(cfg.helicopter, map);   // built once, reused for every VIP airlift
            emojis = EmojiReactions.Create(cfg.emoji, map.Cam);         // pooled reaction bubbles, reused every level
            exhaust = ExhaustPuffs.Create(cfg.exhaust, map.Cam);        // pooled exhaust puffs behind taxiing planes
            DevLog.Took("boot", "helicopter build", t); t = DevLog.Stamp();

            taps = new TapInput();
            hud = HudView.Create(cfg.hud);
            DevLog.Took("boot", "HUD build", t, 30f); t = DevLog.Stamp();
            hud.SetFrame(map.FramePx);
            hud.RestartRequested += RestartLevel;
            hud.PauseRequested += () => SetPaused(true);
            hud.ResumeRequested += () => SetPaused(false);
            hud.ResultConfirmed += OnResultConfirmed;

#if UNITY_EDITOR
            gameObject.AddComponent<ReferenceOverlay>().Init(cfg, map);
#endif
            gameObject.AddComponent<PerformanceProbe>();
            DebugMenu.Attach(gameObject, this, cfg.debugMenu);   // no-op outside the editor / development builds
            store = new SaveStore(cfg.saveFileName);
            t = DevLog.Stamp();
            progress = new Progress(store.Load(), cfg.economy);
            DevLog.Took("save", "load (level " + progress.CurrentLevel + ", coins " + progress.Coins + ")", t, 10f);
            progress.CoinsChanged += hud.SetCoins;
            progress.CoinsChanged += c => RequestSave();
            progress.BoostersChanged += () => RequestSave();
            hud.SetCoins(progress.Coins);
            boosters = new BoosterFlow(progress, cfg.economy.boosters, hud);
            boosters.SlotUnlocked += i => EnvironmentBuilder.SetStandOpen(environment, i, true, activeTheme);
            boosters.Used += (kind, paid) =>
            {
                audioService.Play(Cue.Booster);
                GameEvents.Track("booster_used", "level", levelId, "kind", kind, "paid_coins", paid, "coins", progress.Coins);
            };

            t = DevLog.Stamp();
            audioService = AudioService.Create(cfg.sound);
            DevLog.Took("boot", "audio (placeholder tones generated)", t, 20f);
            UiFactory.AnyClick += PlayButtonSound;
            hud.SettingToggled += ToggleSetting;
            hud.IntroClosed += ShowNextIntro;
            ApplySettings();
            DevLog.Took("boot", "start-up total before first level", boot, 200f);
            LoadLevel(Mathf.Clamp(progress.CurrentLevel, 1, LevelLibrary.MaxLevel));
        }

        // settings ----------------------------------------------------------------------------------------------------
        void PlayButtonSound() { if (audioService != null) audioService.Play(Cue.Button); }

        void ApplySettings()
        {
            var st = progress.Data.settings;
            audioService.Enabled = st.sound;
            Haptics.Enabled = st.haptics;
            cfg.reduceMotion = st.reduceMotion;
            hud.SetSettings(st.sound, st.haptics, st.reduceMotion);
        }

        void ToggleSetting(int which)
        {
            var st = progress.Data.settings;
            if (which == 0) st.sound = !st.sound;
            else if (which == 1) st.haptics = !st.haptics;
            else st.reduceMotion = !st.reduceMotion;
            RequestSave();
            ApplySettings();
        }

        // booster unlock sequence -------------------------------------------------------------------------------------
        /// <summary>Queues a popup for every booster that unlocks at or before this level and has not been introduced.</summary>
        void QueueBoosterIntros(int level)
        {
            introQueue.Clear();
            for (int i = 0; i < BoosterConfig.KindCount; i++)
                if (progress.TryIntroduceBooster((BoosterKind)i, level)) introQueue.Enqueue((BoosterKind)i);
            if (introQueue.Count > 0) { RequestSave(); ShowNextIntro(); }
        }

        void ShowNextIntro()
        {
            if (introQueue.Count == 0)
            {
                if (phase == Phase.Paused && !hud.IntroVisible) { phase = Phase.Playing; Time.timeScale = 1f; }
                return;
            }
            var kind = introQueue.Dequeue();
            phase = Phase.Paused; Time.timeScale = 0f;   // the level waits while the popup is up
            audioService.Play(Cue.Unlock);
            hud.ShowIntro(BoosterFlow.NameOf(kind), BoosterFlow.DescriptionOf(kind), cfg.economy.boosters.GiftOf(kind));
            GameEvents.Track("booster_unlocked", "level", levelId, "kind", kind);
        }

        static Camera FindOrCreateMainCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.AddComponent<AudioListener>();
            return go.AddComponent<Camera>();
        }

        bool ApplyConfig()
        {
            if (cfg.track == null) { Debug.LogError("[SkyQueue] GameConfig.track is not assigned (create a TrackDefinition asset)."); return false; }
            if (cfg.litTemplate == null || cfg.unlitTemplate == null) { Debug.LogError("[SkyQueue] GameConfig material templates are not assigned."); return false; }
            if (!cfg.primitives.IsComplete) { Debug.LogError("[SkyQueue] GameConfig primitive meshes are not assigned."); return false; }
            if (!Rules.Configure(cfg.rules)) return false;
            Palette.Configure(cfg.palette);
            return true;
        }

        /// <summary>Switches the scenery to the level's world (only rebuilds when the world actually changes).</summary>
        void ApplyWorld(int level)
        {
            var th = cfg.ThemeFor(level);
            if (th == activeTheme && environment != null) return;
            long t = DevLog.Stamp();
            if (environment != null) Destroy(environment.gameObject);
            activeTheme = th;
            environment = EnvironmentBuilder.Build(map, slots, track, cfg, th);
            map.Cam.backgroundColor = th.cameraBackground;
            ConfigureLighting(th);
            DevLog.Took("world", "built '" + th.name + "' (world " + cfg.WorldIndexFor(level) + ", " + environment.childCount + " objects) for level " + level, t, 50f);
            GameEvents.Track("world_enter", "level", level, "world", th.name);
        }

        void ConfigureLighting(EnvironmentTheme th)
        {
            var light = FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(th.lightEuler);
                light.intensity = th.lightIntensity; light.shadows = LightShadows.Soft; light.shadowStrength = th.shadowStrength;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = th.ambient;
        }

        // level lifecycle -------------------------------------------------------------------------------------------
        void RestartLevel() { SetPaused(false); LoadLevel(levelId); }

        void OnResultConfirmed()
        {
            if (phase == Phase.Won) levelId = Mathf.Min(levelId + 1, LevelLibrary.MaxLevel);
            LoadLevel(levelId);
        }

        void SetPaused(bool paused)
        {
            if (paused && phase != Phase.Playing) return;
            phase = paused ? Phase.Paused : Phase.Playing;
            hud.ShowPause(paused);
            Time.timeScale = paused ? 0f : 1f;
        }

        void Teardown()
        {
            Time.timeScale = 1f;
            if (helicopter != null) helicopter.Abort();
            if (exhaust != null) exhaust.Clear();
            foreach (var v in views.Values) if (v != null) Destroy(v.gameObject);
            views.Clear();
            awaitingDeparture.Clear();
            if (passengers != null) Destroy(passengers);
            model = null;
        }

        public void LoadLevel(int id)
        {
            using (Markers.LevelLoad.Auto()) LoadLevelInner(id);
        }

        void LoadLevelInner(int id)
        {
            long t0 = DevLog.Stamp(), t = t0;
            Teardown();
            DevLog.Took("level", "teardown", t, 10f); t = DevLog.Stamp();
            levelId = id;
            ApplyWorld(id);
            progress.SetCurrentLevel(id);
            RequestSave();
            var data = LevelLibrary.Get(id);
            DevLog.Took("level", "generate level " + id + " (" + data.Difficulty + ", " + data.Cols + "x" + data.Rows + ", " + data.Planes.Count + " planes, "
                                 + data.ColorCount + " colours, " + data.PassengerCount + " passengers in " + data.PassengerBands.Count + " bands)", t, 20f);
            t = DevLog.Stamp();
            phase = Phase.Playing;
            hud.HideAllPanels();
            hud.SetLevel(id);

            board = new BoardLayout(map, cfg, data.Cols, data.Rows);
            slotPlaneScale = slots.PlaneScale(board.Cell);

            var ring = new RingSettings
            {
                Spacing = cfg.passengerSpacing, Length = track.Ring.Length, BoardS = track.BoardS, Speed = cfg.loopSpeed,
                MergeS = track.MergeS, FeedLen = track.Feeds[0].Length, FeedHeadStart = track.FeedHeadStart, HoldGap = cfg.feedHoldGap, StartFull = cfg.ringStartsFull
            };
            model = new GameModel(data, ring);
            model.OnPlaneLaunched += HandleLaunched;
            model.OnRowBoarded += HandleRowBoarded;
            model.OnPlaneFull += HandlePlaneFull;
            model.OnPlaneRecolored += RecolorPlane;
            boosters.Bind(model);
            for (int i = 0; i < model.Slots.Length; i++)
                if (model.Slots[i].Kind == SlotKind.Purchasable) EnvironmentBuilder.SetStandOpen(environment, i, model.Slots[i].Unlocked, activeTheme);
            var difficulty = data.Difficulty;
            model.OnLevelWon += () => StartCoroutine(ShowResult(true, difficulty));
            model.OnLevelFailed += () => StartCoroutine(ShowResult(false, difficulty));

            passengers = gameObject.AddComponent<PassengerRenderer>();
            passengers.Bind(model.Ring, track.Ring, track.Feeds, cfg, map.Cam, track.LaneWidth);
            emojis.Bind(model.Ring, track.Feeds);

            foreach (var p in data.Planes)
            {
                var v = PlaneView.Create(p, board.Cell, planesRoot, cfg);
                v.SetPose(board.PlaneCenter(p), board.PlaneRotation(p.Dir));
                views[p.Id] = v;
            }

            DevLog.Took("level", "spawn " + data.Planes.Count + " plane views + passenger renderer", t, 30f);
            DevLog.Took("level", "load total", t0, 60f);
            levelStartTime = Time.time;
            GameEvents.Track("level_start", "level", id, "difficulty", data.Difficulty, "planes", data.Planes.Count, "colors", data.ColorCount);
            QueueBoosterIntros(id);
            if (data.Difficulty == Difficulty.SuperHard) hud.Banner("SUPER HARD");
            else if (data.Difficulty == Difficulty.Hard) hud.Banner("HARD");
        }

        System.Collections.IEnumerator ShowResult(bool won, Difficulty difficulty)
        {
            phase = won ? Phase.Won : Phase.Failed;
            var reward = new WinReward();
            float seconds = Time.time - levelStartTime;
            DevLog.Info("level", (won ? "WON" : "FAILED") + " level " + levelId + " in " + seconds.ToString("0.0") + " s, " + model.Moves + " moves, ring rows " + model.Ring.RingRows);
            if (won)
            {
                reward = progress.RecordWin(levelId, difficulty);
                progress.SetCurrentLevel(Mathf.Min(levelId + 1, LevelLibrary.MaxLevel));   // resume on the next level
                RequestSave();
                GameEvents.Track("level_win", "level", levelId, "moves", model.Moves, "seconds", seconds, "coins_earned", reward.Total, "coins", progress.Coins);
            }
            else GameEvents.Track("level_fail", "level", levelId, "moves", model.Moves, "seconds", seconds, "ring_rows", model.Ring.RingRows);
            yield return new WaitForSeconds(won ? cfg.feel.winResultDelay : cfg.feel.failResultDelay);
            audioService.Play(won ? Cue.Win : Cue.Fail);
            if (won && reward.Total > 0) audioService.Play(Cue.Coins);
            if (!won) Haptics.Pulse();
            hud.ShowResult(won, levelId, reward);
        }

        // model events ----------------------------------------------------------------------------------------------
        void HandleLaunched(PlaneData p, int slot)
        {
            var view = views[p.Id];
            Vector3 stand = slots.Positions[slot];
            audioService.Play(Cue.Launch);
            DevLog.Info("plane", "launch " + p.Id + " (" + p.Size + ", colour " + p.Color + ", " + p.Capacity + " seats) -> stand " + slot + " [" + model.Slots[slot].Kind + "]");
            if (model.Slots[slot].Kind == SlotKind.Vip)
            {
                // VIP: a helicopter airlifts the plane to the VIP stand (planes do not fly over the board)
                helicopter.Carry(view.transform, stand, slotPlaneScale, slots.Rotations[slot], () =>
                {
                    model.NotifyArrived(p);
                    view.ShowLabel(SeatLabelPos(slot));
                    view.SetRemaining(p.Remaining);
                });
                return;
            }
            var route = PlaneRoutes.Launch(p, view.transform.position, board, model.Board.CellsToEdge(p), slots, slot, cfg.exitMarginCells, cfg.planeBodyHalfCells, cfg.planeTurnRadiusCells);
            if (cfg.logRouteChecks && Debug.isDebugBuild) LogRouteCheck(p, route);
            view.Follow(route, cfg.planeSpeed, slotPlaneScale, cfg.planeTurnDegPerSec, () =>
            {
                model.NotifyArrived(p);
                view.ShowLabel(SeatLabelPos(slot));
                view.SetRemaining(p.Remaining);
            }, 0f, slots.Rotations[slot]);
            exhaust.Follow(view);   // puffs from the tail while it taxis to the stand
        }

        /// <summary>Seats-left badge: just below the tilted pad, on its axis.</summary>
        Vector3 SeatLabelPos(int slot)
        {
            return slots.Positions[slot] - slots.Forward[slot] * slots.LabelOffset + Vector3.up * cfg.feel.labelHeight;
        }

        /// <summary>Debug: walks the launch route (centre and both body edges) in screen space and reports any booster overlap.</summary>
        void LogRouteCheck(PlaneData p, Polyline route)
        {
            float body = cfg.planeBodyHalfCells * board.Cell, step = board.Cell * 0.25f;
            Vector2 floorScreen = map.Cam.WorldToScreenPoint(new Vector3(0f, 0f, slots.FloorZ));
            float lowest = float.MaxValue;
            if (float.IsNaN(route.Length) || route.Length <= 0f)
            {
                Debug.LogError("[SkyQueue][route] plane " + p.Id + " (" + p.Dir + ") has an invalid route (length " + route.Length + ")");
                return;
            }
            for (float s = 0f; s <= route.Length; s += step)
            {
                Vector3 c = route.Point(s), t = route.Tangent(s).normalized;
                Vector3 side = new Vector3(-t.z, 0f, t.x) * body;
                Vector3 nose = t * (p.Length * 0.5f * board.Cell);
                for (int e = 0; e < 4; e++)
                {
                    Vector3 w = e == 0 ? c : e == 1 ? c + side : e == 2 ? c - side : c + nose;   // centre, both sides, nose
                    Vector2 sp = map.Cam.WorldToScreenPoint(w);
                    lowest = Mathf.Min(lowest, sp.y);
                    int hit = hud.BoosterAt(sp);
                    if (hit >= 0)
                    {
                        Debug.LogWarning("[SkyQueue][route] plane " + p.Id + " (" + p.Dir + ", " + p.Size + ") passes over booster " + hit + " at screen " + sp + " (route s=" + s.ToString("0.00") + "/" + route.Length.ToString("0.00") + ")");
                        return;
                    }
                }
            }
            Debug.Log("[SkyQueue][route] plane " + p.Id + " (" + p.Dir + ") clear: lowest screen y " + lowest.ToString("0") + ", floor line y " + floorScreen.y.ToString("0") + ", screen " + Screen.width + "x" + Screen.height);
        }

        void HandleRowBoarded(PlaneData p, int color, float ringS)
        {
            audioService.Play(Cue.Board);
            DevLog.Detail("board", "row of colour " + color + " -> plane " + p.Id + " (" + p.Remaining + " seats left)");
            var view = views[p.Id];
            Vector3 stand = slots.Positions[p.Slot];
            for (int lane = 0; lane < Rules.RowSize; lane++)
            {
                view.PendingWalkers++;
                Vector3 target = stand + new Vector3(Random.Range(-cfg.feel.walkerJitterX, cfg.feel.walkerJitterX), 0f, Random.Range(-cfg.feel.walkerJitterZ, cfg.feel.walkerJitterZ));
                float delay = lane * cfg.feel.walkerStagger + Random.Range(0f, cfg.feel.walkerStagger * 0.5f);   // one after another
                float duration = cfg.walkerTime * (1f + Random.Range(-cfg.feel.walkerTimeJitter, cfg.feel.walkerTimeJitter));
                passengers.SpawnWalker(lane, ringS, target, color, view, delay, duration);
            }
            view.SetRemaining(p.Remaining);
        }

        /// <summary>Jumble changed a plane's colour: rebuild its placeholder model in place.</summary>
        void RecolorPlane(PlaneData p)
        {
            PlaneView old;
            if (!views.TryGetValue(p.Id, out old) || old == null) return;
            var v = PlaneView.Create(p, board.Cell, planesRoot, cfg);
            v.SetPose(old.transform.position, old.transform.rotation);
            views[p.Id] = v;
            Destroy(old.gameObject);
        }

        void HandlePlaneFull(PlaneData p)
        {
            audioService.Play(Cue.PlaneFull);
            DevLog.Info("plane", "full " + p.Id + " at stand " + p.Slot + "; ring rows " + model.Ring.RingRows + ", passengers left " + model.Ring.Remaining);
            awaitingDeparture.Add(views[p.Id]);
        }

        void BeginDeparture(PlaneView view)
        {
            var p = view.Data;
            Vector3 stand = slots.Positions[p.Slot];
            model.BeginDeparture(p);
            audioService.Play(Cue.Depart);
            view.HideLabel();
            view.Follow(PlaneRoutes.Depart(stand, map, slots, cfg.feel), cfg.planeSpeed * cfg.departSpeedFactor, cfg.departScale, cfg.departTurnDegPerSec, () =>
            {
                model.NotifyDeparted(p);
                if (view != null) Destroy(view.gameObject);
            }, cfg.departLift);
            if (cfg.exhaust.onDeparture) exhaust.Follow(view);
        }

        /// <summary>Marks progress dirty; it is written once at the end of the frame (several changes in one frame = one write).</summary>
        void RequestSave() { saveDirty = true; }

        void FlushSave()
        {
            if (!saveDirty || store == null) return;
            saveDirty = false;
            store.Save(progress.Data);
        }

        void LateUpdate() { FlushSave(); }

        // debug menu (editor + development builds only; see DebugMenu) ------------------------------------------------
        public int CurrentLevel { get { return levelId; } }

        public void DebugGrantCoins(int amount)
        {
            progress.GrantCoins(amount);
            DevLog.Info("debug", "+" + amount + " coins -> " + progress.Coins);
        }

        public void DebugGrantBoosters(int each)
        {
            for (int i = 0; i < BoosterConfig.KindCount; i++) progress.GrantBooster((BoosterKind)i, each);
            DevLog.Info("debug", "+" + each + " of every booster");
        }

        public void DebugUnlockAllBoosters()
        {
            boosters.DebugUnlockAll = true;      // this session only
            progress.IntroduceAllBoosters();     // no intro popups later
            boosters.Refresh();
            RequestSave();
            DevLog.Info("debug", "all boosters unlocked for this session");
        }

        public void DebugLoadLevel(int level)
        {
            SetPaused(false);
            LoadLevel(Mathf.Clamp(level, 1, LevelLibrary.MaxLevel));
            DevLog.Info("debug", "jumped to level " + levelId);
        }

        public void DebugFinishLevel(bool won)
        {
            if (model == null || phase != Phase.Playing) return;
            DevLog.Info("debug", "forced " + (won ? "win" : "fail") + " on level " + levelId);
            StartCoroutine(ShowResult(won, model.Level.Difficulty));
        }

        public void DebugResetSave()
        {
            saveDirty = false;
            store.Save(Progress.New(cfg.economy));
            DevLog.Info("debug", "save reset; reloading");
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
        }

        void OnApplicationPause(bool paused) { if (paused) FlushSave(); }

        void OnApplicationQuit() { FlushSave(); }

        void OnDestroy() { if (taps != null) taps.Dispose(); UiFactory.AnyClick -= PlayButtonSound; }

        // frame loop ------------------------------------------------------------------------------------------------
        void Update()
        {
            if (map.RefitIfScreenChanged(cfg))
            {
                hud.SetFrame(map.FramePx);
                slots = new SlotLayout(map, cfg);   // rails and floor line follow the new screen edges
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) RestartLevel();
                if (kb.nKey.wasPressedThisFrame) { levelId = levelId >= LevelLibrary.MaxLevel ? 1 : levelId + 1; LoadLevel(levelId); }
            }
            if (model == null || phase != Phase.Playing) { if (taps != null) taps.Clear(); return; }

            model.Tick(Time.deltaTime);

            for (int i = awaitingDeparture.Count - 1; i >= 0; i--)
            {
                var v = awaitingDeparture[i];
                if (v != null && v.PendingWalkers > 0) continue;   // last passengers still walking in
                awaitingDeparture.RemoveAt(i);
                if (v != null) BeginDeparture(v);
            }

            Vector2 tap;
            while (taps.TryDequeue(out tap)) HandleTap(tap);
        }

        void HandleTap(Vector2 screen)
        {
            using (Markers.Tap.Auto()) HandleTapInner(screen);
        }

        void HandleTapInner(Vector2 screen)
        {
            if (DebugMenu.Open || hud.IsOverUi(screen)) return;   // the HUD (or the debug panel) owns this touch
            Ray ray = map.Cam.ScreenPointToRay(screen);

            // a closed "+" stand: offer it for purchase
            if (Mathf.Abs(ray.direction.y) > 1e-5f)
            {
                Vector3 ground = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                int stand = slots.StandAt(ground);
                if (stand >= 0 && model.Slots[stand].Kind == SlotKind.Purchasable && !model.Slots[stand].Unlocked) { boosters.OnLockedStandTapped(stand); return; }
            }

            int col, row;
            if (!board.TryPickCell(ray, out col, out row)) return;
            int id = model.Board.At(col, row);
            if (id < 0) return;
            if (boosters.OnPlaneTapped(id)) return;   // VIP booster armed: this tap picks its plane

            int slot;
            var result = model.TryLaunch(id, out slot);
            DevLog.Detail("tap", "plane " + id + " -> " + result + (result == LaunchResult.Success ? " (stand " + slot + ")" : ""));
            if (result == LaunchResult.Success) return;
            var plane = model.GetPlane(id);
            PlaneView view;
            if (plane != null && views.TryGetValue(id, out view)) view.PlayShake(board.DirWorld(plane.Dir));
            audioService.Play(Cue.Blocked);
            Haptics.Pulse();
            if (result == LaunchResult.NoSlot) hud.Toast("Boarding stands full");
        }
    }
}
