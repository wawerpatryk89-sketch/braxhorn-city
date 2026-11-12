#!/usr/bin/env python3
"""BRAXHORN — paczka GLOBALNA postaci (właściciel 12.11.2026 16:57: „Postacie będą musiały i tak być globalnie zrobione,
te same pod całą grę”; zasada docs/zasady/ZASADA_POSTACIE_GLOBALNE_20261112.md).

    pakuj_postacie.py <nazwa paczki> <target> <katalog z zawartością> "<źródło: kto, kiedy, co>"

Katalog z zawartością (= folder <target> w paczce):
  <id>/<id>.fbx              model z Tripo (szkielet Mixamo 65 kości, poza T), oryginał właściciela
  <id>/<id>_BaseColor.jpg    kolor 2K, <id>_Normal.png 2K, <id>_ORM.png 1K (R = 1, G = szorstkość, B = metal)
  _ruchy_T/<Klucz>.fbx       klipy Mixamo bez skóry z paczki właściciela „pozycja T animacje” (ten sam szkielet)
  katalog.json               spis postaci i klipów

Format jak paczki sklepowe (unity-city/Prywatne/braxhorn-sklep-*): tar -> openssl cms -encrypt -aes256 (DER) do
recipient.crt -> części <= 90 MB, SHA-256 części, tara i każdego pliku w manifest.json. Szyfruje tylko kluczem PUBLICZNYM;
klucz prywatny otwiera się wyłącznie w Actions sekretem KLUCZ. Repo jest publiczne: nic jawnego od właściciela nie trafia
do gita. Nie uruchamia Unity, buildu ani workflow.
"""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PRYW = ROOT / "unity-city/Prywatne"
WZOR = PRYW / "braxhorn-sklep-walka-01"          # certyfikat i zaszyfrowany klucz prywatny — wspólne dla paczek globalnych
CZESC = 90 * 1024 * 1024


def sha(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def pakuj(nazwa, target, zawartosc, zrodlo):
    if not nazwa.startswith("braxhorn-postacie-"): sys.exit("nazwa paczki musi zaczynać się od braxhorn-postacie-")
    dst = PRYW / nazwa
    if dst.exists(): shutil.rmtree(dst)
    dst.mkdir(parents=True)
    for f in ("recipient.crt", "recipient.pem.enc"): shutil.copy(WZOR / f, dst / f)
    with tempfile.TemporaryDirectory() as tmp:
        st = Path(tmp) / "staging"
        shutil.copytree(zawartosc, st / target)
        files = {str(p.relative_to(st)): sha(p) for p in sorted(st.rglob("*")) if p.is_file()}
        tar = Path(tmp) / "pack.tar"
        with tarfile.open(tar, "w") as t:
            for rel in files: t.add(st / rel, arcname=rel)
        cms = Path(tmp) / "pack.cms"
        subprocess.run(["openssl", "cms", "-encrypt", "-binary", "-aes256", "-outform", "DER", "-in", str(tar), "-out", str(cms),
                        str(dst / "recipient.crt")], check=True)
        dane = cms.read_bytes(); parts = []
        for i in range(0, len(dane), CZESC):
            p = dst / ("part-%03d.cms" % (i // CZESC)); p.write_bytes(dane[i:i + CZESC])
            parts.append({"name": p.name, "bytes": p.stat().st_size, "sha256": sha(p)})
        manifest = {
            "source": zrodlo, "target": target,
            "staging": "unity-city/Assets/Source/Prywatne/postacie (BraxPostacie — Assets/Editor/Claude/Region/BraxPostacieGlobalne.cs)",
            "format": "tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (Braxhorn camp input transfer; its private key "
                      "recipient.pem.enc is protected by KLUCZ in Actions) -> split 90 MB",
            "tarSha256": sha(tar), "cmsSha256": hashlib.sha256(dane).hexdigest(), "parts": parts, "files": files,
            "recipientCertificateSha256": sha(dst / "recipient.crt"),
        }
        (dst / "manifest.json").write_text(json.dumps(manifest, indent=1, ensure_ascii=False) + "\n")
    print("PACZKA " + nazwa + " -> " + target + ": plików " + str(len(files)) + ", części " + str(len(parts)))


if __name__ == "__main__":
    if len(sys.argv) != 5: sys.exit(__doc__)
    pakuj(*sys.argv[1:5])
