"""Pliki repozytorium także przy rzadkim pobraniu (szybkie testy CI: sparse checkout bez zaszyfrowanych paczek .cms / .tar.gz.enc).
Plik jest „w repo”, gdy leży na dysku albo jest w indeksie gita (pominięty przez sparse checkout); rozmiar — z dysku albo z gita."""
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def _rel(p: Path) -> str:
    return str(Path(p).resolve().relative_to(ROOT))


def w_repo(p: Path) -> bool:
    p = Path(p)
    if p.is_file():
        return True
    r = subprocess.run(["git", "ls-files", "--error-unmatch", _rel(p)], cwd=ROOT, capture_output=True)
    return r.returncode == 0


def rozmiar(p: Path) -> int:
    p = Path(p)
    if p.is_file():
        return p.stat().st_size
    r = subprocess.run(["git", "cat-file", "-s", "HEAD:" + _rel(p)], cwd=ROOT, capture_output=True, text=True)
    return int(r.stdout.strip()) if r.returncode == 0 and r.stdout.strip().isdigit() else -1
