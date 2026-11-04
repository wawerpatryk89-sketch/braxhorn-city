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
Stan: przygotowany test, brak wyniku. Zielona kompletność nie oznacza
akceptacji wyglądu. Bez zmian runtime, zapisu scen, publikacji cache lub APK.

## Kolejność wykonania

1. Odebrać obrazy i wskazać konkretny wynik dla każdej postaci.
2. Poprawić tylko potwierdzone problemy, wybrać właściwy swobodny ruch.
3. Podłączyć rzeczywiste trasy do worków, odpoczynku i wejścia klatki.
4. Rotacja par, rundy, kolizje, pauza, przydziały Moniki i reakcje worków.
5. Play Mode na scenie obozu: role, przemieszczenia, zmiany par i przeszkody.
6. Jeden następny APK po przejściu kontroli. Test telefonu nadal niewykonany.
