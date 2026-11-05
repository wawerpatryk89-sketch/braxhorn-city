# BRAXHORN — OBÓZ TRENINGOWY: PLAN WYKONAWCZY (ROBOCZY)

5 listopada 2026 | STATUS: WYŁĄCZNIE PRZYGOTOWANIE

NIE ROZPOCZYNAĆ PRAC, NIE URUCHAMIAĆ BUDOWANIA APK, NIE WYSYŁAĆ AUTOMATYCZNEGO ZLECENIA. Właściciel dostarcza dalsze materiały i dopiero zatwierdzi plan.

Repozytorium: wawerpatryk89-sketch/braxhorn-city. Podstawa: OBOZ #18, run 41983757537, commit e2ee759be31532fa283a3dd7e3f98acb12e9f256. Silnik wyłącznie Unity 6 (Android), nie HTML/Three.js. Inne rejony gry poza zakresem.

## 1. Stan udokumentowany
- Teren z kodu Obozu #18: 280 x 280 przed skalowaniem K=1.294, ogrodzenie X -126..126, Z -125..115 (około 10,1 hektara w przestrzeni Unity). Na filmach wciąż bardzo dużo pustej przestrzeni.
- Brama: (0,-125); rondo: (0,-94), promień jezdni 18, wyspa 6; autobus (21,-92), wiata (27,-92).
- Ring 1 (-23,15), Ring 2 (-12,51), oktagon (0,-5) w jednostkach źródłowych. Obiekty przy trasach wymagają przebudowy funkcjonalnego układu.
- W #18 zakodowano owal bieżni w (65,72) i prostą tzw. 100 m w (113,-35), czerwony tartan i białe linie; log potwierdza generowanie, ale właściciel NIE WIDZI bieżni w APK. Bez odbioru na telefonie brak potwierdzenia wizualnego.
- 24 worki tworzone są programowo ze zwykłych brył; wymagają wymiany na właściwe brązowe modele 3D.

## 2. Proponowana zagęszczona działka
Celem jest zmniejszenie obozu o około 50–60% powierzchni, bez usuwania istniejących funkcji. Roboczy wariant do testu rozmieszczenia: X od -87.5 do 87.5, Z od -80 do 80 przed K. Daje około 226 x 207 jednostek świata i około 4,7 hektara. To WARIANT, nie zatwierdzone ostateczne współrzędne. Dostosować po zwymiarowaniu bieżni, budynków, stref bezpieczeństwa i manewrów autobusu.

Układ od strony bramy:
1. WEJŚCIE I PRZYJAZDY: główna brama oraz furtka na południu. Szyld PREMIUM nad bramą, dokładnie na szerokość bramy, wysoko zawieszony, bez rozciągania grafiki. Logo byka i BRAXHORN / OBÓZ TRENINGOWY. Bez kolizji ze skrzydłem.
2. RONDO: czytelna pełna BIAŁA linia obwodu na asfalcie, wyspa pośrodku. Docelowy realistyczny posąg byka na cokole (zatwierdzić konkretny model), nie wjeżdża w skrajnię pojazdów.
3. AUTOBUS: zatoka poza główną osią pieszą. Wiata ze ścianami po bokach i z tyłu, tylko bok od wsiadania otwarty; realistyczny dach, lepsze ławki premium, oświetlenie. W pełni połączony chodnik z kostki od pasów do furtki i głównej alei. Nie stawiać autobusu wewnątrz wiaty lub na trawie obok chodnika.
4. PIESI: jedna czytelna centralna alejka i prostopadłe odnogi do wejść budynków. Prawdziwa kostka, obrzeża, łączenia bez uskoków. Ring, słupy i ławeczki poza przejściem.
5. WALKA: oddzielny bezpieczny plac z Ringiem 1, Ringiem 2 i oktagonem; zapewnić podejścia do schodków, wolną przestrzeń i poprawny NavMesh. Zachować NPC, animacje i fizyczny kontakt.
6. SIŁOWNIA I WORKI: osobna zadaszona siłownia, realistyczne urządzenia 3D. Wymienić 24 imitacje worków na dwa zatwierdzone rodzaje skórzanych. Zachować komponent BraxWorek, trafienia, zawieszenie, bujanie, kolizje i rozstaw.
7. BIEŻNIA: czerwona trasa długodystansowa z białymi liniami wokół zwartej strefy obozu plus osobna prosta 100 METRÓW RZECZYWISTYCH po uwzględnieniu K=1.294. Nie przecinać placu autobusowego i przejść. Musi być widoczna na urządzeniu; log generatora nie jest dowodem wystarczającym.
8. DOMKI I ZAPLECZE: cztery domki z prawdziwymi łóżkami i szafkami, w jednym skupisku; kuchnia/stołówka i krzesła z materiałów właściciela; osobne WC, umywalnia, prawdziwa łaźnia z prysznicami, medyczny i magazyn. Pełne kolizje, drzwi i wyposażenie.
9. LAS: las gęsty przy płocie i miejscami między strefami; mieszanka jodeł, sosen, młodych drzew i liściastych, krzewy i ściółka. Źródła Poly Haven CC0: https://polyhaven.com/a/fir_tree_01 , https://polyhaven.com/a/pine_tree_01 , https://polyhaven.com/a/tree_small_02 . Nie zasłaniać bramy i wymaganych tras widokowych.
10. PANORAMA: montorfano_4k.hdr 4096 x 2048, 360 stopni, góry i jezioro, https://polyhaven.com/a/montorfano (CC0). Tło świata za płotem, z horyzontem dopasowanym do sceny i połączone optycznie z drzewami, bez płaskiej ściany. Druga panorama wspomniana przez właściciela: JESZCZE NIE DOSTARCZONA / NIE ZATWIERDZONA.

## 3. Murale użytkownika — zachować kompozycje
- OBÓZ WOJOWNIKÓW: ściana drewnianego budynku przy torze przeszkód.
- BRAXHORN FIGHT ZONE: ściana przy strefie walki.
- DROGA MISTRZA: ściana strefy domków / rekreacji.
- BRAXHORN TRAINING CAMP: budynek treningowy.
Oryginalne obrazy bez kadrowania, rozciągania, obniżania jakości lub zmiany typografii. Odbiór lokalizacji z właścicielem.

## 4. Zasada bezwzględna: JAKOŚĆ MAŁYCH ZASOBÓW
Nie wolno redukować geometrii, rozdzielczości tekstur, szwów, materiałów PBR ani detali żadnego lekkiego modelu 3D. Dotyczy szczególnie dwóch rodzajów worków skórzanych, wyposażenia i dekoracji. Oryginały są nienaruszalne. Zmniejszenie zużycia RAM uzyskiwać przede wszystkim przez wspólne mesh i tekstury między instancjami, batching/instancing, culling. Zmiany jakości wyłącznie po dowodzie potrzeby i nowej zgodzie właściciela. Duże drzewa mogą mieć LOD-y przy zachowaniu źródłowych assetów i jakości bliskiego widoku.

Pierwszy dostarczony worek: leather_punching_bag.glb, 16 097 556 bajtów, 26 950 trójkątów, 4 tekstury 2048². Drugi wariant Vintage/Old Punching Bag był rozważany, ale na dziś brak zweryfikowanego drugiego pliku GLB. Nie udawać, że jest do importu. Nie pomylić z animacją Punching Bag ani stojakiem na piłki model(1).glb.

## 5. Warunki odbioru PO zatwierdzeniu zlecenia
- Plan z góry z wymiarami, pełnymi trasami, strefami, drzewami i przejściami.
- Zdjęcia i wideo na telefonie: ringi, rundka po obozie, wiata z każdej strony, rondo, przejście przez bramę, murale, obie bieżnie, wyposażenie wnętrz.
- Test kolizji, przetestowane 24 worki i sparingi NPC, fizyczne wejścia, poprawny NavMesh i brak blokowania tras.
- Pomiary czasu, rozmiaru i pamięci na Androidzie; podpis APK stałym kluczem gry; mechanizm aktualizacji bez konieczności wielogodzinnego pełnego APK, o ile został osobno przetestowany.
- ŻADEN build i ŻADNA ingerencja w scenę przed oddzielną dyspozycją właściciela.

## 6. Otwarte do dostarczenia
Dodatkowe materiały użytkownika, drugi GLB worka Vintage/Old, druga panorama HDR, reszta zakupionego/pobranego wyposażenia domków, stołówki i sprzętu. Po dostarczeniu aktualizować wyłącznie inwentarz oraz plan.