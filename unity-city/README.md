# BRAXHORN CITY -> Unity: migracja bez zmiany wyglądu

## Cel i zakres
Przenieść istniejącą grę Braxhorn City, nie Braxhorn Arena, z Three.js r160 do Unity.
Pierwsza lokalizacja: oryginalna siłownia BRAXHORN + zaułek i wyjście na ulicę.
Zachować geometrię, kolory, tekstury, pozycje, postacie i własne animacje.
Bez zastępczych brył i zmiany projektu wizualnego bez zgody operatora.

## Źródła prawdy
- dev/src/gym.js — siłownia, ring, worki, łuki, wyjście.
- dev/src/gymfloor.js — matowa gumowa podłoga, beton, trawa.
- dev/src/bake.js + public/assets/bake/gym4.bin.gz — upieczone światło.
- dev/src/world.js — oryginalne kolizje box/circle w metrach.
- dev/src/district.js — spawn (-1.5,0,2.8), wejścia.
- dev/src/fighter.js — oryginalny system ruchu, skala 0.024, chód 2 m/s, bieg 5 m/s.
- public/assets/chars/brax.glb.gz, nyxa.glb.gz, clips.json — szkielety i klipy.
- public/assets/tex/ — tekstury postaci.

## Etap A: oryginalna geometria (odseparowany eksport)
Narzędzie uruchamia istniejącą grę i eksportuje jej rzeczywisty obiekt scene/gym
z materiałami i baked vertex colours do braxhorn-original-gym.glb.
To NIE jest odtworzenie siłowni za pomocą klocków.

Skrypty: dev/tools/unity-gym-export.html, dev/tools/export-gym-for-unity.mjs.
CI: .github/workflows/city-unity-gym-export.yml.

Pliki wyniku (GitHub Actions artifact, NIE publikacja):
- braxhorn-original-gym.glb — rzeczywiste siatki;
- gym-collisions.json — prawdziwe dane box/circle collision;
- gym-manifest.json — inwentarz, skala, spawn, wyjścia i ostrzeżenia;
- gym-web-source.png — zrzut obrazu źródłowego do porównania;
- export-browser.log — zapis ostrzeżeń.

Eksport nie jest uznany za udany port Unity bez testu wizualnego po imporcie.

## Etap B: Unity — warunki migracji
- Unity 6000.0.58f2 oraz importer glTFast (com.unity.cloud.gltfast).
- Zachować dokładne pozycje i transformacje; odtworzyć collidery i drzwi.
- Oryginalny Brax i Nyxa z oryginalnymi klipami, bez substytutów.
- Kamera nie może obracać się samoczynnie.
- Dzień; matowe materiały, bez bloom/halo/świecących postaci.
- Telefon poziomo, joystick, chód/bieg, kamera ręcznie.
- Test na oddzielnej wersji. Żadnych automatycznych zmian na main.

## Etap C: miasto
Po zatwierdzeniu siłowni przenieść ulicę, sklepy, kamienice, nabrzeże,
podwórko, wiadukt, podziemny ring, tunel i kolejne plansze.
Nie używać modeli odrzuconych w ZLECENIE.md.

## Kryteria odbioru
1. Siłownia wygląda jak źródło Three.js na zestawionych ujęciach.
2. Rzeczywiste modele i animacje Braxa/Nyxy; nie T-pose.
3. Działa wejście/wyjście do zaułka; kolizje nie przepuszczają przez ściany.
4. Joystick i kamera działają dotykowo na iPhone i Android.
5. Jest film lub zrzut z Unity i test mobilny; sam zielony build to NIE PASS.

Nie zmieniać Vercel, main, ani Braxhorn Arena bez wyraźnego polecenia.
