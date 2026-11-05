# Obóz — rzeczywisty audyt 146 tekstur i współdzielenia, 5.11.2026

**Status:** audyt raportu Unity z Obóz #13; przed zmianami źródeł. W tym dokumencie **nie** zatwierdzono usuwania map, zmiany wyglądu ani zmiany jakości. Nie jest to pomiar RAM telefonu, rozmiaru APK ani test funkcji na Androidzie.

## Źródło i metoda

- GitHub Actions #13: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41914794060
- Artefakt `camp-life-before-apk-41914794060`, raport `texture-audit.json`.
- Pola: `estimatedAndroidTextureBytesWithMips`, `sourceWidth/sourceHeight`, `androidFormat`, `asset`.
- Sumowane **146 różnych referencji Texture2D** (skrypt używa `Distinct`). Rozstawienie 1712 drzew nie jest 1712 osobnymi kompletami tekstur.

## Wynik

**1 521 271 184 B** = około **1 521 MB dziesiętnie** = **1450,8 MiB** z pełnymi mipmapami.

| Grupa | Mapy | Szacunek (MiB) |
| --- | ---: | ---: |
| Postacie | 28 | 549,3 |
| Materiały prywatne PBR (drewno, metal, żwir itd.) | 19 | 405,3 |
| Materiały publiczne PBR (asfalt, kostka, trawa) | 12 | 256,0 |
| Roślinność, w tym warianty liści i billboardów | 22 | 88,9 |
| Tekstury modeli środowiska (bunkier itd.) | 48 | 83,0 |
| Podłoże leśne | 3 | 64,0 |
| Materiały autobusu (generowane kopie) | 14 | 4,2 |
| **Razem** | **146** | **~1450,7** |

59 map 4096×4096 ASTC 4×4 to około **1258,7 MiB** (większość sumy).
26 map 2048×2048 ASTC 4×4 to około **138,7 MiB**.
33 map 1024×1024 ASTC 4×4 to około **44,0 MiB**.
Pozostałe mapy mniejsze lub atlasy ASTC 6×6 dopełniają resztę.
Wszystkie liczby z audytu są oszacowaniem całego łańcucha mipmap, nie koniecznie jednocześnie aktywnych mipów na urządzeniu.

## Współdzielenie i rzeczywiste duplikaty

- Drzewa: `BraxObozVegetation.Populate` używa **6 prototypów dla 1712 instancji**; materiały drzewa mają cache.
- Trawa: wspólna siatka i materiał + dane instancji.
- Zabudowa: `BraxObozStableMesh.Save` deduplikuje siatki statyczne po zawartości.
- Na podstawie `unity-tools/oboz/private-inputs.json` 204 pliki mają 200 unikalnych SHA-256 — cztery pary tych samych bajtów (~1,65 MB nadmiarowych plików źródłowych).
- **Tylko dwie pary identycznych danych wystąpiły równocześnie w audycie sceny:** `underground_bunker_textures/24_Color.png` == `31_Color.png` oraz `46_Color.jpg` == `47_Color.jpg`. Wszystkie cztery mają 1024² ASTC 4×4; po bezpiecznym przekierowaniu referencji teoretycznie mogłyby zredukować około **2,67 MiB** pełnych map GPU. **Nie jest to jeszcze osiągnięta oszczędność:** prefab GLB i importer mogą wymagać zachowania osobnych plików, a pliki mogłyby być nadal pośrednimi zależnościami.
- Pozostałe dwie pary w manifeście są z siłowni, ale ich mapa nie wystąpiła równocześnie jako dwa pliki w raporcie sceny — brak powodu do usuwania ich przed APK.
- Wygenerowane warianty liści (`Leaf-*.png`), billboardy (`Billboard-*.png`) i mapy autobusu są uzasadnione odrębnym ustawieniem alfa i/lub trwałą dostawą scen; **nie potwierdzono**, że można je bezpiecznie usunąć. Wymagany jest test źródeł i referencji.

## Kompresja Androida — sprawdzenie kodu

- `BraxObozImport.OnPreprocessTexture` ustawia prywatne mapy do 4096 px, **ASTC 4×4**; billboardy do 8192 px i **ASTC 6×6**.
- `BraxObozMaterials.ConfigurePublicMap`: publiczne 4K **ASTC 4×4**, mipmapy i filtrowanie trilinear, anisotropy 8.
- `BraxObozBuild.Configure`: `EditorUserBuildSettings.androidBuildSubtarget=ASTC` oraz `overrideTextureCompression=NoOverride` — brak globalnego wymuszenia importu 1K/512.
- `BraxObozBuild.BuildAndroidReview` korzysta obecnie z `BuildOptions.None`; opcja kompresji danych archiwum nie jest jawnie ustawiona na LZ4. **Kompresja pakietu nie jest równoznaczna z kompresją tekstur GPU.**
- Projekt zakłada **4K blisko / minimum 2K dalej**; mniej szczegółowe źródła nie odzyskają 4K przez sztuczne powiększenie. Profilowania rzeczywistych mipów na Androidzie jeszcze nie ma.

## Wniosek i decyzja bezpieczeństwa

**Nie wolno usuwać losowych plików przed odzyskaniem działającego APK.** Poważne oszczędności pamięci nie wynikną z samych 4 powtórzeń w manifeście ani z instancjowania drzew, ponieważ ten mechanizm już działa. Następne zmiany powinny dotyczyć bezstratnego zachowania kolorów i PBR przy kontrolowanym streamingu mipmap oraz ewentualnym współdzieleniu kanałów danych, ze zdjęciami i pomiarem telefonu.

Narzędzie z tej gałęzi `BraxSharedResourceAudit.cs` jest **tylko do odczytu**, nie ma prawa modyfikować scen ani usuwać zasobów. Do jego wyników potrzebny jest Unity Editor. Po APK: odczytaj rozmiar `braxhorn-oboz.apk` i `release-identity.json` oraz zbierz pomiary RAM/FPS podczas przejścia z bliska do dalekiego widoku na Androidzie.

**Nie uruchomiono kolejnego buildu ani nie zmieniono `gpt/oboz` na podstawie tego raportu.**
