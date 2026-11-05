# Brązowe realistyczne worki bokserskie — status i wymagania

**WYNIK WERYFIKACJI: WŁAŚCIWY MODEL BRĄZOWY NIE ZOSTAŁ JESZCZE ZIDENTYFIKOWANY ANI DOSTARCZONY.**

1. W Obozie #18 działa generator 24 worków (12 pod zadaszeniem). Kod: `unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs`, metoda `Bags()`, obiekty `BraxWorek`; modele tworzone programowo, nie z osobnego GLB.
2. W `unity-tools/oboz/private-inputs.json` NIE występuje plik realistycznego brązowego worka. `animations/krov/Punching Bag.fbx` to **animacja**, nie siatkowy model worka.
3. W dotychczasowym raporcie siłowni BRAXHORN odnotowano dawny `build-source/BoxingBagSource.glb`, źródło [Innerscene — Boxing Gloves and Bag](https://www.innerscene.com/tools/library/3d-parts/boxing-gloves-and-bag-e3d766bc). Nie znaleziono go w obecnej paczce Obozu; wygląd/kolor oraz dostępność oryginalnego pliku wymagają osobnej weryfikacji. Nie przedstawiać go jako potwierdzonego brązowego worka.
4. Najnowsze 10 dostarczonych GLB NIE zawiera worka. `model(1).glb` to stojak na piłki lekarskie.

### Zasady przyszłej zamiany (jeszcze NIE wdrażać)
- Najpierw obejrzeć zaakceptowany przez właściciela model **brązowy**, skórzany, realistyczny, z dobrym PBR, szwami i zawieszeniem.
- Podmienić siatki worków w 24 istniejących punktach treningowych, zachowując sensowne proporcje (wysokość, odległość od ziemi i haków) oraz realistyczne mocowanie.
- Zachować i przetestować `BraxWorek`, uderzenia, kołysanie, sztywność/wysokość zawieszenia, kolizje fizyczne i dźwięk, a nie zastępować mechaniki samą dekoracją.
- Przy przenoszeniu stref po zmniejszeniu obozu aktualizować pozycje i nawigację NPC oraz testować cały nowy układ.
- Po otrzymaniu modelu sprawdzić jego licencję przed ewentualnym opublikowaniem binarnej kopii w publicznym repozytorium.
- Przed zatwierdzeniem wymiany dostarczyć widok worka w Unity i krótki test trafienia na telefonie.

**Status: oczekuje na model / akceptację właściciela. Niczego nie podmieniono.**
