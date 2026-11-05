# Miasto Mistrzów: Brax zamiast Patryka

Polecenie właściciela z 5 listopada: usunąć Patryka z gry w tym regionie i użyć Braxa z miasta. Nie zmieniano osiedla ani obozu. Bez uruchamiania buildu.

## Przyczyna #7
Run 41936956758, commit 6b94884: źródła Walk/Run istniały. Kontrola retargetingu odrzuciła klipy (m.in. odchylenia długości kości i stóp); agregator zgłosił następnie brak wymaganych klipów. Kontrola przejść była pominięta. To nie był brak przesłanych plików.

## Zmiana
Explorer tworzy Braxa przez BraxChampionsBrax. Oryginalny model i sześć animacji pochodzą z public/assets/chars/brax.glb.gz, identycznego bajtowo w sprawdzonym źródle miasta 0013226. Zachowane węzły, szkielet, siatka, dane binarne i kanały animacji; bez retargetingu Patryka. Skala .024 i kapsuła 2.25 pochodzą z miejskiego generatora. Materiał korzysta z miejskiego HeroPremium, map koloru/normalnej/ORM i miejskiej korekty koloru. Kamera i sterowanie Miasta Mistrzów zachowane.

Oba workflow przygotowują teraz Braxa zamiast odszyfrowywania Patryka. Archiwa Patryka nie są pobierane w sparse checkout. Historyczny kod i zasoby innych regionów nie są usuwane. Kontrole jakości pozostają ostrzeżeniami według wcześniej opublikowanego c1388c7; generowanie Braxa nie uruchamia kontroli ani retargetingu Patryka. Brak faktycznego modelu/klipu pozostaje błędem technicznym, sprawdzanym także przed Unity.

Załączone Patryk(2).zip i Patryk anima(3).zip sprawdzono przed zmianą polecenia: model i cztery mapy zgodne z dotychczasowymi hashami, sześć animacji zgodnych z katalogiem właściciela. Nie dodawano kolejnych kopii tych samych zasobów.

## Weryfikacja
71 testów Pythona PASS, w tym cztery testy rzeczywistego transferu Braxa: zachowanie szkieletu/geometrii/danych binarnych, sześciu oryginalnych animacji, wykrycie brakującego Run przed zapisem oraz komplet map. Parser C#: 276 plików, 0 błędów; actionlint dla obu workflow oraz git diff --check PASS. To nie jest kompilacja C# w Unity ani test APK/telefonu. Nie potwierdzono wyglądu i działania nowego APK.
