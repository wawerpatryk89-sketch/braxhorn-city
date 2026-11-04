using System.Collections.Generic;
using UnityEngine;

// BRAXHORN — worek treningowy, który reaguje (właściciel 1.11, 17:52: „worki w ogóle na łańcuchach nie wiszą… nie można
// w nie uderzać, nie reagują nic”). Wahadło 2D: wychylenie s (radiany: x = dół worka w stronę +x świata, y = w stronę +z)
// i prędkość kątowa; ω² = g / L (L = od haka do środka worka), tłumienie skóry i łańcucha. Obiekt = punkt zawieszenia
// (hak w belce); łańcuch, okucia i worek są w jednej siatce pod nim, więc obracają się razem z workiem.
//  • CIOS / KOP Braxa (BraxGymPlayer.OnCios — chwila trafienia z animacji): worek w zasięgu 1.2 m przed Braxem dostaje
//    impuls w kierunku ciosu (kop mocniej) i głuchy dźwięk uderzenia w skórę (klip liczony w kodzie, bez plików).
//  • Brax wchodzący w worek ciałem: kolizja kapsuły worka (kinematyczna, rusza się z workiem) zatrzymuje gracza, a kontakt
//    liczony z promieniem o 8 cm większym popycha worek — gracz napierający lekko go odsuwa.
//  • Koszt: liczone tylko gdy worek się buja albo Brax jest bliżej niż 6 m; w spoczynku komponent nic nie robi.
// Tylko moduły rdzenia (bez fizyki cząsteczek, bez Rigidbody w ruchu — kapsuła przesuwana transformem).
public sealed class BraxWorek : MonoBehaviour
{
    public float dlugosc = 2.75f;    // hak → środek worka (m)
    public float gora = 1.95f;       // hak → górna krawędź skóry (m)
    public float dol = 3.6f;         // hak → spód worka (m)
    public float promien = .28f;     // promień worka
    public float tlumienie = .9f;    // 1/s
    public float silaCiosu = .62f, silaKopu = .95f;   // impuls prędkości kątowej (rad/s)
    public BraxGymPlayer pauseWith; // Optional camp pause; other zones keep existing behavior.

    Vector2 s, v;                    // wychylenie (rad) i prędkość kątowa (rad/s)
    Quaternion spoczynek;
    AudioSource zrodlo;
    bool audioPaused;
    static AudioClip klip;
    static Transform gracz; static CharacterController cialo; static float szukaj;

    void Awake()
    {
        spoczynek = transform.rotation;
        zrodlo = gameObject.AddComponent<AudioSource>();
        zrodlo.playOnAwake = false; zrodlo.spatialBlend = .85f; zrodlo.minDistance = 2f; zrodlo.maxDistance = 22f; zrodlo.volume = .55f;
        zrodlo.rolloffMode = AudioRolloffMode.Linear;
        if (!klip) klip = Uderzenie();
    }
    void OnEnable() { BraxGymPlayer.OnCios += Cios; }
    void OnDisable() { BraxGymPlayer.OnCios -= Cios; if(zrodlo)zrodlo.Stop(); }

    // Głuche „łup” w skórzany worek: krótki szum (plaśnięcie skóry) + niski ton 70→45 Hz (masa worka), szybkie wygaszenie.
    static AudioClip Uderzenie()
    {
        const int rate = 22050; var d = new float[rate / 4]; var r = new System.Random(17); float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / rate;
            lp += (((float)r.NextDouble() * 2f - 1f) - lp) * .18f;                         // szum przez prosty filtr dolnoprzepustowy
            float slap = lp * Mathf.Exp(-t * 55f) * 1.6f;
            float body = Mathf.Sin(2f * Mathf.PI * (70f - 100f * t) * t) * Mathf.Exp(-t * 16f);
            d[i] = Mathf.Clamp((slap + body) * .8f, -1f, 1f);
        }
        var c = AudioClip.Create("Worek: uderzenie w skórę", d.Length, 1, rate, false); c.SetData(d, 0); return c;
    }

    static void ZnajdzGracza()
    {
        if (gracz || Time.unscaledTime < szukaj) return;
        szukaj = Time.unscaledTime + 1f;
        var p = FindAnyObjectByType<BraxGymPlayer>();
        if (p) { gracz = p.transform; cialo = p.GetComponent<CharacterController>(); }
    }

    // Środek worka na wysokości y (w świecie) przy obecnym wychyleniu.
    Vector3 Os(float h) => transform.position + transform.rotation * Quaternion.Inverse(spoczynek) * (Vector3.down * h);

    void Cios(Vector3 kto, Vector3 kierunek, bool kop)
    { TryStrike(kto, kierunek, kop); }

    public bool StrikeFromNpc(Vector3 from, Vector3 direction) => TryStrike(from, direction, false);

    bool TryStrike(Vector3 kto, Vector3 kierunek, bool kop)
    {
        if (!isActiveAndEnabled || (pauseWith && pauseWith.Paused)) return false;
        var c = Os(Mathf.Lerp(gora, dol, .45f)); var d = c - kto; d.y = 0f;
        float odl = d.magnitude - promien; kierunek.y = 0f;
        if (odl > 1.2f || kierunek.sqrMagnitude < .01f) return false;
        kierunek.Normalize();
        if (d.sqrMagnitude > .0001f && Vector3.Dot(kierunek, d.normalized) < .3f) return false;   // worek musi być przed Braxem
        float k = kop ? silaKopu : silaCiosu;
        v += new Vector2(kierunek.x, kierunek.z) * k;
        if (zrodlo && klip) { zrodlo.pitch = kop ? .85f : 1f + Random.Range(-.06f, .06f); zrodlo.PlayOneShot(klip, kop ? 1f : .8f); }
        return true;
    }

    void Update()
    {
        bool paused=pauseWith&&pauseWith.Paused;
        if(audioPaused!=paused)
        {audioPaused=paused;if(zrodlo){if(paused)zrodlo.Pause();else zrodlo.UnPause();}}
        if (pauseWith && pauseWith.Paused) return;
        ZnajdzGracza();
        float dt = Mathf.Min(Time.deltaTime, .05f);
        bool rusza = s.sqrMagnitude > 1e-6f || v.sqrMagnitude > 1e-6f;
        bool blisko = false;
        if (gracz)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            blisko = g.sqrMagnitude < 36f;
        }
        if (!rusza && !blisko) return;
        float w2 = 9.81f / Mathf.Max(.5f, dlugosc);
        var a = -w2 * s - tlumienie * v;
        // kontakt z ciałem Braxa (kapsuła gracza: promień + 8 cm zapasu, na wysokości worka)
        if (gracz && blisko)
        {
            float r = (cialo ? cialo.radius : .35f) + .08f;
            float y0 = gracz.position.y, y1 = y0 + (cialo ? cialo.height : 2.25f);
            float wy0 = transform.position.y - dol, wy1 = transform.position.y - gora;
            if (y1 > wy0 && y0 < wy1)
            {
                var c = Os(Mathf.Lerp(gora, dol, .5f)); var d = c - gracz.position; d.y = 0f;
                float pen = r + promien - d.magnitude;
                if (pen > 0f && d.sqrMagnitude > 1e-6f)
                {
                    var n = d.normalized;
                    a += new Vector2(n.x, n.z) * (pen * 60f / Mathf.Max(.5f, dlugosc));
                    v *= 1f - Mathf.Min(1f, dt * 2f);
                }
            }
        }
        v += a * dt; s += v * dt;
        if (s.magnitude > .7f) { s = s.normalized * .7f; v *= .5f; }        // ok. 40° — dalej worek by uderzał w belkę
        if (!blisko && s.sqrMagnitude < 2e-6f && v.sqrMagnitude < 2e-6f) { s = Vector2.zero; v = Vector2.zero; }
        transform.rotation = Quaternion.Euler(-s.y * Mathf.Rad2Deg, 0f, s.x * Mathf.Rad2Deg) * spoczynek;
    }
}
