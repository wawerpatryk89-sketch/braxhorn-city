"""Dobor ruchow z bramka jakosci.
   python3 rk_dobor.py <id> <model.fbx> <kandydaci.json> <katalog_wyj>

1. przenosi WSZYSTKICH kandydatow na postac (rk_przenies.py),
2. mierzy kazde ujecie (rk_mierz.py): przenikanie rak w cialo i zgniecenie palcow,
3. bierze pierwszy dobry spoczynek, pierwszy dobry chod i pierwsze 4 dobre ruchy,
4. przenosi jeszcze raz tylko wybrane i puszcza zwykla droga postaci (rk_potok.py),
5. mierzy wynik koncowy.
Ujecie, ktore nie przechodzi bramki, NIE trafia do postaci.
Bramka (wzor: zatwierdzona obsada — Kamil, Monika, Patryk): przenikanie najwyzej 2 % wzrostu
(~3,6 cm), krawedz palca nie krotsza niz 15 % spoczynku."""
import json
import os
import re, subprocess, sys
from pathlib import Path

B = os.environ.get("BRAX_BLENDER", "/home/claude/bl/blender-4.2.3-linux-x64/blender")
N = Path(__file__).resolve().parent
POTOK = os.environ.get("BRAX_POTOK", str(Path(__file__).resolve().parents[1]))
KLIPY = os.environ.get("BRAX_KLIPY", "/home/claude/test_rk/klipy")   # nagrania Rokoko — nie ma ich w repozytorium
PRZEN_MAX = 0.020
PALEC_MIN = 0.15
ROZC_MAX = 3.0      # najwieksze rozciagniecie skory dloni w zatwierdzonej obsadzie: x2,98 (Monika, Drink)


def blender(*a):
    r = subprocess.run([B, "-b", "--factory-startup", "--disable-autoexec", "--python-exit-code", "1", "-P", *a],
                       capture_output=True, text=True)
    out = [l for l in r.stdout.splitlines() if l.startswith(("BRAX_", "BRAXHORN_POSTAC"))]
    if r.returncode != 0:
        err = [l for l in (r.stdout + r.stderr).splitlines() if "Error" in l or "Traceback" in l or "SystemExit" in l]
        raise SystemExit("BLAD blendera: %s" % " | ".join(err[-4:]))
    return out


def rodzaj(stan):
    """KIdle0 / KWalk2 -> "Idle" / "Walk"; wszystko inne (tez KWalkaZCieniem!) to ruch."""
    m = re.fullmatch(r"K(Idle|Walk)\d+", stan)
    return m.group(1) if m else "ruch"


def dobre(u, H):
    return (u["przenikanie_max_cm"] / (H * 100) <= PRZEN_MAX and u["dlon_max_zgniecenie"] >= PALEC_MIN
            and u["dlon_max_rozciagniecie"] <= ROZC_MAX)


def main(ident, model, kand, wyj):
    kand = json.loads(Path(kand).read_text())
    wyj = Path(wyj); wyj.mkdir(parents=True, exist_ok=True)
    roz = kand.get("rozstaw_nog", 0.4)
    ekstra = {k: kand[k] for k in ("palce_max",) if k in kand}
    wpisy = []
    for i, p in enumerate(kand["idle"]):
        wpisy.append({"stan": "KIdle%d" % i, "plik": p, "ile": 300, "na_miejscu": True, "rozstaw_nog": roz, **ekstra})
    for i, p in enumerate(kand["walk"]):
        wpisy.append({"stan": "KWalk%d" % i, "plik": p, "ile": 400, "chod": True, "rozstaw_nog": roz, **ekstra})
    for st, p in kand["ruchy"]:
        wpisy.append({"stan": "K" + st, "plik": p, "rozstaw_nog": roz, **ekstra})
    z1 = wyj / "etap1.json"
    z1.write_text(json.dumps({"klipy": KLIPY, "potok": POTOK, "postacie": {ident: wpisy}}, ensure_ascii=False))
    for l in blender(str(N / "rk_przenies.py"), "--", model, str(wyj / "etap1"), ident, str(z1)):
        if l.startswith("BRAX_RECE"): print(ident, l, flush=True)
    blender(str(N / "rk_mierz.py"), "--", str(wyj / "etap1" / (ident + ".fbx")), str(wyj / "etap1_miara.json"), "3")
    m = json.loads((wyj / "etap1_miara.json").read_text()); H = m["wzrost_m"]; u = m["ujecia"]
    for k, v in u.items():
        print("%s %-22s %s  przenika max %.1f cm (%.1f%%)  palec min x%.2f  rozciag. x%.2f" % (
            ident, k, "DOBRE" if dobre(v, H) else "ODPADA", v["przenikanie_max_cm"],
            v["przenikanie_max_cm"] / H, v["dlon_max_zgniecenie"], v["dlon_max_rozciagniecie"]), flush=True)
    def wynik(w): return u[w["stan"]]["przenikanie_max_cm"] - 100 * u[w["stan"]]["dlon_max_zgniecenie"]
    idle_l = [w for w in wpisy if rodzaj(w["stan"]) == "Idle" and dobre(u[w["stan"]], H)]
    walk = next((w for w in wpisy if rodzaj(w["stan"]) == "Walk" and dobre(u[w["stan"]], H)), None)
    if not idle_l or not walk:
        # bez dobrego spoczynku/chodu postac nie wejdzie do gry — biore najlepszy i zglaszam
        idle_l = idle_l or [min((w for w in wpisy if rodzaj(w["stan"]) == "Idle"), key=wynik)]
        walk = walk or min((w for w in wpisy if rodzaj(w["stan"]) == "Walk"), key=wynik)
        print(ident, "UWAGA: spoczynek/chod bez czystego kandydata — wziety najlepszy", flush=True)
    ruchy = [w for w in wpisy if rodzaj(w["stan"]) == "ruch" and dobre(u[w["stan"]], H)][:4]
    # Zwykla droga postaci PRZEBUDOWUJE spoczynek (petla, dogiecie przedramion, oddech) —
    # spoczynek „w gardzie” moze przez to wejsc w klatke. Dlatego spoczynek sprawdzam
    # PO tej drodze, a jak odpada, biore nastepnego dobrego kandydata.
    for nr, idle in enumerate(idle_l):
        wybor = [dict(idle, stan="Idle"), dict(walk, stan="Walk")]
        wybor += [dict(w, stan="Rk" + w["stan"][1:]) for w in ruchy]
        z2 = wyj / "etap2.json"
        z2.write_text(json.dumps({"klipy": KLIPY, "potok": POTOK, "postacie": {ident: wybor}}, ensure_ascii=False, indent=1))
        blender(str(N / "rk_przenies.py"), "--", model, str(wyj / "final"), ident, str(z2))
        (wyj / "final" / "po").mkdir(parents=True, exist_ok=True)
        blender(str(N / "rk_potok.py"), "--", POTOK, str(wyj / "final" / (ident + ".fbx")),
                str(wyj / "final" / "po" / (ident + ".fbx")), "bez_poprawek")
        linie = blender(str(N / "rk_mierz.py"), "--", str(wyj / "final" / "po" / (ident + ".fbx")),
                        str(wyj / "final_miara.json"), "3")
        mf = json.loads((wyj / "final_miara.json").read_text())["ujecia"]
        if dobre(mf["Idle"], H) or nr == len(idle_l) - 1:
            break
        print(ident, "UWAGA: spoczynek %s po zwyklej drodze odpada (%.1f cm) — biore nastepny"
              % (idle["plik"], mf["Idle"]["przenikanie_max_cm"]), flush=True)
    for l in linie:
        print(ident, "KONIEC", l, flush=True)
    print(ident, "WYBRANE:", [w["stan"] + "<-" + w["plik"] for w in wybor], flush=True)


if __name__ == "__main__":
    main(*sys.argv[1:5])
