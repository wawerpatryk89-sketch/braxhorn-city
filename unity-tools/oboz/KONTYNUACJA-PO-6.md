# Obóz — kontynuacja po #6, 4 listopada 2026

Baza zdalna: `acd2dea75103dee7cf85b65859fe284cb672d7b3` na `gpt/oboz`.
Zachowano w całości decyzję właściciela o ręcznych workflow i osobnej zgodzie
na Unity/APK. Kod tej kontynuacji nie uruchamia buildu, testu Unity ani odzyskiwania.
Miasto, osiedle, wygląd, modele i sterowanie pozostają poza zmianą.

Aktualne zlecenie właściciela: połączyć tę kontynuację z zawartością #6 i
uruchomić jeden pełny build obozu. Nowy kod wymaga nowego APK; odzysk starego
#6 nie dostarczyłby zmian trzech aren. Zgoda na build została udzielona,
ale samo zapisanie tego dokumentu nie oznacza, że workflow wystartował.

## Przygotowane zachowanie — jeszcze bez uruchomienia w Unity

- Para NPC zmienia arenę po rundzie: oktagon → Ring 1 → Ring 2. Worki i odpoczynek
  nadal mają po dwa miejsca; sześciu zawodników zamienia się zajęciami.
- Punkty na matach są wiązane z trzema istniejącymi arenami. Generator sprawdza
  90 kierunkowych połączeń NavMesh między dziesięcioma miejscami zajęć.
  Nie dodano teleportowania NPC, nie przesuwano ringów ani wejść.
- AKCJA wewnątrz zajętej areny wybiera aktualnego obecnego zawodnika, zamiast
  przypisanego na stałe NPC, który może właśnie trenować gdzie indziej.
- Interakcja z NPC nie zasłania przycisku sparingu i nie zmienia zadań podczas
  walki Moniki. Po jej zakończeniu automatyczny plan wznawia pracę.
- Podczas sparingu Moniki zatrzymuje się automatyczna para; pozostali dalej
  chodzą do stanowisk, uderzają worki i odpoczywają. Rozmowy odpoczywającej pary
  mogą trwać; automatyczna para nie rozmawia przez walkę gracza.
- Przygotowano próbę Play Mode: runda w każdej arenie, wejście/wyjście Moniki
  przez wspólny rejestr AKCJA we wszystkich trzech arenach, odrzucenie zmiany
  zajęcia podczas walki, dalsze trafienia worków i powrót do rotacji.
- Kadr audytu uwzględnia również szerokość leżącej postaci. Kamery diagnostyczne
  śledzą bieżącą arenę; kamera gracza pozostaje bez zmian.

Nie rozszerzono jeszcze ćwiczeń na sześciu dodatkowych stanowiskach, transportu,
fortu, tartaku, kojców ani autonomicznej roli Moniki. Jakość głosów nadal czeka
na odsłuch/akceptację. Nie ma testu telefonu ani pomiarów FPS/RAM.

## Rzeczywisty wynik #6

Run: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41852545458

Źródło: `b0d35c0d4bbfef14f3f7711f7800c2efea2b8e0c`, job `126652390459`.
Kompilacja Unity i weryfikacja Androida przeszły. Tworzenie draft release
zatrzymało się na `HTTP 403: Resource not accessible by integration`.
Log deklaruje `Contents: write`; dokumentacja GitHub opisuje dodatkowy wymóg
uprawnienia Workflows dla części docelowych commitów. Obie gałęzie wskazujące
wcześniej na źródłowe SHA zostały w trakcie runu przesunięte zmianą zasad CI.
Nie zmieniono uprawnień, nie ponowiono buildu i nie uznano błędu za sukces.

Dokumentacja: https://docs.github.com/en/rest/releases/releases#create-a-release

Pobrany artifact `13337323766` (77911 B), SHA-256
`e18e08651431499e4325f68fd578b94a2a819a58df725b43cbfc9bb86ff7e90c`, zawiera
raporty i logi, **nie APK**. Raport tożsamości potwierdza wynik kontroli w CI:

- applicationId: `com.braxhorn.oboz`
- versionCode: `215967386`
- certyfikat SHA-256: `f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe`
- APK SHA-256: `a8d5d9f4af7598a3c301d250516e6d4f47d64e50686c25ac3b80a7fd5f642af0`

Sam plik APK nie został odzyskany ani ponownie sprawdzony poza CI.
Nie ma potwierdzonego linku pobrania #6. Nie zastępować go APK #5.

## Zabezpieczenie i przygotowany odzysk bez kompilacji

- Przyszły zatwierdzony build zapisze zweryfikowany APK jako osobny artifact
  **przed** zapisem wydania. Błąd release nadal zakończy job błędem, ale nie
  usunie drogi pobrania APK. Backup nie publikuje prywatnej Library ani scen.
- #6 zapisał pełną zaszyfrowaną Library wraz z Bee do Actions cache. Log wskazuje
  3767722736 B i SHA-256
  `f870c84ea41f6becfe55c2b8f0918392905eacd4fea0da072d89fb78a6b82735`.
  Trwały zapis release cache także dostał 403; nie uznawać go za dostępny.
- `oboz-recover-apk.yml` jest przygotowany wyłącznie do ręcznego uruchomienia
  po decyzji właściciela, z `gpt/oboz`, gdzie dostępny jest cache źródłowego runu.
  Najpierw trzeba wprowadzić ten workflow na właściwą gałąź i udostępnić jego
  ręczne uruchomienie zgodnie z zasadami repo. Ta kontynuacja tego nie uruchamia.
- Odzysk pobiera dokładny raport #6, sprawdza sumę raportu i cache, odtwarza Bee
  dotychczasowym narzędziem, wyszukuje konkretny `launcher-release.apk`, sprawdza
  identyczność bajtów, pakiet, wersję i podpis. Nie uruchamia Unity/Gradle, nie
  podpisuje ponownie i nie wybiera innego APK, gdy właściwego brakuje.
- Istnienie APK wewnątrz cache **nie zostało jeszcze sprawdzone**. Jeśli go tam
  nie ma albo cache wygasł, odzysk zakończy się błędem bez kompilacji zastępczej.

## Jedno połączone uruchomienie

- `oboz-unity-build.yml` pozostaje wyłącznie ręczny i ograniczony do `gpt/oboz`.
  Najpierw uruchamia Play Mode bieżącego SHA i sprawdza pełny raport: trzy areny,
  sparing Moniki na każdej, uderzenia worków podczas jej walki, rotację, pauzę
  oraz komplet 98 przypadków animacji. Brak lub błąd raportu blokuje APK.
- Dopiero po zaliczeniu tej kontroli kompiluje jeden APK, sprawdza pakiet,
  rosnącą wersję i stały podpis oraz zapisuje niezależny artifact z APK.
- `publish_apk.py` nie tworzy wydania ani tagu. Zapisuje pliki z wersją i SHA
  w istniejącym drafcie `oboz-review-latest`. Kolizja nazwy przerywa zapis,
  zamiast nadpisywać wcześniejszy APK. Wskaźnik `release-identity.json` jest
  aktualizowany dopiero po udanej wysyłce wszystkich plików tej wersji.
- Obecność draftu jest sprawdzana przed kosztownym importem Unity. Kontrola
  lokalna nie potwierdza jeszcze, że uprawnienia do uploadu w CI wystarczą.
- Przywracanie nagrań i checkpointu obsługuje zarówno dotychczasowe wydania,
  jak i nowe nazwy zawierające wersję oraz SHA. Kontrola SHA-256 pozostaje.
- Domyślna gałąź `main` nie zawiera jeszcze wejścia ręcznego workflow obozu.
  Rejestracja wymaga osobnej zmiany CI; nie przenosi kodu gry na `main`.
  W obecnej sesji nie ma operacji `workflow_dispatch` ani zalogowanej
  przeglądarki GitHub. Nie raportować startu bez rzeczywistego numeru runu.

## Weryfikacja tej kontynuacji

- 7 testów odzyskiwania: zgodne bajty, niewłaściwy commit/APK/pakiet/wersja/
  certyfikat, nieudana weryfikacja kryptograficzna, brak pliku/symlink,
  zachowanie innego istniejącego APK. Android tools są atrapami testowymi;
  nie jest to weryfikacja prawdziwego APK #6.
- 15 dotychczasowych testów tożsamości wydania: PASS.
- 7 testów publikacji do istniejącego draftu: PASS. Sprawdzają niezmienność
  nazw, kolejność wskaźnika, błędy wysyłki, kolizję, draft, tożsamość i zmianę
  bajtów APK. GitHub jest atrapą testową; nie jest to rzeczywista wysyłka.
- Parser C# dla ośmiu zmienionych plików, YAML i Bash: PASS; nie kompilacja C#.
- Wszystkie workflow obozu są ręczne; brak push/PR/schedule/workflow_run.
- Play Mode trzech aren: NIEURUCHOMIONY. Odzysk #6: NIEURUCHOMIONY.
- Nowe APK: NIEURUCHOMIONE. Telefon: NIETESTOWANY.
