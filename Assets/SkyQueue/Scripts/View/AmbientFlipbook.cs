using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Looping sprite flipbook billboard for scenery (e.g. the desert signal smoke). Plays the frames, rests, repeats.
    /// Cost: one sprite swap per frame step; no allocations.
    /// </summary>
    public sealed class AmbientFlipbook : MonoBehaviour
    {
        Sprite[] frames;
        SpriteRenderer sr;
        Camera cam;
        float fps, rest, size, t;

        public static AmbientFlipbook Create(Transform parent, string name, Sprite[] frames, Vector3 position, float worldSize, float fps, float restSeconds, Camera cam)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var f = go.AddComponent<AmbientFlipbook>();
            f.frames = frames; f.fps = Mathf.Max(1f, fps); f.rest = Mathf.Max(0f, restSeconds); f.size = worldSize; f.cam = cam;
            f.sr = go.AddComponent<SpriteRenderer>();
            f.sr.sprite = frames[0];
            float h = frames[0].bounds.size.y;
            go.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.001f, h));
            return f;
        }

        void LateUpdate()
        {
            if (frames == null || frames.Length == 0) return;
            float play = frames.Length / fps;
            t = (t + Time.deltaTime) % (play + rest);
            int i = t < play ? Mathf.Min(frames.Length - 1, (int)(t * fps)) : -1;
            sr.enabled = i >= 0;
            if (i >= 0) sr.sprite = frames[i];
            if (cam != null) transform.rotation = cam.transform.rotation;   // face the camera
        }
    }
}
