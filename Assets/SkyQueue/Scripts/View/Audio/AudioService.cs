using System;
using UnityEngine;

namespace SkyQueue
{
    public enum Cue { Button, Launch, Blocked, Board, PlaneFull, Depart, Win, Fail, Booster, Coins, Unlock }

    /// <summary>
    /// One sound per game cue. Assign real clips in the config; any empty slot plays a short generated placeholder tone so
    /// the game is never silent during development. Volumes and tones are data, not code.
    /// </summary>
    [Serializable]
    public sealed class SoundSettings
    {
        [Range(0f, 1f)] public float masterVolume = 0.6f;
        [Tooltip("Optional clips indexed by Cue (Button, Launch, Blocked, Board, PlaneFull, Depart, Win, Fail, Booster, Coins, Unlock).")]
        public AudioClip[] clips = new AudioClip[11];
        [Tooltip("Placeholder tone per cue: x = frequency Hz, y = length seconds (used when the clip slot is empty).")]
        public Vector2[] placeholderTones =
        {
            new Vector2(880f, 0.04f), new Vector2(520f, 0.12f), new Vector2(160f, 0.15f), new Vector2(1250f, 0.03f),
            new Vector2(660f, 0.18f), new Vector2(330f, 0.35f), new Vector2(784f, 0.45f), new Vector2(196f, 0.5f),
            new Vector2(990f, 0.2f), new Vector2(1320f, 0.12f), new Vector2(698f, 0.3f)
        };
        [Tooltip("Minimum seconds between two plays of the same cue (keeps rapid boarding from becoming noise).")]
        public float repeatGuard = 0.05f;
    }

    public sealed class AudioService : MonoBehaviour
    {
        SoundSettings s;
        AudioSource source;
        AudioClip[] resolved;
        float[] lastPlayed;
        public bool Enabled = true;

        public static AudioService Create(SoundSettings settings)
        {
            var go = new GameObject("Audio");
            var a = go.AddComponent<AudioService>();
            a.s = settings;
            a.source = go.AddComponent<AudioSource>();
            a.source.playOnAwake = false;
            int n = Enum.GetValues(typeof(Cue)).Length;
            a.resolved = new AudioClip[n];
            a.lastPlayed = new float[n];
            for (int i = 0; i < n; i++)
            {
                AudioClip clip = settings.clips != null && i < settings.clips.Length ? settings.clips[i] : null;
                a.resolved[i] = clip != null ? clip : Tone((Cue)i, settings);
                a.lastPlayed[i] = -1f;
            }
            return a;
        }

        public void Play(Cue cue)
        {
            if (!Enabled) return;
            int i = (int)cue;
            float now = Time.unscaledTime;
            if (lastPlayed[i] >= 0f && now - lastPlayed[i] < s.repeatGuard) return;
            lastPlayed[i] = now;
            if (resolved[i] != null) source.PlayOneShot(resolved[i], s.masterVolume);
        }

        /// <summary>Short sine blip with a quick fade, built once at start-up (no per-play allocation).</summary>
        static AudioClip Tone(Cue cue, SoundSettings s)
        {
            int i = (int)cue;
            if (s.placeholderTones == null || i >= s.placeholderTones.Length) return null;
            float freq = s.placeholderTones[i].x, len = Mathf.Max(0.01f, s.placeholderTones[i].y);
            const int rate = 22050;
            int samples = Mathf.CeilToInt(rate * len);
            var data = new float[samples];
            for (int k = 0; k < samples; k++)
            {
                float t = k / (float)rate;
                float env = Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((len - t) / (len * 0.6f));
                data[k] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            var clip = AudioClip.Create("Tone_" + cue, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Phone vibration, gated by the player's setting. No-op in the editor and on platforms without it.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Pulse()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Enabled) Handheld.Vibrate();
#endif
        }
    }
}
