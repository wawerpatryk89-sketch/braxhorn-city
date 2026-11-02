using UnityEngine;

// PODGLĄD INTERFEJSU DO ZDJĘĆ CI (build 14, tylko przeglądarka z ?qa=1). Strona wysyła komunikaty do obiektu
// „BraxhornPlayer” (SendMessage), więc ten komponent jest dokładany w grze do obiektu gracza (BraxTouchControls) i przekazuje:
//   BraxSend('ReviewUi', 'przyciski' | 'menu' | 'ustawienia' | 'test' | 'teleport' | 'wczytywanie' | 'zdjecia' | 'wyl')
// do BraxTouchControls.Podglad. Na telefonie komponent nie jest dokładany. Osobny plik = nazwa klasy (twarda reguła 1).
public sealed class BraxInterfejsQa : MonoBehaviour
{
    public void ReviewUi(string tryb) { BraxTouchControls.Podglad(tryb); }
}
