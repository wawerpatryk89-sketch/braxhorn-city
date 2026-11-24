# Silnik kładów — źródła (MIASTO MISTRZÓW, 24.11.2026)

Prawdziwe nagrania kładu z BigSoundBank (Joseph Sardin), licencja **CC0** (domena publiczna; użycie w grze, przeróbki
i rozpowszechnianie bez opłat i bez wymogu podpisu — bigsoundbank.com, FAQ).

| plik | źródło | odcinek | co to |
|---|---|---|---|
| `klad_silnik_wolne.ogg` | #711 „Quad - stopped” (https://bigsoundbank.com/quad-stopped-s0711.html, OGG 48 kHz mono) | 15,5–22,0 s | wolne obroty (zapłon ~12 Hz, ~1450 obr/min jednocylindrowego) |
| `klad_silnik_jazda.ogg` | #713 „Quad - way” (https://bigsoundbank.com/quad-way-s0713.html, OGG 48 kHz mono) | 55,5–61,5 s | jazda (zapłon ~36 Hz, ~4300 obr/min) |

Obróbka (scratchpad, bez zmian brzmienia): 22 050 Hz mono, ogon pętli przenikany w jej początek (0,3 s, równa moc),
głośność wyrównana (RMS 0,16), OGG Vorbis q4. W grze (`BraxChampionsKlad.Silnik`) obie pętle grają naraz: wolne cichną,
jazda rośnie i podnosi ton z obrotami (CVT: najpierw gaz, potem prędkość).
