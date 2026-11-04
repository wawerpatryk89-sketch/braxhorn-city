# Wspólny standard pracy — miasto, osiedle, obóz, Miasto Mistrzów

Polecenie właściciela z 4 listopada 2026: wykorzystać sprawdzone rozwiązania Claude Anthropic we wszystkich rejonach, przekazać wykonawcom sposób pracy i naprawiać awarie na podstawie dowodów.

Ten dokument określa obowiązki wykonawców. Samo jego dodanie NIE oznacza, że wszystkie istniejące materiały, sceny lub buildy spełniają już te wymagania. Nie jest zgodą na uruchomienie ani powtórzenie buildu. Obowiązują aktualne polecenia właściciela i miejscowe zasady startu, w tym opisany w AGENTS.md wyjątek dla pojedynczego, jawnie zleconego buildu obozu.

## 1. Ustal właściwą wersję i przyczynę

- Przed pracą odczytaj z GitHuba aktualny HEAD gałęzi, run, job i pierwszy rzeczywisty błąd. Zapisz pełne SHA, rejon, wariant i wersję Unity. Nie diagnozuj wyłącznie z czerwonej ikony, nazwy workflow ani ostatniego błędu uploadu.
- Odróżnij: błędne wejście formularza, pobranie/import, kompilację C#, generowanie sceny, IL2CPP/APK, dostawę pliku, cache i test telefonu. Awaria każdego z tych etapów wymaga innej poprawki.
- Potwierdź istnienie zdalnej gałęzi PRZED wysłaniem jej nazwy. Do kopiowania podawaj samą dokładną nazwę w osobnym bloku. Podaj również bezpośredni link do workflow i wymagane pola formularza; nie każ wpisywać nazwy gałęzi do pola przyjmującego wyłącznie SHA.
- Sprawdź cudzą bieżącą pracę i istniejące runy. Nie nadpisuj nowszych commitów, nie anuluj ani nie dubluj cudzych buildów. Zapisuj z kontrolą oczekiwanego HEAD. Nie scalaj całych gałęzi rejonów i nie wysyłaj zmian na main bez osobnego polecenia.

## 2. Wzorzec Claude — źródła możliwe do odtworzenia

| Rejon odniesienia | Dokładny commit | Dowód zakończenia CI |
| --- | --- | --- |
| Miasto | `57c1767a54e2405e4d696ac3f2c0ac23b149ad12` | [#48](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41536703095): WebGL, Android APK, zadania wykonania zdjęć zakończone sukcesem |
| Osiedle | `34b592fce367ff712938607f410b56ba192b7123` | [#13](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41218258242): WebGL i zadania wykonania zdjęć zakończone sukcesem; to nie jest dowód Androida |

To wersje referencyjne procesu i wykonania. Nie cofać aktualnych poprawek ani nie przywracać ich dawnych automatycznych wyzwalaczy i niższych limitów tekstur. Oznaczenie współautorstwa Claude w historii nie oznacza wyłącznego autorstwa każdego użytego pliku.

Kod do studiowania w przypiętych wersjach:

- Osiedle: `unity-tools/claude/fetch_osiedle.py`, `unity-city/Assets/Editor/Claude/BraxClaudeOsiedle.cs` — Map, Pbr, GlossScale, UV liczone w metrach, detale i trawa.
- Miasto: `unity-tools/claude/fetch_region.py`, `unity-city/Assets/Editor/Claude/Region/BraxRegion.cs`, `unity-city/Assets/Editor/CityRebuild/Materials/BraxCityMaterials.cs` — pobranie, dobór materiałów i map, zachowanie danych zaimportowanych modeli.
- Miasto: `unity-city/Assets/Editor/Claude/BraxZapisAssetu.cs` — ochrona przed nadpisaniem pliku materiału/siatki przez inny obiekt. Przy adaptacji zachować istniejące GUID i referencje.
- Shader trawy: `unity-city/Assets/Claude/Shaders/OsiedleTrawa.shader` — tekstury w przestrzeni świata, różne skale i zróżnicowanie, uzupełnione geometrią roślin.

## 3. Materiały i geometria

1. Korzystaj z oryginalnych zasobów właściciela oraz zweryfikowanych źródeł Poly Haven / ambientCG. Zapisz ID, adres źródła, wersję lub SHA-256 i rozdzielczość. Nie przedstawiaj proceduralnego zamiennika jako fotoskanu.
2. Wymaganie właściciela: **4K z bliska, minimum 2K dalej**. Nie obniżaj go do dawnych limitów Claude 1K/512. Sprawdź źródło, importer i nadpisania Androida; sam plik 4K nie dowodzi, że silnik używa 4K. Skalowanie pliku 1K do 4K nie tworzy detalu. Dobór pamięci/streamingu wymaga pomiaru i nie upoważnia do potajemnego obniżenia jakości.
3. Kolor albedo/diffuse importuj jako sRGB; normalne i dane jako liniowe. Dla normalnych użyj właściwego typu i konwencji oczekiwanej przez shader. Pełny materiał musi mieć właściwie podłączone normalne, szorstkość/gładkość i AO; metaliczność zgodną z powierzchnią.
4. Nie mieszaj pakowania map: używane w mieście ARM ma R=AO, G=roughness, B=metallic; osiedlowe mg.png ma metaliczność w R i smoothness=1-roughness w A. Shader i importer muszą odpowiadać temu pakowaniu. Mapa danych nigdy nie może zastępować koloru.
5. Powtarzanie tekstur licz w metrach na całej powierzchni. Wszystkie mapy danego materiału mają zgodną skalę i przesunięcie. Oceniaj wielkość kostki, desek i ziarna asfaltu względem postaci; zwiększenie rozdzielczości nie poprawia złego UV.
6. Dobierz połysk do materiału i oświetlenia. Nie zostawiaj plastikowego drewna lub trawy. Mipmapy, filtracja i anizotropia są elementami kontroli importu, nie dowodem jakości bez obrazu z gry.
7. Krawężniki, dachy, rynny, okna i obramowania wymagają odpowiedniej geometrii. Trawę oceniaj jako powierzchnię i roślinność 3D. Nie zmieniaj uzgodnionego układu ani szerokości chodników bez podstawy w zleceniu właściciela.
8. Obóz pozostaje obozem przetrwania: asfalt na dojeździe i pętli autobusu, realistyczne nawierzchnie, drewno domków i blaszane dachy zgodnie ze zleceniem. Nie kopiuj miejskiego układu ulic do obozu. Przed przeróbką sprawdź faktyczną wersję widoczną na filmie oraz przypisania materiałów w scenie.

## 4. Kontrola przed kosztowną kompilacją

1. Zbierz i sprawdź komplet wymaganych źródeł po odszyfrowaniu/pobraniu: poprawny format, integralność, mapy PBR i zależności modelu. Samo istnienie model.gltf nie dowodzi obecności jego tekstur i buforów. Nie akceptuj HTML zamiast modelu. Brak wymaganych źródeł blokuje kolejny etap; raportuj wszystkie niezależne braki naraz.
2. Uruchom testy dotyczące zmiany, kontrolę składni skryptów, YAML i bloków shell oraz istniejące walidatory danego rejonu. Sprawdź dostępność skryptów również po sparse-checkout. Nie uruchamiaj walidatora wymagającego tramwaju w projekcie, który go nie zawiera.
3. Parser C# nie sprawdza wszystkich typów ani API. Do potwierdzenia kompilacji wymagany jest wynik Unity 6 i kontrola całego kodu kompilowanego w tym wariancie, nie wyłącznie widocznej sceny. Tanie testy nie są gwarancją przejścia Unity.
4. Po imporcie sprawdź rzeczywisty typ zasobów i hierarchię obiektów. Nazwane drzwi mogą być rodzicem siatek potomnych. Test geometrii powinien rozpoznawać taki model oraz odrzucać rzeczywisty brak geometrii; nie wyłączać bramki tylko po to, aby build był zielony.
5. Klucz cache uwzględnia rejon, platformę, Unity, pakiety i odpowiednie źródła. Nie zastępuj aktualnego kodu starym checkpointem. Brak cache oznacza pełny import, nie zgodę na pominięcie walidacji. Zapewnij miejsce na obraz Unity, import, wynik i swap zgodnie z faktycznym runnerem; cache prywatnych źródeł musi pozostać zaszyfrowany.
6. Przed APK sprawdź stały certyfikat, applicationId i rosnący versionCode. Nie obchodź błędów podpisu generowaniem nowego klucza.
7. Po jawnym zleceniu wykonaj jeden build konkretnego rejonu. Bez samoczynnych restartów. Zabezpiecz istniejące APK przed cache i backupem; stosuj APK FIRST i jawnie rozróżniaj plik odzyskany po błędzie od sprawdzonego wydania. Późniejsza awaria opcjonalnego backupu nie powinna odbierać pliku.
8. Do odbioru wyglądu dostarcz obraz z rzeczywistego playera tego samego SHA: nawierzchnia przy stopach, drewno, dach, krawężnik, trawa i widok ogólny. Osobno test telefonu: start, przejścia scen, sterowanie i pamięć. Render poglądowy ani zielony build nie zastępują testu urządzenia.

## 5. Potwierdzone awarie i stan poprawek

Odczyt 2026-11-04 około 19:50 UTC. Statusy runów są chwilowe; każdy wykonawca ma je odświeżyć przed działaniem.

| Rejon | Dowód awarii | Poprawka / stan |
| --- | --- | --- |
| Miasto #89 | [run 41892865063](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41892865063), job 126795051127: `TRAM_T1_DRZWI_NIEGOTOWE`; nazwane drzwi były rodzicem siatek | Kontrola hierarchii i odpowiednik C# poprawione; agregacja braków zamiast przerwania na pierwszym. Są w SHA `10da8f80e32d726c969c482007492b6056c1bc7d`. [#90](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41897510961) przeszedł tę bramkę i kompiluje APK. Nie ma jeszcze potwierdzenia gotowego APK/telefonu. |
| Osiedle #85 | [run 41882388584](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41882388584): CS0246, nieznane `InvalidOperationException` w dwóch miejscach BraxClaudeOsiedleSalon.cs | Użyto `System.InvalidOperationException`; SHA `a6e0e4594ae43e0497ab035c46def9bb0b192970`. [#88](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41890123902) kompiluje APK. |
| Obóz #9 | [run 41883412619](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41883412619); szczegóły w `unity-city/Docs/AI/Camp9TextureImportFix-20261104.md` | SHA `f333a94fd2345eb431b08386ff9db654f32fc1af` ustawia import publicznych map przed załadowaniem przez generator. To poprawka podejrzanej ścieżki importu; pełne potwierdzenie przyczyny wymaga Unity. [#12](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41892941205) pracuje, podczas odczytu był na kontroli sceny/NPC. |
| Miasto Mistrzów #3 | [run 41898644227](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41898644227): pole SOURCE_COMMIT zawierało nazwę gałęzi, stary formularz przyjmował pełne SHA | Aktualny workflow w SHA `752cc830614feb24843da2e71de42ca9f52f182b` obsługuje pustą wartość, właściwą gałąź i pełne SHA. Osobny wykonawca przygotował zasoby runnera, szyfrowany cache i zabezpieczenie APK; nie nadpisywać jego zmian. Nowego buildu tej wersji nie potwierdzono. |

Nie przenoś poprawki tramwaju do rejonu bez tramwaju ani importera obozu do innego generatora w ciemno. Przenoś rozwiązanie do odpowiadającej mu ścieżki wykonania po porównaniu aktualnego kodu i testach regresji. Wspólny standard jest obowiązkowy dla wszystkich, ale przyczyny powyższych awarii są różne.

## Aktualizacja po zakończeniu miasta #90

Późniejszy odczyt: #90 zakończył się błędem Unity C# **CS0136**, a nie błędem tramwaju. Log joba 126811323044 z 19:51:12 UTC wskazuje `BraxMiastoWyspaArena.cs:965`: `wc` jako pozycja misy w pętli koliduje z późniejszym `string wc` w zakresie nadrzędnym (wiersz 993). Ten sam kod był obecny w #89, lecz kompilacja nie została wtedy uruchomiona z powodu wcześniejszej bramki. To wyjaśnia ujawnienie kolejnego błędu po naprawieniu tramwaju.

Poprawka miasta zmienia nazwę identyfikatora punktów na `wcId` oraz dwa jej użycia. Współrzędne, materiały, nazwy punktów i wygląd pozostają identyczne. W wersji Miasta Mistrzów ten konkretny konflikt nie występuje; osiedle i obóz nie zawierają tego pliku.

Wspólna kontrola `unity-tools/validation/check_csharp_scopes.py` jest dodawana przed kosztownymi krokami Unity w workflow czterech rejonów (dla miasta i osiedla w obu wariantach). Kontroluje składnię i konflikty zwykłych zmiennych lokalnych/parametrów w zagnieżdżonych zakresach. Używa przypiętych parserów z `unity-tools/validation/requirements.txt`. Nie rozwiązuje typów ani API, nie analizuje wszystkich wzorców C# i pomija deklaracje objęte warunkową kompilacją; nadal wymaga kompilacji Unity.

Dowód: na oryginalnym pliku z #90 wykrywa konflikt `wc` w wierszach 965/993; po poprawce nie wykrywa go. 14 testów obejmuje także legalne powtórzenia nazw w osobnych blokach, pętlach i funkcjach lambda. Odczytano dokładne źródła czterech gałęzi i zweryfikowano Git SHA każdego pliku: miasto 275, osiedle 245, obóz 275, Miasto Mistrzów 271. Kontrola przechodzi na tych źródłach po trzywierszowej poprawce miasta. To nie jest wynik nowej kompilacji Unity ani instalacji na telefonie. Nie uruchomiono ponownego buildu.

Polecenia przed kolejnym zleceniem:

```sh
python3 -m pip install -r unity-tools/validation/requirements.txt
python3 -m unittest discover -s unity-tools/validation -p 'test_*.py' -v
python3 unity-tools/validation/check_csharp_scopes.py unity-city/Assets
```

## 6. Dowód przy przekazaniu pracy

Przekazanie ma zawierać: gałąź istniejącą zdalnie, pełne SHA, przyczynę z logu, zmienione pliki, polecenia i wyniki testów, pozostałe ryzyka oraz statusy osobno: źródła / kompilacja / APK / dostawa / telefon. Nowy wykonawca najpierw czyta ten dokument i aktualny AGENTS.md, następnie historię swojego rejonu. Nie deklarować „wszystko działa” ani „wszystkie błędy usunięte” bez odpowiednich dowodów.

Weryfikacja przy przygotowaniu standardu: ponownie odczytano log miasta #89, log osiedla #85, log Miasta Mistrzów #3, aktualne HEAD czterech gałęzi i kroki bieżących runów. 31 istniejących testów narzędzi miasta, w tym hierarchii drzwi i agregacji braków, przeszło lokalnie. Nie wykonano nowej kompilacji Unity ani testu Androida. Aktualizacja instrukcji nie zmienia wyglądu scen ani nie stanowi odbioru wszystkich materiałów.
