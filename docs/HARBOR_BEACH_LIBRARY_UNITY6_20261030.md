# BRAXHORN CITY — port i plaża z oryginalnej biblioteki (UNITY 6 / TEST ONLY)

## Rzeczywisty stan
- Odizolowana gałąź: `codex/harbor-beach-library-completion-20261030`.
- Baza: odzyskana gałąź `codex/waterfront-recovery-20261030` (commit `de4c194bce83b6c300b29641a095af07f792867`).
- Nowy kod sceny: `unity-city/Assets/Editor/Waterfront/BraxLibraryWaterfront.cs`.
- Komponent ruchu zacumowanych łodzi: `unity-city/Assets/Waterfront/BraxWatercraftMooring.cs`.
- Opt-in entrypoint: `BraxGymBuild.BuildWaterfrontLibraryPreview`.
- Normalne `BuildCity`, `BuildGym`, główna gałąź, produkcja i sklep Nutrition pozostają bez zmian.
- Nie jest to jeszcze zaliczony import/build ani dowód wizualnej jakości Unity/iPhone.

## Dlaczego nie ma sztucznych zamienników
Realne modele z biblioteki właściciela są wymagane przez import:
```
Assets/Source/Waterfront/Models/yacht.glb
Assets/Source/Waterfront/Models/bateau_riva_boat.glb
Assets/Source/Waterfront/Models/jetski_collection.glb
Assets/Source/Waterfront/Models/harbor_crane.glb
Assets/Source/Waterfront/Models/rustic_wooden_dock.glb
Assets/Source/Waterfront/Models/lifeguard_tower.glb
```
Plaża: `Assets/Source/Waterfront/Sand/` z prawdziwych map `coast_sand_02` i `damp_sand` Poly Haven: 2K preview JPG/PNG pochodne z 4K oryginałów `diff`, `nor_gl`, `rough`, `disp`; ARM R=AO/G=roughness/B=metalness. Brak automatycznego zastąpienia modeli prymitywami. Brak plików blokuje build.

Woda zachowuje `BraxCityWaterBuild` (Unity poziom -1.7 m; brzeg X=84 m). Istniejące molo i miasto pozostają. Nowy drewniany pomost, dźwig, 2 jednostki motorowe, 2 wybrane z pięciu wariantów skuterów i prawdziwa wieża ratownicza są rozmieszczone tylko w obszarze sceny testowej. Ruch zacumowanych jednostek to subtelne kołysanie, nie implementacja pływania/sterowania.

### Wykluczone świadomie
- `dock_pier.glb`: GLB zawiera model `VALVoodoohut`; nie pasuje do docelowego realistycznego portu.
- `frickies_yacht.glb`: CC BY-NC, brak dopuszczenia komercyjnego.
- `super-yacht02.glb`: NoAI / licencja Sketchfab Standard wymagająca osobnej kontroli.
- `PortModularSystem.unitypackage`: według audytu semi-stylized oraz niepełne ustalenia licencyjne.

## Kontrola licencji
- yacht — Sergei (CC BY 4.0), https://sketchfab.com/3d-models/yacht-0dd451f295d049cea20c17d3ffa87ee3
- bateau_riva_boat — Configcars / maxipub (CC BY 4.0), https://sketchfab.com/3d-models/bateau-riva-boat-91f0f11ba42b4000abf788d54d73b9b1
- jetski_collection — Jungle Jim (CC BY 4.0), https://sketchfab.com/3d-models/jetski-collection-d261f758b40c45fdb03ddb57fe89a59e
- harbor_crane — MisterH (CC BY 4.0), source URL in original GLB `asset.extras`
- rustic_wooden_dock — Evan16 (CC BY 4.0), https://sketchfab.com/3d-models/rustic-wooden-dock-a2d3be37c4184bc79480912a625a50f1
- lifeguard_tower — exiS7-Gs (CC BY 4.0), https://sketchfab.com/3d-models/lifeguard-tower-8707a73922004d5f8b67088b91a7a393
- coast_sand_02 + damp_sand — Poly Haven (CC0).

Metadane licencji odczytano z oryginalnych GLB. Wydanie komercyjne wymaga atrybucji autora i uwzględnienia ewentualnych znaków towarowych.

## Granice dowodów
Jest kod źródłowy i zweryfikowane źródłowe assety w bibliotece. Nie ogłaszamy Unity build PASS ani wizualnego PASS. Biblioteka ChatGPT nie jest automatycznie montowana w GitHub Actions ani w Unity Cloud. Pochodnych GLB nie należy wrzucać do publicznego GitHuba bez osobnej kontroli praw, a użycie ich w preview wymaga świadomego przekazania do izolowanego środowiska budowania Unity.

Po przekazaniu plików do `Assets/Source/Waterfront/` otworzyć projekt `unity-city` w Unity 6 (pinned wersja w ProjectSettings), wywołać `BraxGymBuild.BuildWaterfrontLibraryPreview` przez Editor/CI i obejrzeć prawdziwe zdjęcia oraz film sterowanego przejścia. Sprawdzić kolizje, powierzchnię morza/piasku, skalę jachtów, oprawę wizualną i FPS na iPhone. Dopiero użytkownik akceptuje. 
