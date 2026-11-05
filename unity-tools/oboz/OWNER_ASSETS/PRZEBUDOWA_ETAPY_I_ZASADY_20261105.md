# BRAXHORN OBÓZ — zlecenie aktywne: zasady jakości i blokady odbioru
Data: 2026-11-05. Przebudowa wyłącznie obozu w Unity 6, Android mobile-first.
Status: **ROZPOCZĘTA PRACA KODOWA, BEZ ODBIORU UNITY/APK**.

## Nienaruszalne reguły właściciela
1. Postacie, siatki, szkielety, animacje, tekstury, materiały i sterowanie postaci: **ZAMROŻONE**. Nie uruchamiać nowego pakowania, zmiany limitów importu ani retargetingu dla Moniki, VORN, ZYRO, RAKK, KADE, GORR i Misia w ramach tego zlecenia. Jeżeli starsza dokumentacja opisuje zmiany tych zasobów, nie stosować ich jako części przebudowy krajobrazu.
2. **Otoczenie i wyposażenie oglądane z bliska: co najmniej rzeczywiste źródłowe 2K** (nie upscalować źródeł słabszych niż 2K; zgłosić je do oceny), oryginały źródłowe 4K przechowywać bez zmian. PBR: kolor, normalna, roughness, metallic i AO/height tam, gdzie są w źródle. Tiling UV w realnej skali, bez rozciągania małych map na całe budynki.
3. **Oddalenie**: zwykłe mipmapy i trilinear dla map 2K; selektywny mipmap streaming dla zwykłych UV0 i wspieranych shaderów. Nie deklarować streamingu map Grass001, żwiru i warstw leśnych z niestandardowymi UV/warstwami. Streaming mipów nie tworzy nowych detali i nie zastępuje LOD.
4. Współdzielić siatki i materiały; GPU instancing, culling, LOD drzew poza bliskim planem. Nie obniżać źródłowej geometrii ani tekstur dostarczonych lekkich modeli. Dla ciężkich modeli najpierw pomiar RAM/VRAM i propozycja zmian do akceptacji.
5. Nie zastępować oryginalnych modeli atrapami. Zachować fizykę worków BraxWorek, kolizje, przejścia i działające sanitariaty; żadnych zmian w mieście i osiedlu.
6. Nie wyzwalać automatycznie workflow ani APK, nie zmieniać gałęzi gpt/oboz i main. Przed długim buildem krótki preflight obecności zasobów, schematu i podpisu.

## Docelowa sceneria — lista kontrolna
- [ ] Zatwierdzony, wymiarowany rzut 2D zwartego **zaokrąglonego** obozu; zachować główną bramę przy (0,-125) przed K=1.294; ogrodzenie całkowicie zamknięte.
- [ ] Brama: właścicielski szyld BRAXHORN Training Camp w całości na prawdziwym nośniku 3D nad bramą.
- [ ] Wjazd: rondo z pełną białą linią i zieloną wyspą, autobus z poprawną skrajnią, nowe dojścia z kostki i trójstronna wiata z dostarczonego FBX.
- [ ] Posąg byka: właścicielski GLB 4x bez zmiany tekstury, UV i siatki, ustawiony na stabilnym cokole po sprawdzeniu skrajni i wymiarów w Unity.
- [ ] Ringi 1+2 oraz oktagon na wydzielonych stanowiskach, bez blokowania chodników; animacji postaci nie ruszać.
- [ ] Dwie bieżnie: czerwona pełna pętla przy **NOWYM ogrodzeniu**, linie białe; prosta dokładnie 100 m, 4 tory, START/META, oznaczenie co 10 m (pomiar po skali K).
- [ ] Trening: 24 oryginalne skórzane worki i BraxWorek z trafieniami/kołysaniem; **nie pobierać ponownie**. Weryfikować dwa warianty w posiadanych źródłach; manifest główny obecnie nazywa tylko leather_punching_bag.glb, więc nie deklarować nieznanej ścieżki drugiego modelu.
- [ ] Siłownia pod dachem, realistyczne ławki, hantle, obciążenia, drążki/poręcze, liny, opony, stanowiska; obecna geometria zastępcza nie jest finalnym importem materiałów właściciela.
- [ ] Domki dwuosobowe: dwa łóżka, wojskowe szafki, mały aneks, umywalka, kosz, wejścia i działające drzwi; brak łóżek w przekazanej paczce wymaga rozliczenia.
- [ ] Stołówka: dostarczona kuchnia/witryna/tace/sztućce/garnki, rzeczywiste meble i przejścia; nie przypisywać niezweryfikowanych stołów/krzeseł.
- [ ] Zachować i dopracować **murowany** WC/łaźnia/umywalnia, usunąć starą drewnianą; dostarczone WC, pisuary, umywalki i brodziki, działające krany/spłukiwanie/prysznice.
- [ ] Mieszana gęsta trawa 3D (Grass Medium 01/02), błoto tylko w miejscach naturalnych, zwarty las mieszany bez kępek-atrap, panorama montorfano_4k.hdr 360° za ogrodzeniem.
- [ ] Cztery murale i szyld właściciela bez kadrowania, rozciągania, zmiany liter i redukcji jakości.
- [ ] Raport licencji: repo publiczne; prywatnych GLB/BLEND nie publikować bez prawa redystrybucji.

## Inwentarz przekazania
Archiwum źródeł: BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip (345273037 bajtów; 17 GLB + 7 ZIP + 1 HDR + 5 JPEG + manifest).
Archiwum referencji: BRAXHORN_OBOZ_REFERENCJE_20261105.zip (241667206 bajtów; 3 filmy + 10 zdjęć + dokumentacja).
**Prywatne archiwa znajdują się w bibliotece właściciela; nie są jeszcze kompletnie dostępne wykonawcy pracującemu tylko na GitHub.** Nie wpisywać nieistniejących URL.

## Nowa praca na tej gałęzi
- Dodatkowy pełny biały obrys ronda i warunkowa integracja właścicielskiego posągu GLB; **nie jest jeszcze importowany**, dopóki nie trafi do prywatnej lokalizacji:
  Assets/Source/Prywatne/Oboz/OwnerAssets/BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb
- Model powiększony przez Transform; bez dotykania map UV, wierzchołków ani map osadzonych w GLB.
- **Postacie bez zmian**; tę deklarację sprawdzić diffem względem gałęzi bazowej.

## Odbiór przed publikacją / APK
1. Rzut z góry, dokładne obrysy terenu, miejsca obiektów, pieszych, bieżni, pełna skrajnia autobusu — do zatwierdzenia.
2. Dostać legalne źródła prywatnie; sprawdzić SHA-256 i brakujące modele **przed** wywołaniem Unity.
3. Unity Edit Mode/Play Mode: kompletność sceny, typy i źródła materiałów, shader compile, NavMesh/kolizje, wszystkie przejścia, obrót i rozmiar pomnika, 24 worki i sanitarne interakcje.
4. Android: prawdziwy RAM/VRAM, FPS, APK size, wygląd bliskich 2K i dalszych mipów, oryginalny podpis i aktualizacja. Nie deklarować sukcesu na podstawie testów statycznych.
5. Nienaruszenie postaci musi być udowodnione porównaniem plików i zdjęciami z aktualnego urządzenia.

## Brak automatycznego wdrożenia
To nie jest potwierdzenie gotowej przebudowy. Nie uruchomiono buildu ani nie zweryfikowano sceny na telefonie.
