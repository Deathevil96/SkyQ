using UnityEngine;
using UnityEngine.Profiling;

namespace SkyQueue
{
    /// <summary>
    /// Development-build probe: logs frame time statistics and managed allocation per frame every few seconds so
    /// performance can be checked over adb logcat without a profiler. Does nothing in release builds.
    /// </summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        [SerializeField] float windowSeconds = 5f;
        [SerializeField] float spikeMs = 40f;           // a single frame slower than this is logged on its own
        int gcAtWindowStart, spikes;
        float[] frameMs = new float[512];
        int count;
        float elapsed;
        long allocAtWindowStart;

        void Awake()
        {
            if (!Debug.isDebugBuild) { enabled = false; return; }
            allocAtWindowStart = Profiler.GetMonoUsedSizeLong();
            gcAtWindowStart = System.GC.CollectionCount(0);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt * 1000f > spikeMs && Time.frameCount > 5)
            {
                spikes++;
                Debug.LogWarning(string.Format("[SkyQueue][perf] spike {0:F1} ms at frame {1} (gc gen0 total {2}, passengers drawn {3})",
                    dt * 1000f, Time.frameCount, System.GC.CollectionCount(0), PassengerRenderer.LastInstanceCount));
            }
            if (count == frameMs.Length) System.Array.Resize(ref frameMs, count * 2);
            frameMs[count++] = dt * 1000f;
            elapsed += dt;
            if (elapsed < windowSeconds) return;

            System.Array.Sort(frameMs, 0, count);
            float avg = elapsed / count * 1000f;
            float p95 = frameMs[Mathf.Min(count - 1, Mathf.FloorToInt(count * 0.95f))];
            float worst = frameMs[count - 1];
            long mono = Profiler.GetMonoUsedSizeLong();
            int gc = System.GC.CollectionCount(0);
            Debug.Log(string.Format("[SkyQueue][perf] {0:F1} fps  avg {1:F1} ms  p95 {2:F1} ms  worst {3:F1} ms  mono {4:F1} MB (delta {5:+0.00;-0.00} MB)  gc {6}  spikes {7}  passengers {8}  walkers {9}  frames {10}",
                count / elapsed, avg, p95, worst, mono / 1048576f, (mono - allocAtWindowStart) / 1048576f, gc - gcAtWindowStart, spikes,
                PassengerRenderer.LastInstanceCount, PassengerRenderer.LastWalkerCount, count));
            allocAtWindowStart = mono; gcAtWindowStart = gc; spikes = 0; count = 0; elapsed = 0f;
        }
    }
}
