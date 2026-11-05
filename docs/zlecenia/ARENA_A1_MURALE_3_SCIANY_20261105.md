# Arena A1 — trzy oryginalne murale ścienne (5 listopada 2026)

## Decyzja właściciela
Zachować bez żadnych zmian trzy dostarczone panoramy 1536 × 512:
1. **Ściana na wprost wejścia publicznego:** `BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg`, oryginalny mural z bykiem, gorillą, pitbullem i panterą; środkowa luka 4,10 m szerokości × 4,20 m wysokości pozostaje wolna.
2. **Ściana lewa, patrząc od głównych szklanych drzwi w kierunku oktagonu:** `BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg`, panorama zawodników.
3. **Ściana prawa, patrząc od głównych szklanych drzwi w kierunku oktagonu:** `BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg`, pas BRAXHORN CHAMPION na czarno-złotym tle.

Mural centralny już był podłączony; nie powielono jego geometrii. Dwa nowe murale są generowane przez `BraxChampionsArenaSideMurals.Generate(root)` obok centralnego `BraxChampionsArenaMural.Generate(root)`.

## Dokładne umiejscowienie murali bocznych
W oryginalnych lokalnych współrzędnych hali sprzed zatwierdzonego obrotu o 180° i skali `K=BraxClaudeOsiedle.K`:
- LEWA od wejścia: płaszczyzna `X=-26,11`, wektor normalny **do wnętrza, -X**;
- PRAWA od wejścia: płaszczyzna `X=-145,90`, wektor normalny **do wnętrza, +X**;
- każda panorama na **całej długości bocznej ściany 70 m** od lokalnego `Z=28,3` do `Z=98,3`, wysokość dokładnie `70/3=23,333... m`;
- dolna krawędź `Y=2,65`, nad zachowaną czerwoną listwą ściany `Y=2,40..2,56`, górna krawędź `Y=25,983...` pod sufitem hali na `Y=28`;
- oryginalny rozmiar obrazu 3:1 zachowany, bez rozciągania twarzy i pasa. Brak wymiany konstrukcyjnych ścian ani nowych kolizji; nie zasłania się centralnego przejścia. Dziesięć małych banerów bocznych usunięto, bo przesłaniałyby duże obrazy.

## Pliki i źródło
Minimalny pakiet dwóch nowych murali wraz z oryginalnym centralnym: `ARENA_A1_TRZY_MURALE_ORIGINALY.zip`:
- **9 pozycji**: 3 × bezstratnie zachowane oryginalne JPEG, 3 × metadane GUID, istniejąca `ArenaMurals.meta`, manifest i README;
- **1 378 688 bajtów**, SHA256 `171865e56bff25d23c11d819befd6525d9ae49c08e4b7a1320ae63be6b43736e`;
- oryginalny środkowy `sha256=94ed97faea3125ebde2e8cb9137465202aac2a03d0eb97b9abb213128c68156f`;
- lewy `sha256=97f52b27d4811afe833f45a37a3705084c824204053a25b7f7a4695209c14c5e`;
- prawy `sha256=0e43f5e68d12e3705a8eed5ceb81da1c489647ff0742dd7edc4389588a2c64a2`.

`unity-tools/mistrzowie/import_arena_three_murals.py` weryfikuje SHA całego ZIP, każdego obrazu i każde GUID; nie nadpisuje zmian. `verify_arena_three_murals.py` potwierdza obecność oryginałów przed Unity. `BraxChampionsArenaMuralTextureImport.cs` ogranicza import do **trzech JPEG** i Android ASTC 4×4 z mipmapami, zachowując natywną rozdzielczość obrazów.

## Przekazanie do buildu
Repo: `wawerpatryk89-sketch/braxhorn-city`, gałęzie A1 i T11. Ręczny `.github/workflows/mistrzowie-apk.yml` potrafi pobrać gotowy duży ZIP areny z wydania `champions-a1-20261105` pod faktycznie używaną nazwą `ARENA_A1_DO_GITHUB_POBIERZ_PONOWNIE.zip` oraz — po dołączeniu do **tego samego wydania** — mały ZIP `ARENA_A1_TRZY_MURALE_ORIGINALY.zip`. Nie zmieniać obu pakietów i ich sum SHA.

**Stan:** kod, orientacja płaszczyzn, źródła i ZIP przygotowane. Paczka trzech murali NIE JEST jeszcze przesłana do GitHub Release; bez tego ręczny build powinien zakończyć się przed kompilacją Unity. Kompilacja C#, scena, test z telefonu i wizualny odbiór muralu NIE zostały wykonane. Żadnych automatycznych buildów.
