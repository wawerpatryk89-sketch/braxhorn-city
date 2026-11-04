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
- `BraxClaudeMetro.cs`: wąska strefa oczyszczania drobnych dekoracji i ich kolizji przed wejściem do metra; bez niszczenia ścian wieżowca i innych budynków.

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
