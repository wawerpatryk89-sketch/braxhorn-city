using System;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    static void DopasujKabine(BraxMieszkaniec osoba, Transform fotel)
    {
        var kabina = fotel.GetComponent<BraxFotelAuta>();
        if (!kabina || !osoba.anim) return;
        float top = 0;
        var baked = new Mesh();
        try
        {
            foreach (string name in osoba.klipy)
            {
                var clip = osoba.anim.GetClip(name); if (!clip) continue;
                for (int frame = 0; frame < 8; frame++)
                {
                    clip.SampleAnimation(osoba.anim.gameObject, clip.length * frame / 8f);
                    // 2.11 dyspozytor: głowa z pozycji kości (świat) + margines — BakeMesh+TransformPoint liczył skalę dwa razy
                    foreach (var renderer in osoba.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        foreach (var b in renderer.bones)
                            if (b) top = Mathf.Max(top, Vector3.Dot(b.position - fotel.position, fotel.up) + .1f * K);
                }
            }
            // Lower the seating position before considering a small, uniform stature adjustment.
            float lower=Mathf.Min(Mathf.Max(0,top-kabina.przeswit+.04f*K),Mathf.Min(.18f*K,Mathf.Max(0,fotel.localPosition.y-.22f*K)));
            osoba.transform.position-=fotel.up*lower;top-=lower;
            float scale = Mathf.Min(1f, (kabina.przeswit-.04f*K)/Mathf.Max(.01f,top));
            if (scale < .90f) { Debug.LogWarning("BRAXHORN_KABINA DO POPRAWKI: kabina zbyt niska " + fotel.parent.name + " głowa=" + top.ToString("F2") + " prześwit=" + kabina.przeswit.ToString("F2") + " — osoba bez zmniejszania"); scale = 1f; }
            osoba.transform.localScale *= scale;
            osoba.wzrost *= scale;
            var first = osoba.anim.GetClip(osoba.klipy[0]); if (first) first.SampleAnimation(osoba.anim.gameObject, 0);
            var lod = osoba.GetComponent<LODGroup>(); if (lod) lod.RecalculateBounds();
            Debug.Log("BRAXHORN_KABINA " + fotel.parent.name + " osoba=" + osoba.name + " głowa=" + (top*scale).ToString("F3") + " prześwit=" + kabina.przeswit.ToString("F3") + " skala=" + scale.ToString("F3"));
        }
        finally { UnityEngine.Object.DestroyImmediate(baked); }
    }
}
