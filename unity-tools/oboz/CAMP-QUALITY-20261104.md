# Obóz — przygotowanie całości według standardu właściciela

Baza: `gpt/oboz`, `8ee5fbe296db466d9b6d70c2936386a458154de9`, Unity 6000.0.58f2.
Zakres: obóz i podłączenie podstawowych animacji Moniki. Zapis kodu do kolejnej
poprawki, bez uruchomienia Unity, workflow, APK lub zmiany zlecenia buildu.
Wzorzec wykonania: przypięte źródła miasta/osiedla opisane w
`docs/zasady/STANDARD_PRACY_UNITY6_20261104.md`.

## Wynik audytu i przygotowane poprawki

| Obszar | Stan kodu / zmiana | Czego to nie potwierdza |
| --- | --- | --- |
| Dojazd i pętla autobusu | Istniejący asfalt Poly Haven clean_asphalt, fizyczna skala 2,1 m, komplet czterech map 4K | Odbioru wyglądu i wydajności na telefonie |
| Chodniki | Istniejąca kostka concrete_pavers, skala 1,9 m, szerokości projektowe 1,8/2/2,4 m razy K=1,294 | Końcowej oceny szerokości w kamerze gracza |
| Domki, pomosty, wiaty, dachy | Istniejące drewno, podłoga i metal PBR, deskowanie, okna, okapy i rynny; poprawione UV walców i rur w metrach, zamiast rozciągania całego obrazu na słupie | Pełnego wykończenia premium i odbioru wnętrz |
| Postacie i materiały prywatne | Import v4 zachowuje do 4096 px również dla NPC i map danych Moniki, ASTC 4×4/90, trilinear i jawne Texture2D | Że każdy plik źródłowy rzeczywiście zawiera 4K |
| Las i trawa | Zachowane AO, siła normalnych, siła AO oraz transformacje/wybór UV z glTF; dane normalnych pozostają liniowym RGB XYZ | Zgodności wszystkich modeli po nowym imporcie Unity |
| Liście i dalsze widoki drzew | Mip coverage z progu źródła, własne warianty map z alfą, źródłowe obrazy i materiały bez zmian; limit atlasu 8192 pozwala zachować cztery widoki po 2K, jeśli źródło je zawiera | Nie tworzy nowych szczegółów w obecnym mniejszym atlasie |
| Trawnik wewnątrz obozu | Uzupełnienie oryginalną geometrią bermuda, wspólny renderer instancji Claude; wyłączenia dla dróg, dojść, domków, aren, worków, zaplecza, bunkra i rezerwowanych miejsc | Wyglądu gęstości oraz FPS/RAM bez rzeczywistego playera |
| Autobus | Kopie zewnętrznych map dla obozu nie dziedziczą już ograniczenia 2K z miejskiego konwertera; oryginał osiedla bez zmian | Nie zwiększa rozdzielczości tekstur osadzonych w GLB |
| Monika | Trzy zatwierdzone przez właściciela FBX z poprzedniej dostawy zachowane; kontroler dopasowany do pełnej długości skoku i chwili odbicia | Naturalnego retargetingu na podstawie samych hashy i kości |

Raport `build/CampLifeReview/texture-audit.json`, tworzony przy składaniu sceny,
obejmuje także postacie i instancjonowaną trawę: wymiary źródła, wymiary importu,
nadpisanie Androida, format, alfa, braki względem 4K/2K oraz oszacowanie pamięci
ASTC z mipmapami. Tekstury osadzone/nieznane są jawnie oznaczone; suma dotyczy
tylko znanych formatów i nie jest pomiarem całej pamięci telefonu. Nie ma
automatycznego obniżania jakości ani sztucznego powiększania obrazów.

Manifest źródeł potwierdza 4K map Moniki, VORNA, ZYRO, RAKKA, KADE i GORRA.
Miś ma mieszane 2K/4K; wariant źdźbeł bermuda pochodzi z paczki 2K.
Te istniejące ograniczenia źródła nie znikają po zmianie limitu importera.
Nie oznaczać całego obozu jako spełniającego 4K z bliska przed rozliczeniem
raportu rzeczywistych tekstur i oceną źródeł. W tej kontynuacji nie pobrano
nowych fotoskanów, modeli ani prywatnych kluczy.

## Awaria ostatniej próby — oddzielnie od jakości obrazu

Run #12: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41892941205
Źródło `f333a94fd2345eb431b08386ff9db654f32fc1af`, job 126794837417.
Timeout 1200 s próby życia obozu przy 352 s symulacji; wymaganie wynosi 420 s.
APK nie rozpoczął kompilacji. Artefakt z dowodami:
`b3b3838c52334b9adf023607f1b8f8f07c94009b0b58448aadad26f12503fb34`.

W #12 potwierdzono 90/90 tras, 7 postaci × 14 przypadków póz, trzy areny,
sparing Moniki w każdej i dalszy trening worków. Jedyny błąd raportu życia to
timeout. To wynik starego SHA, nie nowego retargetingu ani nowych materiałów.
Klatki z tego runu posłużyły do kontroli dojazdu, nawierzchni i roślinności.

`8ee5fbe` usuwa automatyczne renderowanie diagnostycznych kamer pomiędzy
ręcznymi zapisami PNG. Kamera gracza działa nadal. Nie skrócono wymaganych
420 s, nie wydłużono timeoutu, nie pominięto tras/aren ani dowodów.
Wpływ poprawki na czas zakończenia pozostaje niezmierzony bez kolejnego runu.

Przygotowana dodatkowa próba skoku w `ReviewCampLife` sprawdza rzeczywistą
komendę, odbicie, pełną fazę powrotu i lądowanie, dodając 24 obrazy oraz próbki
stanu kontrolera. Nie uruchamia osobnego workflow i nie wydłuża symulacji.

## Wykonane kontrole lokalne

- Dokładne źródła bazowego SHA zweryfikowano po Git blob SHA; zachowano także
  równoległą dostawę Patryka z `490213a29605e0afdefa433ac75f115a56801975`.
- 75 testów narzędzi obozu: PASS, w tym bezpieczne przywracanie źródeł,
  nowa zaszyfrowana dostawa Moniki, kontrola zleceń, podpisu i dostawy APK.
  Pierwszy przebieg lokalny nie miał kopii wspólnego `prywatne.py`; po jej
  odtworzeniu z bazowego SHA pełny zestaw przeszedł. Nie zmieniano testów,
  by ukryć tę brakującą zależność.
- Kontrola gramatyki i konfliktów nazw C#: 275 plików, zero błędów.
  Nie zastępuje rozwiązywania typów/API, kompilacji shaderów ani Unity.
- Obliczenia planu trawnika: 9193 zachowane kępy + 10644 dodane, razem 19837.
  Nowe punkty nie wchodzą w podane obrysy, ścieżki ani obniżony kanał strumienia;
  najbliższy nowy punkt ma 2,591 m od krawędzi ścieżki po skali K.
  To niezależne obliczenie układu, nie kontrola kolizji w Unity.
- Pliki źródłowe animacji, pierwotny VegetationLayout, geometria źródłowych
  modeli oraz istniejące `.meta` nie są zastępowane. Żadna konfiguracja workflow
  ani `build-request.json` nie została zmieniona.

## Stan całości i następny odbiór

Przygotowane poprawki kodu nie są odbiorem ukończonego obozu. Nadal potrzebne są
nowe rzeczywiste ujęcia z Unity: wejście, asfalt przy autobusie, kostka przy
stopach, elewacje i dachy domków, zaplecze, ringi, oktagon, trawa oraz Monika
w chodzie/biegu/skoku. Sprawdzić także ślizganie stóp, przejścia animacji,
czas próby, pamięć tekstur i płynność na urządzeniu.

Fort, tartak, kojce i chata leśniczego w obecnym generatorze pozostają kotwicami,
nie gotowymi modelami. Dodatkowe stanowiska treningowe mają geometrię, ale
nie pełną obsługę ćwiczeń; autobus jest zaparkowany. Nie ogłoszono tych funkcji
jako gotowych i nie zastąpiono ich przypadkowymi modelami. Brak Unity i
odszyfrowanej pełnej sceny w tej sesji uniemożliwia rzetelny odbiór wizualny.

Źródła/poprawki: przygotowane. Nowy import Unity: niewykonany. Nowy APK:
nieuruchomiony. Telefon: niesprawdzony. Następny build wymaga nowego polecenia
właściciela; ta kontynuacja nie jest zleceniem.
