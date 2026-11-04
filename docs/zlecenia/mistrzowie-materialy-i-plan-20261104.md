# Miasto Mistrzów — materiały, pamięć i porównanie całego planu

## Źródła i zakres

Plan właściciela: `Braxhorn_Miasto_Mistrzow_Mapa_i_Plan_v1.pdf`, 14 stron, wewnętrzna wersja 1.5, 4 listopada 2026. Odczytano cały tekst i obejrzano mapy stron 1–2. Porównano z `plan_inventory.json`, `layout.py`, `legacy_foundation.py`, rzeczywistymi wygenerowanymi siatkami Exteriors.bytes i generatorami Unity. Nie zmieniano pozycji ani rozmiarów działek i budynków.

Kod bazowy tej pracy: 752cc830614feb24843da2e71de42ca9f52f182b. W trakcie pracy pojawiły się commity 5a2a30b i 11329ca (wspólny standard oraz parser C#). Zachowano je i włączono ich kontrolę także do drugiego workflow Mistrzów.

Adaptacja trawy: istniejące `BraxClaudeOsiedle.cs`, `OsiedleTrawa.shader`, `BraxClaudeTrawa.cs` i `BraxClaudeTrawaDane.cs` odczytane z bazowej gałęzi. Nowy shader jest osobny; shaderów i układu osiedla nie zmieniono. Kępy są nową prostą geometrią źdźbeł, a nie kopią atlasu osiedla ani nowym fotoskanem. Rysuje je istniejący mechanizm instancingu Claude’a.

## Co zmieniono w przygotowaniu

- Oryginalne ARM 4K PNG z Poly Haven: R=AO, G=szorstkość, B=metaliczność. Metadane 12 materiałów potwierdzają dostępność; zapis odpowiedzi 4K w `pbr-provider-preflight.json`. Zachowano również oryginalne roughness EXR i normalne GL EXR.
- Grass004: rzeczywista mapa AmbientOcclusion z oryginalnego archiwum właściciela; dodany SHA-256 do blokady źródeł. ARM pakowany bez zmiany wymiarów i kanałów. Metal048A i Leather037 pozostają z neutralnym AO, ponieważ ich używany kontrakt źródeł nie zawiera AO; nie przedstawiamy tego jako pomierzonej mapy zagłębień.
- Pakowanie ambientCG przeniesione do Pythona. Unity nie tworzy już tablic Color[] dla każdej mapy 4K. Nie jest to pomiar oszczędności RAM ani dowód usunięcia wszystkich awarii.
- Importer map nowego regionu: 4096, sRGB tylko kolor, dane liniowo, mipmapy, filtrowanie trójliniowe, anizotropia 8; jawny Android ASTC 4×4, jakość 100, brak kopii CPU.
- Osobne APK Mistrzów: streaming mipmap dla wszystkich poziomów jakości, budżet 512 MB, maksymalna redukcja jednego poziomu (źródło 4K → minimum rezydentne 2K). To budżet docelowy, nie twarda gwarancja RAM: nieprzełączane tekstury i dolny limit jakości mogą go przekroczyć. Patryk zachowuje wcześniejszy kontrakt 4K. Streaming własnych shaderów i odziedziczonego metra/drzew wymaga pomiaru na urządzeniu.
- Trawa: wzór co 2 m, druga skala i makroplamy 29 m, pasy koszenia, matowa powierzchnia, kalibracja liniowej średniej koloru wzorem Claude’a. Maska ziemi tylko wokół pni, bez przemalowania ulic i otwartego trawnika. Źródło ziemi `brown_mud_leaves_01` jak u Claude’a.
- 14 328 małych kęp w istniejących rabatach przy 287 nasadzeniach, na rzeczywistych trójkątach trawnika. Instancje w kawałkach 9 m, zanik 12–22 m. Nie jest to pokrycie całego regionu gęstą trawą 3D.
- Opcjonalne zdjęcia: osobny player Linux budowany dopiero po potwierdzonej dostawie APK; pięć ujęć z rzeczywiście uruchomionej sceny, kontrola liczby/rozmiaru i niejednolitego obrazu. Formularz `zdjecia` domyślnie wyłączony, ponieważ dodatkowy player wydłuża build. Błąd zdjęć nie odbiera APK. Po zmianie platformy nie zapisujemy Linux Library pod kluczem Android. Ujęcia dzienne; odbiór nocny i Android pozostają osobnym testem.

## Rozmieszczenie — wynik

| Zakres | Wynik statycznego sprawdzenia |
|---|---|
| 33 rezerwacje rdzenia | Wszystkie mieszczą się w 1200×1000; brak wzajemnego nakładania, brak wejścia w sprawdzane pasy ulic/chodników i otwory metra |
| A1/T01 | Obecnie jedyne dwa budynki generowane jako zewnętrza. Środki zgodne z rejestrem: (600,800), (885,815). Cała wygenerowana geometria mieści się w rezerwacjach |
| A2 | Południowy zachód względem A1, zgodnie z mapą; nadal rezerwacja |
| Trening | H1/H2 na północnym zachodzie, H3/H4 przy apartamentach na południowym wschodzie; nadal rezerwacje |
| Hotele/apartamenty | T01–T03 i T04–T07 we właściwych kwartałach; poza T01 budynki nie są jeszcze generowane |
| P1, G1/G2, dworzec, terminal, E1 | Zachowane miejsca z rejestru i relacje z mapy, nie gotowe obiekty |
| Wybrzeże | Promenada 1100×30 m, plaża 650 m; cztery rezerwy usług C01–C04 osobno w generatorze podstawy; marina z 80 miejscami, nie 80 modelami łodzi |
| Komunikacja | Trzy stacje, 10 przystanków tramwajowych i 32 stanowiska autobusowe; kursowanie i pełne połączenie z innymi rejonami nie są wdrożone |

## Rozbieżności i niedokończone elementy

1. **A1: hala 192×118 m w kodzie zamiast 220×150 m w PDF.** Rezerwacja 220×150 m jest mniejsza od planowanej działki około 320×240 m. A2 ma rezerwację 220×150 m wobec około 280×230 m działki w PDF. Nie powiększono ich automatycznie kosztem dojść i sąsiedztwa.
2. Metro w `layout.py` ma środki M1=(400,359), M2=(765,545), M3=(765,800). Dawne znaczniki w rejestrze: (400,330), (800,450), (800,800). Dokumentacja wcześniejszego wdrożenia wyjaśnia dopasowanie do prawdziwych modeli stacji. Nie cofać tych przesunięć bez pomiarów wejść/tuneli.
3. TR01: kod (460,330) wobec znacznika (400,330); TR03: (125,650) wobec (70,650). Przesunięte perony wymagają sprawdzenia wygody przesiadek, nie tylko ciągłości toru. Część przystanków autobusowych też przesunięto w `layout.py`; stare tablice transportu nie są źródłem wykonawczym.
4. Marina: obrys pomostów w generatorze obejmuje około 420×290 m, PDF podaje basen około 420×260 m. Basen i zewnętrzny obrys nabrzeży to różne miary; zgodność wymaga odbioru modelu. Same miejsca postojowe nie dowodzą dopasowania modeli statków.
5. Pozostałe **31 z 33 obiektów rdzenia** to rezerwacje, a nie gotowe budynki. Brakuje też wdrożenia sektorów 24×(300×250 m), wnętrz, usług i ruchu. Nie deklarować, że cała mapa jest ukończona.
6. Brak potwierdzenia obrazu, kolizji i przejść w aktualnym Unity/Androidzie. Testy statyczne działek nie zastępują spaceru po scenie.

## Dowody walidacji

- 43 testy Python narzędzi Mistrzów PASS, w tym pakowanie kanałów, integralność danych, 48 wyborów map na 12 rzeczywistych odpowiedziach API oraz kolizje działek.
- 14 testów istniejącego parsera C# PASS. Kontrola lokalnych 35 plików C# (w tym nowych i zmienionych): 0 błędów składni/konfliktów. Użyto przypiętego tree-sitter 0.25.2; systemowy 0.26.0 ulegał awarii, dlatego nie jest podstawą wyniku. Nie sprawdza typów/API Unity ani shaderów.
- Dwa YAML, 26 kroków shell przez bash -n oraz 6 przypadków formularza źródeł PASS.
- Oryginalne Grass004 Color/Roughness mają zgodne wcześniejsze SHA; ARM 4096×4096 zawiera identyczny co do każdego piksela kanał AO.
- `layout-audit.json`: 33 rezerwacje, 2 wygenerowane zewnętrza, 0 sprawdzanych kolizji. `landscape-checks.json`: 14 328 kęp.
- Nie uruchomiono Unity, playera ani builda. Dostępność metadanych Poly Haven nie oznacza pobrania i sprawdzenia wszystkich nowych map PNG. To pozostaje częścią preflight następnego jawnie zleconego uruchomienia.
