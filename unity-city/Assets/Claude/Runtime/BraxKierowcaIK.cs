using System;
using UnityEngine;

// Two-bone IK: rotations only, never stretches Patryk's skeleton.
[Serializable]
public sealed class BraxRekaKierowcy
{
    public Transform ramie, lokiec, dlon, uchwyt;
    public Vector3 srodekDloni;
    public Quaternion orientacjaDloni = Quaternion.identity;
    public float strona;
    public Transform[] palce = new Transform[0];
    public Quaternion[] chwyty = new Quaternion[0];

    public bool Gotowa => ramie && lokiec && dlon && uchwyt;

    public bool Ustaw(Transform auto, float waga)
    {
        if (!Gotowa || waga <= 0f) return false;
        var rot = auto.rotation * orientacjaDloni;
        var cel = uchwyt.position - rot * Vector3.Scale(srodekDloni, dlon.lossyScale);
        var punkt = Vector3.Lerp(dlon.position, cel, waga);
        bool ok = BraxKierowcaIK.Rozwiaz(ramie, lokiec, dlon, punkt,
            ramie.position + auto.right * strona * .45f - auto.up * .65f);
        dlon.rotation = Quaternion.Slerp(dlon.rotation, rot, waga);
        for(int i=0;i<palce.Length && i<chwyty.Length;i++)
            if(palce[i])palce[i].localRotation=Quaternion.Slerp(palce[i].localRotation,chwyty[i],waga);
        return ok;
    }
}

public static class BraxKierowcaIK
{
    public static bool Rozwiaz(Transform a, Transform b, Transform c, Vector3 cel, Vector3 biegun)
    {
        float l1 = Vector3.Distance(a.position, b.position), l2 = Vector3.Distance(b.position, c.position);
        var delta = cel - a.position; float d = delta.magnitude;
        if (l1 < 1e-5f || l2 < 1e-5f || d < 1e-5f) return false;
        bool dostepny = d <= l1 + l2 - .001f && d >= Mathf.Abs(l1 - l2) + .001f;
        var os = delta / d;
        d = Mathf.Clamp(d, Mathf.Abs(l1 - l2) + .001f, l1 + l2 - .001f);
        var bok = Vector3.ProjectOnPlane(biegun - a.position, os);
        if (bok.sqrMagnitude < 1e-8f) bok = Vector3.ProjectOnPlane(b.position - a.position, os);
        if (bok.sqrMagnitude < 1e-8f) bok = Vector3.Cross(os, Mathf.Abs(os.y) < .9f ? Vector3.up : Vector3.right);
        float x = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
        var kolano = a.position + os * x + bok.normalized * Mathf.Sqrt(Mathf.Max(0, l1 * l1 - x * x));
        a.rotation = Quaternion.FromToRotation(b.position - a.position, kolano - a.position) * a.rotation;
        b.rotation = Quaternion.FromToRotation(c.position - b.position, a.position + os * d - b.position) * b.rotation;
        return dostepny;
    }
}
