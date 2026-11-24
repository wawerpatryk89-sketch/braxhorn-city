using System.Threading.Tasks;
using UnityEngine;

// Owner 24.11 01:31 ("Wykonałeś mi te dźwięki z plaży?"): the beach's sounds at every real sea of the region (the city
// beach, the villa island, the seafront villa's private beach). One component per sea, its waterline baked by the build
// (BraxChampionsPlaza.SzumMorza: the shore field's waterline every ~8 units):
//  - the surf's roar from the waterline point nearest the camera (one looping 3D source gliding along the shore, wide);
//  - each wave heard as it reaches the shore IN TIME WITH THE PICTURE: the sea shader's wave reaches the waterline when
//    frac(t / period + Faza(w)) wraps to 0 (ChampionsMorze: _Time.y, _Fala.y, Faza with the sea's _WzdluzZ), so the
//    nearest point and its two neighbours along the shore (+-4 points, ~32 units) each start the wave clip Uderzenie s
//    before their own arrival; the field's side fade (_Boki, the seafront) softens them as it softens the surf;
//  - gulls now and then (6-22 s) over the water near you, from 10-28 units up;
//  - nothing beyond zasieg (the sources stop); the clips are made once, on a worker thread (BraxChampionsMorzeDzwiek).
public sealed class BraxChampionsSzumMorza : MonoBehaviour
{
    public Vector3[] brzeg = new Vector3[0];         // the waterline (world), every ~8 units
    public float[] sila = new float[0];              // the surf's strength at each point (1, less in a field's side fade)
    public float wzdluzZ;                             // the sea's _WzdluzZ (0 a shore along x, 1 an island)
    public float dlugosc = 20f, okres = 7f;           // the sea's _Fala.x, _Fala.y
    public float zasieg = 170f;                       // heard from up to (units)
    public float glosnosc = .85f;

    static Task<float[][]> praca;
    static AudioClip szumKlip, falaKlip; static AudioClip[] mewyKlipy;

    AudioSource tlo, mewa; AudioSource[] fale; int kolejna;
    readonly float[] ostatnia = { -1f, -1f, -1f }; readonly float[] grala = { -99f, -99f, -99f };
    int naj = -1; float szukaj, mewaZa = 8f; bool gra; Vector3 tloPoz; Transform kamera; float kameraSzukaj;

    void Start()
    {
        if (brzeg.Length == 0) { enabled = false; return; }
        if (praca == null && szumKlip == null)
            praca = Task.Run(() => new[] { BraxChampionsMorzeDzwiek.Szum(), BraxChampionsMorzeDzwiek.Fala(),
                BraxChampionsMorzeDzwiek.Mewa(0), BraxChampionsMorzeDzwiek.Mewa(1), BraxChampionsMorzeDzwiek.Mewa(2), BraxChampionsMorzeDzwiek.Mewa(3) });
    }

    static bool Gotowe()
    {
        if (szumKlip) return true;
        if (praca == null || !praca.IsCompleted) return false;
        if (praca.IsFaulted) { Debug.LogWarning("MISTRZOWIE_SZUM_MORZA synthesis failed: " + praca.Exception); praca = null; return false; }
        var d = praca.Result; praca = null;
        AudioClip Klip(string n, float[] x) { var c = AudioClip.Create(n, x.Length, 1, BraxChampionsMorzeDzwiek.SR, false); c.SetData(x, 0); return c; }
        szumKlip = Klip("morze: szum", d[0]); falaKlip = Klip("morze: fala", d[1]);
        mewyKlipy = new AudioClip[4]; for (int k = 0; k < 4; k++) mewyKlipy[k] = Klip("mewa " + k, d[2 + k]);
        return true;
    }

    AudioSource Zrodlo(string n, float min, float max, float spread)
    {
        var g = new GameObject(n); g.transform.SetParent(transform, false);
        var s = g.AddComponent<AudioSource>();
        s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = min; s.maxDistance = max;
        s.dopplerLevel = 0f; s.spread = spread; s.playOnAwake = false; s.priority = 90;
        return s;
    }

    // as ChampionsMorze's Faza: each wave different along the shore
    static float Wzdluz(float x) => Mathf.Sin(x * .0131f) * .35f + Mathf.Sin(x * .0047f + 1.7f) * .55f;
    float Faza(Vector3 w) => Wzdluz(w.x) + wzdluzZ * Wzdluz(w.z * 1.13f + 77f);

    void Update()
    {
        if (!kamera && Time.unscaledTime >= kameraSzukaj) { kameraSzukaj = Time.unscaledTime + 1f; var c = Camera.main; if (c) kamera = c.transform; }
        if (!kamera) return;
        var l = kamera.position;
        if (Time.unscaledTime >= szukaj || naj < 0)
        {
            szukaj = Time.unscaledTime + .25f; float best = float.MaxValue;
            for (int i = 0; i < brzeg.Length; i++)
            {
                float dx = brzeg[i].x - l.x, dz = brzeg[i].z - l.z, q = dx * dx + dz * dz;
                if (q < best) { best = q; naj = i; }
            }
        }
        var p = brzeg[naj];
        float od = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(l.x, l.z));
        if (od > zasieg + 20f || !Gotowe())
        {
            if (gra) { tlo.Stop(); mewa.Stop(); foreach (var f in fale) f.Stop(); gra = false; }
            return;
        }
        if (!tlo)
        {
            tlo = Zrodlo("szum morza", 18f, zasieg, 140f); tlo.clip = szumKlip; tlo.loop = true; tlo.priority = 64;
            fale = new AudioSource[4]; for (int k = 0; k < fale.Length; k++) fale[k] = Zrodlo("fala " + k, 12f, zasieg, 90f);
            mewa = Zrodlo("mewy", 15f, zasieg * 1.2f, 30f);
            tloPoz = p;
        }
        if (!gra)
        {
            tlo.transform.position = tloPoz = p;
            tlo.time = Random.Range(0f, szumKlip.length * .9f); tlo.Play(); gra = true;
        }
        // the roar glides along the shore (no jump when the nearest point changes)
        tloPoz = Vector3.MoveTowards(tloPoz, p, 40f * Time.deltaTime);
        tlo.transform.position = tloPoz;
        float daleko = 1f - Mathf.SmoothStep(0f, 1f, (od - (zasieg - 30f)) / 50f);                  // no step where it stops
        tlo.volume = glosnosc * .5f * daleko * (sila.Length == brzeg.Length ? Mathf.Lerp(.5f, 1f, sila[naj]) : 1f);

        // the waves reaching the shore, in time with the sea's picture
        float t = Time.timeSinceLevelLoad, prog = 1f - BraxChampionsMorzeDzwiek.Uderzenie / okres;
        for (int k = 0; k < 3; k++)
        {
            int i = k == 0 ? naj : k == 1 ? naj - 4 : naj + 4;
            if (i < 0 || i >= brzeg.Length) { ostatnia[k] = -1f; continue; }
            var q = brzeg[i];
            if (k > 0 && (q - p).sqrMagnitude > 45f * 45f) { ostatnia[k] = -1f; continue; }      // not the same stretch of shore
            float ph = t / okres + Faza(q), f = ph - Mathf.Floor(ph);
            bool przeszla = ostatnia[k] >= 0f && ostatnia[k] < prog && f >= prog;
            ostatnia[k] = f;
            if (!przeszla || t - grala[k] < okres * .6f) continue;
            grala[k] = t;
            float s = sila.Length == brzeg.Length ? sila[i] : 1f;
            if (s < .05f) continue;
            var src = fale[kolejna]; kolejna = (kolejna + 1) % fale.Length;
            src.transform.position = q; src.clip = falaKlip; src.pitch = Random.Range(.94f, 1.06f);
            src.volume = glosnosc * s * daleko * (k == 0 ? .9f : .45f);
            src.Play();
        }

        // gulls now and then, over the water near you
        mewaZa -= Time.deltaTime;
        if (mewaZa <= 0f)
        {
            mewaZa = Random.Range(6f, 22f);
            if (od < zasieg * .85f)
            {
                var wzdluz = naj + 1 < brzeg.Length ? brzeg[naj + 1] - p : p - brzeg[Mathf.Max(naj - 1, 0)];
                wzdluz.y = 0f; wzdluz = wzdluz.sqrMagnitude > 1e-4f ? wzdluz.normalized : Vector3.right;
                var morze = new Vector3(p.x - l.x, 0f, p.z - l.z); morze = morze.sqrMagnitude > 1f ? morze.normalized : Vector3.Cross(wzdluz, Vector3.up);
                mewa.transform.position = p + morze * Random.Range(8f, 60f) + wzdluz * Random.Range(-50f, 50f) + Vector3.up * Random.Range(10f, 28f);
                mewa.clip = mewyKlipy[Random.Range(0, mewyKlipy.Length)]; mewa.pitch = Random.Range(.92f, 1.1f);
                mewa.volume = glosnosc * daleko * Random.Range(.35f, .55f);
                mewa.Play();
            }
        }
    }

    void OnDisable()
    {
        if (gra) { tlo.Stop(); mewa.Stop(); foreach (var f in fale) f.Stop(); gra = false; }
    }
}
