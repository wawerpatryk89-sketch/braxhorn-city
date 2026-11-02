using System.Collections;
using UnityEngine;

// MIESZKANIEC W MIEJSCU (build 14, właściciel 21:01: „ludzie mają być wszędzie obstawieni… musi się coś dziać”):
// obsada lokali (za ladą: recepcja, bar, kawiarnia, kasa), ochroniarz przed klubem, policjanci, lekarz i pielęgniarka,
// ludzie siedzący na ławkach, pary rozmawiające z gestami, osoby z telefonem, tańczący, kierowcy i pasażerowie aut.
// Klipy Rocketbox (Legacy) zmieniane co kilka–kilkanaście sekund; para: mówi / słucha na zmianę z dymkami.
// Gracz blisko: obsługa patrzy na niego i wita; AKCJA = POGADAJ (kwestia roli). Ciosy: trafiony wzdryga się (odchylenie
// tułowia — tylko obrót kości, klip reakcji), krzyczy; obsługa woła o spokój; świadkowie odwracają głowę.
// Kierowca: siedzi (klip siedzenia przy stole), ręce lekko uniesione do kierownicy — obrót ramion po animacji (bez
// deformacji), głowa czasem rozgląda się; bez reakcji i bez POGADAJ (jest w aucie).
// Koszt: animacja tylko w promieniu BraxLudzie.Promien (kierowcy do 35 m), dalej poza zamrożona.
public sealed class BraxMieszkaniec : MonoBehaviour, IBraxInterakcja, IBraxOsoba
{
    public Animation anim;
    public string[] klipy = new string[0];
    public float zmianaMin = 6f, zmianaMax = 14f;
    public string[] mowi = new string[0], slucha = new string[0], reakcje = new string[0];
    public BraxMieszkaniec rozmowca;            // para rozmawiająca (lider = ten, który ma rozmowce i lider = true)
    public bool lider;
    public bool siedzi, wAucie, obsluga;
    public string rola = "";                   // barman, recepcja, kasjer, ochrona, policja, lekarz, … — kwestie POGADAJ
    public Transform glowa, szyja, kregoslup, ramieL, ramieP;
    public float korektaRak;                   // stopnie: uniesienie ramion do przodu (kierowca)
    public float ton = 1f, wzrost = 1.8f;

    float nastZmiana, nast, ostatni, lean, leanCel, patrzWaga, patrzCel, doKiedyPatrzy, nastPowitanie, nastRozgl;
    Vector3 leanOs = Vector3.right, patrzNa;
    bool blisko = true, mowiTeraz;
    int wariant;
    Quaternion obrot0;
    AudioSource glos;

    public Vector3 Pozycja => transform.position;
    public bool MozeReagowac => isActiveAndEnabled && !wAucie;

    void OnEnable() { if (!wAucie) { BraxInterakcje.Dodaj(this); BraxLudzie.Dodaj(this); } }
    void OnDisable() { BraxInterakcje.Usun(this); BraxLudzie.Usun(this); }

    void Start()
    {
        wariant = Random.Range(0, 64); obrot0 = transform.rotation;
        if (ton <= 0f) ton = 1f;
        if (anim && klipy.Length > 0)
        {
            var k = klipy[Random.Range(0, klipy.Length)];
            if (anim[k]) { anim.Play(k); anim[k].time = Random.value * anim[k].length; }
        }
        nastZmiana = Time.time + Random.Range(zmianaMin, zmianaMax);
        if (lider && rozmowca) StartCoroutine(Rozmowa());
    }

    void Graj(string k, float fade, float szyb = 1f)
    {
        if (!anim || string.IsNullOrEmpty(k)) return;
        var st = anim[k]; if (!st) return;
        st.speed = szyb;
        if (!anim.IsPlaying(k)) anim.CrossFade(k, fade);
    }
    static string Los(string[] z) => z != null && z.Length > 0 ? z[Random.Range(0, z.Length)] : null;

    void Update()
    {
        float d2 = BraxLudzie.OdlKamery2(transform.position);
        bool b2 = wAucie ? d2 < 35f * 35f && BraxLudzie.Animuj(d2) : BraxLudzie.Animuj(d2);
        if (b2 != blisko) { blisko = b2; if (anim) anim.enabled = blisko; }
        if (!blisko) { if (Time.time < nast) return; nast = Time.time + .5f; }
        float dt = Mathf.Min(Time.time - ostatni, 1f); ostatni = Time.time;
        lean = Mathf.MoveTowards(lean, leanCel, 120f * dt); leanCel = Mathf.MoveTowards(leanCel, 0f, 16f * dt);
        patrzWaga = Mathf.MoveTowards(patrzWaga, patrzCel, 2f * dt);
        if (!blisko) return;

        // zmiana klipu (para: rozmowa sama steruje klipami)
        if (!rozmowca && Time.time > nastZmiana && klipy.Length > 1)
        {
            nastZmiana = Time.time + Random.Range(zmianaMin, zmianaMax);
            Graj(Los(klipy), .6f);
        }
        var gracz = BraxLudzie.Gracz;
        if (Time.time < doKiedyPatrzy) { patrzCel = 1f; }
        else if (gracz && !wAucie && (obsluga || siedzi) && (gracz.position - transform.position).sqrMagnitude < 4f * 4f)
        {
            // obsługa i siedzący patrzą na gracza, który podchodzi; obsługa wita
            patrzCel = .8f; patrzNa = gracz.position;
            if (obsluga && Time.time > nastPowitanie) { nastPowitanie = Time.time + 45f; Powiedz(Powitanie(), 2.4f); }
        }
        else if (wAucie)
        {
            // kierowca: czasem spojrzy w bok (lusterko, skrzyżowanie)
            if (Time.time > nastRozgl) { nastRozgl = Time.time + Random.Range(2.5f, 6f); patrzCel = patrzCel > .1f ? 0f : .6f; patrzNa = transform.position + transform.right * (Random.value < .5f ? -5f : 5f) + transform.forward * 4f; }
        }
        else if (!mowiTeraz) patrzCel = 0f;
        // mieszkaniec stojący wraca do swojego kierunku po reakcji
        if (!siedzi && !wAucie && Time.time > doKiedyPatrzy) transform.rotation = Quaternion.Slerp(transform.rotation, obrot0, Mathf.Clamp01(dt * 1.5f));
    }

    void LateUpdate()
    {
        if (!blisko) return;
        if (korektaRak != 0f)
        {
            var r = transform.right;
            if (ramieL) ramieL.rotation = Quaternion.AngleAxis(-korektaRak, r) * ramieL.rotation;
            if (ramieP) ramieP.rotation = Quaternion.AngleAxis(-korektaRak, r) * ramieP.rotation;
        }
        if (lean > .2f && kregoslup) kregoslup.rotation = Quaternion.AngleAxis(lean, leanOs) * kregoslup.rotation;
        if (patrzWaga > .01f && glowa)
        {
            var cel = patrzNa - glowa.position; cel.y = 0f; var przod = transform.forward; przod.y = 0f;
            if (cel.sqrMagnitude < .01f || przod.sqrMagnitude < .01f) return;
            float kat = Mathf.Clamp(Vector3.SignedAngle(przod, cel, Vector3.up), -70f, 70f) * patrzWaga;
            if (szyja) szyja.rotation = Quaternion.AngleAxis(kat * .4f, Vector3.up) * szyja.rotation;
            glowa.rotation = Quaternion.AngleAxis(kat * (szyja ? .6f : 1f), Vector3.up) * glowa.rotation;
        }
    }

    // ---------------- para: rozmowa z gestami ----------------
    IEnumerator Rozmowa()
    {
        yield return new WaitForSeconds(Random.Range(.5f, 4f));
        while (rozmowca)
        {
            var kwestie = BraxDymki.MiedzySoba[Random.Range(0, BraxDymki.MiedzySoba.Length)];
            for (int k = 0; k < kwestie.Length && rozmowca; k++)
            {
                var kto = k % 2 == 0 ? this : rozmowca; var sluch = k % 2 == 0 ? rozmowca : this;
                float czas = 1.8f + kwestie[k].Length * .05f;
                kto.mowiTeraz = true; sluch.mowiTeraz = false;
                kto.Graj(Los(kto.mowi) ?? Los(kto.klipy), .4f); sluch.Graj(Los(sluch.slucha) ?? Los(sluch.klipy), .4f);
                kto.patrzCel = sluch.patrzCel = .5f; kto.patrzNa = sluch.transform.position; sluch.patrzNa = kto.transform.position;
                if (BraxLudzie.OdlKamery2(transform.position) < 24f * 24f) kto.Powiedz(kwestie[k], czas);
                yield return new WaitForSeconds(czas + .4f);
            }
            if (rozmowca) { mowiTeraz = rozmowca.mowiTeraz = false; Graj(Los(slucha) ?? Los(klipy), .5f); rozmowca.Graj(Los(rozmowca.slucha) ?? Los(rozmowca.klipy), .5f); }
            yield return new WaitForSeconds(Random.Range(6f, 14f));
        }
    }

    // ---------------- ciosy ----------------
    public void Trafiony(Vector3 g, Vector3 kierunek, bool kop)
    {
        leanOs = Vector3.Cross(Vector3.up, kierunek); leanCel = (kop ? 22f : 15f) * (siedzi ? .6f : 1f);
        doKiedyPatrzy = Time.time + 4f; patrzNa = g; patrzCel = 1f;
        if (!siedzi)
        {
            var d = g - transform.position; d.y = 0f; if (d.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(d);
            if (reakcje.Length > 0) { var k = reakcje[wariant % reakcje.Length]; if (anim && anim[k]) { anim[k].time = 0f; anim.CrossFade(k, .1f); anim[k].speed = 1.4f; } }
            nastZmiana = Time.time + 3f;
        }
        Powiedz(obsluga ? BraxLudzie.Los(BraxLudzie.Obsluga) : BraxLudzie.Los(BraxLudzie.Okrzyki), 2f, BraxLudzie.Krzyk(wariant));
        BraxDzwiekiSwiata.Uderzenie(transform.position + Vector3.up * wzrost * (siedzi ? .5f : .75f), kop ? 7f : 5f, 2);
    }

    public void Swiadek(Vector3 g, float odl, bool ktosTrafiony)
    {
        doKiedyPatrzy = Time.time + Random.Range(2f, 4f); patrzNa = g; patrzCel = 1f;
        if (!siedzi && !obsluga && ktosTrafiony && odl < 9f)
        {
            var d = g - transform.position; d.y = 0f; if (d.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(d);
        }
        if (ktosTrafiony && odl < 10f && Random.value < (obsluga ? .5f : .25f))
            Powiedz(obsluga ? BraxLudzie.Los(BraxLudzie.Obsluga) : BraxLudzie.Los(BraxLudzie.Swiadkowie), 2f, BraxLudzie.Krzyk(wariant + 2));
    }

    // ---------------- mowa ----------------
    void Powiedz(string tekst, float czas, AudioClip klip = null)
    {
        BraxDymki.Pokaz(transform, tekst, czas, (siedzi ? wzrost * .72f : wzrost) + .4f);
        BraxLudzie.Glos(gameObject, ref glos, klip ? klip : BraxDymki.Gwar(), ton, klip ? .7f : .5f, klip ? 0f : Mathf.Min(czas * .55f, 1.1f));
    }

    string Powitanie()
    {
        switch (rola)
        {
            case "barman": return BraxLudzie.Los(new[] { "Co podać?", "Dobry wieczór! Coś do picia?", "Szejk czy coś mocniejszego?" });
            case "barista": return BraxLudzie.Los(new[] { "Dzień dobry! Kawa?", "Polecam dziś sernik.", "Cappuccino jak zawsze?" });
            case "recepcja": return BraxLudzie.Los(new[] { "Dzień dobry! W czym mogę pomóc?", "Witamy! Karnet czy wejście jednorazowe?" });
            case "kasjer": return BraxLudzie.Los(new[] { "Dzień dobry!", "Następny proszę.", "Płatność kartą?" });
            case "kasyno": return BraxLudzie.Los(new[] { "Dobry wieczór. Żetony?", "Witamy w kasynie. Ile wymieniamy?" });
            case "ochrona": return BraxLudzie.Los(new[] { "Dobry wieczór.", "Zapraszam, tylko bez awantur.", "Lista gości? Żartuję, wchodź." });
            case "policja": return BraxLudzie.Los(new[] { "Dzień dobry. Coś się stało?", "Komisariat, słucham.", "Proszę poczekać, zaraz ktoś podejdzie." });
            case "lekarz": return BraxLudzie.Los(new[] { "Dzień dobry. Rejestracja jest na wprost.", "Proszę poczekać, zaraz pana poproszą." });
            case "szatnia": return BraxLudzie.Los(new[] { "Kurtkę zostawiamy?", "Dobry wieczór! Numerek proszę." });
            default: return BraxDymki.Los(BraxDymki.Powitania);
        }
    }

    // AKCJA: POGADAJ (obsługa i ludzie w miejscu; nie kierowcy)
    public string Kontekst(Vector3 g)
    {
        if (!isActiveAndEnabled || wAucie || mowiTeraz) return "";
        var d = g - transform.position; d.y = 0f;
        return d.sqrMagnitude < (obsluga ? 2.8f : 2.2f) * (obsluga ? 2.8f : 2.2f) ? "POGADAJ" : "";
    }

    public bool Wykonaj(Vector3 g)
    {
        var gracz = BraxLudzie.Gracz; if (!gracz) return false;
        StartCoroutine(Pogadaj(gracz));
        return true;
    }

    IEnumerator Pogadaj(Transform gracz)
    {
        mowiTeraz = true; doKiedyPatrzy = Time.time + 7f; patrzNa = gracz.position;
        string w = obsluga ? Powitanie() : BraxDymki.Los(BraxDymki.Wiesci); float c1 = 2f + w.Length * .045f;
        if (!siedzi) Graj(Los(mowi) ?? Los(klipy), .3f);
        Powiedz(w, c1); yield return new WaitForSeconds(c1 + .2f);
        string o = BraxDymki.Los(BraxDymki.OdpowiedziBraxa); BraxDymki.Pokaz(gracz, o, 2.4f, 2.55f); yield return new WaitForSeconds(2.5f);
        Powiedz(BraxDymki.Los(BraxDymki.Pozegnania), 2f); yield return new WaitForSeconds(1.8f);
        mowiTeraz = false;
    }
}
