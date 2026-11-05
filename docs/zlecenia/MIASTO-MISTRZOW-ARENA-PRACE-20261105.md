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
W rozmowie jest oryginalny ZIP z pasem oraz dwa arkusze szyldów; lokalnie przygotowano pełny `champions_arena_A1_full_import.zip` (ok. 108 MB) z:
- `Assets/Source/Mistrzowie/Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx` + oryginalne mapy w folderze .fbm
- `Assets/Source/Mistrzowie/Trophies/ChampionshipBelt_ARM.png` (roughness/metallic spakowane z oryginalnych map)
- czterema przyciętymi RGBA PNG w `Assets/Source/Mistrzowie/ArenaSigns/`:
  `champion_arena_red_front.png`, `champion_arena_gold_front.png`, `champion_arena_red_fight_night.png`, `champion_arena_gold_arena_walk.png`.

**Pełny ZIP zawiera dodatkowo `Assets/Source/Mistrzowie/VIP/free_luxurious_barrel_seat_2k.glb` (oryginalne siatki, tekstury 2K). Wszystkie pliki binarne nadal wymagają rzeczywistego dostarczenia do GitHub przed budową; ich ścieżki w generatorze nie oznaczają, że już są w repo.** Bez nich kod bezpiecznie pozostawia istniejący szyld i pustą szklaną gablotę, ale wymagana prezentacja pasa NIE jest ukończona.

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


## Uzupełnienie: ekskluzywne miejsca VIP przy sędziach
- Właściciel dostarczył `free_luxurious_barrel_seat.glb` (~50,5 MB) z pięcioma siatkami i sześcioma teksturami 4096×4096.
- Zapisano nieinwazyjną pochodną `free_luxurious_barrel_seat_2k.glb` (~13,6 MB): sześć map 2048×2048, normalne po filtracji ponownie znormalizowane; wszystkie nieobrazowe buforowania geometrii zachowane bajt w bajt.
- SHA256 oryginału: `81d407ca81d33b868ec3915a38d08d00533632ff21ed1a5782d5c57884121e33`.
- SHA256 przygotowanego GLB: `a8896bccfa939235579d0daf9cad294edd4a7c103ae79a1801c2bdc1366356f1`.
- Docelowa ścieżka w repo: `unity-city/Assets/Source/Mistrzowie/VIP/free_luxurious_barrel_seat_2k.glb`.
- `BraxChampionsVIPSeats.cs` generuje 16 foteli z oryginalnej siatki na dwóch stronach chodnika przy stanowiskach sędziowskich. Rozstaw 1,45 m w rzędzie, 1,75 m między rzędami, 0,96 m maks. obrys i prosta kolizja każdego fotela.
- **Stoły sędziów, monitory, ich krzesła i funkcje pozostają bez zmian.** Tanie/przejściowe miejsca VIP nie są generowane, jeśli importowany model jest dostępny. Jeśli GLB nie istnieje w projekcie, obecny model tymczasowy pozostaje (bez fałszywego twierdzenia o ukończeniu wymiany).
- Generator odrzuca mapy większe niż 2048 lub import bez faktycznych poziomów mip (mipmapCount). Filtr glTF minFilter=9987 żąda trilinear mipmapping; zachowanie w Unity wymaga sprawdzenia po imporcie.
- Test statyczny `test_vip_owner_model.py` sprawdza ilość, rozstaw, materiał, warunek importu i nietknięte stanowiska sędziowskie.
- Lokalny łączny ZIP: `champions_arena_A1_full_import.zip` (~107,8 MB) zawiera 11 plików źródłowych: pas, mapy, cztery szyldy oraz VIP GLB. ZIP sprawdzony przez `ZipFile.testzip()`. **Nie został automatycznie przesłany do GitHub**, co blokuje odbiór końcowy i render postaci na urządzeniu.

Status: kod na gałęzi, źródła binarne spakowane lokalnie, przed dopuszczeniem APK potrzebne rzeczywiste dodanie binariów do projektu, kompilacja Unity i test Android. Żadnego automatycznego buildu.

### Walidacja aktualnej pełnej paczki
Archiwum `champions_arena_A1_full_import.zip`: 107 782 665 bajtów, SHA256 `453a5fa9cd637d6a2a733893c453faa12914dc2610f69bf73dd3d5f65fbfcef1`, 13 pozycji (11 zasobów + manifest + README). W lokalnej kontroli: ZIP CRC PASS; 11 x SHA256 PASS; oryginalny FBX bez zmiany bajtów; GLB 5 siatek i 14 węzłów bez zmian oraz 6 map PBR 2K. **Nie jest to test Unity.**
