"""Tekst -> gotowa kwestia .ogg, głosem z generatora (Piper, offline).

Tak powstał bank 292 kwestii w grze — więc tą samą drogą idą nowe teksty.
Właściciel nie musi niczego nagrywać: pisze zdanie, tu wychodzi plik.

Ustawienia takie same jak w osiedle_glosy.py, żeby nowe kwestie brzmiały jak
reszta banku: length_scale 0.9, highpass 80 Hz, podbicie 3 kHz, loudnorm -16 LUFS.

  python3 -I zrob_kwestie.py <plik_z_tekstami.json> <katalog_modeli> <katalog_repo>

Plik wejściowy: [{"nazwa": "mateusz_rekl_01_m", "glos": "darkman-medium",
                  "tekst": "Bracie, ty pierdolisz."}, ...]
"""
import json
import subprocess
import sys
import tempfile
from pathlib import Path

FILTR = ("highpass=f=80,equalizer=f=3000:t=o:w=1.2:g=3,"
         "loudnorm=I=-16:TP=-2:LRA=11")


def main(spis, modele, repo):
    pozycje = json.loads(Path(spis).read_text(encoding="utf-8"))
    modele = Path(modele)
    cel = Path(repo) / "unity-city/Assets/Resources/OsiedleGlosy"
    cel.mkdir(parents=True, exist_ok=True)

    for p in pozycje:
        model = modele / ("pl_PL-" + p["glos"] + ".onnx")
        if not model.is_file():
            raise SystemExit("Brak modelu: " + str(model))
        with tempfile.TemporaryDirectory() as t:
            surowy = Path(t) / "x.wav"
            subprocess.run(
                [sys.executable, "-m", "piper", "-m", str(model),
                 "-f", str(surowy), "--length-scale", "0.9",
                 "--noise-scale", "0.5", "--noise-w-scale", "0.6"],
                input=p["tekst"], text=True, check=True,
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            wyjscie = cel / (p["nazwa"] + ".ogg")
            subprocess.run(
                ["ffmpeg", "-v", "error", "-nostdin", "-i", str(surowy),
                 "-ac", "1", "-ar", "48000", "-af", FILTR,
                 "-c:a", "libvorbis", "-q:a", "4", str(wyjscie), "-y"],
                check=True)
        czas = subprocess.run(
            ["ffprobe", "-v", "error", "-show_entries", "format=duration",
             "-of", "default=nw=1:nk=1", str(wyjscie)],
            capture_output=True, text=True).stdout.strip()
        print("  %-24s %5.2f s  %s" % (p["nazwa"], float(czas), p["tekst"][:46]))
    print("zapisane w", cel)


if __name__ == "__main__":
    main(*sys.argv[1:4])
