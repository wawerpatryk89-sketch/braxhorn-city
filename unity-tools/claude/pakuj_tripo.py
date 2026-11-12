#!/usr/bin/env python3
"""BRAXHORN — paczka globalna z planszy Tripo właściciela (pojazdy, sprzęt, eksponaty) w formacie paczek sklepowych kolegi
(unity-city/Prywatne/braxhorn-sklep-*/, rozpakowuje unity-tools/sklep/install_sklep_modele.py w CI):

    pakuj_tripo.py <nazwa paczki> <target> <oryginał.glb> <spec.json> <katalog pociętych modeli> "<źródło: kto, kiedy, co>"

  tar (oryginal/<plik>.glb, oryginal/spec.json, <target>/*.glb, <target>/_tekstury/*, <target>/katalog.json)
  -> openssl cms -encrypt -aes256 (DER) do recipient.crt (certyfikat transferowy CI „Braxhorn camp input transfer”,
     ten sam co w paczkach kolegi) -> części <= 90 MB, SHA-256 części, tara i każdego pliku w manifest.json.

Szyfruje tylko kluczem PUBLICZNYM (recipient.crt). Klucz prywatny (recipient.pem.enc) otwiera się wyłącznie w Actions sekretem
KLUCZ — tu go nie ma i nie jest potrzebny. Repo jest publiczne: nic jawnego od właściciela nie trafia do gita. Nie uruchamia
Unity, buildu ani workflow.
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


def pakuj(nazwa, target, oryginal, spec, pociete, zrodlo):
    dst = PRYW / nazwa
    if dst.exists(): shutil.rmtree(dst)
    dst.mkdir(parents=True)
    for f in ("recipient.crt", "recipient.pem.enc"): shutil.copy(WZOR / f, dst / f)
    with tempfile.TemporaryDirectory() as tmp:
        st = Path(tmp) / "staging"
        (st / "oryginal").mkdir(parents=True)
        shutil.copy(oryginal, st / "oryginal" / Path(oryginal).name)
        shutil.copy(spec, st / "oryginal" / "spec.json")
        shutil.copytree(pociete, st / target)
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
            "staging": "unity-city/Assets/Source/Prywatne/Miasto/" + target + " (BraxMiastoAssety.Model)",
            "format": "tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (Braxhorn camp input transfer; its private key "
                      "recipient.pem.enc is protected by KLUCZ in Actions) -> split 90 MB",
            "tarSha256": sha(tar), "cmsSha256": hashlib.sha256(dane).hexdigest(), "parts": parts, "files": files,
            "recipientCertificateSha256": sha(dst / "recipient.crt"),
        }
        (dst / "manifest.json").write_text(json.dumps(manifest, indent=1, ensure_ascii=False) + "\n")
    print("PACZKA " + nazwa + " -> " + target + ": plików " + str(len(files)) + ", części " + str(len(parts)))


if __name__ == "__main__":
    if len(sys.argv) != 7: sys.exit(__doc__)
    pakuj(*sys.argv[1:7])
