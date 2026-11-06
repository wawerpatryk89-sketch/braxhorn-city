# OBÓZ TRENINGOWY — naprawy po audycie 6.11

Gałąź: `codex/oboz-przebudowa-pbr-20261105`. Punkt wyjścia: `72ae341cd927406015036603cea06051caa8cbe7`.
Zlecenie właściciela w tej rozmowie: „Dokończ te zlecenie”. Tryb: **sceny, bez APK**.
Odbiór wizualny i nagranie właściciela pozostają wymagane. Żaden brakujący model nie jest zastępowany kostką.

## A-01 — rzeczywista skala, podparcie, postacie i wiarygodny pomiar

- Wymiary modeli są mierzone w odpowiednim układzie lokalnym. Obrót domku o ±90° nie zamienia długości łóżka z szerokością.
- Collidery wyposażenia korzystają z lokalnego obrysu kompletnego modelu po ustawieniu.
- Drzwi domków: skrzydło 0,90 × 2,10 m przed K, dopasowane futryny, bale przy otworze i collidery ościeża.
- Oryginalne części kuchni są kopiowane raz, z zachowaniem hierarchii i transformacji źródła. Stół z kuchenką ma wysokość gotowania 0,90 m × K; stoły jadalni 0,75 m × K. Kuchenka i talerz mają rzeczywistą podporę wyznaczoną raycastem.
- Misa zlewu ma wysokość 0,85 m, dolna krawędź pisuaru 0,55 m, środek lustra 1,60 m ponad zmierzoną podłogą. Tyły wyposażenia i półka są ustawiane na faktycznych ścianach. Oryginalne krany pozostają częścią GLB.
- Monika i sześciu NPC mają wspólny pomiar geometrii podeszew. Korekta zachowuje początkowy pivot i wygasa względem niego podczas skoku. Nie zmieniono modelu, szkieletu ani animacji źródłowych. Poślizg w ruchu nadal wymaga przebiegu sceny/Play Mode.
- Audyt zapisuje całe modele, podparcie ścienne, wysokości montażu, postacie, materiały i elementy niezmierzone. Nie uznaje samej nazwy `Monika`, `NPC` lub `półka` za dowód poprawnego kontaktu.
- Byk pozostaje w swojej powiększonej skali. Kod `RondoBull` i przekształcenie `NativeMetres` nie zmieniają skali. Audyt porównuje faktyczny obrys z niezależnie zmierzonym wzorcem oryginału 4× × K: **2,434245 × 3,904765 × 6,201168 jednostek świata**.
- Kontrola sceny pobiera `.gitignore` i zależności testów, sprawdza dokładny `expected_sha`, kończy edytor po synchronicznej metodzie i zapisuje pełne komunikaty generatora. Nie wywołuje kompilacji APK.

### Weryfikacja A-01

- Testy Python źródeł są wykonywane z `unity-tools/oboz`; wynik przed publikacją zostaje zweryfikowany na dokładnym indeksie tego etapu.
- Sprawdzenie składni i zakresów C# nie zastępuje kompilacji Unity.
- Do przebiegu Unity dodano 17 testów rzeczywistego zachowania: osie modelu przy czterech obrotach i dwóch skalach rodzica, kontakt z podłożem z wykluczeniem własnych colliderów oraz osiem pomiarów geometrii stóp i zachowania pivotu.
- Opublikowany commit: `005e76cde0c979cb6ffad44545e7dcf98ed09519`. Dokładny indeks przed publikacją: **214 testów źródeł + 2 testy kompilacyjnego kontraktu treningu PASS**, kontrola składni/zakresów 292 plików C# bez błędów. To nadal nie jest kompilacja Unity.
- GitHub test źródeł nr 58: **success**, run `42304677403`, ten sam SHA.
- Scena nr 25: run `42304823334`, job `128191396007`, ten sam SHA. Uruchomiony wyłącznie `oboz-scene-only`; `android-oboz` pominięty. W chwili przygotowania C trwa krok Unity; wynik i liczba ostrzeżeń zostaną dopisane z rzeczywistego raportu.
- APK: **nie budowano**. Telefon/nagranie: **nie zweryfikowano**.

## C-01 i domknięcie A-02 — powierzchnie, dojścia, stopy w ruchu

- Ścieżki do stołówki, magazynu i punktu medycznego kierują się do rzeczywistych południowych wejść. Dodano dojście do sauny. Łaźnia otrzymała dojście wokół wschodniej ściany do północnego wejścia; poprzednia odnoga przecinała jej wnętrze. Dodano połączenie siłowni z siecią. Pozycje budynków pozostają zachowane.
- Dziesięć łagodnych podejść ma zgodną powierzchnię widoczną i kolizyjną: cztery ganki, cztery budynki usługowe, łaźnia i siłownia. Test planu sprawdza połączenie, fronty i brak przecięcia wnętrz; audyt zapisanej sceny sprawdza każde podejście 33 promieniami.
- Podkład stanowiska kończy się na 4 cm; mata ma 4 cm grubości i górną powierzchnię na 8 cm. Przy pełnym ugięciu 3 cm pozostaje 1 cm ponad podkładem. Poprzednia mata miała 1 cm grubości i chowała się w podkładzie.
- Deformacja korzysta z rzeczywistych podeszew gracza i NPC, promienia 0,4 m oraz limitu 3 cm przed K. Kontakt stóp odczytuje wysokość widocznych zdeformowanych trójkątów; fizyczna kapsuła nadal korzysta ze stabilnej kolizji.
- Ślady: zachowane referencje po wczytaniu sceny, wspólna pula 40 obiektów, wspólna siatka i materiał, zanikanie przez 20 sekund. Najbliższe podłoże i maska mieszanki rozstrzygają, czy trafiono w błoto/piasek; ślady nie są rysowane przez chodnik na grunt pod spodem.
- Oryginalna trawa Grass Medium 01/02 zachowuje siatki i tekstury. Obie klasy instancji mają zasięg 25 jednostek świata; shader wygasza widoczną geometrię między 23 a 25 m, również na obrzeżach całych grup renderowania. Dalsze podłoże korzysta z istniejącej tekstury trawy. Nie zmieniono wspólnego renderera innych rejonów.
- Maska oryginalnego `mud_forest_2k` obejmuje także rzeczywistą lokalizację Ringu 2 i worków pod pawilonem. Audyt odczytuje rzeczywiste mapy i kolory wierzchołków zapisanej sceny.
- Zgodnie z bieżącą tabelą A.1 „człowiek 1,8” manifest ustawia 1,8 m przed K dla siedmiu postaci. Stare 1,68 m Moniki i prowizoryczne deklaracje NPC nie stanowią wyjątku od tego zlecenia. Źródłowe modele, szkielety i klipy nie są modyfikowane. **Wyraźny wyjątek właściciela dotyczy byka; jego 4× pozostaje zachowane.**
- Walk/Run: edytor mierzy prędkość podporowej stopy w oryginalnych klipach na jednorazowej kopii postaci. Komponent obozu dopasowuje odtwarzanie do rzeczywistego przesunięcia kapsuły. Pauza, skoki, walka i przejścia animacji pozostają wyłączone z tego dopasowania. Dane trafiają do `audit/oboz-krok-postaci.json`. Nie deklarujemy jeszcze wyeliminowania poślizgu na nagraniu.

### Dowody C-01 / A-02

- Rozszerzono rzeczywistą regresję Unity o 18 kontroli serializacji śladów, puli, zanikania, geometrii i interpolacji maty oraz pomiaru klipu o znanej prędkości. Razem z A jest 35 kontroli; licznik raportu wynika z wykonanych kontroli, nie z wpisanej obietnicy.
- Weryfikacja przed publikacją C: **226 testów źródeł + 2 testy kontraktu treningu PASS**, C# **35 plików Editor i 17 runtime, 0 błędów składni/zakresów**; `git diff --check` bez uwag.
- Lokalnie dostępne są testy źródeł i analiza C#. **Unity i fizyka sceny wymagają przebiegu C na opublikowanym SHA.** Nagranie telefonu i FPS pozostają niezweryfikowane; aktualnego rozmiaru APK nie ma.

## Pozostałe etapy

C → D → E → F → G będą publikowane i raportowane osobno. Lista H jest weryfikowana równolegle; nowe modele wymagają wskazanej przez właściciela akceptacji. Nie ma potwierdzenia limitu 400 MB dla nowej wersji, ponieważ nie uruchomiono APK.
