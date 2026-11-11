# D2 Dworzec kolejowo-autobusowy — modele

| Model | Skąd | Autor | Licencja | Użycie w D2 |
|---|---|---|---|---|
| `Assets/Source/Claude/Metro/metro_train.glb` (Metro Train 3D - Free 3D) | https://sketchfab.com/3d-models/metro-train-3d-free-3d-c6bfec7c75b147909a39de96045495c5 | kane_sk06 (https://sketchfab.com/kanesk06) | CC-BY-4.0 | pociąg: 2 jednostki × 2 wagony (BraxChampionsD2.Train, jeździ BraxChampionsD2Pociag) |
| `miasto_pojazdy/autobus.glb` (paczka właściciela, zaszyfrowana w `unity-city/Prywatne`) | biblioteka właściciela | KarpWorkz (MAN Lion's City 12 m) | CC-BY | autobusy na 5 z 8 stanowisk terminalu (BraxChampionsD2.Buses) |
| `Assets/Source/Claude/Library/drzewo_verdant.glb` | jak drzewa ulic rejonu | zob. `drzewo_verdant.CREDITS.md` | zob. plik | drzewa na placu, w parkingu i w ogrodzie zachodnim |

Paczka `miasto_pojazdy` jest odszyfrowywana tylko w CI (`unity-tools/mistrzowie/restore_private.py`, sam model `autobus`). Bez niej
stanowiska stoją puste, a log buildu podaje `pack miasto_pojazdy MISSING`.
