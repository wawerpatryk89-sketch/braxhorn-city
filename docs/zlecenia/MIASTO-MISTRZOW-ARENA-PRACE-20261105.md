# Miasto Mistrzów — arena A1, pakiet zmian (5.11.2026)

## Zasada wykonania
Pracować wyłącznie w `gpt/miasto-mistrzow-arena-premium-20261105`, bazie `codex/miasto-mistrzow-20261104`.
Nie scalać na `main`, nie uruchamiać buildu Unity/APK bez nowego polecenia właściciela.
Zachować skalę `K=BraxClaudeOsiedle.K=2.33/1.80`, modele grywalne, strukturę Miasta Mistrzów i oryginalne Poly Haven PBR.

## Stan źródłowy sprawdzony
- `BraxChampionsBuildings.cs`: dawne `facade_glass` miało ciemny kolor i tryb **OPAQUE**. Kod teraz nadaje szkłu blend SRC_ALPHA, ZWrite 0, niemetaliczny materiał.
- `BraxChampionsArena.cs`: dawna sala miała cztery punktowe lampy nad oktagonem (zasięg 9 m, intensywność 1.25), tryb ForceVertex, bez cieni. Przesłane filmy wskazywały niedoświetlenie.
- `CityStaticPBR.shader`: cieniowanie czterech światłek lokalnych w jednym przebiegu przy `VERTEXLIGHT_ON`. Nie dodawać reflektorów ForcePixel wymagających ForwardAdd, którego shader nie ma.
- Przed wejściem publicznym były sztuczne, zamknięte segmenty elewacji; otwarto osobne frontowe i tylne otwory; ich drzwi generuje `BraxChampionsArenaArrival`.

## Zmiany zapisane w kodzie
- `BraxChampionsArena.cs`: cztery lampy nad oktagonem o parametrach 13 m/2.15, barwa ciepła biel, bez cieni; widoczne oprawy LED oraz cztery emisyjne pasy na istniejącej kratownicy. Nie dodano nowych real-time lamp nad oktagonem.
- `BraxChampionsArena.cs`: zachowany centralny korytarz dla widzów; rzeczywiste przerwy między sektorami na schody po obu stronach.
- `BraxChampionsBuildings.cs`: alpha-blend szkła i wywołania modułu wejścia/parkingów oraz modułu szyldów wyłącznie dla A1.
- `BraxChampionsArenaArrival.cs`: parkingi po bokach dojścia, oznakowanie miejsc, krawężniki, czerwony dywan ze słupkami i zwisającymi czerwonymi linami, publiczne szklane drzwi, osobne tylne wejście i szatnie, przeszklona gablota pasa; niskokosztowe oświetlenie holu.
- `BraxChampionsArenaSigns.cs`: cztery grafiki CHAMPION ARENA rzutowane na krzywizny elewacji; gdy oryginalne PNG nie są w repo, generator zachowuje poprzednie litery zamiast tworzyć atrapę.
- `BraxChampionsArenaTextureImport.cs`: tylko nowa biblioteka znaków/pasa; Android ASTC 4×4, limit importu 2048, mipmap streaming, bez naruszania plików źródłowych.
- `build_exteriors.py`: frontowy i tylny otwór wejścia, usunięte atrapy zamkniętych drzwi; przestawiony kiosk serwisowy za areną, by nie przecinał tylnego wejścia.

## Pakiet wejściowy od właściciela — NIE MA POTWIERDZENIA IMPORTU DO REPO
W rozmowie jest oryginalny ZIP z pasem oraz dwa arkusze szyldów; lokalnie przygotowano `champions_asset_import.zip` (ok. 91 MB) z:
- `Assets/Source/Mistrzowie/Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx` + oryginalne mapy w folderze .fbm
- `Assets/Source/Mistrzowie/Trophies/ChampionshipBelt_ARM.png` (roughness/metallic spakowane z oryginalnych map)
- czterema przyciętymi RGBA PNG w `Assets/Source/Mistrzowie/ArenaSigns/`:
  `champion_arena_red_front.png`, `champion_arena_gold_front.png`, `champion_arena_red_fight_night.png`, `champion_arena_gold_arena_walk.png`.

**Te pliki binarne nadal wymagają rzeczywistego dostarczenia do GitHub przed budową; ich ścieżki w generatorze nie oznaczają, że już są w repo.** Bez nich kod bezpiecznie pozostawia istniejący szyld i pustą szklaną gablotę, ale wymagana prezentacja pasa NIE jest ukończona.

## Dowody / status
- Statycznie odczytano źródło C# oraz shader; potwierdzono cztery punkty, moc 2.15, 13 m zasięgu, brak dynamicznych cieni, shader emission, integrację A1 i poprawny typ alpha blend.
- Brak połączonego Unity Editor / Unity MCP w tej sesji. Nie wykonano kompilacji C# w Unity ani odtworzenia sceny.
- Brak Play Mode, Android APK, pomiaru FPS / pamięci i wizualnego odbioru.
- Kolejny krok: zapewnić pliki binarne w repo, wykonać test statycznej geometrii całej sceny, dopiero po osobnej zgodzie właściciela ręczny `Mistrzowie — APK`; sprawdzić światło dzienne/nocne, wejścia, trybuny, gablotę i parking na telefonie.

## Ryzyka do kontrolowanego odbioru
- Zewnętrzne parkingi wykorzystują południowy bufor obok działki areny A1 (nie mieszczą się w bryle 220×150 m); zweryfikować przejazd od drogi Z=650, odległość od linii tramwajowej i zachowanie wejścia do metra.
- Szkło wymaga testu sortowania przezroczystości i kosztu overdraw na mobilnym GPU.
- Drzwi działają przez istniejący `BraxCityDoor`; automatyczne otwieranie, kolizje zawiasów i prześwit wymagają testu w scenie.
- Kolizje wszystkich nowych brył, schodów trybun oraz dojścia do gabloty: do testu w Unity.
