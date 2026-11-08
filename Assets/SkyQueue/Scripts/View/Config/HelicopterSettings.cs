using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Look and timing of the VIP helicopter that airlifts a plane to the VIP stand (placeholder art).</summary>
    [Serializable]
    public sealed class HelicopterSettings
    {
        [Header("Path (reference px for screen points, world units for heights)")]
        [Tooltip("Where the helicopter enters / leaves (reference px; may be off-screen).")]
        public Vector2 entryPx = new Vector2(-180f, 1150f);
        public Vector2 exitPx = new Vector2(1100f, 600f);
        public float cruiseHeight = 4f;          // flying height
        public float hookHeight = 1.6f;          // helicopter height above the plane while carrying (cable length)
        public float flySpeed = 13f;
        public float verticalSpeed = 6f;
        public float hoverPause = 0.15f;         // pause when hooking / releasing

        [Header("Art model (Art/3D/Vehicle; empty = primitive placeholder)")]
        public Mesh model;
        public Texture2D modelTexture;
        [Tooltip("Yaw that turns the model's nose to +Z (the art faces +X).")]
        public float modelYaw = -90f;
        [Tooltip("World length nose-to-tail.")]
        public float modelLength = 2.4f;
        [Tooltip("Top slice of the model (fraction of its height) split off as the spinning main rotor (blades + hub).")]
        [Range(0.02f, 0.4f)] public float rotorSliceFraction = 0.11f;

        [Header("Placeholder look")]
        public float scale = 1.1f;
        public Color body = new Color(0.95f, 0.35f, 0.25f);
        public Color trim = new Color(0.98f, 0.95f, 0.9f);
        public Color rotor = new Color(0.2f, 0.22f, 0.28f);
        public Color cable = new Color(0.15f, 0.15f, 0.18f);
        public float rotorDegPerSec = 1500f;
        public float tiltDegrees = 12f;          // nose-down lean while flying forward
    }
}
