# BRAXHORN CITY — Metro M1, odbiór zmian w kodzie Unity 6 — 4 listopada 2026

## Zakres
Użytkownik zlecił poprawę **istniejącej** stacji metra, bez tworzenia nowej sieci:
- odsunięcie wejścia od budynku i zapewnienie wejścia bez wody;
- przechodnie schody stałe i ruchome, bez muru przecinającego przejście;
- płynniejsze przewożenie gracza na schodach ruchomych;
- specjalny pociąg z Biblioteki, wjazd, otwarcie drzwi, przejście pasażerów, odjazd i następny kurs po około minucie.

## Pociąg
- W Bibliotece właściciela występuje `metro_train_3d_-_free_3d.glb` (218,29 MB).
- Potwierdzono autora i źródło z metadanych oryginalnego GLB: kane_sk06 / Metro Train 3D – Free, CC BY 4.0.
- W repo istnieje **ten sam model źródłowy** jako `unity-city/Assets/Source/Claude/Metro/metro_train.glb` (25,20 MB). `CREDITS.md` dokumentuje redukcję 383k → 96k trójkątów, 4K → 1K. Nie importujemy ponownie 218 MB do APK.
- Model nie ma animacji ani wydzielonych, nazwanych skrzydeł drzwi. Zamiast twierdzić, że oryginalny GLB jest rigowany, dodano **sceniczny efekt** przesuwanych paneli po stronie peronu (ciemne światło w otworze). To sztuczka, nie rzeczywiste wycięcie drzwi/pełna kabina.
- Dwa istniejące wagony tworzą teraz **jeden ruchomy skład** z jednym kinematycznym Rigidbody. Poprzednie nieruchome kolizje w miejscu postoju usunięte; kolizje wagonów poruszają się z modelem.
- `BraxMetroM1Ruch.cs`: dojazd 10 s → otwarcie 1,4 s → postój 15 s → zamknięcie 1,4 s → odjazd 10 s → niewidoczny tunel 22 s, łącznie 59,8 s. Przy restarcie skład przestawiany jest niewidocznie poza stacją, bez migania.
- `BraxMetroM1Pasazerowie.cs`: maksimum 2 osoby Rocketbox/Pieszy przy jednym postoju, wyłącznie blisko kamery. Jedna odgrywa wejście, druga wyjście. Brak rzeczywistych modeli ludzi jest sygnalizowany; nie tworzymy atrap. Pasażerowie nie wykonują pełnej jazdy AI po sieci metra.

## Wejście, ściana, schody
- Punkt wejścia `EX0`: **66,5 m → 69,0 m**, przesunięcie +2,5 m od narożnika budynku. Przestawiane są z nim: otwór, schody, zadaszenie, murki, kolizje i punkt misji.
- Zasypanie dawnych dziur G5: naprawione widoczne nawierzchnie + kolizja (wcześniej w miejscu dziury pozostawała tylko niewidzialna kolizja). **Nadal wymagany test kolorystyczny na Androidzie — brak dowodu, że niebieska plama/woda zniknęła w całości**.
- W `SchodyNaPeron()` pełna ściana techniczna zajmowała x=54…58,4 i z=25,4…31,5, przechodząc przez schody stałe (z=25,6…28,7) i ruchome (z=28,95…31,25). Wycofana; pozostają tylko boczne krótkie elementy z=25,4…25,52 oraz z=31,35…31,5.
- `BraxSchodyRuchomeJazda.cs`: kontrola po ruchu postaci i przed kamerą, łagodne wejście/zejście z prędkością minimalną 55% przy krawędziach (nie zatrzymuje gracza przed spocznikiem).
- `BraxClaudeMetro.KontrolaPrzejscia()` wywoływana także po wygenerowaniu całego miasta. Testuje chodnik → schody → antresolę → bramki → schody na peron → peron; rejestruje nazwy kolizji, próg i brak podłogi.
- `BraxMiastoOdbior.cs` sprawdza wczytanie dynamicznego M1, liczbę paneli, pasażerów, częstotliwość oraz ląd i przeszkody budynków na dojściu.

## Zasady i dowody
- Silnik **wyłącznie Unity 6**. Osiedle i obóz nietknięte. Zmiany wyłącznie na gałęzi `codex/miasto-naprawy-filmow-20261104`.
- Test przed droższym buildem: `python3 unity-tools/claude/check_metro_glb.py` sprawdza zoptymalizowany model, jego geometrię i limit rozmiaru.
- Dodano pliki `.meta` z trwałymi GUID dla nowych komponentów, aby skrypty nie znikały z wygenerowanych scen.
- **Brak kompilacji Unity i brak APK z tej gałęzi.** Wszystkie wyniki powyżej oznaczają zmianę źródła i statyczny audyt. Trzeba sprawdzić na telefonie wejście bez wody, swobodne przejście, płynność schodów, poprawny przejazd dwóch wagonów, efekt drzwi, obecność NPC, FPS i pamięć. Dopiero wtedy wolno potwierdzić działanie.

## Obowiązkowe logi do odczytu po pierwszym buildzie
`BRAXHORN_METRO_PRZEJSCIE`, `BRAXHORN_METRO_M1_POCIAG_KURS`, `BRAXHORN_METRO_M1_RUCH`, `BRAXHORN_METRO_M1_POSTOJ`, `BRAXHORN_METRO_M1_NPC_WSIADA`, `BRAXHORN_METRO_M1_NPC_WYSIADA`, `BRAXHORN_METRO_M1_WEJSCIE_KOD_OK`, `BRAXHORN_METRO_M1_MODEL_OK`.

W sprawie nazewnictwa „bez budowania”: nie dodawano nowych tuneli ani stacji. Panele drzwi zostały dodane jako ruchomy element efektu scenicznego istniejącego modelu pociągu.

## Kontrola końcowa zmian kodu — bez APK

- Schody ruchome: `BraxSchodyRuchome` oblicza długość kroku w `Start`, po przypisaniu danych generatora, a nie w `Awake` z wartości domyślnej. Usunięto powód widocznego przeskoku taśmy. `BraxSchodyRuchomeJazda` przenosi gracza w `LateUpdate`, po sterowaniu postaci, bez drugiej fizyki.
- Przejście obok schodów: pełny murek techniczny zastąpiono bocznymi fragmentami. Z podłogi na granicy biegów zdjęto dodatkową listwę stalową ciągnącą się przez całe zejście. Balustrady bezpieczeństwa po bokach pozostają.
- Wejście z chodnika: w `BraxMiastoOdbior.SprawdzMetroM1` dodano 27 prób promieniem w dół na trzech pasach dojścia przed otworem. Brak nawierzchni lub hit niżej o ponad 22 cm zgłasza `BRAXHORN_METRO_M1_NIEBIESKA_DZIURA`. To dowód zabezpieczenia w kodzie, **nie dowód, że woda na ekranie Androida zniknęła**.
- Pociąg: do sceny podłączona jest **jedna** implementacja `BraxMetroM1Ruch` z `BraxMetroM1Pasazerowie`; nieużywany drugi sterownik prototypowy usunięto. Sprawdzono strukturę nawiasów obydwu plików oraz generatora Unity.
- Model ma oryginalną metalową burtę i nie ma nazwanych oddzielnych skrzydeł. Drzwi na peronie są zatem inscenizacją rozsuwanych paneli, **nie fizycznym otwarciem oryginalnej karoserii**. Przy tym zakresie nie należy obiecywać grywalnego wejścia Braxa do pociągu.
- W CI model sprawdza `check_metro_glb.py` pod kątem geometrii/rozmiaru i jest testowany przed Unity; dojazd, animacja, panorama i brak wody **wymagają testu w nowym APK**.

**Decyzja o gotowości:** modyfikacje źródłowe przygotowane do pierwszego buildu kontrolnego. Nie ogłaszać całości „ukończone i działające” przed rzeczywistą kompilacją i sprawdzeniem filmów po poprawce.
