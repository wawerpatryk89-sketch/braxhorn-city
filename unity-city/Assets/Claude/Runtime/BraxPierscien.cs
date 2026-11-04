using System;
using UnityEngine;

// PIERŚCIEŃ HORYZONTU 3D (pkt 7 / 7b / 15d): każdy element to bryła zbudowana w prawdziwej skali wokół własnego środka
// (wyspa, pasmo wzgórz, góry). Co klatkę stawiana przy kamerze: kierunek i wysokość kątowa jak z prawdziwej odległości
// (offset × k, skala k = R / D), więc wygląda jak 2–9 km dalej i nigdy się nie przybliża (pierścień jedzie z kamerą).
// Nasza wyspa (dynamiczna): prawdziwe położenie w świecie, widoczna, dopóki jej scena nie jest wczytana.
// Głębia elementów jest tuż przed płaszczyzną daleką (shader Braxhorn/MiastoPierscien) — scena zawsze z przodu.
public sealed class BraxPierscien : MonoBehaviour
{
    [Serializable] public sealed class Element
    {
        public Transform t;
        public Vector3 offset;          // pierścień: prawdziwy środek względem kamery (x, z); y = poziom podstawy (świat)
        public bool dynamiczny;         // nasza wyspa: offset = punkt świata
        public string scena;            // ukryj, gdy ta scena jest wczytana (prawdziwa bryła na miejscu)
        public float R = 300f;          // promień w pierścieniu (m); bliższe prawdziwie = mniejszy R (kolejność głębi)
        [NonSerialized] public Renderer[] r;
    }
    public Element[] elementy = new Element[0];
    float nast;
    Camera panoramaKamera;
    float zasięgPrzedTarasem;
    bool naTarasie;

    void LateUpdate()
    {
        var cam = Camera.main; if (!cam) return;
        var c = cam.transform.position;
        // Pierścień 3D ma projekcję 120–500 m, a kamera startowa tylko 100 m.
        // Zasięg podnosimy wyłącznie dla wysokich punktów widokowych, nie na ulicy.
        if (cam != panoramaKamera)
        {
            panoramaKamera = cam;
            zasięgPrzedTarasem = cam.farClipPlane;
            naTarasie = false;
        }
        if (!naTarasie && c.y > 55f)
        {
            zasięgPrzedTarasem = cam.farClipPlane;
            naTarasie = true;
            Debug.Log("BRAXHORN_PANORAMA_360_TARAS widok=" + c.y.ToString("F1") +
                      "m; zasięg 580m zamiast " + zasięgPrzedTarasem.ToString("F1") + "m");
        }
        if (naTarasie && c.y < 45f)
        {
            naTarasie = false;
            cam.farClipPlane = zasięgPrzedTarasem;
        }
        if (naTarasie && cam.farClipPlane < 580f)
            cam.farClipPlane = 580f;
        bool sprawdz = Time.unscaledTime >= nast; if (sprawdz) nast = Time.unscaledTime + .5f;
        foreach (var e in elementy)
        {
            if (e == null || !e.t) continue;
            if (sprawdz && e.dynamiczny && !string.IsNullOrEmpty(e.scena))
            {
                if (e.r == null) e.r = e.t.GetComponentsInChildren<Renderer>(true);
                bool wid = !UnityEngine.SceneManagement.SceneManager.GetSceneByName(e.scena).isLoaded;
                foreach (var rr in e.r) if (rr && rr.enabled != wid) rr.enabled = wid;
            }
            Vector3 off = e.dynamiczny ? new Vector3(e.offset.x - c.x, 0f, e.offset.z - c.z) : new Vector3(e.offset.x, 0f, e.offset.z);
            float D = Mathf.Max(1f, new Vector2(off.x, off.z).magnitude);
            float k = Mathf.Min(1f, e.R / D);
            off.y = e.offset.y - c.y;
            e.t.position = c + off * k;
            e.t.localScale = Vector3.one * k;
        }
    }
}
