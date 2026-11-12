# ZASADA GLOBALNA: postacie (zawodnicy i bohaterowie) — jedne na całą grę — 12.11.2026 (właściciel, 16:34–19:30)

> „Zobacz tych dwóch, co tam walczą: zdeformowani, nie biją się, źle ciosy dobrane. Trzeba ich tam zrobić, żeby się napierdalali, a nie stoją w miejscu.” (16:34)
> „Pitbull, Doberman — ale oni w obozie się nie nadają, zdeformowani cali, do naprawy od zera.” (16:46)
> „Są animacje T dla nich, wystarczy tylko dodać, bez kombinowania.” (16:48)
> „Brax nie ruszaj!!” (16:54) · „Jaki byk? To Brax.” (16:56) · „Buldog to buldog, pitbul to pitbul.” (16:59)
> „W obozie jest więcej, weź ich, usuń animacje i wstaw im normalnie.” (16:57)
> „Postacie będą musiały i tak być globalnie zrobione, te same pod całą grę.” (16:57)
> „Brax, Nyxa, Krov, Vexa, Miś, Danny, Nina nie rób, bo ja mam ich w drugiej grze, to ich przeniesiemy.” (19:30)

Obowiązuje w całej grze, u wszystkich wykonawców.

## Zasady
- **Jedna paczka postaci dla wszystkich rejonów.** Żadnych kopii w rejonach (jak towar sklepów i sprzęt siłowni).
- **Brax to BYK.** Braxa, Nyxy, Krova, Vexy, Misia, Danny'ego i Niny nie ruszamy — właściciel przenosi ich z drugiej gry.
- **Animacje wprost, bez przerabiania szkieletu.** Wszystkie postacie mają szkielet Mixamo (65 kości). Klipy z paczek
  właściciela kładziemy po nazwach kości (`PatrykPrzenies`: obroty lokalne + wysokość bioder w skali postaci). Zakaz mostu
  Humanoid → Legacy z obozu — to on deformował zawodników.
- **Poza T → paczka „pozycja T animacje”, poza A → „animacje pozycja A”.**
- **Zdeformowana postać nie trafia do gry:** test kości (8 klatek każdego klipu, długość każdej kości ±2 %). Błąd = postaci
  nie ma, build idzie dalej.
- **Zawodnicy mają naprawdę walczyć:** podchodzić, krążyć, bić seriami, reagować na trafienia. Dystans z zasięgu ciosu.

## Źródła od właściciela
- Szkic wydania `postacie-globalne-20261112` („Postacie”): 36 paczek z modelem (35 postaci + kij) i 1 paczka ruchów.
- Szkic wydania `global-animation-library-at-20261107`: `pozycja T animacje.zip` (119 FBX), `animacje pozycja A.zip` (118 FBX).

## Paczka i CI
- `unity-city/Prywatne/braxhorn-postacie-<nr>/` — zaszyfrowana CMS do certyfikatu transferowego CI (jak paczki sklepu).
  Budowanie: `python3 unity-tools/postacie/pakuj_postacie.py <nazwa> <target> <katalog> "<źródło>"`.
- W każdym workflow APK, w kroku z `KLUCZ` przed Unity: `python3 unity-tools/postacie/install_postacie.py` →
  `Assets/Source/Prywatne/postacie/<id>/<id>.fbx` + `<id>_{BaseColor.jpg,Normal.png,ORM.png}` oraz `_ruchy_T/<Klucz>.fbx`.
- W kodzie (Editor): `BraxClaudeOsiedle.PostacJest(id)`, `PostacZawodnik(id, nazwa, rodzic, wzrost, out anim)`,
  `WalkaNaMacie(korzen, srodekMaty, promien, K, idA, nazwaA, idB, nazwaB)` — plik `BraxPostacieGlobalne.cs`.
- W grze: `BraxArenaWalka` (wymiany ciosów, doskok na zasięg, reakcje, bloki, zejścia, kontry, krążenie).

## Paczki (stan 12.11.2026)
| paczka | postacie | klipy |
|---|---|---|
| `braxhorn-postacie-01` | `pitbul` (PITBULL), `doberman` (DOBERMAN) | 33 klipy walki z paczki T: garda, kroki, 10 ciosów i serii, 6 kopnięć, 6 reakcji, 4 bloki, unik, zaczepka |

## Do zrobienia
- Pozostałe postacie w pozie T: bokser, buldog, goryl, Monika, ochroniarz, Patryk, niedźwiedź, nosorożec, pantera,
  kulturysta, bokser (starszy), dzik, wilczyca, wilk, gepardzica, dwie kobiety; w pozie A: tygrysica, boss1.
- Odchudzenie FBX (wbudowane tekstury 4K — 30 MB na postać) przed pakowaniem reszty.
- Nokaut i wstawanie (klipy są w paczce T; wymagają zgrania pozycji leżącej).
