using UnityEngine;

// BRAXHORN R2 — wieża w willi: gracz podchodzi (do `zasieg` m od wzmacniacza) i sam włącza/wyłącza muzykę:
//  • przycisk akcji z interfejsu gry (strona wysyła Command("action") do obiektu gracza — odbiera go BraxClaudeAkcja),
//  • klawisz E na komputerze,
//  • przycisk na ekranie (pojawia się tylko przy wieży; działa też dotykiem na telefonie).
// Głośność 2D: pełna w domu (prostopadłościan `dom` = parter, piętro, taras), cichnie na `wygaszenie` m za ścianą.
public class BraxClaudeGrajek : MonoBehaviour
{
    public AudioClip utwor;
    public Bounds dom;
    public float zasieg = 2.6f, glosnosc = .7f, wygaszenie = 6f;
    public static BraxClaudeGrajek Blisko;
    AudioSource src; Transform gracz; float szukaj; bool gra;
    GUIStyle styl;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = utwor; src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 0f; src.priority = 32;
    }

    void Update()
    {
        if (!gracz)
        {
            gracz = BraxClaudeMuzyka.Gracz(ref szukaj); if (!gracz) return;
            if (!gracz.GetComponent<BraxClaudeAkcja>()) gracz.gameObject.AddComponent<BraxClaudeAkcja>();
        }
        bool blisko = (gracz.position - transform.position).sqrMagnitude < zasieg * zasieg;
        if (blisko) Blisko = this; else if (Blisko == this) Blisko = null;
        if (blisko && Input.GetKeyDown(KeyCode.E)) Przelacz();
        float d = Mathf.Sqrt(dom.SqrDistance(gracz.position));
        float cel = gra ? glosnosc * Mathf.Clamp01(1f - d / wygaszenie) : 0f;
        src.volume = Mathf.MoveTowards(src.volume, cel, Time.deltaTime * (gra ? .8f : 1.6f));
        if (!gra && src.volume <= .001f && src.isPlaying) src.Pause();
    }

    public void Przelacz()
    {
        if (!src || !utwor) return;
        gra = !gra;
        if (gra && !src.isPlaying) src.Play();
    }

    void OnGUI()
    {
        if (Blisko != this || !utwor) return;
        if (styl == null) styl = new GUIStyle(GUI.skin.button);
        styl.fontSize = Mathf.Max(14, Screen.height / 24);
        float w = Mathf.Max(220f, Screen.width * .26f), h = Mathf.Max(44f, Screen.height * .085f);
        var r = new Rect((Screen.width - w) * .5f, Screen.height * .70f - h, w, h);
        if (GUI.Button(r, gra ? "■  WYŁĄCZ MUZYKĘ" : "▶  WŁĄCZ MUZYKĘ", styl)) Przelacz();
    }
}
