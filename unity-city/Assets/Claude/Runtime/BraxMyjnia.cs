using UnityEngine;

// MYJNIA AUTOMATYCZNA BRAXHORN (właściciel 3.11 15:02: „myjnia ma być funkcjonalna”). Gdy auto gracza stanie w hali,
// portal jedzie na szynach od bramy do końca hali i z powrotem (cykl 24 s), szczotki pionowe i pozioma kręcą się.
// Po cyklu czeka, aż auto wyjedzie (kolejny wjazd = kolejne mycie). Poza zasięgiem kamery nic nie liczy.
public sealed class BraxMyjnia : MonoBehaviour
{
    public Transform portal, poziom;
    public Transform[] pion;
    public Vector3 strefaSrodek, strefaRozmiar;
    public float zStart, zKoniec, cykl = 24f;
    float faza; bool myje, zrobione;

    bool AutoWHali()
    {
        foreach (var a in BraxAutoGracza.Wszystkie)
        {
            if (!a || !a.isActiveAndEnabled) continue;
            var p = a.transform.position - strefaSrodek;
            if (Mathf.Abs(p.x) < strefaRozmiar.x * .5f && Mathf.Abs(p.z) < strefaRozmiar.z * .5f && Mathf.Abs(p.y) < 3f) return true;
        }
        return false;
    }

    void Update()
    {
        if (!portal) return;
        bool jest = AutoWHali();
        if (!jest) zrobione = false;
        if (!myje && jest && !zrobione) { myje = true; faza = 0f; Debug.Log("BRAXHORN_MYJNIA mycie: start"); }
        if (!myje) return;
        faza += Time.deltaTime / Mathf.Max(4f, cykl);
        float k = faza < .5f ? faza * 2f : 2f - faza * 2f;
        var p0 = portal.position; p0.z = Mathf.Lerp(zStart, zKoniec, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k))); portal.position = p0;
        float w = 420f * Time.deltaTime;
        if (pion != null) for (int i = 0; i < pion.Length; i++) if (pion[i]) pion[i].Rotate(0f, i % 2 == 0 ? w : -w, 0f, Space.Self);
        if (poziom) poziom.Rotate(0f, w, 0f, Space.Self);
        if (faza >= 1f) { myje = false; zrobione = true; Debug.Log("BRAXHORN_MYJNIA mycie: koniec"); }
    }
}
