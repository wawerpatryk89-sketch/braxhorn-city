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
- **Klub Złoty Róg** (model gym_3desain): czwarty rewir, jasny fitness klub, szef Trener.
- Fasady po stronie siłowni: wypukłości okien (normal mapy), jaśniejsze witryny, gzymsy, markizy, klimatyzatory, schody przeciwpożarowe; słońce świeci na witryny.
- Kamera walki: przy przeciwniku w promieniu 6 m kamera ustawia się w poprzek walki od otwartej strony (ulica, magazyn, klub).
- Patrole Czerwonego Rogu w dzień na ulicy.
- Obraz: rozdzielczość dopasowuje się sama (1.0–2.0× piksela, cel ~50 kl/s), słabszy blask i mgła.

## Siłownia etapami (28.10, południe)

- **Etap 1** (`src/gymfloor.js`): koniec „mokrej” podłogi z odbiciami. Matowa czarna guma 1×1 m z drobinkami (szare + zielone), w zaułku sucha popękana betonowa wylewka. Lampy wiszące oświetlają salę przez pole lamp (`gym.lampSources`).
- **Etap 2: kolory BRAXHORN** (`src/gym.js`):
  - ściany z cegły pomalowanej na grafit (`T.brick(…, { paint })`), czarny sufit i belki;
  - czarne panele do 1,3 m z zieloną listwą LED #8bff3a; zielony LED wzdłuż łuków i w niszach;
  - białe, neutralne światło główne;
  - ring: grafitowa mata z odznaką, narożniki zielone i czarne, górna lina zielona;
  - worki czarne z zielonymi pasami, jeden zielony, jeden biały; czarna mata pod workami z zieloną ramką i napisem;
  - pas sztucznej trawy z liniami co 2 m i napisem BRAXHORN;
  - logo `wide_a` na prawej ścianie i na podłodze; kartony BRAXHORN NUTRITION; czarne szafki z zielonymi etykietami; odżywki na półkach.
  - Wyjście: czarne drzwi z zieloną ramką LED, neon BRAXHORN GYM nad drzwiami, zielony neon OPEN 24/7, byk sprayem na ścianie. Sąsiednie budynki w zaułku zostały z czerwonej cegły.
- **Etap 3: jaśniej i premium** (po nagraniu użytkownika: za ciemno, neony za mocne, postacie „świecą”):
  - jasna malowana cegła (równa, bez plam) z miękkim „podświetleniem ścian” (emissive 0.16); czarne obramowania łuków; zielony LED o połowę słabszy;
  - neon i logo na ścianie na czarnych tablicach, nisze z półkami oświetlone na biało;
  - przyczyna świecących postaci: odbicie otoczenia (envMapIntensity) 0.9–1.0 przy 0.3 na ścianach, a Brax/Nyxa miały metalness 0.6. Teraz 0.35 i metalness 0.15; ludzie z Mixamo też 0.35 (`people.mats`);
  - cieplejsze światło ogólne, słabsze reflektory, nasycenie 1.2 w siłowni.

## Kierunek 28.10 (użytkownik: „zrób siłownię, żeby dobrze wyglądała i nie zamulała”)

- Pełne „Wysoka” jak na komputerze zamulało na telefonie. Tymczasowo na telefonie: 1.5× piksela + MSAA 4×, 4 reflektory, 1 światło punktowe, cienie 1024.
- Propozycja: zostać w przeglądarce, ale robić jak gry mobilne. Walki w 2,5D na małych arenach (podziemia, ringi) z kamerą z boku i upieczonym światłem, siłownia jako baza z upieczonym światłem, miasto mniejsze i prostsze (tylko przejścia). Docelowy poziom to porządna gra niezależna.
- Unity/Godot odradzone przy tym celu: aplikacja zamiast linku, na iPhone'a potrzebny Mac i konto Apple, praca w edytorze po stronie użytkownika.
- Plan upieczenia światła w siłowni: offline (harness), na wierzchołkach po teselacji co około 0,5 m; promienie do lamp i AO na prostopadłościanach z BVH; zapis Float16 w pliku; statyczne materiały na MeshBasic × kolor wierzchołka; na żywo tylko postacie (pole lamp, hemi, key) i cień gracza (nakładka ShadowMaterial). Zaułek zostaje na żywo (dzień i noc).

## Upieczone światło w siłowni (28.10, 14:00)

- `src/bake.js`: statyczne siatki siłowni cięte siatką co 0,5 m (`tessellate`, równo w osiach świata, bez szpar). Światło liczone raz offline: lampy wiszące z miękkim cieniem, kinkiety, nisze, zielone listwy i łuki, neon, okna, do tego AO. Źródła światła są w `gym.bakeLights` (`BL(...)` w gym.js).
- Pieczenie: `node tools/play2.mjs dev/tools/bake_gym.json 915 412 1` (lokalny serwer na 8765) zapisuje `assets/bake/gym1.bin.gz` (Float16 RGB na wierzchołek, ok. 400 KB). Po KAŻDEJ zmianie geometrii siłowni trzeba upiec od nowa i podbić nazwę pliku (gym2…), bo pliki mają cache 1 dzień. Przy niezgodnej liczbie wierzchołków gra wraca do światła na żywo (ostrzeżenie w konsoli).
- W grze: MeshBasicMaterial (tekstura × upieczone światło × `BAKE_GAIN` 1.75), siatki bez cieni na żywo; nakładka ShadowMaterial na podłodze i ringu daje cienie postaci. Zaułek (`userData.outside`, zewnętrzna połowa ściany frontowej) zostaje na żywo, bo ma dzień i noc.
- Strefy: w hali ulica, jej auta i przechodnie nie są rysowane (pojawiają się 2,5 m przed drzwiami).
- W siłowni widać ok. 160 tys. trójkątów i 190 wywołań rysowania.

## Jakość na telefonie (28.10, 13:30)

- „Wysoka” na telefonie = to samo co na komputerze: wygładzanie krawędzi MSAA 4×, cienie 2048, 6 reflektorów, 2 światła punktowe, pełny blask, do 2× piksela. Na telefonach wyłączone są tylko lustro podłogi i smugi światła. „Średnia”: MSAA 2×, do 1.5× piksela. Dynamiczna rozdzielczość dalej obniża piksel, gdy telefon nie wyrabia.
- Wcześniej (tryb płynny po „zamula”) telefon nie miał MSAA, przez co migotały krawędzie i cienkie linie. Użytkownik chce jakości jak w grach mobilnych, więc nie wolno tego po cichu obcinać.
- Migotanie: cień przesuwa się o całe teksele (`keyM`/snap), większe cegły (20 rzędów) i słabsze normal mapy na ścianach i podłodze, neon bez celowego mrugania, grubsze listwy LED.
- Postacie matowe: bez ORM (połysk robił z czarnych ubrań srebrne), metalness 0, roughness 0.8.
- Napis WYJŚCIE: duża zielona tablica z białym tekstem, bez poświaty.

## Tryb zwiedzania (28.10, 12:15)

- Na razie włączony od startu, przełącznik jest w pauzie (Zwiedzanie / Walki), zapamiętywany w `store('explore')`.
- Nie ma jeżdżących aut, zombie, patroli, szefów ulicy, gangów na rewirach ani sparingpartnerki. Gracz nie traci zdrowia.
- Ulica otwarta: ściany kordonu, wiaduktu i strony dalekiej (`story` w `street.js`) mają `top = -10`. Można iść do końca zbudowanej ulicy w obie strony (x −300…330), pod wiaduktem i w zachodnią przecznicę. Nowe ściany `citywall` idą wzdłuż rzędów budynków z `outer.js`.

## Plan użytkownika (kolejność)

1. Dopracować wygląd miasta (ma wyglądać fajnie, nie musi być idealnie) — użytkownik chodzi w trybie zwiedzania i mówi, co poprawić.
2. Potem przygoda: walki na ringach albo w podziemiach, do których trzeba zejść; walka tam w 2,5D, żeby łatwiej się biło. Pomysł do ustalenia razem.

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

## Do zrobienia

- Kolejne rewiry ze skanów: skatepark Southbank, Seven Stars Brick Lane, Crouch End (trzeba wyciąć czarną „skorupę” skanów `trimScan`).

## Do zrobienia (wersja 2, stare)

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
