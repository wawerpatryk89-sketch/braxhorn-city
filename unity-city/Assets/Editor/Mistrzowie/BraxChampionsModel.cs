using UnityEngine;

// AN OWNER MODEL IN ITS REAL SIZE (owner 13.11 10:48, photo: the sauna cabin of the aquapark ~250 m tall next to the road,
// "nie wiem kto kazał postać"). Measured 16.11: sauna_kabina.glb keeps its geometry in centimetres under a root node
// "BRAXHORN_root_metres" with scale 0.01. BraxMiastoAssety.Postaw (R1, shared) SETS the copy's scale to K, so a root scale
// of the source was thrown away: 1.97 m x 100. Here the source's own root scale is kept (scale = root x K); for every model
// with a root scale of 1 - nearly all - nothing changes. Used by every Miasto Mistrzów generator instead of calling Postaw.
public static class BraxChampionsModel
{
    public static int RootScaled;

    public static GameObject Postaw(Transform parent, GameObject zrodlo, string nazwa, Vector3 p, float yaw, float skalaRealna = 1f, bool statyczny = true, bool cienie = true)
    {
        var o = BraxMiastoAssety.Postaw(parent, zrodlo, nazwa, p, yaw, skalaRealna, statyczny, cienie);
        if (!o || !zrodlo) return o;
        var s = zrodlo.transform.localScale;
        if ((s - Vector3.one).sqrMagnitude > 1e-8f)
        {
            o.transform.localScale = Vector3.Scale(s, o.transform.localScale);
            RootScaled++;
            Debug.Log("CHAMPIONS_MODEL_ROOT_SCALE " + nazwa + " source root scale " + s.ToString("F4") + " kept (real size)");
        }
        return o;
    }
}
