# OBÓZ BRAXHORN — grafiki właściciela na budynkach / bramie
Data: 2026-11-05. Status: KOD INTEGRACJI GOTOWY, ASSETY JESZCZE NIE WCIĄGNIĘTE DO UNITY, BRAK ODBIORU W PLAY MODE I ANDROID.

## Zakres
Na prywatnym źródłowym archiwum właściciela:
BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip
rozmiar 345273037 B, SHA-256 b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006
jest pięć oryginalnych grafik, a nie nowe prace wygenerowane przez wykonawcę.

| Grafika | Źródło | Lokalizacja docelowa |
| --- | --- | --- |
| OBÓZ WOJOWNIKÓW | 87405F34-A95B-4AD6-8637-A19599AADA61.jpeg | drewniana elewacja domku 1 |
| DROGA MISTRZA | 6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg | drewniana elewacja domku 3 |
| BRAXHORN FIGHT ZONE | 35502A48-F7AB-4650-9874-3266881B2DF6.jpeg | fizyczna ściana pawilonu treningowego, obok strefy walki |
| BRAXHORN TRAINING CAMP | 6B6E1CA3-605A-47FB-BF0E-F23B1D2C3301(1).jpeg | fizyczna ściana siłowni pod dachem |
| SZYLD BRAXHORN TRAINING CAMP | 44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg | oryginalna brama (0,-125) na korpusie stalowym 3D, światło 8.4 m |

## Techniczne zasady 1:1
- **Nie przerabiać logo, napisu, kompozycji, proporcji ani kolorystyki.**
- Każdy mural renderowany na pełnym quadzie 16:9 z UV 0..1 i własną fizyczną ścianą budynku lub istniejącą elewacją. Bez rozciągania/powtarzania/kadrowania.
- Szyld: istniejąca brama zostaje na tym samym miejscu. Szerokość tablicy 8.4 m, metalowa wielowarstwowa bryła 24 cm grubości, środkowe wzmocnienie i podpory zespolone z ryglem. Nie kłaść samej płaskiej tekstury w powietrzu.
- Plik wejściowy szyldu ma białe tło. Kod odsłania oryginalną sylwetkę poprzez flood fill od obrzeży i robi PNG przezroczyste w wygenerowanym prywatnym katalogu. Wewnętrzny oryginalny kolor i litery pozostają. Trzeba obejrzeć krawędzie na telefonie.
- Źródło muralu **1536x864**, źródło szyldu **1536x658**. To mniej niż rzeczywiste 2K po szerokości; importer 2K nie tworzy nowych pikseli. Nie ogłaszać spełnienia wymogu bliskiego 2K dla tych pięciu plików bez nowego oryginału lub osobno zatwierdzonej obróbki.
- Standard PBR Standard z minimalnym połyskiem, żadnego emissive/bloom; mipmapy/trilinear/aniso i ASTC 4x4 na Android. Wysoka jakość nie zwalnia z pomiaru pamięci/ostrości na telefonie.
- Brak prywatnego JPEG powoduje komunikat BRAXHORN_OWNER_GRAPHICS_PENDING i brak atrapy muralu; oryginalny prosty napis bramy jest zachowany tylko jako niezastępujący go fallback.
- Postacie i animacje/teksturowanie Moniki oraz NPC **NIE SĄ W ŻADEN SPOSÓB EDYTOWANE**.

## Jak przywrócić materiały z archiwum prywatnie
Wykonawca otrzymuje oryginalne archiwum poprzez uprawnioną ścieżkę Library lub bezpieczny transfer.
Na roboczym checkout repo, bez pusha, z katalogu repo:

python3 unity-tools/oboz/import_owner_assets_20261105.py /sciezka/prywatna/BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip
python3 unity-tools/oboz/import_owner_assets_20261105.py /sciezka/prywatna/BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip --extract --expand-nested

Pierwsze polecenie jest WYŁĄCZNIE WERYFIKACJĄ; drugie jest świadomym zapisem do ignorowanej przez Git lokalizacji:
unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets/
Skrypt sprawdza SHA archiwum i każdego z 30 wejściowych plików, nie nadpisuje różnych istniejących źródeł.
Nie wywołuje Unity, APK, GitHub API ani Actions.

## Warunki odbioru
- [ ] Sprawdzenie SHA-256 prywatnego ZIP oraz zebranych źródeł.
- [ ] Załadowanie pięciu JPEG do Unity 6, bez błędów shader/importera.
- [ ] Obecność wszystkich 4 murali na odpowiednich budynkach, światło fizycznej ściany, brak lustrzanych napisów.
- [ ] Brama w starym położeniu, szyld dokładnie na jej szerokość i z prawidłowymi proporcjami, nieprzenikające podpory.
- [ ] Sprawdzenie drożności bramy, drzwi i przejść przy nowym korpusie.
- [ ] Zrzuty na telefonie z bliska i daleka; ostrzeżenia o rozdzielczości źródłowej <2K są jawne.
- [ ] Zapis rezultatów testu Play Mode i Android: brak deklaracji sukcesu z samego C# lub SHA.

Brak automatycznego buildu; gałąź grafiki może być scalona dopiero po przeglądzie z główną gałęzią przebudowy.
