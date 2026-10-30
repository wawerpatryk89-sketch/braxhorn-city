using UnityEngine;
// Runtime QA switch for DOM 04 only; changes nothing without ?qa=dom04.
public sealed class BraxDom04PremiumEvidence : MonoBehaviour
{
    public MeshRenderer wallA, wallB;
    public Material[] oldA, oldB, newA, newB;
    public MeshFilter approach;
    public Mesh oldApproach, newApproach;
    public GameObject visibleUpgrade;
    public MeshRenderer roofRenderer, fenceRenderer;
    public Material[] roofBefore, roofAfter, fenceBefore, fenceAfter;
    public MeshRenderer glassRenderer;
    public Material[] glassBefore,glassAfter;
    public MeshRenderer lawnSourceRenderer;
    public MeshFilter lawnSourceFilter;
    public Mesh lawnBeforeMesh, lawnAfterMesh;
    public Material[] lawnBeforeMaterials, lawnAfterMaterials;
    public MeshFilter fenceFilter;
    public Mesh fenceMeshBefore, fenceMeshAfter;
    public void Dom04Before(string ignored) { Toggle(false); }
    public void Dom04After(string ignored) { Toggle(true); }
    void Toggle(bool after)
    {
        if (!Application.absoluteURL.Contains("dom04=1") || !Application.absoluteURL.Contains("qa=1")) return;
        wallA.sharedMaterials = after ? newA : oldA;
        wallB.sharedMaterials = after ? newB : oldB;
        approach.sharedMesh = after ? newApproach : oldApproach;
        if (lawnSourceFilter) lawnSourceFilter.sharedMesh = after ? lawnAfterMesh : lawnBeforeMesh;
        if (lawnSourceRenderer) lawnSourceRenderer.sharedMaterials = after ? lawnAfterMaterials : lawnBeforeMaterials;
        if (glassRenderer) glassRenderer.sharedMaterials = after ? glassAfter : glassBefore;
        if (roofRenderer) roofRenderer.sharedMaterials = after ? roofAfter : roofBefore;
        if (fenceFilter) fenceFilter.sharedMesh = after ? fenceMeshAfter : fenceMeshBefore;
        if (fenceRenderer) fenceRenderer.sharedMaterials = after ? fenceAfter : fenceBefore;
        visibleUpgrade.SetActive(after);
        Debug.Log("DOM04_ONLY_ACTUAL_UNITY_COMPARE_" + (after ? "AFTER" : "BEFORE"));
    }
}
