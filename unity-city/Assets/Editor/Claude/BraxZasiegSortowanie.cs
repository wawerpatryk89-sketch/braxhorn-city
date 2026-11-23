using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — ZASIĘG WEDŁUG WIELKOŚCI: SORTOWANIE PRZY BUILDZIE (ZASADY 10.4).
//
// Jedna liczba „zasięg kamery” dla wszystkiego daje wybór między dwiema złymi rzeczami:
// albo śmietnik i ławka rysują się z kilometra (Miasto R1: 1600 m w trybie ŚREDNIM), albo
// budynki znikają razem ze śmietnikiem (Osiedle i Mistrzowie: 420 m). Dlatego zasięg zależy
// od wielkości: duże bryły do horyzontu, drobiazgi tylko z bliska.
//
// To jest metoda rejonu Mistrzów (BraxChampionsExplorer.SmallThingsLayers), wyjęta do
// jednego miejsca, żeby Osiedle i Miasto nie pisały jej po swojemu.
// Warunki są przepisane stamtąd bez zmian — sortowany jest TYLKO obiekt, który:
//   • jest zwykłym MeshRendererem na warstwie domyślnej (0),
//   • nie ma własnej kolizji (żeby nic nie zmieniło się w fizyce ani w kolizji kamery),
//   • nie jest częścią Rigidbody (auta, rzeczy ruchome) ani LODGroup — obiektami z poziomami
//     szczegółów rządzi BraxZasiegDrobnych (ZASADY 10.4 b: żeby nic nie wyskakiwało przy postaci).
// Budynki, grunt, duże scalone siatki, drzewa, gracz, lustra i pojazdy zostają, jak były.
//
// UWAGA DLA SIATEK SCALONYCH: obiekt złożony z tysięcy drobiazgów scalonych w jedną siatkę
// ma wymiar całego budynku, więc tu nie trafi i będzie rysowany w całości z każdej odległości.
// Tak było w galerii G1 (16.11): 8,3 mln trójkątów, z czego 7,5 mln w kawałkach poniżej 1,2 m.
// Scalanie robi się PO podziale na wielkości — duże osobno, małe i drobne osobno.
//
// Wołane ze skryptu buildu rejonu, po zbudowaniu sceny, przed zapisem:
//     BraxZasiegSortowanie.Posortuj(scena);
// Zasięgi warstw ustawia w grze BraxPlynnosc (260 i 90 jednostek).
public static class BraxZasiegSortowanie
{
    public static void Posortuj(Scene scena)
    {
        int male = 0, drobne = 0, pominiete = 0, duze = 0;
        foreach (var korzen in scena.GetRootGameObjects())
            foreach (var r in korzen.GetComponentsInChildren<MeshRenderer>(true))
            {
                var g = r.gameObject;
                if (g.layer != 0 || g.GetComponent<Collider>() ||
                    g.GetComponentInParent<Rigidbody>() || g.GetComponentInParent<LODGroup>())
                {
                    pominiete++;
                    continue;
                }
                var s = r.bounds.size;
                int warstwa = BraxPlynnosc.WarstwaDlaRozmiaru(Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
                if (warstwa == BraxPlynnosc.WarstwaDrobne) { g.layer = warstwa; drobne++; }
                else if (warstwa == BraxPlynnosc.WarstwaMale) { g.layer = warstwa; male++; }
                else duze++;
            }

        // Liczby w logu są dowodem (ZASADY 8.2). „male=0 drobne=0” w rejonie pełnym ławek
        // i donic znaczy, że wszystko jest scalone albo ma kolizje — i sortowanie nic nie dało.
        Debug.Log("BRAX_ZASIEG_WARSTWY scena=" + scena.name +
                  " male=" + male + " (ponizej " + BraxPlynnosc.RozmiarMale + " j., do " + BraxPlynnosc.ZasiegMale + " j.)" +
                  " drobne=" + drobne + " (ponizej " + BraxPlynnosc.RozmiarDrobne + " j., do " + BraxPlynnosc.ZasiegDrobne + " j.)" +
                  " duze_bez_zmian=" + duze + " pominiete_kolizja_lod_warstwa=" + pominiete);
    }
}
