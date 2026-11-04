using UnityEngine;

// EFEKTY KLUBU (dyskoteka osiedla, build 45 runda 2 — „DJ premium”): ściana LED za DJ-em z wzorem liczonym w kodzie
// (tekstura 48 × 24, odświeżana 12 razy/s: korektor słupkowy, fala barw, pierścienie — zmiana co 12 s), kula dyskotekowa
// (obrót), wachlarze laserów i stożki ruchomych głów (omiatanie). Bez cząsteczek (moduł wyłączony). Liczy się tylko, gdy
// klub otwarty (BraxGodzinyOtwarcia) i kamera bliżej niż promień od środka sali — dalej zero kosztu (telefon).
public sealed class BraxKlubEfekty : MonoBehaviour
{
    public Renderer ekranLed;
    public Transform kula;
    public Transform[] lasery, glowice;
    public GameObject[] tylkoGdyOtwarte;
    public Vector3 srodek;
    public float promien = 40f;

    const int W = 48, H = 24;
    Texture2D tex; Color32[] px; float nastLed; Quaternion[] bazaL, bazaG; bool widoczne = true;
    // 3.11 15:18/15:58 (właściciel): lasery i stożki MRUGAJĄ w zmiennym tempie — wspólny wzór BraxStrob (też NOVA w mieście)
    BraxStrob strob;

    void Start()
    {
        if (ekranLed)
        {
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ściana LED (wzór z kodu)" };
            px = new Color32[W * H];
            var m = ekranLed.material;
            m.mainTexture = tex;
            if (m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", tex); m.EnableKeyword("_EMISSION"); }
            Rysuj(0f);
        }
        bazaL = Baza(lasery); bazaG = Baza(glowice);
        strob = new BraxStrob();
        if (lasery != null) foreach (var x in lasery) strob.Dodaj(BraxStrob.Wiazki(x, "wiązka", "stożek"));
        if (glowice != null) foreach (var x in glowice) strob.Dodaj(BraxStrob.Wiazki(x, "wiązka", "stożek"));
    }
    static Quaternion[] Baza(Transform[] t)
    {
        if (t == null) return new Quaternion[0];
        var r = new Quaternion[t.Length]; for (int i = 0; i < t.Length; i++) r[i] = t[i] ? t[i].localRotation : Quaternion.identity; return r;
    }

    void Update()
    {
        var kam = BraxLudzie.Kamera;
        bool otwarte = BraxGodzinyOtwarcia.Otwarte("dyskoteka") && (!kam || (kam.position-srodek).sqrMagnitude<=promien*promien);
        if (otwarte != widoczne)
        {
            widoczne = otwarte;
            if (tylkoGdyOtwarte != null) foreach (var g in tylkoGdyOtwarte) if (g) g.SetActive(otwarte);
        }
        if (!otwarte) return;
        float t = Time.time, dt = Time.deltaTime;
        if (kula) kula.Rotate(0f, 24f * dt, 0f, Space.World);
        if (lasery != null)
            for (int i = 0; i < lasery.Length; i++)
                if (lasery[i])
                {
                    lasery[i].localRotation = bazaL[i] * Quaternion.Euler(Mathf.Sin(t * .9f + i * 1.7f) * 6f, Mathf.Sin(t * .55f + i * 2.3f) * 28f, 0f);
                    // Keep the entire fan inside the room throughout its sweep.
                    float length=16f;
                    foreach(float angle in FanAngles)
                    {
                        var d=lasery[i].rotation*Quaternion.Euler(0,angle,0)*Vector3.forward;
                        var p=lasery[i].position;
                        length=Mathf.Min(length,Exit(p.x,d.x,66.25f,105.7f));
                        length=Mathf.Min(length,Exit(p.y,d.y,.2f,6.7f));
                        length=Mathf.Min(length,Exit(p.z,d.z,-.7f,28.7f));
                    }
                    if(lasery[i].childCount>0)lasery[i].GetChild(0).localScale=Vector3.one*Mathf.Clamp01(length/16f);
                }
        if (glowice != null)
            for (int i = 0; i < glowice.Length; i++)
                if (glowice[i]) glowice[i].localRotation = bazaG[i] * Quaternion.Euler(Mathf.Sin(t * .7f + i * 2.1f) * 24f, 0f, Mathf.Cos(t * .5f + i * 1.3f) * 24f);
        strob.Krok(t, dt);
        if (tex && t >= nastLed) { nastLed = t + 1f / 12f; Rysuj(t); }
    }

    static readonly float[] FanAngles={-18f,-12f,-6f,0f,6f,12f,18f};
    static float Exit(float p,float d,float min,float max)=>Mathf.Abs(d)<1e-5f?float.PositiveInfinity:Mathf.Max(0f,((d>0f?max:min)-p)/d);

    // wzór ściany LED (0 = korektor, 1 = fala barw, 2 = pierścienie od środka)
    void Rysuj(float t)
    {
        int tryb = (int)(t / 12f) % 3;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color c;
                if (tryb == 0)
                {
                    int kol = x / 2;
                    float h = (Mathf.Sin(kol * .9f + t * 3.1f) * .5f + .5f) * (Mathf.Sin(kol * .37f - t * 1.7f) * .35f + .65f) * H;
                    bool zapal = (x & 1) == 0 && y < h;
                    c = zapal ? Color.HSVToRGB(Mathf.Lerp(.33f, 0f, y / (float)H), 1f, 1f) : Color.black;
                }
                else if (tryb == 1)
                {
                    float hue = Mathf.Repeat(x / (float)W * .8f + y / (float)H * .3f - t * .25f, 1f);
                    c = Color.HSVToRGB(hue, .9f, .55f + .45f * Mathf.Sin(x * .3f + t * 2f));
                }
                else
                {
                    float dx = x - W * .5f, dy = (y - H * .5f) * 1.6f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float v = Mathf.Sin(d * .6f - t * 4f);
                    c = v > .35f ? Color.HSVToRGB(Mathf.Repeat(d * .02f + t * .1f, 1f), 1f, 1f) : Color.black;
                }
                px[y * W + x] = c;
            }
        tex.SetPixels32(px); tex.Apply(false);
    }

    void OnDestroy() { if (tex) Destroy(tex); }
}
