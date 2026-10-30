# BRAXHORN CITY / PORT + PLAŻA / UNlTY 6 — IZOLACJA

**Status na 30.10.2026:** kod budujący port zapisany na osobnej gałęzi; rzeczywiste źródła 3D z biblioteki właściciela przetworzone do prywatnego pakietu importowego. **NIE PRZESZEDŁ testu kompilacji w Unity 6 ani wizualnego odbioru iPhone.**

## Cel

Rozbudowa oryginalnego miasta Unity 6: istniejące nabrzeże i molo, plaża z prawdziwą PBR teksturą piasku, wieża ratownicza, dźwig, autentyczny drewniany pomost, 2 skutery wodne oraz jacht i motorówka. Materiały pochodzą wyłącznie z dostarczonej biblioteki, bez zastępowania brył generowanymi atrapami.

## Zakres kodu i ochrona równoległej pracy

Wprowadzono tylko:
- `unity-city/Assets/Editor/CityRebuild/BraxHarborBeachBuild.cs` — nowy edytor Unity 6 do dodatkowej części miasta;
- `unity-city/Assets/Editor/CityRebuild/BraxCityRebuild.cs` — pojedynczy, **domyślnie nieaktywny** hak po budowaniu istniejącej wody:
`if(Environment.GetEnvironmentVariable("BRAX_HARBOR_BEACH_SOURCE_READY")=="1") BraxHarborBeachBuild.Apply(root);`
- niniejszy dokument.

Punkt startu gałęzi: `codex/gym-city-renovation-20261030` @ `6ee0260b519543a2176d9441be01d44ec651f0f5`. **Nie wykonano merge, rebase, deploy, zmian produkcyjnych ani modyfikacji sklepu BRAXHORN NUTRITION i siłowni.**

## Miejsce i zakres

- Oryginalna woda Unity: X od 84 m, Y = -1.7 m. Istniejąca geometria miasta zachowana.
- Plaża: X 70..84.35, Z 113..153, materiał PBR z oryginalnych `coast_sand_02_4k` i `damp_sand_4k`.
- Wieża ratownicza: X 74, Z 138, bazowa wysokość ze skanowanego gruntu, źródło `lifeguard_tower.glb`.
- Drewniany pomost: X 99, Z 122, `rustic_wooden_dock.glb`.
- Jacht X 118 Z 100; Riva X 104 Z 48; skutery X 103 /108, Z 84 /85; dźwig X 78 Z -37.

Są to **współrzędne authoringu do zweryfikowania w rzeczywistej scenie Unity**, nie pozycje zatwierdzone wizualnie.

## Zachowane realne zasoby

| Model | Źródło | Status |
|---|---|---|
| yacht.glb | Sergei, CC BY 4.0 | przygotowany |
| bateau_riva_boat.glb | Configcars / maxipub, CC BY 4.0 | przygotowany |
| jetski_01/02.glb | 2 z oryginalnej kolekcji Jungle Jim, CC BY 4.0 | przygotowane, geometria oryginalna |
| harbor_crane.glb | MisterH, CC BY 4.0 | przygotowany |
| rustic_wooden_dock.glb | Evan16, CC BY 4.0 | przygotowany |
| lifeguard_tower.glb | exiS7-Gs, CC BY 4.0 | przygotowany |
| coast_sand_02 i damp_sand | Poly Haven CC0, 4K → 2K | prawdziwe mapy albedo, normal, smoothness, displacement |

**`dock_pier.glb` wykluczony:** w siatkach zawiera `VALVoodoohut`, a użytkownik chce prawdziwą budkę ratowniczą. `frickies_yacht.glb` wykluczony ze względu na CC BY-NC; `super-yacht02.glb` wykluczony do wyjaśnienia licencji/NoAI.

## Realna weryfikacja i blokada publikacji

Przed publikacją wymagane:
1. Umieścić prywatne oryginalne kopie w `unity-city/Assets/Source/HarborBeach/Models` i 2K PBR w `Textures` (gotowy prywatny ZIP użytkownika).
2. Uruchomić Unity 6000.0.58f2, glTFast 6.20.0, `BRAX_HARBOR_BEACH_SOURCE_READY=1`; potwierdzić import 7 prefabs GLB oraz Standard Shader.
3. Kompilacja WebGL/Unity 6 i test obrazu: skala, orientacja, pozycja wieży na plaży, pływanie kadłubów, połączenie pomostu, z-fighting, rzeczywisty piasek i woda, kolizje.
4. Prawdziwy MP4 / zrzuty z tego samego builda oraz test iPhone Safari. Dopiero po jakościowym odbiorze operatora proponować integrację.

**Nie potwierdzono działania w Unity Editor, WebGL ani telefonu.** Niczego nie publikowano. Pliki źródłowe nie są do publicznego repozytorium. Prawa CC BY wymagają atrybucji autorów w grze.
