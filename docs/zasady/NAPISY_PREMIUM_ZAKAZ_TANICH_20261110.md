# ZASADA GLOBALNA: zakaz tanich napisów (właściciel, 10.11.2026)

Właściciel 10.11 10:17, po zdjęciu bramy mariny (cienka czarna rama, kremowe litery DejaVu stojące na górnej belce):
„kompletnie zakazuję takich tanich napisów” — „Globalnie, zakaz tanich napisów”.
10:23 o białych napisach w porcie po poprzednim wykonawcy: „to ma wszystko zniknąć”.

Dotyczy **wszystkich rejonów i wszystkich wykonawców**. Uzupełnia zasadę szyldów 3D z 9.11 („każdy szyld = bryła 3D”).

## Zakazane
1. `TextMesh`, `TextMeshPro` w świecie, tekst z czcionki bitmapowej (`LegacyRuntime.ttf`), napisy jako tekstura na płaskim quadzie, naklejki.
2. Napisy wiszące w powietrzu, stojące na cienkiej belce, ramce lub słupku, przyklejone do niczego.
3. Fonty systemowe w szyldach: DejaVu, Arial, Liberation, FreeSans, Helvetica i podobne.
4. Kremowy lub biały „świecący tekst” bez nośnika, kolor złoty tam, gdzie nie pasuje.
5. Etykiety techniczne w grze, np. „S04 | SKUTER” albo „PORT WYCIECZKOWCÓW | W01 / W02”. Pozycje stanowisk, debug i nazwy robocze nie są widoczne dla gracza.

## Wymagane
1. **Nośnik z architektury.** Napis jest zamontowany na bryle: portal, pylon, belka, attyka, fascia daszka, pas szyldowy, tablica z kamienia lub metalu, słupek prądu przy cumowaniu. Nie stoi na niczym cienkim.
2. **Litery 3D, pełne bryły:** front, boki i tył. Grubość: małe 2–5 cm, duże 7–15 cm, szyldy fasadowe zgodnie z zasadą 3D z 9.11. Odstęp od nośnika 1–3 mm jak przy literach dystansowych.
3. **Font premium z licencją w repozytorium.** W Mieście Mistrzów: Montserrat SemiBold, SIL OFL 1.1 (`unity-tools/mistrzowie/fonts/`, z polskimi znakami). Wielkie litery ze spacjowaniem (tracking). Nazwy marki jako lockup, np. BRAXHORN nad mniejszym podpisem funkcji.
4. **Materiał:** szczotkowana stal lub ciemny grafit, kontrast z tłem (stal na grafitowym pasie, grafit na białym). Złoto tylko tam, gdzie należy do marki.
5. **Światło jak w prawdziwym szyldzie:** linia LED pod literami, halo, downlighty. Litery czytelne o zmierzchu i w cieniu.
6. **Proporcje:** napis mieści się w nośniku z marginesami. Skaluje go kod, nie oko.
7. **Zdjęcie przed buildem.** Każdy nowy lub zmieniony napis trzeba pokazać na renderze z bliska i z 20–30 m.

## Wdrożenie w Mieście Mistrzów (10.11)
- **Bramy pomostów:** portal z białego marmuru, grafitowy pas szyldowy, lockup BRAXHORN / MARINA (MOTOR, WATERSPORTS, PROM, PORT WYCIECZKOWY) ze stali po obu stronach, LED pod literami (`BraxChampionsHarbour.Gates`, `build_harbour_signs.py`).
- **Budki K1–K12 i numery stanowisk:** ten sam font, stal na czarnej fascii, grafit na białych słupkach.
- **TextMesh w rejonie:** wszystkie wymienione na litery 3D. Dotyczy przystanków, drogowskazów areny, wejścia zawodników, szatni, gabloty pasów oraz numerów drzwi w apartamentach i hotelu. Test pilnuje, żeby w kodzie rejonu nie było `TextMesh`.
