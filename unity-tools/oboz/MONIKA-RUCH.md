# Monika — diagnoza ruchu, 4 listopada 2026

Priorytet właściciela: najpierw Monika. Film z telefonu pokazuje skręcanie nóg
i stóp podczas chodzenia. To potwierdzony objaw, nie zaliczony test naprawy.
Model, ubranie, tekstury, proporcje i siatka mają pozostać oryginalne.

## Zebrane dowody

- Oryginalny model Moniki: SHA-256
  `b817b4ed779a71f765c3a28909be43ff81cd663131479803ce216d10f17c9e88`.
- Odczyt FBX w Blenderze 4.5.15: 65 kości, przypisane i znormalizowane wagi;
  rozbieżność pozycji siatki w pozie wiązania poniżej 0.000002 jednostki źródła.
  W tej pozie odchylenie kierunku stóp wynosi około 5.71° / 2.86°.
  To kontrola źródła, nie dowód poprawnego działania Unity.
- Obecny `Walk` (dawca nyxa) ma wysoko zgięte przedramiona: około 125–152°
  od kierunku w dół w 64 próbkach stron i czasu. Nie jest spokojnym chodem
  z opuszczonymi rękami. Wymaga osobnego doboru ruchu.
- Obecny generator gra animację na dawcy, odczytuje HumanPose, przenosi ją
  na Monikę i zapisuje Legacy. Dotychczasowy validator sprawdza liczby
  i długości kości, ale nie sprawdza naturalności kolan i stóp.

## Izolowany eksperyment Unity

`BraxObozCharacters.ReviewMonika` porównuje oryginalną ścieżkę, bezpośrednie
odtwarzanie klipu Humanoid na Monice oraz wariant bezpośredni z awatarem
kalibrowanym na kopii szkieletu. Oryginalne bind poses i FBX nie są zmieniane.

Pięć dotychczasowych ruchów: Idle, Walk, Run, Punch, Training. Trzy warianty,
60 próbek na wariant/ruch; obrazy z przodu, tyłu i boku. Rewizja 3 dodaje
Idle/Walk/Run z biblioteki Basic Motions (wyłącznie warianty bezpośrednie).
Łącznie 21 porównań, 1260 pomiarów i 756 obrazów. Raport rozróżnia kompletność
eksperymentu od akceptacji wyglądu. Sam wynik zielony nie akceptuje ruchu.
Kierunek stopy mierzony jest tylko, gdy jej rzut na podłoże jest mierzalny.
Obrót w boksie trzeba oceniać w kontekście ruchu, nie progiem dla chodzenia.

Workflow uruchamia wyłącznie ten eksperyment. Nie wywołuje BuildPlayer,
nie aktualizuje wydania APK, nie zastępuje cache i nie dotyka innych stref.
Wynikiem są obrazy i pomiary, bez surowych prywatnych FBX i eksportów scen.

## Warunki przyjęcia poprawki

Oględziny pełnego cyklu chodu i biegu: kolana zginają się w naturalnej
płaszczyźnie, stopy nie odwracają się, nogi nie krzyżują przypadkowo;
postój i chód mają swobodne ręce. Ciosy i trening zachowują poprawną gardę
i legalny obrót stopy podporowej. Następnie przejścia w Play Mode i kontrola
na telefonie. Tych kontroli nie zastępuje kompilacja ani parser kodu.

Status historyczny pierwszej próby: narzędzie diagnostyczne, bez przyjętej poprawki.
Run `41824579277` (SHA `67c3d98582ffc3d9108f5d91475b6c7e6a6de75b`)
zakończył 21 przypadków bez wyjątków w Unity 6000.0.58f2/OpenGLCore.
Artefakt `13318747995`, 55869775 bajtów, SHA-256
`0c21bd170a025beca5cdd7777c42790c2f0d73fcfac5221dbab06de645c6bb26`
pobrano i sprawdzono: 756 PNG i raport. Oględziny wykazały niespójność:
np. CasualIdle mierzy przedramię 31.6° od pionu, lecz podgląd pokazuje
uniesione ręce i szeroki rozkrok. Obrazów z tego runu nie uznano za dowód
pozy kości. Podejrzenie: Camera.Render w jednym ticku edytora używa poprzedniego
wyniku skinningu. Rewizja 4 renderuje tymczasową siatkę z BakeMesh (CPU),
z bieżącej mierzonej pozy i z oryginalnym materiałem. Nie zmienia siatki źródła
ani renderowania gry. Dodaje też pomiar pochylenia stopy, aby nie mylić
naturalnego odgięcia nogi w biegu ze skrętem stopy w poziomie.
Pierwszy run `41822204442` nie uruchomił Unity: token `contents: read` nie
widzi prywatnego draft release dostawy (`release not found`). Istnienie draftu
i siedmiu paczek potwierdzono przez API. Rewizja 2 stosuje `contents: write`
tak jak działający workflow obozu; nie dodaje żadnego zapisu do release.
Run `41822556178` skompilował kod i zaimportował źródła, lecz zatrzymał się
w narzędziu diagnostycznym przy mapowaniu palców podczas kalibracji awatara.
Nie powstały obrazy porównawcze. Rewizja 3 normalizuje nazwy mapowania
i obsługuje brak opcjonalnych palców; nie przerywa pozostałych przypadków
po pojedynczym błędzie. Nie jest to jeszcze dowód poprawienia nóg w grze.

Klipy kandydackie pochodzą z biblioteki właściciela, paczka
`BRAXHORN_ANIMACJE_03_RUCH.zip`; SHA poszczególnych plików sprawdzono z katalogiem.
Przekazano wyłącznie CMS AES-256-GCM/RSA-OAEP-SHA256 do istniejącego draftu
transportowego obozu. Asset `709302192`, 233465 bajtów, SHA-256
`846d8cff45095124ec7b483c468fb6c44dba2e819aef7b06560642ed34fa8d6e`.
Właściciel potwierdził zgodę na przenoszenie po zatrzymaniu pierwszej próby
przez automatyczną kontrolę. Źródła nie zostały dodane do publicznego gita.
Przywrócenie testowe sprawdza całą paczkę, certyfikat, opakowany klucz i wszystkie
trzy źródła przed instalacją; lokalnie odrzucono błędny hash i niebezpieczną ścieżkę.
Odczyt cache w rewizji 3 wskazuje bazową gałąź `gpt/oboz`; kod wspólnego narzędzia
pozostaje identyczny, a pamięć źródłowej gałęzi nie jest zapisywana ani usuwana.
Kolejny etap po Monice: pozostała szóstka, zachowania NPC i zaparkowany autobus
skopiowany z istniejącego zasobu miasta/osiedla.

## Wynik poprawionych obrazów i wybór ruchów

Run `41826624631`, SHA `380e1eb2fe47b23c1d8c55f5ab03529ffd6f5727`,
Unity 6000.0.58f2/OpenGLCore: 21 przypadków bez błędów, zapis CPU BakeMesh.
Artefakt `13319269380`: 56867620 bajtów, SHA-256
`cca6e0653c069cd64fd42eeefa4a98f43bb26211d7075e1fc3ce15830165b08b`.
Pobrano i zweryfikowano archiwum. Obrazy są zgodne z pomiarami pozy.
Oględziny z przodu, tyłu i boku potwierdzają: dotychczasowy Walk jest chodem
w gardzie; dotychczasowy Run krzyżuje stopy/nogi i skręca je w wybranych fazach.
Nowe CasualIdle/Walk/Run pokazują swobodne ręce w postoju/chodzie oraz naturalne
zgięcie nóg w biegu. Wariant calibrated-direct wybrano do dalszej próby runtime.
Dla nowego chodu mierzalny poziomy kierunek stopy: p95 13.76°, max 24.58°
(po kalibracji), wobec p95 39.21°, max 164.12° bez kalibracji. W biegu same
wartości yaw bliskie 180° nie są błędem: stopa odgina się wraz ze zgiętą nogą.
Wybór opiera się również na obrazach, nie na jednym progu kąta.

Rewizja 5 zmienia tylko Idle/Walk/Run Moniki na te trzy sprawdzone źródła
oraz bezpośredni retarget z kalibracją. Ciosy i pozostałe ruchy nadal używają
dotychczasowej ścieżki; oryginalny FBX, siatka, bind poses, tekstury, wzrost
oraz klipy sześciu NPC pozostają identyczne. Zwykły build obozu przywraca tę
samą zaszyfrowaną paczkę ruchu. Wspólne narzędzia cache nie są zmienione.
Nowy helper jest częścią zależności cache postaci stosujących tę metodę.

Przygotowana próba `ReviewMonikaRuntime` uruchamia rzeczywisty BraxGymPlayer
w Play Mode w izolowanej scenie CI: postój, chód, bieg, zatrzymanie, cios,
pauza, wznowienie i drugi cios. Zapisuje pozycje, aktywne klipy, pomiary kości,
obrazy z kamery gracza i migawki siatki z trzech stron. Nie zapisuje sceny,
nie buduje APK, nie akceptuje automatycznie wyglądu.

## Wynik Play Mode i zlecenie APK

Run `41828712115`, SHA `44ea2126868da68e63422613b2f7e52b0e5c5144`,
job `monika-motion`: sukces w Unity 6000.0.58f2. Rzeczywisty BraxGymPlayer:
360 próbek, 12.90856 m przemieszczenia, zerowy ruch podczas pauzy, bez błędów.
Odtworzono Idle, Walk, Run, Punch i CombatIdle oraz przejścia, zatrzymanie
i wznowienie. Artefakt `13321037925`, 41116948 bajtów, SHA-256
`019f0e361493357ea01cee400177461b4e26827838735ecd299c679ce534b10c`:
raport i 480 PNG (120 z rzeczywistej kamery gry, po 120 z trzech stron).
Archiwum pobrano i sprawdzono hash. Oględziny klatek runtime nie pokazały
wcześniejszego skręcania nóg; ręce w spokojnym chodzie/postoju są opuszczone.
Ciosy i ich przejścia zachowują gardę. Jest to wynik izolowanej sceny Unity,
nie test obozu na telefonie ani akceptacja wyglądu przez właściciela.

Zlecono jeden APK na `gpt/oboz`, z aktualizacją istniejącej instalacji
(`firstInstall=false`) i kontrolą tego samego podpisu oraz rosnącej wersji.
Obejmuje poprawkę ruchu Moniki i wcześniejszy etap zabudowy z PR #7.
Model, materiały, proporcje i wzrost Moniki są zachowane. Pozostałe sześć
postaci nie ma jeszcze zakończonej kontroli ruchu; autobus i nowe zachowania
NPC pozostają kolejnym etapem. Test telefonu poprawki nadal nie odbył się.
