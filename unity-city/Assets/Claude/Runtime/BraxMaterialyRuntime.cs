using UnityEngine;

// RÓŻOWE PIŁKI (właściciel 18.11, zdjęcie z boiska: „różowe piłki, sprawdzić, czy nie ma więcej różowych rzeczy”).
//
// Różowy w Unity nie jest kolorem — tak wygląda obiekt, któremu w gotowej grze zabrakło shadera.
// Piłki, shaker w barze i walec przy ćwiczeniach powstają dopiero W GRZE z brył Unity
// (GameObject.CreatePrimitive). Taka bryła dostaje wbudowany materiał domyślny, a jego wariant
// shadera Standard nie musi trafić do APK, bo w scenie nic go nie używa wprost — i wtedy
// wszystko, co z niego skopiowane, jest różowe.
//
// Lekarstwo: przy buildzie powstaje PRAWDZIWY materiał w Assets/PlayableGenerated, podpięty tutaj
// do obiektu gracza. Skoro leży w scenie, Unity musi go spakować razem z jego shaderem. Obiekty
// tworzone w locie kopiują ten materiał zamiast domyślnego i mają z czego się narysować.
//
// Czemu nie „zawsze dołączaj shader Standard” w ustawieniach grafiki: to pakuje WSZYSTKIE jego
// warianty (dziesiątki MB), a osiedlu brakuje pamięci, nie miejsca na warianty.
public sealed class BraxMaterialyRuntime : MonoBehaviour
{
    [Tooltip("Materiał bazowy dla obiektów tworzonych w czasie gry (piłki, shaker, walec ćwiczeń).")]
    public Material standard;

    static BraxMaterialyRuntime inst;

    void Awake() { inst = this; }

    /// <summary>Materiał bazowy albo null, gdy build go nie przygotował (wtedy wołający używa swojego).</summary>
    public static Material Standard
    {
        get
        {
            if (!inst) inst = FindFirstObjectByType<BraxMaterialyRuntime>(FindObjectsInactive.Include);
            return inst ? inst.standard : null;
        }
    }

    /// <summary>Wzór dla nowego materiału: ten z buildu, a jak go nie ma — materiał podany przez wołającego.</summary>
    public static Material Wzor(Material zapasowy)
    {
        var m = Standard;
        return m ? m : zapasowy;
    }
}
