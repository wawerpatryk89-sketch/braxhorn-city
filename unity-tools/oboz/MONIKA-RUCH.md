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

Pięć ruchów: Idle, Walk, Run, Punch, Training. Trzy warianty, 60 próbek na
wariant/ruch; obrazy z przodu, tyłu i boku. Raport rozróżnia kompletność
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
Kolejny etap po Monice: pozostała szóstka, zachowania NPC i zaparkowany autobus
skopiowany z istniejącego zasobu miasta/osiedla.
