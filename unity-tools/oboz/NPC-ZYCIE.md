# Obóz — ruch sześciu NPC i zajęcia, 4 listopada 2026

Baza: ad405045e8323a21c5d419c0cfe9490bc7730297, poprawka Moniki po Play Mode.
APK tej bazy jest osobnym, pojedynczym runem 41830727765. Nie wolno go
anulować ani uruchamiać duplikatu przy audycie NPC.

## Zlecenie właściciela

Sprawdzić VORN, ZYRO, RAKK, KADE, GORR i Miś: deformacje, nogi/stopy,
dłonie, postój, chód, bieg, ciosy i trening. Zachować oryginalne modele,
wygląd, proporcje i metadane. Monika pozostaje graczem i kierownikiem obozu.
Dwóch NPC ma sparować w klatce, inni trenować przy workach, odpoczywać
oraz przemieszczać się i wymieniać rolami po rundzie. Monika ma przydzielać
zadania przez istniejącą interakcję. Bez nowego sterowania i przebudowy UI.

## Stan zastany

- Wszystkie sześć NPC używa starszych źródeł Idle/Walk/Run, które u Moniki
  wymagały wymiany. Samo podobieństwo źródeł nie dowodzi deformacji każdego NPC.
- Trasy składają się z dwóch punktów oddalonych o 0.8 * 1.294 m,
  z 24-sekundowym postojem. Nie tworzą obiegu treningowego.
- BraxObozArena obsługuje wyłącznie Monika–NPC. Brak sparingu NPC–NPC.
- Worki mają fizykę, ale NPC nie zadają im impulsów; Training jest samą animacją.
- Wzrosty sześciu NPC pozostają tymczasowymi wartościami podglądu źródła;
  nie zmieniać ani nie oznaczać jako zaakceptowane bez dowodu.

## Audyt przed zmianą ruchów

Izolowana próba ReviewNpcMotion: pięć ruchów każdej z sześciu postaci,
stara ścieżka oraz kandydat kalibrowany bezpośrednio. Dla Idle/Walk/Run
kandydat używa sprawdzonych źródeł Basic Motions właściciela. Punch/Training
zachowują oryginalny klip i porównują sposób przeniesienia na szkielet.
60 przypadków, 3600 pomiarów, 2160 obrazów CPU z przodu, tyłu i boku.
Wynik: run 41832677083, SHA 6ce5320c594be8fcf9a0da6873134a58a37b5088,
Unity 6000.0.58f2, sukces. Artifact 13325070849, SHA-256
21a1728349808385ee21ab3e21e547a7df3028f782e574896ed06c81f3d77620.
Sprawdzono sumę pobranego ZIP, komplet 2160 obrazów i 3600 pomiarów.
Oceniono 30 zestawień po sześć faz z przodu, tyłu i boku. Zielona kompletność
nie jest ogólną akceptacją wyglądu ani testem telefonu.

| Postać | Obserwacja starego ruchu | Wybór |
|---|---|---|
| VORN | Postój w szerokiej gardzie, chód w gardzie, skręt stóp w biegu | Kalibrowane Idle/Walk/Run Basic Motions |
| ZYRO | Jak wyżej; stopa odchylona od kolana w starym biegu | Kalibrowane Idle/Walk/Run Basic Motions |
| RAKK | Jak wyżej; silne boczne obroty butów w biegu | Kalibrowane Idle/Walk/Run Basic Motions |
| KADE | Jak wyżej; stopy bokiem do kierunku kroku w biegu | Kalibrowane Idle/Walk/Run Basic Motions |
| GORR | Jak wyżej; boczny skręt stóp w biegu | Kalibrowane Idle/Walk/Run Basic Motions |
| Miś | Jak wyżej; skręt stóp widoczny z tyłu w biegu | Kalibrowane Idle/Walk/Run Basic Motions |

Kandydaci mają opuszczone ręce w postoju/chodzie i stopy prowadzone zgodnie
z kierunkiem kolan w obejrzanych fazach. Nie zmieniono modeli, proporcji,
wysokości ani map. Punch/Training: obejrzane stare wersje bez rażącego skrętu
nóg jak w biegu; zachowano je, ponieważ wariant kalibrowany zmienia pochylenie
oraz postawę, bez wykazanego zysku. Pozostałe dziewięć klipów każdej postaci
nie należy do tego porównania; użyte w nowym runtime reakcje wymagają kontroli.
Miś nadal ma wcześniej wykazane odchylenia jakości tekstur/geometrii źródła;
wszystkie sześć wzrostów pozostaje niepotwierdzonymi wartościami podglądu.

## Kolejność wykonania

1. Odebrać obrazy i wskazać konkretny wynik dla każdej postaci.
2. Poprawić tylko potwierdzone problemy, wybrać właściwy swobodny ruch.
3. Podłączyć rzeczywiste trasy do worków, odpoczynku i wejścia klatki.
4. Rotacja par, rundy, kolizje, pauza, przydziały Moniki i reakcje worków.
5. Play Mode na scenie obozu: role, przemieszczenia, zmiany par i przeszkody.
6. Jeden następny APK po przejściu kontroli. Test telefonu nadal niewykonany.

## Wspólna gra i dźwięk

Obóz jest strefą tej samej gry co osiedle i miasto. Nie zmieniać ich gałęzi,
sterowania ani wspólnego standardu aktualizacji. Głosy mają być polskie,
naturalne, spójne między strefami i wyłącznie słyszalne w 3D: bez dymków,
napisów dialogowych i zastępowania mowy efektami. Właściciel ocenia stare
głosy osiedla jako przeznaczone do wymiany. Nie przenosić ich jako gotowego
rozwiązania. Właściciel wymaga jednego rozwiązania dla całej gry, bez samowolnego
przejścia na płatną usługę. Odczytano `unity-tools/claude/osiedle_glosy.py`
z SHA 5ea83909a1aa58b74fb9fa54cbe23a5087a3e087: osiedle używa lokalnego
Pipera 1.3.0, domyślnie darkman-medium/gosia-medium, nie płatnego API.
HeyGen był wyłącznie propozycją i nie jest wybranym dostawcą. Dalsza próba
powinna porównać warianty Pipera i rzeczywistą jakość nagrań. Nie raportować
rozmów jako gotowych przed odsłuchem i integracją.
