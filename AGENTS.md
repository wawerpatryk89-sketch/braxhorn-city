# BRAXHORN CITY — JEDYNA GLOBALNA INSTRUKCJA (7.11.2026)
**NAJPIERW PRZECZYTAJ TEN PLIK.** Obowiązuje we wszystkich czterech gałęziach rejonów. Nowa część 0 doprecyzowuje reguły z 6.11.2026; wcześniejsze, sprzeczne kopie zasad są nieaktualne. Wpisuj aktualizację instrukcji jako osobny commit zawierający wyłącznie `AGENTS.md` na każdej gałęzi; nigdy na `main`.

## 0A. GLOBALNY STANDARD TEKSTUR I DUŻEJ GRY (UNITY 6 / ANDROID, 7.11.2026)
**To jest instrukcja techniczna dla KAŻDEGO wykonawcy, na każdej z 4 gałęzi, a NIE informacja, że wdrożenie już wykonano. Kolejność: FAKTY -> DIAGNOZA -> DECYZJA -> DZIAŁANIE -> DOWÓD. Zatwierdzone zdjęcia, wymiary, materiały i wygląd właściciela mają pierwszeństwo przed pomysłem na optymalizację.**

### 0A.1. Nienaruszalne źródła i rzeczywista jakość 2K
1. Oryginalne modele, tekstury, UV, szkielety, proporcje, mapy materiałów i zatwierdzone grafiki właściciela traktuj jako źródła referencyjne. Nie nadpisuj ich, nie regeneruj obrazu, nie zmieniaj wyglądu ani barw, nie rozciągaj map, nie zastępuj brakującego modelu atrapą. Techniczny wariant importu może powstać wyłącznie jako osobna kopia robocza z dowodem porównania.
2. **Z BLISKA DO 2048 px (2K) — wszystkie tekstury otoczenia, budynków, elewacji, drzwi, okien, dróg, kostki, trawy, lasu, pojazdów, sklepów, wyposażenia i wnętrz — poza postaciami i wyraźnie zaakceptowanymi wyjątkami HD.** To maksymalny importowany rozmiar dla typowej mapy; nie obietnica, że każda mapa ma dokładnie 2048x2048, ani że GPU zawsze trzyma 2K. Dobierz proporcje do oryginału. Mipmapy w oddali dobiera kamera.
3. **Nie przycinaj na stałe 2K do 1K na Androidzie:** sprawdź `TextureImporter.maxTextureSize`, `TextureImporterPlatformSettings.maxTextureSize`, format i wszystkie inne skrypty importu (także Python pre-processing oraz tekstury wyjęte z GLB). `QualitySettings.globalTextureMipmapLimit` nie może wymuszać mip 1 globalnie, skoro odbiór wymaga ostrego 2K blisko.
4. Gdy źródło ma 1K / 512 px, nie zmieniaj etykiety na „2K”, nie stosuj upscale i nie obiecuj odzyskanego detalu. W raporcie podaj `ścieżka -> oryginalna rozdzielczość -> import Android -> wymagany oryginał 2K`. Szukaj oryginalnego pliku w zatwierdzonej bibliotece i istniejących zasobach; brak = lista do właściciela, nie losowe źródło.
5. **Postacie mają OSOBNĄ regułę:** tylko dla głównych grywalnych postaci, po potwierdzeniu UV i osobnych materiałów, twarz/głowa do 4K, ciało/strój do 2K, buty/akcesoria do 1K, zgodnie z zatwierdzonym projektem. Nie zmieniaj NPC według profilu grywalnej postaci. Nie przetwarzaj ponownie zatwierdzonego wyglądu postaci bez zgody.
6. **Murale, duże szyldy i grafiki właściciela:** zachowaj dokładnie obrazy i czytelność; zatwierdzony mural może wymagać 4096/8192 px, jeśli taka jest oryginalna rozdzielczość i zostało to zaakceptowane. Nie przepakowuj murali do małego atlasu, nie rozmywaj napisów. Rozmiar i format sprawdzaj osobno, nie stosuj uniwersalnego 2K bez oceny.

### 0A.2. Poprawny pipeline importu PBR (bez utraty wyglądu)
7. Dla każdej mapy zapisuj: źródło, rozdzielczość, SHA-256, typ, kolorystykę, przeznaczenie, docelowy format GPU, `.meta`/GUID i informację, czy potrzebna jest kopia do odczytu. Pliki importera i materiały muszą zachowywać trwałe referencje przy kolejnym Unity buildzie.
8. BaseColor/Albedo dla standardowych tekstur koloru w sRGB; normal map jako prawidłowy typ/import zgodny z shaderem; metalness, roughness, AO/ORM/ARM oraz inne mapy danych w **linear (sRGB=false)**. Uwaga: przy specjalnym shaderze lub glTFast nie zmieniaj formatu kanałów bez testu shaderów i wizualizacji. Nie zamieniaj roughness w glossiness bez jawnej konwersji.
9. Zachowuj oryginalną orientację normalnych (OpenGL/DirectX zależnie od źródła), kolejność kanałów PBR, UV, tiling i offset. Osobno traktuj szkło, przejrzystość, maski alfa, emulsje/odbicia, HDRI i skybox. Nie „naprawiaj” PBR jednym jednolitym kolorem lub zastępczą bryłą.
10. Nie uruchamiaj automatycznej zmiany rozdzielczości, uproszczenia geometrii, nowych tekstur AI ani usunięcia szczegółów na podstawie samego rozmiaru APK. Każdą potencjalnie widoczną zmianę porównaj ze wzorcem właściciela; brak wzorca = brak zgody na zmianę wyglądu.
11. Android: wybierz ASTC (np. 4x4 dla grafiki wymagającej ostrości, 6x6 dla wybranych powierzchni) albo ETC2 zgodnie z **rzeczywistą obsługą urządzenia i udokumentowanymi testami**. Gdy format nie jest wspierany, rozpakowanie na telefonie zwiększa RAM; nie przełączaj globalnie formatu, Vulkan/OpenGLES czy wersji Unity „w ciemno”. Parametry jakości kompresora testuj: rozmiar, czas importu, artefakty obrazu i pamięć.
12. `Read/Write Enabled` wyłącz dla tekstur, do których C# nie musi mieć dostępu. Włączenie przechowuje dodatkową kopię w pamięci. Kompresja tekstury na GPU (ASTC/ETC2), kompresja plików do pobrania (LZ4/LZMA) i rozmiar surowych GLB to różne wielkości; nie myl ich w raportach.

### 0A.3. Mipmapping i streaming zależny od kamery
13. Dla tekstur z użytecznymi mipami: `mipmapEnabled=true` i — jeśli poprawny Renderer/UV/shader wspiera wyznaczanie detalu — `streamingMipmaps=true`. W czasie gry włącz `QualitySettings.streamingMipmapsActive`; konfiguracja kamer, budżetu i priorytetów musi być sprawdzona. Bliski detal może być 2K, dalekie poziomy są mniejsze, ale niski budżet pamięci może obniżyć mipy także blisko.
14. Nie deklaruj, że samo `streamingMipmapsActive=true` naprawia import 1K ani że samo `MipMap Streaming` zmniejsza APK: ono ogranicza załadowane poziomy GPU, nie pobiera modeli/tekstur z internetu. Pobieranie osobnych plików wymaga AssetBundles / Addressables / własnego mechanizmu dystrybucji.
15. **Wyjątki techniczne:** Unity nie obsługuje streamingu mipmap dla Terrain texture layers, 3D textures, texture arrays i cubemap arrays. UI / ekran startowy / bezpośredni draw / billboard, niestandardowy shader, atlas, proceduralny mesh i blendowane podłoże wymagają osobnej kontroli. Nie włączaj streamingu na ślepo na każdej teksturze.
16. Mipmap streaming wylicza wymaganą jakość na podstawie MeshFilter/SkinnedMeshRenderer, UV i kamer. Proceduralny mesh potrzebuje poprawnych UV-distribution metrics (np. `Mesh.RecalculateUVDistributionMetrics`); specjalny shader musi poprawnie przekazywać tiling/offset (np. `_MainTex_ST`). W przeciwnym razie mogą pojawiać się rozmycia blisko i „rysowanie się” materiału przy podejściu.
17. `streamingMipmapsMemoryBudget`, `streamingMipmapsMaxLevelReduction`, priorytety i mipmap bias ustawiaj na podstawie pomiaru `Texture.desiredTextureMemory` i odczytu Profilera na docelowym telefonie. Budżet obejmuje również tekstury niestrumieniowane i **nie gwarantuje twardego limitu GPU RAM**. Nie ustawiaj liczb jako „sprawdzonych”, jeśli nie zmierzono ich na urządzeniu. Krytyczne bliskie elementy mają pierwszeństwo, ale tylko po ocenie pamięci.
18. Pamiętaj o jakości filtrowania i perspektywie (trilinear/anizotropia przy potrzebie). Cienie, LOD geometrii, culling i GPU instancing są oddzielnymi optymalizacjami i nie zastępują prawidłowego 2K. Zakaz „pop-in” i migotania: test przejazdu od sklepu do portu, bram, boisk i wyspy, w obu kierunkach.

### 0A.4. Wzorzec dużej gry: mały Player, pakiety i bez duplikatów
19. **Duża gra nie oznacza ogromnego jednorazowego APK:** osobno lekki bootstrap + oddzielne, wersjonowane zasoby świata, podobnie do opisanego przez Activision systemu pobierania zawartości COD Mobile. Obszary w BRAXHORN: Miasto R1, Osiedle, Obóz, Miasto Mistrzów, wnętrza, postacie, roślinność, opcjonalne HD. Podział jest wymaganiem docelowym, NIE deklaracją, że wszystkie gałęzie go już posiadają.
20. Sam podział monolitu na `level1`, `level2` itp. **wewnątrz tego samego APK** może poprawić ładowanie pamięci, ale nie zmniejsza znacząco instalatora. W celu zmniejszenia APK rzeczywiście przenieś pakietowane zasoby poza Player do Addressables/AssetBundles (lub przyszłego PAD), nie duplikuj ich jednocześnie w Player i w content-only.
21. Dla współdzielonych materiałów, tekstur, animacji i modeli stosuj osobne paczki zależności. Najpierw sprawdź `BuildReport.packedAssets`, Addressables Analyze/Check Duplicate Bundle Dependencies i raport rozmiaru. Nie uruchamiaj kosztownego Analyze czy przebudowy z samego push — wymagają osobnej zgody.
22. Wydziel sceny addytywne zgodnie z rzeczywistym układem i stabilnymi referencjami. Przed wejściem do strefy pobierz/wczytaj potrzebny pakiet, utrzymuj poprzedni obszar przy nieudanym ładowaniu, nie pozostawiaj gracza bez podłoża. Poprawnie zwalniaj sceny i powiązane zasoby według zależności/reference-count; nie odłączaj pakietu, z którego nadal korzysta aktywna scena.
23. Instalator spoza Google Play potrzebuje zatwierdzonego hostingu zawartości, manifestu i bezpiecznego pobierania; repozytorium lub GitHub DRAFT Release nie jest automatycznie publicznym CDN. Google Play Asset Delivery (install-time/fast-follow/on-demand) jest alternatywą dla publikacji przez Google Play, a NIE funkcją automatycznie dostępną w APK z GitHuba.
24. Content-update/delta działa dopiero od **zweryfikowanej, stabilnej bazy**: zapisz katalog, `addressables_content_state.bin`, GUID-y, kompatybilność Unity i hash plików. Download -> staging -> kontrola SHA-256/wersji -> aktywacja -> rollback po awarii; bez kasowania ostatniej działającej wersji. Zmiany C#/IL2CPP/Player wymagają nowego APK; same dane mogą otrzymać aktualizację bez APK, jeśli cała infrastruktura faktycznie działa.
25. Wybór kompresji AssetBundles (LZ4/LZMA) musi wynikać z testu pobierania/odczytu i pamięci. LZ4 nadaje się do szybkiego odczytu fragmentów, LZMA zmniejsza pliki, ale wymaga pełnego rozpakowania. Nie zmieniaj `BuildPlayerOptions` na chybił trafił i nie przypisuj automatycznie korzyści z LZ4 do wielkości APK.

### 0A.5. Wymagane kontrole, dowody i bezpieczne działania
26. Każdy wykonawca przed zmianą przygotowuje **inwentarz**: źródło i licencja, rzeczywiste wymiary map, profil Android, czy jest embedded w GLB, format GPU, mipmaps/streaming, materiał/Renderer, GUID, obecne duplikaty i brakujące źródła 2K. Oryginalny asset pozostaje nietknięty.
27. Przed kolejnym APK podaj pomiary: `APK bytes`, wielkość każdej sceny w `BuildReport`, największe tekstury/meshe, osobno kompresja na dysku i rezydencja RAM/VRAM, brakujące 2K, zależności i powtórzenia, czas ładowania oraz potencjalna regresja obrazu. Porównaj wyniki do ostatniego **sprawdzonego** wydania, a nie do deklaracji w komentarzu.
28. Test źródłowy ≠ test Unity ≠ APK ≠ instalacja na Androidzie. Sprawdzaj mipmapy, źródła 2K, różnice wizualne, 30 FPS (cel, nie gwarancja), obciążenie GPU/CPU, pamięć na Motorola/urządzeniu właściciela, a po crashu rzeczywisty logcat/bugreport; nie zgaduj winnego modelu bez dowodu.
29. Rozmiar: obowiązuje limit wskazany przez właściciela (w starszej instrukcji Obóz <= 400 MB, pozostałe paczki bazowe cel <= 500 MB). Przekroczenie = raport i brak deklaracji „gotowe”, NIE próba cięcia jakości lub skasowania assetów bez zgody. W przyszłości bazowy instalator ma zawierać tylko minimum potrzebne do startu.
30. **Brak działań bez zgody:** nie edytuj zatwierdzonych grafik, modeli, wersji Unity, prefabów innych rejonów, nie usuwaj źródeł; nie uruchamiaj drogich scen/CI/BuildPlayer, nie publikuj/nie restartuj APK bez wyraźnego polecenia. Szybkie statyczne testy można wykonać i uczciwie opisać. Każdy region pozostaje osobną gałęzią, żadne commity na main.

ŹRÓDŁA OFICJALNE (referencja implementacyjna, nie dowód wdrożenia):
- Unity 6 mip streaming / ograniczenia: https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/texture-loading/texture-streaming/configure
- Unity 6 import/compression/Android: https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/textures-reference/class-texture-importer/type-specific
- Unity 6 Android GPU format support: https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/introducing/requirements-and-compatibility
- Unity 6 duplikacja w Player i AssetBundles: https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/assets-managing-runtime/assets-avoid-duplication
- Unity 6 raport pakowania: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/build/reporting/packedassets
- Unity Addressables Analyze: https://docs.unity.cn/Packages/com.unity.addressables@1.22/manual/AnalyzeTool.html
- Unity Addressables content updates: https://docs.unity.com/en-us/build-automation/advanced-build-configuration/build-addressables-using-build-automation
- Android PAD: https://developer.android.com/guide/playcore/asset-delivery
- Activision COD Mobile, osobne pakiety: https://activision.helpshift.com/hc/en/3-cod-mobile/faq/518-details-about-the-latest-updates-for-call-of-duty-mobile-1765413684/

0. GLOBALNY STANDARD DUŻEJ GRY — UNITY 6 / ANDROID (najwyższy priorytet techniczny)
- Wzorzec: mały uruchamialny Player + osobne, wersjonowane pakiety treści, nie pełna przebudowa 1 GB+ APK przy każdej poprawce. To zasada dla wszystkich rejonów, a NIE twierdzenie, że obecny projekt już tak działa.
- Samo rozdzielenie scen *wewnątrz tego samego APK* może poprawić obciążenie pamięci podczas ładowania, ale NIE zmniejsza automatycznie pobieranej paczki. Mniejszy instalator wymaga faktycznego wyprowadzenia zasobów do osobnych AssetBundles/Addressables lub pakietów dystrybucyjnych.
- Projektuj bootstrap: lekkie menu, sterowanie, profil grafiki, sprawdzanie plików, komunikaty o postępie i błędach. Świat ładowany później na żądanie, z możliwością wejścia do gry, wyjścia i ponownego uruchomienia. Nie wolno pokazać `SUCCESS` zanim świat działa na urządzeniu.
- Dostarczanie dla APK spoza Google Play: osobne wersjonowane pakiety Addressables/AssetBundles, źródło pobierania zatwierdzone przez właściciela, offline fallback/instalacja lokalna, bez wbudowanych sekretów GitHuba. GitHub Actions artifact i prywatny DRAFT Release nie są automatycznie publicznym CDN-em.
- Dla przyszłej publikacji w Google Play osobno rozważ Android App Bundle + Play Asset Delivery (`install-time`, `fast-follow`, `on-demand`). PAD nie działa samoczynnie w samodzielnym APK z GitHuba.
- Rozdziel pakiety: wspólna biblioteka postaci/animacji/materialów, Miasto R1, Osiedle, Obóz, Miasto Mistrzów, opcjonalne wnętrza, roślinność oraz HD. Podział logiczny nie upoważnia do przenoszenia assetów między gałęziami lub do zmiany układu świata bez zgody.
- Wykorzystuj Addressables Analyze i Build Layout / BuildReport do wykrywania zduplikowanych siatek/tekstur/zależności: wspólne zasoby umieszczaj w wydzielonej grupie, nie powielaj w Player i w pakietach. Każdy pakiet ma stabilne ID, katalog, SHA-256 i raport rzeczywiście pobranych bajtów.
- Aktualizacja różnicowa WYŁĄCZNIE po zatwierdzeniu działającej bazy: przechowuj katalog bazowy, stabilne `.meta`/GUID i odpowiedni `addressables_content_state.bin`; nową zawartość publikuj bez ponownego instalowania kodu, jeśli to kompatybilne. Zmiana kodu C#/IL2CPP, konfiguracji Playera lub binarium wymaga nowego APK.
- Bezpieczny klient aktualizacji: pobierz do staging, sprawdź rozmiar/hash/zgodność, dopiero potem atomowo aktywuj. Po błędzie zachowaj ostatni działający pakiet; żadnych automatycznych kasowań cudzych assetów ani cache.
- Duży świat: addytywne/asynchroniczne ładowanie stref i wnętrz; bezpieczne zwalnianie referencji Addressables przy wyjściu; PBR, oryginalne geometrie/tekstury, zasięg cieni i LOD zależne od odległości. Nie usuwaj/nie odchudzaj assetów lub jakości wizualnej bez porównania ze wzorcem i zgody właściciela.
- **ZASADA WŁAŚCICIELA 7.11 — 2K Z BLISKA, POTEM MIPMAPPING:** wszystkie tekstury otoczenia, elewacji, przedmiotów, pojazdów i wyposażenia mają udostępniać z bliska oryginalny detal do 2048 px (2K); w oddali GPU/Texture Mipmap Streaming wybiera niższe poziomy zależnie od kamery. NIE wolno na stałe ścinać importerem Androida 2K do 1K ani wymuszać `globalTextureMipmapLimit = 1` w każdym trybie mobilnym. Postacie mają odrębny zatwierdzony standard tekstur; istniejące zatwierdzone murale HD mogą zachować większe źródłowe wymiary. Nie podnoś sztucznie źródła 1K do 2K: wskaż brak źródła 2K w raporcie. Włączaj streaming tylko na obsługiwanych mapach powierzchni przypisanych do Rendererów; dla UI, refleksów i złożonych shaderów sprawdź zgodność z dokumentacją Unity. Budżet pamięci nie gwarantuje poziomu 2K, konieczny test na Androidzie.
- Mipmap streaming ogranicza RAM/VRAM, ale sam NIE pobiera zasobów z serwera. Ustawienia pamięci to budżet, nie gwarancja; tekstury `non-streaming` mogą go przekroczyć. Mierz faktyczne maksymalne zużycie pamięci i ryzyko natywnego crasha Androida.
- Kompresję AssetBundles (LZ4/LZMA) wybieraj na podstawie pomiaru rozmiaru, czasu wczytania i pamięci telefonu, a nie na oko. LZMA często daje mniejszy transfer, ale rozpakowanie może kosztować więcej RAM; LZ4 pozwala odczytywać fragmenty.
- Cele: Obóz APK <= 400 MB wg wcześniejszej decyzji, pozostałe paczki bazowe dążyć do <= 500 MB. Nie nazywaj przekroczenia sukcesem, nie obiecuj wielkości bez rzeczywistego BuildReport. Nie blokuj kompilacji ostrzeżeniem o rozmiarze; zamiast tego raport do właściciela i brak deklaracji gotowości wydania.
- Każdy build: tabela rozmiarów APK, scen (skompresowany/nieskompresowany), tekstur, siatek, animacji, największych źródeł, raport zależności i duplikatów, różnica względem ostatniej wersji, wskaźniki RAM/VRAM/FPS z urządzenia, log awarii. `BuildPipeline.BuildPlayer = Succeeded` oznacza kompilację, NIE sprawny telefon.
- Jeśli w Androidzie `Loading.Preload`, `SIGTRAP`, `Position out of bounds`, `level1 corrupted`: raportuj dowód, analizuj MonoScript/GUID/serializację i dane sceny; nie zgaduj „brak RAM” ani „winny model X” bez symboli/logcat. Brak obowiązkowego MonoScript to błąd integralności, ostrzeżenie wyglądu pozostaje tylko raportem.
- TESTY: szybkie statyczne testy kodu mogą działać bez kosztownego Unity; pełne Unity/IL2CPP/generowanie scen/APK tylko po odrębnej zgodzie właściciela. Nie wywołuj `workflow_dispatch` samoczynnie z `push`; brak restartów i równoległych długich testów.
- Dokumentacja urzędowa i techniczna: Unity `https://docs.unity3d.com/6000.0/Documentation/Manual/assets-avoid-duplication.html` (duplikaty), `https://docs.unity3d.com/6000.0/Documentation/Manual/assetbundles-compression-format.html` (LZ4/LZMA), `https://developer.android.com/guide/playcore/asset-delivery` (PAD). Zasady powyżej są wymaganiami projektu, a nie twierdzeniem, że wykonano wdrożenie.

1. GAŁĘZIE I SILNIK
- Repo wawerpatryk89-sketch/braxhorn-city. Unity 6, Android, mobile-first.
- Miasto R1: claude/miasto-opus-20261031
- Osiedle: claude/osiedle-42-poprawka
- Obóz: codex/oboz-przebudowa-pbr-20261105
- Miasto Mistrzów: gpt/miasto-mistrzow-kasyno-t11-20261105
- Wersja Unity = ta przypięta w workflow danego rejonu (Mistrzowie 6000.0.85f2). Nie zmieniaj jej.
- Nigdy main. Nigdy dwa rejony w jednym commicie.
- JEDNA GAŁĄŹ = JEDEN WYKONAWCA NARAZ. Przed pracą: git fetch, sprawdź ostatnie commity. Przed pushem: fetch + rebase na najnowsze. Jeśli ktoś pushował w ostatnich 30 min, pytasz właściciela.

2. JAK PRACOWAĆ (każde zadanie)
1) Przeczytaj zadanie i kod, którego dotyczy. 2) Ustal przyczynę w kodzie (plik:linia). 3) Napraw WSZYSTKIE podobne miejsca, nie jedno. 4) Uruchom testy statyczne rejonu (python -m unittest w unity-tools/<rejon>, z jego folderu). 5) Commit + push. 6) Raport.
- Bez nowych funkcji, gdy ostatni build padł.
- Nie zmieniasz zatwierdzonego wyglądu. Nie dodajesz rzeczy, o których właściciel nie mówił.
- „Zatwierdzone przez właściciela” piszesz tylko z cytatem jego wiadomości. Wzór/zdjęcie od właściciela jest ważniejsze niż komentarz w kodzie.

3. JAK BUDOWAĆ SCENY (kod generatorów)
- Skala K = 1,294. Każdy model = realny wymiar × K (łóżko 2,0 m, krzesło 0,9 m, drzwi 2,1 m, człowiek 1,8 m). NIGDY nie wciskaj modelu w wymyślone pudełko (min(target/bounds)).
- Stawianie: raycast w dół → dolna krawędź bounds na podłodze/gruncie. Rzeczy ścienne (lustro, pisuar, półka, szyld, mural) raycast do ściany, tył na ścianie. Nic w powietrzu, w ścianie ani na sobie nawzajem; różne produkty = różne miejsca.
- Szyld zawsze na belce/ścianie/pasie nad witryną, nie na patykach i nie przed szybą.
- Materiały: tylko PBR z tekstur już pobieranych w danym rejonie (kolor + normal + ARM). Każda powierzchnia swój materiał (posadzka ≠ ściana ≠ sufit). Lustro = reflection probe.
- Tekstury: pełen standard szczegółowy w §0A. Blisko 2K dla otoczenia/wyposażenia (z wyjątkiem postaci i zatwierdzonych murali HD), niższe mipy w oddali, ASTC/ETC2 tylko wg obsługi urządzenia. Nie ścinaj istniejącej jakości i nie powiększaj źródeł 1K; postacie i murale wg zatwierdzonego wyjątku.
- Brakuje modelu → lista do właściciela. Nie robisz zastępstwa z kostek.
- Telefon: LOD, instancje GPU, światła vertex/wypalone, bez cieni z lamp. Rozmiar APK w raporcie; obóz maks. 400 MB. Limit przekroczony = nie deklaruj wydania gotowego, pokaż źródło wzrostu; nie zatrzymuj kompilacji kontrolą kosmetyczną.

4. JAK PISAĆ KOD, ŻEBY BUILD NIE PADAŁ
- TWARDY BŁĄD (throw) tylko gdy bez tego nie ma gry: błąd kompilacji, brak sceny głównej/postaci gracza, zapis sceny, podpis APK, brak pliku bez którego nic nie powstanie; nieprawidłowy lub brakujący MonoScript/GUID w wymaganej scenie to błąd integralności, nie kosmetyka.
- Wszystko inne = BraxBuildQuality.Warn(...) i build idzie dalej: rozmiary tekstur, mipmapy, liczba sztuk, skala, dokładne liczby (32 leżaki, 35 łodzi, 95 miejsc), wygląd.
- Każdą dekorację i każdy osobny budynek wywołuj przez BraxChampionsFoundation.Optional("nazwa", () => ...) (lub odpowiednik w rejonie): wyjątek → ostrzeżenie z pełnym błędem, reszta świata buduje się dalej. Obiekt pół-ustawiony przy błędzie usuń (DestroyImmediate).
- NIGDY throw wewnątrz AssetPostprocessor (OnPreprocess*). Tylko Debug.LogWarning + return.
- Ścieżki: Unity startuje w unity-city/. Plik spoza Assets (np. unity-tools/...) szukaj w obu miejscach: "x" i "../x". Najlepiej użyj gotowego RepoFile().
- Obrazki: przed wczytaniem wymuś TextureImporter: textureType Default, textureShape Texture2D, potem ImportAsset(ForceSynchronousImport), dopiero potem LoadAssetAtPath<Texture2D>. Nigdy nie sprawdzaj dokładnych pikseli po imporcie (Android je zmienia) — tylko GetSourceTextureWidthAndHeight i to jako ostrzeżenie.
- Każdy plik pobierany w CI ma stałe .meta w repo (stały GUID, textureShape 2D dla obrazków).
- Testy statyczne pilnują ZACHOWANIA, nie dokładnego tekstu kodu. Zmieniasz kod → dopasuj test w tym samym commicie.

5. JAK PUSZCZAĆ BUILDY
- Tylko na wyraźne słowo właściciela w tej rozmowie. Jeden build naraz, workflow_dispatch, z dokładnym SHA. Commit/push NIGDY nie odpala buildu.
- **ZASADA GLOBALNA 7.11.2026 — NIE POWTARZAJ DŁUGIEGO TESTU PRZED APK:** jeśli właściciel mówi „puść build/APK”, a scena dla aktualnego SHA jest już przygotowana/zwalidowana albo można bezpiecznie użyć zgodnej zapisanej sceny/cache, NIE uruchamiaj ponownie pełnego scene-only, długiego audytu, ponownego importu całego świata ani wielogodzinnej walidacji tylko jako rytuału przed buildem. Zrób krótki preflight integralności potrzebny do kompilacji (kod/statyczne testy, wymagane pliki i MonoScript/GUID, podpis, tożsamość wersji), po czym od razu build.
- Pełny/długi test sceny uruchamiaj tylko gdy: właściciel jawnie go zleci; nie ma aktualnej przygotowanej sceny zgodnej z SHA; zmiana dotyka serializacji/geometrii tak, że bez regeneracji nie da się zbudować poprawnego Playera; albo poprzednia awaria daje dowód na problem integralności sceny. Nie wolno dodawać długiego testu „na wszelki wypadek”.
- Ostrzeżenia jakości, kolizji, skali, LOD, wyglądu i rozmiaru mają trafić do raportu i nie blokują builda, o ile nie są błędem integralności/kompilacji. Jeśli build może bezpiecznie iść, priorytetem jest zbudować go i sprawdzić wynik na urządzeniu zamiast zużywać czas na ponowny długi audyt.
- Padło: pierwszy wyjątek + stack trace → przyczyna → naprawa wszystkich podobnych miejsc → testy → jeden nowy build. Bez restartów w ciemno, bez zatrzymywania biegnącego buildu bez zgody.
- Zmiana samych obrazków/tekstur (murale, szyldy) → tryb „aktualizacja”, nie nowy APK, jeśli rejon go ma i działa. Ten sam identyfikator, ten sam certyfikat, versionCode + 1.
- Bez zdjęć w CI. Dowód działania na Androidzie = nagranie właściciela z telefonu i/lub logcat z konkretnego SHA, nie zielony CI.

6. PORZĄDKI
- Usuwanie czegokolwiek (Releases, cache unity-ci-cache-*, stare APK, workflow, gałęzie) tylko po liście zaakceptowanej przez właściciela.
- Docelowo na rejon: 1 workflow APK (z trybami apk/sceny/aktualizacja), 1 test źródeł. Jednorazowe *-once-*, *-smoke-*, *-probe-* do listy usunięcia.

7. RAPORT (zawsze w tej kolejności)
Pierwsze słowa: OBÓZ TRENINGOWY / MIASTO R1 / OSIEDLE / MIASTO MISTRZÓW.
Potem: co zrobione (commit), co przetestowane (wynik testów), liczba ostrzeżeń, co nie działa (pierwszy błąd), co dalej. Bez „gotowe”, dopóki właściciel nie zobaczy tego na telefonie.

8. AKTUALNE PRIORYTETY I ZASADA DOWODÓW
- Najpierw przyczyny nieudanych buildów i awarii telefonu, później nowe funkcje. W R1 obowiązuje zlecony audyt mostu/wiaduktu i infrastruktury przejazdu. Zmiany układu i wizualizacji wymagają weryfikacji ze wzorcem od właściciela, nie zgaduj parametrów.
- Nie zakładaj, że jakakolwiek gałąź ma już mały bootstrap, działające zdalne Addressables lub delta-update. Odczytaj jej prawdziwy `BuildPlayerOptions.scenes`, katalog i `BuildReport`.
- Przed pracą sprawdź aktualny HEAD gałęzi i aktywność innego wykonawcy. Nie nakładaj commitów na czyjeś aktywne zmiany. Zasady globalne zapisuj osobnym commitem tylko `AGENTS.md`, roboty nad R1 osobnym commitem.
- Raportuj w kolejności: FAKTY (źródło, run, commit) -> DIAGNOZA (plik:linia) -> DECYZJA -> DZIAŁANIE (dokładny zakres) -> DOWÓD (test, log, telefon) -> BRAKI. Nie deklaruj wykonania ani testów, których faktycznie nie było.
