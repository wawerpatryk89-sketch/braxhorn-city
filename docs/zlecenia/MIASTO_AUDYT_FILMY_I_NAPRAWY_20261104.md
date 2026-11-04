# BRAXHORN CITY — audyt zgłoszeń z 6 nagrań, 4 listopada 2026

Zakres TYLKO miasto (Unity 6). NIE zmieniać osiedla, obozu, Braxhorn Arena, main ani oryginalnych grafik użytkownika.

## Dowody z rzeczywistego Androida / starego buildu
Baza: #64, GitHub Actions run 41766745542, job Android 126367351125, APK 9d82bac.
- Mural zewnętrzny siłowni: log `BRAXHORN_BILLBOARDY` — `19: brak ślepego, płaskiego muru ... mural nie postawiony`. Grafika `Grafiki/billboardy/19.jpg` **została zaimportowana**, ale brak montażu. Użytkownik dosłał własny mural (Brax i Nyxa, BRAXHORN STREET FIGHTER); zachować oryginalny plik/kolory/proporcje.
- NOVA: log `BRAXHORN_MURAL mural_nova 9.0×3.00 m` dowodzi wygenerowania muralu za DJ-em (NIE udowadnia widoczności z właściwej pozycji). Na filmie widoczna próba wejścia bez pełnego przejścia.
- Marina: `MARINA boksy D i F: jachtów 0/10`, wszystkie boksy odpadały na `Original virtual floor renovation remainder`. Na filmie widać tylko 1 jacht, są motorówki i skutery.
- Migotanie: `BRAXHORN_SILOWNIA_MIGOTANIE pary_przed=4967 pary_po=101 pary_po_powyżej_0.1m²=74 stref_ciecia=1`. Skrypt opierał się na nazwie aktywnej sceny, chociaż audyt wywoływany jest jako `BraxKontrola.Wszystko(scene,"miasto")`.
- Metro: `BRAXHORN_METRO_PRZEJSCIE` zgłosił na dojściu kolizję `Wejście wieżowca: donica z kwiatami` w (63, 0.7, 35.1) oraz słup tablicy w (63, 0, 33.5). Nagranie pokazuje też problemy z położeniem metra i błękitnym polem / wodą; rozmiar i przesunięcie całej stacji NIE są jeszcze zweryfikowane.
- Sklep Nutrition: kod generował 2 lodówki z napojami, wbrew aktualnej dyspozycji właściciela. W #64 były modele odżywek, ale dostarczenie konkretnego oznakowania opakowań wymaga zrzutów z Unity.
- Supermarkety: w logu są setki zaimportowanych i ustawionych produktów; NIE stwierdzono, że wszystkie sklepy są puste. Apteka: szyld `sobczak_apteka` był generowany; ułożenie produktów i dostępność lokalu wymagają testu.
- Nagrania: niedokończone czarne budynki, brakujące szyby, błękitne/lustrzane fragmenty posadzek, podejrzany obiekt na dachu kamienicy, Tower i mieszkanie Braxa nad NOVA wymagają kontroli fizycznej trasy.

## Zmiany w gałęzi codex/miasto-naprawy-filmow-20261104
- `BraxNabrzezeMarina.cs`: priorytet dla 3 boksów + jacht przy A; kontrola obecnych jednostek zamiast ignorowania z powodu dużej kolizji wirtualnej podłogi, docelowo 4 jachty; 5 motorówek przy B + 1 rejsowa przy E; bez zmiany skuterów. Log i błąd gdy <3 jachty.
- `BraxMiastoNutrition.cs`: z generatora usunięte 2 lodówki i ich kolizje, zamiast nich półki z istniejącymi modelami produktów `miasto_sklepy`; mural zachowany.
- `BraxRegionBraxFlat.cs` + `BraxMiastoNova.cs`: poszerzone wejście klatki do 1.5 m (wzajemne dopasowanie otworów i kolizji) i biegi schodów do 1.45 m; szafka na buty blokująca wewnętrzne drzwi usunięta.
- `BraxKontrolaMigotania.cs`: rozpoznanie obszaru `miasto` poprzez kontrolę budowania zamiast nazwy sceny; wszystkie zaplanowane strefy cięcia, bez przesuwania/nakładania konkurencyjnych elewacji.
- `BraxMiastoBillboardy.cs`: mural 19 montowany na znanym licu fasady siłowni również przy braku 20 idealnych raycastów; przy brakującym pliku nie stawiamy pustej ramy. Wymaga zrzutu kontroli położenia.
- `BraxMiastoUlica.cs`: słup nazwy ul. Portowej przesunięty z x63.2 na x59.7, poza dojście do metra.
- `BraxClaudeMetro.cs`: wąska strefa oczyszczania drobnych dekoracji i ich kolizji przed wejściem do metra; bez niszczenia ścian wieżowca i innych budynków. PASS kontroli przejścia tylko gdy przechodnie są także schody ruchome.
- `BraxRegionTylyB.cs`: wycięcie starej fasady i kolizji w świetle drzwi do BRAXHORN TOWER (2.12×2.86 m), bez nakładania nowego portalu na nieusuniętą ścianę.

## Wymagany test odbiorczy — jeszcze NIE wykonany
1. Zbudować Unity 6 Android na **tej gałęzi** i sprawdzić zgodność certyfikatu/versionCode; nie ogłaszać gotowego APK przed uruchomieniem.
2. Zweryfikować `BRAXHORN_MARINA`: minimum 3, docelowo 4 **widoczne** jachty; 6 motorówek, 4 skutery; brak kolizji burta–pomost, drożne kanały i poprawne punkty WSIĄDŹ.
3. Zweryfikować `BRAXHORN_METRO_PRZEJSCIE`: wejście od chodnika, ruchome schody, antresola, peron; usunięcie donicy/słupa, brak wody wewnątrz; fizyczne przetestowanie położenia metra względem fasady.
4. Zmierzyć `BRAXHORN_SILOWNIA_MIGOTANIE`: liczba par >0.1m² powinna spaść do 0, bez dokładania drugiej powierzchni; stare powierzchnie wycinane, materiały PBR z Poly Haven.
5. Przejść do mieszkania nad NOVA, przez klatkę i każde drzwi, następnie wejść do NOVA i sprawdzić mural za DJ-em, parkiet, światła, dostęp do baru, kolizje.
6. Sklep Nutrition: brak lodówek, bogate półki z rzeczywistymi modelami i czytelnymi, nieprzerobionymi etykietami. Apteka: realne pudełka z etykietami, drożne wejście. Sprawdzić szyldy i wszystkie usługi, które FAKTYCZNIE są w mieście.
7. Mural BRAXHORN GYM: właściwy plik nr 19 i płaszczyzna fasady bez zasłaniania drzwi ani szyb; screenshot od ulicy. Sprawdzić Tower, czarne budynki, błękitne podłogi, podejrzany dach oraz pozostałe błędy filmów.
8. Test telefonu: start, zużycie RAM, wejście/wyjście z metra i lokali, loading, działanie postaci oraz brak awarii. Same sukcesy CI nie dowodzą działania gry.

**Status:** zakodowane poprawki nie są jeszcze odbiorem w Unity. Addressables/content-only update nadal nie jest potwierdzone dla istniejącej bazy; zmiana C# wymaga nowego APK. Nie zmieniać jakości ani nie deklarować 'wszystko naprawione' bez powyższych dowodów.


## Szczegółowy rejestr nagrań — komplet 6 filmów, nie tylko wskazane przez właściciela

Oznaczenia: **WIDAĆ** = obserwacja z klatek nagrania Androida; **LOG** = dowód z builda #64; **DO TESTU** = nie da się potwierdzić bez fizyki i uruchomienia Unity 6. Czas ±2 s, klatki kontrolne co ok. 4 s.

| Film i orientacyjny czas | Obserwacja / wymagany zakres | Źródło dowodu | Status |
|---|---|---|---|
| 1, 00–24 s | NOVA — po podejściu do portalu widać klub i mural za DJ-em, ale przejście nie działa poprawnie; sprawdzić dawną kolizję drzwi, podest i całą trasę do parkietu | WIDAĆ + zgłoszenie użytkownika; kod usuwa stare NOVA_SOURCE_ENTRY | Kod wejścia częściowo poprawiony; test PlayMode |
| 1, 28–44 s | APTEKA — istnieje front i interakcja drzwi; nie dowodzi to, że 24 pudełka leków są prawidłowo rozmieszczone i można dojść do kasy | WIDAĆ; modele z katalogu miasto_sklepy | Odbiór wnętrza i czytelności etykiet w Unity |
| 1, 48–68 s | nienaturalnie turkusowe pionowe panele w elewacji na ul. Portowej i podejrzane elementy nad chodnikiem | WIDAĆ | DO TESTU, nie maskować nakładkami |
| 2, 00–16 s | wejście do metra M1 praktycznie przyklejone do narożnika ceglanej kamienicy, przejście przy fasadzie zablokowane dekoracjami | WIDAĆ + LOG: donica i słup nazwy ulicy blokowały trasę | Nowe wejście przesunięte o 2.5 m na bulwar; wejście, dach, schody i punkt misji powiązane; wymaga QA |
| 2, 16–24 s | w rejonie schodów/metra błękitna/wodopodobna płaszczyzna i przejście przy ścianie; ocenić realną ciągłość podłogi | WIDAĆ | odtworzona widoczna posadzka starych otworów G5; test szczelności wymagany |
| 2, 28–48 s | korytarz, bramki i peron istnieją; weryfikacja drogi powrotnej, peronu i skali składu | WIDAĆ; LOG brak przejścia przy dojściu | DO TESTU |
| 3, 00–08 s | wyjście metra/antresola i przejście na plac — sprawdzić położenie zewnętrznej części oraz kolizje na styku | WIDAĆ | DO TESTU |
| 3, 08–32 s | port nabrzeżny: wielki statek, dźwig i elementy zawieszone wysoko przy budynkach; sprawdzić skale 1.294, obrysy statków, fundamenty, kolizje | WIDAĆ | DO TESTU |
| 3, 36–52 s | nieukończone szare ślepe ściany i duże płaskie bryły przy zabudowie | WIDAĆ | DO TESTU — materiał Poly Haven dopiero po wycięciu starej siatki |
| 3, 52–68 s | duże błękitne/lustrzane płachty przy krawędziach budynków i chodników; gracz przemieszcza się po podejrzanej płaszczyźnie | WIDAĆ | P0: rozróżnić brak geometrii podłoża od wody i wadliwego shadera; DO TESTU |
| 4, 24–36 s | zewnętrzna ściana BRAXHORN GYM bez wielkiego muralu, tylko mały szyld | WIDAĆ + LOG: `19: ... mural nie postawiony` | kotwica muralu 19 poprawiona; sprawdzić exact asset i fotomontaż z Unity |
| 4, 48–96 s | marina: szerokie pomosty, skutery 4 szt., motorówki i jeden widoczny jacht w pobliżu; reszta boksów nieobsadzona | WIDAĆ + LOG: `jachtów 0/10` w boksach D/F | kod docelowo 4 jachty, 6 motorówek, skutery zostają; DO TESTU |
| 4, 100 s | mural Braxa i Nyxy wewnątrz siłowni jest widoczny — nie wolno go niszczyć podczas montażu zewnętrznego | WIDAĆ | Zachować bez zmian |
| 5, 00–32 s | znaki drogowe, przejścia, zapora, samochody na drogach: sprawdzić skalę pojazdów i odstępy na zakrętach oraz przejazd bez blokad | WIDAĆ | DO TESTU, nie zmieniać stylu bez zgody |
| 5, 32–56 s | tyły bloków i trawniki z przejściami; sprawdzić brakujące kolizje oraz zieleń na styku z chodnikiem | WIDAĆ | DO TESTU |
| 5, 72–84 s | zabytkowa kamienica i bryły ponad dachami; skontrolować podejrzany obiekt nad dachem i wysokość osadzenia | WIDAĆ | DO TESTU, zlokalizować identyfikator obiektu |
| 6, 00–36 s | rejon nowych wieżowców: ciemne/blisko czarne bryły, fasady z ciemnymi pustymi polami; pojazdy (autobus i auta) stoją blisko geometrii wieżowców | WIDAĆ | poprawiony fallback aktywnej kamery dla uproszczonych sylwetek; odbiór fasad i ruchu w Unity nadal wymagany |

## Pozostałe usterki przekazane opisem przez właściciela (nie mylić z potwierdzonym kadrem)
- Pierwsze mieszkanie Braxa nad NOVA: wąska klatka i meble blokujące wejście; potwierdzona w kodzie szafka na buty w osi przejścia. Usunięta, schody i portal powiększone. QA: wejście od ulicy do łóżka i z powrotem.
- BRAXHORN TOWER: zgłoszone blokujące wejście/niebieskie elementy; kod wykonuje wycięcie starego portalu, ale brak testu poprawnego wycięcia i działania drzwi.
- Migotające elewacje: test szerszych czterech stref usuwania dolnej warstwy; nie tylko polygon offset. Wymagany log par przed/po i zrzuty w ruchu.
- Market, delikatesy, cukiernia, kawiarnia, apteka: wyłącznie te lokale, które istnieją w mieście, mają dostać odpowiednie szyldy i autentyczne modele asortymentu. Nie dodawać usług z osiedla tylko dlatego, że dostarczono szyld.

## Dodatkowe ustalenia z ponownej analizy kodu
- `BraxMiastoBillboardy` w #64 nie rozpoznał ARENY PREMIUM przy billboardzie 12, bo szukał nieistniejącego materiału starej areny. Kryterium wyszukiwania poprawione, ale rzeczywiste osadzenie wymaga testu.
- `BraxMiastoNutrition` w #64 zachowywał źródłowe drzwi `NUTRITION_SOURCE_ENTRY` równolegle do nowych automatycznych. Odsunięcie duplikatu dodano do kodu. Log ostatniego buildu wcześniej meldował wysokość starego skrzydła 0.01 m.
- `BraxSylwetkiZDaleka`: w razie braku `Camera.main` ciemna uproszczona bryła pozostawała widoczna także z bliska. Dodano pobranie pozycji gracza i ukrywanie sylwetki bez wiarygodnej pozycji.
- Metro: brak gwarancji, że sama odbudowa starych prostokątów G5 wystarczy; po przesunięciu wejścia musi być zbadana pełna geometria połączenia z antresolą i odległość do linii wody x=84.
- Modele `2002_grand_banks.glb`, `frickies_yacht.glb`, `super-yacht02.glb` są w Bibliotece właściciela, ale NIE zostały dopisane do publicznego repo ani APK bez optymalizacji/licencji. `Grand Banks` to 556 tys. trójkątów i wymaga LOD; `Frickies` tylko NC; `Super-Yacht` wymaga odrębnej kontroli warunków i NoAI.
- Nie ogłaszać pełnej naprawy miasta bez rzeczywistego testu telefonu po jednym zbiorczym buildzie. Brak wdrożonego content-only update w obecnym bazowym APK; nowy C# wymaga APK.
