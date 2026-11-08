using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// VIP airlift: a helicopter flies in, hovers over the chosen plane, lowers its cable, lifts the plane, carries it to
    /// the VIP stand, sets it down and flies off. One instance is built once and reused (hidden between trips).
    /// Purely visual: the model already moved the plane to the VIP stand; <c>delivered</c> reports arrival back.
    /// </summary>
    public sealed class HelicopterView : MonoBehaviour
    {
        HelicopterSettings s;
        ScreenMap map;
        Transform rotor, tailRotor, cable;
        public bool Busy { get; private set; }

        public static HelicopterView Create(HelicopterSettings settings, ScreenMap map)
        {
            var go = new GameObject("VipHelicopter");
            var h = go.AddComponent<HelicopterView>();
            h.s = settings; h.map = map;
            h.Build();
            go.SetActive(false);
            return h;
        }

        float cableTop;   // cable starts this far below the pivot (the winch under the belly)

        void Build()
        {
            if (s.model != null) { BuildArt(); return; }
            var body = Art.Lit(s.body, 0.6f); var trim = Art.Lit(s.trim, 0.6f); var dark = Art.Lit(s.rotor, 0.3f);
            var root = transform;
            float k = s.scale;
            Art.Primitive(PrimitiveType.Sphere, "Cabin", root, Vector3.zero, Quaternion.identity, new Vector3(0.9f, 0.8f, 1.3f) * k, body);
            Art.Primitive(PrimitiveType.Sphere, "Window", root, new Vector3(0f, 0.12f, 0.38f) * k, Quaternion.identity, new Vector3(0.7f, 0.5f, 0.6f) * k, trim);
            Art.Primitive(PrimitiveType.Capsule, "Tail", root, new Vector3(0f, 0.12f, -1.05f) * k, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.22f, 0.6f, 0.22f) * k, body);
            Art.Primitive(PrimitiveType.Cube, "Fin", root, new Vector3(0f, 0.35f, -1.6f) * k, Quaternion.identity, new Vector3(0.05f, 0.45f, 0.25f) * k, body);
            Art.Primitive(PrimitiveType.Cube, "SkidL", root, new Vector3(-0.4f, -0.5f, 0f) * k, Quaternion.identity, new Vector3(0.06f, 0.06f, 1.2f) * k, dark);
            Art.Primitive(PrimitiveType.Cube, "SkidR", root, new Vector3(0.4f, -0.5f, 0f) * k, Quaternion.identity, new Vector3(0.06f, 0.06f, 1.2f) * k, dark);
            Art.Primitive(PrimitiveType.Cylinder, "Mast", root, new Vector3(0f, 0.48f, 0f) * k, Quaternion.identity, new Vector3(0.08f, 0.1f, 0.08f) * k, dark);

            rotor = new GameObject("Rotor").transform;
            rotor.SetParent(root, false);
            rotor.localPosition = new Vector3(0f, 0.6f, 0f) * k;
            Art.Primitive(PrimitiveType.Cube, "BladeA", rotor, Vector3.zero, Quaternion.identity, new Vector3(2.6f, 0.03f, 0.14f) * k, dark);
            Art.Primitive(PrimitiveType.Cube, "BladeB", rotor, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), new Vector3(2.6f, 0.03f, 0.14f) * k, dark);

            tailRotor = new GameObject("TailRotor").transform;
            tailRotor.SetParent(root, false);
            tailRotor.localPosition = new Vector3(0.08f, 0.35f, -1.65f) * k;
            Art.Primitive(PrimitiveType.Cube, "TailBlade", tailRotor, Vector3.zero, Quaternion.identity, new Vector3(0.02f, 0.6f, 0.08f) * k, dark);

            cable = Art.Primitive(PrimitiveType.Cylinder, "Cable", root, Vector3.zero, Quaternion.identity, Vector3.one, Art.Lit(s.cable, 0.2f)).transform;
            cable.gameObject.SetActive(false);
        }

        /// <summary>
        /// Art helicopter. The model is one mesh, so its top slice (main rotor blades + hub) is split off once at start-up and
        /// spun around the hub; the rest is the body. Nose turned to +Z, scaled to <see cref="HelicopterSettings.modelLength"/>.
        /// </summary>
        void BuildArt()
        {
            Mesh src = s.model;
            Bounds mb = src.bounds;
            float k = s.modelLength / Mathf.Max(1e-4f, mb.size.x);
            float cut = mb.max.y - mb.size.y * s.rotorSliceFraction;
            Mesh bodyMesh, rotorMesh;
            Vector3 hub;
            Art.SplitAbove(src, cut, out bodyMesh, out rotorMesh, out hub);
            var mat = Art.LitTextured(s.modelTexture, Color.white, 0.55f);

            var frame = new GameObject("Model").transform;   // rotated so the nose faces +Z
            frame.SetParent(transform, false);
            frame.localRotation = Quaternion.Euler(0f, s.modelYaw, 0f);
            frame.localScale = Vector3.one * k;
            frame.localPosition = -(frame.localRotation * mb.center) * k;
            Art.MeshObject("Body", bodyMesh, mat, frame).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            rotor = new GameObject("Rotor").transform;        // pivot at the hub so it spins in place
            rotor.SetParent(frame, false);
            rotor.localPosition = hub;
            var blades = Art.MeshObject("Blades", rotorMesh, mat, rotor);
            blades.transform.localPosition = -hub;

            tailRotor = new GameObject("TailRotor").transform;   // baked into the body: nothing separate to spin
            tailRotor.SetParent(transform, false);
            cableTop = -(mb.center.y - mb.min.y) * k * 0.85f;

            cable = Art.Primitive(PrimitiveType.Cylinder, "Cable", transform, Vector3.zero, Quaternion.identity, Vector3.one, Art.Lit(s.cable, 0.2f)).transform;
            cable.gameObject.SetActive(false);
        }

        void Update()
        {
            rotor.Rotate(0f, s.rotorDegPerSec * Time.deltaTime, 0f, Space.Self);
            if (tailRotor.childCount > 0) tailRotor.Rotate(s.rotorDegPerSec * Time.deltaTime, 0f, 0f, Space.Self);
        }

        /// <summary>Airlifts <paramref name="plane"/> to <paramref name="stand"/>, ending at the given scale and rotation.</summary>
        public void Carry(Transform plane, Vector3 stand, float endScale, Quaternion endRotation, Action delivered)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(CarryRoutine(plane, stand, endScale, endRotation, delivered));
        }

        /// <summary>Stops a trip immediately (level restart / teardown) and hides the helicopter.</summary>
        public void Abort()
        {
            StopAllCoroutines();
            cable.gameObject.SetActive(false);
            Busy = false;
            gameObject.SetActive(false);
        }

        IEnumerator CarryRoutine(Transform plane, Vector3 stand, float endScale, Quaternion endRotation, Action delivered)
        {
            Busy = true;
            long t = DevLog.Stamp();
            DevLog.Info("vip", "helicopter airlift started");
            Vector3 up = Vector3.up;
            Vector3 pickup = plane.position;
            transform.position = map.Ground(s.entryPx) + up * s.cruiseHeight;

            // 1. fly in and hover over the plane, then drop to hook height
            yield return Fly(pickup + up * s.cruiseHeight, s.flySpeed, null);
            yield return Fly(pickup + up * s.hookHeight, s.verticalSpeed, null);
            cable.gameObject.SetActive(true);
            yield return Hold(plane, s.hoverPause);

            // 2. lift the plane and carry it to the VIP stand, turning and scaling it on the way
            Vector3 startScale = plane.localScale;
            Quaternion startRot = plane.rotation;
            yield return Fly(pickup + up * s.cruiseHeight, s.verticalSpeed, plane);
            Vector3 from = transform.position, to = stand + up * s.cruiseHeight;
            float dist = Vector3.Distance(from, to);
            yield return Fly(to, s.flySpeed, plane, t =>
            {
                plane.rotation = Quaternion.Slerp(startRot, endRotation, t);
                plane.localScale = Vector3.Lerp(startScale, startScale * endScale, t);
            }, from, dist);

            // 3. lower it onto the stand and release
            yield return Fly(stand + up * s.hookHeight, s.verticalSpeed, plane);
            plane.position = stand;
            plane.rotation = endRotation;
            cable.gameObject.SetActive(false);
            DevLog.Took("vip", "plane delivered to VIP stand after", t, 6000f);
            if (delivered != null) delivered();
            yield return new WaitForSeconds(s.hoverPause);

            // 4. climb and leave
            yield return Fly(stand + up * s.cruiseHeight, s.verticalSpeed, null);
            yield return Fly(map.Ground(s.exitPx) + up * s.cruiseHeight, s.flySpeed, null);
            Busy = false;
            gameObject.SetActive(false);
        }

        IEnumerator Hold(Transform plane, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) { UpdateCable(plane); yield return null; }
        }

        /// <summary>Moves at constant speed to <paramref name="target"/>, carrying the plane below on its cable if given.</summary>
        IEnumerator Fly(Vector3 target, float speed, Transform plane, Action<float> progress = null, Vector3 origin = default(Vector3), float totalDistance = 0f)
        {
            while (true)
            {
                Vector3 pos = transform.position;
                Vector3 delta = target - pos;
                float step = speed * Time.deltaTime;
                Vector3 flat = new Vector3(delta.x, 0f, delta.z);
                if (flat.sqrMagnitude > 1e-4f)
                {
                    Quaternion face = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(s.tiltDegrees, 0f, 0f);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, face, 360f * Time.deltaTime);
                }
                else transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 180f * Time.deltaTime);

                bool arrived = delta.magnitude <= step;
                transform.position = arrived ? target : pos + delta.normalized * step;
                if (plane != null)
                {
                    plane.position = transform.position - Vector3.up * s.hookHeight;
                    if (progress != null && totalDistance > 1e-4f)
                        progress(Mathf.Clamp01(Vector3.Distance(origin, transform.position) / totalDistance));
                }
                UpdateCable(plane);
                if (arrived) yield break;
                yield return null;
            }
        }

        void UpdateCable(Transform plane)
        {
            if (!cable.gameObject.activeSelf) return;
            Vector3 top = transform.position + Vector3.up * cableTop;
            Vector3 bottom = plane != null ? plane.position + Vector3.up * 0.15f : top - Vector3.up * s.hookHeight;
            Vector3 mid = (top + bottom) * 0.5f;
            float len = Vector3.Distance(top, bottom);
            cable.position = mid;
            cable.rotation = Quaternion.FromToRotation(Vector3.up, (top - bottom).normalized);
            cable.localScale = new Vector3(0.04f, len * 0.5f, 0.04f);   // unit cylinder is 2 tall
        }
    }
}
