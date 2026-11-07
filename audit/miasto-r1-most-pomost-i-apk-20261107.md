# MIASTO R1 — rozpoznanie po 7.11.2026 (bez zmiany geometrii i bez APK)

## Fakty z GitHub Actions
- Run APK #110: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42374195080
- Unity wypisało `Build succeeded` i `build/Android/braxhorn-miastogl.apk` = **738 857 496 B**.
- Zadanie GitHub zakończyło się `failure` w **dostawie APK**, nie w kompilacji Unity.
- Ostatnie linie: `build/Android/miastogl-validation.json: Permission denied`. Katalog wygenerował Docker Unity na koncie `root`, a shell GitHub runner nie mógł pisać tam raportu.
- Watcher wypisał `Odrzucono APK miastogl: brak poprawnych danych komponentu morza`, ale na podstawie tej próby nie wolno stwierdzić uszkodzenia morza: redirection failed przed poprawną walidacją.
- Naprawa: `.github/workflows/claude-region-build.yml` zapisuje diagnostykę w `$RUNNER_TEMP/wyslane/`; faktyczna kontrola integralności komponentu `BraxMorzeZaKamera` **pozostaje aktywna**.
- Limit docelowego instalatora R1 według `AGENTS.md`: dążyć do 500 MB. Samo kompilowanie scen `BraxGymBuild.BuildRenovationAndroid()` wewnątrz APK nadal nie jest systemem osobnych zewnętrznych pakietów Addressables.
- Znak `Build succeeded` nie dowodzi działania gry na telefonie; testu Androida dla #110 nie wykonano.

## Rozróżnienie mostów w kodzie R1
### Stary wiadukt przy ulicy Wiaduktowej
- `unity-city/Assets/Editor/Claude/Region/BraxRegionMiastoB.cs:10-18,103-115,176-186,901-911`: dawny `Riveted steel viaduct` i jego kolizje są **celowo usuwane**, a w miejscu powstaje skrzyżowanie w poziomie ulic, zgodnie z zapisaną wcześniejszą decyzją właściciela.
- Nie odtwarzać tego wiaduktu bez nowego, jednoznacznego wzorca. Ruch drogowy, nawierzchnie i kolizje wymagają kontroli na faktycznej scenie.

### Pomost wyspy i dojście do areny
- `unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspa.cs:211-351`: pomost 53 m w morze, głowica T 34 m długości i szerokości `4*K`, pale, drabinki, odbijacze, liny, balustrady, deptak z kolizjami.
- Kod ostrzega, jeśli profil deptaka nie łączy się płynnie z poziomem plaży / placu; brak aktualnego nagrania z telefonu potwierdzającego geometrię. Nie zmieniać tych wymiarów ani wizualnych materiałów na ślepo.
- Potencjalny problem do pomiaru w krótkim audycie Unity po zgodzie: pętla próbkująca profil co 1 m może nie kończyć dokładnie na `xa`, gdy długość jest niecałkowita. Nie stwierdzono jeszcze szczeliny w rzeczywistej scenie.

## Następny krok (bez automatycznego wykonania)
1. Krótkie testy statyczne R1: kontrakt błędu dostawy + pozostałe istniejące testy, bez Unity i APK.
2. Odbiór mostu/pomostu: właściwy fragment nagrania właściciela + pomiar końców, kolizji, podłoża i nachodzenia na scenie; żadnych domyślnych zmian wyglądu.
3. Odbiór dystrybucji: po osobnym poleceniu właściciela krótki test sceny i jeden ręczny build, sprawdzenie walidacji morza i faktycznej dostawy APK.
4. Przed kolejnymi rozszerzeniami świata: oddzielnie zaprojektować mały `Player` oraz zewnętrzne wersjonowane pakiety rejonu; samo dodanie osobnych scen nie zmniejszy instalatora.
