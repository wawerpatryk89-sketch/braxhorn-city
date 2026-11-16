using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// SIATKA TWARZY — próba mimiki tak, jak się ją ocenia naprawdę.
//
// Właściciel 16.11, 21:31 pokazał nagranie „BRAXHORN Mowa — próba głosu, 17–20 z 68”:
// cztery twarze naraz, kadr na samą twarz, nazwa pod spodem, ciemne tło. I zapytał, jaką
// mimikę mają tamte postacie. To jest właściwy wzorzec i tu jest odtworzony.
//
// Dlaczego nie da się tego ocenić w pokazie całej sylwetki: kamera stoi wtedy ponad trzy metry
// od postaci, więc twarz ma na telefonie około stu pikseli. Przy takim kadrze usta i powieki
// są mniejsze od kilku pikseli i nie widać ich, choćby chodziły idealnie.
//
// Każda postać dostaje własną kamerę z własnym prostokątem na ekranie (2x2). Kamery stoją
// PRZED twarzą, licząc od kierunku, w którym postać patrzy. Mówią po kolei, żeby słychać było
// jedno nagranie, a nie cztery naraz — mówiąca ma podświetloną nazwę.
public sealed class BraxTwarzeSiatka : MonoBehaviour
{
    public Animation[] postacie;
    public string[] nazwy;
    public bool[] kobieta;
    public Camera[] kamery;
    public const float SKALA = 2.33f / 1.80f;
    public const int NagranNaOsobe = 3;

    int mowi = -1;
    string stan = "";
    readonly List<Transform> glowy = new List<Transform>();

    static Transform Glowa(Transform korzen)
    {
        foreach (var t in korzen.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("Head", System.StringComparison.Ordinal) &&
                !t.name.Contains("Top")) return t;
        return null;
    }

    IEnumerator Start()
    {
        foreach (var a in postacie)
        {
            glowy.Add(a ? Glowa(a.transform) : null);
            if (!a) continue;
            if (a.GetClip("Idle")) { a["Idle"].wrapMode = WrapMode.Loop; a.Play("Idle"); }
            // W próbie mrugnięcie ma być widać kilka razy, a nie raz na ujęcie.
            foreach (var m in a.GetComponentsInChildren<BraxMimikaOsiedla>(true))
                m.odstepMrugania = .35f;
        }

        var wszystkie = Resources.LoadAll<AudioClip>("OsiedleGlosy");
        while (true)
        {
            for (int i = 0; i < postacie.Length; i++)
            {
                var a = postacie[i];
                if (!a) continue;
                mowi = i;
                bool ona = i < kobieta.Length && kobieta[i];
                var pasujace = new List<AudioClip>();
                foreach (var k in wszystkie)
                    if (k && k.name.EndsWith(ona ? "_f" : "_m", System.StringComparison.Ordinal))
                        pasujace.Add(k);
                var pula = pasujace.Count > 0 ? pasujace : new List<AudioClip>(wszystkie);

                var zrodlo = a.GetComponent<AudioSource>();
                if (!zrodlo) zrodlo = a.gameObject.AddComponent<AudioSource>();
                zrodlo.spatialBlend = 0f;
                zrodlo.playOnAwake = false;
                var mim = a.GetComponentInChildren<BraxMimikaOsiedla>();

                for (int n = 0; n < NagranNaOsobe && pula.Count > 0; n++)
                {
                    var klip = pula[(i * 11 + n * 7) % pula.Count];
                    zrodlo.clip = klip;
                    zrodlo.Play();
                    if (mim) mim.Wypowiedz(zrodlo);
                    stan = "mówi: " + (i < nazwy.Length ? nazwy[i] : a.name) +
                           "  ·  " + (n + 1) + "/" + NagranNaOsobe +
                           (ona ? "  ·  głos kobiecy" : "");
                    float do_ = Time.time + klip.length + .2f;
                    while (Time.time < do_) yield return null;
                }
                if (zrodlo.isPlaying) zrodlo.Stop();
                yield return new WaitForSeconds(.3f);
            }
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < kamery.Length && i < postacie.Length; i++)
        {
            if (!kamery[i] || !postacie[i]) continue;
            var g = i < glowy.Count ? glowy[i] : null;
            var cel = g ? g.position + Vector3.up * (.07f * SKALA)
                        : postacie[i].transform.position + Vector3.up * (1.62f * SKALA);
            var przod = postacie[i].transform.forward;
            przod.y = 0f;
            if (przod.sqrMagnitude < .0001f) przod = Vector3.back;
            przod.Normalize();
            // 0,52 m od twarzy: głowa i ramiona wypełniają kafelek, tak jak we wzorcu.
            kamery[i].transform.position = cel + przod * (.52f * SKALA) + Vector3.up * (.015f * SKALA);
            kamery[i].transform.LookAt(cel);
        }
    }

    void OnGUI()
    {
        int ile = Mathf.Min(kamery.Length, postacie.Length);
        var podpis = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * .021f),
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
        };
        for (int i = 0; i < ile; i++)
        {
            if (!kamery[i]) continue;
            var r = kamery[i].rect;
            // Nazwa POD twarzą, nie na niej — we wzorcu właściciela podpis jest pod kafelkiem.
            float x = r.x * Screen.width, szer = r.width * Screen.width;
            float y = (1f - r.y - r.height) * Screen.height;
            float wys = r.height * Screen.height;
            var pasek = new Rect(x, y + wys - Screen.height * .035f, szer, Screen.height * .035f);
            GUI.color = new Color(0f, 0f, 0f, .55f);
            GUI.DrawTexture(pasek, Texture2D.whiteTexture);
            GUI.color = mowi == i ? new Color(1f, .92f, .45f) : Color.white;
            GUI.Label(new Rect(pasek.x + 12, pasek.y, pasek.width - 16, pasek.height),
                      (i < nazwy.Length ? nazwy[i] : postacie[i].name) + (mowi == i ? "   ♪" : ""),
                      podpis);
            GUI.color = Color.white;
        }
        var duzy = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * .024f),
            fontStyle = FontStyle.Bold,
        };
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 10, Screen.width - 32, 40), "PRÓBA MIMIKI  ·  " + stan, duzy);
    }
}
