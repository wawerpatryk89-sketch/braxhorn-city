using System.Collections.Generic;
using UnityEngine;

// AKWEN MIASTA (pkt 15, właściciel 2.11 23:13: „popłynąłem do końca morza i wypadłem z terenu”).
//  a) MORZE BEZ KRAWĘDZI: BraxMorzeZaKamera — siatka wody 3.2 × 3.2 km przesuwana za kamerą skokowo co 100 m (siatka wyrównana
//     do wody portu x 84…884, z −340…460 — tej nie dubluje, komórki na lądzie pominięte). Shader wody liczy fale ze świata
//     (SwellPhase(world.xz)), więc przesunięcie nie „przesuwa” fal. Kamera ma zasięg 600 m — końca wody nie widać nigdy.
//  b) GRANICA MIĘKKA: na wschód (otwarte morze) od x 2300, wzdłuż wybrzeża na północ od z 1150 i na południe od z −1050,
//     przy brzegu poza wodami miasta (teren za granicą nie ma kolizji) 70 m od linii brzegu: komunikat „Silny prąd — dalej
//     nie płyń, zawróć”, łódź zwalnia i jest łagodnie obracana do środka akwenu; twarda granica 150 m dalej (55 m przy brzegu).
//  c) SIATKA BEZPIECZEŃSTWA (BraxGymPlayer): ostatnia bezpieczna pozycja co 1 s (na gruncie albo na łodzi w akwenie);
//     spadek w pustkę / poza świat → powrót tam (log BRAXHORN_SIATKA_BEZP), zamiast przeniesienia na start.
public static class BraxAkwen
{
    public const float WodaY = -1.7f;
    public const float MiekkaX = 2300f, TwardaX = 2450f, MiekkaZN = 1150f, TwardaZN = 1300f, MiekkaZS = -1050f, TwardaZS = -1200f;
    public const float BrzegMiekki = 70f, BrzegTwardy = 15f;
    public static readonly Vector3 Srodek = new Vector3(900f, WodaY, 40f);
    static readonly Rect WodyMiasta = Rect.MinMaxRect(84f, -340f, 884f, 470f);   // tu brzeg mają nabrzeża i plaże z kolizjami
    public static float Ostatni;   // ostatnia wartość prądu (do komunikatu)
    public static float CzasKomunikatu;

    /// <summary>0 w akwenie, 0…1 w pasie miękkim, ≥ 1 za twardą granicą; doSrodka — kierunek powrotu (poziomo).</summary>
    public static float Prad(Vector3 p, out Vector3 doSrodka)
    {
        float t = 0f;
        t = Mathf.Max(t, (p.x - MiekkaX) / (TwardaX - MiekkaX));
        t = Mathf.Max(t, (p.z - MiekkaZN) / (TwardaZN - MiekkaZN));
        t = Mathf.Max(t, (MiekkaZS - p.z) / (MiekkaZS - TwardaZS));
        // brzeg poza wodami miasta (teren za granicą bez kolizji) — tylko na morzu na wschód od prostokąta grywalnego miasta
        // (miasto na lądzie x < 170 nigdy nie jest „za granicą akwenu”)
        if (p.x > BraxMiastoGranice.X1 && !WodyMiasta.Contains(new Vector2(p.x, p.z)))
        {
            float odBrzegu = p.x - BraxMiastoGranice.Brzeg(p.z);
            t = Mathf.Max(t, (BrzegMiekki - odBrzegu) / (BrzegMiekki - BrzegTwardy));
        }
        doSrodka = Srodek - p; doSrodka.y = 0f;
        doSrodka = doSrodka.sqrMagnitude > 1e-4f ? doSrodka.normalized : Vector3.right;
        return Mathf.Max(0f, t);
    }
    public static float Prad(Vector3 p) => Prad(p, out _);
    public static void Komunikat(float t) { Ostatni = t; CzasKomunikatu = Time.unscaledTime + .6f; }
}

/// <summary>Siatka wody przesuwana za kamerą (morze bez krawędzi) + komunikat granicy akwenu (IMGUI).</summary>
public sealed class BraxMorzeZaKamera : MonoBehaviour
{
    public Material woda;
    public float komorka = 100f; public int pol = 16;            // 32 × 32 komórki po 100 m
    public Rect bezWody = Rect.MinMaxRect(84f, -340f, 884f, 460f); // woda portu (osobne siatki)
    Mesh mesh; Vector2Int ostatnie = new Vector2Int(int.MinValue, int.MinValue);
    readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector3> n = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> t = new List<int>();
    GUIStyle styl;

    void Start()
    {
        if (!woda) { enabled = false; return; }
        mesh = new Mesh { name = "Morze za kamerą" }; mesh.MarkDynamic();
        var mf = gameObject.GetComponent<MeshFilter>(); if (!mf) mf = gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
        var mr = gameObject.GetComponent<MeshRenderer>(); if (!mr) mr = gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = woda;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        transform.position = Vector3.zero; transform.rotation = Quaternion.identity; transform.localScale = Vector3.one;
    }

    void LateUpdate()
    {
        if (!mesh) return;
        var cam = Camera.main; if (!cam) return;
        var p = cam.transform.position;
        // siatka wyrównana do krawędzi wody portu (84, −340), żeby komórki dokładnie ją omijały (bez nakładania)
        var k = new Vector2Int(Mathf.FloorToInt((p.x - bezWody.xMin) / komorka), Mathf.FloorToInt((p.z - bezWody.yMin) / komorka));
        if (k == ostatnie) return;
        ostatnie = k; Przebuduj(k);
    }

    void Przebuduj(Vector2Int k)
    {
        v.Clear(); n.Clear(); uv.Clear(); t.Clear();
        int N = pol * 2; float x0 = bezWody.xMin + (k.x - pol) * komorka, z0 = bezWody.yMin + (k.y - pol) * komorka;
        for (int i = 0; i <= N; i++) for (int j = 0; j <= N; j++) { var q = new Vector3(x0 + i * komorka, BraxAkwen.WodaY, z0 + j * komorka); v.Add(q); n.Add(Vector3.up); uv.Add(new Vector2(q.x, q.z)); }
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                float cx0 = x0 + i * komorka, cz0 = z0 + j * komorka, cx1 = cx0 + komorka, cz1 = cz0 + komorka;
                if (cx0 >= bezWody.xMin - .01f && cx1 <= bezWody.xMax + .01f && cz0 >= bezWody.yMin - .01f && cz1 <= bezWody.yMax + .01f) continue;   // woda portu
                bool lad = true;   // komórka cała na lądzie (rogi i środek na zachód od linii brzegu z zapasem) — bez wody
                foreach (var c in new[] { new Vector2(cx0, cz0), new Vector2(cx1, cz0), new Vector2(cx0, cz1), new Vector2(cx1, cz1), new Vector2((cx0 + cx1) * .5f, (cz0 + cz1) * .5f) })
                    if (c.x > BraxMiastoGranice.Brzeg(c.y) - 30f) { lad = false; break; }
                if (lad) continue;
                int a = i * (N + 1) + j, b = a + N + 1;
                t.Add(a); t.Add(a + 1); t.Add(b + 1); t.Add(a); t.Add(b + 1); t.Add(b);
            }
        mesh.Clear(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        var b0 = new Bounds(new Vector3(x0 + N * komorka * .5f, BraxAkwen.WodaY, z0 + N * komorka * .5f), new Vector3(N * komorka, 1f, N * komorka));
        mesh.bounds = b0;
    }

    void OnGUI()
    {
        if (Time.unscaledTime > BraxAkwen.CzasKomunikatu) return;
        if (styl == null) { styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true }; }
        float s = Mathf.Min(Screen.width, Screen.height); styl.fontSize = Mathf.RoundToInt(s * .045f);
        var r = new Rect(Screen.width * .1f, Screen.height * .18f, Screen.width * .8f, s * .12f);
        string tekst = BraxAkwen.Ostatni >= 1f ? "Dalej nie da się płynąć — zawróć" : "Silny prąd — dalej nie płyń, zawróć";
        GUI.color = new Color(0, 0, 0, .55f); var rr = r; rr.x += 2; rr.y += 2; GUI.Label(rr, tekst, styl);
        GUI.color = new Color(1f, .86f, .3f, 1f); GUI.Label(r, tekst, styl); GUI.color = Color.white;
    }
}
