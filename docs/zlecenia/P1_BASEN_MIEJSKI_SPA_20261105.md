# P1 — BASEN MIEJSKI + SPA / Miasto Mistrzów (Unity 6)

Data projektu: 5.11.2026. Wzorzec wizualny: plansza właściciela „P1 — BASEN MIEJSKI + SPA”, przesłana w rozmowie. Zachować wygląd, nie zastępować przeszkleń ciemną elewacją. Nie zmieniać innych działek ani panoramy promenady/plaży.

## Działka i geometria
- Rezerwacja w **oryginalnym masterplanie** `unity-tools/mistrzowie/plan_inventory.json`: **P1**, środek nominalny **X=520, Z=180**, działka **160 × 120 m**, max. **16 m**.
- `unity-city/Assets/Source/Mistrzowie/PoolP1/plot.json` określa niezależny kontrakt rozmieszczenia trzech skrzydeł, parkingu i niecek. Są to nominalne metry przed wspólnym `BraxClaudeOsiedle.K`, zastosowanym tylko raz.
- Hala sportowa **72 × 42 m**, dach łagodnie zaokrąglony **14–16 m**; skrzydło rodzinne **28 × 22 m**, recepcja/SPA **36 × 24 m**.
- Rzeczywiste fizyczne **trzy puste niecki**, z niebieską ceramiką PBR 2K, bez powierzchni wody: sportowa 25 × 17 m, rodzinna 16 × 9 m, dziecięca 5 × 4 m. Dna i ściany mają geometrię i kolizje. `legacy_foundation.py` wycina otwory w gruncie dokładnie pod tymi trzema nieckami. **Jacuzzi nie jest czwartą wykopaną niecką** — czeka na oryginalny model właściciela, na gotowej posadzce SPA.
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
Właściciel potwierdził, że dostarczy **prawdziwe modele zjeżdżalni, leżaków, szafek, sauny i jacuzzi** w paczce GitHub Release. Kod P1 rezerwuje puste punkty montażowe `OWNER_ASSET_PENDING`, zamiast podstawiać atrapę o przypadkowych wymiarach. Nie deklarować ich jako zamontowanych. Po udostępnieniu paczki należy skontrolować format, jednostki, rzeczywiste 2K/4K tekstury, koordynaty prefabów, spójność licencji i kolizje. Miejsca montażowe, zwłaszcza dużej zakręconej zjeżdżalni i jacuzzi, są prowizoryczne i wymagają wymiarów źródłowych.

## Integracja / ochrona innych wykonawców
- Gałąź: `gpt/miasto-mistrzow-basen-p1-20261105`.
- P1 to osobna klasa `BraxChampionsPoolP1`, z osobnym `PoolP1Pbr2K` i źródłami; `BraxChampionsBuildings.Generate()` wywołuje jej generator po T11.
- Nie zmieniać: promenady, portu, plaży, A1, T01, T11, istniejących materiałów ani modeli innych rejonów.
- Scalanie z gałęzią wykonawcy: najpierw porównać zmiany w `legacy_foundation.py` (wycięcie otworów pod P1) i we wspólnym workflow; zachować obie modyfikacje. Przy zmianach zawartości obowiązują bieżące zasady Addressables i bazowego APK.

## Granice dowodów
Stan: **kod źródłowy i kontrole statyczne**; nie jest to zapis z fizycznego telefonu ani odbiór wyglądu. Bez wyraźnej decyzji właściciela nie uruchamiamy kosztownej kompilacji Unity lub APK. Przed akceptacją: compile/reopen Unity 6, spacery po wnętrzu, widoczność przez fasadę, materiał asfaltu/kostki z bliska, geometrię niecek bez gruntu i bez wody, FPS/RAM na Androidzie i dopiero potem dostarczenie aktualizacji.
