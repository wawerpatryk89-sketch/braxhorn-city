using UnityEditor;
using UnityEngine;

// BUDKA RATOWNIKA NA PLAŻY MIEJSKIEJ (właściciel 3.11 10:52: „pomaluj ładnie budkę ratownika… nawet nie da się na nią wejść.
// Zła skala”; 11:47: „wystarczy przemalować i trochę powiększyć”). Model ZOSTAJE: lifeguard_tower.glb (Source/HarborBeach,
// = DZWIG_PORTOWY_I_BUDKA_RATOWNIKA.zip właściciela, CC BY exiS7-Gs), stawiany w BraxNabrzezePlaza.Wyposazenie.
// Było: skala do 6.2 m całości (pokład ~2.2 m — bez × k), jedna kolizja-pudło 0.9 × obrys (nie da się wejść ani przejść
// pod pokładem), wyblakłe tekstury (szare deski, błękitna łuszcząca się kabina).
// Teraz:
//  • SKALA × k: pokład modelu (y 11.0 jednostki modelu) na 2.4 m × k nad piaskiem → kabina ~2.45 m × k (model 11.0…22.2);
//  • PRZEMALOWANIE: konstrukcja (base1, base2) biała farba, kabina (base3) czerwona; szyby i rekwizyty (flaga) bez zmian —
//    kopie materiałów miasta bez tekstury barwy (zostaje okluzja modelu), zapisane jako assety;
//  • WEJŚCIE: kolizje wg geometrii modelu (pomiar offline z glb, jednostki modelu → świat przez transform modelu):
//    schody (pochylnia po noskach stopni x 11.35…−3.65, z 3.7…7.8), podest (y 10.0), próg podest → pokład (pochylnia 1 jedn.),
//    pokład (y 11.0), kabina (bryła), balustrady na krawędziach pokładu i podestu oraz po zewnętrznej stronie schodów.
//    Kolizje stawiane tylko, gdy obrys modelu po transformacji zgadza się z bryłą renderera (≤ 0.15 m) — inaczej log DO POPRAWKI
//    i zostaje stara kolizja-pudło (bezpiecznie: nie da się wejść, ale nic nie przenika).
public static class BraxNabrzezeRatownik
{
    public const float WysModelu = 31.013f, PokladModelu = 11.0f;   // jednostki modelu (pomiar glb): całość z flagą, pokład
    static float K => BraxNabrzeze.K;
    public static float Skala => 2.4f * K / PokladModelu;          // jednostka modelu → metr świata

    public static void Przemaluj(GameObject budka)
    {
        var bialy = new Color(.93f, .93f, .9f); var czerw = new Color(.74f, .1f, .08f);
        int n = 0;
        foreach (var r in budka.GetComponentsInChildren<Renderer>(true))
        {
            string nm = r.name.ToLowerInvariant(); bool kab = nm.StartsWith("base3"), kon = nm.StartsWith("base1") || nm.StartsWith("base2");
            if (!kab && !kon) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                var m = new Material(mats[i]) { name = kab ? "Ratownik: kabina (czerwona farba)" : "Ratownik: konstrukcja (biała farba)" };
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
                if (m.HasProperty("_Color")) m.SetColor("_Color", kab ? czerw : bialy);
                if (m.HasProperty("_Roughness")) m.SetFloat("_Roughness", .55f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                BraxZapisAssetu.Utworz(m, BraxNabrzeze.Out + "/ratownik_" + (kab ? "kabina" : "konstrukcja") + "_" + (n++) + ".mat");
                mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
        BraxNabrzeze.Log("budka ratownika: przemalowana (" + n + " materiałów: konstrukcja biała, kabina czerwona, szyby i flaga bez zmian)");
    }

    /// <summary>Kolizje wejścia wg geometrii modelu. Zwraca liczbę kolizji (0 = pominięte, obrys się nie zgadza).</summary>
    public static int Wejscie(GameObject budka, Transform kol, out float pokladY)
    {
        var t = budka.transform; pokladY = 0f;
        Vector3 W(float x, float y, float z) => t.TransformPoint(new Vector3(-x, y, z));   // glTFast: x Unity = −x glTF
        // kontrola: obrys modelu (jednostki glTF) po transformacji = bryła renderera
        var bm = new Bounds(W(-9.9f, 0f, -14f), Vector3.zero);
        foreach (var x in new[] { -9.9f, 10.8f }) foreach (var y in new[] { 0f, WysModelu }) foreach (var z in new[] { -14f, 9f }) bm.Encapsulate(W(x, y, z));
        var br = BraxRegion.Bounds(budka);
        float blad = Mathf.Max((bm.center - br.center).magnitude, (bm.size - br.size).magnitude);
        if (blad > .15f) { BraxNabrzeze.Log("budka ratownika: DO POPRAWKI kolizje wejścia pominięte — obrys modelu ≠ renderer (" + blad.ToString("F2") + " m)"); return 0; }
        int n = 0;
        void Box(string nazwa, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            var a = W(x0, y0, z0); var b = W(x1, y1, z1);
            BraxNabrzeze.KolMM(kol, "Budka ratownika: " + nazwa, Vector3.Min(a, b), Vector3.Max(a, b)); n++;
        }
        // pochylnia od g0 do g1 (glTF), szerokość szer wzdłuż osi bokG (glTF), górna powierzchnia w linii g0–g1
        void Rampa(string nazwa, Vector3 g0, Vector3 g1, float szer, Vector3 bokG)
        {
            var a = W(g0.x, g0.y, g0.z); var b = W(g1.x, g1.y, g1.z); var dir = (b - a).normalized;
            var e = W(g0.x + bokG.x * szer, g0.y, g0.z + bokG.z * szer) - a; float wS = e.magnitude; var bok = e / Mathf.Max(1e-4f, wS);
            var up = Vector3.Cross(dir, bok).normalized; if (up.y < 0) up = -up;
            BraxNabrzeze.KolRot(kol, "Budka ratownika: " + nazwa, (a + b) * .5f - up * .06f, new Vector3(wS, .12f, Vector3.Distance(a, b) + .1f), Quaternion.LookRotation(dir, up)); n++;
        }
        // schody: noski stopni y = 0.9 + (10 − x)·(0.6/0.9), od piasku (x 11.35) do podestu (x −3.65, y 10.0); środek z 5.75, szer. 4.1
        Rampa("schody", new Vector3(11.35f, 0f, 5.75f), new Vector3(-3.65f, 10.0f, 5.75f), 4.1f, Vector3.forward);
        Box("podest", -9.0f, 9.6f, 3.5f, -3.1f, 10.0f, 8.0f);
        Rampa("próg podest–pokład", new Vector3(-6.65f, 10.0f, 4.6f), new Vector3(-6.65f, 11.0f, 3.0f), 4.7f, Vector3.right);
        Box("pokład", -9.5f, 10.4f, -12.0f, 9.5f, 11.0f, 3.0f);
        Box("kabina", -4.4f, 11.0f, -11.8f, 4.6f, 22.2f, -1.0f);
        // balustrady (do poręczy modelu y 14.7): krawędzie pokładu poza wejściem z podestu (x −9.5…−4.2 na z 3), podest, schody
        Box("balustrada płn. pokładu", -4.2f, 11.0f, 3.0f, 9.9f, 14.7f, 3.4f);
        Box("balustrada wsch. pokładu", 9.5f, 11.0f, -12.0f, 9.9f, 14.7f, 3.4f);
        Box("balustrada zach. pokładu", -9.9f, 11.0f, -12.0f, -9.5f, 14.7f, 3.0f);
        Box("balustrada płd. pokładu", -9.9f, 11.0f, -12.4f, 9.9f, 14.7f, -12.0f);
        Box("balustrada podestu (zach.)", -9.4f, 10.0f, 3.0f, -9.0f, 13.7f, 8.2f);
        Box("balustrada podestu (płn.)", -9.4f, 10.0f, 8.0f, -3.1f, 13.7f, 8.4f);
        {   // zewnętrzna strona schodów (z 8.1): ukośna ściana wzdłuż biegu
            var a = W(11.35f, 0f, 8.1f); var b = W(-3.65f, 10f, 8.1f); var dir = (b - a).normalized; var bok = (W(11.35f, 0f, 9f) - a).normalized;
            var up = Vector3.Cross(dir, bok).normalized; if (up.y < 0) up = -up; float h = 3.7f * Skala;
            BraxNabrzeze.KolRot(kol, "Budka ratownika: balustrada schodów", (a + b) * .5f + up * (h * .5f), new Vector3(.1f, h, Vector3.Distance(a, b)), Quaternion.LookRotation(dir, up)); n++;
        }
        pokladY = W(0f, PokladModelu, 0f).y;
        return n;
    }
}
