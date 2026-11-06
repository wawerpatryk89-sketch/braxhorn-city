# P1 — BASEN MIEJSKI + SPA / Miasto Mistrzów (Unity 6)

Data projektu: 5.11.2026. Wzorzec wizualny: plansza właściciela „P1 — BASEN MIEJSKI + SPA”, przesłana w rozmowie. Zachować wygląd, nie zastępować przeszkleń ciemną elewacją. Nie zmieniać innych działek ani panoramy promenady/plaży.

## Działka i geometria
- Rezerwacja w **oryginalnym masterplanie** `unity-tools/mistrzowie/plan_inventory.json`: **P1**, środek nominalny **X=520, Z=180**, działka **160 × 120 m**, max. **16 m**.
- `unity-city/Assets/Source/Mistrzowie/PoolP1/plot.json` określa niezależny kontrakt rozmieszczenia trzech skrzydeł, parkingu i niecek. Są to nominalne metry przed wspólnym `BraxClaudeOsiedle.K`, zastosowanym tylko raz.
- Hala sportowa **72 × 42 m**, dach łagodnie zaokrąglony **14–16 m**; skrzydło rodzinne **28 × 22 m**, recepcja/SPA **36 × 24 m**.
- Rzeczywiste fizyczne **trzy napełnione baseny**, z niebieską ceramiką PBR 2K i widoczną, ruchomą, półprzezroczystą wodą: sportowy 25 × 17 m, rodzinny 16 × 9 m i dziecięcy 5 × 4 m. Dna i ściany mają geometrię i kolizje; powierzchnia wody nie jest sztywną przeszkodą. `legacy_foundation.py` wycina otwory w gruncie dokładnie pod tymi trzema nieckami. **Jacuzzi nie jest czwartą wykopaną niecką** — cztery autentyczne jacuzzi właściciela stoją w projekcie montażowym na wykończonej posadzce SPA RELAKS, z wodą i bąbelkami. Weryfikacja dopiero po imporcie źródłowych GLB do Unity.
- Osobne rzeczywiste pomieszczenia: lobby, recepcja, kawiarnia, biuro, szatnie K/M/rodzinne, toalety, prysznice, ratownicy, SPA/relaks, sauna, techniczne, magazyny i ścieżki komunikacji. Ściany i posadzki mają materiały i wejścia; bez ciemnych, nieprzezroczystych ścian udających okna.
- Cały teren: wejście z zadaszeniem, chodniki, taras rekreacyjny po wschodniej stronie, dojście techniczne za halą, rabaty, podświetlenia, **95 oznaczonych miejsc parkingowych**, sześć stanowisk TAXI i przejazd do istniejącej siatki dróg.

## Wykończenia, tylko prawdziwe źródła PBR
- Wyłącznie udokumentowane Poly Haven, natywne **2048 × 2048**. Skrypt `unity-tools/mistrzowie/prepare_pool_p1_textures_2k.py` pobiera mapy Diffuse (sRGB), Normal GL i ARM (liniowe), sprawdza wymiary i sumy oraz zachowuje stałe Unity GUID.
- Import Android: **ASTC 4×4**, mipmap streaming, filtrowanie trójliniowe; kolor i normal/ARM zgodne z CityStaticPBR.
- **Czarny asfalt parkingowy**: [Poly Haven Clean Asphalt](https://polyhaven.com/a/clean_asphalt), przyciemniony jedynie materiał P1 (nie całej gry).
- **Odmienny ciepły wzór kostki przy wejściu i na tarasie**: [Poly Haven Herringbone Brick 02](https://polyhaven.com/a/herringbone_brick_02); ważne, żeby odróżniał się od chodników innych rejonów.
- **Jasne płyty chodnikowe**: [Concrete Pavement 02](https://polyhaven.com/a/concrete_pavement_02).
- **Białe ściany**: [White Stucco 02](https://polyhaven.com/a/white_stucco_02); **wykończone podłogi/niecki**: [Long White Tiles](https://polyhaven.com/a/long_white_tiles) z odrębnymi tintami ceramicznymi; **dekoracyjne SPA**: [Marble Mosaic Tiles](https://polyhaven.com/a/marble_mosaic_tiles); **drewno**: [Teak Veneer](https://polyhaven.com/a/teak_veneer); **kamienne elementy**: [Granite Tile 03](https://polyhaven.com/a/granite_tile_03).
- Szkło to prawdziwa przezroczystość shaderowa (alpha blending, metaliczność 0), bardzo jasny subtelny tint, bez paneli imitujących czarne lustro.

## Wyposażenie dostarczane przez właściciela
Właściciel przesłał w rozmowie paczkę Beach & Water Park z modelem zjeżdżalni, modele sauny, leżaków, drabinki, trampoliny, stołu do masażu i umywalki. **Oryginalne jacuzzi 3D oraz grafiki P1 BASEN i P1 SPA są dostarczone w rozmowie/Bibliotece.** Kod P1 próbuje instancjonować oryginalne modele po prywatnym imporcie. Gdy brakuje źródłowego GLB, zachowuje pusty oznaczony socket i ostrzeżenie, a nie podróbkę. Wydanie GitHub Release z tymi modelami nie jest jeszcze zweryfikowane. Po udostępnieniu paczki należy skontrolować format, jednostki, rzeczywiste 2K/4K tekstury, koordynaty prefabów, spójność licencji i kolizje. Miejsca montażowe, zwłaszcza dużej zakręconej zjeżdżalni i jacuzzi, są prowizoryczne i wymagają wymiarów źródłowych.

## Integracja / ochrona innych wykonawców
- Gałąź: `gpt/miasto-mistrzow-basen-p1-20261105`.
- P1 to osobna klasa `BraxChampionsPoolP1`, z osobnym `PoolP1Pbr2K` i źródłami; `BraxChampionsBuildings.Generate()` wywołuje jej generator po T11.
- Nie zmieniać: promenady, portu, plaży, A1, T01, T11, istniejących materiałów ani modeli innych rejonów.
- Scalanie z gałęzią wykonawcy: najpierw porównać zmiany w `legacy_foundation.py` (wycięcie otworów pod P1) i we wspólnym workflow; zachować obie modyfikacje. Przy zmianach zawartości obowiązują bieżące zasady Addressables i bazowego APK.

## Granice dowodów
Stan: **kod źródłowy i kontrole statyczne**; nie jest to zapis z fizycznego telefonu ani odbiór wyglądu. Bez wyraźnej decyzji właściciela nie uruchamiamy kosztownej kompilacji Unity lub APK. Przed akceptacją: compile/reopen Unity 6, spacery po wnętrzu, widoczność przez fasadę, materiał asfaltu/kostki z bliska, geometrię niecek bez przebijającego gruntu, wodę i ruch postaci w basenach, FPS/RAM na Androidzie i dopiero potem dostarczenie aktualizacji.

## Oryginalny szyld basenu — zgodnie z poleceniem właściciela 5.11.2026
- Front recepcji ma mieć **fizyczną bryłę szyldu**: kaseton z mocowaniami, aluminiową obwódką i wąską linią świetlną. Oryginalną grafikę właściciela naklejamy na front w proporcji **4:1** bez kadrowania, zmian koloru i bez sztucznie wygenerowanego napisu.
- Kod: `unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1OriginalSign.cs`; kaseton 10,0 × 2,5 m, umieszczony nad frontowym wejściem, bez przecięcia z daszkiem ani dachem SPA. Obramowanie po wczytaniu PNG jest kolorystycznie wyprowadzane z **rzeczywistych pikseli obrzeża grafiki**, nie z odgórnie wymyślonej palety.
- Wymagane dokładne źródła oryginalnych PNG 2048 × 512 z paczki 148 szyldów: `Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/P1_BASEN_BASEN_MIEJSKI.png` oraz `P1_SPA_SPA_WELLNESS.png`, zawsze z oryginalnymi `.meta`/GUID. Przy innych plikach z `BASEN` (TR10 i taras) NIE wybierać po słowie kluczowym. Loga bez zmiany pikseli na fizycznych kasetonach z kolorystyczną obróbką obrzeży.
- **Status źródła:** oryginały obu grafik odnaleziono i zweryfikowano w prywatnym, dwuwarstwowym archiwum użytkownika `MIASTO_MISTRZOW_SZYLDY_148_I_MURALE_3(1).zip` → `MIASTO_MISTRZOW_SZYLDY_01_06_UNITY.zip` → `01_GLOWNE/`. Przygotowano je także w oddzielnej paczce P1 z oryginalnymi GUID. **Nie potwierdzono jeszcze ich fizycznego umieszczenia w widocznym wydaniu GitHub Release**; kod bez ich zaimportowania nie rysuje sztucznej grafiki.
- Wymagana dalsza akcja: ustalić prawidłowy identyfikator wydania GitHub i dokładny plik BASEN, pobrać oryginał i dopiero wtedy wizualnie zatwierdzić kolor oraz położenie kasetonu. Nie uruchamiać kompilacji Unity/APK bez polecenia właściciela.

## Nowa decyzja właściciela — 5/6 listopada 2026: woda i pływanie
- Poprzedni zakaz wody **został odwołany**. W basenie sportowym, rodzinnym i dziecięcym są animowane, niebieskie powierzchnie wody (`BraxChampionsPoolP1Water.Basin` i shader `Braxhorn/ChampionsP1PoolWater`).
- Baseny 25m i rodzinny mają oddzielne **trigger volumes** z globalnym komponentem `BraxSwimmingZone` i wspólną obsługą w `BraxGymPlayer`. Brodzik płytki pozostaje do brodzenia. To źródłowa integracja P1, **bez testu działania w Unity/Androidzie**. Pozostałe rejony potrzebują podłączenia odpowiednich stref do swych akwenów.
- Wizualna woda to cienka bezkolizyjna powierzchnia; dno i ściany zachowują fizyczne kolizje. Nie dodawać pełnej płaszczyzny wody poza obrysem niecki, bo zakryje posadzki i będzie migotała.
- Efekt jacuzzi ma gotowy tryb `_Bubbles=1`. **Oryginalny model `hot-tub-original-exterior-model.glb` jest dostarczony**, ma wymiary ok. 2,23×2,85×0,98 m; funkcja `BraxChampionsPoolP1SpaFurnishing.Generate` instancjonuje do 4 sztuk i dodaje ruchomą wodę `BraxChampionsPoolP1Water.Jacuzzi(...)` na wysokości niższej od rzeczywistego rantu. Montaż wymaga importu paczki; bez GLB zachowuje socket źródła.
- **Nie tworzyć ani nie zmieniać animacji.** Oryginalne klipy, rig i sterowanie eksploracją pozostają bez zmian. Mechanika wejścia w wodę działa na poziomie wspólnego kontrolera; wykorzystanie istniejących klipów pływania właściciela wymaga osobnej weryfikacji źródeł, bez wymuszania nowych.
- Odbiór dopiero po udanym Unity 6: widok przez szkło (czy widać wodę), wejście do basenu, unoszenie postaci, wyjście, brak przechodzenia przez dno, brak migotania wody i podłogi; potem test Android FPS, pamięci i dotyku. Żadne budowanie APK nie zostało zlecone.

## Zjeżdżalnia — wykonanie ze źródła właściciela
- Źródło z przesłanego `beach-and-water-park.zip`: **`raft-slide-water-park.glb`**, 281288 bajtów, SHA-256 `e001b4e47c59e43c31155b8d4c8e2f3f3caa3ac9627de9753d10a02802cc7a8f`. Weryfikowane bezpośrednio na przesłanym pliku.
- Oryginalny model pochodzi z [Shuriken Studio](https://shuriken.studio/models/props/raft-slide-water-park); licencja pozwala włączyć i modyfikować model w grze, nie pozwala rozdawać surowych modeli jako osobny pakiet. Dlatego **nie publikujemy źródłowego GLB jako samodzielnej biblioteki**. `prepare_pool_p1_slide.py` pobiera właściwy plik z oficjalnego adresu w etapie CI, sprawdza SHA-256 i stabilny Unity GUID.
- `BraxChampionsPoolP1Slide.Generate` umieszcza element wyłącznie za skrzydłem rodzinnym P1: środek nominalny (X=-54,Z=47) względem działki, współczynnik modelu 0,18, wspólny `K` zastosowany raz. Rzeczywiste źródłowe wymiary 77,65 × 33,0 × 127,18 po przeskalowaniu dają ok. **14,0 × 5,94 × 22,9 m**. Granice modelu: lokalne X około -60,0..-46,0, Z około 36,25..59,14 — nie przecina hali rodzinnej i mieści się na działce.
- Z GLB zachowujemy rzeczywiste koryto zjeżdżalni, podporę, bramkę startową i obrys zbiornika zjazdowego. Usuwamy importowane tło plażowe/palmy, dekoracyjne napisy i wodę niskiej jakości. Koryto otrzymuje odrębne wykończenie jasnego niebieskiego laminatu.
- W miejscu zabawkowych schodów/podestu tworzymy **18 fizycznych stopni**, właściwy podest na podporach, poręcze, zabezpieczenia po bokach, dojście z nawierzchnią PBR; zmiany są oddzielone od oryginalnego prefabowego modelu, co umożliwia późniejszą podmianę bez prucia hali.
- Zbiornik odbiorczy korzysta z projektu `Braxhorn/ChampionsP1PoolWater` (falująca turkusowa woda). Zmniejszono długość tylnej ścieżki i pominięto kolidujące donice, żeby uniknąć nachodzących powierzchni. `BraxChampionsPoolP1Water.SlideLanding` nie dodaje nieprawdziwej geometrrii zjeżdżalni.
- **Granica:** nie realizujemy animacji zjazdu ani logiki atrakcji na tym etapie; celem są poprawna architektura, dojście, podest i wykończenia. Weryfikacja konstrukcji po kompilacji Unity/Android; nie uruchamiać APK bez decyzji właściciela.

## Nowe przesłane modele — audyt źródeł
- `sauna_product_model.glb`: 6 osadzonych map 2048×2048 — dobry kandydat do finalnego montażu.
- `pool-ladder.glb`: 3 mapy 2048×2048 — dobry kandydat do wymiany proceduralnych barierek przy nieckach.
- `diving-board.glb`: 6 map 2048×2048 — możliwy montaż po sprawdzeniu bezpiecznego głębokiego miejsca.
- `portable-massage-table.glb`: większość obrazów 128–512 px, więc nie deklarujemy jakości 2K. Wymaga wymiany map lub lepszego modelu przed odbiorem.
- `double-bathroom-vanity.glb`: 9 map 1024×1024 — wymaga lepszego źródła/ponownego wykończenia 2K, inaczej nie spełni wymagania.
- `Sun-lounger+FBX.zip`: zawiera FBX, bez odrębnych map źródłowych w archiwum; dopasować potwierdzone Poly Haven PBR 2K.
- `luxury-modern-spa-interior-scene-sketchup.zip`: 93 MB, przede wszystkim scena `.skp` i obrazy, nie należy jej automatycznie wstawiać do Unity. Dopiero po ekstrakcji/konwersji i weryfikacji wnętrza.
- Fizycznych plików przesłanych wyłącznie do rozmowy nie da się uznać za zaimportowane w GitHub CI; potrzebny jest rzeczywisty plik w pobieralnym, uprawnionym źródle. Podawane punkty montażowe nie są dowodem instalacji modeli.

## GLOBALNE PŁYWANIE — dyspozycja właściciela z 5/6 listopada 2026
- Pływanie jest **globalną funkcją sterowanej postaci** w całym BRAXHORN CITY, nie specyficzną animacją ani systemem basenu P1.
- Wspólny `BraxGymPlayer` zawiera stan wejścia/wyjścia ze strefy wody i lekką wyporność. Każdy rejon może użyć `BraxSwimmingZone` na swojej wodzie (P1 wykorzystuje adapter `BraxChampionsPoolSwimVolume` z zachowanym GUID), bez kopiowania kontrolera lub animacji.
- System **nie zmienia** istniejących klipów ruchu, wsiadania do samochodu, łodzi ani sterowania kamerą; wejście do strefy wody jest ignorowane podczas siedzenia/podróży łodzią.
- Ten commit integruje strefy basenów P1. **Pozostałe akweny miasta, wysp i innych rejonów nie są jeszcze podłączone do globalnego systemu stref** — wymaga to ich oddzielnego audytu i montażu, nie deklarujemy działania pływania wszędzie przed testem.
- Obowiązują obecne ustalenia: na tym etapie priorytet to teren, pełna geometria i wykończenia 3D. Animator i nowe klipy pozostają poza zakresem, kontroler ma służyć zwiedzaniu do wykrywania błędów. 

## Aktualizacja 6.11 — dwa pokoje masażu, sauna, cztery jacuzzi i szyldy
- Nowy kod `BraxChampionsPoolP1.SpaMassageRooms()` buduje **dwa odrębne gabinety masażu** z wykończonymi ścianami, prawdziwymi wejściami z bocznego korytarza i oświetleniem. To dobudowa w przestrzeni przy wewnętrznym łączniku SPA–hala; zachowany front przeszklonego SPA i wejście od ulicy.
- `BraxChampionsPoolP1SpaFurnishing.Generate()` pobiera wyłącznie **oryginalne źródłowe modele użytkownika**: sauna `sauna_product_model.glb`, dwie leżanki `portable-massage-table.glb`, podwójna szafka/umywalka `double-bathroom-vanity.glb` (nie nazywać nieprawdziwie toaletką kosmetyczną), oryginalne jacuzzi `hot-tub-original-exterior-model.glb` ×4, cztery kabiny `shower_cabin.glb`, banki `lockers.glb` ×2 i ławka szatniowa. Nie dodaje fikcyjnych modeli, a w przypadku braku źródła raportuje wymagany import. Skalowanie jest **jednolite** i wyprowadzone z `Renderer.bounds`, więc bez rozciągania.
- Jacuzzi: pokój SPA RELAKS 10,8×7,8m, rzeczywiste zajęte szerokości 2,23m, głębokości 2,85m; środki (9,2;−11,85), (14,7;−11,85), (9,2;−15,65), (14,7;−15,65) w nominalnych metrach P1. Pośrodku odstęp między kolumnami ok. 3,27m, między rzędami ok. 0,95m; **to ocena wymiarowa, nie fizycznie sprawdzone dojście w Unity**.
- Oryginalne szyldy 4:1: `P1_BASEN_BASEN_MIEJSKI.png` na głównym kasetonie 10×2,5m oraz `P1_SPA_SPA_WELLNESS.png` na prawej części fasady na kasetonie 4,2×1,05m. Bez zmiany źródłowego PNG/GUID.
- Wygenerowana **prywatna paczka źródłowa** `P1_SPA_ORYGINALY_MODELE_SZYLDY_DO_RELEASE_20261106.zip` zawiera 7 GLB + 2 PNG oraz ich oryginalne/plombowane .meta i plik manifestu. SHA256 całego archiwum: `3b86a094f3b4b31ff11597d78272e853181857f057d429f8b4d13493ab0059e9`. Importer offline: `python3 unity-tools/mistrzowie/import_pool_p1_spa_bundle.py ARCHIWUM --dry-run`, potem bez `--dry-run`. **Paczka nie została jeszcze przesłana jako asset do GitHub Release**, nie uruchomiono Unity 6 i APK.
- Spirala `water_slide_spiral_5_turn.glb` zawiera około 460 612 trójkątów bez źródłowych map i **nie jest dopuszczona do P1 jako pełna siatka**. Redukcja 2k/8k dotychczas powodowała zbyt wiele fragmentów. Dopóki nie będzie ciągłej, wizualnie poprawnej LOD, P1 zachowuje aktualny lżejszy moduł zjeżdżalni. Nie deklarować gotowej spirali.
