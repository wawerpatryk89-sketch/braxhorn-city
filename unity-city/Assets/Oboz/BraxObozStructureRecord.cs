using System;
using UnityEngine;

// Evidence about an explicitly captured set of construction pieces. A child
// added afterwards is NOT a member. Ranges refer to the final rendered mesh,
// including after several levels of static mesh combination.
[DisallowMultipleComponent]
public sealed class BraxObozStructureRecord : MonoBehaviour
{
    [Serializable] public sealed class Part
    {
        public string label;
        public MeshFilter filter;
        public int firstVertex, vertexCount, firstIndex, indexCount;
        public bool rangesVerified;
    }

    public Part[] parts = new Part[0];
    public Renderer[] members = new Renderer[0];
    public Collider[] ownColliders = new Collider[0];
    public float expectedTopMetres = -1f;
    public Renderer[] heightMembers = new Renderer[0];

    public bool Contains(Renderer renderer)
    {
        if (!renderer) return false;
        foreach (var member in members) if (member == renderer) return true;
        return false;
    }
}
