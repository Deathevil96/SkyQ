using System;
using System.Collections;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Visual mirror of one <see cref="PlaneData"/>. Never decides puzzle truth; reports animation completion to the controller.</summary>
    public sealed class PlaneView : MonoBehaviour
    {
        public PlaneData Data { get; private set; }
        /// <summary>Boarding walkers still on their way to this plane (a full plane waits for them before departing).</summary>
        public int PendingWalkers;
        public bool Departing;

        float cell;
        GameConfig cfg;
        Transform labelRoot;
        TextMesh label;
        Coroutine moveRoutine, shakeRoutine;

        public static PlaneView Create(PlaneData data, float cell, Transform parent, GameConfig config)
        {
            var go = new GameObject("Plane_" + data.Id + "_" + data.Size);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<PlaneView>();
            view.Data = data; view.cell = cell; view.cfg = config;
            view.BuildModel();
            view.BuildLabel();
            return view;
        }

        void BuildModel()
        {
            PlaneArtSettings a = cfg.planeArt;
            int si = (int)Data.Size;
            if (a.models != null && si < a.models.Length && a.models[si] != null) { BuildArtModel(a, a.models[si], si); return; }
            Color col = Palette.Colors[Data.Color];
            var body = Art.Lit(col, a.bodySmoothness);
            var accent = Art.Lit(col * a.accentDarken, a.accentSmoothness);
            float len = Data.Length * cell * a.lengthFill;
            float w = cell * a.bodyWidth;

            Art.Primitive(PrimitiveType.Capsule, "Fuselage", transform, new Vector3(0, w * 0.5f, 0), Quaternion.Euler(90, 0, 0), new Vector3(w, len * 0.5f, w), body);
            Art.Primitive(PrimitiveType.Cube, "Wings", transform, new Vector3(0, w * 0.45f, len * a.wingOffset), Quaternion.identity,
                new Vector3(cell * a.wingSpan, cell * a.wingThickness, Mathf.Min(len * a.wingChordFraction, cell * a.wingMaxChord)), accent);
            Art.Primitive(PrimitiveType.Cube, "TailWing", transform, new Vector3(0, w * 0.5f, len * a.tailOffset), Quaternion.identity,
                new Vector3(cell * a.tailSpan, cell * a.tailThickness, len * a.tailChordFraction), accent);
            Art.Primitive(PrimitiveType.Cube, "Fin", transform, new Vector3(0, w * 0.9f, len * a.finOffset), Quaternion.identity,
                new Vector3(cell * a.finThickness, cell * a.finHeight, len * a.finChordFraction), accent);
            Art.Primitive(PrimitiveType.Sphere, "Cockpit", transform, new Vector3(0, w * 0.82f, len * a.cockpitForward), Quaternion.identity,
                new Vector3(w * 0.7f, w * 0.4f, w * 0.7f), Art.Lit(a.cockpitColor, a.cockpitSmoothness));

            var arrow = Art.MeshObject("Arrow", Art.ArrowMesh(), Art.Unlit(a.arrowColor), transform);
            arrow.transform.localPosition = new Vector3(0, w * 1.03f, len * a.arrowForwardOffset);
            arrow.transform.localScale = new Vector3(cell * a.arrowSize.x, 1f, cell * a.arrowSize.y);
        }

        /// <summary>
        /// Art model: length fills the footprint, wingspan is capped to <see cref="PlaneArtSettings.maxWingCells"/> so planes
        /// never overlap on the board, sits on the ground, tinted with the gameplay colour via the grey paint texture.
        /// </summary>
        void BuildArtModel(PlaneArtSettings a, Mesh mesh, int si)
        {
            Bounds mb = mesh.bounds;                      // model space: nose along +X, up +Y, wings along Z
            float len = Data.Length * cell * a.lengthFill;
            float sx = len / Mathf.Max(1e-4f, mb.size.x);
            float sz = Mathf.Min(sx, a.maxWingCells * cell / Mathf.Max(1e-4f, mb.size.z));
            float sy = sx * a.modelHeightScale;

            var go = new GameObject("Model");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, a.modelYaw, 0f);
            go.transform.localScale = new Vector3(sx, sy, sz);
            // centre the footprint and stand on the ground (offsets in the rotated frame: model X -> plane Z)
            Vector3 c = go.transform.localRotation * Vector3.Scale(mb.center, new Vector3(sx, 0f, sz));
            go.transform.localPosition = new Vector3(-c.x, -mb.min.y * sy, -c.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Texture2D paint = a.paintTextures != null && si < a.paintTextures.Length ? a.paintTextures[si] : null;
            go.AddComponent<MeshRenderer>().sharedMaterial = Art.LitTextured(paint, Palette.Colors[Data.Color], a.paintSmoothness);

            var arrow = Art.MeshObject("Arrow", Art.ArrowMesh(), Art.Unlit(a.arrowColor), transform);
            arrow.transform.localPosition = new Vector3(0f, mb.size.y * sy + a.arrowLift, len * a.arrowForwardOffset);
            arrow.transform.localScale = new Vector3(cell * a.arrowSize.x, 1f, cell * a.arrowSize.y);
        }

        void BuildLabel()
        {
            FeelConfig f = cfg.feel;
            labelRoot = new GameObject("Label_" + Data.Id).transform;
            labelRoot.SetParent(transform.parent, false);
            Art.Primitive(PrimitiveType.Quad, "Badge", labelRoot, Vector3.zero, Quaternion.Euler(90, 0, 0),
                new Vector3(f.badgeSize.x, f.badgeSize.y, 1f), Art.Unlit(Palette.Colors[Data.Color] * cfg.planeArt.accentDarken));
            label = Art.MakeText("", labelRoot, Color.white, f.labelTextSize);
            label.transform.localPosition = new Vector3(0, 0.02f, 0);
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
            labelRoot.gameObject.SetActive(false);
        }

        void OnDestroy() { if (labelRoot != null) Destroy(labelRoot.gameObject); }

        public void SetPose(Vector3 pos, Quaternion rot) { transform.SetPositionAndRotation(pos, rot); }

        /// <summary>True while the plane drives along a path (launch or departure).</summary>
        public bool Moving { get { return moveRoutine != null; } }

        /// <summary>World point at the plane's tail (exhaust), following its current scale.</summary>
        public Vector3 TailPoint
        {
            get { return transform.position - transform.forward * (Data.Length * cell * cfg.planeArt.lengthFill * 0.5f * transform.localScale.z); }
        }

        // seat counter -----------------------------------------------------------------------------------------------
        public void ShowLabel(Vector3 worldPos)
        {
            labelRoot.gameObject.SetActive(true);
            labelRoot.SetPositionAndRotation(worldPos, Quaternion.identity);
        }
        public void HideLabel() { if (labelRoot != null) labelRoot.gameObject.SetActive(false); }
        public void SetRemaining(int remaining) { if (label != null) label.text = remaining.ToString(); }

        // feedback ---------------------------------------------------------------------------------------------------
        /// <summary>
        /// A passenger reached the plane. The plane swells a little with every passenger (owner's favourite "funny" look),
        /// reaching <see cref="FeelConfig.boardGrowMax"/> when full, plus a short pop. Growth is computed from the seat count,
        /// never accumulated from the current scale, so it is bounded and identical on every device and frame rate.
        /// </summary>
        public void WalkerArrived()
        {
            PendingWalkers = Mathf.Max(0, PendingWalkers - 1);
            if (boardedSeen == 0) restScale = transform.localScale;   // parked size, before any growth
            boardedSeen++;
            if (popRoutine != null) StopCoroutine(popRoutine);
            if (cfg.reduceMotion) { transform.localScale = restScale * Growth(); popRoutine = null; }
            else popRoutine = StartCoroutine(Pop());
        }

        int boardedSeen;
        Vector3 restScale;
        Coroutine popRoutine;

        float Growth() { return 1f + cfg.feel.boardGrowMax * Mathf.Clamp01(boardedSeen / (float)Mathf.Max(1, Data.Capacity)); }

        IEnumerator Pop()
        {
            FeelConfig f = cfg.feel;
            Vector3 from = transform.localScale, target = restScale * Growth();
            for (float t = 0f; t < f.bumpSeconds; t += Time.deltaTime)
            {
                float k = t / f.bumpSeconds;
                transform.localScale = Vector3.Lerp(from, target, k) * (1f + f.bumpAmount * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            transform.localScale = target;
            popRoutine = null;
        }

        /// <summary>Short shake along <paramref name="axis"/> to show a rejected tap. Ignored while moving or already shaking.</summary>
        public void PlayShake(Vector3 axis)
        {
            if (cfg.reduceMotion || shakeRoutine != null || moveRoutine != null) return;
            shakeRoutine = StartCoroutine(Shake(axis));
        }

        IEnumerator Shake(Vector3 axis)
        {
            FeelConfig f = cfg.feel;
            Vector3 basePos = transform.position;
            for (float t = 0f; t < f.shakeSeconds; t += Time.deltaTime)
            {
                transform.position = basePos + axis * (Mathf.Sin(t * f.shakeFrequency) * f.shakeAmplitude * (1f - t / f.shakeSeconds));
                yield return null;
            }
            transform.position = basePos;
            shakeRoutine = null;
        }

        // movement ---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Moves along a path at constant speed, turning towards the tangent. The plane scales to <paramref name="scaleEnd"/>
        /// (eased over the last stretch, or linearly while lifting off) and optionally rises by <paramref name="liftHeight"/>.
        /// </summary>
        public void Follow(Polyline path, float speed, float scaleEnd, float turnDegPerSec, Action done, float liftHeight = 0f, Quaternion? finalRotation = null)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            if (popRoutine != null) { StopCoroutine(popRoutine); popRoutine = null; }   // a last pop must not fight the departure scale
            moveRoutine = StartCoroutine(FollowRoutine(path, speed, scaleEnd, turnDegPerSec, done, liftHeight, finalRotation));
        }

        IEnumerator FollowRoutine(Polyline path, float speed, float scaleEnd, float turnDegPerSec, Action done, float liftHeight, Quaternion? finalRotation)
        {
            FeelConfig f = cfg.feel;
            float s = 0f, total = path.Length;
            Vector3 startScale = transform.localScale;
            Quaternion rot = transform.rotation;
            bool lifting = liftHeight > 0f;
            while (s < total)
            {
                s = Mathf.Min(total, s + speed * Time.deltaTime);
                Vector3 pos = path.Point(s);
                rot = Quaternion.RotateTowards(rot, Quaternion.LookRotation(path.Tangent(s), Vector3.up), turnDegPerSec * Time.deltaTime);
                float progress = s / total;
                if (lifting) pos.y += progress * progress * liftHeight;
                transform.SetPositionAndRotation(pos, rot);

                float k = lifting ? progress : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - f.arrivalScaleStart) / (1f - f.arrivalScaleStart)));
                transform.localScale = Vector3.Lerp(startScale, startScale * scaleEnd, k);
                yield return null;
            }
            if (finalRotation.HasValue)
            {
                Quaternion from = transform.rotation;
                for (float t = 0f; t < f.settleRotateSeconds; t += Time.deltaTime)
                {
                    transform.rotation = Quaternion.Slerp(from, finalRotation.Value, t / f.settleRotateSeconds);
                    yield return null;
                }
                transform.rotation = finalRotation.Value;
            }
            moveRoutine = null;
            if (done != null) done();
        }
    }
}
