using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Owns the fixed gameplay camera and the mapping between the reference composition and the real screen.
    ///
    /// The camera always renders the full screen. The reference composition (a "frame" with the reference aspect) is fitted
    /// inside the device safe area and centred; the camera's vertical FOV is derived so the frame keeps exactly the
    /// calibrated world scale, whatever the screen shape. Anything outside the frame (taller phones, tablets, landscape)
    /// simply shows more scenery, so there are no black bars and no layout drift. Static world layout stays valid across
    /// resizes because the frame -> world mapping never changes.
    /// </summary>
    public sealed class ScreenMap
    {
        public readonly Camera Cam;

        /// <summary>The gameplay frame in screen pixels (origin bottom-left).</summary>
        public Rect FramePx { get; private set; }

        int lastW, lastH;
        Rect lastSafe;

        public ScreenMap(Camera main, Color background)
        {
            Cam = main;
            Cam.orthographic = false;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = background;
            Cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// Pure layout maths: the largest reference-aspect frame that fits the safe area, centred on the screen.
        /// Symmetric insets keep the frame centred on the camera's optical axis (notch/cutout/gesture-bar safe).
        /// </summary>
        public static Rect FitFrame(float screenW, float screenH, Rect safeArea, float frameAspect)
        {
            float w = Mathf.Max(1f, screenW), h = Mathf.Max(1f, screenH);
            float insetX = Mathf.Max(safeArea.xMin, w - safeArea.xMax);
            float insetY = Mathf.Max(safeArea.yMin, h - safeArea.yMax);
            float usableW = Mathf.Max(1f, w - 2f * insetX), usableH = Mathf.Max(1f, h - 2f * insetY);
            float fw, fh;
            if (usableW / usableH < frameAspect) { fw = usableW; fh = fw / frameAspect; }   // narrow screen: fit width
            else { fh = usableH; fw = fh * frameAspect; }                                    // wide screen: fit height
            return new Rect((w - fw) * 0.5f, (h - fh) * 0.5f, fw, fh);
        }

        /// <summary>Vertical FOV that keeps a frame of <paramref name="frameH"/> px at the calibrated scale on a screen of <paramref name="screenH"/> px.</summary>
        public static float FovForFrame(float calibratedFov, float screenH, float frameH)
        {
            float tanFrame = Mathf.Tan(calibratedFov * 0.5f * Mathf.Deg2Rad);
            return 2f * Mathf.Atan(tanFrame * screenH / frameH) * Mathf.Rad2Deg;
        }

        /// <summary>Recompute the frame and camera from the current screen. Returns true (always changes the camera).</summary>
        public bool Apply(GameConfig cfg)
        {
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            Rect safe = Screen.safeArea;
            FramePx = FitFrame(w, h, safe, GameConfig.RefAspect);

            Cam.transform.SetPositionAndRotation(new Vector3(0f, cfg.camHeight, cfg.camZ), Quaternion.Euler(cfg.camPitch, 0f, 0f));
            // camFov is the calibrated vertical FOV of the frame; widen/narrow it so the frame keeps that scale on this screen.
            Cam.fieldOfView = FovForFrame(cfg.camFov, h, FramePx.height);
            Cam.ResetAspect();

            lastW = Screen.width; lastH = Screen.height; lastSafe = safe;
            return true;
        }

        /// <summary>Call every frame (cheap): re-fits when the window, orientation or safe area changes.</summary>
        public bool RefitIfScreenChanged(GameConfig cfg)
        {
            if (Screen.width == lastW && Screen.height == lastH && Screen.safeArea == lastSafe) return false;
            return Apply(cfg);
        }

        /// <summary>Reference-screenshot pixel (921 x 2000, y down) to a ground-plane world position.</summary>
        public Vector3 Ground(Vector2 px, float height = 0f)
        {
            Rect f = FramePx;
            float vx = (f.x + px.x / GameConfig.RefW * f.width) / Screen.width;
            float vy = (f.y + (1f - px.y / GameConfig.RefH) * f.height) / Screen.height;
            Ray r = Cam.ViewportPointToRay(new Vector3(vx, vy, 0f));
            if (Mathf.Abs(r.direction.y) < 1e-5f) return r.origin;
            float t = (height - r.origin.y) / r.direction.y;
            return r.origin + r.direction * t;
        }

        public Vector3 Ground(float x, float y, float height = 0f) { return Ground(new Vector2(x, y), height); }

        /// <summary>Reference-pixel x of the visible safe-area edge (left or right) on the current screen; may lie outside 0..RefW on wide screens.</summary>
        public float SafeEdgeRefX(bool left)
        {
            Rect f = FramePx, safe = Screen.safeArea;
            float screenX = left ? safe.xMin : safe.xMax;
            return (screenX - f.x) / Mathf.Max(1f, f.width) * GameConfig.RefW;
        }

        public float WorldDistance(Vector2 pxA, Vector2 pxB) { return Vector3.Distance(Ground(pxA), Ground(pxB)); }
    }
}
