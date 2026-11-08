using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    public struct LevelSpec
    {
        public int Id, Seed, Cols, Rows, PlaneCount, Colors;
        public float SmallW, MediumW, LargeW;
        public Difficulty Difficulty;
    }

    /// <summary>
    /// Builds a solvable jam by reverse construction: planes are inserted in reverse removal order and each insertion
    /// requires a clear exit ray at that moment, so the removal order is a guaranteed solution. Passenger totals per
    /// colour always equal the sum of capacities of planes of that colour.
    /// </summary>
    public static class LevelGenerator
    {
        public static LevelData Generate(LevelSpec s)
        {
            using (Markers.Generate.Auto())
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var lvl = Build(s, s.Seed + attempt * 7919);
                var order = lvl.Planes.Count > 0 ? GameModel.SolveOrder(lvl) : null;
                if (order != null)
                {
                    lvl.Solution = order;   // a verified removal order (hints / tests)
                    if (attempt > 0) DevLog.Info("gen", "level " + s.Id + " needed " + (attempt + 1) + " seeds");
                    if (lvl.Planes.Count < s.PlaneCount) DevLog.Info("gen", "level " + s.Id + " placed " + lvl.Planes.Count + "/" + s.PlaneCount + " planes (board too dense)");
                    return lvl;
                }
            }
            throw new Exception("Could not generate solvable level " + s.Id);
        }

        static LevelData Build(LevelSpec s, int seed)
        {
            var rng = new System.Random(seed);
            var board = new TrafficBoard(s.Cols, s.Rows);
            var placed = new List<PlaneData>();
            int fails = 0;
            float tw = s.SmallW + s.MediumW + s.LargeW;
            while (placed.Count < s.PlaneCount && fails < 6000)
            {
                float r = (float)rng.NextDouble() * tw;
                PlaneSize size = r < s.SmallW ? PlaneSize.Small : (r < s.SmallW + s.MediumW ? PlaneSize.Medium : PlaneSize.Large);
                Dir dir = (Dir)rng.Next(4);
                var p = new PlaneData { Size = size, Dir = dir };
                int maxC = s.Cols - p.W, maxR = s.Rows - p.H;
                if (maxC < 0 || maxR < 0) { fails++; continue; }
                p.Col = rng.Next(maxC + 1);
                p.Row = rng.Next(maxR + 1);
                if (board.CanPlace(p) && board.CanExit(p))
                {
                    p.Id = placed.Count;
                    board.Place(p);
                    placed.Add(p);
                }
                else fails++;
            }

            // Colours: guarantee every colour is used, then random.
            for (int i = 0; i < placed.Count; i++)
                placed[i].Color = i < s.Colors ? i : rng.Next(s.Colors);
            Shuffle(placed, rng, true);

            var lvl = new LevelData
            {
                Id = s.Id, Difficulty = s.Difficulty, Cols = s.Cols, Rows = s.Rows,
                Planes = placed, ColorCount = s.Colors
            };

            // Passenger totals per colour -> bands of 6..12 -> shuffled ring order.
            var totals = new int[s.Colors];
            foreach (var p in placed) totals[p.Color] += p.Capacity;
            var bands = new List<int[]>();   // [color, size]
            for (int c = 0; c < s.Colors; c++)
            {
                int rem = totals[c];
                while (rem > 0)
                {
                    int size = Math.Min(rem, Rules.RowSize * rng.Next(2, 5));
                    bands.Add(new[] { c, size });
                    rem -= size;
                }
            }
            Shuffle(bands, rng, false);
            // Avoid two adjacent bands of one colour where possible (readability).
            for (int i = 1; i < bands.Count; i++)
            {
                if (bands[i][0] == bands[i - 1][0])
                {
                    for (int j = i + 1; j < bands.Count; j++)
                        if (bands[j][0] != bands[i - 1][0]) { var t = bands[i]; bands[i] = bands[j]; bands[j] = t; break; }
                }
            }
            lvl.PassengerBands = bands;
            return lvl;
        }

        // Shuffles list; when keepIds is true the elements keep their Id/geometry (only order changes).
        static void Shuffle<T>(List<T> list, System.Random rng, bool keepIds)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }
    }

    [Serializable]
    public sealed class LevelSpecEntry
    {
        public int planes, colors, cols, rows;
        public int small, medium, large;       // plane-size weights
        public int seed;
        [Tooltip("Optional handcrafted override: Normal, Hard or SuperHard. Empty = the every-5 / every-10 cadence.")]
        public string difficulty;
        [Tooltip("Designer note (documentation only).")]
        public string note;
    }

    /// <summary>
    /// One stretch of the procedural level range (levels past the authored table, up to <see cref="LevelTable.maxLevel"/>).
    /// Plane count ramps linearly from planesFrom to planesTo across the band; Hard / Super Hard levels add extra planes
    /// and colours. Every value is data, so the 1,000-level curve is tuned in JSON, not code.
    /// </summary>
    [Serializable]
    public sealed class ProceduralBand
    {
        public int fromLevel;
        public int planesFrom, planesTo;
        public int colors;
        public int cols, rows;
        public int small, medium, large;
        public int hardExtraPlanes = 4, hardExtraColors;
        public int superHardExtraPlanes = 8, superHardExtraColors = 1;
    }

    [Serializable]
    public sealed class LevelTable
    {
        public int hardEvery = 5;
        public int superHardEvery = 10;
        public int maxLevel = 1000;
        public int proceduralSeedBase = 20000;
        public int proceduralSeedStride = 7919;
        public LevelSpecEntry[] levels = new LevelSpecEntry[0];
        public ProceduralBand[] procedural = new ProceduralBand[0];
    }

    /// <summary>
    /// Level set loaded from data (<c>Resources/SkyQueue/levels.json</c>). Levels 1..N come from the authored table; levels
    /// beyond it up to <c>maxLevel</c> are generated deterministically from the procedural bands (same id = same level on
    /// every device). Every level is produced by the validated reverse-construction generator, so it is always solvable.
    /// </summary>
    public static class LevelLibrary
    {
        const string ResourcePath = "SkyQueue/levels";
        const int ShrinkAttempts = 6;
        static LevelTable table;

        /// <summary>Authored levels in the table.</summary>
        public static int Count { get { Ensure(); return table.levels.Length; } }

        /// <summary>Highest playable level (authored + procedural).</summary>
        public static int MaxLevel { get { Ensure(); return Mathf.Max(table.levels.Length, table.procedural.Length > 0 ? table.maxLevel : table.levels.Length); } }

        /// <summary>Loads a table from JSON (used by the game, tests and tools).</summary>
        public static void Initialize(string json)
        {
            var parsed = JsonUtility.FromJson<LevelTable>(json);
            if (parsed == null || parsed.levels == null || parsed.levels.Length == 0)
                throw new InvalidOperationException("Level table is empty or malformed.");
            if (parsed.procedural == null) parsed.procedural = new ProceduralBand[0];
            Array.Sort(parsed.procedural, (x, y) => x.fromLevel.CompareTo(y.fromLevel));
            table = parsed;
        }

        static void Ensure()
        {
            if (table != null) return;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing level table at Resources/" + ResourcePath + ".json");
            Initialize(asset.text);
        }

        public static Difficulty DifficultyFor(int id)
        {
            Ensure();
            if (id >= 1 && id <= table.levels.Length)
            {
                string o = table.levels[id - 1].difficulty;
                if (!string.IsNullOrEmpty(o))
                {
                    Difficulty d;
                    if (TryParse(o, out d)) return d;
                }
            }
            if (table.superHardEvery > 0 && id % table.superHardEvery == 0) return Difficulty.SuperHard;
            if (table.hardEvery > 0 && id % table.hardEvery == 0) return Difficulty.Hard;
            return Difficulty.Normal;
        }

        static bool TryParse(string s, out Difficulty d)
        {
            try { d = (Difficulty)Enum.Parse(typeof(Difficulty), s, true); return true; }
            catch (ArgumentException) { d = Difficulty.Normal; return false; }
        }

        public static LevelSpec SpecFor(int id)
        {
            Ensure();
            id = Mathf.Clamp(id, 1, MaxLevel);
            var difficulty = DifficultyFor(id);
            if (id <= table.levels.Length)
            {
                var e = table.levels[id - 1];
                return new LevelSpec
                {
                    Id = id, Seed = e.seed, PlaneCount = e.planes, Colors = e.colors, Cols = e.cols, Rows = e.rows,
                    SmallW = e.small, MediumW = e.medium, LargeW = e.large, Difficulty = difficulty
                };
            }
            return ProceduralSpec(id, difficulty);
        }

        static LevelSpec ProceduralSpec(int id, Difficulty difficulty)
        {
            ProceduralBand band = table.procedural[0];
            int next = table.maxLevel + 1;
            for (int i = 0; i < table.procedural.Length; i++)
            {
                if (table.procedural[i].fromLevel > id) { next = table.procedural[i].fromLevel; break; }
                band = table.procedural[i];
            }
            float t = next - 1 > band.fromLevel ? Mathf.Clamp01((id - band.fromLevel) / (float)(next - 1 - band.fromLevel)) : 0f;
            int planes = Mathf.RoundToInt(Mathf.Lerp(band.planesFrom, band.planesTo, t));
            int colors = band.colors;
            if (difficulty == Difficulty.Hard) { planes += band.hardExtraPlanes; colors += band.hardExtraColors; }
            else if (difficulty == Difficulty.SuperHard) { planes += band.superHardExtraPlanes; colors += band.superHardExtraColors; }
            return new LevelSpec
            {
                Id = id, Seed = table.proceduralSeedBase + id * table.proceduralSeedStride, PlaneCount = planes,
                Colors = Mathf.Clamp(colors, 1, Palette.Count), Cols = band.cols, Rows = band.rows,
                SmallW = band.small, MediumW = band.medium, LargeW = band.large, Difficulty = difficulty
            };
        }

        /// <summary>Builds level <paramref name="id"/>. If a spec is too dense to generate, it retries with fewer planes.</summary>
        public static LevelData Get(int id)
        {
            var spec = SpecFor(id);
            for (int attempt = 0; ; attempt++)
            {
                try { return LevelGenerator.Generate(spec); }
                catch (Exception)
                {
                    if (attempt >= ShrinkAttempts || spec.PlaneCount <= 1) throw;
                    spec.PlaneCount = Mathf.Max(1, Mathf.RoundToInt(spec.PlaneCount * 0.9f));
                    DevLog.Warn("gen", "level " + id + " could not be generated; retrying with " + spec.PlaneCount + " planes");
                }
            }
        }
    }
}
