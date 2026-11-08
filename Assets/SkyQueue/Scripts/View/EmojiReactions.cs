using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>When and how rows waiting in the feed lines react.</summary>
    [Serializable]
    public sealed class EmojiSettings
    {
        public bool enabled = true;
        [Tooltip("A row starts reacting after waiting this many seconds in view on a feed line.")]
        public float waitSeconds = 8f;
        [Tooltip("How often waiting rows are checked (seconds).")]
        public float checkInterval = 0.6f;
        [Range(0f, 1f)] public float chancePerCheck = 0.3f;
        [Tooltip("Minimum seconds between two reactions of the same row.")]
        public float rowCooldown = 6f;
        [Tooltip("Rows closer than this (world units along the feed) to the top of the screen never react, so bubbles stay under the HUD.")]
        public float minFeedDistance = 1.2f;
        public int maxVisible = 6;                    // pool size; never allocates more
        public float lifetime = 1.6f;
        public float height = 0.9f, rise = 0.5f;
        public float popSeconds = 0.15f;
        [Tooltip("Bubble size in world units (sprite height).")]
        public float worldSize = 0.7f;
        [Tooltip("Emoji sprites (Art/Sprites/Emoji). If empty, the text faces below are used instead.")]
        public Sprite[] sprites = new Sprite[0];
        public float characterSize = 0.07f;           // text fallback only
        public int fontSize = 96;
        public string[] reactions = { ">:(", "zZ", "?!", "!!", ":(", "..." };
        public Color[] colors = { new Color(1f, 0.35f, 0.3f), new Color(0.6f, 0.75f, 1f), new Color(1f, 0.85f, 0.2f), new Color(1f, 0.5f, 0.2f), new Color(0.75f, 0.75f, 0.85f), Color.white };
    }

    /// <summary>
    /// Owner request: rows that have waited too long in the feed lines show a random emoji above them. Pooled sprite (or text)
    /// billboards created once; checks run on an interval, so idle cost is a timer compare per frame.
    /// </summary>
    public sealed class EmojiReactions : MonoBehaviour
    {
        sealed class Bubble
        {
            public Transform Root; public SpriteRenderer Sprite; public TextMesh Text;
            public PassengerRing.Row Row; public int Feed; public float Age; public Vector3 LastPos; public Color Color;
            public bool Active { get { return Root.gameObject.activeSelf; } }
        }

        EmojiSettings s;
        Camera cam;
        PassengerRing ring;
        Polyline[] feeds;
        readonly List<Bubble> pool = new List<Bubble>();
        readonly Dictionary<PassengerRing.Row, float> nextAllowed = new Dictionary<PassengerRing.Row, float>();
        readonly List<PassengerRing.Row> candidates = new List<PassengerRing.Row>();
        readonly List<int> candidateFeed = new List<int>();
        readonly System.Random rng = new System.Random();
        float checkTimer;
        bool useSprites;

        public static EmojiReactions Create(EmojiSettings settings, Camera camera)
        {
            var go = new GameObject("EmojiReactions");
            var e = go.AddComponent<EmojiReactions>();
            e.s = settings; e.cam = camera;
            e.useSprites = settings.sprites != null && settings.sprites.Length > 0;
            for (int i = 0; i < Mathf.Max(1, settings.maxVisible); i++)
            {
                var b = new Bubble();
                if (e.useSprites)
                {
                    var holder = new GameObject("Emoji" + i);
                    holder.transform.SetParent(go.transform, false);
                    b.Sprite = holder.AddComponent<SpriteRenderer>();
                    b.Root = holder.transform;
                }
                else
                {
                    b.Text = Art.MakeText("", go.transform, Color.white, settings.characterSize, settings.fontSize);
                    b.Text.anchor = TextAnchor.MiddleCenter;
                    b.Root = b.Text.transform;
                }
                b.Root.gameObject.SetActive(false);
                e.pool.Add(b);
            }
            return e;
        }

        /// <summary>New level: watch this ring's feed lines; hide anything still showing.</summary>
        public void Bind(PassengerRing r, Polyline[] feedPaths)
        {
            ring = r; feeds = feedPaths;
            nextAllowed.Clear();
            foreach (var b in pool) { b.Row = null; b.Root.gameObject.SetActive(false); }
        }

        void Update()
        {
            if (ring == null || s == null) return;
            if (s.enabled && (checkTimer += Time.deltaTime) >= s.checkInterval)
            {
                checkTimer = 0f;
                Check();
            }
            Animate();
        }

        void Check()
        {
            float now = Time.time;
            candidates.Clear(); candidateFeed.Clear();
            for (int f = 0; f < ring.Feed.Length; f++)
                foreach (var row in ring.Feed[f])
                {
                    if (row.Entering || row.FeedWait < s.waitSeconds || row.S < s.minFeedDistance) continue;
                    float next;
                    if (nextAllowed.TryGetValue(row, out next) && now < next) continue;
                    candidates.Add(row); candidateFeed.Add(f);
                }
            for (int i = 0; i < candidates.Count; i++)
            {
                if (rng.NextDouble() > s.chancePerCheck) continue;
                var bubble = FreeBubble();
                if (bubble == null) return;   // pool full: skip, never allocate
                var row = candidates[i];
                nextAllowed[row] = now + s.rowCooldown;
                Show(bubble, row, candidateFeed[i]);
                DevLog.Detail("emoji", "feed " + candidateFeed[i] + " row of colour " + row.Color + " waited " + row.FeedWait.ToString("0") + " s");
            }
        }

        void Show(Bubble b, PassengerRing.Row row, int feed)
        {
            b.Row = row; b.Feed = feed; b.Age = 0f;
            b.LastPos = feeds[feed].Point(row.S);
            if (useSprites)
            {
                var sprite = s.sprites[rng.Next(s.sprites.Length)];
                b.Sprite.sprite = sprite;
                b.Color = Color.white;
            }
            else
            {
                int pick = rng.Next(s.reactions.Length);
                b.Text.text = s.reactions[pick];
                b.Color = s.colors != null && s.colors.Length > 0 ? s.colors[pick % s.colors.Length] : Color.white;
            }
            b.Root.gameObject.SetActive(true);
        }

        Bubble FreeBubble()
        {
            foreach (var b in pool) if (!b.Active) return b;
            return null;
        }

        void Animate()
        {
            Quaternion face = cam != null ? cam.transform.rotation : Quaternion.identity;
            foreach (var b in pool)
            {
                if (!b.Active) continue;
                b.Age += Time.deltaTime;
                float k = b.Age / s.lifetime;
                if (k >= 1f) { b.Root.gameObject.SetActive(false); b.Row = null; continue; }
                if (b.Row != null && ring.Feed[b.Feed].Contains(b.Row)) b.LastPos = feeds[b.Feed].Point(b.Row.S);   // follow the row while it queues
                float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(b.Age / Mathf.Max(0.01f, s.popSeconds)));
                b.Root.position = b.LastPos + Vector3.up * (s.height + s.rise * k);
                b.Root.rotation = face;
                Color c = b.Color; c.a = Mathf.Clamp01((1f - k) / 0.3f);
                if (useSprites)
                {
                    float h = b.Sprite.sprite != null ? b.Sprite.sprite.bounds.size.y : 1f;
                    b.Root.localScale = Vector3.one * (s.worldSize / Mathf.Max(0.001f, h) * pop);
                    b.Sprite.color = c;
                }
                else
                {
                    b.Root.localScale = Vector3.one * pop;
                    b.Text.color = c;
                }
            }
        }
    }
}
