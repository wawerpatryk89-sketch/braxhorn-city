using UnityEngine;

// Barman podnosi butelkę prawą ręką, nalewa i odstawia ją na szynę.
// Zamówiona szklanka czeka na odbiór, dopóki gracz jest przy barze.
public sealed class BraxBarDyskoteki : MonoBehaviour, IBraxInterakcja
{
    [System.Serializable]
    public sealed class Stanowisko
    {
        public Transform barman, ramie, dlon;
        public Transform[] chwyt, butelki;
        public Vector3[] korekta;
        public Transform szklanka, ciecz, strumien;
        public Animation ruch;
        public Vector3 osButelkiWRece, dnoWRece, punktOdbioru;
        public float butelkaWys = .4f, szklankaWys = .12f, cieczSkalaY = .1f, strumienR = .006f;
        [System.NonSerialized] public float t = -1f, nast, pelna, napelnienie;
        [System.NonSerialized] public Vector3 bPoz;
        [System.NonSerialized] public Quaternion bRot;
    }

    public Stanowisko[] stanowiska;
    public Bounds strefaZamow;
    public BraxGymPlayer gracz;
    Stanowisko zamowione, wydane;
    public float blisko = 14f;
    public string[] odpowiedzi = { "Już się robi!", "Chwila, nalewam.", "Proszę bardzo, na zdrowie!", "Dla pana — z lodem?" };
    readonly System.Random rnd = new System.Random(4521);

    public const float Bierze = 89f/24f;
    public const float Poczatek = 78f/24f;
    const float Koniec = 163f/24f, Odkłada = 150f/24f;
    const float StabilnyStrumienOd = 110f/24f, StabilnyStrumienDo = 133f/24f;
    public static Vector3 DnoButelki(Stanowisko s,float czas)
    {
        float f=Mathf.Clamp((czas-Bierze)*24f,0f,s.korekta.Length-1);
        int k=Mathf.FloorToInt(f);
        var v=Vector3.Lerp(s.korekta[k],s.korekta[Mathf.Min(k+1,s.korekta.Length-1)],f-k);
        return s.dlon.TransformPoint(s.dnoWRece+v);
    }

    void OnEnable()
    {
        BraxInterakcje.Dodaj(this);
        if (!gracz && BraxLudzie.Gracz) gracz = BraxLudzie.Gracz.GetComponent<BraxGymPlayer>();
        if (gracz) gracz.bar = this;
    }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        if (gracz && wydane != null) gracz.OdstawSzklanke();
        if (stanowiska != null) foreach (var s in stanowiska)
        {
            if (s == null) continue;
            if (s.t >= 0f && s.butelki != null && s.butelki.Length > 0 && s.butelki[0])
                s.butelki[0].SetPositionAndRotation(s.bPoz, s.bRot);
            if (s.ruch && s.ruch.IsPlaying("Pour")) s.ruch.CrossFade("Idle", .15f);
            s.t = -1f; s.pelna = 0f;
            if (s.ciecz) s.ciecz.gameObject.SetActive(false);
            if (s.strumien) s.strumien.gameObject.SetActive(false);
        }
        zamowione = wydane = null;
        if (gracz && gracz.bar == this) gracz.bar = null;
    }

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

    static bool Gotowe(Stanowisko s) => s != null && s.barman && s.ruch && s.ruch.GetClip("Pour") && s.chwyt != null && s.chwyt.Length == 5 && s.korekta != null && s.korekta.Length == 62 && s.butelki != null && s.butelki.Length > 0 && s.szklanka;

    Stanowisko Najblizsze(Vector3 g)
    {
        Stanowisko wynik = null;
        float najmniej = float.PositiveInfinity;
        if (stanowiska != null) foreach (var s in stanowiska)
        {
            if (!Gotowe(s)) continue;
            float d = (s.szklanka.position - g).sqrMagnitude;
            if (d < najmniej) { wynik = s; najmniej = d; }
        }
        return wynik;
    }

    public string Kontekst(Vector3 g)
    {
        if (wydane != null || !strefaZamow.Contains(g) || !BraxGodzinyOtwarcia.Otwarte("dyskoteka")) return "";
        if (zamowione != null)
        {
            if (zamowione.t >= 0f) return "NALEWANIE";
            Vector3 delta = zamowione.punktOdbioru - g;
            delta.y = 0;
            return delta.sqrMagnitude <= 1.03f*1.03f ? "NAPIJ SIĘ" : "ODBIERZ PRZY BARMANIE";
        }
        return Najblizsze(g) != null ? "ZAMÓW" : "";
    }

    public bool Wykonaj(Vector3 g)
    {
        string akcja = Kontekst(g);
        if (akcja.Length == 0) return false;
        if (akcja == "ZAMÓW")
        {
            zamowione = Najblizsze(g);
            if (zamowione.t < 0f) Rozpocznij(zamowione);
            BraxDymki.Powiedz(zamowione.barman, odpowiedzi[0]);
        }
        else if (akcja == "NAPIJ SIĘ" && gracz && gracz.Pij(zamowione.szklanka))
        {
            wydane = zamowione; zamowione = null;
            wydane.szklanka.gameObject.SetActive(false);
            wydane.ciecz.gameObject.SetActive(false);
        }
        return true;
    }

    public void Zwrot()
    {
        if (wydane == null) return;
        wydane.szklanka.gameObject.SetActive(true);
        wydane.pelna = 0f;
        wydane.nast = Time.time + 20f;
        wydane = null;
    }

    void Rozpocznij(Stanowisko s)
    {
        s.t = Poczatek; s.napelnienie = 0f;
        var bt = s.butelki[0]; if (!bt) { s.t = -1f; return; }
        s.bPoz = bt.position; s.bRot = bt.rotation;
        var stan = s.ruch["Pour"];
        stan.speed = 1f; stan.wrapMode = WrapMode.ClampForever;
        s.ruch.CrossFade("Pour", .12f, PlayMode.StopAll);
        stan.time = 0f;
        if (s.ciecz) { s.ciecz.gameObject.SetActive(true); var k = s.ciecz.localScale; k.y = 1e-4f; s.ciecz.localScale = k; }
        s.pelna = 0f;
    }

    void Zakoncz(Stanowisko s)
    {
        var bt = s.butelki[0]; if (bt) bt.SetPositionAndRotation(s.bPoz, s.bRot);
        if (s.strumien) s.strumien.gameObject.SetActive(false);
        s.ruch.CrossFade("Idle", .18f, PlayMode.StopAll);
        s.t = -1f; s.nast = Time.time + 20f + (float)rnd.NextDouble() * 20f; s.pelna = Time.time + 8f;
    }

    void Update()
    {
        if (stanowiska == null) return;
        bool otw = BraxGodzinyOtwarcia.Otwarte("dyskoteka");
        var gr = BraxLudzie.Gracz; float teraz = Time.time;
        if (zamowione != null && (!otw || !gr || (gr.position-zamowione.barman.position).sqrMagnitude>blisko*blisko))
        {
            if (zamowione.t<0f) zamowione.pelna=teraz+8f;
            zamowione=null;
        }
        foreach (var s in stanowiska)
        {
            if (!Gotowe(s)) continue;
            if (s.t < 0f)
            {
                if (s == zamowione || s == wydane) continue;
                if (s.pelna > 0f && teraz > s.pelna) { s.pelna = 0f; if (s.ciecz) s.ciecz.gameObject.SetActive(false); }
                if (otw && teraz >= s.nast && gr && (gr.position - s.barman.position).sqrMagnitude < blisko * blisko) Rozpocznij(s);
                continue;
            }
            var stan = s.ruch["Pour"];
            s.t = Poczatek + stan.time;
            if (s.t >= Koniec || !s.ruch.IsPlaying("Pour")) Zakoncz(s);
        }
    }


    void LateUpdate()
    {
        if (stanowiska == null) return;
        foreach (var s in stanowiska)
        {
            if (!Gotowe(s) || s.t < 0f) continue;
            var bt = s.butelki[0];
            var os = Vector3.up;
            var dno = s.bPoz;
            if (s.t > Bierze && s.t <= Odkłada)
            {
                os = s.dlon.TransformDirection(s.osButelkiWRece).normalized;
                dno = DnoButelki(s,s.t);
            }
            bt.SetPositionAndRotation(dno,Quaternion.FromToRotation(Vector3.up,os)*s.bRot);
            var szyjka = dno + os*s.butelkaWys;
            var nadSzklem = Vector3.ProjectOnPlane(szyjka-s.szklanka.position,Vector3.up);
            bool leje = s.t>StabilnyStrumienOd && s.t<StabilnyStrumienDo && os.y<.015f
                && szyjka.y>s.szklanka.position.y+s.szklankaWys && nadSzklem.magnitude<s.szklankaWys*(.039f/.095f);
            if (leje) s.napelnienie=Mathf.Min(.80f,s.napelnienie+Time.deltaTime*1.968f);
            if (s.ciecz)
            {
                var v=s.ciecz.localScale; v.y=Mathf.Max(.0001f,s.cieczSkalaY*s.napelnienie); s.ciecz.localScale=v;
            }
            if (!s.strumien) continue;
            s.strumien.gameObject.SetActive(leje);
            if (leje)
            {
                float dolY=s.szklanka.position.y+s.szklankaWys*(.085f+.76f*s.napelnienie);
                s.strumien.SetPositionAndRotation(new Vector3(szyjka.x,dolY,szyjka.z),Quaternion.identity);
                s.strumien.localScale=new Vector3(s.strumienR,Mathf.Max(.001f,szyjka.y-dolY),s.strumienR);
            }
        }
    }
}
