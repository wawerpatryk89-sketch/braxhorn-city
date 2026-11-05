# T11 — KASYNO WAWER, korekta garażu i zamknięcie etapu kodu (05.11.2026)

## Zatwierdzony wygląd: elewacja nietknięta
Budynek T11 pozostaje na oryginalnej działce planu Miasta Mistrzów: środek (495,465), parcela 120x100 m (w jednostkach nominalnych). Fasada zgodna z referencją właściciela: jasny kremowy kamień, grafitowe pylony, złote oprawy, szkło i duży oryginalny szyld C1 `KASYNO WAWER` 2048x512 (4:1). Nie malować zewnętrznych ścian na biało. Wnętrze kasyna i biur bez ścian działowych ani wyposażenia.

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
