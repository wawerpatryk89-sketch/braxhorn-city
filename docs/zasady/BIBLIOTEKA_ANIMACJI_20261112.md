# BIBLIOTEKA ANIMACJI: gdzie leży każda animacja (właściciel 12.11.2026 23:13: „zapisz to, żeby już nie znikło nigdy w życiu”)

Wszystkie źródła animacji gry w jednym miejscu. **Przed ściąganiem czegokolwiek nowego z Mixamo sprawdź tę listę.** Nazwy plików każdej zaszyfrowanej paczki wypisuje zadanie bez Unity: `mistrzowie-apk.yml`, `etap=biblioteka` (`unity-tools/mistrzowie/biblioteka_animacji.py`, ok. 2 min).

| Źródło | Plików | Co jest w środku |
|---|---:|---|
| `unity-city/Prywatne/ruchy_brax.tar.gz.enc` | 65 | walka Braxa, idle, kroki, siłownia (dawne dev/anims) |
| `unity-city/Prywatne/ruchy_wlasciciela.tar.gz.enc` (nina/, nyxa/, xd/) | 96 | walka, capoeira, breakdance, hip-hop, Cheering, Rallying, Entry |
| `unity-city/Prywatne/nina_1..3.tar.gz.enc` | 3 | Breakdance 1990, Breakdance Ending 2, Female Laying Pose (z postacią) |
| `unity-city/Prywatne/braxhorn-animacje-20261112/` (CMS jak braxhorn-sklep-*) | 74 + model Buldog | 12.11: bar, picie, pijany (Male Drunk Pack), siedzenie, oklaski, taniec, krupier (Cards, Shaking Dice), Goalkeeper Pass (kręgle), Throwing (rzutki), Swimming, Talking ×6, Talking On Phone, Arguing, Yelling, Disappointed, Laying ×4, **Carrying (chód z rzeczą trzymaną przed sobą: kelnerka z tacą)**; spis: `animacje_20261112/katalog.json` |
| `unity-city/Prywatne/braxhorn-animacje-20261112-2/` | 4 | 12.11 23:25: Waving ×2, Talking At Watercooler, Watering |
| `unity-city/Prywatne/braxhorn-animacje-20261112-3/` | 7 | 12.11 23:29: Carrying (1), Carrying Turn, Box Walk Arc (1), Box Turn (1), Jogging With Box, Walk To Stop, Walking (kelnerka z tacą: chód, obrót, zatrzymanie) |
| `unity-city/Prywatne/braxhorn-animacje-20261112-4/` | 4 | 12.11 23:32: Pointing ×3, Pointing Gesture (wskazywanie na stojąco) |
| Biblioteka A/T: wydanie DRAFT `global-animation-library-at-20261107` (gałąź `codex/global-animation-library-20261107`, `asset-library/animations/catalog.json`) | A 118 + T 119 | walka (komplet), golf (12), Treading Water, Swimming To Edge, Shaking Hands, Driving/Entering/Exiting Car, piłka, Speedbag, Victory/Defeat |
| Patryk, 22 animacje: gałąź `claude/osiedle-42-poprawka`, `unity-tools/claude/patryk-owner-22-manifest.json`, `unity-city/Prywatne/import-patryk-animations-20261106` | 22 | Swimming, Treading Water, Drinking, Typing, Sitting, Male/Female Sitting Pose, Angry Gesture, piłka, bramkarz, auto |
| `unity-city/Assets/Source/Mistrzowie/Brax/CarAnimations` | 3 | Entering Car, Driving, Exiting Car |

Zasady:
- Pliki „z postacią” (Mixamo z własnym buldogiem) zapisujemy bez wbudowanych tekstur (`unity-tools/mistrzowie/fbx_bez_tekstur.py`): ruch, szkielet i siatka zostają.
- W Mixamo przed pobraniem wybieraj postać **Y Bot**.
- Poza A i T nie ma znaczenia przy imporcie Humanoid; z par o tej samej nazwie bierzemy jedną wersję.
- Każda nowa paczka od razu do repo (zaszyfrowana) i do tej tabeli.
