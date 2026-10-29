# BRAXHORN CITY — Unity 6 / osiedle 1:1

**Gałąź wyłącznie do migracji:** `work/unity6-estate-migration-20261029`. Bez zmian siłowni, miasta, produkcji, main i Braxhorn Arena.

Źródło: oryginalne `dev/osiedle/layout.js`, `level.js`, `world.js`, `civic.js`, `arenas.js`, `doors.js`, `entrances.js`, `vegetation.js` oraz oryginalne modele GLB.

## Wykonany etap techniczny
- Osobny projekt `unity-city` dla Unity `6000.0.58f2`, z glTFast 6.20.0.
- GitHub Actions generuje z rzeczywistych builderów Three.js `Generated/EstateProceduralAndDoors.glb` i `EstateSourceManifest.json`, nie zamieniając obiektów w atrapy.
- Odtwarzane jest oryginalne rozmieszczenie 21 instancji modeli, 30 roślin i 24 drzwi.
- Import Unity kopiuje niezmodyfikowane oryginalne pliki GLB do własnej przestrzeni Assets. Sprawdza hashe SHA-256; brakującego modelu nigdy nie zastępuje innym.
- Skrypt odtwarza oryginalne normalizacje skali, odbicie osi Z między Three i Unity, otwory w ścianach trzech budynków, oryginalne kolizje i pozycje gracza.
- Sterowanie testowe: autentyczny Brax z oryginalnego GLB, klawiatura WASD, Shift (bieg), PPM (kamera), E (drzwi) i dotykowy joystick. Bez nowych zastępczych animacji.

## Instrukcja uruchomienia w Unity
1. Otwórz w Unity Hub podkatalog `unity-city/` (Unity 6000.0.58f2).
2. Otwórz gałąź migracji i zaczekaj na import glTFast.
3. Pobierz/udostępnij oryginalne brakujące modele, zachowując ich nazwy i sumy kontrolne.
4. Wybierz **BRAXHORN → Osiedle → Import 1:1 ONLY if all assets exist**. Scena zostanie zapisana tylko w `Assets/EstateMigration/Scenes/`.
5. Jeżeli oryginałów nadal brakuje, można uruchomić **Partial survey - NOT 1:1**, aby obejrzeć istniejącą geometrię bez zastępowania brakujących budynków. To nie jest odbiór 1:1.

## Potwierdzone problemy
W katalogach bieżącej gałęzi GitHub nie ma trzech modeli wymaganych przez oryginalną grę:
`gameready_casino_scene.glb`, `policestation.glb`, `park_tree.glb`. Poprzedni raport `dev/osiedle/review/report.json` zawierał ich użycie, ale nie samą zawartość binarną. Bez odzyskania dokładnych plików nie można uczciwie ukończyć migracji 1:1.

## Test odbioru — nie zastępować deklaracją
Unity Editor musi skompilować skrypty; trzeba przejść oryginalnym Braxem wszystkie wejścia, wnętrza, tarasy, boisko, park i plac walk, zweryfikować kolizje, zawiasy i kamerę. Potrzebne autentyczne screeny z Unity na etapach 15/30/45/60/75/90/100% oraz test na fizycznym iPhonie i Androidzie. Status **PASS** dopiero po sprawdzeniu gry i akceptacji użytkownika.

**Nie przeprowadzono jeszcze kompilacji w Unity ani testu fizycznego telefonu. Nie ma zgody na merge lub publikację produkcyjną.**
