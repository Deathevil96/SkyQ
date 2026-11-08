using System.Diagnostics;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

namespace SkyQueue
{
    /// <summary>
    /// Development logging for optimisation work. Every call is compiled out of release builds ([Conditional]), so it costs
    /// nothing in production. Lines are tagged <c>[SkyQueue][area]</c> so they can be filtered in the console or with
    /// <c>adb logcat -s Unity | findstr SkyQueue</c>. Timed steps report milliseconds and warn when they exceed a budget.
    /// </summary>
    public static class DevLog
    {
        /// <summary>High-frequency logs (each boarding row, each tap). Off by default to keep the console readable.</summary>
        public static bool Verbose;

        const string Dev = "DEVELOPMENT_BUILD", Editor = "UNITY_EDITOR";

        [Conditional(Dev), Conditional(Editor)]
        public static void Info(string area, string message) { Debug.Log("[SkyQueue][" + area + "] " + message); }

        [Conditional(Dev), Conditional(Editor)]
        public static void Detail(string area, string message) { if (Verbose) Debug.Log("[SkyQueue][" + area + "] " + message); }

        [Conditional(Dev), Conditional(Editor)]
        public static void Warn(string area, string message) { Debug.LogWarning("[SkyQueue][" + area + "] " + message); }

        /// <summary>High-resolution timestamp for <see cref="Took"/> (cheap; usable in any build).</summary>
        public static long Stamp() { return Stopwatch.GetTimestamp(); }

        public static float MsSince(long stamp) { return (Stopwatch.GetTimestamp() - stamp) * 1000f / Stopwatch.Frequency; }

        /// <summary>Logs how long a step took; warns when it went over <paramref name="budgetMs"/>.</summary>
        [Conditional(Dev), Conditional(Editor)]
        public static void Took(string area, string step, long stamp, float budgetMs = 16f)
        {
            float ms = MsSince(stamp);
            string line = "[SkyQueue][" + area + "] " + step + " " + ms.ToString("0.00") + " ms";
            if (ms > budgetMs) Debug.LogWarning(line + " (over " + budgetMs.ToString("0") + " ms budget)");
            else Debug.Log(line);
        }
    }

    /// <summary>Named profiler markers for the hot paths (visible in the Unity Profiler; near-free when not profiling).</summary>
    public static class Markers
    {
        public static readonly ProfilerMarker ModelTick = new ProfilerMarker("SkyQueue.Model.Tick");
        public static readonly ProfilerMarker RingTick = new ProfilerMarker("SkyQueue.Ring.Tick");
        public static readonly ProfilerMarker FailCheck = new ProfilerMarker("SkyQueue.Model.FailCheck");
        public static readonly ProfilerMarker Generate = new ProfilerMarker("SkyQueue.Level.Generate");
        public static readonly ProfilerMarker PassengerDraw = new ProfilerMarker("SkyQueue.Passengers.Draw");
        public static readonly ProfilerMarker Tap = new ProfilerMarker("SkyQueue.Input.Tap");
        public static readonly ProfilerMarker LevelLoad = new ProfilerMarker("SkyQueue.Level.Load");
        public static readonly ProfilerMarker Save = new ProfilerMarker("SkyQueue.Save");
    }
}
