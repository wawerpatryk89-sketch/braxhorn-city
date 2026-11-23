"""Zwykla droga postaci (osiedle_ruch) dla postaci PROBNEJ, ktora nie ma jeszcze ustawionej twarzy.

    blender --background --python rk_potok.py -- <katalog potoku> <model.fbx> <wynik.fbx>

Tabela twarzy (osiedle_twarz.FACES) ma wspolrzedne oczu i ust odczytane recznie dla kazdej
postaci. Postac probna ich nie ma. Zeby mogla przejsc potok, dostaje TYMCZASOWY uklad twarzy —
a w pokoju prob jej mimika jest wylaczona (identyfikator z koncowka _rk), wiec ten uklad
niczego na twarzy nie maluje. Twarz zostaje dokladnie taka, jak w modelu.
Tabela w repozytorium nie jest zmieniana.
"""
import sys
from pathlib import Path

argv = sys.argv[sys.argv.index("--") + 1:]
potok, model, wynik = argv[0], Path(argv[1]).resolve(), Path(argv[2]).resolve()
sys.path.insert(0, potok)
import osiedle_twarz  # noqa: E402
import osiedle_ruch  # noqa: E402

aktor = model.stem
if aktor not in osiedle_twarz.FACES:
    osiedle_twarz.FACES[aktor] = osiedle_twarz.FACES["gitara"]
    print("BRAX_PROBA %s: tymczasowy uklad twarzy (mimika wylaczona w pokoju)" % aktor, flush=True)
if "bez_poprawek" in argv[3:]:
    osiedle_ruch.POPRAWKI_LUDZKIE_POMIJANE.add(aktor)
osiedle_ruch.prepare(model, wynik)
