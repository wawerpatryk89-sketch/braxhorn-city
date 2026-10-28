# Braxhorn City — osiedle, etap 2

**Aktualizacja 28.10:** połączono lokalnie z ukończoną pierwszą dzielnicą i poprawiono oświetlenie siłowni. Aktualny stan, zasady publikacji i wyniki: `docs/HANDOFF_INTEGRATION.md`. Poniższy opis zachowuje historię samodzielnego etapu 2; zamknięta brama centrum ma teraz działający przycisk zmiany planszy.

Gałąź: `codex/osiedle-stage1`. Osobna plansza `/osiedle/`.
Nie zmieniono kodu miasta, siłowni, `public/game.js`, głównego indeksu ani konfiguracji wdrożenia.
Nie scalać z main bez uwzględnienia równoległej pracy nad pierwszą dzielnicą.

## Wykonana architektura

- 8 domów łącznie: sześć instancji dostarczonych modeli, własna willa Wawera i dom kontaktowy.
- Ulice Wiśniowa, Bydgoska i Poznańska, pętla ulic bocznych, działki, furtki, chodniki i przejścia.
- Willa Wiśniowa 4: salon, kuchnia/jadalnia, sypialnia, łazienka, taras i ogród.
- Kasyno: własna obudowa budynku i foyer; dostarczona sala z automatami, stołami i barem; dodatkowe blackjack, ruletka i poker.
- Komisariat: własna obudowa i foyer oraz dostarczone szczegółowe biura. Krzesła blokujące nowe wejście przeniesione do poczekalni.
- Dyskoteka: własna elewacja, foyer, szatnia i kompletna sala/scena z modelu MozillaHubs. Podniesione korytarze wejściowe modelu zastąpiono własnym dojściem, aby zachować przejście dla postaci; nie są używane jako niewidoczne kolizje.
- Szpital: własny budynek, rejestracja, centralny korytarz i 4 gabinety; 4 łóżka, 3 krzesła i wózek z paczki Madduck z przypisanymi oryginalnymi teksturami.
- Sala bokserska i hala z oktagonem: platformy, otwarte wejścia, schodki, szafki, ławki, miejsca widowni, kasa i zaplecza.
- Park Wiśniowy: 7 drzew złożonych z części Verdant Trail, 17 źródłowych krzewów i 6 kamieni; alejki, ławki i drążki.
- Boisko do koszykówki 28×15 m: linie, kosze i ogrodzenie z wejściami.
- Garaże, miejsca przyszłych zleceń i widocznie zamknięte bramy do lasu i centrum.

## Dane misji, bez uruchamiania rozgrywki

`layout.js` zawiera punkty spotkań, dostawy, recepcji, kasyna, zaplecza klubu i zapisów na walki.
Dla obu aren zapisane są spawny gracza/przeciwnika, obszar walki, wejście, wyjścia, widownia i miejsce wypłaty.
NPC, auta, walki, nagrody, logika misji i przejścia do pozostałych plansz pozostają na następny etap zgodnie z kolejnością użytkownika.

## Materiały i pochodzenie

- `dev/osiedle/assets/manifest.json`: źródła, autorstwo i sumy kontrolne modeli kasyna, klubu, komisariatu i stołów; pochodzenie eksportów roślin/szpitala.
- `dev/source-assets/verdant-trail/`: oryginalny blend i archiwa tekstur, w tym dosłane dwie części archiwum 5. Nie wszystkie obrazy całej sceny zostały dostarczone; szczegóły braków w README.
- Drzewo jest montażem oryginalnego pnia/gałęzi jacaranda i liści island_tree z tej samej paczki; brakujących tekstur liści jacaranda nie zastąpiono zgadywanym obrazem.
- `dev/source-assets/hospital/`: oryginalna paczka Madduck, kontrola SHA-256 i opis eksportów.
- `dev/source-assets/people-packs/`: oryginalne HCG Law Enforcement i Low Poly People, ZIP-y sprawdzone; bez dodawania NPC do poziomu.
- Płatna paczka 14 NPC odłożona; nie dokonano zakupu.

Modele źródłowe i kopie wykonawcze są rozdzielone. Duże tekstury nowych kopii gry ograniczono do 1024 px; geometria parkowych gałęzi/liści uproszczona, drzewa/krzewy/kamienie instancjonowane. Oryginały pozostają nietknięte.
Raport budżetu tekstur: `public/osiedle/texture-budget.json`.

## Poprawki techniczne

- Scalanie statycznych siatek według materiału; dekodowanie znormalizowanych atrybutów przed transformacją eliminuje zdeformowanie skompresowanego domu.
- Kolizje odpowiadają rzeczywistym otworom w ścianach. Przy wycinaniu wejść zachowano UV i użyto tej samej siatki do renderowania i kolizji.
- Dekoracyjne linie źródłowych modeli nie tworzą kolizji z metrowym marginesem raycastera.
- Naprawiono szerokość pionowych elementów ogrodzeń.
- Korony drzew nie blokują przejść; kolizje obejmują pnie i kamienie.
- Ruch dzielony na małe kroki, kolizje uwzględniają promień postaci, podłogi i platformy.
- Dzień, materiały matowe, brak bloom/emissive, kamera obracana wyłącznie przez gracza.

## Odtworzenie i kontrola

```sh
python3 dev/osiedle/build.py
node dev/osiedle/check.cjs
```

Build korzysta z `ESBUILD` lub przypiętego esbuild przez npm i nie przepisuje pierwszej dzielnicy.
Harness sam uruchamia serwer; potrzebuje Playwright i Chromium (`CHROME_PATH`).
`OS_FAST=1` pomija obrazy; `OS_SHOTS=park,court,...` ogranicza zapis wybranych zrzutów.
`review/report.json` zawiera rzeczywiste wyniki tras, liczbę załadowanych modeli, błędy JS i statystyki renderowania.
Testy przechodzą kontrolerem do wnętrz oraz wracają; samo teleportowanie do zdjęcia nie jest testem wejścia.
Zrzuty wykonywane w 915×412 z emulacją dotyku. Plan `review/plan-osiedla.png` jest rysunkiem układu, nie renderem końcowej jakości.

## Pozostałe ograniczenia

To etap architektury i przygotowania przestrzeni, nie finalna oprawa całej gry.
Elewacje własne, część mebli, drzewa poza parkiem i oświetlenie nadal wymagają dalszego dopracowania.
Nie przetestowano wszystkich pomieszczeń i pięter sześciu importowanych domów; nie przedstawiać ich jako odebranych wnętrz.
Brak pomiaru FPS na fizycznym iPhonie/Androidzie; software rendering i emulacja rozmiaru ekranu tego nie zastępują.
Brak upieczonych lightmap/AO wnętrz. Bramy do centrum/lasu pozostają zamknięte.
Zmiany zapisane lokalnie; nie twierdzić, że są na GitHub/Vercel, dopóki push i wdrożenie nie zostaną potwierdzone.

## Wynik kontroli etapu 2

21/21 tras i kontroli bram zakończonych powodzeniem; 0 błędów JavaScript; 21 instancji modeli budynków/wyposażenia oraz 30 instancji roślin/kamieni. Potwierdzono 8 domów. Obejrzano zrzuty wnętrz, parku, boiska i obu aren.

## Ustalenie użytkownika 28.10: start gry i siłownia BRAXHORN

Równoległa praca nad miastem jeszcze trwa. Nie rozpoczynać scalania ani zmian w jej plikach, dopóki użytkownik nie poinformuje o zakończeniu tej pracy. Poniższe wymagania dotyczą kontroli i poprawek po otrzymaniu końcowej wersji.

- Sprawdzić start gry w siłowni: ładowanie, początkowe położenie postaci i kamery, sterowanie, wyjście oraz powrót.
- Użytkownik nadal ocenia wygląd siłowni jako niewystarczający. Nie uznawać jej za zaakceptowaną wizualnie na podstawie starszych notatek; obejrzeć rzeczywisty końcowy stan przed poprawkami.
- Usunąć zbędne światła i efekty obciążające grę. Bez bloom, halo, migotania i świecących dekoracji. Neon może pozostać jako matowy napis lub element dekoracji, bez poświaty i dodatkowego oświetlania otoczenia.
- Zachować czytelne, równomierne oświetlenie sali; dla nieruchomego wyposażenia wykorzystać istniejące światło wyliczone wcześniej. Po zmianach geometrii zaktualizować jego dane zgodnie z PROJECT.md.
- Zachować ustaloną kolorystykę BRAXHORN, dzień i ręczny obrót kamery. Poprawić materiały i wykończenie bez samowolnej zmiany zaakceptowanego kierunku wizualnego.
- Porównać wygląd i koszt renderowania przed i po poprawkach w tych samych ujęciach. Obejrzeć zrzuty 915×412 z dotykiem; emulacji nie przedstawiać jako pomiaru płynności na fizycznym iPhonie.

Status: zapisano wymagania; w ramach tego ustalenia nie zmieniono siłowni, nie wykonano scalania ani wdrożenia.
