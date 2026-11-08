using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Basic shape meshes used by the placeholder art. They are project assets referenced from <see cref="GameConfig"/>
    /// because <c>GameObject.CreatePrimitive</c> adds colliders, which fails in player builds that strip the physics
    /// module (and the game uses no physics). Swap any of them for final models without touching code.
    /// </summary>
    [Serializable]
    public sealed class PrimitiveMeshes
    {
        public Mesh cube, sphere, capsule, cylinder, quad;

        public Mesh Get(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Cube: return cube;
                case PrimitiveType.Sphere: return sphere;
                case PrimitiveType.Capsule: return capsule;
                case PrimitiveType.Cylinder: return cylinder;
                case PrimitiveType.Quad: return quad;
                default: throw new ArgumentOutOfRangeException("type", type, "No mesh configured for this primitive type.");
            }
        }

        public bool IsComplete { get { return cube != null && sphere != null && capsule != null && cylinder != null && quad != null; } }
    }
}
