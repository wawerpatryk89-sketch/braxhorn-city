# Postacie BRAXHORN CITY

Jedna postać ma stały identyfikator i osobny pakiet z modelem, teksturami i ruchami dopasowanymi do jej szkieletu. Kolejne wersje zachowują identyfikator. Rejon wskazuje wersję w swojej obsadzie. Postać pojawia się tylko w wybranych rejonach. Patryk z 13 listopada jest przeznaczony do Osiedla.

Właściciel przekazuje po jednej postaci, określa jej rolę i rejony. Każda postać ma spokojne stanie, chód, bieg, siadanie, odpoczynek na siedząco, wstawanie, rozmowę i gesty. Dalsze czynności dobiera się do roli, m.in. picie, taniec, pływanie, przejścia między wodą a lądem, obsługę auta, łodzi lub skutera, obsługę baru, tacę kelnerską i recepcję. Właściciel wskazuje postacie uczestniczące w bójkach. Zwykli mieszkańcy dostają podstawowy zestaw uliczny. Zawodnicy ligi otrzymują indywidualne style.

Walcząca postać zaczyna w zwykłym spoczynku. Dopiero potwierdzone trafienie albo decyzja postaci o własnym ataku uruchamia stan walki. Samo zbliżenie, wybór celu, próba bloku i próba uniku nie uruchamiają gardy. Po zakończeniu bójki postać wraca do zwykłego spoczynku.

Wspólne archiwa źródeł A/T służą do doboru ruchów. Przed użyciem sprawdza się pozę, nazwy kości, oryginalne wiązanie siatki i przebieg ruchu na konkretnej postaci. Paczka, pełna ścieżka i suma SHA-256 identyfikują konkretny plik. Porównuj przebieg ruchu kości, zwłaszcza przy identycznych nazwach. Warianty ruchu zachowuj osobno. Sama nazwa lub dopisek (1)/(2) nie rozstrzyga zgodności. Podgląd w Blenderze i odbiór w scenie Unity zapisuje się oddzielnie. Czynności z brakującym źródłem są otwarte.

Dla każdego modelu prywatny pakiet ma katalog postaci z plikiem postac.json. Wersję oraz sumę SHA-256 manifestu zapisuje się w obsadzie. Lokalne źródła Unity znajdują się pod Assets/Source/Prywatne/Postacie/<id>. W CI służy do tego zaszyfrowana paczka postaci. Surowe FBX pozostają prywatne.

Dla Patryka z 13 listopada CI przyjmuje również oryginalny plik `Patryk_globalny_13-11_Unity.zip` dodany jako załącznik do prywatnego szkicu `postacie-globalne-20261112`. Wydanie musi pozostać szkicem. Przed importem sprawdzane są SHA-256 ZIP-a, manifestu oraz plików modelu, tekstur i ruchów. Identyfikator wydania i oczekiwane sumy zapisuje obsada Osiedla.

Gitara (gitara, 2026-11-14) ma 20 klipów i pięć ciosów. Szablon ruchów dla tej postaci tworzy polecenie edytora `Braxhorn/Postacie/Przygotuj Gitarę` po instalacji prywatnej paczki `Gitara_globalny_Unity.zip`. Rejon wybiera właściciel.
