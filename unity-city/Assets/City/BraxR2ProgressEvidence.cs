using System;
using UnityEngine;

// QA-only material/UV comparison. Player, physics, camera and animations are untouched.
public sealed class BraxR2ProgressEvidence : MonoBehaviour
{
    [Serializable] public sealed class Binding
    {
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Mesh beforeMesh, afterMesh;
        public Material[] beforeMaterials, afterMaterials;
    }
    public Binding[] bindings;
    public void ProgressBefore(string ignored) { Set(false); }
    public void ProgressAfter(string ignored) { Set(true); }
    void Set(bool after)
    {
        if (!Application.absoluteURL.Contains("qa=1")) return;
        foreach (var b in bindings)
        {
            b.filter.sharedMesh = after ? b.afterMesh : b.beforeMesh;
            b.renderer.sharedMaterials = after ? b.afterMaterials : b.beforeMaterials;
        }
        Debug.Log("R2_PROGRESS_COMPARISON " + (after ? "AFTER" : "BEFORE") + " bindings=" + bindings.Length);
    }
}
