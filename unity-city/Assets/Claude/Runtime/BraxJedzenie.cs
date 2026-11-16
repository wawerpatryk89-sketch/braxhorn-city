using UnityEngine;

public sealed class BraxJedzenie : MonoBehaviour
{
    public Animation ruch;
    public Renderer[] widok;

    void LateUpdate()
    {
        bool je = ruch && ruch.IsPlaying("Eat");
        foreach (var r in widok) if (r && r.enabled != je) r.enabled = je;
    }

    void OnDisable() { if (widok != null) foreach (var r in widok) if (r) r.enabled = false; }
}
