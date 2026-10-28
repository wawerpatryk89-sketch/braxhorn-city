# ZLECENIE dla nowego czatu: BRAXHORN CITY

Przeczytaj całość, zanim cokolwiek zrobisz. Pełna historia projektu jest w `PROJECT.md`, tutaj są tylko rzeczy potrzebne do pracy.

## 1. Czym jest projekt

- **BRAXHORN CITY:** bijatyka uliczna 3D na telefon (poziomo, iPhone/Safari i Android/Chrome), w przeglądarce. Link do gry: https://braxhorn-city.vercel.app
- Gra służy do testu marki BRAXHORN użytkownika i nie jest na sprzedaż. Docelowo ma się stać aplikacją na telefon (Android przez opakowanie gry przeglądarkowej, na iPhone'a potrzebny Mac).
- **Bohaterowie:** Brax i Nyxa, oboje na czarno.
- **Kolory marki:** czerń, neonowa zieleń #8bff3a i stal/biel.
- **Logo:** `public/assets/logo/` (wide, wide_a z przezroczystością, badge, mark, poster).

## 2. Zasady użytkownika (twarde, łamane wcześniej, przez co stracił czas)

1. **Pisz po polsku, krótko.** Na pytanie najpierw odpowiedz, potem działaj.
2. **Nic nie świeci.** Żadnego bloomu, halo, neonów, LED-ów ani emissive. Wyjątek: delikatne światła aut i latarnie. Ozdoby, które się żarzą, użytkownik odbiera jako tandetę, która psuje wygląd i zamula.
3. **Kamera nigdy nie obraca się sama.** Obraca ją wyłącznie gracz, a kamera tylko podąża za postacią.
4. **Na razie tylko dzień** (`DAY_ONLY` w `dev/src/main.js`).
5. **Jakość na telefonie ma być dobra i nie może zamulać.** Nie obcinaj jej po cichu. Jakość ma pochodzić z upieczonego światła i dobrych modeli, nie z efektów liczonych na żywo.
6. **Nie mów „zrobione”, jeśli tego nie widziałeś.** Przed każdym wdrożeniem zrób zrzuty w harnessie (915×412, dotyk) i obejrzyj je. Użytkownik testuje na telefonie i od razu widzi, czy jest efekt.
7. **Nie wycinaj kawałków skanów i nie zamykaj ich niewidzialnymi ścianami jako „miejsc”.** Wyszły z tego puste korytarze („biegniesz 3 m w prawo i w lewo”). Każde miejsce ma mieć cel i coś do roboty.
8. **Nie twórz rzeczy bez sensu,** jak willa, do której nie da się wejść, albo wrak auta, którym się jeździ.
9. **Pracuj etapami.** Jeden etap ma być dopracowany, zanim zaczniesz następny. Po każdym etapie wdrożenie i krótki raport z obrazkiem.

## 3. Stan na 28.10, godz. 15:15

- **Siłownia (baza):**
  - jasna cegła, czarne panele i łuki, matowe zielone akcenty, ring, worki, trawa;
  - upieczone światło (`dev/src/bake.js`, plik `public/assets/bake/gym3.bin.gz`);
  - postacie matowe, w naturalnych kolorach;
  - bez bloomu i bez świecenia.
  Użytkownik uznał ją za w miarę dobrą.
- **Ulica:** stara, prosta ulica z budynkami generowanymi w kodzie, matowa. Dalsze odcinki są pustymi wypełniaczami i trzeba ją zastąpić planszą 1.
- **Miejsca z mapy:** tunel, zaułek, magazyn, klub i willa są oceniane jako ZŁE (puste, małe, bez wyjść). Do usunięcia lub zastąpienia.
- **Tryb zwiedzania:** domyślnie włączony (bez aut, wrogów i obrażeń), przełącznik w pauzie. W pauzie jest też „Mapa”, żeby nigdy nie utknąć.
- **Wersja:** numer widać na ekranie startowym i w pauzie („Wersja: dd.mm gg:mm”). Dzięki temu wiadomo, czy telefon ma najnowszą.

## 4. Co robimy teraz: PLANSZA 1 „Dzielnica BRAXHORN”

Rzut: `dev/docs/plansza1_rzut.png`. Użytkownik jeszcze go nie zaakceptował, więc pokaż go i zapytaj jednym zdaniem. Jeśli powie „rób”, budujesz.

- **Zachód:** ocean z nabrzeżem i molo (walizki, dostawy). Wody i molo nie ma w modelach, trzeba je zrobić samemu, matowo.
- **Środek:** główna ulica z kamienicami i sklepami, przy niej siłownia jako baza.
- **Południe:** blokowisko z podwórkiem (zadania, ludzie, walizki).
- **Wschód:** wiadukt kolejowy z łukami. W łuku zejście do **podziemnego klubu walk**: podziemie ze sklepieniami i octagon w środku. Walki w 2,5D z kamerą z boku.
- **Koniec:** tunel, czyli wyjazd na planszę 2 (osiedle domków), otwarty po pokonaniu szefa. Potem plansza 3: las z bunkrami.
- Wszystko w zamkniętej, zwartej planszy z upieczonym światłem, bez hektarów pustych ulic.

Proponowana kolejność etapów: (1) układ planszy, ulica, blokowisko, kamienice i nabrzeże, upieczone światło. (2) Wiadukt i podziemny klub z octagonem oraz walki 2,5D. (3) Zadania: walizki, dostawy, wejścia do walk. (4) Tunel i przejście do planszy 2.

## 5. Materiały (wszystko jest w repo)

Repo: `wawerpatryk89-sketch/braxhorn-city` (dodaj przez `add_repo`, `access: push`).

- `public/` to gotowa gra (to, co serwuje Vercel), z przetworzonymi assetami w `public/assets`:
  - postacie `chars/*.glb.gz` i tekstury `tex/`;
  - ludzie Mixamo `people/` (w tym `fight.json.gz`);
  - skany i auta `*.glb`;
  - logo i upieczone światło `bake/`.
- `dev/src/`: kod gry (three.js r160). `main.js` to pętla i logika, `gym.js` siłownia, `gymfloor.js`, `bake.js`, `street.js`, `traffic.js`, `people.js`, `places.js`, `env.js`, `ui.js`, `template.html`.
- `dev/three/`: three.js r160 (build + examples).
- `dev/models/`: modele wybrane do planszy 1 (spakowane gltfpack, małe):
  - `blokowisko_city7.glb`: blokowisko z kamienicami, model do gier;
  - `kamienice_city_street.glb`: fronty kamienic do gier;
  - `wiadukt_bridge.glb`: skan wiaduktu z łukami. Ma czarną obudowę skanu do wycięcia;
  - `podziemie_undercroft.glb`: podziemie ze sklepieniami. Też ma obudowę do wycięcia;
  - `octagon.glb`: klatka. Mata ma cudze logo (UFC, Monster…) i trzeba ją przemalować na BRAXHORN;
  - tunel jest w `public/assets/tunnel.glb`.
- `dev/docs/`:
  - `przeglad_modeli.jpg`: wszystkie modele użytkownika z oceną zostaje/odpada;
  - `plansza1_rzut.png`;
  - `stan_28-10_15h.jpg`.
- **Odrzucone modele** (nie używać): willa, zaułek Freemana, magazyn, klub fitness, skan ulicy NY street_1, Brick Lane, skatepark, Crouch End, Baltimore, spalony sklep, Trump Village, after the rain, Hotel Ukraina (prawdziwe zniszczenia wojenne).
- Oryginały modeli były tylko w załącznikach starego czatu. Jeśli potrzebny jest pełniejszy model, poproś użytkownika o ponowne wysłanie.

## 6. Jak pracować (sprawdzone)

- **Build:** `python3 dev/tools/build_quick.py` buduje `public/game.js` i `public/index.html` z numerem wersji. Z `--dev` buduje bez minifikacji. esbuild jest w `/opt/npm-tools/node_modules/esbuild`.
- **Lokalny serwer:** `python3 -m http.server 8765 --directory <repo>` uruchom w tle, gra jest pod `/public/index.html`.
- **Harness zrzutów:** `BX_OUT=/tmp/shots node dev/tools/play2.mjs kroki.json 915 412 1`. W JSON-ie są kroki: wait, pause, eval, click, shot, sim. W grze `window.__mg` udostępnia `tp(x,z)`, `cam`, `sim(s)`, `travel(dest)`, `setHour`, `act`, `bakeGym()` i inne.
- **Podgląd całego modelu:** `node dev/tools/run_shot2.mjs <url> <prefix> '<views>'` (strona `dev/tools/shot2.html`).
- **Pieczenie światła:**
  - `__mg.bakeGym()` liczy światło i zwraca plik. Ścieżka zapisu w `main.js` (`bakeGym`) wskazuje na stary katalog `/home/claude/work/assets/bake/…`, więc zmień ją na `public/assets/bake/`.
  - Po każdej zmianie geometrii trzeba upiec od nowa i podnieść numer pliku (gym4…), bo assety mają cache 1 dzień.
  - Schemat: statyczne siatki cięte co 0,5 m, światło i AO na wierzchołkach, MeshBasic × kolor wierzchołka. Na żywo oświetlane są tylko postacie.
- **Pakowanie modeli:** gltfpack z `-cc -tc` i dla skanów `-si 0.2…0.3 -slb`. Binarki nie ma w repo, w starym środowisku leżała w `/home/claude/work/bin/gltfpack`. Jeśli jej nie ma, pobierz z GitHub releases (zeux/meshoptimizer).
- **Wdrożenie:**
  - commit i push na `main`, Vercel publikuje sam (projekt `prj_qeJX3ynFW1b21OYRsZlXKChzSRDh`, team `team_z2Zm14RoeGXMXDd5Dorzotxj`);
  - autor commitów: `-c user.name="wawerpatryk89-sketch" -c user.email="wawerpatryk89@gmail.com"`;
  - stan builda sprawdzaj przez Vercel MCP `list_deployments`.
- **Telefony** mają w `main.js` ustawienia jakości: High = 1.75× piksela + MSAA 4×, bez bloomu.

## 7. Pierwszy krok w nowym czacie

1. Dodaj repo, przeczytaj `PROJECT.md` (sekcje od „Siłownia etapami” w dół) i ten plik.
2. Pokaż użytkownikowi `dev/docs/plansza1_rzut.png` i zapytaj jednym zdaniem, czy budować według niego.
3. Po zgodzie zbuduj etap 1 planszy 1, zrób zrzuty, obejrzyj je i dopiero wtedy wdróż. Wyślij obrazek i 3–5 zdań, co się zmieniło.
