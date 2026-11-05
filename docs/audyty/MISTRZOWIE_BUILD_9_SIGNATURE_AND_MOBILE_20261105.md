# Miasto Mistrzów — awaria APK #9 i optymalizacja mobilna
Data: 5 listopada 2026. Zakres: tylko region Miasto Mistrzów / Unity 6 / Android.
Dowód: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41945016718
Run 41945016718, wejściowy SHA 251aa3f3828ae6b0eaba0773ee523e8948734840.

## Bezsporne fakty z logu
- Unity 6000.0.85f2: "Build Finished, Result: Success", APK 937 MiB (raport \`ls -lh\`).
- Niepowodzenie w \`apk_delivery.py stage\`, przed publikacją: \`RuntimeError: APK is not signed with the existing repository certificate\`.
- Klucz repozytorium przeszedł preflight (weryfikacja CSR), ale **to nie dowodzi**, że wyjściowy APK został podpisany nim prawidłowo. Faktyczny fingerprint starego APK nie został zapisany w logu #9 — nie przypisywać bez dowodu podpisu debug.
- Raport Unity: tekstury 1.1 GB (73.2% user assets), meshes 419.5 MB; widoczne liczne generowane mapy 4K po 21.3 MB każda. To rozmiary *niekompresowane* z raportu budowania, a nie pomiar GPU/RAM na telefonie.
- Obecne inne ostrzeżenia: 1 błąd importu glTF dot. UV set 2 dla \`harbor_crane.glb\` (HarborBeach); 40 ostrzeżeń o osieroconych \`.meta\` dawnych importów; test przejścia wyłączony w #9; runtime i wygląd Braxa niezweryfikowane na telefonie. Nie mylić z przyczyną awarii publikacji.

## Wprowadzone poprawki
1. \`unity-tools/mistrzowie/apk_delivery.py\`: zachowany obowiązkowy test \`applicationId\`, \`versionCode\` i SHA-256 certyfikatu. Jeżeli BuildPlayer zwróci APK z innym/niepoprawnym podpisem, \`apksigner\` podpisuje **tym samym istniejącym kluczem repozytorium**, do pliku tymczasowego. Przed atomowym zastąpieniem oryginału sprawdza podpis oraz tożsamość pakietu. W razie błędu nie publikuje APK i nie nadpisuje oryginalnego pliku. Zero generowania lub wymiany klucza.
2. \`unity-tools/mistrzowie/test_apk_delivery.py\`: przypadki regresyjne dla poprawnego podpisu, naprawy podpisu i odrzucenia błędnego wyniku bez utraty oryginału (kod testów, **nie uruchomiony w Unity/CI**).
3. \`BraxChampionsBuild.cs\`: podcel tekstur Android na ASTC zgodny z ustawieniami importerów PBR; nie nazywa już pliku "signed" zanim zewnętrzny sprawdzacz to potwierdzi.
4. \`.github/workflows/mistrzowie-apk.yml\`: diagnostyka przejść wciąż dostępna po zaznaczeniu, domyślnie pominięta do pierwszej kompilacji; fizyczne kolizje nie zostały wyłączone.
5. Wcześniejsze zmiany tej samej gałęzi: \`BraxChampionsTextureImport.cs\` 2K dla generowanych map otoczenia, mipmaps, streaming, ASTC 4x4, zachowany oryginalny wygląd i tiling; \`BraxChampionsBuild.cs\` ma wstępny budżet 256 MB i redukcję mip level max 3; \`prepare_brax.py\` używa oryginalnego Braxa z sześcioma animacjami.

## Kryteria przed odebraniem
- Preflight Pythona i testy C# / Unity: sprawdzić na nowym SHA.
- Kompilacja Unity do APK, \`apksigner verify --print-certs\` z dokładnie tym samym certyfikatem co JKS, zgodne ID/wersionCode i publikacja linku.
- Zmierzyć rzeczywisty rozmiar, pamięć tekstur i RAM oraz przetestować płynność, jakość blisko/daleko, ruchy Braxa i kolizje na realnym Androidzie.
- Zweryfikować, czy sprzęt obsługuje ASTC. Androidy bez ASTC mogą rozpakować tekstury i zużywać więcej pamięci; w takim przypadku przygotować oddzielny wariant ETC2. Sam ASTC nie gwarantuje działania na każdym urządzeniu.
- Naprawić glTF \`harbor_crane.glb\` dopiero po weryfikacji jego UV/modelu, bez niszczenia materiałów innych rejonów.
- Zbadać osierocone \`.meta\` w runnerze i pochodzenie z cache, bez kasowania assetów wykorzystywanych przez inne rejony.

**Status:** poprawki zapisane w źródłach; nowy APK nie zbudowany ani nie uruchomiony. Weryfikacja pełna i pomiary wymagają osobnego ręcznego uruchomienia przez właściciela. Wpis nie jest dowodem naprawy runtime.
