# BRAXHORN — katalog 10 nowych paczek modeli (29.10.2026)

**Stan:** do GitHub zapisano WYŁĄCZNIE SPRAWDZONY KATALOG z SHA-256. Nie przesłano oryginalnych archiwów, GLB, modeli, tekstur ani aplikacji EXE. Nie dodano ich do gry i nie zmieniono produkcji.

Plik `catalog-20261029.json` opisuje dokładnie 10 przesłanych archiwów (481 721 572 bajty łącznie), sumy SHA-256, formaty, zawartość, potencjał jakościowy i prawa użytkowania.

## Podział

- **Do jakości premium / do dalszego przygotowania:** Poly Haven Modern Arm Chair 01 4K (CC0), Abandoned Soviet Garages (Sketchfab CC-BY-4.0, wymaga podania autora).
- **Elementy do testowego montażu/scen:** Gym Environment (FBX/OBJ/Blend), Modular Building 3 (Unitypackage, 12 budynków + elementy modułowe; styl średniowieczny), Low poly kitchen assets (FBX + tekstury).
- **Tylko zapasowe low-poly:** Furniture Vol.2 (30 modeli), Gym (28), Hospital (27), City Vehicles (205 wariantów modeli i kolorów, nie 205 unikatowych pojazdów).
- **Nie importować:** Build - Kitchen.rar — zawiera gotową aplikację Windows (EXE + DLL + pliki buildu), a nie edytowalne modele; nie uruchamiać pobranych plików wykonywalnych.

## Licencje i bezpieczeństwo

- Modele `reodesuxz` — licencje w ZIP pozwalają na wykorzystanie w projektach komercyjnych, ale zabraniają redystrybucji surowych modeli jako assetów, ich odsprzedaży i trenowania AI na nich.
- Poly Haven Modern Arm Chair: CC0, oficjalna strona: https://polyhaven.com/a/modern_arm_chair_01 ; autor Vibrant Nordic. Można korzystać komercyjnie i udostępniać; wymagana optymalizacja 4K do iPhone.
- Sketchfab garages: z metadanych GLB, autor olegolegperepelica, CC-BY-4.0; źródło https://sketchfab.com/3d-models/abandoned-soviet-garages-pack-game-ready-fcf453872c2d4cfbad12197d072fbfc4 ; trzeba zachować informację o autorze i licencji.
- Pozostałe licencje nie były dołączone do archiwów — **nie zakładać** możliwości rozpowszechniania tych pakietów.
- Repozytorium `braxhorn-city` jest **PUBLICZNE**. Surowe paczki z ograniczeniami redystrybucji wymagają wyodrębnionego prywatnego magazynu assetów, do którego dostęp ma wyłącznie właściciel/uprawnieni.
- Dwa duże pliki źródłowe (fotel 138,8 MB i garaże 153,4 MB) przekraczają zwykły limit pojedynczego pliku GitHub Git, więc potrzebują Git LFS lub innego magazynu z kontrolą dostępu. Obecne narzędzia integracji GitHub nie przesyłają bezpośrednio lokalnych binarnych załączników; tego kroku jeszcze nie wykonano.

## Użycie w Unity City

1. Weryfikacja licencji i sum SHA-256 z katalogiem.
2. Przechowywanie oryginałów w oddzielnym magazynie assetów, bez publikowania surowych modeli w publicznym źródle gry.
3. Importowanie po jednym pakiecie w osobnej scenie Unity; sprawdzać format, skalę, pivot, normalne, mapy PBR i liczbę trójkątów.
4. Modele low-poly nie zastępują bez wyraźnej zgody istniejącej oprawy Braxhorn.
5. Po testach — konwersja i optymalizacja modeli szczegółowych do mobilnych LOD i tekstur 1K/2K; oryginały 4K pozostają jako źródła.
6. Po zatwierdzeniu dopiero dodać wybrane elementy do właściwej sceny, bez zmian `main` ani produkcji.

## Cel
Budynki i wnętrza możemy tworzyć własnymi modułami geometrii w Unity ProBuilder lub za pomocą skryptów C#. Modele ludzi, mebli i pojazdów importować z FBX/GLB/Blend po ocenie jakości i licencji. Drzwi, okna otwierane i budynki do zniszczenia wymagają osobnych elementów i logiki, nie wystarczy sam model 3D.
