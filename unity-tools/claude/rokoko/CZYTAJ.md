# Ruchy Rokoko na naszych postaciach — próba z 23.11.2026

Właściciel 23.11, 01:54: „dodasz każdej postaci po parę ruchów z Rokoko i wpuścisz na pokój.
I zobaczymy, co z tego wyjdzie.”

## Co tu jest

- `rk_przenies.py` — przenosi ruch z nagrania (Rokoko, Mixamo, HumanIK) na szkielet naszej postaci.
- `rk_podglad.py`, `arkusz.sh` — zdjęcia każdego ujęcia w Blenderze i arkusz do obejrzenia.
- `zlecenie-test-20261123.json` — które nagranie poszło na którą postać.
- `rk_potok.py` — zwykła droga postaci (osiedle_ruch) dla postaci próbnej bez ustawionej twarzy.
- `rk_mierz.py` — pomiar bez patrzenia: ile przedramię i dłoń wchodzą w tułów/uda, ile skóra
  dłoni jest zgnieciona i rozciągnięta, skręt nadgarstka. Każde ujęcie, co trzecia klatka.
- `rk_dobor.py` + `kandydaci/` — przenosi kandydatów, mierzy, do postaci idą TYLKO te ujęcia,
  które przejdą bramkę (niżej). `rk_wszyscy.sh` puszcza całą piątkę.
- `rk_gdzie.py`, `rk_gdzie_przen.py` — które palce się gniotą / które wierzchołki wchodzą w ciało.
- `pomiary/` — liczby: `*_przed.json` (to, co było w APK 23.11 rano), `*_po.json` (teraz),
  `WZOR_*.json` (zatwierdzona obsada: Kamil, Monika, Patryk), logi doboru.
- `paczka-testowa/` — pięć gotowych postaci próby. Wszystkie to SUROWE modele z listy „do zrobienia”
  (szkic wydania `postacie-globalne-20261112`), które nigdy nie miały żadnych ruchów:
  - `bokser2_rk` — drugi bokser (`72e3042b`): spoczynek, chód, boks, walka z cieniem, pięść w górę, walka „Iron Man”
  - `kulturysta_rk` — kulturysta (`61ff34d9`): spoczynek, chód, kopnięcie okrężne, kopnięcie proste, walka, rzut karny
  - `kobieta_bojowki_rk` — kobieta w bojówkach (`9baee982`): spoczynek, chód, cztery tańce
  - `kobieta_czarna_rk` — kobieta w czarnym stroju (`b4333206`): prowadząca program (lotto), monolog, brawa, dwoje prowadzących
  - `niedzwiedz_rk` — niedźwiedzica-atletka (`00802d59`): rozglądanie się, chód, obserwacja, stukanie stopą, zdziwienie, dłoń na twarzy

  NIE WOLNO brać postaci, które mają już zrobione ruchy: obsada osiedla, Patryk, Monika, Kamil,
  ani postaci z drugiej gry: Brax, Nyxa, Krov, Vexa, Miś, Danny, Nina (właściciel 12.11 i 23.11, 02:33).

## Dlaczego nie „akcja wprost”

Nagrania Rokoko i nasze postacie mają kości o tych samych nazwach (`mixamorig:*`), ale inną
pozę spoczynkową: nagranie spoczywa w pozie T, modele z Tripo w pozie A, a korzeń i skala są
inne (udo 43,2 w nagraniu, 22,7 u nas). Akcja wstawiona wprost nakłada obroty liczone względem
pozy T na pozę A — stąd powykręcany Brax z 18.11.

`rk_przenies.py` liczy dla każdej kości obrót W PRZESTRZENI ŚWIATA względem spoczynku źródła
i nakłada go na naszą kość po wyrównaniu kierunku ramion, przedramion, ud i goleni. Biodra
dostają przesunięcie przeskalowane długością nóg, stopa stoi na ziemi. Nic nie jest poprawiane
ręcznie, klatka po klatce.

## Bramka (właściciel 23.11, 14:01: „deformacje rąk i przenikanie przez ciało”)

Ujęcie wchodzi do postaci tylko, gdy na GOTOWEJ postaci (po zwykłej drodze) spełnia wszystkie trzy:

| | próg | zatwierdzona obsada |
|---|---|---|
| przedramię/dłoń w tułowiu lub udzie | ≤ 2 % wzrostu | do 3,8 cm |
| najmocniej zgnieciona krawędź skóry dłoni | ≥ x0,15 | x0,15–0,50 |
| najmocniej rozciągnięta krawędź skóry dłoni | ≤ x3,0 | do x2,98 |

Co zmienione w `rk_przenies.py`:
1. Palce: z nagrania brany jest tylko KĄT ZGIĘCIA stawu (bez rozczapierzania), z limitem
   55° (bokser, kulturysta: 35° — ich dłonie gniotą się już przy lekkiej pięści).
2. Palce zginają się razem: kąt każdego stawu w 70 % idzie w stronę średniej czterech palców,
   bo w dłoni z Tripo sąsiednie palce mają wspólne wagi i różny kąt rwie skórę między nimi.
3. Ręka, która wchodzi w ciało, jest odsuwana obrotem ramienia (do 35°) w stronę, z której
   weszła — co drugą klatkę, wygładzone.
4. Pomiar przenikania potwierdza „w środku” promieniem (dalej w głąb musi być druga ścianka
   trafiona od środka). Bez tego dziury przy pachach dawały fałszywe 5–14 cm.
5. Obrót ramienia wokół własnej osi (właściciel 23.11, 15:14: „dłonie strasznie powykręcane
   w spoczynku”). Po samym wyrównaniu kierunku wnętrze dłoni stało obrócone o 83–93° względem
   aktora (poza A z Tripo vs poza T nagrania) — w każdym ruchu dłoń wnętrzem do przodu zamiast
   do uda. Ramię i przedramię dostają ten sam obrót wokół swojej osi, tak że wnętrze dłoni
   w spoczynku patrzy tam, gdzie u aktora. Sprawdzenie: `rk_skret_dloni.py`, zbliżenia
   `rk_dlonie.py` → `pomiary/dlonie/` (górny rząd spoczynek, dolny ruch).
6. Spoczynek sprawdzany PO zwykłej drodze — ona go przebudowuje (dogięcie przedramion), a garda
   boksera wchodziła wtedy w klatkę na 7 cm. Taki spoczynek odpada, bierze się następny.

## Zasada

Żadne ujęcie nie idzie do gry, zanim nie zostanie obejrzane na zdjęciach na TEJ postaci.

## Jak powtórzyć

    blender -b -P rk_przenies.py -- <model.fbx> <wyjscie> <id> zlecenie.json
    blender --background --python rk_potok.py -- .. <wyjscie>/<id>.fbx <wynik>/<id>.fbx bez_poprawek
    blender -b -P rk_podglad.py -- <wynik>/<id>.fbx <klatki> <Color.png> 6 Idle,Walk,RkBoks
    ./arkusz.sh <klatki> arkusz.jpg 6

Nagrania źródłowe (paczka „263 Rokoko motion capture assets” i „16 walk and run cycles”)
nie leżą w repozytorium — są do pobrania za darmo ze strony Rokoko.

## Czego ta próba NIE sprawdza

- Ujęcia Rokoko grają raz, z prawdziwym przemieszczeniem — nie są jeszcze pętlami do gry.
- Chód to jeden cykl wycięty z nagrania i zapętlony.
- Twarz: postacie próbne nie mają odczytanego układu oczu i ust, więc mimika jest u nich wyłączona.
- Zdjęcia z Blendera nie pokazują ślizgania stóp ani drgań — to widać dopiero w ruchu, w APK.
