using System.Collections.Generic;
using UnityEngine;

// ROZMOWY NA OSIEDLU (właściciel 1.11, 18:35: „żeby ludzie z tobą gadali, odzywali się do siebie”). Dymki nad głowami
// (rysowane jednym OnGUI dla całej sceny) i „gwar” — krótkie sylaby syntetyzowane w grze (bez nagrań i licencji),
// wysokość głosu inna dla każdej postaci. Kwestie po polsku, bez imion postaci. Pod misje: BraxDymki.Pokaz z dowolnego
// skryptu (np. zlecenie od przechodnia).
public sealed class BraxDymki : MonoBehaviour
{
    sealed class Dymek { public Transform cel; public string tekst; public float od, doKiedy, wys; }
    static BraxDymki inst;
    readonly List<Dymek> lista = new List<Dymek>();
    GUIStyle styl; static AudioClip gwar;

    public static void Pokaz(Transform cel, string tekst, float czas, float wys = 2.25f)
    {
        if (!cel || string.IsNullOrEmpty(tekst)) return;
        if (!inst) inst = new GameObject("BRAXHORN dymki rozmów").AddComponent<BraxDymki>();
        inst.lista.RemoveAll(d => d.cel == cel);
        inst.lista.Add(new Dymek { cel = cel, tekst = tekst, od = Time.time, doKiedy = Time.time + czas, wys = wys });
    }

    void OnGUI()
    {
        var kam = Camera.main; if (!kam || lista.Count == 0) return;
        lista.RemoveAll(d => !d.cel || Time.time > d.doKiedy);
        float s = Mathf.Min(Screen.width, Screen.height);
        if (styl == null)
        {
            styl = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            styl.normal.textColor = new Color(.1f, .1f, .1f);
            var tlo = new Texture2D(1, 1); tlo.SetPixel(0, 0, new Color(1f, 1f, 1f, .92f)); tlo.Apply(); styl.normal.background = tlo;
            styl.padding = new RectOffset(10, 10, 6, 6);
        }
        foreach (var d in lista)
        {
            Vector3 w = d.cel.position + Vector3.up * d.wys, sp = kam.WorldToScreenPoint(w);
            float odl = Vector3.Distance(kam.transform.position, w);
            if (sp.z < .3f || odl > 24f) continue;
            float alfa = Mathf.Clamp01((d.doKiedy - Time.time) / .4f) * Mathf.Clamp01((Time.time - d.od) / .2f);
            styl.fontSize = Mathf.RoundToInt(s * Mathf.Lerp(.034f, .022f, odl / 24f));
            float szer = Mathf.Min(Screen.width * .42f, styl.CalcSize(new GUIContent(d.tekst)).x + 24f);
            float wys = styl.CalcHeight(new GUIContent(d.tekst), szer);
            var r = new Rect(sp.x - szer * .5f, Screen.height - sp.y - wys, szer, wys);
            var c = GUI.color; GUI.color = new Color(1, 1, 1, alfa); GUI.Box(r, d.tekst, styl); GUI.color = c;
        }
    }

    // Gwar: 1.6 s sylab (formanty samogłosek a/e/i/o/u na tonie krtaniowym 140 Hz), tworzony raz, w grze.
    public static AudioClip Gwar()
    {
        if (gwar) return gwar;
        const int sr = 22050; int n = (int)(sr * 1.6f); var x = new float[n]; var r = new System.Random(5);
        float[,] F = { { 730, 1090 }, { 530, 1840 }, { 300, 2290 }, { 570, 840 }, { 330, 870 } };
        float t = .05f;
        while (t < 1.45f)
        {
            int v = r.Next(5); float dl = .09f + (float)r.NextDouble() * .1f, f0 = 140f * (.92f + (float)r.NextDouble() * .16f);
            int s0 = (int)(t * sr), sn = (int)(dl * sr);
            for (int i = 0; i < sn && s0 + i < n; i++)
            {
                float u = i / (float)sn, tt = i / (float)sr, a = 0f;
                for (int k = 1; k <= 22; k++)
                {
                    float f = k * f0, g = 1f / (1f + Mathf.Pow((f - F[v, 0]) / 90f, 2)) + .55f / (1f + Mathf.Pow((f - F[v, 1]) / 140f, 2));
                    a += g * Mathf.Sin(2f * Mathf.PI * f * tt) / k;
                }
                float env = Mathf.Min(1f, u * 8f) * Mathf.Min(1f, (1f - u) * 5f);
                float spol = i < sr * .018f ? ((float)r.NextDouble() * 2f - 1f) * .25f : 0f;   // krótka spółgłoska na początku
                x[s0 + i] += (a * .5f + spol) * env;
            }
            t += dl + (r.NextDouble() < .25 ? .12f : .025f);
        }
        float m = 1e-5f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < n; i++) x[i] *= .7f / m;
        gwar = AudioClip.Create("gwar rozmowy", n, 1, sr, false); gwar.SetData(x, 0);
        return gwar;
    }

    static readonly System.Random los = new System.Random();
    public static string Los(string[] z) => z[los.Next(z.Length)];

    public static readonly string[] Powitania = { "Siema, Brax!", "O, Brax! Cześć!", "Elo! Co tam?", "Dzień dobry!", "Ale forma, szacun!", "Trening dziś był?", "Hej!", "Cześć, mistrzu!" };
    public static readonly string[] Wiesci =
    {
        "Słyszałeś? Za kasynem jest siłownia BRAXHORN. Podobno sprzęt jak w zawodowym klubie.",
        "W Parku Wiśniowym chłopaki grają w kosza. Wpadnij, może trafisz za trzy.",
        "W hali walk szykują galę. Ludzie już pytają o bilety.",
        "Na Poznańskiej zrobili rondo. W końcu nie stoi się w korku pod tunelem.",
        "Fontanna w parku znowu działa. Dzieciaki mają frajdę.",
        "W Shake Barze robią najlepsze szejki na osiedlu. Spróbuj bananowego.",
        "Tunelem dojdziesz do miasta. Tam dopiero się dzieje.",
        "Pogoda dziś jak marzenie. Aż chce się pobiegać.",
        "Mój syn chce trenować kick-boxing. Przyjmujecie dzieciaki?",
        "W kasynie wczoraj ktoś rozbił bank. Cała dzielnica o tym gada.",
        "Na korcie ktoś zostawił wyrzutnię piłek. Można poodbijać.",
        "Mapę osiedla masz w menu. Ja bym się tu bez niej zgubił.",
    };
    public static readonly string[] OdpowiedziBraxa = { "Wpadnę, zobaczę.", "Dzięki, dobrze wiedzieć.", "No to trzeba sprawdzić.", "Brzmi nieźle.", "Jasne, zapraszam na trening.", "Dzięki, trzymaj się." };
    public static readonly string[] Pozegnania = { "Powodzenia!", "Trzymaj się!", "To do zobaczenia!", "Nara!", "Miłego dnia!" };
    public static readonly string[][] MiedzySoba =
    {
        new[] { "Widziałeś nowe auta na parkingu?", "Te Porsche? Pewnie ktoś z kasyna.", "No ba." },
        new[] { "Idziesz jutro na siłownię?", "Rano, przed pracą.", "To się widzimy." },
        new[] { "Ale dziś słońce, co?", "Wreszcie. Cały tydzień lało." },
        new[] { "Byłeś w nowym Shake Barze?", "Byłem. Truskawkowy daje radę." },
        new[] { "Gdzie tu jest komisariat?", "Na Wiśniowej, za skrzyżowaniem.", "Dzięki!" },
        new[] { "Słyszałeś o gali w hali walk?", "Słyszałem. Brax ma podobno wystąpić.", "To trzeba iść!" },
        new[] { "Kupujesz tę willę na sprzedaż?", "Chciałbym. Ale ceny kosmos." },
        new[] { "Fajnie, że zrobili to rondo.", "No, wcześniej był tu chaos." },
        new[] { "Grasz dziś w kosza?", "Jak skończę robotę. Wpadaj o szóstej." },
    };
}
