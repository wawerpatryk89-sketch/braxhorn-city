# Obóz Misia — odbudowa po utracie wcześniejszego projektu

Stan: prace w toku. To nie jest gotowy ani uruchomiony obóz.

## Pierwszy build obozu — przygotowanie

Dodano osobny proces Android z Moniką: `unity-tools/oboz/README.md`.
204 pliki wejściowe mają łącznie 1 148 328 099 bajtów; to źródła przed importem,
nie rozmiar APK. Wykluczono pozostałości kopiowania i nieużywane kopie eksportów.
Kod obsługuje podpis, kontrolę tożsamości APK, cache Library/Bee i szyfrowany
zapis wygenerowanych scen. Wspólne narzędzia pochodzą bez zmian z commita
`1d83c1a6db4f6c71eb670e5a57bab963ce50e5c1`.

Blokada: paczki źródeł niezaszyfrowane i niewysłane do CI; w tym środowisku
brak bezpiecznego dostępu do klucza. Nie utworzono zlecenia uruchomienia buildu.
73 testy Pythona przeszły; kompilacja Unity i test telefonu niewykonane.

## Standard aktualizacji — 4 listopada 2026

Obowiązuje standard z gałęzi `codex/zone-updates-20261104`, commit `38422c5f031b5f9b85a3e1981c1a8199c9f75731`.
Usunięto zależność nazw siatek generatora od kolejności tworzenia obiektów.
Przygotowano eksport rejestru scen/prefabów, GUID, zależności i metadanych oraz
kontrolowaną kopię tych plików. Kod i testy nie oznaczają jeszcze pierwszego
eksportu Unity ani aktualizacji telefonu. Szczegóły, dowody i braki:
`unity-tools/updates/OBOZ-WDROZENIE.md`.

## Odzyskane źródła

- 336 plików własnego kodu i ustawień z `wawerpatryk89-sketch/braxhorn-city`, gałąź `claude/osiedle-42-poprawka`, commit `d533aa8128271c94087d34e06209db0cab3ff516`. Hash każdego pliku sprawdzony względem Git blob. Spis: `evidence/source-files.json`.
- Oryginalne modele Moniki, VORN, ZYRO, RAKK, KADE, GORR i Misia oraz ich mapy. Pliki prywatne pozostają poza gitem. Spis i SHA-256: `evidence/characters.json`. Identyfikatory archiwów: `evidence/recovery-baseline.json` (historyczny status w tym pliku nie opisuje aktualnego pobrania).
- 19 źródłowych klipów z paczek Brax, Nyxa, Vexa, Krov. Dawcy animacji nie są dodatkową obsadą sceny. Dobór klipów roboczy, do kontroli ruchu w Unity.

## Przygotowany nowy kod

- Import źródeł bez zmiany oryginalnych plików; kompresja Android ASTC 6x6, jakość 50; Monika Color/Normal 4096, pozostałe mapy 2048.
- Retarget Humanoid → Legacy dla istniejącego sterowania. Skala świata 1,294; Monika 1,68 m przed skalą.
- Audyt binarnych FBX: każda postać ma 65 kości. Monika 14275 quadów, VORN 13558 (w tolerancji 500), ZYRO 16153, RAKK 14341, KADE 14029, GORR 14439, Miś 8773.
- Generator roboczej zabudowy w edytorze Unity: podłoże, strumień, dwie kładki, cztery drewniane domki z drzwiami, jeden ring zewnętrzny, oktagon i kopia źródłowej salki z drugim ringiem. Liczby worków w kodzie: 4 w salce + 8 pod zadaszeniem + 12 na zewnątrz = 24. To kod generatora; scena nie została tutaj wygenerowana.
- Składanie podglądu z Moniką, sześcioma NPC, wspólną kamerą, sterowaniem, drzwiami i interakcją sparingu. Proste trasy trening/odpoczynek, zatrzymanie przy przeszkodzie, pauza, przerwanie sparingu po opuszczeniu ringu. Zasady sparingu są robocze, niepotwierdzone w grze.
- Podgląd może pokazać oryginalnego Misia z wyraźnym ostrzeżeniem o odchyleniach. Nie ustawia to zgody na odchylenia ani akceptacji do publikacji.
- Cache wypalonych postaci zależy od oryginalnych modeli, map, klipów, importera i wersji Unity; bez zmiany zależności można użyć gotowego prefabu. Cache nie był uruchamiany w tym środowisku.
- Zabezpieczenie zapisu: nierozpoznany lub uszkodzony stary zapis jest tylko odczytywany i nie zostaje nadpisany. Nowa rozgrywka używa wersjonowanego schematu; migracja starego schematu nadal wymaga jego odzyskania.
- Odzyskano oryginalny bunkier (476954 trójkąty), leśną drogę (341109 trójkątów w definicjach siatek), mapy igliwia 4K, pięć paczek PBR oraz źródłową salkę (166411 trójkątów według manifestu archiwum). Oryginalny bunkier i droga wymagają sprawdzenia kosztu na telefonie.
- Wyjęto bezstratnie 145 map z trzech GLB do importu mobilnego. Powstałe pliki `__unity.glb` mają zmienione tylko odwołania do obrazów; binarny chunk i oryginalne GLB pozostały identyczne. Kompresja/dopasowanie rozmiaru następują dopiero w Unity.
- Z paczek jodeł i trawy wyeksportowano 14 modeli: po trzy warianty dużych drzew i młodych jodeł, każdy z autorskimi LOD1/LOD2, oraz dwa źdźbła trawy. Nie wykonano decymacji. Zachowano oryginalne archiwa, pliki Blender i mapy.
- Kontrola eksportu wykryła błędne zapakowanie liniowych kolorów igliwia do obrazu sRGB. Nowe mapy RGBA zostały złożone z oryginalnych 16-bitowych kanałów koloru i alfa; wartości pikseli sprawdzono. Nie rozjaśniano ani nie przemalowywano oryginałów.
- Wersje roślin do Unity mają 16 wspólnych map i około 27 MB geometrii, zamiast ponad 1 GB z powtarzanymi obrazami. Usunięto zbędne osadzone kopie obrazów; wszystkie używane zakresy bajtów siatek, UV i pozostałych danych pozostają identyczne.
- Przygotowano sześć atlasów dalekich widoków drzew, po cztery kierunki. Są to rzuty oryginalnych modeli, nie obrazy generatywne. Atlas zawiera kolor i alfa; shader stosuje aktualne światło sceny, bez stałego świecenia w nocy. Działanie shadera wymaga kontroli w Unity.
- Generator lasu ma 1729 drzew i 9552 kępy trawy, wyłączenia przy ścieżkach, strumieniu i zabudowie, LOD i kolizje pni w obszarze chodzenia. Trawa wykorzystuje istniejący system instancji. Liczby dotyczą danych generatora; nie jest to pomiar działającej sceny.
- Obejrzano rendery źródłowych siedmiu postaci. Rękawice potwierdzone u ZYRO, KADE i Misia; pozostałe modele mają gołe dłonie lub owijki. Miś nie ma T-pozy. Modele są znormalizowane do około 1 m: poza Moniką nie odzyskano potwierdzonego wzrostu. Wyłącznie w podglądzie stosowana jest jawna, robocza baza 1,80 m dla NPC; publikacja wymaga ustawienia właściwych wysokości.
- Korekta dłoni GORR: maska na kopii siatki wyliczana z wag kości dłoni/palców i korekta koloru w shaderze. Oryginalna geometria, kości, mapy i owijki pozostają bez zmian. Ciemne dłonie sprawdzono w renderze kontrolnym Blender, lecz jeszcze nie w Unity.

## Weryfikacja wykonana lokalnie

- `evidence/validation.json`: sumy kontrolne oryginałów i przygotowanych map, obsada, kości, wersja i skala.
- Parser gramatyki C#: kontrola nowych plików obozu oraz zmienionego `BraxGymPlayer.cs`. To nie jest kompilacja C# ani kontrola API Unity.
- Trzynaście testów narzędzi: zachowanie źródła i danych geometrii, metadane, przestrzeń barw, deduplikacja, kompaktowanie, odrzucanie nieprawidłowych danych oraz powtarzalność rozmieszczenia roślin i wolne dojścia.

## Niepotwierdzone / brakujące

- Brak uruchomienia i kompilacji w Unity: w tym środowisku nie ma edytora Unity ani kompilatora C#.
- Scena, ruchy, kontakt stóp, wygląd, logotypy i wydajność telefonu nie są zatwierdzone.
- Fort, architektura bunkrów, wyposażenie stołówki/pryszniców/sauny/medycznego/magazynu, namioty i detale otoczenia pozostają nieukończone. Część z nich ma tylko kotwicę w roboczym generatorze; nie jest przedstawiana jako wykonany model. Las ma przygotowane zasoby i generator, ale wygląd, przełączanie LOD i koszt na telefonie nie są zweryfikowane.
- Źródłowa salka wymaga uzgodnienia z późniejszym wykończeniem, muralem i sprzętem obecnej salki. Nie zmieniono pierwszej siłowni w mieście.
- Pełny plan dnia NPC, zwoływanie przez Misia, polskie głosy 3D, docelowe zasady walk i poprawki logotypów nie są ukończone.
- Miś ma odchylenia od wymagań liczby quadów i rozdzielczości części map; nie dodano automatycznego wyjątku.
- Nie wykonano APK. Przygotowany proces używa odzyskanego `com.braxhorn.oboz` i wymaga dotychczasowego podpisu; rzeczywisty certyfikat nie został jeszcze sprawdzony na pliku APK.
- Cel zapisu gry: istniejący klucz `braxhorn_oboz_progress_v1`. Nie odzyskano poprzedniego schematu zapisu; nie wolno go nadpisać niezgodnym formatem.

Przed publikacją: kontrola właściciela, próba w Unity, szyfrowanie zasobów przez Claude, zgodność podpisu i rosnący versionCode. Sama kopia kodu nie oznacza ukończenia gry.
