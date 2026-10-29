# BRAXHORN CITY — Unity Asset Store: legalne darmowe zasoby do siłowni

Data przeglądu: 2026-10-29.

## Rzeczywisty stan

Asset Store jest zewnętrzną usługą kont Unity, **nie** pobraną biblioteką w repozytorium. Aby pobrać plik `.unitypackage`, właściciel musi przypisać ofertę do **My Assets** na swoim Unity ID. Następnie Unity Editor > Package Manager > My Assets > Download > Import. Bez dostępu do zalogowanego Unity ID nie wolno raportować, że pakiet został pobrany lub zainstalowany.

### Zweryfikowane darmowe propozycje

| Produkt | Oficjalny link | Cena (29.10.2026) | Dane producenta | Ocena względem BRAXHORN |
|---|---|---|---|---|
| Sports Equipment – Dumbbell, Pretorius Lab | https://assetstore.unity.com/packages/3d/props/sports-equipment-dumbbell-216505 | FREE | 5 prefabów, 4 tekstury 2048x2048, 4 części hantli, PBR, Built-in pipeline | Kandydat do testu w Unity; **nie pobrano, nie zaakceptowano wizualnie** |

### Odrzucone w obecnym zakresie

- `Cute Magic – Stylized Low Poly Interior Pack - Free`: https://assetstore.unity.com/packages/3d/props/cute-magic-stylized-low-poly-interior-pack-free-379394 — styl low-poly/cartoon, niezgodny z premium/realizmem BRAXHORN; na stronie deklarowany Built-in **niekompatybilny**, URP kompatybilny.
- `Gym Pack VOL. 2`: https://assetstore.unity.com/packages/3d/props/interior/gym-pack-vol-2-174498 — **płatny $6.99**; zawiera ławki i stojak, ale nie został zakupiony.
- `Modern Gym Equipment`: https://assetstore.unity.com/packages/3d/environments/modern-gym-equipment-352362 — **płatny $49.99**, nie został zakupiony.

## Instrukcja uzyskania pakietu (wymagana czynność właściciela konta)

1. Otwórz oficjalną stronę `Sports Equipment – Dumbbell` i zaloguj się na własne Unity ID.
2. Kliknij **Add to My Assets** (darmowy asset; bez kupowania dodatków).
3. W Unity 6 otwórz **Window > Package Manager > My Assets** i znajdź dokładnie ten pakiet.
4. Kliknij **Download**, a po pobraniu **Import** do **izolowanego projektu/gałęzi testowej**, nie do main ani wspólnych scen miasta.
5. Zachowaj pochodzenie i licencję; sprawdź modele, materiały, tekstury i działanie na telefonie. Po zatwierdzeniu odnotuj identyfikator pakietu, SHA zawartości i dowód gry.

Nie publikować samodzielnych źródłowych plików z Asset Store w publicznym GitHubie. Licencja standardowa dopuszcza wbudowywanie zasobów w grę zgodnie z EULA, nie automatyczną redystrybucję źródeł jako biblioteki.

## Oficjalne podstawy

- https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-store/downloads/packages
- https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/managing-packages-window/upm-ui-actions-ap/upm-ui-import
- https://unity.com/legal/as-terms

## Dalsze wykorzystanie

Dopóki nie otrzymamy prawdziwego pliku z `My Assets` lub oficjalnego dostępu do konta, **nie wpisywać Unity Asset Store assetów jako zaimportowanych do sceny**. W siłowni korzystamy z legalnie posiadanych oryginałów oraz własnych modeli wykonanych na potrzeby BRAXHORN.

Zakres gałęzi: `test/gym-premium-ceiling-recovery-20261029`. `main`, miasto, produkcja: bez zmian.
