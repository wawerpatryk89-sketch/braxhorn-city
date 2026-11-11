# ZASADA GLOBALNA: jak sprawdzać migotanie narzędziem (właściciel, 11.11.2026, 19:13)

Właściciel: „Na przyszłość zapisać mi to globalnie, żeby każdy wiedział, jak to robić.” Dotyczy **wszystkich rejonów i
wszystkich wykonawców**, którzy budują świat kodem. Uzupełnia `ZERO_MIGOTANIA_ZAKAZ_NAKLADANIA_20261110.md` (co jest
zakazane). Ta zasada mówi, **jak to sprawdzić**, zanim właściciel zobaczy podgląd albo zanim pójdzie build.

## Metoda (taka sama w każdym rejonie)
1. **Atrapa Unity.** Mały projekt .NET z podróbką tylko tych części Unity, których używa generator: Vector3,
   Quaternion, GameObject, Transform, Mesh, Material, kolidery i AssetDatabase jako no-op. Komponenty gry (windy, drzwi,
   kino, schody ruchome) są stubami, które tylko zapisują swoje pola.
2. **Prawdziwy kod bez zmian.** Do projektu dołącza się **te same pliki .cs**, które idą do builda, przez
   `<Compile Include="../../../unity-city/Assets/Editor/...">`. Nie przepisuje się ich, nie kopiuje, nie upraszcza.
3. **Zrzut geometrii.** Każdy zbudowany mesh trafia do JSON: nazwa obiektu, materiał, wierzchołki i trójkąty, w metrach
   świata.
4. **Test par.** Szukane są powierzchnie zwrócone w tę samą stronę, równoległe, nakładające się (część wspólna > 0)
   i w **różnych materiałach**. Progi:
   - **z zewnątrz: odstęp < 2 cm** (telefon, patrzenie z daleka);
   - **wewnątrz: odstęp < 12 mm**, czyli nakładka ma stać co najmniej 12 mm nad powierzchnią albo dostać własną bryłę;
   - pomijane: powierzchnie dociśnięte do siebie przeciwnie zwrócone (niewidoczne) i ten sam materiał.
5. **Wynik = 0 par z powierzchnią większą od zera** przed każdym podglądem dla właściciela. Styki samych krawędzi
   (0,00 m²) można zgłosić jako uwagę.
6. **Zdjęcia z tego samego zrzutu.** Podgląd renderuje się z JSON-u, więc pokazuje dokładnie to, co zbuduje Unity.
7. **Duże sceny dzieli się na pasy** (np. 4 pasy po osi X z zakładką 5 m), bo cały zrzut może nie zmieścić się w pamięci.
   Siedzenia kinowe, towar w sklepach i podobne drobne krzywe obiekty można wyłączyć z testu, jeśli nie stykają się płasko
   z niczym.
8. **Pokrycie terenu.** Tym samym zrzutem sprawdza się, czy każdy punkt działki ma pod sobą powierzchnię (siatka co
   0,5 m). Dziura w terenie to taki sam błąd jak migotanie.

## Typowe przyczyny i poprawki (z kontroli G1, 11.11: 146 miejsc → 0)
- Okładzina lub listwa w płaszczyźnie krawędzi stropu. Okładzina stoi przed krawędzią, a nie w niej.
- Cokół lub płyta na tej samej wysokości co posadzka. Cokół kończy się 12 mm niżej albo poza posadzką.
- Słupek ramy w płaszczyźnie końca ściany. Ściana kończy się 1–2 cm za linią ramy.
- Grzebień schodów ruchomych na wysokości posadzki. Grzebień i płyta stoją na posadzce, a stopnie są 12 mm pod nią.
- Litery 3D muszą być zwrócone od ściany. Kierunek: (sin yaw, 0, cos yaw) od punktu bazowego.

## Gdzie są narzędzia
- **Miasto Mistrzów:**
  - `unity-tools/mistrzowie/*_preview/` (atrapy i projekty: `apartments_preview`, `d2_preview`, `g1_preview`,
    `hotel_sobczak_preview`);
  - test par: `apartments_preview/zfight.py`;
  - render: `hotel_sobczak_preview/render3d.js`.
- **R1:** narzędzie kolegi opisuje jego dokument w projekcie, przekazywany przez właściciela (zasada komunikacji rejonów).
