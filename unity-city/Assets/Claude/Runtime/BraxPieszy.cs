using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PRZECHODZIEŃ (właściciel 1.11: „ludzie mają tylko żyć — chodzić, nic nie atakować”). Chodzi tam i z powrotem po odcinku
// chodnika, czasem przystaje (rozgląda się), przed Braxem staje i czeka. Animacje: Walking / Idle (te same klipy Mixamo co
// Brax, przepięte na szkielet postaci przy buildzie). Daleko od kamery (> 70 m) — bez animacji i co 0,25 s (taniej).
// ROZMOWY (18:35: „żeby z tobą gadali, odzywali się do siebie”): mija Braxa — czasem się wita (dymek + gwar);
// AKCJA przy przechodniu = POGADAJ (wieść z osiedla, odpowiedź Braxa, pożegnanie); dwóch przechodniów obok siebie
// czasem staje i rozmawia (BraxDymki).
public sealed class BraxPieszy : MonoBehaviour, IBraxInterakcja
{
    public Vector3 a, b;
    public float predkosc = 1.25f;
    public Animation anim;
    public string chod = "Walking", stoi = "Idle";
    bool doB = true, rozmowa;
    float pauza, nast, nastRozmowa, nastPowitanie, ton = 1f, przeszedl;
    Transform gracz, rozmowca;
    AudioSource glos;
    static Camera kam;
    static readonly List<BraxPieszy> wszyscy = new List<BraxPieszy>();

    void OnEnable() { wszyscy.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { wszyscy.Remove(this); BraxInterakcje.Usun(this); }

    void Start()
    {
        if (anim && anim[chod]) { anim[chod].speed = predkosc / 1.3f; anim.Play(chod); }
        transform.position = Vector3.Lerp(a, b, Random.value);
        doB = Random.value > .5f;
        ton = Random.Range(.82f, 1.3f);
        nastRozmowa = Time.time + Random.Range(5f, 25f);
    }

    void Update()
    {
        if (!kam) kam = Camera.main;
        float dt = Time.deltaTime;
        if (kam)
        {
            float d2 = (kam.transform.position - transform.position).sqrMagnitude;
            bool daleko = d2 > 70f * 70f;
            if (anim && anim.enabled == daleko) anim.enabled = !daleko;
            if (daleko) { if (Time.time < nast) return; dt = .25f; nast = Time.time + .25f; }
        }
        if (!gracz) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        if (rozmowa)
        {
            if (anim && !anim.IsPlaying(stoi)) anim.CrossFade(stoi, .25f);
            if (rozmowca) { var g = rozmowca.position - transform.position; g.y = 0f; if (g.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(g), dt * 4f); }
            return;
        }
        if (gracz && Time.time > nastPowitanie)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            if (g.magnitude < 3f) { nastPowitanie = Time.time + 40f; if (Random.value < .35f) Powiedz(BraxDymki.Los(BraxDymki.Powitania), 2.6f); }
        }
        if (Time.time > nastRozmowa) { nastRozmowa = Time.time + .6f; SzukajRozmowcy(); if (rozmowa) return; }
        if (pauza > 0f)
        {
            pauza -= dt;
            if (pauza <= 0f && anim) anim.CrossFade(chod, .3f);
            return;
        }
        Vector3 cel = doB ? b : a, d = cel - transform.position; d.y = 0f;
        if (d.magnitude < .3f)
        {
            doB = !doB;
            if (Random.value < .45f) { pauza = Random.Range(2f, 7f); if (anim) anim.CrossFade(stoi, .3f); }
            return;
        }
        var dir = d.normalized;
        // Brax na drodze: stanąć i poczekać (nie wchodzić w niego)
        if (gracz)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            if (g.magnitude < 1.8f && Vector3.Dot(g.normalized, dir) > .4f)
            {
                if (anim && !anim.IsPlaying(stoi)) anim.CrossFade(stoi, .25f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(g), dt * 3f);
                return;
            }
        }
        if (anim && !anim.IsPlaying(chod)) anim.CrossFade(chod, .25f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
        float krok = Mathf.Min(predkosc * dt, d.magnitude); transform.position += dir * krok;
        przeszedl += krok; if (przeszedl > .72f) { przeszedl = 0f; if (kam && (kam.transform.position - transform.position).sqrMagnitude < 14f * 14f) BraxDzwiekiSwiata.Krok(transform.position, .13f); }
    }

    void Powiedz(string tekst, float czas)
    {
        BraxDymki.Pokaz(transform, tekst, czas);
        if (!kam || (kam.transform.position - transform.position).sqrMagnitude > 20f * 20f) return;
        if (!glos)
        {
            glos = gameObject.AddComponent<AudioSource>(); glos.spatialBlend = 1f; glos.minDistance = 2f; glos.maxDistance = 16f;
            glos.rolloffMode = AudioRolloffMode.Logarithmic; glos.volume = .5f; glos.priority = 180; glos.playOnAwake = false;
        }
        glos.pitch = ton * Random.Range(.96f, 1.04f);
        glos.clip = BraxDymki.Gwar(); glos.time = Random.value * .5f;
        glos.Play(); glos.SetScheduledEndTime(AudioSettings.dspTime + Mathf.Min(czas * .55f, 1.1f));
    }

    // dwóch przechodniów blisko siebie (do 2.4 m), obaj wolni — czasem staną i pogadają
    void SzukajRozmowcy()
    {
        if (Random.value > .25f) return;
        foreach (var o in wszyscy)
        {
            if (o == this || o.rozmowa || Time.time < o.nastRozmowa - 30f) continue;
            if ((o.transform.position - transform.position).sqrMagnitude > 2.4f * 2.4f) continue;
            StartCoroutine(Rozmowa(this, o, BraxDymki.MiedzySoba[Random.Range(0, BraxDymki.MiedzySoba.Length)]));
            return;
        }
    }

    static IEnumerator Rozmowa(BraxPieszy x, BraxPieszy y, string[] kwestie)
    {
        x.rozmowa = y.rozmowa = true; x.rozmowca = y.transform; y.rozmowca = x.transform;
        yield return new WaitForSeconds(.6f);
        for (int i = 0; i < kwestie.Length; i++)
        {
            var kto = i % 2 == 0 ? x : y; if (!kto) break;
            float czas = 1.6f + kwestie[i].Length * .045f;
            kto.Powiedz(kwestie[i], czas);
            yield return new WaitForSeconds(czas + .3f);
        }
        foreach (var p in new[] { x, y }) if (p) { p.rozmowa = false; p.rozmowca = null; p.nastRozmowa = Time.time + Random.Range(40f, 90f); p.pauza = 0f; }
    }

    // AKCJA: POGADAJ (rejestr BraxInterakcje)
    public string Kontekst(Vector3 g)
    {
        if (!isActiveAndEnabled || rozmowa) return "";
        var d = g - transform.position; d.y = 0f;
        return d.sqrMagnitude < 2.3f * 2.3f ? "POGADAJ" : "";
    }

    public bool Wykonaj(Vector3 g)
    {
        if (rozmowa || !gracz) return false;
        StartCoroutine(PogadajZBraxem());
        return true;
    }

    IEnumerator PogadajZBraxem()
    {
        rozmowa = true; rozmowca = gracz;
        string w = BraxDymki.Los(BraxDymki.Wiesci); float c1 = 2f + w.Length * .045f;
        Powiedz(w, c1); yield return new WaitForSeconds(c1 + .2f);
        string o = BraxDymki.Los(BraxDymki.OdpowiedziBraxa); BraxDymki.Pokaz(gracz, o, 2.4f, 2.55f); yield return new WaitForSeconds(2.5f);
        Powiedz(BraxDymki.Los(BraxDymki.Pozegnania), 2f); yield return new WaitForSeconds(1.8f);
        rozmowa = false; rozmowca = null; nastPowitanie = Time.time + 60f; nastRozmowa = Time.time + 30f;
    }
}
