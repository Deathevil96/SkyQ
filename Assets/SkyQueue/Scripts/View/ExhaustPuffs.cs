using System;
using System.Collections;
using UnityEngine;

namespace SkyQueue
{
    [Serializable]
    public sealed class ExhaustSettings
    {
        public bool enabled = true;
        [Tooltip("Puff animation frames (Art/Sprites/Exhaust).")]
        public Sprite[] frames = new Sprite[0];
        [Tooltip("Seconds between puffs while a plane taxis to its stand.")]
        public float interval = 0.07f;
        public float lifetime = 0.7f;
        [Tooltip("Puff size in world units at the start and end of its life.")]
        public float startSize = 0.35f, endSize = 0.9f;
        [Tooltip("Height above the ground and backwards drift (world units per second).")]
        public float height = 0.25f, drift = 0.4f;
        [Tooltip("Also puff while a full plane departs.")]
        public bool onDeparture = false;
        public int poolSize = 48;   // never allocates more; oldest puff is reused when full
    }

    /// <summary>
    /// Pooled exhaust puffs behind taxiing planes. Billboards created once; each puff plays the frames over its lifetime,
    /// grows, drifts back and fades. Emission is driven by <see cref="Trail"/> while a plane is moving.
    /// </summary>
    public sealed class ExhaustPuffs : MonoBehaviour
    {
        struct Puff { public SpriteRenderer Sr; public float Age; public Vector3 Vel; }

        ExhaustSettings s;
        Camera cam;
        Puff[] pool;
        int next;
        float spriteHeight = 1f;

        public static ExhaustPuffs Create(ExhaustSettings settings, Camera camera)
        {
            var go = new GameObject("ExhaustPuffs");
            var e = go.AddComponent<ExhaustPuffs>();
            e.s = settings; e.cam = camera;
            int n = Mathf.Max(1, settings.poolSize);
            e.pool = new Puff[n];
            if (settings.frames != null && settings.frames.Length > 0 && settings.frames[0] != null) e.spriteHeight = settings.frames[0].bounds.size.y;
            for (int i = 0; i < n; i++)
            {
                var p = new GameObject("Puff" + i);
                p.transform.SetParent(go.transform, false);
                var sr = p.AddComponent<SpriteRenderer>();
                p.SetActive(false);
                e.pool[i] = new Puff { Sr = sr, Age = -1f };
            }
            return e;
        }

        bool Usable { get { return s != null && s.enabled && s.frames != null && s.frames.Length > 0; } }

        /// <summary>Emits puffs from the plane's tail for as long as it keeps moving.</summary>
        public IEnumerator Trail(PlaneView view)
        {
            if (!Usable) yield break;
            yield return null;   // the move starts this frame
            if (wait == null) wait = new WaitForSeconds(Mathf.Max(0.01f, s.interval));   // cached: no garbage per puff
            while (view != null && view.Moving)
            {
                Emit(view.TailPoint, -view.transform.forward);
                yield return wait;
            }
        }

        WaitForSeconds wait;

        /// <summary>Starts a trail for this plane (runs on this component, independent of the plane).</summary>
        public void Follow(PlaneView view) { if (Usable && view != null) StartCoroutine(Trail(view)); }

        void Emit(Vector3 at, Vector3 back)
        {
            int i = next;
            next = (next + 1) % pool.Length;   // ring buffer: the oldest puff is reused when the pool is full
            var p = pool[i];
            p.Age = 0f;
            p.Vel = back * s.drift;
            p.Sr.transform.position = at + Vector3.up * s.height;
            p.Sr.sprite = s.frames[0];
            p.Sr.gameObject.SetActive(true);
            pool[i] = p;
        }

        void LateUpdate()
        {
            if (pool == null) return;
            Quaternion face = cam != null ? cam.transform.rotation : Quaternion.identity;
            float dt = Time.deltaTime;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Age < 0f) continue;
                var p = pool[i];
                p.Age += dt;
                float k = p.Age / Mathf.Max(0.01f, s.lifetime);
                if (k >= 1f) { p.Age = -1f; p.Sr.gameObject.SetActive(false); pool[i] = p; continue; }
                var t = p.Sr.transform;
                t.position += p.Vel * dt;
                t.rotation = face;
                t.localScale = Vector3.one * (Mathf.Lerp(s.startSize, s.endSize, k) / Mathf.Max(0.001f, spriteHeight));
                p.Sr.sprite = s.frames[Mathf.Min(s.frames.Length - 1, (int)(k * s.frames.Length))];
                pool[i] = p;
            }
        }

        /// <summary>Level change: hide everything still showing.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++) { pool[i].Age = -1f; pool[i].Sr.gameObject.SetActive(false); }
        }
    }
}
