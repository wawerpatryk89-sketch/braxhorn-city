# BRAXHORN CITY — R2 / MASTER PLAN DRÓG I CHODNIKÓW V1

**Status: PROJEKT TECHNICZNY DO WDROŻENIA W ODDZIELNYM TEST. ŻADNA NOWA DROGA NIE JEST JESZCZE ZBUDOWANA.**

Gałąź robót drogowych: codex/estate-r2-roads-expansion-20261030 (powstała z 80b4004f078269e59c598ada5e0920406dddc8ec). Jedyny silnik i uruchomienie: **Unity 6 (6000.0.58f2)**. Scena **BraxhornEstate**.

## Fakty i decyzja

- MASTER V3 przewiduje w R2: 8 dotychczasowych domów, willę i dom kontaktowy; kasyno, klub, komisariat, szpital, garaże, ring, oktagon, park i boisko oraz przyszłą szkołę, aptekę, sklep odzieżowy i punkt odżywek. Zarezerwować trasowanie dla **5 misji głównych + 6 pobocznych**; nie uruchamiać teraz misji.
- Aktualny plan źródłowy: około **228 × 201 m = 4,58 ha**, x −114..114, z −102..99 (nie oznacza zatwierdzonej mapy Unity).
- Proponowana rama R2: **450 × 350 m = 15,75 ha**, x −225..225, z −175..175, około **3,4×** więcej. To propozycja, nie zmierzone granice Unity; dopasuj po audycie Unity, zachowując czytelną iOS wydajność.
- Master wymaga przejść między pobliskimi punktami w ok. 20–60 s. Większy teren nie może być pusty i wymuszać niekończących się marszów.
- Etap 1: **kompletna struktura dróg, chodników, dojść, podjazdów, zielonych pasów i terenu**. Etap 2: remont wszystkich istniejących domów. Etap 3: nowe parcele i zabudowa. **Nie przestawiaj żadnego istniejącego budynku.**

## Obecne osie, których nie wolno zniszczyć

Poniższe dane są odczytane z źródłowego rozkładu dróg, nie są potwierdzonymi pozycjami renderu Unity:

- SRC-01: oś X=0, Z=−24..+98, jezdnia ok. 9 m — do zachowania, odnowienia i połączenia z drogą obwodową.
- SRC-02: oś Z=−19, X=−45,5..+45,5, ok. 8 m szerokości — do przedłużenia na zewnątrz.
- SRC-03: oś Z=+39, X=−45,5..+45,5, ok. 5 m — do przedłużenia na zewnątrz, bez bezmyślnego poszerzania między posesjami.
- SRC-04 i SRC-05: boczne drogi X≈−43 i +43, Z≈−16,5..+36,5 — pozostają.
- Brama powrotna do miasta w okolicy źródłowego (0,+94) — **nie zasłaniać**.

W obrębie starej geometrii wykorzystuj istniejące drogi jako bazę; nowe odcinki dodawaj poza ich końcami, wyrównując wysokości i UV. **Scena Unity po imporcie odwraca znak źródłowego X** — geometrię finalną najpierw zweryfikować w Unity.

## Projekt nowych jezdni — budować całość, nie tylko kawałek

**RD-01 — Droga obwodowa R2.** Zamknięta, dwukierunkowa pętla po osi: (−174,−126) → (+174,−126) → (+174,+126) → (−174,+126) → początek. Jezdnia 7,0 m, chodniki 2,3 m obustronnie, zielone obrzeża, łuki narożne ~9 m. Nie betonować całego pasa działek. Obwodnica otacza obecne obiekty, pozostawia rezerwy budowlane na zewnątrz i pomiędzy domami.

**RD-02 — Poprzeczna ulica północna.** Przedłuż obecną ulicę wzdłuż Z≈−19 od X≈−174 do +174, przechodząc przez istniejący, niezmieniony odcinek SRC-02. Na nowych odcinkach 7,0 m jezdni i chodniki obustronnie. Obsługuje dojścia do willi, domów, kasyna i komisariatu.

**RD-03 — Poprzeczna ulica południowa.** Przedłuż istniejącą ulicę Z≈+39 na X≈−174..+174. Nowe odcinki 6,4 m i chodniki obustronne, z dopasowaniem przejścia do starej ulicy 5 m. Dostęp do domów, klubu, szpitala i dwóch hal sportowych, bez przecinania budynków.

**RD-04-W i RD-04-E — Dwie aleje usługowe.** Biegną wzdłuż X≈−130 i X≈+130 od Z≈−126 do +126. Jezdnia 6,4 m i chodniki po obu stronach, około 2,2 m. Wariant zachodni obsługuje okolice kasyna, klubu i boksu; wschodni komisariat, szpital i oktagon. Dystans od fasad oraz każda kolizja muszą być sprawdzone w Unity.

**RD-05 — Dojazd od bramy miasta.** Przedłuż istniejącą oś główną od Z≈+98 do nowej obwodnicy Z≈+126, bez zmiany bramy przejścia oraz sterowania Braxa.

**SRC-01..05 — Odnów w CAŁOŚCI.** Również istniejące jezdnie, pobocza i chodniki mają uzyskać końcowy materiał premium, prawidłowe krawężniki, tekstury PBR, UV, kolizje i zejścia do wejść. Nie zostawiaj starego plastiku na połowie trasy.

Odcinki to **proponowane polilinie w źródłowym układzie**, nie gotowe współrzędne nawigacyjne Unity. Rysunek tras i wszystkie założenia geometryczne w pliku **docs/r2-road-network-design-v1.json**.

## Chodniki i place / ruch pieszy

- Przy RD-01 do RD-04 chodnik **po obu stronach**, ciągły, min. 2,2–2,3 m; przy wejściach usługowych i publicznych lokalne poszerzenie do ~2,5–3 m. Przy istniejących ulicach dopasować do realnych furtek zamiast przesuwać dom.
- Każdy z 8 domów: nieprzerwane dojście od jezdni do rzeczywistego wejścia. Wymagane chodniki pod dom kontaktowy, willę, garaże, tarasy oraz oba domy remontowane przez drugiego wykonawcę.
- Park/boisko: promenada piesza przy północnej krawędzi parku, piesze połączenia do boiska i przyszłej szkoły. **Nie przeciąć boiska drogą** ani nie zakryć istniejących drzew.
- Kasyno, klub, komisariat, szpital, sala bokserska i oktagon: oddzielne dojścia, odpowiednie rampy, przejścia i oświetlenie. Prawdziwe wejścia i drzwi muszą pozostać dostępne.
- Skrzyżowania: geometria płynna, narożniki, krawężniki, zatoki odwodnieniowe i czytelne przejścia, bez z-fighting i wiszących fragmentów jezdni.
- Rezerwa pod przyszłą szkołę, sklepy, dzielnicę mieszkalną premium, duży park, place pojedynków i **plac 3v3 co najmniej 16 × 18 m**. Teraz zostawić niezabudowane i przygotować do nich dojścia. **Bez tworzenia nowych budynków.**
- Bezpośrednia brama wyjścia do miasta istnieje i musi działać. Trasy do następnych regionów tylko jako rezerwy; niczego nie odblokowywać samowolnie.

## Materiały, realistyczny wygląd i wydajność

- Asfalt: rzeczywisty materiał PBR z właścicielskiej Biblioteki, preferowany TwoLaneRoadPatches02_MR_4K; ciemny naturalny grafit, drobne ziarno, **nie jasna płaska płyta**. FourLaneRoadClean01 używać wyłącznie po ocenie przydatności do konkretnej jezdni, nie kopiować oznakowania czteropasmówki na ulicę dwupasmową.
- Chodniki: PavingStones151_2K, realne fugi i skala, naturalne obrzeża; wejścia bez wysokich progów.
- Pobocza: naturalna, wyraźnie zielona trawa z grass_bermuda_01_2k i rzeczywiste rośliny, bez plastikowych brył.
- Materiały źródłowe 4K zostają w bibliotece, runtime Unity 6 po testach skala 1–2K, mipmapy, dobór kompresji i LOD.
- Brak dostępu do danej paczki = **BLOCKED dla jej materiału**, a nie zgoda na podrobiony PBR. Przed importem sprawdzić faktyczną dostępność, licencję i sumę SHA.
- Wszystkie drogi i chodniki muszą być rzeczywistą geometrią Unity z kolizją i materiałami; nie tylko obrazem naklejonym na teren.
- Zachować iOS/WebGL: rozsądny podział siatek, statyczne elementy, instancing, streaming dla kolejnych dzielnic; nie zakładać, że samo większe pole jest bezkosztowe.

## Obowiązkowa kolejność i separacja wykonawców

1. **Faktyczny audyt sceny Unity**: import realnej sceny BraxhornEstate, 553 źródłowe siatki, transformacja X, wszystkie drzwi, działki i zasięg gruntu. Raport footprintów przed drogami.
2. **Przedstaw mapę nakładkową** obecnego i nowego obrysu oraz przebiegu RD-01..RD-05; wszystkie kolizje z budynkami i portalami muszą zostać wyeliminowane. Wykonawca może korygować krótkie odcinki bez przemieszczania budynków i bez przestawiania zatwierdzonej sieci.
3. **Wykonaj realne skrzyżowanie, nawierzchnię i chodnik** na pierwszych 15% robót, pokaż zdjęcia i MP4 z Unity. Odrębne, zweryfikowane nagranie nie zastępuje kompilacji.
4. Zrealizuj **wszystkie** RD-01..05, odnowienie SRC-01..05, łączące je piesze trasy PW-01..08, rampy, przejścia, krawężniki, pobocza, kolizje i teren.
5. Dla każdego skrzyżowania i wejścia sprawdź w Unity poruszanie Braxem, brak szczelin i softlocka. Zapisz SHA + numer kompilacji, prawdziwe zdjęcia i filmy z ciągłego spaceru; pokaż pełny przejazd tras i dojście do każdego obiektu.
6. Po **zaakceptowanym odbiorze dróg** wrócimy do remontu 8 domów, a później ustawimy nowe budynki na wcześniej przygotowanych parcelach. Integracja z całym Unity 6 tylko po osobnej zgodzie właściciela.

## Warunki PASS i granice

- Wszystkie istniejące domy, działki, wejścia, boisko, park, budynki użyteczności, oryginalna geometria i Brax pozostają na miejscu.
- Nie zmieniać siłowni, miasta, wspólnego sterowania, mastera, innych gałęzi, produkcji ani Biblioteki ChatGPT.
- Tylko **Unity 6 / C# / GitHub Actions / prawdziwe WebGL**. Brak pseudo-buildów, renderów zastępczych i sztucznych filmów.
- **Pierwszy obowiązkowy dowód po około 15%**: >=4 screenshoty przed/po z rzeczywistej sceny i ciągły film MP4 20–40 sekund, commit + run ID, lista rzeczywiście przypiętych PBR. Właściciel **nie musi wchodzić do gry**, aby obejrzeć dowody.
- **Końcowy dowód**: wszystkie drogi i chodniki wdrożone, mapa w Unity, dokładne ujęcia, co najmniej kilka ciągłych spacerów obejmujących całą sieć, QA kolizji i pełna lista PASS/FAIL/BLOCKED.
- Automatyczna kompilacja PASS nie jest akceptacją wyglądu. Fizycznego iPhone'a nie symulować jako już przetestowanego.
- Każda istotna zmiana założonych stref, istniejącego układu czy przesunięcie budynku wymaga uprzedniej zgody właściciela.

**Cel zlecenia: najpierw kompletne, wiarygodne, połączone DROGI i CHODNIKI regionu R2 w Unity 6. Bez nowych budynków i bez publikacji produkcyjnej.**

## Trwałość oryginalnych eksportów i poprawny tryb pracy Codexa

- Nie szukaj edytora Unity na swoim lokalnym terminalu jako warunku startu. Pracujemy: **GitHub connector → zmiany C# w izolowanej gałęzi → GitHub Actions Unity 6000.0.58f2 → WebGL → prawdziwe screenshoty i MP4**. Brak lokalnego Unity nie jest powodem zakończenia zadania.
- **Źródłowa gałąź R2 ma starszy workflow**, który nadal pobiera oryginalne modele z GitHub Actions run **40787817505** i **40840263799**. Ich artefakty są czasowe. W pierwszej kolejności przystosuj ODIZOLOWANY workflow drogowy do trwałego szkicu GitHub Release: **unity6-original-source-imports-20261030**.
- W sprawdzonej gałęzi **codex/unity6-integration-20261030** istnieje działający kod **unity-tools/city/fetch_original_imports.py** oraz rozdzielony workflow: etap dostępu do prywatnego szkicu (uprawnienie wyłącznie do odczytania źródeł, token technicznie wymaga contents:write), następnie odizolowany etap kompilacji read-only. Zastosuj ten zweryfikowany wzorzec z kontrolą SHA-256; nie powracaj do wygasających oryginalnych Actions jako jedynego źródła.
- Każdy nowy workflow i każda nowa scena tylko na **codex/estate-r2-roads-expansion-20261030**. Żadnych zmian ani automatycznego łączenia gałęzi **codex/estate-premium-renovation-20261030** (wykonawca domów), **codex/unity6-integration-20261030** (wykonawca miasta) i **main**.
- Rzeczywiste drogi implementuj w odrębnej klasie C#, np. **BraxEstateR2RoadNetwork**. Podłącz ją minimalnym, izolowanym wywołaniem wyłącznie do tworzenia sceny **BraxhornEstate**. Nie przepisuj kontrolera Braxa, prefabów siłowni ani cudzych materiałów.
- Pliki z Biblioteki ChatGPT nie są automatycznie montowane w Codex. Jeśli zasób PBR jest poza GitHub workspace, zastosuj zatwierdzony transfer jego kopii i weryfikację licencji / SHA; nie twórz atrap. Braki odnotuj jako **BLOCKED**, a prawdziwe drogi, które można wykonać, przygotuj bez podawania fikcyjnego końcowego PASS.
- Odbiory 15%, 100% i inne raporty nie zastępują **własnego wizualnego odbioru operatora**. Nie obiecuj działającego linku przed rzeczywistą publikacją i testem WebGL.

## Potwierdzony zakres tego commitu

Ten commit obejmuje **jedynie plan, układ proponowanych osi i kontrakt dla Codexa**. Nie utworzono żadnej drogi w scenie Unity 6, nie użyto plików właściciela do importu i nie wykonano żadnej kompilacji drogi. Realizacja fizyczna i materiałowa jest zadaniem kolejnego etapu.
