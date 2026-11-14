"""Read the original T clips from their verified source packages."""
from contextlib import ExitStack, contextmanager
from hashlib import sha256, file_digest
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from zipfile import ZipFile


@contextmanager
def open_clips(catalog, main_archive=None):
    primary = catalog["archive"]
    packages = {primary["name"]: primary, **catalog.get("extra_archives", {})}
    opened = {}
    with TemporaryDirectory() as tmp, ExitStack() as stack:
        def read(clip):
            name = clip.get("archive", primary["name"])
            if name not in opened:
                descriptor = packages[name]
                if main_archive is None:
                    path = Path(tmp) / name
                elif name == primary["name"]:
                    path = Path(main_archive)
                else:
                    path = Path(main_archive).with_name(name)
                if not path.is_file() and (main_archive is None or name != primary["name"]):
                    from pobierz_obsade import pobierz
                    path = Path(tmp) / name
                    pobierz(descriptor, os.environ["GITHUB_REPOSITORY"], path)
                with path.open("rb") as handle:
                    if file_digest(handle, "sha256").hexdigest() != descriptor["sha256"]:
                        raise ValueError("Niepoprawna suma paczki " + name)
                opened[name] = stack.enter_context(ZipFile(path))
            content = opened[name].read(clip["path"])
            if sha256(content).hexdigest() != clip["sha256"]:
                raise ValueError("Niepoprawna suma klipu " + clip["path"])
            return content

        yield read
