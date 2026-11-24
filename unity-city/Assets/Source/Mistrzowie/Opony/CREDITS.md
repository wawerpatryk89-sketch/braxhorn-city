# Opony toru kładów

`opona.bytes`: dwa poziomy szczegółów opony do `BraxChampionsOponyKladow`.

1. Sam skan, 2880 trójkątów.
2. Ten sam skan uproszczony meshoptimizerem (gltfpack 1.3.0, `-si 0.3`), z zachowanymi szwami UV, 896 trójkątów.

Format i skrypt: `unity-tools/mistrzowie/opony/opona_lod.py`. Tekstury to własne mapy skanu w 4K, pobiera je `unity-tools/mistrzowie/prepare_assets.py` (`old_tyre`).

| Co | Skąd | Autor | Licencja |
|---|---|---|---|
| Old Tyre: zużyta opona samochodowa, 0,6 m średnicy, 0,165 m szerokości | https://polyhaven.com/a/old_tyre | Poly Haven | CC0 |
