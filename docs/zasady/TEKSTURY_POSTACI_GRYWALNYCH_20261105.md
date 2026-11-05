# BRAXHORN — standard tekstur wyłącznie głównych postaci grywalnych
Data decyzji właściciela: 5 listopada 2026 r. | Unity 6 | priorytet: Android

## Zakres — ścisły
Standard stosować **wyłącznie do głównych bohaterów sterowanych przez gracza** we właściwym trybie gry. Przykłady: Brax, Nyxa, Patryk, Monika — **tylko gdy są grywalni / użytkownik bezpośrednio nimi steruje**. Reguła dotyczy odpowiednich rejonów BRAXHORN CITY i ewentualnie BRAXHORN ARENA, gdy pojawi się tam taka postać.

**Nie dotyczy:** zwykłych NPC, przechodniów, policjantów, trenerów, sprzedawców, niegrywalnych przeciwników, postaci tła ani postaci występującej tylko w roli NPC. Nie nakładać globalnego limitu 4K na wszystkie modele ludzi.

## Docelowy podział PBR (jeżeli rzeczywisty model ma osobne siatki UV i materiały)
| Część grywalnej postaci | Maksymalny rozmiar mapy BaseColor | Pozostałe mapy |
| --- | --- | --- |
| Głowa / twarz (zbliżenia, dialogi) | 4096 × 4096 (4K) | Normal do 4K, gdy dostarcza widocznego detalu; ORM wg potrzeby, często 1K–2K |
| Ciało i odzież | 2048 × 2048 (2K) | Normal/ORM maks. 2K, jeżeli uzasadnia to detal |
| Buty, paski, małe akcesoria | 1024 × 1024 (1K) | Normal/ORM do 1K; łączyć małe elementy w atlasy tam, gdzie jest to optymalne |

Wartości są **budżetem maksymalnym**, nie poleceniem zwiększania rozdzielczości ani obietnicą oszczędności pamięci. Nie powiększać źródeł 1K/2K do 4K. Postać bez ujęć twarzy z bliska nie musi mieć głowy 4K. Wyższy detal twarzy nie jest obowiązkowy, gdy pomiar i obraz na telefonie wskazują brak korzyści.

## Warunki techniczne
1. Oddzielne materiały i mapy mają sens tylko przy poprawnym przypisaniu do mesh/submesh i map UV. **Nie dzielić automatycznie jednego obecnego atlasu na głowę, ciało i dodatki.** Taki podział wymaga pracy nad UV i nowymi źródłowymi mapami.
2. Nie zastępować wszystkich slotów oryginalnego modelu jednym wspólnym materiałem, jeśli autor dostarczył prawidłowe sloty dla głowy, ciała i akcesoriów. Również nie rozcinać siatek ani nie zmieniać oryginalnych materiałów tylko dla osiągnięcia nominalnych 4K/2K/1K.
3. Zachować wygląd, kolory, szczegóły twarzy i stroju, szkielet, oryginalną skalę, rig, animacje i bindposes. Nie zmieniać wyglądu graficznego bez zgody właściciela.
4. W Unity: BaseColor w sRGB, Normal jako Normal Map, ORM w liniowej przestrzeni; odpowiednia kompresja platformy, mipmapy i — gdzie wspierane / opłacalne — mipmap streaming. Nie przestawiać obecnego wariantu kompresji Androida (ETC2/ASTC) bez sprawdzenia zgodności urządzeń.
5. Dodatkowe materiały powodują dodatkowe koszty draw calls, a oddzielne 4K/2K/1K mogą zwiększyć VRAM. Nie twierdzić, że taki podział automatycznie oszczędza pamięć. Mierzyć VRAM, liczbę materiałów/draw calls, wielkość APK i płynność na docelowym telefonie.
6. Weryfikować jakość na zbliżeniu twarzy, ujęciu całej postaci, podczas ruchu i z różnych odległości. Wprowadzać zmiany indywidualnie po audycie materiałów/UV konkretnego bohatera, z dowodem przed/po.
7. Jeśli model ma tylko jeden atlas dla całej sylwetki (np. aktualny Patryk w niektórych gałęziach), **zachować dotychczasowy wygląd i tekstury**; nie wymuszać fikcyjnego podziału w kodzie, nie wykonywać destrukcyjnej migracji.
8. Dokument jest zasadą dla przyszłego przygotowania i importu assetów. **Sam zapis nie oznacza, że istniejące postacie zostały już przebudowane** ani że przetestowano APK. Przed realną podmianą potrzebne są rzeczywiste rozdzielone tekstury i kontrola UV.
9. Zmiany kodu i notatki nie są zgodą na budowanie APK, commit na main, uruchamianie automatycznych workflow ani modyfikowanie cudzych postaci.

## Zasada priorytetu
Na telefonie: **spójny, oryginalny wygląd grywalnego bohatera i stabilność > sztywne liczby rozdzielczości**. Stosować 4K twarzy / 2K ciała / 1K dodatków tylko tam, gdzie jakość źródłowych map i UV rzeczywiście to uzasadnia.
