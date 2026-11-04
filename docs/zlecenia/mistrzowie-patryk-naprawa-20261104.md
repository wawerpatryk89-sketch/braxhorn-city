# Patryk: zachowanie oryginału i poprawka ruchu — 4.11.2026

Zakres: `codex/miasto-mistrzow-20261104`, baza `e6850779deeb2f208d971143c10531c5dc7d21a5`.
Nie zmieniono gałęzi osiedla ani miasta. Materiał użytkownika pokazuje osiedle;
nie znamy SHA aplikacji z nagrania. Poprawka dotyczy przeniesionej implementacji
Patryka w Mieście Mistrzów.

## Potwierdzone fakty

- Dostarczone `Patryk.zip`: SHA-256 `52d7f58de8ea38f269a3d5c2b24b9667c6c8a7eb36d3784ad0366f0f3ac44b47`.
- Oryginalny FBX: `fb8e2630bc7f835a743d917c35c378c7d074f0fde993a0bbf864b31ab67e1d97`, zgodny z kontrolą importera.
- Oryginały Color, Normal, roughness i metallic mają po 4096 × 4096 pikseli.
- Dotychczasowe przygotowanie zmniejszało roughness/metallic do 2048 przed spakowaniem ORM.
- Generator dodawał do światła świata trzy światła kierunkowe tylko dla gracza.
- Stary retarget uruchamiał Foot IK na modelu źródłowym, kopiował HumanPose na Patryka,
  a następnie obracał same kostki po przekroczeniu 38°. Ta operacja ma nieciągłość
  i nie naprawia płaszczyzny zginania kolan. Nie potwierdzono jej udziału w konkretnym APK.
- Ustawianie dłoni w Idle wyznaczało kierunek palców bez ustalenia obrotu wokół tej osi.

## Zmiana

1. FBX, Color i Normal są kopiowane bajt w bajt. ORM zachowuje każdy oryginalny
   texel roughness i metallic, bez przeskalowania i korekcji gamma. Android:
   wszystkie trzy wynikowe mapy 4K, ASTC 4×4, jakość kodowania 100. Shader i tint bez zmian.
2. Usunięto dodatkowe światła kamery wyłącznie w generatorze sceny Mistrzów.
3. Walk/Run są odtwarzane bezpośrednio na kopii Patryka przez Animator/PlayableGraph
   z Foot IK na docelowym szkielecie. Zapis Legacy obejmuje rotacje i translacje
   wszystkich kości przy 60 Hz. Kontrola porównuje każdą zapisaną próbkę z wynikiem
   natywnego odtwarzania. Usunięto poprawianie yaw samych kostek.
4. Idle ustala kierunek kciuka, a oparcie postaci wyznacza ze skóry po animacji,
   zamiast z nieanimowanych wierzchołków FBX. Siatka i macierze wiązania nie są edytowane.
5. Kontrola nóg obejmuje próbki 120 Hz i spójny znak osi kolan; duży skręt,
   przeprost lub zmiana długości kości przerywa przygotowanie wadliwego klipu.
   Te kontrole wymagają Unity — tutaj nie zostały uruchomione.

Źródła API: oficjalne Unity `HumanPoseHandler.SetHumanPose`, kod Unity-Technologies
`HumanPoseHandler.bindings.cs` (setter przyjmuje bodyPosition, bodyRotation i muscles),
Unity 6000.0 `AnimationClipPlayable`, `SetApplyFootIK`, przykłady Playables.

## Walidacja wykonana

- 6 testów transferu animacji/map, w tym pełne mapy syntetyczne 4096 × 4096.
- 5 istniejących testów szyfrowanego transferu; wymagały dodania katalogu
  `repo-context/unity-tools/claude` do PYTHONPATH w niepełnym lokalnym checkout.
- 6 testów dostawy APK: metadane, certyfikat, SHA-256, obcy commit, awaria jednego miejsca zapisu.
- Rzeczywisty oryginał: FBX/Color/Normal identyczne bajty; po 16 777 216 texeli
  roughness i metallic identycznych z wejściem. Dowód: `mistrzowie-evidence/patryk-original-fidelity.json`.
- Parser C# i YAML: kontrola składni, bez kompilatora Unity.

**Niepotwierdzone:** kompilacja Unity, poprawność Avatar/Foot IK po imporcie, Play Mode,
  wizualny wygląd, bieg i sterowanie na Androidzie. Brak Unity w bieżącym środowisku.
  ORM 4K zużyje około 16 MiB więcej pamięci GPU z mipmapami niż poprzednia mapa 2K
  w ASTC 4×4. Wymagany pomiar na telefonie.

## APK do zwiedzania

Właściciel polecił w tej sesji: „Jak to zrobisz biec bulid z tym co zrobiłeś i on ma to zwiedzać sterowanie z gry przenieś”.
Zakres zgody: jeden nowy build z tymi poprawkami, Miasto Mistrzów, Android APK do zwiedzania.
Zgoda nie obejmuje automatycznych ponowień ani innych rejonów.

- Osobny workflow: **Miasto Mistrzów — APK**, `mistrzowie-apk.yml`, wyłącznie ręczny `workflow_dispatch`.
- Wpis wymaga rejestracji na `main`, aby pojawił się osobno w GitHub Actions. Rejestracja jest oddzielnym, jednoplikiowym PR; nie scala scen ani kodu innych rejonów.
- Gałąź: `codex/miasto-mistrzow-20261104`.
- Jedno wejście `commit`: pełne SHA gry; puste oznacza aktualną gałąź Miasta Mistrzów. Domyślna wartość w PR rejestracyjnym jest przypięta do przygotowanej wersji.
- Kod gry jest pobierany z wybranego SHA niezależnie od gałęzi formularza. Raport Unity, nazwa APK i manifest zapisują SHA gry, a `workflowCommit` oddzielnie wskazuje wersję workflow.
- Nie trzeba wybierać osiedla/miasta ani wariantu: to zawsze APK Miasta Mistrzów.
- Metoda: `BraxChampionsBuild.BuildAndroid`.
- Unity 6000.0.58f2, ARM64/IL2CPP, OpenGLES3, API minimum 24, Linear.
- Osobny identyfikator `com.braxhorn.mistrzowie`, stały klucz repozytorium;
  versionCode = numer przebiegu × 100 + numer próby.
- Sceny: lekki ekran `BraxStart`, następnie `Mistrzowie_Explore`.
- Sterowanie: istniejące `BraxGymPlayer`, `BraxTouchControls` i kamera gry.
- APK jest sprawdzane przez aapt/apksigner, a certyfikat porównywany ze stałym kluczem.
  Następnie od razu Actions artifact i osobne, unikalne wydanie testowe. Błąd
  jednego uploadu nie usuwa drugiego; potwierdzenie w API jest obowiązkowe.
- Eksport edytowalnej sceny/bundles pozostaje osobnym wariantem `region`.

Bieżące połączenie GitHub nie ma operacji uruchamiania nowego workflow.
Nie uruchomiono kompilacji. Dopiero po scaleniu PR rejestracyjnego dostępny będzie osobny formularz; polecenie ręczne:

```bash
gh workflow run mistrzowie-apk.yml --repo wawerpatryk89-sketch/braxhorn-city --ref main
```

Po buildzie trzeba sprawdzić na telefonie: wejście do sceny, joystick, obrót kamery,
przejście Idle → Walk → Run → Idle, skok, schody metra; bieg z przodu, boku i tyłu,
dłonie, podeszwy i materiał w słońcu oraz cieniu. Kompilacja nie jest odbiorem wizualnym.
