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
- Wynik Unity, liczba ostrzeżeń i pierwszy błąd: **oczekują na autoryzowany przebieg „sceny” tego etapu**. APK: **nie budowano**. Telefon/nagranie: **nie zweryfikowano**.

## Pozostałe etapy

C → D → E → F → G będą publikowane i raportowane osobno. Lista H jest weryfikowana równolegle; nowe modele wymagają wskazanej przez właściciela akceptacji. Nie ma potwierdzenia limitu 400 MB dla nowej wersji, ponieważ nie uruchomiono APK.
