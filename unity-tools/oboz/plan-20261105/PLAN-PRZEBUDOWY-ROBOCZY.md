# Obóz BRAXHORN — plan roboczy do wspólnego zatwierdzenia

**Uwaga: nic z poniższego nie jest poleceniem implementacji. Właściciel nadal dosyła materiały.**

## A. Fakty sprawdzone w kodzie Obozu #18
- Generator terenu: 280 × 280 jednostek przed współczynnikiem K=1.294.
- Ogrodzenie: X -126..126, Z -125..115; brama główna w (0,-125), furtka w (6.8,-125).
- Plac autobusowy: środek (0,-94), zewnętrzny promień 18, wyspa promień 6; autobus (21,-92); obecna wiata (27,-92).
- Ring 1 (-23,15), Ring 2 (-12,51), oktagon (0,-5). Ringi są za blisko ciągów komunikacyjnych i wymagają rozdzielenia.
- Owalny tartan (65,72), 4 tory; prosta 100 m (113,-35), 4 tory. Log Unity zawiera `BRAXHORN_OBOZ_BIEZNIE`, ale nie zastępuje to oględzin na telefonie.
- 24 programowe worki, w tym 12 pod zadaszeniem, utrzymują mechanikę `BraxWorek`. Nie znaleziono brązowego modelu GLB w manifeście prywatnych zasobów #18.

## B. Diagnoza z 3 filmów i 10 zdjęć
Za dużo pustego obszaru; duże odległości między strefami; ring stoi w problematycznym miejscu względem dojść; wiata i autobus nie tworzą wygodnego przystanku; pasy nie łączą się konsekwentnie z chodnikiem z kostki; plac nawrotu ma niedostateczne oznaczenie; znak nad wjazdem nie ma wyglądu premium; siłownia i sanitariaty zbyt uproszczone; las wygląda rzadko; worki wymagają realistycznych modeli; wnętrza domków i stołówki mają zostać wyposażone w dostarczone assety.

## C. Proponowany, NIEZATWIERDZONY układ funkcjonalny
- Znacząco zmniejszyć teren i ogrodzenie, zachowując możliwość bezpiecznego zawracania autobusu, dostępu służb i pełnego obwodu bieżni. Wstępny wariant około 230 × 210 m przed skalowaniem: **to punkt wyjścia do przeliczenia, nie gotowe współrzędne**.
- Zachować główną bramę jako punkt orientacyjny; skrócić nadmierną drogę, przestawić obiekty bliżej siebie. Przygotować plan z góry z wymiarami, drogami, alejkami, ringami, bieżnią, budynkami, lasem i ogrodzeniem.
- Główna komunikacja: droga wyłącznie do autobusów/obsługi, osobne alejki piesze z realistycznej kostki; przejścia dla pieszych mają łączyć się bez przerw i stopni z utwardzonymi chodnikami, wejściami i furtką.
- **Plac zawracania:** wyraźna ciągła BIAŁA linia po całym obwodzie okręgu. Na wyspie centralnej zaprojektować posąg byka BRAXHORN; weryfikować obrys posągu i promień skrętu autobusu. Biała linia, wyspa i posąg są czymś innym niż bieżnia.
- **Brama i szyld:** premium, dobrze wykonana konstrukcja, napis BRAXHORN — OBÓZ TRENINGOWY i identyfikacja byka; tablica rozpięta dokładnie na szerokość bramy (nie pomniejszona, nie poza słupami). Nie blokować przejazdu.
- **Przystanek:** przestawić wiatę do właściwej zatoki; obudować trzy boki, zostawić jeden otwarty od strony pasażerów, zapewnić drogę bez kolizji, realistyczny dach/szkło/drewno, lepsze ławki premium, dobre oświetlenie, dostępną nawierzchnię i widok na autobus.
- **Strefa walki:** oddzielne place ringów i oktagonu; ring nie może stać na przejściu. Swobodne obejście, bezpieczne schody i dojścia, miejsce na sparingi NPC. Nie zmieniać szkieletów ani działających animacji bez uzasadnienia.
- **Bieżnia:** czerwona syntetyczna nawierzchnia z BIAŁYMI liniami torów i start/meta; docelowo długa pętla wokół skompaktowanego obozu, zamiast dalekiej samotnej bieżni; osobna prosta 100 m, jeśli faktycznie zmieści się bez przecinania ruchu pieszych i aut.
- **Las:** gęstszy za ogrodzeniem i na granicach, różne skale drzew, LOD/billboard dla dalekiego tła. Chronić wjazdy i przejścia, a także limit pamięci Androida.
- **Zabudowa:** domki z osobnym prawdziwym wyposażeniem, prysznice/łaźnia/WC z działającymi drzwiami, wodą, spłukiwaniem i umywalkami; stołówka z wyposażeniem właściciela; rzeczywista siłownia i sala treningowa, nie uproszczone bryły.
- **Murale:** cztery dostarczone grafiki na płaskich, odpowiednich ścianach drewnianych: OBÓZ WOJOWNIKÓW, BRAXHORN FIGHT ZONE, DROGA MISTRZA, BRAXHORN TRAINING CAMP. Nie kadrować, nie deformować, nie przepisywać napisów i nie zmieniać grafiki.
- **Dodatki:** grube liny, opony, worki, hantle, realistyczne meble i szafki. Użyć własnych assetów; brakujące dopiero szukać i zatwierdzać.

## D. Ograniczenia
Jedyny silnik: Unity 6. Android mobile-first. Kolizje fizyczne zostają. Zakaz niewidzialnych ścian blokujących wejścia. Brak nowych niezaufanych ciężkich 4K/8K tekstur bez budżetu. Zachować podpis APK, stabilność instalacji i separację stref gry. Bez automatycznego startu buildu.

## E. Materiały już dostarczone
10 plików GLB, 4 murale, 10 zdjęć, 3 nagrania. Na razie tylko inwentaryzacja. Łóżka, krzesła i komplet stołówki właściciel posiada, ale ma dosłać. Realistyczny **brązowy** model worka bokserskiego pozostaje do potwierdzenia.

## F. Dowody wymagane od przyszłego wykonawcy
1. Plan z góry z wymiarami i wszystkimi zmianami rozmieszczenia, do akceptacji przed przebudową.
2. Porównanie „przed / po” ronda, bramy, wiaty, ringu, bieżni, domków i sanitariatów.
3. Test dostępności wszystkich drzwi i przejść, NavMesh oraz nieprzenikania geometrii.
4. Pokaz działających trafień i kołysania worków po zamianie modelu 3D.
5. Raport budżetu model/tekstury Android; stabilna pamięć, bez nadmiernych pełnych map 4K.
6. Podpisany i zweryfikowany APK, prawidłowy certyfikat i test instalacji oraz uruchomienia na urządzeniu właściciela.

**Nie rozpoczynać prac:** najpierw dojdą dodatkowe materiały i ostateczna zgoda właściciela.
