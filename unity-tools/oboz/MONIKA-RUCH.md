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

Status: narzędzie diagnostyczne; poprawka produkcyjna jeszcze nie przyjęta.
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
