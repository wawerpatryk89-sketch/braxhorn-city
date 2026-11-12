#!/usr/bin/env python3
"""Trwała kopia plików źródłowych buildu, gdy artefakt Actions wygasł (GitHub kasuje artefakty po 14 dniach).

    GH_TOKEN=... python3 unity-tools/updates/kopia_bazy.py <plik.zip> <katalog docelowy>

Bolid 12.11.2026 (43284852752) padł po 4 minutach: artefakty z 29.10 — „city-rebuild-curated-extended-…” (fasady i materiały
PBR miasta) oraz „unity-kickboxing-review-…” (źródło worków) — wygasły tego dnia w południe. Ich kopia leży od 30.10 w ukrytym
wydaniu claude-base-backup-20261030 („Trwała kopia plików po buildach, które GitHub kasuje automatycznie”, workflow
claude-backup-preview.yml). Ten skrypt pobiera wskazany plik z wydania (REST, po identyfikatorze), sprawdza SHA-256 z przypiętą wartością i rozpakowuje
go dokładnie tam, gdzie krok download-artifact kładł artefakt. Wydanie jest szkicem — potrzebny token z contents: write.
"""
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import zipfile

WYDANIE = "claude-base-backup-20261030"
SHA = {   # SHA256SUMS.txt z wydania (30.10.2026)
    "curated-extended.zip": "91376f3306fb447082c5576cd1659be4816d2f342fb46c19c9eeecba9cbf36bc",
    "kickboxing-review.zip": "fe9ec260463b9a8bd2f5d9c4b97473078a87642cf55f5940b7f7f8096e5a8482",
    "unity6-original-imports.zip": "9bda33e3d1cafb7e81565f5fe6c76d29d07e51d7513b8abbdaa7632069bd3200",
    "original-gym.zip": "55ca12c62b6873ca08390455859e97d04e26cb912d19c76119c0028883341dc4",
}


def sha256(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def rozpakuj(zip_path, cel):
    cel = pathlib.Path(cel).resolve(); cel.mkdir(parents=True, exist_ok=True); n = 0
    with zipfile.ZipFile(zip_path) as z:
        for i in z.infolist():
            dst = (cel / i.filename).resolve()
            if cel != dst and cel not in dst.parents: raise SystemExit("KOPIA_BAZY niebezpieczna ścieżka w archiwum: " + i.filename)
            if i.is_dir(): dst.mkdir(parents=True, exist_ok=True); continue
            dst.parent.mkdir(parents=True, exist_ok=True)
            with z.open(i) as src, open(dst, "wb") as out:
                for b in iter(lambda: src.read(1 << 20), b""): out.write(b)
            n += 1
    return n


def main(plik, cel, pobierz=None):
    if plik not in SHA: raise SystemExit("KOPIA_BAZY nieznany plik " + plik + " (znane: " + ", ".join(sorted(SHA)) + ")")
    with tempfile.TemporaryDirectory() as tmp:
        if pobierz: pobierz(plik, tmp)
        else:
            # Szkic wydania jest niewidoczny dla „gh release download <tag>” — szukamy go na liście wydań przez REST
            # (jak unity-tools/city/fetch_original_imports.py) i pobieramy plik po identyfikatorze.
            repo = os.environ.get("GITHUB_REPOSITORY", "wawerpatryk89-sketch/braxhorn-city")
            asset = None
            for strona in range(1, 6):
                r = subprocess.run(["gh", "api", "repos/%s/releases?per_page=100&page=%d" % (repo, strona)], capture_output=True, text=True)
                if r.returncode != 0: raise SystemExit("KOPIA_BAZY lista wydań niedostępna: " + r.stderr.strip()[:300])
                lista = json.loads(r.stdout)
                for rel in lista:
                    if rel.get("tag_name") == WYDANIE:
                        asset = next((x for x in rel.get("assets", []) if x.get("name") == plik), None)
                if asset or len(lista) < 100: break
            if not asset: raise SystemExit("KOPIA_BAZY w wydaniu " + WYDANIE + " nie ma pliku " + plik + " (token bez prawa zapisu nie widzi szkicu)")
            with open(pathlib.Path(tmp) / plik, "wb") as f:
                r = subprocess.run(["gh", "api", "-H", "Accept: application/octet-stream", "repos/%s/releases/assets/%d" % (repo, asset["id"])], stdout=f, stderr=subprocess.PIPE)
            if r.returncode != 0: raise SystemExit("KOPIA_BAZY nie pobrano " + plik + ": " + r.stderr.decode(errors="replace").strip()[:300])
        p = pathlib.Path(tmp) / plik
        if not p.exists(): raise SystemExit("KOPIA_BAZY brak pliku po pobraniu: " + plik)
        got = sha256(p)
        if got != SHA[plik]: raise SystemExit("KOPIA_BAZY zła suma SHA-256 " + plik + ": " + got)
        n = rozpakuj(p, cel)
    print("KOPIA_BAZY_OK " + plik + " -> " + str(cel) + " (plików " + str(n) + ", SHA-256 zgodne, wydanie " + WYDANIE + ")")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3: sys.exit(__doc__)
    sys.exit(main(sys.argv[1], sys.argv[2]))
