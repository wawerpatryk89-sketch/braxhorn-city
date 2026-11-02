using UnityEngine;

// BAR DYSKOTEKI (build 45 runda 2, E30 dopisek): na każdym stanowisku 4–6 OSOBNYCH butelek (osobne obiekty, nie w scalonej
// siatce), co 20–40 s — tylko gdy gracz bliżej niż 14 m — sekwencja nalewania: barman sięga (poza ramienia w LateUpdate,
// po animacji: obrót ramienia w stronę butelki), butelka unosi się nad szklankę, przechyla, strumień (walec) do szklanki,
// szklanka się napełnia, butelka wraca na miejsce; pełna szklanka stoi 8 s. Przycisk AKCJA przy ladzie: „ZAMÓW” —
// najbliższy barman odpowiada i nalewa od razu. Poza ramienia: jeśli animacja jej nie nadpisała (postać bez animacji
// poza promieniem BraxLudzie), najpierw przywracana — obrót się nie kumuluje.
public sealed class BraxBarDyskoteki : MonoBehaviour, IBraxInterakcja
{
    [System.Serializable]
    public sealed class Stanowisko
    {
        public Transform barman, ramie, dlon;
        public Transform[] butelki;                 // korzeń butelki = środek dna
        public Transform szklanka, ciecz, strumien; // szklanka: korzeń = środek dna; ciecz: walec 0–1 (y); strumień: walec 0–1 (y)
        public float butelkaWys = .4f, szklankaWys = .12f, cieczSkalaY = .1f, strumienR = .006f;
        [System.NonSerialized] public float t = -1f, nast, pelna, w;
        [System.NonSerialized] public int b;
        [System.NonSerialized] public Vector3 bPoz;
        [System.NonSerialized] public Quaternion bRot, bazaRamie, mojaRamie;
        [System.NonSerialized] public bool ik;
    }

    public Stanowisko[] stanowiska;
    public Bounds strefaZamow;
    public float blisko = 14f;
    public string[] odpowiedzi = { "Już się robi!", "Chwila, nalewam.", "Proszę bardzo, na zdrowie!", "Dla pana — z lodem?" };
    readonly System.Random rnd = new System.Random(4521);

    const float Siega = .7f, Niesie = 1.5f, Przechyla = 1.9f, Leje = 3.6f, Odnosi = 4.5f, Koniec = 5.2f, Kat = 112f;

    void OnEnable() => BraxInterakcje.Dodaj(this);
    void OnDisable() => BraxInterakcje.Usun(this);

    void Start()
    {
        if (stanowiska == null) return;
        foreach (var s in stanowiska)
        {
            if (s == null) continue;
            s.nast = Time.time + 4f + (float)rnd.NextDouble() * 20f;
            if (s.ciecz) s.ciecz.gameObject.SetActive(false);
            if (s.strumien) s.strumien.gameObject.SetActive(false);
        }
    }

    static bool Gotowe(Stanowisko s) => s != null && s.barman && s.butelki != null && s.butelki.Length > 0 && s.szklanka;

    public string Kontekst(Vector3 g) => BraxGodzinyOtwarcia.Otwarte("dyskoteka") && strefaZamow.Contains(g) ? "ZAMÓW" : "";
    public bool Wykonaj(Vector3 g)
    {
        if (stanowiska == null) return false;
        Stanowisko best = null; float bd = float.MaxValue;
        foreach (var s in stanowiska) { if (!Gotowe(s)) continue; float d = (s.barman.position - g).sqrMagnitude; if (d < bd) { bd = d; best = s; } }
        if (best == null) return false;
        if (best.t < 0f) Rozpocznij(best); else best.nast = 0f;
        BraxDymki.Powiedz(best.barman, odpowiedzi[rnd.Next(odpowiedzi.Length)]);
        return true;
    }

    void Rozpocznij(Stanowisko s)
    {
        s.t = 0f; s.b = rnd.Next(s.butelki.Length);
        var bt = s.butelki[s.b]; if (!bt) { s.t = -1f; return; }
        s.bPoz = bt.position; s.bRot = bt.rotation;
        if (s.ciecz) { s.ciecz.gameObject.SetActive(true); var k = s.ciecz.localScale; k.y = 1e-4f; s.ciecz.localScale = k; }
        s.pelna = 0f;
    }

    void Zakoncz(Stanowisko s)
    {
        var bt = s.butelki[s.b]; if (bt) bt.SetPositionAndRotation(s.bPoz, s.bRot);
        if (s.strumien) s.strumien.gameObject.SetActive(false);
        s.t = -1f; s.w = 0f; s.nast = Time.time + 20f + (float)rnd.NextDouble() * 20f; s.pelna = Time.time + 8f;
    }

    void Update()
    {
        if (stanowiska == null) return;
        bool otw = BraxGodzinyOtwarcia.Otwarte("dyskoteka");
        var gr = BraxLudzie.Gracz; float teraz = Time.time;
        foreach (var s in stanowiska)
        {
            if (!Gotowe(s)) continue;
            if (s.t < 0f)
            {
                if (s.pelna > 0f && teraz > s.pelna) { s.pelna = 0f; if (s.ciecz) s.ciecz.gameObject.SetActive(false); }
                if (otw && teraz >= s.nast && gr && (gr.position - s.barman.position).sqrMagnitude < blisko * blisko) Rozpocznij(s);
                continue;
            }
            s.t += Time.deltaTime;
            if (s.t >= Koniec) Zakoncz(s);
        }
    }

    void LateUpdate()
    {
        if (stanowiska == null) return;
        foreach (var s in stanowiska)
        {
            if (!Gotowe(s)) continue;
            if (s.ramie && s.ik && s.ramie.localRotation == s.mojaRamie) s.ramie.localRotation = s.bazaRamie;
            s.ik = false;
            if (s.t < 0f) continue;
            var bt = s.butelki[s.b]; if (!bt) continue;
            float t = s.t;
            var dno = s.szklanka.position; var gora = dno + Vector3.up * s.szklanka.lossyScale.y * s.szklankaWys;
            var d = gora - s.bPoz; d.y = 0f; d = d.sqrMagnitude > 1e-6f ? d.normalized : s.barman.forward;
            var os = Vector3.Cross(Vector3.up, d);
            float kat = t < Siega ? 0f : t < Niesie ? Mathf.SmoothStep(0f, 40f, (t - Siega) / (Niesie - Siega)) : t < Przechyla ? Mathf.SmoothStep(40f, Kat, (t - Niesie) / (Przechyla - Niesie))
                      : t < Leje ? Kat : t < Odnosi ? Mathf.SmoothStep(Kat, 0f, (t - Leje) / (Odnosi - Leje)) : 0f;
            var R = Quaternion.AngleAxis(kat, os);
            var nad = gora + Vector3.up * .04f - R * Vector3.up * s.butelkaWys;     // dno butelki tak, by szyjka była nad szklanką
            Vector3 poz = t < Siega ? s.bPoz : t < Niesie ? Vector3.Lerp(s.bPoz, nad, Mathf.SmoothStep(0f, 1f, (t - Siega) / (Niesie - Siega)))
                        : t < Leje ? nad : t < Odnosi ? Vector3.Lerp(nad, s.bPoz, Mathf.SmoothStep(0f, 1f, (t - Leje) / (Odnosi - Leje))) : s.bPoz;
            bt.SetPositionAndRotation(poz, R * s.bRot);
            // strumień i napełnianie
            bool leje = t > Przechyla && t < Leje;
            float fill = Mathf.Clamp01((t - Przechyla) / (Leje - Przechyla));
            if (s.ciecz) { var k = s.ciecz.localScale; k.y = Mathf.Max(1e-4f, s.cieczSkalaY * fill); s.ciecz.localScale = k; }
            if (s.strumien)
            {
                s.strumien.gameObject.SetActive(leje);
                if (leje)
                {
                    var a0 = poz + R * Vector3.up * s.butelkaWys;
                    var a1 = s.ciecz ? s.ciecz.position + Vector3.up * s.ciecz.lossyScale.y : dno;
                    var v = a0 - a1; float L = Mathf.Max(.01f, v.magnitude);
                    s.strumien.SetPositionAndRotation(a1, Quaternion.FromToRotation(Vector3.up, v));
                    s.strumien.localScale = new Vector3(s.strumienR, L, s.strumienR);
                }
            }
            // poza ramienia barmana (po animacji): ramię do butelki, waga narasta przy sięganiu i maleje po odstawieniu
            s.w = t < Siega ? t / Siega : t < Odnosi ? 1f : Mathf.Clamp01(1f - (t - Odnosi) / (Koniec - Odnosi));
            if (s.ramie && s.dlon && s.w > 0f)
            {
                s.bazaRamie = s.ramie.localRotation;
                var cel = poz + R * Vector3.up * (s.butelkaWys * .35f);
                var cur = s.dlon.position - s.ramie.position; var want = cel - s.ramie.position;
                if (cur.sqrMagnitude > 1e-6f && want.sqrMagnitude > 1e-6f)
                {
                    s.ramie.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, want), s.w) * s.ramie.rotation;
                    s.mojaRamie = s.ramie.localRotation; s.ik = true;
                }
            }
        }
    }
}
