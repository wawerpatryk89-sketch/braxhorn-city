# ZASADA: piasek zapada się pod nogami i zostają ślady (10.11.2026)

## Polecenie właściciela

- 15:59: „Piasek na plaży ma być zapadający się pod nogami, zostawiając ślady, jak idę po nim. To jest globalnie na każdą plażę.”
- 16:39, na pytanie o czas: „Znikają po ok. 1 min”.

## Co ma być, w każdym rejonie z plażą

1. **Stopy zapadają się w piasku.** Kiedy postać stoi na piasku, jej model schodzi 4 cm w dół. Wchodzi w piasek i wychodzi z niego łagodnie (ok. 0,16 s). Na kostce, deskach i trawie nie zapada się wcale.
2. **Ślad po każdym kroku.** Stopa schodzi na ziemię, wykrywamy to jak dźwięk kroków w `BraxKroki`: kość stopy opada po uniesieniu. Wtedy w miejscu stopy powstaje prawdziwy ślad 3D:
   - zarys buta 27 × 10 cm;
   - lewa i prawa stopa są odbite lustrzanie;
   - ślad jest obrócony w kierunku marszu i dopasowany do pochylenia piasku.
3. **Bez nakładania i migotania** (ZASADA-ZERO-MIGOTANIA):
   - podeszwa z ubitego, ciemniejszego piasku leży 10 mm nad powierzchnią;
   - wokół niej jest wał wypchniętego piasku wysoki na 35 mm;
   - zewnętrzny stok wału zaczyna się 10 mm **pod** piaskiem;
   - nic nie leży na piasku w odstępie 1–5 mm i nie ma przezroczystych naklejek.
4. **Znikanie po ok. 1 min.** Przez 50 s ślad stoi. Przez kolejne 10 s osiada w piasek, czyli schodzi pod powierzchnię bez przezroczystości. Potem wraca do puli.
   - Najwyżej 160 śladów, czyli około minuty marszu.
   - Wszystkie ślady mają jedną wspólną siatkę i dwa materiały: piasek i ubity piasek.
5. **Co jest piaskiem.** Collider albo materiał, który w nazwie ma „sand”, „piasek” lub „piask”, na przykład `sand_dry` i `sand_wet` z fundamentu Miasta Mistrzów.

## Wykonanie

- **Miasto Mistrzów:** `unity-city/Assets/Mistrzowie/BraxChampionsSandSteps.cs`.
  - Komponent sam dopina się do `BraxGymPlayer` po wczytaniu sceny, jak `BraxKroki`.
  - `BraxGymPlayer` się nie zmienia.
  - Przesuwany jest tylko `visual` gracza, nigdy obiekt z CharacterController.
- **Pozostałe rejony** (R1 miasto, wyspa, osiedle, obóz): każdy wykonawca dokłada ten sam komponent u siebie, kopią albo przeniesieniem do wspólnego katalogu. Musi pilnować, żeby jego piasek miał w nazwie collidera albo materiału „sand” albo „piasek”. Rejonu nie wolno zmieniać spoza jego gałęzi.

## Odbiór

1. Zdjęcie z podglądu przed buildem: ścieżka śladów na plaży, z góry, z oczu i z bliska.
2. W grze (właściciel):
   - chodzenie po plaży zostawia ślady, a postać jest trochę zapadnięta;
   - po minucie ślady znikają;
   - na kostce nie ma śladów ani zapadania.
