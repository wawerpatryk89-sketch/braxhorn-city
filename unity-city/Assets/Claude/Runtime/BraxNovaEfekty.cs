using UnityEngine;

// CLUB NOVA — efekty sali (przebudowa 2.11, właściciel: „LASERY mają chodzić, KULA ma błyskać”, „wszystko premium”).
// Jeden komponent na klub. Pracuje tylko, gdy gracz jest bliżej niż `zasieg` (poza tym zero pracy, efekty gasną):
//  • kula lustrzana obraca się (kąt przekazywany shaderom: _NovaKat — plamki na ścianach biegną razem z kulą),
//  • lasery: każdy wachlarz promieni omiata salę (odchylenie i kiwanie, różne fazy), co 8 bitów zmienia tempo,
//  • głowice ruchome (reflektory) kreślą ósemki snopami,
//  • światła punktowe sali zmieniają barwę w rytm (bit 124/min), bar i loże mają stałe ciepłe światło,
//  • zmienne globalne shaderów: _NovaCzas, _NovaBeat (liczba bitów), _NovaAktywny (0…1 płynnie), _NovaKat.
// Klasa w pliku o tej samej nazwie, pola publiczne bez #if (zasady 1–2). Bez cząsteczek (moduł wyłączony).
public sealed class BraxNovaEfekty : MonoBehaviour
{
    public Transform kula;
    public float obrotyNaMinute = 7f;
    public Transform[] lasery = new Transform[0];
    public Transform[] glowice = new Transform[0];
    public Light[] swiatla = new Light[0];
    public float bpm = 124f;
    public float zasieg = 48f;

    static readonly int idCzas = Shader.PropertyToID("_NovaCzas"), idBeat = Shader.PropertyToID("_NovaBeat"),
                        idAkt = Shader.PropertyToID("_NovaAktywny"), idKat = Shader.PropertyToID("_NovaKat");
    static readonly Color[] Barwy = { new Color(1f, .15f, .75f), new Color(.25f, .45f, 1f), new Color(.6f, .2f, 1f), new Color(.15f, .95f, .85f), new Color(1f, .35f, .2f) };

    Quaternion[] laserStart, glowStart;
    Transform gracz; float szukaj, czas, aktywny, kat;

    void Awake()
    {
        laserStart = new Quaternion[lasery.Length];
        for (int i = 0; i < lasery.Length; i++) if (lasery[i]) laserStart[i] = lasery[i].localRotation;
        glowStart = new Quaternion[glowice.Length];
        for (int i = 0; i < glowice.Length; i++) if (glowice[i]) glowStart[i] = glowice[i].localRotation;
        Shader.SetGlobalFloat(idAkt, 0f);
    }

    bool Blisko()
    {
        if (!gracz && Time.unscaledTime >= szukaj)
        {
            szukaj = Time.unscaledTime + 1f;
            var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform;
        }
        if (!gracz) return false;
        return (gracz.position - transform.position).sqrMagnitude < zasieg * zasieg;
    }

    void Update()
    {
        bool b = Blisko();
        aktywny = Mathf.MoveTowards(aktywny, b ? 1f : 0f, Time.deltaTime * 1.5f);
        Shader.SetGlobalFloat(idAkt, aktywny);
        if (aktywny <= 0f) return;
        czas += Time.deltaTime;
        float beat = czas * bpm / 60f;
        Shader.SetGlobalFloat(idCzas, czas);
        Shader.SetGlobalFloat(idBeat, beat);
        // kula: obrót wokół pionu (kąt w radianach do shadera plamek — ten sam kierunek co obrót siatki kuli)
        kat = Mathf.Repeat(kat + Time.deltaTime * obrotyNaMinute / 60f * Mathf.PI * 2f, Mathf.PI * 2f);
        Shader.SetGlobalFloat(idKat, kat);
        if (kula) kula.localRotation = Quaternion.Euler(0f, -kat * Mathf.Rad2Deg, 0f);
        // lasery: omiatanie (wachlarz wokół osi pionowej) + kiwanie (wokół osi poprzecznej); tempo zmienia się co 8 bitów
        float tryb = Mathf.Floor(beat / 8f) % 3f;
        float tempo = tryb == 0f ? .35f : tryb == 1f ? .7f : .2f;
        for (int i = 0; i < lasery.Length; i++)
        {
            var t = lasery[i]; if (!t) continue;
            float f = i * 1.7f;
            float yaw = Mathf.Sin(czas * tempo * 2.1f + f) * 38f;
            float pitch = Mathf.Sin(czas * tempo * 1.3f + f * .6f) * 16f + (tryb == 2f ? Mathf.Sin(beat * Mathf.PI) * 6f : 0f);
            t.localRotation = laserStart[i] * Quaternion.Euler(pitch, yaw, 0f);
        }
        for (int i = 0; i < glowice.Length; i++)
        {
            var t = glowice[i]; if (!t) continue;
            float f = i * 2.3f, s = czas * .45f;
            t.localRotation = glowStart[i] * Quaternion.Euler(Mathf.Sin(s * 2f + f) * 28f, Mathf.Sin(s + f) * 55f, 0f);
        }
        // światła sali: barwa w rytm (zmiana na każdy takt), jasność z pulsem bitu
        float puls = 1f - Mathf.Repeat(beat, 1f);
        for (int i = 0; i < swiatla.Length; i++)
        {
            var l = swiatla[i]; if (!l) continue;
            int k = ((int)(beat / 4f) + i) % Barwy.Length;
            l.color = Color.Lerp(l.color, Barwy[k], Time.deltaTime * 6f);
            l.intensity = (1.1f + .9f * puls) * aktywny;
        }
    }

    void OnDisable() { Shader.SetGlobalFloat(idAkt, 0f); }
}
