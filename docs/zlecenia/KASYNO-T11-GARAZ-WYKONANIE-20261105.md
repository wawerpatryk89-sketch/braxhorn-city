# T11 — KASYNO WAWER, korekta garażu i zamknięcie etapu kodu (05.11.2026)

## Zatwierdzony wygląd: elewacja nietknięta
Budynek T11 pozostaje na oryginalnej działce planu Miasta Mistrzów: środek (495,465), parcela 120x100 m (w jednostkach nominalnych). Fasada zgodna z referencją właściciela: jasny kremowy kamień, grafitowe pylony, złote oprawy, szkło i duży oryginalny szyld C1 `KASYNO WAWER` 2048x512 (4:1). Nie malować zewnętrznych ścian na biało. Wnętrze kasyna i biur bez ścian działowych ani wyposażenia.

## Wybrana przez właściciela okładzina kasyna (zastępuje wcześniejszą propozycję)
- Materiał **Concrete Brick Wall 001** (oryginalny Poly Haven), dokładny slug `concrete_brick_wall_001`: https://polyhaven.com/a/concrete_brick_wall_001
- Na **wszystkie istniejące jasne panele/pilastry elewacji zewnętrznej** T11, gdzie generator używa materiału `cream`; zachować pierwotne proporcje, wysokości, otwory drzwiowe i szklane fasady.
- **Tylko natywne 2K (2048×2048)**: diffuse JPEG, normal GL PNG, ARM PNG. Żadnych nowych pobrań 1K/4K; wybór rozdzielczości jest wymuszany w `prepare_casino_textures_2k.py`, ASTC/mipmap streaming w imporcie Android.
- Zastosować neutralny mnożnik `Color.white` dla jasnych paneli, aby **nie zmieniać barwy oryginalnie wybranej tekstury**. Materiał jest naturalnie jasnobeżowy i ma wyraźne dłutowane cegły, zatem jego faktura nie jest gładkim tynkiem.
- Grafitowe części, metaliczne złote listwy, oświetlenie, duże okna, oryginalny szyld `C1_KASYNO_KASYNO_WAWER.png`, asfalt zewnętrzny i biały garaż B1 **bez zmian**.
- Wcześniejsze `rectangular_facade_tiles_02` nie jest już pobierane ani nakładane jako jasna elewacja T11.

## B1 parking — docelowo
- Tylko **w garażu**: osobny matowy biały materiał na istniejących ścianach i słupach, bez dodawania drugiego zestawu ścian.
- Biały żelbetowy sufit, rzeczywiste poprzeczne i podłużne belki stropowe, 6 białych słupów dotykających stropu i podłogi.
- Czarny asfalt `clean_asphalt` z oryginalnego Poly Haven 2K, nakładany na całą halę ok. 73x53m i na rampę; czarna nawierzchnia ma collider.
- Białe oznakowanie 32 miejsc (2x16, szerokość 3,10m), linie obwodowe oraz osiem krótkich przerywanych znaków środka dwukierunkowej drogi.
- Białe pasy w garażu mają **osobny materiał** `garageParkingPaint`; zewnętrzny lakier parkingowy i cały wygląd zewnętrzny zachowują wcześniejszy kolor.
- Fizyczny zjazd o długości 46m i spadku ~9,5%, osobny wycięty otwór w `legacy_foundation.py`, ściany oporowe, połączenie z posadzką oraz rzeczywiste oprawy bez cieni dynamicznych (mobilne Android).
- Brak dekoracyjnych atrap zamiast nawierzchni. Brak ścian/wyposażenia wewnątrz kasyna.

## Stan techniczny i ograniczenia
Zmiany w `BraxChampionsCasinoT11.cs`, testy kontraktów w `test_casino_t11_garage.py`, `test_casino_2k.py` i `test_casino_materials_2k.py`. Statycznie porównano 12 niezmienników kodu, wszystkie PASS; nawiasy w źródle C# są zbilansowane. **To nie jest kompilacja C# ani uruchomienie fizyki w Unity.**

Źródłowy PNG szyldu C1 i 2K tekstury Poly Haven są referencjami, ale nie są jeszcze binarnymi plikami w tej gałęzi GitHub. Przygotowane skrypty `import_city_signs.py` i `prepare_casino_textures_2k.py` muszą rzeczywiście dostarczyć je do runnera. Nie udawać udanego importu. Zewnętrzne oświetlenie, stropy, prześwity samochodów i działanie aplikacji wymagają weryfikacji w Unity 6 i na Androidzie.

**Zakaz samoczynnego startu APK:** proces ręczny tylko na polecenie właściciela. Nie zmieniać innych stref ani nie scalać na main.
