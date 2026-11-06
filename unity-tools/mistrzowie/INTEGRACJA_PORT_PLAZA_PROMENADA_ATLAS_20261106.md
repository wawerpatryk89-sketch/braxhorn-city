# MIASTO MISTRZÓW A1+T11 — integracja portu, plaży i promenady

Data: 6 listopada 2026. Silnik: Unity 6 (6000.0.85f2). Wyłącznie Android i istniejąca aplikacja Miasta Mistrzów.

## Co przeniesiono
- Źródło: `codex/miasto-mistrzow-promenada-k1-k12-20261105` @ `8373563bb63effaf22599e9df360f137d3cc39fa` (zawiera wcześniejsze prace nad portem i plażą).
- Podstawa: `gpt/miasto-mistrzow-kasyno-t11-20261105` @ `f36289178593cbddf98975759c6f6389c3d1672b`.
- Nowa geometria wybrzeża: 650 m zakrzywionej plaży, natywny piasek PBR, woda i pianka fal; istniejący chodnik nadmorski pozostawiony.
- Port: 56 elementów pomostów, 38 miejsc cumowniczych; oryginalne modele 15 jachtów, 10 Riva i 10 skuterów wodnych, 2 rezerwacje na wycieczkowce i 1 prom (bez sztucznych dużych statków).
- Promenada: 12 pawilonów K1–K12, rowerowa trasa 3 m, 25 oryginalnych modeli Poly Haven 2K do asortymentu, ratownik R1, 32 leżaki i 16 parasoli bez postaci.
- Szyldy: **tymczasowy**, zatwierdzony przez właściciela, oryginalny atlas 3×4. Nie wymaga ręcznego cięcia. W Unity kolejne K1–K12 otrzymują kolejne komórki UV jednego obrazu.

## Pochodzenie i integralność szyldów
- GitHub draft Release ID: `435656828`.
- Wgrany PNG asset ID: `715528115`; nazwa w Release: `D73D6E26-5D8B-4C6B-B585-0748476343DD.png`.
- 2172×724 RGBA, 2 564 862 bajtów; SHA-256: `eac1e17f55d8b77340e7d2cca4c9f97ea72e256acaf8237286ff550012145886`.
- Skrypt `prepare_promenade_atlas.py` pobiera oryginalne bajty tylko z tej pozycji Release, sprawdza rozmiar/SHA-256/nagłówek PNG i przygotowuje stabilne GUID dla Unity; zatrzymuje się przy innej zawartości.
- Każdy fragment ma natywnie ok. 724×181 px, **nie jest osobnym znakiem 2K**; odbiór jakości wizualnej na telefonie pozostaje otwarty.

## Czego nie ruszono
- Basen P1 i gałąź `gpt/miasto-mistrzow-basen-p1-20261105` pozostają całkowicie poza tym scaleniem; wykonawca może tam dalej pracować.
- Główne miasto, osiedle, obóz i gałąź `main` nie są zmieniane.
- Geometria kasyna T11, pierwotne murale areny i skrypty Braxa pozostają z naprawionej podstawy; w kodzie fundamentu zachowano kontrolę natywnego 4K oddzielną od mobilnego Texture2D.
- Metra pięciostacyjnego z gałęzi wybrzeża nie scalono automatycznie, żeby nie zmieniać działającej konfiguracji trzech stacji A1+T11 bez osobnego testu.

## Dowody i granice testów
- Poprzedni test portu/prom: `42131947397` — SUCCESS, bez APK.
- Audyt właścicielskiego atlasu i połączonego kodu: `42148181461` — SUCCESS po korekcie asercji wykrywającej otwór garażu; oba joby zaliczone. Pierwszy run `42147959430` wykazał niedopasowaną do rozszerzonej geometrii asercję testową; nie usuwano kontroli otworu.
- Testy statyczne **nie potwierdzają** Unity Editor importu 25 glTF, kompilacji skryptów Unity, prawidłowego renderu, wygenerowania scen, IL2CPP, podpisu APK ani działania telefonu. Dalszy ciężki build wymaga osobnej decyzji właściciela.

## Uruchamianie
Wyłącznie ręczny workflow APK na gałęzi Miasta Mistrzów A1+T11. Nie uruchamiać długiego automatycznego testu scen. `mistrzowie-apk.yml` najpierw przywraca oryginały A1+T11, pobiera atlas 12 szyldów, przygotowuje prawdziwe Poly Haven 2K i generuje zintegrowaną geometrię.

Kolejny test na telefonie musi potwierdzić: otwór garażu T11, wejście do kasyna i areny, 35 jednostek, nieprzerwaną linię plaża-woda, brak nakładania się rowerówki z brukiem, szyldy K1–K12 i zasięg urządzenia Android.
