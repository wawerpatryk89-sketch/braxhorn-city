using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Jeden rozmówca prowadzi wymianę. Następna rozmowa wymaga ponownego podejścia.
public sealed class BraxZnajomyPatryka : MonoBehaviour
{
    public string postac, zajecie, powitanie, temat, odpowiedz, zakonczenie;
    public int drugaRozmowa;
    public bool kobieta, spacer;
    public Vector3 celSpaceru;
    public Animation ruch;
    public Transform gracz, glowa;
    public BraxZnajomyPatryka partner;
    static readonly List<BraxZnajomyPatryka> obecni = new List<BraxZnajomyPatryka>();
    static BraxZnajomyPatryka mowi;
    static float ciszaDo;
    static Vector3 miejsceOstatniejRozmowy;
    static bool bylaRozmowa;
    Vector3 start;
    Quaternion kierunek;
    bool powrot, powitany;
    int wizyty;
    float kontrola, przerwa, nastepnaWizyta, tlo;
    float Ton => kobieta ? 1.22f : 1f;
    const float Blisko = 3.5f, Odejscie = 5.4f;

    void OnEnable() { obecni.Add(this); }
    void OnDisable()
    {
        obecni.Remove(this);
        if (obecni.Count == 0) { bylaRozmowa = false; ciszaDo = 0f; }
        if (mowi == this) { mowi = null; ciszaDo = Time.time + 1f; }
    }

    void Start()
    {
        start = transform.position;
        kierunek = transform.rotation;
        Graj(zajecie);
        if (ruch && ruch[zajecie] != null) ruch[zajecie].normalizedTime = Mathf.Repeat(drugaRozmowa * .173f, 1f);
        tlo = Time.time + 8f;
    }

    void Graj(string klip)
    {
        if (!ruch || ruch.IsPlaying(klip)) return;
        ruch[klip].wrapMode = WrapMode.Loop;
        ruch.CrossFade(klip, .22f);
    }

    bool Widzi(Transform kto)
    {
        Vector3 a = glowa ? glowa.position : transform.position + Vector3.up * 1.95f;
        Vector3 b = kto.position + Vector3.up * 1.95f;
        return !Physics.Linecast(a, b, ~(1 << 8), QueryTriggerInteraction.Ignore);
    }

    void Update()
    {
        if (!gracz) return;
        float d = Vector3.Distance(gracz.position, transform.position);
        if (d > Odejscie) powitany = false;
        if (spacer && mowi != this) Spaceruj();
        if (Time.time < kontrola || mowi || Time.time < ciszaDo) return;
        kontrola = Time.time + .25f;
        if (!powitany && d <= Blisko && Time.time >= nastepnaWizyta && Widzi(gracz) &&
            (!bylaRozmowa || Vector3.Distance(gracz.position, miejsceOstatniejRozmowy) >= 1.1f))
        {
            BraxZnajomyPatryka najblizszy = this;
            foreach (var osoba in obecni)
            {
                if (osoba == this) continue;
                float odleglosc = Vector3.Distance(osoba.transform.position, gracz.position);
                if (odleglosc >= d || !osoba.Widzi(gracz)) continue;
                d = odleglosc;
                najblizszy = osoba;
            }
            if (najblizszy == this) StartCoroutine(Rozmowa());
        }
        else if (partner && Time.time >= tlo && d > Odejscie && d < 18f)
        {
            tlo = Time.time + 30f;
            StartCoroutine(RozmowaWTle());
        }
    }

    IEnumerator Rozmowa()
    {
        mowi = this;
        // W grupie reaguje najbliższa osoba. Do kolejnej można podejść z innej strony.
        powitany = true; bylaRozmowa = true; miejsceOstatniejRozmowy = gracz.position;
        nastepnaWizyta = Time.time + 18f;
        if (spacer || zajecie == "Idle" || zajecie == "Talk" || zajecie.StartsWith("Dance")) { Obroc(gracz.position); Graj("Talk"); }
        var wymiana = BraxDymki.Wymiany[drugaRozmowa];
        bool pierwsza = wizyty++ % 2 == 0;
        string[] tekst = pierwsza ? new[] {powitanie, temat, odpowiedz, zakonczenie} : new[] {"O, Patryk! Siema!", wymiana[0], wymiana[1], wymiana[2]};
        for (int i = 0; i < tekst.Length; i++)
        {
            if (Vector3.Distance(transform.position, gracz.position) > Odejscie || !Widzi(gracz)) break;
            float trwa = BraxDymki.Powiedz(i == 2 ? gracz : transform, tekst[i], i == 2 ? 1f : Ton);
            yield return new WaitForSeconds(trwa + .18f);
        }
        if (!spacer) { transform.rotation = kierunek; Graj(zajecie); }
        ciszaDo = Time.time + 1.2f;
        mowi = null;
    }

    IEnumerator RozmowaWTle()
    {
        var tekst = BraxDymki.MiedzySoba[(wizyty + (int)(Time.time / 30)) % BraxDymki.MiedzySoba.Length];
        for (int i = 0; i < tekst.Length; i++)
        {
            if (mowi || !partner || Vector3.Distance(transform.position, gracz.position) < Odejscie) yield break;
            var osoba = (i % 2 == 0) ? this : partner;
            float trwa = BraxDymki.Powiedz(osoba.transform, tekst[i], osoba.Ton, BraxGlosPostaci.Rodzaj.Tlo);
            yield return new WaitForSeconds(trwa + .25f);
        }
    }

    Quaternion bazaGlowy, ostatniaGlowa;
    bool odwrocona;
    void LateUpdate()
    {
        if (!glowa) return;
        if (odwrocona && Quaternion.Angle(glowa.localRotation, ostatniaGlowa) < .005f)
            glowa.localRotation = bazaGlowy;
        odwrocona = false;
        if (mowi != this || (zajecie != "SeatedIdle" && zajecie != "Drink")) return;
        var doGracza = Vector3.ProjectOnPlane(gracz.position - glowa.position, Vector3.up);
        if (doGracza.sqrMagnitude < .01f) return;
        float kat = Mathf.Clamp(Vector3.SignedAngle(transform.forward, doGracza, Vector3.up), -32f, 32f);
        bazaGlowy = glowa.localRotation;
        glowa.rotation = Quaternion.AngleAxis(kat, Vector3.up) * glowa.rotation;
        ostatniaGlowa = glowa.localRotation;
        odwrocona = true;
    }

    void Obroc(Vector3 cel)
    {
        var v = cel - transform.position;
        v.y = 0;
        if (v.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(v);
    }

    void Spaceruj()
    {
        if (Time.time < przerwa) { Graj("Idle"); return; }
        var cel = powrot ? start : celSpaceru;
        Obroc(cel);
        Graj("Walk");
        transform.position = Vector3.MoveTowards(transform.position, cel, Time.deltaTime * 1.18f);
        if ((cel - transform.position).sqrMagnitude < .001f) { powrot = !powrot; przerwa = Time.time + 2.8f; }
    }
}
