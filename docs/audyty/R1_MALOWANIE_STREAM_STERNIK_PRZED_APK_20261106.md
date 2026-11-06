# BRAXHORN CITY R1 — malowanie, render z daleka i sternicy przed APK (06.11.2026)

**Obszar:** supermarket spożywczy BRAXHORN → wulkanizacja / warsztaty / boiska → port i plaża → cała wyspa z areną. **Silnik:** Unity 6, Android.

**Zasada właściciela:** nowe nawierzchnie pozostają, stare POD nimi są wycinane (łącznie z odpowiadającą im fizyką), nigdy nie dodawać drugiej płyty w celu przykrycia migotania. Jakość sceny i kolizji = RAPORT, nie blokada kosztownego APK. Twarde błędy kompilacji i wymagane pliki źródłowe pozostają rzeczywistymi usterkami.

## Zmiany źródłowe, które sprawdzono statycznie

- Dalsza sylwetka wyspy używa tych samych czterech wag i tego samego materiału PBR co teren rzeczywisty. Po faktycznym wczytaniu `BraxhornCity_Wyspa` znika proxy, a przy zwolnieniu sceny wraca. Nie ukrywamy wyspy dlatego, że gracz przekroczył sam próg odległości.
- Doczytanie obszarów w projekcie: wyspa 350 m od obrysu obszaru; nabrzeże 175 m (zgodnie z aktualnymi argumentami funkcji Wydziel). Wymaga pomiaru szczytowego RAM na telefonie.
- Ogrodzenia boisk: kontroler LOD dobiera oryginalne LOD0 w odległości do 360 m. Nie zastępuje wyglądu czarnym prostokątem; nie jest to jednak dowód wyglądu w uruchomionej grze.
- Migotanie: analiza współpłaszczyznowych powierzchni, wycięcie STAREJ siatki pod NOWĄ w obszarze x=-168…72, z=-138…108 oraz porcie / plaży x=63…187, z=-140…185. Zmodyfikowany MeshCollider jest aktualizowany, gdy należy do przyciętego mesha. Limit operacji 32 s / 120 siatek na miejskim przebiegu, pozostałe usterki = ostrzeżenia.
- Jacht i motorówka: kamera z pozycji sternika. Skuter: Brax siedzący na siodle (TPP). Punkty kontaktu dłoni pochodzą od istniejących węzłów GLB, a jeśli ich brak, program zgłasza ostrzeżenie o niezweryfikowanej kierownicy. IK działa bez translacji i skalowania kości.

## Lekka kontrola przedbuildowa

Statycznie sprawdzono 12 powiązań źródłowych: wycinanie starego materiału, obszar sklepu/wulkanizacji, port/plażę, raportowy limit wycinania, spójny materiał PBR wyspy, przełączanie po doczytaniu sceny, LOD boisk, uchwyty kierownic, kamery jachtu/motorówki, IK dłoni, wcześniejsze ładowanie dwóch obszarów oraz ręczny tylko workflow build. Wszystkie 12 powiązań obecne na gałęzi R1; dodatkowo 12 kluczowych plików C# ma sparowane nawiasy i literały według kontroli strukturalnej.

**NIE jest to kompilacja Unity ani test Androida.** Nie wykonano rejsu w rzeczywistej scenie, nie potwierdzono braku każdej pary migających powierzchni i nie potwierdzono, że wszystkie modele łodzi mają identyfikowalne kierownice.

## Ręczny APK — nie zmieniać na push

Workflow `.github/workflows/claude-region-build.yml` ma uruchomienie `workflow_dispatch` z opcją **rejon=miasto**, **pamiec=true**. Link:
https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/workflows/claude-region-build.yml

Oczekiwane nazwy logów: `BRAXHORN_MIGOTANIE`, `BRAXHORN_SILOWNIA_MIGOTANIE`, `BRAXHORN_R1_BOISKA_SIATKA`, `BRAXHORN_WYSPA_STREAM`, `BRAXHORN_WYSPA_WIDOCZNOSC`, `BRAXHORN_STER_KAMERA`, `BRAXHORN_POSE`, `WYSPA_CALA_TRASA_AUDYT`. Wartości bez pokrycia źródłem nie mogą być oznaczane jako 0.

## Test na telefonie po zbudowaniu

Przejść i przepłynąć ciąg supermarket → wulkanizacja → boiska → port → promenada i plaża → jacht/skuter/motorówka → wyspa/arena. Obserwować PBR i migotanie przy zbliżaniu do boisk, ogrodzeń, dachów, krawężników i plaży. Kontrolować wejście z molo na jacht, wciśnięcie STERUJ, realny widok koła sterowego, ręce Braxa na uchwytach skutera i motorówki. W razie błędu raportować współrzędne / miejsce i klatkę ekranu, nie dokładać nowego podkładu do zatuszowania problemu.

**Status na dzień 06.11.2026:** poprawki w GitHub, kontrola struktury i powiązań kodu; APK ani Unity Play Mode nieuruchomione w tej kontroli.
