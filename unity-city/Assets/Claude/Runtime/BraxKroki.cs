using UnityEngine;
using UnityEngine.SceneManagement;

// KROKI BRAXA (właściciel 1.11, 18:42: „jak biegnę Braxem, powinno być słychać, jak biegnie — lekki tup-tup”).
// Krok = stopa schodzi na ziemię: wysokość kości stopy (…LeftFoot / …RightFoot) nad podstawą postaci spada poniżej progu
// po wcześniejszym uniesieniu (z histerezą) — działa z każdym klipem chodu i biegu, bez znaczników w animacji.
// Dźwięk syntetyzowany w grze (głuche uderzenie + krótki szelest podeszwy), 4 warianty, losowy ton; bieg głośniej.
// Dokładany sam do gracza po wczytaniu każdej sceny (bez zmian w BraxGymPlayer i w scenach).
public sealed class BraxKroki : MonoBehaviour
{
    Transform lewa, prawa; CharacterController cc; AudioSource src;
    float spocznikL = float.NaN, spocznikP = float.NaN; bool gorL, gorP;
    static AudioClip[] kroki;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { Dopnij(); SceneManager.sceneLoaded += (s, m) => Dopnij(); }
    static void Dopnij()
    {
        var p = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (p && !p.GetComponent<BraxKroki>()) p.gameObject.AddComponent<BraxKroki>();
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (!lewa && t.name.EndsWith("LeftFoot")) lewa = t;
            if (!prawa && t.name.EndsWith("RightFoot")) prawa = t;
        }
        src = gameObject.AddComponent<AudioSource>(); src.spatialBlend = .35f; src.playOnAwake = false; src.priority = 90;
        if (kroki == null) Synteza();
    }

    public static AudioClip Losowy() { if (kroki == null) Synteza(); return kroki[Random.Range(0, kroki.Length)]; }
    static void Synteza()
    {
        const int sr = 22050; kroki = new AudioClip[4]; var r = new System.Random(3);
        for (int k = 0; k < 4; k++)
        {
            int n = (int)(sr * .16f); var x = new float[n]; float f = 70f + k * 9f, sz = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float tup = Mathf.Sin(2f * Mathf.PI * f * t * (1f - t * 1.5f)) * Mathf.Exp(-t * 38f);         // głuche „tup”
                sz += .35f * ((float)(r.NextDouble() * 2 - 1) - sz);                                        // szelest podeszwy
                x[i] = tup * .9f + sz * .55f * Mathf.Exp(-t * 70f);
            }
            float m = 1e-5f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < n; i++) x[i] *= .8f / m;
            kroki[k] = AudioClip.Create("krok " + k, n, 1, sr, false); kroki[k].SetData(x, 0);
        }
    }

    void LateUpdate()
    {
        if (!cc || !cc.enabled || (!lewa && !prawa)) return;
        float v = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
        bool naZiemi = cc.isGrounded && v > .4f;
        Stopa(lewa, ref spocznikL, ref gorL, naZiemi, v);
        Stopa(prawa, ref spocznikP, ref gorP, naZiemi, v);
    }

    void Stopa(Transform s, ref float spocznik, ref bool gora, bool ruch, float v)
    {
        if (!s) return;
        float h = s.position.y - transform.position.y;
        if (float.IsNaN(spocznik) || (!ruch && cc.isGrounded)) { spocznik = float.IsNaN(spocznik) ? h : Mathf.Lerp(spocznik, h, .1f); gora = false; return; }
        if (h > spocznik + .07f) gora = true;
        else if (gora && h < spocznik + .03f)
        {
            gora = false;
            if (!ruch || kroki == null) return;
            src.pitch = Random.Range(.9f, 1.1f) * (v > 3.2f ? 1.06f : 1f);
            src.PlayOneShot(kroki[Random.Range(0, kroki.Length)], v > 3.2f ? .42f : .24f);
        }
    }
}
