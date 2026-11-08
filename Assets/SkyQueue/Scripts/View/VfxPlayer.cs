using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>One flipbook burst (frames from Art/Sprites/VFX).</summary>
    [Serializable]
    public sealed class FlipbookFx
    {
        public Sprite[] frames = new Sprite[0];
        public float seconds = 0.5f;
        [Tooltip("Billboard height in world units.")]
        public float size = 1.6f;
        [Tooltip("Lift above the ground (world units) so the burst is not hidden by the plane.")]
        public float height = 0.3f;
        [Range(0f, 1f)] public float fadeStart = 0.6f;
        public bool Usable { get { return frames != null && frames.Length > 0 && frames[0] != null; } }
    }

    [Serializable]
    public sealed class VfxSettings
    {
        public bool enabled = true;
        [Tooltip("Dust ring when a plane settles on its pad.")]
        public FlipbookFx park = new FlipbookFx { seconds = 0.5f, size = 1.8f, height = 0.15f };
        [Tooltip("Sparkle when a plane's last seat is filled.")]
        public FlipbookFx full = new FlipbookFx { seconds = 0.55f, size = 1.4f, height = 0.9f };
        [Tooltip("White poof when a full plane leaves its pad.")]
        public FlipbookFx depart = new FlipbookFx { seconds = 0.5f, size = 1.7f, height = 0.3f };
        public int poolSize = 12;   // bursts on screen at once; the oldest is reused when full
    }

    /// <summary>
    /// Pooled world-space flipbook bursts (camera-facing sprites). Created once; playing a burst never allocates.
    /// </summary>
    public sealed class VfxPlayer : MonoBehaviour
    {
        struct Burst { public SpriteRenderer Sr; public FlipbookFx Fx; public float Age; }

        VfxSettings s;
        Camera cam;
        Burst[] pool;
        int next;

        public static VfxPlayer Create(VfxSettings settings, Camera camera)
        {
            var go = new GameObject("Vfx");
            var v = go.AddComponent<VfxPlayer>();
            v.s = settings; v.cam = camera;
            v.pool = new Burst[Mathf.Max(1, settings.poolSize)];
            for (int i = 0; i < v.pool.Length; i++)
            {
                var b = new GameObject("Burst" + i);
                b.transform.SetParent(go.transform, false);
                var sr = b.AddComponent<SpriteRenderer>();
                b.SetActive(false);
                v.pool[i] = new Burst { Sr = sr, Age = -1f };
            }
            return v;
        }

        public void Play(FlipbookFx fx, Vector3 ground)
        {
            if (s == null || !s.enabled || fx == null || !fx.Usable) return;
            int i = next;
            next = (next + 1) % pool.Length;
            var b = pool[i];
            b.Fx = fx; b.Age = 0f;
            b.Sr.sprite = fx.frames[0];
            b.Sr.color = Color.white;
            var t = b.Sr.transform;
            t.position = ground + Vector3.up * fx.height;
            float h = fx.frames[0].bounds.size.y;
            t.localScale = Vector3.one * (fx.size / Mathf.Max(0.001f, h));
            if (cam != null) t.rotation = cam.transform.rotation;
            b.Sr.gameObject.SetActive(true);
            pool[i] = b;
        }

        void LateUpdate()
        {
            if (pool == null) return;
            float dt = Time.deltaTime;
            Quaternion face = cam != null ? cam.transform.rotation : Quaternion.identity;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Age < 0f) continue;
                var b = pool[i];
                b.Age += dt;
                float k = b.Age / Mathf.Max(0.01f, b.Fx.seconds);
                if (k >= 1f) { b.Age = -1f; b.Sr.gameObject.SetActive(false); pool[i] = b; continue; }
                var f = b.Fx.frames;
                b.Sr.sprite = f[Mathf.Min(f.Length - 1, (int)(k * f.Length))];
                b.Sr.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(b.Fx.fadeStart, 1f, k)));
                b.Sr.transform.rotation = face;
                pool[i] = b;
            }
        }

        /// <summary>Level change: hide everything still playing.</summary>
        public void Clear()
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++) { pool[i].Age = -1f; pool[i].Sr.gameObject.SetActive(false); }
        }
    }
}
