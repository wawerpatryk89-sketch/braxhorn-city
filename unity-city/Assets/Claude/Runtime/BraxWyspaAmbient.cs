using UnityEngine;

// Trzy głosy na wyspę; referencje do sceny wyspy, bez DontDestroyOnLoad.
public sealed class BraxWyspaAmbient : MonoBehaviour
{
    public AudioSource morze, wiatr, kibice;
    public Vector3[] brzeg;
    public Vector3 srodekAreny;
    public float apotemaAreny = 21.5f;
    public float podlogaAreny, wysokoscAreny = 13.4f;
    Transform sluchacz;
    float nastepne, falaCel, wiatrCel, kibiceCel;
    Vector3 punktMorza;

    void Update()
    {
        if (!sluchacz && Camera.main) sluchacz = Camera.main.transform;
        if (!sluchacz || !morze || !wiatr || !kibice || brzeg == null || brzeg.Length < 3) return;
        if (Time.unscaledTime >= nastepne)
        {
            nastepne = Time.unscaledTime + .15f;
            var p = sluchacz.position; float best = float.PositiveInfinity;
            for (int i = 0; i < brzeg.Length; i++)
            {
                var a = brzeg[i]; var b = brzeg[(i + 1) % brzeg.Length]; var ab = b - a;
                var q = a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(.001f, ab.sqrMagnitude));
                float d = (q - p).sqrMagnitude;
                if (d < best) { best = d; punktMorza = q; }
            }
            bool wewnatrz = Wewnatrz(p);
            float tlumienie = wewnatrz ? .045f : 1f;
            falaCel = .38f * Mathf.Clamp01(1f - Mathf.Sqrt(best) / 75f) * tlumienie;
            wiatrCel = (wewnatrz ? .004f : .065f) * Mathf.Clamp01(1f - Mathf.Sqrt(best) / 180f);
            kibiceCel = wewnatrz ? .18f : .025f * Mathf.Clamp01(1f - (p - srodekAreny).magnitude / 50f);
        }
        morze.transform.position = Vector3.Lerp(morze.transform.position, punktMorza, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f));
        Glos(morze, falaCel); Glos(wiatr, wiatrCel); Glos(kibice, kibiceCel);
    }

    public bool Wewnatrz(Vector3 p)
    {
        if (p.y < podlogaAreny - .2f || p.y > podlogaAreny + wysokoscAreny) return false;
        var d = p - srodekAreny;
        float x = Mathf.Abs(d.x), z = Mathf.Abs(d.z);
        return Mathf.Max(x, z) < apotemaAreny && (x + z) * .70710678f < apotemaAreny;
    }
    static void Glos(AudioSource src, float cel)
    {
        src.volume = Mathf.MoveTowards(src.volume, cel, Time.unscaledDeltaTime * .2f);
        if (src.volume > .001f && !src.isPlaying) src.Play();
        else if (src.volume <= .001f && src.isPlaying) src.Stop();
    }
    void OnDisable()
    {
        if (morze) morze.Stop(); if (wiatr) wiatr.Stop(); if (kibice) kibice.Stop();
    }
}
