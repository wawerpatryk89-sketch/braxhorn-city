using UnityEngine;

/// <summary>
/// STANDARD POSTACI DLA CAŁEJ GRY — nakładany po każdym wczytaniu sceny.
///
/// Powód (właściciel 18.11, 10:18: „mamy już ustawienia pod postacie globalne"):
/// ustawienia BYŁY, ale tylko na gałęzi pokoju prób. Na `main` nie było ich wcale,
/// więc postacie w innych rejonach dostawały co innego. To ta sama choroba, co
/// z zasadami obrazu — i leczona tak samo: jeden plik na `main`, który sam się
/// nakłada, zamiast ośmiu skryptów rejonów robiących po swojemu.
///
/// Co wymusza i dlaczego — pomiary, nie upodobania:
///
/// 1. POŁYSK NA ZERO. Właściciel 16.11, 21:56: „Naturalna skóra ma być, żadnych
///    białych odblasków, nic". Mapy gładkości z generatora 3D są wyliczane, nie
///    malowane: ten sam połysk dostaje wełna, skóra i dłoń. Na ciemnej tkaninie
///    odblask jest JEDYNĄ rzeczą, którą widać — zmierzone na ochroniarzu: w jego
///    teksturze nie ma ani jednego białego piksela (maksimum 204, mediana 35),
///    a mimo to miał „białe ślady" na barku. To było światło, nie malunek.
///    Tak samo rozmywał się krawat kapitana: jasność 42 na koszuli 229, gładkość
///    0,449 — najwyższa na całej postaci.
///
/// 2. CZTERY KOŚCI NA WIERZCHOŁEK. Zmierzone na całej obsadzie: 43–68 % wierzchołków
///    trzymała jedna albo dwie kości, stąd załamania na łokciach i kolanach.
///    Wagi poprawia potok, ale renderer musi jeszcze CHCIEĆ czytać cztery — przy
///    SkinQuality.Bone2 najlepsze wagi i tak idą do kosza.
///
/// 3. SONDY ŚWIATŁA I CIENIE. Postać bez sond jest oświetlona inaczej niż otoczenie
///    i „nie stoi" w scenie.
///
/// Zasada jest jednostronna: to jest PODŁOGA. Rejon może dać postaci lepiej
/// (własne mapy, wyższa jakość), nie może dać gorzej.
///
/// Wyjątek: obiekt z komponentem <see cref="BraxObrazWyjatek"/> gdziekolwiek nad
/// sobą jest pomijany, a powód trafia do logu — po cichu się nie da.
///
/// Reguły: ZASADY.md w korzeniu repozytorium, punkt 5.
/// </summary>
[DefaultExecutionOrder(-9000)]
public static class BraxPostacGlobalna
{
    /// Gładkość postaci. 0 = skóra i tkanina całkowicie matowe.
    public const float Gladkosc = 0f;

    /// Metaliczność. Tkanina i skóra nie są metalem — mapy z generatora potrafią
    /// dać 0,60 na całej postaci, co jest fizycznie niemożliwe.
    public const float Metalicznosc = 0f;

    /// Wzmocnienie mapy normalnych. Mapy z generatora są prawie płaskie
    /// (odchylenie 0,02 przy 74 % pikseli całkiem płaskich), więc lekkie
    /// podbicie oddaje fakturę. Wyżej niż ~1,5 robi się szum na skórze.
    public const float SilaNormalnych = 1.3f;

    static readonly int IdGladkosc = Shader.PropertyToID("_Gladkosc");
    static readonly int IdMetal = Shader.PropertyToID("_MetalMnoznik");
    static readonly int IdNormalne = Shader.PropertyToID("_SilaNormalnych");
    static readonly int IdStdGloss = Shader.PropertyToID("_Glossiness");
    static readonly int IdStdMetal = Shader.PropertyToID("_Metallic");
    // Braxhorn/HeroPremium — shader Patryka, Braxa i Nyxy. NIE MA żadnej z powyższych nazw, więc
    // standard „bez odblasków" go dotąd OMIJAŁ: bohaterowie zostawali błyszczący, choć właściciel
    // zaakceptował matowe (18.11: „postacie takie jak je zaakceptowałem, żadne inne. Wszystkie").
    // Tutaj matowość nazywa się inaczej: szorstkość, i działa odwrotnie niż gładkość — 1 = matowo.
    static readonly int IdHeroSzorstkosc = Shader.PropertyToID("_RoughnessFloor");
    static readonly int IdHeroMetal = Shader.PropertyToID("_MetallicScale");
    static readonly int IdHeroNormalne = Shader.PropertyToID("_BumpScale");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Podlacz()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => Nalozi();
        Nalozi();
    }

    static bool Pominac(Transform t)
    {
        for (var p = t; p; p = p.parent)
            if (p.GetComponent<BraxObrazWyjatek>()) return true;
        return false;
    }

    /// <summary>Nakłada standard na wszystkie postacie w scenie. Można wołać ręcznie
    /// po doładowaniu obsady w trakcie gry.</summary>
    public static void Nalozi()
    {
        var skory = Object.FindObjectsByType<SkinnedMeshRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        int postaci = 0, pominietych = 0, bohaterow = 0;
        foreach (var r in skory)
        {
            if (!r) continue;
            if (Pominac(r.transform)) { pominietych++; continue; }

            // Cztery kości na wierzchołek — inaczej poprawione wagi idą do kosza.
            if (r.quality != SkinQuality.Bone4) r.quality = SkinQuality.Bone4;
            r.receiveShadows = true;
            if (r.lightProbeUsage == UnityEngine.Rendering.LightProbeUsage.Off)
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            if (r.reflectionProbeUsage == UnityEngine.Rendering.ReflectionProbeUsage.Off)
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;

            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue;
                // Shader obsady ma własne mnożniki; zwykły Standard ma _Glossiness.
                if (m.HasProperty(IdGladkosc))
                {
                    m.SetFloat(IdGladkosc, Gladkosc);
                    if (m.HasProperty(IdMetal)) m.SetFloat(IdMetal, Metalicznosc);
                    if (m.HasProperty(IdNormalne)) m.SetFloat(IdNormalne, SilaNormalnych);
                }
                else if (m.HasProperty(IdHeroSzorstkosc))
                {
                    // Podłoga szorstkości w górę do pełnej matowości = odpowiednik gładkości 0.
                    // Tylko w górę: rejon, który ma już bardziej matowo, zostaje ze swoim.
                    float matowo = 1f - Gladkosc; bohaterow++;
                    if (m.GetFloat(IdHeroSzorstkosc) < matowo) m.SetFloat(IdHeroSzorstkosc, matowo);
                    if (m.HasProperty(IdHeroMetal) && m.GetFloat(IdHeroMetal) > Metalicznosc)
                        m.SetFloat(IdHeroMetal, Metalicznosc);
                    // Siła mapy normalnych też jak w wariancie 6 — właściciel 18.11, 16:46:
                    // „była jedna wersja, ta 6 bez żadnych odblasków, i nikt nie ma wyglądać inaczej.
                    // Nikt." _BumpScale mnoży XY mapy normalnych dokładnie tak samo jak
                    // _SilaNormalnych w shaderze obsady, więc ta sama liczba znaczy to samo.
                    if (m.HasProperty(IdHeroNormalne)) m.SetFloat(IdHeroNormalne, SilaNormalnych);
                }
                else if (m.HasProperty(IdStdGloss))
                {
                    // Tu nie ma mnożnika, więc gładkość ustawiam wprost — ale tylko
                    // w dół: rejon, który ma już matowo, zostaje ze swoim.
                    if (m.GetFloat(IdStdGloss) > Gladkosc) m.SetFloat(IdStdGloss, Gladkosc);
                    if (m.HasProperty(IdStdMetal) && m.GetFloat(IdStdMetal) > Metalicznosc)
                        m.SetFloat(IdStdMetal, Metalicznosc);
                }
            }
            postaci++;
        }
        if (postaci > 0 || pominietych > 0)
            Debug.Log($"BRAX_POSTAC standard nałożony na {postaci} postaci"
                      + (pominietych > 0 ? $", pominięto {pominietych} z wyjątkiem" : "")
                      + $" (gładkość {Gladkosc}, metal {Metalicznosc}, normalne {SilaNormalnych}, Bone4)"
                      + $"; materiałów bohaterskich (HeroPremium) zmatowionych: {bohaterow}");
    }
}
