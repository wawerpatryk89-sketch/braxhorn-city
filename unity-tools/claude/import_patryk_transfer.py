#!/usr/bin/env python3
"""Import the owner's encrypted replacement using the existing CI key.

No plaintext model, password, or private key is written to the repository.
The existing prywatne.szyfruj function produces the canonical encrypted package.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import zipfile

import prywatne

TRANSFER = prywatne.PACZKI / "import-patryk-20261103"
ZIP_SHA256 = "52d7f58de8ea38f269a3d5c2b24b9667c6c8a7eb36d3784ad0366f0f3ac44b47"


def restore(transfer=TRANSFER):
    if not prywatne.klucz():
        raise RuntimeError("Brak klucza CI do nowego Patryka")
    manifest = json.loads((transfer / "manifest.json").read_text())
    if manifest["zip_sha256"] != ZIP_SHA256:
        raise ValueError("Nieprawidłowa paczka źródłowa Patryka")
    with tempfile.TemporaryDirectory(prefix="patryk-import-") as tmp:
        work = Path(tmp)
        key = work / "key.pem"
        subprocess.run(prywatne.OPENSSL + ["-d", "-in", str(transfer / "import-private.pem.enc"),
                       "-out", str(key), "-pass", "env:KLUCZ"], check=True, capture_output=True)
        key.chmod(0o600)
        password = subprocess.run(["openssl", "pkeyutl", "-decrypt", "-inkey", str(key),
                                  "-in", str(transfer / "package-key.rsa"),
                                  "-pkeyopt", "rsa_padding_mode:oaep", "-pkeyopt", "rsa_oaep_md:sha256"],
                                 check=True, capture_output=True).stdout.decode("ascii")
        env = dict(os.environ, PATRYK_TRANSFER_PASSWORD=password)
        archive = io.BytesIO()
        for i, chunk in enumerate(manifest["chunks"]):
            name = f"part-{i:03d}.enc"
            if chunk["file"] != name:
                raise ValueError("Nieprawidłowa kolejność części Patryka")
            data = (transfer / name).read_bytes()
            if len(data) != chunk["bytes"] or not data.startswith(b"Salted__") or hashlib.sha256(data).hexdigest() != chunk["sha256"]:
                raise ValueError("Uszkodzona zaszyfrowana część: " + name)
            plain = subprocess.run(prywatne.OPENSSL + ["-d", "-pass", "env:PATRYK_TRANSFER_PASSWORD"],
                                   input=data, capture_output=True, check=True, env=env).stdout
            archive.write(plain)
        if len(archive.getbuffer()) != manifest["zip_bytes"] or hashlib.sha256(archive.getbuffer()).hexdigest() != ZIP_SHA256:
            raise ValueError("Suma całej paczki Patryka nie zgadza się z oryginałem")
        model = work / "source" / "patryk"
        model.mkdir(parents=True)
        archive.seek(0)
        with zipfile.ZipFile(archive) as z:
            for info in z.infolist():
                if info.is_dir():
                    continue
                target = (model / info.filename).resolve()
                if model.resolve() not in target.parents:
                    raise ValueError("Ścieżka poza paczką Patryka")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(z.read(info))
        prywatne.szyfruj("patryk", work / "source")
        print("BRAXHORN_PATRYK_IMPORT nowa paczka zweryfikowana; źródłowe pliki zachowane")


if __name__ == "__main__":
    restore()
