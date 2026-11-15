# Globalna paczka animacji T

[Paczka FBX, 14 listopada 2026](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Pozycja_T_animacje_20261114.zip) · [Katalog źródeł i profili ról](mixamo.json)

Dziewięć dzisiejszych dostaw rozszerza bazę T ze 110 do 197 różnych ruchów. Archiwum zawiera też trzy starsze klipy w referencyjnej pozie T użyte w obsadzie (spoczynek, wstawanie oraz piłka). Każdy plik zawiera animowany szkielet. Wydzielanie animacji zachowuje klucze i ustawienia kości. W katalogu zapisano pochodzenie każdego wariantu. Paczka A jest przechowywana osobno.

## Dobór ruchów dla kolejnych postaci

Każda postać dostaje podstawowe czynności potrzebne w grze. Dla każdej osobno dobierz chód, bieg, spoczynek, gesty rozmowy i powitania oraz sposób siadania, siedzenia i wstawania. Uwzględnij rolę, strój, obuwie i sytuację. Do chodu codziennego korzystaj z wariantów wskazanych przez właściciela poniżej.

Przed zapisaniem profilu sprawdź, które klipy źródłowe mają już postacie tej roli oraz modele przygotowywane razem. Porównuj rzeczywisty ruch, uwzględniając pliki z identyczną animacją pod różnymi nazwami. Spośród wariantów pasujących do postaci wybieraj także te rzadziej używane. Każdemu imprezowiczowi z tej samej grupy dobierz inny główny taniec i osobno wybierz tańce dodatkowe. Podczas rozmowy gest powinien pasować do sytuacji, np. powitania gościa, swobodnej pogawędki lub rozmowy przy stole.

Uliczni zadymiarze mogą dzielić podstawowe ruchy walki. Pozostałe ciosy, kopnięcia, uniki i reakcje dobieraj do postaci. Zawodnikom ligi dobieraj indywidualny zestaw pod ich rolę i styl walki. Przed przypisaniem porównaj go z zestawami innych zawodników.

Na docelowym modelu obejrzyj krok i podparcie stopy, ruch barków i dłoni oraz przejście od siadania przez siedzenie do wstawania. Dla pozy przy stole sprawdź oparcie przedramion o blat. Dla pozy z nogą na nodze sprawdź ułożenie kolan i przenikanie ubrania. Na wspólnym filmie podpisz postacie i dobrane klipy, żeby można było porównać ruch obok ruchu.

## Chód codzienny postaci T

Do chodu codziennego właściciel wybrał pięć plików z dostawy 14 listopada o 19:49. Źródła i ich sumy SHA-256 są w [katalogu dozwolonych chodów](chody_T_wlasciciela.json). Używaj tego katalogu przy przygotowywaniu kolejnych postaci. Starszy plik o nazwie Walking (3) ma inny ruch i inną sumę. W nowej dostawie Walking (1) i Walking (3) mają jednakowe klucze animacji i trajektorię bioder. Zostały zachowane pod dwiema nazwami właściciela.

W profilach tej partii Gitara ma przypisany `Strut Walking`, a pozostałe 13 postaci ma `Walking (1)`. Instalator pobiera wskazane oryginalne FBX ze szkicu wydania. Do pracy offline umieść oba archiwa T w jednym katalogu.

[Oryginalne FBX](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Chody_T_wlasciciela_5.zip) · [Pięć chodów na Gitarze w Blenderze](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_5_chodow_T.blend) · [Film z przodu i z boku](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_5_chodow_T.mp4)

Poniższe polecenie odtwarza próbę na jednym modelu. Wybór domyślnego wariantu dla danej postaci należy do właściciela. Próbę renderowano w Blenderze 4.3.2.

```sh
blender -b --python-exit-code 1 -P unity-tools/claude/proba_chodow_t.py -- Gitara.blend Chody_T_wlasciciela_5.zip Gitara_5_chodow_T.blend
```

## Archiwalna próba Rokoko na Gitarze

Właściciel odrzucił wygląd postaci na dotychczasowych podglądach Unity. Liczba odtworzonych klipów poniżej opisuje zakres testu. Odbiór wizualny 14 postaci pozostaje otwarty.

W Blenderze 4.3.2 z oficjalnym dodatkiem Rokoko Studio Live 1.4.4 sprawdzono na oryginalnym modelu Gitary trzy klipy z paczki T, wybrane z jej profilu. Powstały zdjęcia rozmowy i machania oraz film z chodem z dwóch stron. Projekt do sprawdzenia zawiera trzy osobne akcje i osadzone tekstury do 2048 px. Otwórz wersję z przyrostkiem **2K**. Zachowuje czas trwania wszystkich trzech źródeł przy wspólnej osi czasu 60 kl./s. Oglądane pozy pochodzą z Blendera. Ta próba wymaga jeszcze sprawdzenia po imporcie do gry.

[Projekt Gitary 2K](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_Rokoko_3_ruchy_2K.blend) · [Film](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_Rokoko_chod.mp4) · [Gesty](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_Rokoko_gesty.jpg) · [Fazy chodu](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Gitara_Rokoko_chod.jpg)

[Skrypt wsadowy Blendera](../../unity-tools/claude/rokoko_postacie_t.py) odczytuje model i profil roli. Weryfikuje SHA-256 klipu, mapuje kości względem pozy referencyjnej T i korzysta z dodatku na roboczej kopii szkieletu. Zapisuje rotacje i przemieszczenie bioder jako oddzielną akcję. Dla każdego ruchu porównuje współrzędne wierzchołków, wagi skóry i macierze kości w pozie wiązania przed przeniesieniem ruchu i po nim. Spójność tych danych i wygląd animowanego modelu to dwie osobne kontrole. [Instrukcja producenta](https://support.rokoko.com/hc/en-us/articles/4410463481489-Retarget-an-animation-in-Blender)

Przykład dla Blendera z włączonym dodatkiem. Podaj rozpakowany FBX modelu, wspólną paczkę T, katalog ruchów, identyfikator postaci i nową ścieżkę wyjściową. Opcja `--ruchy` ogranicza wybór do wskazanych stanów. Bez tej opcji skrypt przetwarza wpisy w profilu tej postaci.

```sh
blender -b --python-exit-code 1 -P unity-tools/claude/rokoko_postacie_t.py -- \
  Gitara.fbx Pozycja_T_animacje_20261114.zip Postacie/ruchy/mixamo.json gitara Gitara_2K.blend --ruchy Walk Talk Wave
```

## Przygotowanie postaci w Unity

Po rozpakowaniu modelu użyj z katalogu głównego repozytorium polecenia poniżej. Podaj identyfikatory postaci i lokalną wspólną paczkę. Przygotowanie Osiedla wykonuje ten krok również w CI.

```sh
python3 unity-tools/claude/ruchy_t.py gitara wiechu --archive Pozycja_T_animacje_20261114.zip
```

Modele mają własny Avatar Humanoid. Dla obu dłoni importer mierzy kierunek od małego palca do wskazującego w pozie referencyjnej modelu i wyrównuje płaszczyznę dłoni do pozycji opisanej przez Unity. W spoczynku i w gestach poza wynika z klipu. Chód, bieg oraz trucht korzystają z celów stóp zapisanych przez Humanoid (Foot IK). [Unity o T-pozie i stopach](https://unity.com/blog/engine-platform/mecanim-humanoids)

Oryginalne ruchy są współdzielone. Dobór ruchów i dopuszczenie do walki wynikają z profilu postaci. Dla bieżących 14 modeli potwierdzono 65 identycznie połączonych kości Mixamo, przy różnych wymiarach i orientacji dłoni w pozie wiązania.

W Unity 6 sprawdź mapowanie obydwu rąk i palców oraz referencyjną T-pozę Avatara. Zielony znacznik oznacza dopasowanie wymaganych kości. Przy błędzie konfiguracji Unity przewiduje pobranie pozy wiązania, automatyczne mapowanie i wymuszenie T-pozy w konfiguracji Avatara. Ustawienie dotyczy interpretacji klipów przez ten model. Zachowaj oryginał FBX i wagi skóry. [Konfiguracja Avatara w Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html)

Do kontroli wizualnej wybierz **Braxhorn → Postacie → Kontrola T w Unity** po przygotowaniu profili i modeli. Edytor odtwarza wszystkie ruchy przypisane do profilu i trzy ćwiczenia dla każdej postaci korzystającej z siłowni. Zapisuje zdjęcie każdego ruchu i kilka faz wybranych ruchów. Ujęcia danej postaci mają stały kierunek kamery. Dla chodu i biegu narzędzie porównuje obie strony ciała z awatarem źródłowym w 32 chwilach cyklu. Wartości CSV określają różnicę w kierunku palców, płaszczyzny dłoni, uda, podudzia i stopy względem tułowia. Render korzysta z aktualnego położenia wierzchołków. Brak ruchu nóg oznacza błąd testu. Wyniki trafiają do katalogu `KontrolaPostaciT` obok projektu Unity. Sprawdź spoczynek, kilka faz chodu, dłoń i łokieć obu rąk, stopy, gest i ruchy właściwe dla roli. Poprzednie podglądy Blender z kart postaci pochodzą ze starszego sposobu przenoszenia ruchu.

Przy porównaniu stopy ze źródłem użyj także oryginalnego FBX. Podgląd na Avatarze źródłowym w Unity sam podlega importowi Humanoid. W klipie Fast Run zmienia on na niektórych klatkach kąt stopy. Poniższe polecenie porównuje fazę z rzeczywistym plikiem z paczki właściciela. Zapisuje odchylenie kierunku uda, podudzia i stopy dla obu stron w `lokomocja_wzgledem_FBX.csv`. Odcinek od kostki do palców zależy też od położenia stawów w konkretnym modelu. Ocenę widocznego skrętu wykonaj na animowanej siatce i sprawdź ułożenie stopy przy kontakcie z podłożem.

```sh
blender -b --python-exit-code 1 -P unity-tools/claude/porownaj_lokomocje_t.py -- KontrolaPostaciT Pozycja_T_animacje_20261114.zip
```

## Kontrola obsady 14 listopada

W edytorze Unity 6000.0.58f2 odtworzono 290 przypisań ruchów dla 14 postaci, w tym 21 ćwiczeń siłowni. Powstało 762 zdjęć animowanych modeli. Dla chodu wszystkich 14 postaci oraz biegu 12 z nich zmierzono obie strony w 32 fazach. Patryk i Wiechu otrzymali korektę orientacji dłoni w pozie referencyjnej. Kontrola edytora i próbki ruchu nie zastępują sprawdzenia kontaktów z meblem, rekwizytem i podłożem w konkretnej scenie.

Dla Patryka test długości kości objął 50 klipów i osiem próbek każdego klipu. Największa zmiana długości wyniosła 0,002%. W chodzie mediana różnicy orientacji płaszczyzny dłoni względem źródłowego Avatara zmalała ze 106,8° do 28,1° u Patryka oraz ze 104,9° do 25,6° u Wiecha.

Ostrzeżenie starego testu nóg Patryka utrzymuje się dla obu klipów. Test mierzy kierunek poziomego rzutu stopy względem bioder i klasyfikuje podparcie na podstawie kąta kolana i podudzia. To inny pomiar niż kąt między stopą modelu i oryginalną stopą w 3D. Maksymalna różnica tego drugiego kąta dla Patryka wynosiła 30,1° w chodzie i 20,9° w biegu przy 32 próbkach na stronę. W dwudziestej próbce biegu lewe palce źródłowego FBX są blisko podłoża, kostka się unosi, a pozioma składowa kierunku stopy spada poniżej 30%. Nie wyznacza to stabilnego kierunku podeszwy. Zapisujemy ostrzeżenie, żeby ocenić kontakt buta z podłożem w Osiedlu.

[Podgląd na telefon (PDF)](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Braxhorn_Postacie_T_role_14.pdf) · [Zdjęcia i pomiary Unity](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/untagged-4487374c3ba5c1e097f9/Podglady_Unity_T_43620556579_1.zip)
