using UnityEngine;
// Runtime QA switch for DOM 04 only; changes nothing without ?qa=dom04.
public sealed class BraxDom04PremiumEvidence : MonoBehaviour
{
    public MeshRenderer wallA, wallB;
    public Material[] oldA, oldB, newA, newB;
    public MeshFilter approach;
    public Mesh oldApproach, newApproach;
    public GameObject visibleUpgrade;
    public void Dom04Before(string ignored) { Toggle(false); }
    public void Dom04After(string ignored) { Toggle(true); }
    void Toggle(bool after)
    {
        if (!Application.absoluteURL.Contains("qa=dom04")) return;
        wallA.sharedMaterials = after ? newA : oldA;
        wallB.sharedMaterials = after ? newB : oldB;
        approach.sharedMesh = after ? newApproach : oldApproach;
        visibleUpgrade.SetActive(after);
        Debug.Log("DOM04_ONLY_ACTUAL_UNITY_COMPARE_" + (after ? "AFTER" : "BEFORE"));
    }
}
