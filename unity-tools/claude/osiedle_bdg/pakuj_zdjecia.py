#!/usr/bin/env python3
"""MIASTO R1 — zdjęcia wzorcowe od właściciela (Bydgoszcz) zapisane w repo na stałe, ZASZYFROWANE.

23.11.2026 zdjęcia wysłane poprzedniemu wykonawcy zginęły razem z jego rozmową — dlatego od teraz każde zdjęcie-wzór
idzie do repo. Repo jest publiczne: tar -> openssl cms -encrypt -aes256 kluczem PUBLICZNYM (recipient.crt, ten sam
certyfikat co paczki sklepowe), części <= 90 MB, SHA-256 w manifest.json. Nazwa katalogu NIE zaczyna się od
„braxhorn-sklep-”, więc CI (install_sklep_modele.py) tej paczki nie rozpakowuje do gry — to wzór, nie zasób gry.

    pakuj_zdjecia.py <katalog ze zdjęciami> <nazwa paczki>
"""
import hashlib
import json
import shutil
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PRYW = ROOT / "unity-city/Prywatne"
WZOR = PRYW / "braxhorn-sklep-walka-01"
CZESC = 90 * 1024 * 1024


def sha(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def main(src, nazwa):
    src = Path(src); dst = PRYW / nazwa
    if dst.exists(): shutil.rmtree(dst)
    dst.mkdir(parents=True)
    shutil.copy(WZOR / "recipient.crt", dst / "recipient.crt")
    files = {str(p.relative_to(src)): sha(p) for p in sorted(src.rglob("*")) if p.is_file()}
    with tempfile.TemporaryDirectory() as tmp:
        tar = Path(tmp) / "zdjecia.tar"
        with tarfile.open(tar, "w") as t:
            for rel in files: t.add(src / rel, arcname=rel)
        cms = Path(tmp) / "zdjecia.cms"
        subprocess.run(["openssl", "cms", "-encrypt", "-binary", "-aes256", "-outform", "DER", "-in", str(tar), "-out", str(cms),
                        str(dst / "recipient.crt")], check=True)
        dane = cms.read_bytes(); parts = []
        for i in range(0, len(dane), CZESC):
            p = dst / ("part-%03d.cms" % (i // CZESC)); p.write_bytes(dane[i:i + CZESC])
            parts.append({"name": p.name, "bytes": p.stat().st_size, "sha256": sha(p)})
        man = {"opis": "zdjęcia wzorcowe właściciela (Bydgoszcz) — do odwzorowania, nie do gry", "tar_sha256": sha(tar),
               "pliki": files, "czesci": parts}
    (dst / "manifest.json").write_text(json.dumps(man, ensure_ascii=False, indent=1), encoding="utf-8")
    print(nazwa, len(files), "plików,", sum(p["bytes"] for p in parts), "B")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
