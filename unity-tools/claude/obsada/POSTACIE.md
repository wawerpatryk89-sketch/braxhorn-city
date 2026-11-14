# Postacie BRAXHORN CITY

Jedna postać ma stały identyfikator i osobny pakiet z modelem, teksturami i ruchami dopasowanymi do jej szkieletu. Kolejne wersje zachowują identyfikator. Rejon wskazuje wersję w swojej obsadzie. Postać pojawia się tylko w wybranych rejonach. Patryk z 13 listopada jest przeznaczony do Osiedla.

Każda postać otrzymuje ruchy codzienne. Obejmują spoczynek, chód, siadanie na krześle, siedzenie, wstawanie, picie z puszki i taniec w klubie. Przygotowanie kolejnych czynności wynika z roli podanej przez właściciela. O przypisaniu walki decyduje właściciel osobno dla każdej postaci. Walczący otrzymuje dodatkowo przynajmniej trzy różne ciosy ręką, trzy różne kopnięcia, uniki i reakcje na trafienia.

Wspólne archiwa źródeł A/T służą do doboru ruchów. Przed użyciem sprawdza się pozę, nazwy kości, oryginalne wiązanie siatki i przebieg ruchu na konkretnej postaci. Podgląd w Blenderze i odbiór w scenie Unity zapisuje się oddzielnie. Czynności z brakującym źródłem są otwarte.

Dla każdego modelu prywatny pakiet ma katalog postaci z plikiem postac.json. Wersję oraz sumę SHA-256 manifestu zapisuje się w obsadzie. Lokalne źródła Unity znajdują się pod Assets/Source/Prywatne/Postacie/<id>. W CI służy do tego zaszyfrowana paczka postaci. Surowe FBX pozostają prywatne.
