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

    // pkt 15b′ (właściciel 23:14 „wypływasz z drugiej strony”): na końcach północnym i południowym wzdłuż wybrzeża PAS MGŁY
    // 150 m; w jego połowie łódź (z graczem) przenosi się do połowy pasa na drugim końcu — ten sam kurs, prędkość i odległość
    // od brzegu — i wypływa z mgły (krótkie przyciemnienie). Pkt 17: od otwartego morza (wschód) SZTORM: ciemno, deszcz, wiatr,
    // wysokie fale, „Sztorm — zawróć, bo utoniesz”, prąd zawraca łódź; twarda granica 150 m dalej.
    public const float PetlaN = 1150f, PetlaS = -1050f, PasMgly = 150f;
    public static int Rodzaj;                 // 0 brak, 1 sztorm (wschód), 2 za pętlą (awaryjnie), 4 brzeg bez kolizji
    public static float Ciemno;               // do kiedy przyciemnienie po przeniesieniu (Time.unscaledTime)

    /// <summary>0 w akwenie, 0…1 w pasie miękkim, ≥ 1 za twardą granicą; doSrodka — kierunek powrotu (poziomo).</summary>
    public static float Prad(Vector3 p, out Vector3 doSrodka)
    {
        float t = 0f; Rodzaj = 0;
        float te = (p.x - MiekkaX) / (TwardaX - MiekkaX); if (te > t) { t = te; Rodzaj = 1; }
        // za pasem mgły (gdyby przeniesienie zawiodło) — granica awaryjna
        float tn = Mathf.Max((p.z - (PetlaN + PasMgly + 60f)) / 100f, ((PetlaS - PasMgly - 60f) - p.z) / 100f); if (tn > t) { t = tn; Rodzaj = 2; }
        // brzeg poza wodami miasta (teren za granicą bez kolizji) — tylko na morzu na wschód od prostokąta grywalnego miasta
        // (miasto na lądzie x < 170 nigdy nie jest „za granicą akwenu”)
        if (p.x > BraxMiastoGranice.X1 && !WodyMiasta.Contains(new Vector2(p.x, p.z)))
        {
            float odBrzegu = p.x - BraxMiastoGranice.Brzeg(p.z);
            float tb = (BrzegMiekki - odBrzegu) / (BrzegMiekki - BrzegTwardy); if (tb > t) { t = tb; Rodzaj = 4; }
        }
        doSrodka = Srodek - p; doSrodka.y = 0f;
        doSrodka = doSrodka.sqrMagnitude > 1e-4f ? doSrodka.normalized : Vector3.right;
        return Mathf.Max(0f, t);
    }
    public static float Prad(Vector3 p) => Prad(p, out _);

    /// <summary>Gęstość mgły pasa pętli (0 poza pasem, 1 w jego połowie).</summary>
    public static float Mgla(Vector3 p) => Mathf.Max(Mathf.InverseLerp(PetlaN, PetlaN + PasMgly * .5f, p.z), Mathf.InverseLerp(PetlaS, PetlaS - PasMgly * .5f, p.z));

    /// <summary>Pętla wybrzeża: punkt za połową pasa mgły → odpowiadający punkt na drugim końcu (ta sama odległość od brzegu,
    /// ten sam zapas za połową pasa, kurs bez zmian — wypływa się z mgły do środka akwenu). false = bez przeniesienia.</summary>
    public static bool Petla(Vector3 p, out Vector3 cel)
    {
        float zN = PetlaN + PasMgly * .5f, zS = PetlaS - PasMgly * .5f; cel = p;
        if (p.z > zN) cel.z = zS + (p.z - zN);
        else if (p.z < zS) cel.z = zN + (p.z - zS);
        else return false;
        float odBrzegu = p.x - BraxMiastoGranice.Brzeg(p.z);
        cel.x = Mathf.Min(MiekkaX - 20f, BraxMiastoGranice.Brzeg(cel.z) + Mathf.Max(odBrzegu, BrzegMiekki + 10f));
        return true;
    }
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

    // pogoda granic: sztorm (przyciemnienie, deszcz z przewijanej tekstury, wiatr i deszcz z syntezy) i mgła pętli
    Texture2D deszcz, biel; AudioSource wiatr; float sztorm, mgla;
    void Update()
    {
        var cam = Camera.main; if (!cam) return;
        var p = cam.transform.position;
        float pr = BraxAkwen.Prad(p); int rod = BraxAkwen.Rodzaj;
        sztorm = Mathf.MoveTowards(sztorm, rod == 1 ? Mathf.Clamp01(pr * 1.4f) : 0f, Time.deltaTime * .5f);
        mgla = Mathf.MoveTowards(mgla, BraxAkwen.Mgla(p), Time.deltaTime * 2f);
        if (sztorm > .01f && !wiatr) Dzwiek();
        if (wiatr) { wiatr.volume = sztorm * .85f; if (sztorm > .01f && !wiatr.isPlaying) wiatr.Play(); else if (sztorm <= .01f && wiatr.isPlaying) wiatr.Stop(); }
    }
    void Dzwiek()
    {   // wiatr (szum brązowy z falowaniem) + deszcz (szum biały w pakietach) — synteza, 6 s w pętli bez przeskoku
        int hz = 22050, n = hz * 6, F = 2000; var src = new float[n + F]; var r = new System.Random(17); float b = 0f, lp = 0f;
        for (int i = 0; i < n + F; i++)
        {
            float w = (float)(r.NextDouble() * 2 - 1); b = Mathf.Clamp(b + w * .02f, -1f, 1f) * .998f; lp += (w - lp) * .35f;
            float fal = .6f + .4f * Mathf.Sin(i * Mathf.PI * 2f * 3f / n) * Mathf.Sin(i * Mathf.PI * 2f * 5f / n);
            src[i] = b * .9f * fal + lp * .22f;
        }
        var d = new float[n];   // pętla bez przeskoku: początek przenikany z próbkami po końcu
        for (int i = 0; i < n; i++) d[i] = i < F ? Mathf.Lerp(src[n + i], src[i], (float)i / F) : src[i];
        var clip = AudioClip.Create("Sztorm: wiatr i deszcz", n, 1, hz, false); clip.SetData(d, 0);
        wiatr = gameObject.AddComponent<AudioSource>(); wiatr.clip = clip; wiatr.loop = true; wiatr.spatialBlend = 0f; wiatr.playOnAwake = false; wiatr.volume = 0f;
    }
    void Tekstury()
    {
        biel = new Texture2D(2, 2) { name = "biel" }; biel.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); biel.Apply();
        int W = 128, H = 256; deszcz = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "deszcz", wrapMode = TextureWrapMode.Repeat };
        var px = new Color[W * H]; var r = new System.Random(3);
        for (int k = 0; k < 140; k++)
        {
            int x0 = r.Next(W), y0 = r.Next(H), L = 14 + r.Next(26); float a = .25f + (float)r.NextDouble() * .35f;
            for (int j = 0; j < L; j++) { int x = (x0 + j / 4) % W, y = (y0 + j) % H; px[y * W + x] = new Color(.85f, .88f, .92f, a); }
        }
        deszcz.SetPixels(px); deszcz.Apply();
    }

    void OnGUI()
    {
        if (!biel) Tekstury();
        var full = new Rect(0, 0, Screen.width, Screen.height);
        if (sztorm > .01f)
        {
            GUI.color = new Color(.08f, .1f, .13f, .5f * sztorm); GUI.DrawTexture(full, biel);
            float t = Time.time; GUI.color = new Color(1, 1, 1, .75f * sztorm);
            GUI.DrawTextureWithTexCoords(full, deszcz, new Rect(t * .9f, t * 3.2f, Screen.width / 256f, Screen.height / 512f));
            GUI.DrawTextureWithTexCoords(full, deszcz, new Rect(-t * .6f + .37f, t * 4.1f, Screen.width / 180f, Screen.height / 360f));
        }
        if (mgla > .01f) { GUI.color = new Color(.86f, .88f, .9f, Mathf.SmoothStep(0f, .97f, mgla)); GUI.DrawTexture(full, biel); }
        if (Time.unscaledTime < BraxAkwen.Ciemno) { GUI.color = new Color(.85f, .87f, .9f, 1f); GUI.DrawTexture(full, biel); }
        GUI.color = Color.white;
        if (Time.unscaledTime > BraxAkwen.CzasKomunikatu) return;
        if (styl == null) { styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true }; }
        float s = Mathf.Min(Screen.width, Screen.height); styl.fontSize = Mathf.RoundToInt(s * .045f);
        var r = new Rect(Screen.width * .1f, Screen.height * .18f, Screen.width * .8f, s * .12f);
        string tekst = BraxAkwen.Rodzaj == 1 ? (BraxAkwen.Ostatni >= 1f ? "Sztorm — dalej nie przepłyniesz, zawróć" : "Sztorm — zawróć, bo utoniesz")
                     : BraxAkwen.Ostatni >= 1f ? "Dalej nie da się płynąć — zawróć" : "Silny prąd — dalej nie płyń, zawróć";
        GUI.color = new Color(0, 0, 0, .55f); var rr = r; rr.x += 2; rr.y += 2; GUI.Label(rr, tekst, styl);
        GUI.color = new Color(1f, .86f, .3f, 1f); GUI.Label(r, tekst, styl); GUI.color = Color.white;
    }
}
