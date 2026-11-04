# Obóz: zabudowa treningowa — etap 1

Zmiana po odrzuconym wizualnie APK obozu #4 (`41812655634`), baza
`7908ed31b290a21ea6a4b449eddfcd7e4b914a8a`. Stan tego etapu: implementacja
w źródłach do przeglądu, **bez kompilacji i oględzin nowej sceny w Unity**.

## Zmienione zachowanie

- Generator nie wstawia starej siłowni, jej wnętrza, drzwi ani kolizji.
  Zachowuje pliki źródłowe i ich GUID. Na jej miejscu powstaje otwarty pawilon
  z Ringiem 2 i czterema zadaszonymi workami. Łącznie pozostają dwa ringi,
  oktagon, 24 worki (12 pod zadaszeniem), cztery domki i siedem postaci.
- Sześć nowych stanowisk: mobilność, drążki, poręcze, skrzynie, dwa tory sań
  i liny. To wyposażenie z geometrią/kolizją; animacje ćwiczeń na tych
  stanowiskach nie są zaimplementowane.
- Widoczne ogrodzenie zastępuje niewidzialną granicę. Brama przesuwna i
  furtka otwierają się po zbliżeniu gracza. Droga, plac do zawracania,
  zatoka, dojścia piesze, wiata, ławki i oświetlenie tworzą strefę przyjazdu.
  Nie dodano autobusu ani automatycznej jazdy. Obecny sterownik bramy
  rozpoznaje gracza, nie pojazd.
- Domyślna kamera obozu: 3,5 jednostki zamiast 5,8; cel na 62% wysokości
  kapsuły Moniki. FOV pozostaje 58. Nie zmieniono modelu, wzrostu, animacji
  ani sterowania. Rzeczywisty kadr wymaga kontroli w Unity.
- Drzewa i trawa omijają nową zabudowę/dojazd. Nie poprawiano w tym etapie
  widocznych na nagraniu problemów kart roślinności.

Układ i wymiary generatora używają dotychczasowej skali świata `K=1.294`.
Wymiary podane w planie są przed tym przeskalowaniem. Nie zmniejszono całej
mapy: granica obozu pozostaje przy ±150; teren zewnętrzny jest szerszy.

## Materiały i zachowanie zasobów

Pobrane i zweryfikowane: osiem map 4096×4096, 95 384 189 bajtów, CC0:
[Clean Asphalt](https://polyhaven.com/a/clean_asphalt) i
[Concrete Pavers](https://polyhaven.com/a/concrete_pavers).
`public-materials.json` zawiera dokładne adresy, rozmiary i SHA-256.
`public_materials.py` odtwarza je przed importem Unity; błędne bajty nie
zastępują istniejącego pliku. Repo przechowuje metadane, nie kopie JPEG.
Import Androida ustawia 4K, mipmapy, anizotropię 8 i ASTC 4×4.

Nowe mapy wchodzą do identyfikatora wejść generatora. Rejestr uwzględnia
`CampusLayout.json`. Nie zmieniono wspólnych narzędzi cache, podpisu,
pakietu APK ani `build-request.json`. Nie wprowadzono Addressables,
wspólnego loadera ani hostingu małych aktualizacji.

## Dowód i granice kontroli

- 29 testów Pythona narzędzi obozu: PASS, w tym cztery testy dostawy
  publicznych materiałów (integralność, zachowanie metadanych, błędne dane).
- Wszystkie osiem faktycznie pobranych map zgadza się z manifestem SHA-256.
- Parser gramatyki: 24 pliki C#, bez błędów. YAML i składnia 17 kroków Bash
  workflow poprawne; `git diff --check` bez błędów.
- Geometryczny obrys pojazdu 12×2,6 na promieniu 12 mieści się między
  wyspą R6 i placem R18: około 4,7 do wyspy i 3,41 do zewnętrznej krawędzi.
  To wyliczenie prostokąta, **nie symulacja skrętu, najazdu ani przejazdu**.
- Walidator Unity został rozszerzony o sześć stanowisk, dwie podłączone
  bramy i zakaz instancji starej siłowni. Nowe warunki nie były jeszcze
  wykonane w edytorze. Sam parser nie kompiluje C# ani shaderów.

W tym środowisku nie ma Unity. Nie ma zrzutów nowej sceny, pomiaru FPS/RAM,
nowego APK ani testu telefonu. Nie ma podstaw, by uznać wygląd za odebrany
lub nazwać ten etap gotowym wydaniem premium. Fort, tartak i część pozostałej
zabudowy wciąż wymagają osobnych prac; kotwice nie są gotowymi budynkami.

## Odbiór w Unity przed kolejnym APK

1. Odtworzyć komplet prywatnych źródeł obozu dotychczasową szyfrowaną
   ścieżką i uruchomić `python3 unity-tools/oboz/public_materials.py`.
2. W Unity 6000.0.58f2 wykonać `BraxObozEnvironment.Prepare`, następnie
   `BraxObozActors.Assemble`; sprawdzić wynik `BraxObozValidation.Check`.
3. Obejrzeć Monikę przy nowym kadrze, tablice, powierzchnie, pawilon,
   wszystkie stanowiska, bramę, furtkę i dojazd. Przejść otwory i schody,
   sprawdzić zamykanie bram oraz krawędzie kolizji. Zweryfikować import map
   i pamięć telefonu; sama rozdzielczość 4K nie potwierdza jakości sceny.
4. Oddzielnie sprawdzić obrys, promień skrętu i ruch rzeczywistego autobusu
   oraz podłączyć jego wykrywanie do bramy po wdrożeniu pojazdu.

Następny uzgodniony krok: audyt deformacji Moniki, VORNA, ZYRO, RAKKA,
KADE, GORRA i Misia w spoczynku, chodzie i biegu. Ten etap nie zawiera
naprawy rigów ani potwierdzenia, że wszystkie siedem postaci jest poprawnych.
