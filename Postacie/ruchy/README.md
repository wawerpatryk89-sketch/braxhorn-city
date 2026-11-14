# Globalna paczka animacji T

[Paczka FBX, 14 listopada 2026](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Pozycja_T_animacje_20261114.zip) · [Katalog źródeł i profili ról](mixamo.json)

Dziewięć dzisiejszych dostaw rozszerza bazę T ze 110 do 197 różnych ruchów. Archiwum zawiera też trzy starsze klipy w referencyjnej pozie T użyte w obsadzie (spoczynek, wstawanie oraz piłka). Każdy plik zawiera animowany szkielet. Wydzielanie animacji zachowuje klucze i ustawienia kości. W katalogu zapisano pochodzenie każdego wariantu. Paczka A jest przechowywana osobno.

## Przygotowanie postaci

Po rozpakowaniu modelu użyj z katalogu głównego repozytorium polecenia poniżej. Podaj identyfikatory postaci i lokalną wspólną paczkę. Przygotowanie Osiedla wykonuje ten krok również w CI.

```sh
python3 unity-tools/claude/ruchy_t.py gitara wiechu --archive Pozycja_T_animacje_20261114.zip
```

Modele mają własny Avatar Humanoid. Dla obu dłoni importer mierzy kierunek od małego palca do wskazującego w pozie referencyjnej modelu i wyrównuje płaszczyznę dłoni do pozycji opisanej przez Unity. W spoczynku i w gestach poza wynika z klipu. Chód, bieg oraz trucht korzystają z celów stóp zapisanych przez Humanoid (Foot IK). [Unity o T-pozie i stopach](https://unity.com/blog/engine-platform/mecanim-humanoids)

Oryginalne ruchy są współdzielone. Dobór ruchów i dopuszczenie do walki wynikają z profilu postaci. Dla bieżących 14 modeli potwierdzono 65 identycznie połączonych kości Mixamo, przy różnych wymiarach i orientacji dłoni w pozie wiązania.

W Unity 6 sprawdź mapowanie obydwu rąk i palców oraz referencyjną T-pozę Avatara. Zielony znacznik oznacza dopasowanie wymaganych kości. Przy błędzie konfiguracji Unity przewiduje pobranie pozy wiązania, automatyczne mapowanie i wymuszenie T-pozy w konfiguracji Avatara. Ustawienie dotyczy interpretacji klipów przez ten model. Zachowaj oryginał FBX i wagi skóry. [Konfiguracja Avatara w Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html)

Do kontroli wizualnej wybierz **Braxhorn → Postacie → Kontrola T w Unity** po przygotowaniu profili i modeli. Dla chodu i biegu narzędzie porównuje obie strony ciała z awatarem źródłowym w 32 chwilach cyklu. Wartości CSV określają różnicę w kierunku palców, płaszczyzny dłoni, uda, podudzia i stopy względem tułowia. Render korzysta z aktualnego położenia wierzchołków. Brak ruchu nóg oznacza błąd testu. Wyniki trafiają do katalogu `KontrolaPostaciT` obok projektu Unity. Sprawdź spoczynek, kilka faz chodu, dłoń i łokieć obu rąk, stopy, gest i ruchy właściwe dla roli. Poprzednie podglądy Blender z kart postaci pochodzą ze starszego sposobu przenoszenia ruchu.
