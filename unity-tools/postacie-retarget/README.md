# Animacje postaci Braxhorn City

Narzędzie dla Blendera 4.3.x przelicza ruch z humanoidalnych FBX Mixamo na rzeczywisty szkielet postaci, zapisuje akcje i sprawdza animowaną siatkę. Każda postać otrzymuje tylko stany ze swojego profilu. Dołączony katalog opisuje robocze przypisania z wersji gry `941f165` oraz osobne ćwiczenia siłowni. Dla nowych ról można wybrać inne klipy, których zgodność z zadaniem potwierdził właściciel.

## Pierwsze uruchomienie

Potrzebujesz Blendera 4.3.x, Pythona 3.11 lub nowszego do przygotowania materiałów i prywatnych źródeł FBX lub ZIP gry. Blender zawiera moduł NumPy. Do filmów potrzebny jest opcjonalny FFmpeg z filtrem drawtext. Polecenia wykonuj z folderu narzędzia. Jeśli blender nie jest w PATH, wpisz pełną ścieżkę do programu.

1. Umieść własne archiwa lub rozpakowane FBX w osobnym folderze. Źródła są w [szkicu Postacie](https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/tag/untagged-4487374c3ba5c1e097f9) i w oryginalnych paczkach właściciela. Model rozpoznajemy po sumie FBX, a nie po nazwie ZIP. Jeśli masz tylko zoptymalizowaną pochodną modelu, użyj jej jako nowej pozycji ze swoim ID albo dostarcz oryginał. Przygotuj nowy folder roboczy poza wejściem.

~~~sh
python3 przygotuj.py --wejscie /dane/zrodla --projekt /dane/proba --postacie patryk gitara
~~~

2. Przygotuj wspólne źródła, wygeneruj zdjęcia i pliki Blender. Przy kolejnych iteracjach program wykorzysta zapisaną reprezentację tego samego źródłowego FBX. Nowy wynik skieruj do osobnego katalogu. Oryginały pozostają wejściem tylko do odczytu.

~~~sh
blender -b --python-exit-code 1 -P brax_animacje.py -- --plan /dane/proba/plan.json --cache /dane/cache --output /dane/wynik1 --photos --blend
~~~

3. Otwórz wynikowy plik Blender i wybierz akcję w Action Editor. Dla pełnego filmu z jednego lub kilku stanów użyj polecenia poniżej.

~~~sh
blender -b /dane/wynik1/patryk/patryk.blend --python-exit-code 1 -P podglad.py -- --ruch Walk Point --film /dane/patryk_chod_gest.mp4
~~~

Dodatkowe opcje dla brax_animacje.py obejmują `--fbx` do eksportu oddzielnego animowanego FBX postaci, `--characters gitara` i `--states Walk Point` do ograniczenia zadania. Opcja `--samples 0` mierzy każdą klatkę wyniku. Domyślnie program mierzy 9 faz, w tym początek i koniec. `--mode cache` tylko przygotowuje wspólne źródła. `--mode batch` korzysta z gotowych danych. Dostępne jest też bezpośrednie podanie pliku plan.json z własnymi ścieżkami do modeli i klipów.

## Dodanie kolejnych postaci

Nowe modele muszą mieć humanoidalny szkielet o nazwach Mixamo (opcjonalny prefiks z dwukropkiem), poprawną pozę wiązania i wagi siatki. Narzędzie nie tworzy szkieletu. Plik `nowe.json` określa źródłowe modele i role. Ścieżki FBX są względne wobec tego pliku albo absolutne.

~~~json
[
  {"id":"przechodzien_01","model":"modele/osoba_01.fbx","role":"tlum"},
  {"id":"bohater_02","model":"modele/osoba_02.fbx","role":"glowna"}
]
~~~

~~~sh
python3 przygotuj.py --wejscie /dane/animacje --nowe /dane/nowe.json --projekt /dane/nowa_partia
~~~

W obsada.json są przykładowe profile ról: `tlum` zawiera 6 stanów, `klub` 15, `glowna` odpowiada 50 przypisaniom Patryka razem z siłownią. Można skopiować profil pod nową nazwą, usunąć ruchy niepasujące do roli i wybrać inne warianty z katalogu. To szablony do selekcji, nie gotowy odbiór nowych postaci. Pole `window` wycina znormalizowany fragment osi czasu, np. sam koniec wstawania z dłuższej animacji. Obróbka działa w 30 kl./s z zachowaniem czasu trwania próbkowanego klipu.

## Co sprawdza pomiar

- Każdy model zachowuje własne długości kości, siatkę i wagi. Program przelicza rotacje do docelowych osi z uwzględnieniem kierunków kończyn i płaszczyzn dłoni. Przesunięcie bioder jest skalowane długością nóg.
- Wynik audit.json odnosi pomierzone długości krawędzi skórkowanej siatki do pozy wiązania. CHECK_STRETCH pojawia się, gdy najwyższy próbkowany 99. percentyl przekracza 1,5 albo pojedyncza krawędź przekracza 5 razy długość spoczynkową. To progi diagnostyczne tej wersji, także drobne szwy mogą je przekroczyć. VISUAL_REVIEW oznacza brak takiego przekroczenia w próbkach. ERROR opisuje błąd przetwarzania.
- W ujęciu przednim widać środek klipu. Ujęcie skośne pochodzi z próbki o największym 99. percentylu wydłużenia. W filmie kamera pozostaje stała dla całego klipu. Dla stanów zaczynających się od Point program kontroluje dodatkowo obie dłonie. Szuka palca wskazującego zgiętego maksymalnie o 30° przy średnim zgięciu palca środkowego i serdecznego co najmniej 40° oraz różnicy przynajmniej 25°. Raportuje układ jednej lub obu dłoni. Kierunek celu nadal wymaga oceny w scenie.
- Kierunek palca, znaczenie gestu, estetykę ubrań, kontakt z krzesłem lub kierownicą, ślizganie stóp i przejścia między stanami ocenia się w odtwarzaniu. Siatkę deformującą się na poprawnym szkielecie sprawdź pod kątem wag i geometrii. Źródłowe cele animacji, kontakt z rekwizytem, IK oraz przejścia Animator w Unity wymagają obejrzenia na docelowym prefabie. Oddzielnie sprawdź modele z pozy A, nietypowe łańcuchy, dodatkowe kości i animacje z przesunięciami innych kości niż biodra. Ta wersja przenosi rotacje kości i przesunięcie bioder. Wynik z Blendera i odtworzenie w Unity mogą się różnić.
- Jeśli chcesz współdzielić klipy w grze, ustaw osobny Avatar Humanoid dla każdego modelu i sprawdź palce w konfiguracji. Kopiowanie Avataru jest przeznaczone dla identycznego rigu. Dla porównania ruchu już wypieczonego na konkretnym szkielecie można zaimportować wynik jako Generic. [Unity 6, retargeting](https://docs.unity3d.com/6000.0/Documentation/Manual/Retargeting.html) · [Konfiguracja Avataru](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html)

## Próba 15 listopada

Źródła objęły 19 modeli, 354 przypisania podstawowe i 21 ćwiczeń z katalogu siłowni. Odtworzono 375 przypisań w Blenderze, zapisano 750 ujęć i wykonano 84 139 próbek siatki w pełnym przebiegu ruchów. Próg diagnostyczny przekroczyło 236 przypisań. 139 pozostało do oceny wizualnej bez alarmu rozciągania. Plik audyt_375.csv zawiera wszystkie stany, źródłowe FBX, sumy kontrolne i wartości pomiaru. Osobny eksport i ponowny import FBX sprawdzono na dwóch stanach kobiety w zielonej sukience. W próbie nie uruchamiano Unity ani APK.
