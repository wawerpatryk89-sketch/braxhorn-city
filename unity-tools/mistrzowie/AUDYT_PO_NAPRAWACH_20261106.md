# Miasto Mistrzów A1+T11 — raport po naprawach, 6 listopada 2026

Zakres: ONLY Miasto Mistrzów, Arena A1, Kasyno T11, powiązana stacja metra i Brax. Główne miasto, osiedle i port nie należą do tej kompilacji.

## Fakty: błędy rzeczywistych kompilacji Unity

- [APK A1+T11 — 42116287704](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42116287704): wyjątek w BraxChampionsVIPSeats.cs:57. Importowany model VIP GLB nie miał mipmap; kryterium wizualne błędnie kończyło generowanie scen.
- [Równoległy APK — 42116236295](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42116236295): identyczna przyczyna na starszym commicie.
- [APK #2 — 42129518172](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42129518172): wyjątek w BraxChampionsCasinoPbr2K.cs:28 dla konkretnej tekstury concrete_brick_wall_001_diff_2k.jpg. Log potwierdza wcześniej 21 pomyślnie pobranych plików Poly Haven i import tej tekstury. Stara funkcja łączyła sprawdzanie jej obecności z rozdzielczością po imporcie do Androida. Sam log nie rozstrzyga, który warunek był fałszywy.

## Diagnoza i poprawki wdrożone na aktualnej gałęzi

1. Kasyno T11: oryginalne 21 map 2048x2048 zachowane bez downscalingu. Źródłową rozdzielczość odczytuje teraz TextureImporter.GetSourceTextureWidthAndHeight. Jeśli Texture2D została zmniejszona przez Androida, jest ostrzeżenie jakości, a nie fałszywy FileNotFoundException. Wczesna kontrola źródeł działa przed kosztownym generowaniem świata.
2. Roślinność T11: zachowano oryginalne pliki Poly Haven poza AssetDatabase w folderze OriginalSources. GLB do Unity powstają z oryginalnych bajtów geometrycznych i map 2K, bez podmiany siatki, UV lub materiału. Błędne zewnętrzne odwołania do zdjęć w plikach glTF zastąpiono osadzonymi danymi.
3. Auto: do Unity dodano KTX i meshoptimizer, bez zamiany modelu car_rev.glb na zamiennik. Osobny preflight wymaga prawdziwej zaimportowanej siatki.
4. Brax: wczesne sprawdzenie jego oryginalnego modelu, sześciu klipów podstawowych i trzech animacji Mixamo do samochodu.
5. Szyldy KASYNO WAWER, TAXI, PARKING: weryfikacja niezmienionych oryginalnych obrazów 2048x512. Wygląd i proporcje niezmienione.
6. Arena VIP: mipmapy i rozmiar map raportowane jako ostrzeżenia jakości. Fotele, oryginalne modele, geometria i fizyczne kolidery pozostają.
7. Raport źródeł ograniczony do Assets/Source/Mistrzowie i używanego oryginalnego modelu metra. HarborBeach/harbor_crane.glb należy do głównego miasta i nie jest elementem Miasta Mistrzów.
8. Diagnostyczne kontrole przejść są pomijane zgodnie z decyzją właściciela. Kompilator C#, autentyczność modeli, tożsamość i podpis APK nadal są obowiązkowe; koliderów fizycznych nie usunięto.

## Dowody po naprawach, przed Unity

- [Preflight #10](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42139327068): SUCCESS — testy źródeł, kodu i kontraktów.
- [Preflight #11](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42139763352): SUCCESS — testy wczesnego importu oryginalnych źródeł i regresji.
- [Test rzeczywistych Poly Haven T11 #1](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42140367292): SUCCESS — pobrano i sprawdzono SHA-256 oraz rozdzielczość 21 natywnych map 2K, integralność 2 osadzonych GLB, geometrię i niezmienione materiały.
- Testy powyższe NIE wykonują Unity Editor, BuildPlayer ani instalacji Android.

## Otwarty audyt: bez fałszywej obietnicy zero błędów

Wymaga realnego uruchomienia Unity: import samochodu, obu roślin i animacji po aktualizacji; generowanie scen Foundation i Explorer, serializacja i ponowne otwarcie; dalsze warunki generatorów, kompilacja ARM64 IL2CPP, zgodność podpisu, instalacja i test na telefonie. Nie wszystkie kolejne błędy można rozpoznać statycznie przed wygenerowaniem sceny.

## Decyzja produkcyjna

Nie powtarzać starszych commitów z błędami VIP ani T11. Po pełnym statycznym audycie uruchomić JEDEN test generowania scen w Unity (bez APK); dopiero gdy przejdzie, uruchomić JEDEN podpisany APK. Obecny sukces testów źródłowych nie jest gotowym APK.

## Uzupełnienie po anulowanym teście i weryfikacji CI — 6 listopada 2026

- Długi test generowania scen [#42141022644](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42141022644) został **anulowany przez właściciela** 01:09:40 UTC podczas importu/kompilacji środowiska Unity. Log potwierdza przygotowanie 21 map 2K, lecz **nie** potwierdza wygenerowania ani zapisania scen. Nie oznaczać anulowania jako naprawy.
- Krótka kontrola [#42141825354](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42141825354) zakończyła się sukcesem: oba joby, bez Unity, kompilowania Androida i wysyłki APK (01:08:46–01:09:08 UTC). To **kontrola statyczna**, a nie test scen.
- Przebieg APK [#42129518172](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42129518172) potwierdza pobranie 21 map i zatrzymanie starego kodu walidacji `concrete_brick_wall_001_diff_2k.jpg`. Poprawka importera jest w źródłach, ale nie przeszła ponownie generatora Unity.
- W rzeczywistej ścieżce `mistrzowie-apk.yml` po pobraniu źródeł wykonują się `verify_t11_real_sources.py` i `verify_native_vegetation_glb.py` **zanim wystartuje Unity**. Sprawdzają integralność 21 obrazów oraz oryginalne bufory i osadzone mapy obu GLB. Nie zmieniają materiałów, UV, geometrii, GUID, podpisu, kolizji ani rozdzielczości oryginalnych tekstur.
- Nieaktualny `mistrzowie-scene-audit-once-20261106.yml`, który uruchamiał kosztowny test na `push`, został wycofany z gałęzi. Czasochłonne uruchomienia Unity/APK wymagają osobnej decyzji właściciela.
- Poprzedni APK #14 nie stanowi gotowej bazy Addressables dla aktualizacji scen. Pierwszy poprawny APK z nowym loaderem musi być wydany raz jako nowa baza dla przyszłych `.braxupdate.zip`; nie przedstawiać go jako istniejącej małej paczki delta.

**Wciąż NIEPOTWIERDZONE:** rzeczywisty import i kompilacja całego projektu w Unity 6000.0.85f2 po zmianach, modele w scenie, ponowny odczyt scen, IL2CPP ARM64, podpis i działanie na Androidzie. Nowego APK nie uruchomiono; baseny P1, promenada i plaża pozostają na osobnych gałęziach. Raport nie oznacza „zero błędów”.
