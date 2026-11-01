using System.Collections.Generic;
using UnityEngine;

// AUTO W RUCHU (właściciel 1.11: „żeby się ruch toczył — ludzi i samochodów”). Jedzie po zamkniętej trasie (prawy pas,
// skrzyżowania łukami, rondo przeciwnie do wskazówek zegara), dokładnie po linii trasy (bez zjeżdżania), zwalnia w zakrętach,
// staje przed Braxem i przed autem z przodu. Koła kręcą się wokół osi poprzecznej auta. Daleko od kamery — co 0,2 s.
public sealed class BraxAuto : MonoBehaviour
{
    public Vector3[] trasa;
    public float vmax = 8f, vZakret = 4f, start;
    public Transform[] kola = new Transform[0];
    public float promienKola = .33f;
    public static readonly List<BraxAuto> Wszystkie = new List<BraxAuto>();
    float[] cum; float dl, s, v; int seg;
    Transform gracz;
    static Camera kam;
    float nast;

    void OnEnable() { Wszystkie.Add(this); }
    void OnDisable() { Wszystkie.Remove(this); }

    void Start()
    {
        if (trasa == null || trasa.Length < 2) { enabled = false; return; }
        cum = new float[trasa.Length + 1];
        for (int i = 0; i < trasa.Length; i++) cum[i + 1] = cum[i] + Vector3.Distance(trasa[i], trasa[(i + 1) % trasa.Length]);
        dl = cum[trasa.Length]; s = Mathf.Repeat(start, dl); v = vmax * .5f;
        Ustaw(0f, true);
    }

    void Update()
    {
        if (cum == null) return;
        float dt = Time.deltaTime;
        if (!kam) kam = Camera.main;
        if (kam && (kam.transform.position - transform.position).sqrMagnitude > 120f * 120f)
        { if (Time.time < nast) return; dt = .2f; nast = Time.time + .2f; }
        if (!gracz) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        // prędkość: zakręt przed nami (kąt trasy w ciągu 10 m), przeszkody
        Vector3 f0 = Kierunek(s), f1 = Kierunek(s + 10f);
        float want = Vector3.Angle(f0, f1) > 20f ? vZakret : vmax;
        float przed = Przeszkoda(transform.position, f0);
        if (przed < 12f) want = Mathf.Min(want, Mathf.Max(0f, (przed - 5f) * 1.1f));
        v = Mathf.MoveTowards(v, want, (want < v ? 8f : 2.5f) * dt);
        s += v * dt;
        Ustaw(dt, false);
        if (kola.Length > 0 && v > .01f)
        {
            float deg = v * dt / promienKola * Mathf.Rad2Deg;
            foreach (var k in kola) if (k) k.Rotate(transform.right, deg, Space.World);
        }
    }

    void Ustaw(float dt, bool odRazu)
    {
        s = Mathf.Repeat(s, dl);
        if (s < cum[seg]) seg = 0;
        while (seg < trasa.Length - 1 && s > cum[seg + 1]) seg++;
        Vector3 p0 = trasa[seg], p1 = trasa[(seg + 1) % trasa.Length];
        float t = Mathf.InverseLerp(cum[seg], cum[seg + 1], s);
        transform.position = Vector3.Lerp(p0, p1, t);
        var dir = Kierunek(s);
        if (dir.sqrMagnitude > 1e-4f)
        {
            var q = Quaternion.LookRotation(dir);
            transform.rotation = odRazu ? q : Quaternion.Slerp(transform.rotation, q, Mathf.Clamp01(dt * 8f));
        }
    }

    // kierunek trasy w punkcie s (wygładzony: z odcinka przed i po)
    Vector3 Kierunek(float at)
    {
        at = Mathf.Repeat(at, dl);
        int i = 0; while (i < trasa.Length - 1 && at > cum[i + 1]) i++;
        var d = trasa[(i + 1) % trasa.Length] - trasa[i]; d.y = 0f;
        return d.normalized;
    }

    float Przeszkoda(Vector3 pos, Vector3 fwd)
    {
        float best = 99f;
        if (gracz)
        {
            var g = gracz.position - pos; g.y = 0f; float a = Vector3.Dot(g, fwd);
            if (a > 0f && a < best && Vector3.Cross(fwd, g).magnitude < 2.6f) best = a;
        }
        foreach (var o in Wszystkie)
        {
            if (o == this) continue;
            var g = o.transform.position - pos; g.y = 0f; float a = Vector3.Dot(g, fwd);
            if (a > 0f && a < best && Vector3.Cross(fwd, g).magnitude < 1.9f && Vector3.Dot(o.transform.forward, fwd) > -.2f) best = a;
        }
        return best;
    }
}
