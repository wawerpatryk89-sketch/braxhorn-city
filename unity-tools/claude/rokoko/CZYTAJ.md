# Ruchy Rokoko na naszych postaciach — próba z 23.11.2026

Właściciel 23.11, 01:54: „dodasz każdej postaci po parę ruchów z Rokoko i wpuścisz na pokój.
I zobaczymy, co z tego wyjdzie.”

## Co tu jest

- `rk_przenies.py` — przenosi ruch z nagrania (Rokoko, Mixamo, HumanIK) na szkielet naszej postaci.
- `rk_podglad.py`, `arkusz.sh` — zdjęcia każdego ujęcia w Blenderze i arkusz do obejrzenia.
- `zlecenie-test-20261123.json` — które nagranie poszło na którą postać.
- `paczka-testowa/` — cztery gotowe postacie próby (`brax_rk`, `nyxa_rk`, `kamil_rk`, `monika_rk`).

## Dlaczego nie „akcja wprost”

Nagrania Rokoko i nasze postacie mają kości o tych samych nazwach (`mixamorig:*`), ale inną
pozę spoczynkową: nagranie spoczywa w pozie T, modele z Tripo w pozie A, a korzeń i skala są
inne (udo 43,2 w nagraniu, 22,7 u nas). Akcja wstawiona wprost nakłada obroty liczone względem
pozy T na pozę A — stąd powykręcany Brax z 18.11.

`rk_przenies.py` liczy dla każdej kości obrót W PRZESTRZENI ŚWIATA względem spoczynku źródła
i nakłada go na naszą kość po wyrównaniu kierunku ramion, przedramion, ud i goleni. Biodra
dostają przesunięcie przeskalowane długością nóg, stopa stoi na ziemi. Nic nie jest poprawiane
ręcznie, klatka po klatce.

## Zasada

Żadne ujęcie nie idzie do gry, zanim nie zostanie obejrzane na zdjęciach na TEJ postaci.

## Jak powtórzyć

    blender -b -P rk_przenies.py -- <model.fbx> <wyjscie> <id> zlecenie.json
    blender --background --python ../osiedle_ruch.py -- <wyjscie>/<id>.fbx <wynik>/<id>.fbx
    blender -b -P rk_podglad.py -- <wynik>/<id>.fbx <klatki> <Color.png> 6 Idle,Walk,RkBoks
    ./arkusz.sh <klatki> arkusz.jpg 6

Nagrania źródłowe (paczka „263 Rokoko motion capture assets” i „16 walk and run cycles”)
nie leżą w repozytorium — są do pobrania za darmo ze strony Rokoko.

## Czego ta próba NIE sprawdza

- Ujęcia Rokoko grają raz, z prawdziwym przemieszczeniem — nie są jeszcze pętlami do gry.
- Chód Braxa i Nyxy to jeden wycięty cykl; pozostałe postacie mają własny, dotychczasowy chód.
- Zdjęcia z Blendera nie pokazują ślizgania stóp ani drgań — to widać dopiero w ruchu, w APK.
