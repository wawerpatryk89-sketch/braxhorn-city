# Miasto Mistrzów — pięć stacji i pełne dwutorowe połączenie

Stan: **kod oraz statyczne generowanie geometrii wykonane; Unity / Android NIE PRZETESTOWANE**.

Prace na izolowanej gałęzi: `codex/miasto-mistrzow-metro-5-stacji-20261105`. Gałęzi właściciela `gpt/miasto-mistrzow-kasyno-t11-20261105` nie nadpisywać. Nie uruchamiać APK ani nie publikować, dopóki obecne A1 + T11 nie zostanie zakończone.

## Trasa

Jedna nieprzecinająca się linia fizyczna dla obu torów:

`(1130,245) -> (800,245) -> (800,359) -> (235,359) -> (235,975) -> (765,975) -> (765,380)`

W zaplanowanej kolejności przystanków:
- **M5 GALERIA WSCHODNIA** (965,245)
- **M1 DWORZEC** (400,359) — oryginalna stacja i stojący oryginalny pociąg
- **M4 SPORT ZACHÓD** (235,645)
- **M3 ARENY / HOTELE** (765,800)
- **M2 CENTRUM** (765,545)

Każda stacja używa tego samego rzeczywistego szablonu Unity: wejście i zejście do hali, siedem oryginalnych bramek, dwie pary napędów schodów ruchomych, peron wyspowy, dwa tory, tunele, sprzęt i kolizje. Cztery pozostałe stacje otrzymują prefab bez zmniejszania drzwi i człowieka. Osobne atlasy tablic 1024x1024, nazwy, kolory ścian, podłóg, listew, sufitów i oryginalne tekstury. Fizyczna poręcz schodów została odczepiona od taśmy stopni; przesuwana jest tylko tekstura gumy.

### Kontrola torów i wysokości

- Dwa tory, cztery główki szyn, oryginalny rozstaw z modelu źródłowego, wysokość szyn zgodna z peronem.
- Wyliczone 6 odcinków tuneli łącznikowych po odjęciu 5 prefabów. Końce torów licują z oryginalnymi portalami prefabów `TW0=-70`, `TW1=112`; każdy odcinek toru wewnątrz stacji ma ~140,6 jednostki planu.
- Łączna długość osi trasy: **2633,54 nominalnych jednostek planu**. Łuki rozłączone od stacji; brak skrzyżowań w poziomie.
- Zmieniono korytarz od strony wschodniej, aby omijać podziemny parking i rampę kasyna **T11**. Nie dowodzi to jeszcze bezkolizyjności ze wszystkimi *przyszłymi* fundamentami A2/H2 ani ze wszystkimi obiektami runtime.
- Wejścia M4/M5 połączone z chodnikami, ich podstawowe rezerwacje nie pokrywają działek w spisie `plan_inventory.json`.
- `trafficEnabled=false`, `trainOperationVerified=false`: **pociągi nie kursują**. Nie jest napisany ani przetestowany system jazdy, zatrzymania i drzwi.

### Dowód kodu, nie działania gry

GitHub Actions #42109543378 (5 listopada 2026) zakończony sukcesem:
https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42109543378

W logu:
- `STATIC_METRO_NETWORK_PASS_UNITY_NOT_RUN`, 5 stacji, 6 łączników, 2 tory, 4 szyny, wykluczony parking T11;
- `GEOMETRY_CHECKS_PASS_UNITY_PENDING`, 312 prefabrykowanych grup siatek infrastruktury, 801336 trójkątów, 10 przystanków tramwajowych, 32 perony autobusowe;
- 8 + 3 + 4 zaliczone testy statyczne i poprawne parsowanie trzech zmienionych plików C#.

**Dopiero po przejściu testu rzeczywistej sceny Unity 6000.0.85f2 i Android można uznać ją za gotową.**

## Testy wymagane przed następnym APK

1. W Unity wygenerować **całe pięć stacji** i wszystkie 6 odcinków łączących torowiska. Sprawdzić, czy nie ma widocznych lub fizycznych szczelin w przejazdach i czy każda para szyn trafia w portal prefabów.
2. Przejść fizycznie od wejścia ulicznego M1–M5 do obu krawędzi peronu, przez oryginalne bramki i oba ruchome schody. Żadne kolizje nie mogą zostać globalnie wyłączone.
3. Obejrzeć tablice i pięć różniących się wnętrz z perspektywy gracza; potwierdzić prawidłową orientację UV.
4. Zbadać odległości torów od garażu T11, fundamentów budynków A2/H2, tramwajów i innych przyszłych podziemnych obiektów, z uwzględnieniem wysokości Y.
5. Profilować **312 siatek i 801336 trójkątów całej infrastruktury** (nie tylko metra). Geometria jest grupowana przestrzennie w `meshkit.py`; potwierdzić działanie cullingu, renderowania i RAM na prawdziwym Androidzie.
6. Jeśli ma kursować pociąg, potrzebny jest oddzielny kontroler ruchu z odwracaniem na krańcach, kierunkiem, postojami, kolizjami i otwieraniem drzwi, testowany w runtime. Nie ustawiaj `trafficEnabled=true` bez tego.
7. Przenieść poprawki do **następnego** wspólnego wydania tylko po zaakceptowaniu już rozpoczętej aktualizacji Areny A1 + Kasyna T11. Zmiany kodu C# nie mogą być dostarczone do starego APK samą paczką Addressables.
