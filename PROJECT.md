# BRAXHORN CITY: notatki projektu

Testowa bijatyka 3D (chodzona) w przeglądarce, na telefon (poziomo, pełny ekran).
Osobny projekt, **niezwiązany z grą braxhorn-arena**.

- Hosting: Vercel, projekt `braxhorn-city`, adres **https://braxhorn-city.vercel.app**. Każdy push na `main` publikuje nową wersję.
- Silnik: three.js r160 (bez frameworka), bundel esbuild → `public/game.js`.
- Status: **test, nie na sprzedaż**. Część materiałów (np. auta, skany, logotypy UFC/Monster na oktagonie) ma licencje, których nie sprawdzaliśmy.

## Struktura repo

| Katalog | Zawartość |
|---|---|
| `public/` | gotowa strona (to serwuje Vercel): `index.html`, `game.js`, `manifest.webmanifest`, ikony, `assets/` |
| `public/assets/chars/` | postacie ze szkieletem i animacjami (`*.glb.gz`) + `clips.json` (root motion, długość kroku itp.) |
| `public/assets/tex/` | tekstury postaci (`_color`, `_normal`, `_orm`, WebP) |
| `public/assets/*.glb` | skany ulicy/krypty, auta, latarnie, pałka (KTX2 + meshopt, zrobione `gltfpack`) |
| `public/assets/logo/` | loga BRAXHORN (szerokie, okrągłe, plakat z minotaurem, znak byka) |
| `dev/src/` | kod gry (moduły ES): `main.js` (rozgrywka), `fighter.js`, `street.js` (ulica, wiadukt, pociąg), `outer.js` (dalsze miasto), `traffic.js`, `crypt.js`, `villa.js`, `meshcol.js` (kolizje z modelu), `people.js` (przechodnie Mixamo), `env.js` (dzień/noc + „lamp field” z reflektorami), `grade.js` (filtr obrazu), `sound.js` (syntezowane dźwięki i muzyka), `ui.js`, `story.js`, `gym.js`, `template.html` |
| `dev/tools/` | potok konwersji: `pipeline.js` + `convert.html` (FBX → retarget → GLB w przeglądarce, uruchamiane przez `run_js.mjs`/Playwright), `build_web.py` (buduje `dist/`), `pack.sh` (gltfpack), `play2.mjs` (testy w Playwright) |
| `dev/anims/` | oryginalne animacje Mixamo (FBX): `brax/`, `extra/`, `extra2/`, `gym/` (Clean And Jerk, Snatch, Sumo High Pull, Back Squat, Reloading) |
| `dev/logos/` | oryginalne PNG logotypów |

## Co nowego (28.10)

- Ulica ciągnie się dalej w obie strony (proceduralne kamienice z oświetlonymi oknami w nocy, wieżowce na horyzoncie), zamiast doklejonych modeli miasta, które stały na jezdni.
- Wiadukt kolejowy z cegły (łuk nad ulicą, ślepe łuki, drzwi KRYPTA) zamiast skanu mostu, który sypał „odłamkami” w powietrzu. Po wiadukcie co jakiś czas jedzie pociąg (z dźwiękiem).
- Światła aut: prawdziwe pozycje reflektorów i tylnych lamp z modeli, małe flary zależne od kąta patrzenia, reflektor oświetla jezdnię i ludzi (stożek w „lamp field”), czerwone światła z tyłu (mocniejsze przy hamowaniu).
- Niebo: gwiazdy jako punkty, chmury z tekstury (koniec z kreskami/artefaktami na Androidzie), noc mniej niebieska.
- HUD premium: karta gracza z paskiem życia (z „duchem” obrażeń), kasą i zegarem; pasek bossa na środku tylko w walce; jeden przycisk pauzy. Wszystkie ustawienia (kamera, jakość, dźwięk, czas, pełny ekran, ruchy) są w menu pauzy. Gra pauzuje się sama po wyjściu z karty.
- Końcowy filtr obrazu: tonemapping + lekka korekcja kolorów, winieta, ziarno, czerwony brzeg ekranu przy obrażeniach.
- Krypta: wycięte sklepienie i kolumny w środku klatki (kamera widzi walkę z góry), kamera ustawia się w poprzek walki.
- Willa: spokojniejsze światło w nocy.
- Logo z przezroczystym tłem (`wide_a.webp`) na ekranie tytułowym, pauzie i w loaderze; na iPhonie podpowiedź „Do ekranu początkowego” (jedyny sposób na pełny ekran w Safari).

## Co nowego (28.10, rano)

- **Dźwięk naprawiony**: pogłos (ConvolverNode) miał próbkowanie 44,1 kHz, a telefony grają w 48 kHz → przeglądarka rzucała błąd i cały dźwięk się wyłączał. Teraz pogłos jest przeliczany do częstotliwości telefonu.
- **Mapa miasta**: przy swoim aucie (ulica, willa, wejście do rewiru) akcja **JEDŹ** otwiera mapę: Ulica, Willa, **Tunel Futatsugoya**, **Zaułek Freemana**.
- **Rewiry**: trzy fale gangu Czerwonego Rogu (ludzie z Mixamo bijący się animacjami Braxa), potem szef rewiru (Szczur w tunelu, Szpachla z pałką w zaułku). Za zdobycie nagroda; kolejne wizyty: gang wraca, mniejsza nagroda.
- Skany tunelu i zaułka spakowane z `-slb` (bez szpar po upraszczaniu siatki).
- **Stary magazyn** (abandoned warehouse): trzeci rewir, szef Księgowy. Materiały spec-gloss przerobione na metal-rough skryptem `dev/tools/sg2mr.py` (three.js r160 nie czyta spec-gloss).
- Obraz: rozdzielczość dopasowuje się sama (1.0–2.0× piksela, cel ~50 kl/s), słabszy blask i mgła.

## Fabuła (story.js)

1. **Siłownia BRAXHORN GYM**: sparing z partnerem na ringu, wyjście drzwiami WYJŚCIE na zaułek.
2. **Ulica**: dzień/noc (12 min), ruch uliczny, w nocy zombie („Zarażeni”), walizki z kasą (zanieś do siłowni → $500), kolczasta pałka w zaułku.
   Bossowie po kolei: **Pazur** (tygrysica), **Pręga** (tygrysica 2), **Róża** (różowa minotaurka), **Kafar** (osiłek z pałką, pilnuje drzwi do Krypty pod wiaduktem).
3. **Krypta** (skan podziemi + oktagon z logo BRAXHORN): **Asterion** (minotaur, szef gangu Czerwonego Rogu), potem **Rogaty** (demon). Koniec: świt nad miastem.

Grywalne postacie: **Brax** (siła, specjal: kolano/łokieć) i **Nyxa** (szybkość, specjal: martelo/kopnięcie z obrotu).

## Materiały od autora (oryginały poza repo, za duże na GitHuba)

- Postacie Tripo (FBX ze szkieletem): Brax, Nyxa, Minotaur (Asterion), Tygrysica ×2, Różowa minotaurka, Osiłek z pałką, Zombie, Demon. Spider-Gwen i Tifa nie mają riga.
- Kolczasta pałka (bez riga → broń do podniesienia).
- 22 postacie Mixamo (Arissa, Brute, Remy, The Boss, Big Vegas, Kachujin, Lola, Ch06–Ch47…): planowani przechodnie, ćwiczący na siłowni, gangsterzy.
- Skany (Sketchfab, fotogrametria): rowhouses Baltimore, small fire at local business, rick roll bridge (wiadukt), Lincoln's Inn undercroft (krypta), street_1, trump village, freeman alley NYC, futatsugoya tunnel, abandoned warehouse, crouch end station, southbank skatepark, seven stars brick lane, St Pancras crypt, smoking room, billiards room, estances casa gassia, St Bartholomew, hotel Ukraine (celowo pominięty), gym 3desain.
- Auta GLB: Audi S4, Audi R8, Lamborghini Revuelto, Porsche 918, Mustang 1996, Dodge Challenger, Lamborghini Diablo (policja), Marussia (policja), ŻUK, GMC Sierra. (Stare rdzewiejące auta i VW policja były w RAR, nieużyte.)
- Oktagon UFC, drewniany stół, ulica (road avenue), latarnia, słupy, low-poly ludzie, dziewczyna (bez riga).
- Loga BRAXHORN NUTRITION (szerokie, okrągłe, plakat z minotaurem, minimalny znak byka).

## Do zrobienia (wersja 2)

- Przechodnie z Mixamo (chodzą po chodnikach, uciekają przed bójką) + ćwiczący na siłowni (animacje z `dev/anims/gym`) + gangsterzy w dzień.
- Willa głównej postaci (skany: smoking room + billiards room, garaż z Lamborghini), sklep z ulepszeniami za pieniądze.
- Mapa miasta i rewiry na porachunki: tunel (futatsugoya), opuszczony magazyn, nowojorski zaułek (freeman alley).
- Animacje Mixamo dla zombie (Zombie Walk/Attack/Scream/Dying) i demona (Mutant Roaring/Swiping/Jump Attack/Punch/Dying).
- Prawdziwa muzyka (MP3 bez praw autorskich) zamiast syntezy.

## Jak budować (w sesji z Claude)

```
cd /home/claude/work && python3 tools/build_web.py          # → dist/
cp -r dist/* braxhorn-city/public/ && git push                # Vercel publikuje sam
```
Postacie: `node tools/run_js.mjs convert.html <skrypt eksportu>` (Playwright + three.js w przeglądarce).
