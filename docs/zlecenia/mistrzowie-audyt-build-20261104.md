# Miasto Mistrzów — audyt blokad buildu, 4 listopada 2026

## Werdykt

**Nie ma jeszcze podstaw do stwierdzenia, że cały build przejdzie.** Zidentyfikowano i poprawiono błąd uruchamiania generatora oraz niezależny problem trasy walidacyjnej w arenie. Kontrole Python i opisane niżej kontrole geometrii przeszły. Nie uruchomiono nowego Unity/Android APK; nie wykonano testu telefonu. To audyt ścieżki budowania i jej zależności, nie odbiór całego planu miasta.

Bazowy kod gry: `6f799b67994e1afa430906a04817d7df447a9678`.
Gałąź: `codex/miasto-mistrzow-20261104`.
Workflow nieudanego przebiegu pochodził z `main`, SHA `46309b5f9c78dc1ef9a750c92843806afe5eb1bc`; jest to inny identyfikator niż kod gry.
Unity: `6000.0.58f2`, Android ARM64/IL2CPP, OpenGLES3.

Dowód poprzedniej awarii: [run #2](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41889411877), [job i log](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41889411877/job/126782499232).

## 1. Potwierdzona awaria sceny

`BraxChampionsBuild.BuildInternal` tworzył pustą niezapisaną scenę Single, po czym `BraxChampionsFoundation.Generate` wywoływał NewScene Additive. Log zatrzymał się na:

`InvalidOperationException: Cannot create a new scene additively with an untitled scene unsaved.`

Poprawka w Foundation:
- Przy dokładnie jednej załadowanej, pustej i niezapisanej scenie generator wykorzystuje tę scenę.
- Przy zapisanych scenach zachowuje wcześniejszą ścieżkę Additive i przywracanie aktywnej sceny.
- Niezapisana scena z obiektami powoduje czytelny błąd przed generowaniem materiałów; żadne obiekty użytkownika nie są usuwane.
- Reużywana scena nie jest zamykana w finally; na sukces zapisuje się pod istniejącą docelową ścieżką, na błędzie zostaje do inspekcji.
- Ochrona istniejącej sceny Foundation/GUID/ręcznych zmian pozostaje.

Mechanizm wynika bezpośrednio ze stosu i kodu. Poprawka wymaga jeszcze sprawdzenia w Unity.

## 2. Druga blokada: prosta trasa przez tył schodów trybun

Przejrzano `CheckExteriorAccess`, `BraxChampionsArena.CopiedEstateArena`, `TierStairs` i rzeczywiste tworzenie BoxCollider przez `BraxWnetrzaKit`.

Stara trasa od (600, 740.7) do (600, 800) przechodzi przez środek schodów trybun. Po obrocie hali o 180 stopni i zastosowaniu K, tył południowych schodów wypada przy nominalnym Z=768.2. Schody mają 32 stopnie po 0.2 m, czyli 6.4*K ≈ 8.284 jednostki wysokości, a ich szczyt Y≈8.569. Walidator idący od wejścia trafia w wysoką tylną ścianę; nie jest to próg do pokonania.

Niezależna kontrola rzutu tych kolizji, próbkowanie co najwyżej 0.25 jednostki, promień 0.35: stara trasa ma 171 trafień próbka–bryła, pierwsze około (600, 768.040); skorygowana trasa nie przecina tych brył.

Nowa trasa w nominalnych współrzędnych:
(600,740.7) → (600,766.5) → (602,766.5) → (602,787) → (600,787) → (600,800).

Przechodzi przez dotychczasowe drzwi i bok centralnej alejki, następnie wraca na dojście do oktagonu. Zmieniono wyłącznie punkty testu, nie układ, bryły, schody, materiały ani kolizje. Kontrole Physics.Raycast, wysokości i CheckCapsule nadal są obowiązkowe. Kontrola analityczna dotyczy schodów; **nie zastępuje Physics całej sceny**.

## 3. Wyniki wykonanych kontroli

| Zakres | Wynik i ograniczenie |
|---|---|
| 23 testy Python | PASS: 6 dostawy APK, 6 wyboru/weryfikacji PBR, 6 przenoszenia Patryka, 5 geometrii powierzchni. Testy dostawy korzystają z atrap usług; nie dowodzą uprawnień zdalnego workflow. |
| Składnia Python | PASS, 20 plików narzędzi Miasta Mistrzów. |
| Generowanie Foundation | PASS: 100 siatek, 95 668 trójkątów, 287 roślin; skończone współrzędne, UV, winding, rezerwy mariny i nasadzenia. Miejsca na jednostki nie są wstawionymi jachtami. |
| Infrastruktura | PASS: 278 siatek, 715 616 trójkątów, 3 stacje i otwory, 2 tory metra, 2 tramwajowe, 10 platform tramwaju, 32 autobusowe, 69 przejść. |
| Kontrakt metra | PASS: skala i rozstawy zachowane, 6 końców źródłowych tuneli na prostych połączeniach, otwory przez teren, brak działek na otworach. |
| A1/T01 | PASS geometrii: A1 LOD0/LOD1 34 722/12 182 trójkątów, T01 97 206/22 514; geometria w rezerwach działek i zadanych wysokościach. |
| Szyldy 3D | PASS: 2 szyldy, 28 382 trójkąty, 5 gwiazd hotelu; pełna rzeźba byka nadal oznaczona SOURCE_MODEL_REQUIRED. |
| Grafiki areny | Wykonano istniejący generator: 6 grafik. Nie zmieniono projektów. |
| Podłoże dojść i startu | 677 próbek na rzeczywistych wygenerowanych trójkątach Foundation/Infrastructure/Exteriors: 383 do areny, 234 przez ulicę do hotelu, 33 pod wejście hotelu, 27 przy starcie gracza. Brak luki i skoku ponad limit 0.3; nie jest to pełny test kapsuły. |
| Przygotowanie źródeł w poprzednim CI | Krok Geometry and source preparation w run #2 zakończył się sukcesem. Dotyczy również prywatnych paczek, znaków i areny. Nie pobierano ponownie wszystkich prywatnych źródeł lokalnie. |
| C# przed poprawkami | Poprzedni run dotarł do wykonywania BuildAndroid, więc jego skrypty przeszły kompilację. Zmieniony C# nie był ponownie kompilowany tutaj. |
| Nowy APK/telefon | NIEURUCHOMIONE, niezweryfikowane. |

Lokalny `prepare_signage.py` nie został wykonany do końca, ponieważ diagnostyczny checkout nie zawiera binarnego `metro_atlas.png`. Nie jest to dowód braku pliku w repo ani nowej awarii CI: cały ten krok przeszedł w run #2. Generator szyldów 3D i generator grafik areny przeszły osobno.

## 4. Dalsza ścieżka — sprawdzony kod i pozostałe bramki

- Materiały: generator używa nazw zgodnych z prepare_assets, w tym jawnego pliku fabric_pattern_05_diff_4k.jpg zapisywanego z col_01 dostawcy. Poprzednie pobranie przeszło. Kolejny run nadal zależy od dostępności API/CDN; cache nie usuwa zapytań o metadane. Nie zastąpiono tekstur ani ich rozdzielczości.
- Metro: używany jest istniejący generator i modele, wymagane 7 bramek z 4 skrzydłami i 2 napędy schodów. Skontrolowano przeliczenie pozycji napędów przy obrocie stacji. Fizyczny spacer i zachowane referencje po zapisie nadal wymagają Unity.
- Budynki: importer odczytuje gzip JSON, role materiałów, LOD i siatki kolizji. Zapisuje prefaby, a potem scenę. Nie zezwala na nadpisanie istniejącego katalogu budynków. W bazowym drzewie repo nie ma wygenerowanej sceny Mistrzowie, więc czysty checkout nie trafia w tę ochronę.
- Patryk: model, mapy 4K, wymagane różne klipy Walk/Run, pozycja stóp i zapis klipów są sprawdzane w kodzie. Retargeting ma twardą blokadę nieprawidłowych stóp. Nie osłabiono jej. Wynik na rzeczywistych modelach/animacjach nie jest jeszcze znany. Ostrzeżenie testu kości nie jest automatycznie blokadą — przejście buildu nie oznacza braku deformacji.
- Scena eksploracji: używa Foundation, dodaje jednego gracza/kamerę, zapisuje osobną Explore, ponownie otwiera i sprawdza referencje, 7 klipów oraz 5 punktów. Potem APK zawiera Start + Explore, nie drugą kopię Foundation.
- Android: stały alias braxhorn, package com.braxhorn.mistrzowie, versionCode z numeru i próby workflow, ARM64 IL2CPP i GLES3. To osobny pakiet eksploracyjny zgodnie z obecną konfiguracją, nie dowód integracji regionów ani aktualizacji zawartości bez pełnego APK.
- Dostawa: stage weryfikuje pakiet, wersję i certyfikat; artefakt jest przed release i końcowymi raportami. Dwie drogi dostawy są niezależne. Rzeczywisty stage potrzebuje aapt, apksigner i keytool na runnerze; etap nie był osiągnięty w #2. Uprawnienia publikacji i ilość wolnego miejsca nie są potwierdzone dla nowego przebiegu.

## 5. Co musi wykonać zatwierdzony przebieg Unity

1. Start z jednej pustej niezapisanej sceny → Foundation zapisane bez wyjątku Additive.
2. Weryfikacja ręcznego użycia: zapisana scena z obiektami pozostaje nietknięta; niezapisana z obiektami daje wczesny błąd bez utraty obiektów; istniejące Foundation nie jest nadpisywane.
3. Wszystkie trzy stacje: bramki, napędy, spacer i ponowny odczyt sceny.
4. A1/T01 i skorygowana droga do oktagonu: rzeczywiste raycasty i kapsuły, bez pomijania testów.
5. Patryk: retarget, kontrola stóp, zapis/ponowne otwarcie Explore.
6. Kompilacja APK i potwierdzenie co najmniej jednej rzeczywistej dostawy.
7. Oddzielnie na Androidzie: start, wejście do gry, ruch, obraz, pamięć i FPS.

Brak błędu w wykonanej części audytu nie jest dowodem braku błędów w nieosiągniętych etapach.

## 6. Uruchomienie i zakres zmian

Zmieniono Foundation i trasę CheckExteriorAccess. Nie zmieniano wyglądu, geometrii, jakości materiałów, zasobów, animacji, innych regionów ani main. Nie uruchomiono i nie anulowano workflow.

Formularz z main nadal ma stary domyślny commit 47c89f0…: pozostawienie go uruchomi starą wersję. Dla poprawionej wersji trzeba jawnie podać SHA końcowego commita tej naprawy albo wybrać gałąź codex/miasto-mistrzow-20261104 i puste pole commit. Nie używać Re-run starego przebiegu jako testu nowego kodu.

[AGENTS.md](../../AGENTS.md): „Nowe buildy tylko po osobnej zgodzie właściciela”. Polecenie naprawy i audytu wykonano bez samoczynnego nowego buildu.
