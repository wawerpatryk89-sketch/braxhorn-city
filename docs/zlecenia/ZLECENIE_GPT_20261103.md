# ZLECENIE dla wykonawcy GPT — Braxhorn City (od 3.11.2026 do poniedziałku 9.11, potem przejmuje z powrotem Claude)

Właściciel: Patryk (pisze po polsku, krótko i ostro — odpowiadaj po polsku, krótko, szczerze). Gra: „GTA połączone z
walkami” na Androida (Unity 6000.0.58f2, Built-in RP, glTFast). Miasto i osiedle to osobne APK. Wizja gry, postacie,
ekonomia: skrót w `docs/zlecenia/ZASADY_I_WIZJA.md` (pełne dokumenty w projekcie właściciela).

## 0. ZASADY BEZWZGLĘDNE (łamanie = właściciel przerywa pracę)
1. **Nic nie wysyłasz bez zgody właściciela.** Push na gałąź, która uruchamia CI (lista niżej), tylko po jego słowie
   „puszczaj”. Pracujesz na własnych gałęziach `gpt/miasto-20261103` i `gpt/osiedle-20261103` (od HEAD podanych niżej).
   Nigdy nie pushujesz na `main`.
2. **Nigdy nic nie nakładamy.** Stara warstwa jest WYCINANA (trójkąty odjęte, Sutherland–Hodgman — gotowe:
   `BraxWycinanieSiatki`, `WytnijZGrupy`, `CutVolumes`, `OznakowanieWWycieciach`), nie przesuwana o milimetry.
3. **Skala:** świat pod Braxa 2.33 m, **K = 1.294** — każdy realny wymiar × K (drzwi 0.9 × 2.1 m → 1.16 × 2.72 m). Ludzie
   bez zmian (baza 2.27 jak w buildzie 44). Modele Poly Haven są w metrach — × K przy wstawianiu.
4. **Premium i detale zawsze** (gzymsy, parapety, cokoły, rynny do ziemi, fugi, krawężniki z łączeniami, listwy,
   ościeżnice, klamki, lampy, tabliczki, faktury). „Lepiej nie zrobić niż zrobić gówno” — rzecz złą i nie do zrobienia
   porządnie USUŃ i domknij porządnie. **Zakaz „klocków”**: żaden mebel, pojazd, maszyna z gołych prostopadłościanów.
5. **Bez marek handlowych, bez wymyślania nazw** (robocze typu „DOMKI-1”). Własna marka gry BRAXHORN — wolno.
6. **Licencje assetów:** tylko CC0 / CC-BY / Free Standard. Bez CC BY-SA. Nic od autorów mortalityrexotable/golukumar.
7. **Repo jest PUBLICZNE.** Modele/obrazy/ruchy właściciela (Patryk, Nyxa, Nina, Monika, grafiki) i paczki Free Standard
   są w repo TYLKO zaszyfrowane (`unity-city/Prywatne/*.tar.gz.enc`, `unity-tools/claude/prywatne.py`, klucz tylko w
   sekrecie CI). Nie masz klucza — nie dodajesz nowych paczek, nie odszyfrowujesz; surowych plików nigdy nie commitujesz.
   Nie ruszasz keystore ani żadnych sekretów.
8. **Jedno wejście dla gości** do każdego budynku (drugie tylko zaplecze/ewakuacja, zamknięte). **Dźwięk wnętrz** słychać
   na zewnątrz tylko przez otwarte drzwi. **Strój walki nie chodzi po mieście.**
9. Moduły Unity WYŁĄCZONE (w CI błąd CS1069): ParticleSystem, Terrain, WheelCollider, NavMesh, Video, Cloth, Wind, 2D,
   Tilemap, UI/Canvas, UIElements, TMP, ScreenCapture, AndroidJava*. Interfejs = IMGUI. Efekty = shader/animacja.
10. Każdy krok budowy w osobnym try/catch (błąd = log „DO POPRAWKI”, build idzie dalej). Niedokończone — za flagą
    wyłączoną. Każda zmiana z logiem `BRAXHORN_*` i 1–2 ujęciami `DETAL_*` w `unity-tools/claude/region_shots.mjs`
    (format `'x,y,z,yaw,pitch,dystans'`, yaw 0 = patrzy na −Z, 90 = −X, 180 = +Z, 270 = +X).
11. Zapis assetów tylko przez `BraxZapisAssetu.Utworz` (nigdy `AssetDatabase.CreateAsset` na zajętej ścieżce — to
    powodowało różowe obiekty).
12. Commity: autor `wawerpatryk89-sketch`, jeden punkt = jeden commit, opis po polsku: co, gdzie, jaki log.

## 1. Stan i gałęzie (po wysyłce 3.11)
| | gałąź CI (push = build ~4.5 h) | HEAD | rejon (`unity-city/rejon-buildu.txt`) |
|---|---|---|---|
| MIASTO | `claude/miasto-opus-20261031` | (uzupełnia Claude przy wysyłce) | `miasto` |
| OSIEDLE | `claude/osiedle-42-poprawka` | (uzupełnia Claude przy wysyłce) | `osiedle` |
APK i zdjęcia CI: draft release `claude-region-preview`. Logi buildu: artefakty przebiegu. Raporty wykonawców tej sesji:
`docs/zlecenia/RAPORT_C_MIASTO_KONIEC.md`, `docs/zlecenia/RAPORT_B_OSIEDLE_KONIEC.md`, pełna lista zgłoszeń właściciela:
`docs/zlecenia/MIASTO_LISTA_20261103.md` (pkt 1–60).

## 2. NAJPIERW: sprawdzenie buildów z 3.11 (zanim cokolwiek zmienisz)
Pobierz logi i zdjęcia obu buildów. Sprawdź i wypisz właścicielowi (krótko, po polsku): `BRAXHORN_MATERIALY_BRAK` (cel 0),
`BRAXHORN_METRO_PRZEJSCIE` (wejście ok, schody_ruchome ok), `BRAXHORN_LUDZIE_STROJE poza_plaza=0`,
`BRAXHORN_LUDZIE_SKALA`, `BRAXHORN_SKALA`, `BRAXHORN_KLOCKI`, `BRAXHORN_WEJSCIA`, `BRAXHORN_DROGI_ZAPORY`, `BRAXHORN_AKWEN*`,
`BRAXHORN_PANORAMA`, `BRAXHORN_BILLBOARDY`, `BRAXHORN_FITNESS`, `BRAXHORN_SALON*`, `BRAXHORN_START`, `BRAXHORN_MIGOTANIE`,
każde „DO POPRAWKI” i każdy wyjątek. Najpierw naprawiasz to, co build pokazał jako zepsute.

## 3. MIASTO — kolejność (szczegóły w MIASTO_LISTA i RAPORT_C)
1. Naprawy z list `BRAXHORN_SKALA` i `BRAXHORN_KLOCKI` (drzwi, lady, siedziska vs Brax; meble/maszyny z klocków → model
   lub porządny kod albo usuń).
2. Wyspa (16, 16b, 23): drzewa-stożki i blade palmy → Poly Haven (pine_tree_01, fir_tree_01, shrub_01…04, island_tree_*
   z naprawionym materiałem liści), skały coast_rocks_* / coastal_cliff_04, piana przy brzegu.
3. Port i kontenery od nowa (8, 8b, 8c, 13, 21): kontenery ISO 20/40 ft × K z blachy falistej (geometria), suwnice na
   szynach, rzut z wymiarami do akceptacji właściciela PRZED budową, test wypłynięcia łodzi z każdego stanowiska,
   łodzie zawsze na wodzie.
4. Sklepy z wyposażeniem i pracownikiem (38), kawiarnia (32–35, WC usunąć), szpital i komisariat (50, 51).
5. NOVA i mieszkanie Braxa (43, 46, 47): zwalić „dziurę”, budynek klubu na całą pierzeję, lasery ruchome, kule świecą;
   brama klatki + mieszkanie premium nad klubem. Wejście na siłownię premium (45): szyld 3D.
6. Schody ewakuacyjne (30), wieżowce „na dachach” tylko tam, gdzie widać nadbudowę (39b), rynny do ziemi (49),
   układ mebli (35), granica lądu: obwodnica/tory/rzeka zamiast murka (20).
7. Dźwięki łodzi i skuterów (14), dźwięk stref we wnętrzach miasta (28), jedno wejście w mieście (29), płynność (44).
8. Metro: pociąg z otwartymi drzwiami i wnętrzem; kolejne stacje = kopia stacji M1 + jazda „prowizorka” (60).

## 4. OSIEDLE — kolejność (szczegóły w RAPORT_B)
1. E23 przystanki + autobus, E24 Żuk, E16 piesi się mijają, E20b ganki/schody z kolizją.
2. Klocki pozostałe (lista z `BRAXHORN_KLOCKI`), sale bez dźwięku stref (szpital, komisariat, sala bokserska, arena,
   lokale B, wille), szpital E33 od nowa.
3. Billboardy osiedla z grafik właściciela (paczka „grafiki”, plan `docs/zlecenia/GRAFIKI_ROZMIESZCZENIE.md`) — mało,
   na nośnikach z ramą, mural tylko na murze.
4. NIE ruszasz bez decyzji właściciela: uliczki S1/S2, rozbudowa B2/gospodarstwo, tunel do miasta (A/B).

## 5. Sprawdzone pułapki
Patrz RAPORT_B §5 i RAPORT_C. Najważniejsze: strefy scen (osiedle B i port doczytywane — drzwi przez `BraxCityDoor.All`),
siatki obiektów osobnych zapisywać jako asset, kolejność kroków w `Build()`, nie zamykać drzwi używanych przez testy CI
(`*-front-door`, `casino-zaplecze-hall`, `gym-rear`), `fetch_polyhaven.py` zmienia klucz cache CI.

## 6. Raport dla właściciela po każdym dniu
Co zrobione (commit + zdanie), co niesprawdzone w Unity, ryzyka, czego potrzebujesz od niego (decyzje, pliki do ściągnięcia).
