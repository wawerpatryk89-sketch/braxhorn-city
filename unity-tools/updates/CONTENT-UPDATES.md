# Małe aktualizacje stref — instrukcja wykonawców

Wdrożenie obejmuje istniejące sceny osiedla, osobne paczki zależności, zapis trwałej bazy i prywatny import pliku. Wygląd, pozycje obiektów i progi doczytywania obszarów pozostają takie jak w źródłach. Jedyna nowa funkcja menu to **USTAWIENIA → WCZYTAJ AKTUALIZACJĘ** w bazowym APK obsługującym paczki.

## Zasada wspólna dla miasta, osiedla i obozu

1. **Kod C#, pakiety, typy komponentów i ustawienia silnika:** nowy bazowy APK. Paczka zasobów nie zmienia kodu IL2CPP.
2. **Zapisane sceny, transformacje, modele, materiały i prefabrykaty istniejących typów:** aktualizacja content. Nie uruchamiać generatora całego świata ani `BuildPlayer`.
3. Każda strefa i każdy wariant graficzny ma własny `baseId`, runtime fingerprint, grupy Addressables i oryginalny `addressables_content_state.bin`. Bazy nie wolno nadpisywać. Wspólne zasoby w ramach strefy są osobnymi paczkami, nie kopiami w każdej scenie.
4. Zachować `.meta` scen, modeli, materiałów i skryptów. Pracować na snapshotcie wydanej bazy, nie na ponownie wygenerowanych scenach o nowych GUID-ach.
5. Nie dodawać kodu nowego zachowania do paczki content. Kontrola zgodności zatrzyma taką aktualizację i zażąda nowego APK.

## Pierwszy bazowy APK

Osiedle: `unity-city/content-enabled.txt` włącza nowy tryb w workflow `claude-region-build.yml`. Istniejący generator jest używany **raz do utworzenia bazy**. Po jego zakończeniu:

- `BraxContentBuild.Baseline` tworzy grupę dla każdej istniejącej sceny oraz osobne zależności;
- pierwsze paczki są w APK, więc pierwszy start i późniejsza gra nie wymagają sieci;
- APK zawiera lekką scenę startową; kolejne sceny ładuje Addressables;
- workflow zapisuje zaszyfrowany snapshot źródeł i stanu content w prywatnym draft release `content-base-<baseId>`;
- istniejący stały podpis APK zostaje zachowany. To nadal testowa dystrybucja, nie publikacja produkcyjna.

Nie uruchamiać pełnego buildu dla każdej poprawki pliku content. Pierwszy bazowy APK wymaga kompilacji Unity/IL2CPP. Nie ma potwierdzonej obietnicy, że zakończy się w kilka minut.

## Kolejna aktualizacja bez APK

1. Odczytać `baseId` z prywatnego wydania bazy. Utworzyć `unity-city/content-update-request.json`:

```json
{"baseId":"osiedle-<32 znaki identyfikatora z wydanej bazy>"}
```

2. Zapisać **kumulatywną listę zmian względem tej bazy** w `unity-city/content-patch.json`. Poniższy przykład pokazuje format, nie polecenie zmiany wyglądu gry:

```json
{
  "edits": [{
    "scene": "Assets/PlayableGenerated/BraxhornEstate.unity",
    "path": "DOKLADNA_NAZWA_KORZENIA/DOKLADNA_NAZWA_OBIEKTU",
    "operation": "transform",
    "position": {"x": 0, "y": 0, "z": 0},
    "rotation": {"x": 0, "y": 0, "z": 0},
    "scale": {"x": 1, "y": 1, "z": 1}
  }]
}
```

`transform` ustawia pozycję, obrót i skalę lokalną. `add-prefab` wymaga pola `prefab` z istniejącą ścieżką `Assets/...prefab`, a `path` wskazuje rodzica. `delete` usuwa dokładnie wskazany obiekt. Nieistniejąca lub niejednoznaczna ścieżka przerywa build. Nowe pliki modeli/prefabów dodać razem z ich `.meta`. Aktualizacja nie dodaje nowej sceny strefy do istniejącej bazy; zmianę granic i listy scen należy osobno przygotować i przetestować.

3. Commit oznaczyć **`[content-update]`**. Workflow pełnego APK pominie ten commit. Workflow `zone-content-update.yml` przywróci niezmienną bazę, sprawdzi kod i GUID-y, zastosuje poprawki do zapisanych scen, wykona `BuildContentUpdate` i prześle prywatny plik `.braxupdate.zip`. Alternatywnie uruchomić ten workflow ręcznie, podając `base_id`.
4. Pobierz plik z draft release po zalogowaniu do GitHuba. W aplikacji wybierz **USTAWIENIA → WCZYTAJ AKTUALIZACJĘ**, wskaż ZIP i poczekaj na potwierdzenie. Potem zamknij i uruchom grę ponownie.

Paczka jest kumulatywna względem bazowego APK: można przejść z bazy lub dowolnej wcześniejszej poprawki do bieżącej wersji. Nie zawiera niezmienionych bazowych paczek. Przy wielu aktualizacjach może zawierać wcześniejsze zmienione paczki; nie jest to binarny diff pojedynczych bajtów. Zmiana obiektu może wymagać przesłania całej jego paczki/sceny. Rozmiar zależy od faktycznych zależności i powinien być podany przy każdym wydaniu.

## Integralność, wznowienie i wycofanie

- Transport bazy jest szyfrowany i podzielony na części z SHA-256. Kompletny indeks jest zapisywany dopiero po wysłaniu wszystkich części. Przerwany upload nie zastępuje działającej kopii.
- Import aktualizacji sprawdza strefę, bazę, kod, rozmiary i SHA-256. Odrzuca duplikaty, niebezpieczne ścieżki, niekompletne oraz uszkodzone paczki. SHA-256 wykrywa uszkodzenie; nie jest podpisem wydawcy. Importować tylko pliki przekazane przez właściciela projektu.
- Pliki trafiają do nowego katalogu, bez nadpisania aktywnej wersji. Katalog content przełącza się przy następnym starcie. Nie podmieniamy paczek podczas chodzenia po mapie.
- Dziennik próby jest zapisywany przed przełączeniem. Jeżeli następny start nie dochodzi do potwierdzenia po wczytaniu scen, kolejny start wraca do poprzedniej wersji. Uszkodzone dane mogą spowodować powrót do bazy w APK. Zachowywane są wersja bieżąca i poprzednia.
- Import pliku jest celowo prywatny. Nie wyłączono SSO Netlify. Nie ma tokenu GitHuba w APK ani automatycznego publicznego CDN.

## Jak przenieść do miasta i obozu

Przenieść `Assets/Updates`, `Assets/Editor/Updates`, zależności manifestu oraz `unity-tools/updates`. W swoim builderze wywołać `BraxContentBuild.Baseline("miasto" lub "oboz", orderedScenePaths)` po zapisaniu i sprawdzeniu scen. Pierwsza ścieżka to główna scena strefy. W APK zostawić bootstrap i wpiąć `BraxContent.Initialize`, `Begin`/`Load`, `Unload` i `Available` we własny loader. Nazwa strefy/identyfikator bazy musi być zgodna z konfiguracją workflow. Wariant OpenGL miasta może używać `miastogl`; należy też jawnie przypisać zgodny profil cache.

Nie nadpisywać całego workflow innego wykonawcy. Dodać do własnego pełnego workflow pomijanie commitów `[content-update]`, zapis prywatnej bazy i własną obsługiwaną gałąź w workflow content. Samo skopiowanie instrukcji nie wdraża mechanizmu w obozie ani mieście.

## Dowód wymagany przed uznaniem wdrożenia za gotowe na telefonie

- Testy Python transportu i izolacji snapshotu.
- Kompilacja wszystkich C# oraz budowa bazowych i zmienionych paczek Androida.
- Test uruchomionego playera: baza, import, restart, właściwa zmiana w scenie, doczytanie/zwolnienie obszaru i wycofanie przerwanej próby. Test Linux w CI sprawdza loader; nie zastępuje Androida.
- Bazowy APK osiedla i test na docelowym telefonie: instalacja jako aktualizacja, zachowany wygląd oraz jedna rzeczywista poprawka bez wymiany APK. Podać rozmiar paczki i zmierzone czasy. Do tego momentu nie deklarować całej ścieżki jako potwierdzonej na urządzeniu.

Oficjalna dokumentacja: [Unity Addressables — aktualizacje zawartości](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/content-update-builds-overview.html), [ładowanie scen](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/LoadingScenes.html).
