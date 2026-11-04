# Obóz #12 — limit czasu testu życia obozu

Stan: poprawka przygotowana, bez nowego uruchomienia Unity/APK. Zachowano
Monikę z `736df34d5d0a99a061c76d7f137e61d985a1308d` oraz późniejszy import
wspólnych źródeł Patryka z `490213a29605e0afdefa433ac75f115a56801975`.

## Dowód awarii

- Run [41892941205](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41892941205),
  job `126794837417`, SHA `f333a94fd2345eb431b08386ff9db654f32fc1af`.
- Unity `6000.0.58f2`, renderer `llvmpipe (LLVM 15.0.7, 256 bits)`.
- Krok „Rzeczywista scena, trasy i rotacja NPC bez APK” zakończył się błędem
  `TimeoutException: Próba życia obozu: limit 1200 s` o 20:22:49 UTC.
  Symulacja działała nadal, ostatnia próbka miała 352.0 s, wymagany koniec 420 s.
  Kompilacja Android APK nie rozpoczęła się.
- Pobrano [artefakt 13358449934](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41892941205/artifacts/13358449934):
  384431012 bajtów, SHA-256
  `b3b3838c52334b9adf023607f1b8f8f07c94009b0b58448aadad26f12503fb34`.
  Zawiera 2213 PNG i 3 raporty JSON; hash lokalnego ZIP jest zgodny z GitHubem.

`camp-life-review.json` zawiera 1409 próbek i dokładnie jeden błąd: timeout.
Zapisane wyniki: 7 rund (3/2/2), 141 trafień w worki, 21 trafień sparingowych,
2 polecenia, 25 wypowiedzi, 21 zdarzeń dźwiękowych trafień. Monika rozpoczęła
i zakończyła sparing na wszystkich trzech arenach; podczas jej sparingu worki
otrzymały 18 trafień. Przesunięcie NPC i obrót worków podczas pauzy: zero.
`navigation-review.json`: 90/90 kompletnych tras. `final-pose-review.json`:
7 postaci × 14 przypadków, komplet pomiarów, zero zgłoszonych błędów.
To wyniki wcześniejszego SHA, a nie test nowych animacji Moniki ani telefonu.
Nie zmieniono nieudanego wyniku runu na sukces.

## Mechanizm i poprawka

Bezpośrednia przyczyna przerwania jest potwierdzona: sztywny limit czasu
rzeczywistego przerwał test przed pełnymi 420 sekundami symulacji. Nie ma w tym
raporcie błędu tras, trafień, pauzy ani nieskończonej pętli; postęp był ciągły.

Kod tworzył `lifeCamera` i `lifePoseCamera` z domyślnym `enabled=true`, mimo
że używa ich tylko przez jawne `Camera.Render` podczas zapisu PNG. Renderowały
więc dodatkowo na każdej klatce Play Mode. To konkretny zbędny koszt na rendererze
programowym; jego dokładny udział w czasie i skuteczność naprawy wymagają pomiaru.

Wyłączono automatyczne renderowanie tych dwóch kamer oraz jednorazowej kamery
autobusu. Zdjęcia nadal powstają przez te same jawne wywołania, z tymi samymi
rozmiarami, materiałami, mapami i częstotliwością. Kamera gracza pozostaje aktywna,
żeby zachować Camera.main, widoczność i rzeczywiste zachowanie prezentacji gry.
Dodano sprawdzenie, że kamery diagnostyczne nie włączają się samoczynnie,
oraz czas symulacji, czas rzeczywisty i ich stan do raportu końcowego.

Oficjalna dokumentacja Unity 6 [Camera.Render](https://docs.unity3d.com/cn/6000.0/ScriptReference/Camera.Render.html)
wskazuje wyłączoną kamerę i jawne wywołanie Render jako sposób ręcznego renderowania.

Nie skrócono testu (420 s), nie zmieniono kroku symulacji (.05 s), limitu 1200 s,
próbkowania, liczby zdjęć, warunków odbioru, fizyki, tras, sterowania ani wyglądu.
Nie zapisano zmian do sceny. Pliki workflow i zlecenia pozostają identyczne.

## Walidacja i dalszy odbiór

Porównano źródła z dokładnymi hashami Git bieżącej bazy: 275 plików C# w Assets,
w tym równoległą zmianę Patryka. Zastosowano tylko zmianę pliku testu. Kontrola
składni i lokalnych zakresów C# przechodzi. To nie jest kompilator Unity/API.
Przejrzano diff: zachowane wszystkie dotychczasowe bramki i ręczne wywołania
zapisujące PNG. Nie dodano testu tekstowego, który jedynie sprawdza własną zmianę.

Po osobnym poleceniu właściciela wymagane jest rzeczywiste uruchomienie:
420 s symulacji przed 1200 s czasu testu, `manualDiagnosticCameras=true`, komplet
tras/rund/sparingów/pauzy, co najmniej 500 próbek i 100 PNG. Zdjęcia trzeba obejrzeć.
Dopiero ten wynik potwierdzi usunięcie timeoutu. Nowe ruchy Moniki wymagają osobnej
oceny stóp i przejść oraz skoku. Nie uruchomiono buildu, dispatch ani retry.
