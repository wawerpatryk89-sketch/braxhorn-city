# Wspólna aktualizacja osiedla: Puls, Patryk, szyldy i ruch

Baza: `claude/osiedle-42-poprawka`, commit `d533aa8128271c94087d34e06209db0cab3ff516`.

## Zmiany

- Materiały i siatki szyldów zachowują referencje przy ponownym użyciu. Klucze przeciwległych plakatów uwzględniają pozycję i normalną. Grafiki mieszczą się w fasadzie z zachowaniem proporcji; szyldy obejmuje rejestr kontroli.
- Puls: mural właściciela pokrywa ścianę za DJ-em. Pojedyncza sofa i fotel są wydzielane z paczki Chesterfield; 13 loż z odstępami. Butelki mierzone jako kompletne przedmioty, bez skalowania według nakrętek i etykiet.
- Nowy Patryk z oryginalnej paczki właściciela: wzrost 2.33 jednostki, oryginalne Color/Normal 4096, ORM 2048, tekstury Android ASTC 4x4. Chód bez biegu, neutralne stanie, pełna kombinacja dwóch ciosów, kopnięcie i skok.
- Auta i postać mają wspólną skalę K=2.33/1.80. Usunięte dodatkowe powiększanie auta gracza do 125%; wybierana najlepsza zmierzona pozycja kierowcy. Ruch uwzględnia pełne rozmiary pojazdów. Autobus sprawdza obrysy na trasie hamowania, pozostałe autobusy, gracza i strefy zatrzymania.
- Zatoki i przejazdy dopasowane do skali K=2.33/1.80. Na płytkich placach miejsca równoległe; przy wyborze auta Patryka sprawdzane wolne miejsce przy drzwiach.
- Drogi B: szerokość 7.00 → 9.06 jednostki świata (7 metrów rzeczywistych). Parking pawilonu B: miejsca równoległe, przejazd 7.72 zamiast 4.48, wjazd 9.06. Podjazdy przy willach mają prawdziwy obrys 6.50, mieszczący się przed bramą; auta wybierane do dostępnej długości.
- Kontrola zakrętów bada całą długość auta, zamiast tylko środkowych 2.4 jednostki. Raport `skala-aut-i-drog.txt` podaje zmierzone w Unity szerokości pojazdów z lusterkami, jezdnie, pasy, marginesy i miejsca parkingowe; zwężenia A wymagają wyniku tej kontroli.
- Raport sceny sprawdza materiały, siatki, szyldy, mural, sofy, Patryka, auto i parkingi, dołącza wynik istniejącej 600-sekundowej symulacji aut.

## Weryfikacja przed Unity

- 240 plików C#: parser składni bez błędów. To nie jest kompilacja Unity.
- Python: kompilacja składni i 5 testów importu zakończone poprawnie: szyfrowany transfer, uszkodzona część, zły rozmiar archiwum, ścieżka poza archiwum, oddzielenie importu miasta od osiedla.
- YAML workflow i `git diff --check`: poprawne.
- Oryginalny nowy FBX: 65 kości `mixamorig`, 13 621 quadów, 22 trójkąty i 2 pięciokąty. Liczba quadów mieści się w tolerancji 500 poniżej 14 000.
- Przygotowanie oryginalnej paczki wykonane lokalnie; Color/Normal zachowują sumy kontrolne źródłowych PNG. Sprawdzone sumy 47 zaszyfrowanych części.

## Co wymaga wyniku Unity i sprawdzenia APK

Kompilacja C#, import Humanoid, testy kości/nóg, 600 sekund symulacji i kontrola zbudowanej sceny wykonują się dopiero w Unity. Nie są potwierdzone testami lokalnymi.

Na telefonie: przejść całe Puls (mural, meble, butelki); obejść obie strony szyldów salonu i stacji; sprawdzić chód, dwa ciosy, kopnięcie, skok i wsiadanie Patryka; przejechać zakręty i parkingi; obserwować auta oraz autobusy na skrzyżowaniach i przystankach.

Na polecenie właściciela brak zapisanej Library oraz błędy diagnostyczne NIE blokują APK. Wyniki pozostają w logu i artefakcie `osiedle-diagnostyka-<sha>`. Błąd kompilacji albo brak niezbędnego zasobu może nadal uniemożliwić utworzenie aplikacji. Ta aktualizacja nie oznacza potwierdzenia, że całe osiedle jest wolne od błędów.
