# OBOZ BRAXHORN — kompaktowa zabudowa i sport | 05.11.2026

**Status: kod przygotowany do odbioru, NIE wykonano jeszcze Unity Editor / Android / testu telefonu.**
Silnik: wyłącznie Unity 6. Gałąź robocza: `codex/oboz-realny-kompaktowy-20261105`.

## Ustalenia z siedmiu filmów właściciela
- za duża, pusta działka, słabo powiązane budynki i infrastruktura;
- Monika i NPC wyglądali na oderwanych od podłoża, materiał Moniki sprawiał plastikowe wrażenie;
- domki: niedomknięte szczeliny przy drzwiach, zbyt delikatne futryny;
- autobus i wiata przystankowa rozmieszczone bez czytelnego dojazdu;
- ring i oktagon wizualnie stapiały się z ciągiem pieszym;
- NPC podczas sparingu byli zbyt daleko od siebie, brakowało wyraźnego kontaktu;
- brakowało pełnego zaplecza siłowego i bieżni.

## Wprowadzone zmiany w generatorze
- Teren 420×420 -> 280×280 m (280/420)^2 = 44,4% pierwotnej powierzchni. Nie zmniejszano skali bohaterów, domków ani sprzętu.
- Furtka i brama w ogrodzeniu około 252×240 m. Strumień jako krajobraz poza wschodnią granicą; teren pozostaje fizycznie zamknięty.
- Skorygowany układ autobus + zatoka + przejścia + wiata + rondo, bez jazdy przez ogrodzenie.
- Bieżnia owalna, 4 czerwone tory z białymi liniami; osobna prosta 100 m z START/META i znacznikami co 10 m.
- Powtarzalna, niewielka tekstura mikrogranulatu tartanu (256×256), bez dodatkowych map 4K; źródłowe PBR i tiling istniejących powierzchni zachowane.
- Siłownia pod dachem: podłoga gumowa, maty, trzy klatki na gryfy, talerze, trzy ławki, stojak na hantle i lustro/powierzchnia satynowa. Sprzęt jest geometrią z kolizjami; **nie deklaruje się jeszcze fizycznej symulacji podnoszenia ciężarów**.
- 6 stanowisk plenerowych + 3 w siłowni ma teraz interakcję TRENING, czas serii i licznik; specjalistyczne animacje siłowe wymagają osobnego odbioru.
- Wydzielone strefy gumowego podłoża ringów i oktagonu, progi i futryny domków.
- Monika: ten sam, zweryfikowany model FBX i mapy PBR użytkownika. Materiał ma obniżony metaliczny połysk, wyższą chropowatość i bardziej neutralne światło. Oryginalne kości i wygląd tekstury zachowane.
- Wstępne osadzanie postaci przez promień fizyki do podłogi/ringu/terenu. NPC podchodzi do dystansu kontaktu, miesza Punch/Kick i zalicza trafienie wyłącznie, gdy pozycja, kierunek i brak przeszkody są zgodne.
- Na Androidzie dalsze billboardy drzew do 4K, oryginały i widoki bliskie niezmienione. Source paczki prywatnej **nie kasowano**.

## Monika — integralność źródeł
Ponownie przesłane `Monika(1).zip` oraz `Monika anim(3).zip` są identyczne bajt w bajt z wpisami źródłowymi wykorzystywanymi dotychczas w obozie:
- model FBX: SHA-256 `b817b4ed779a71f765c3a28909be43ff81cd663131479803ce216d10f17c9e88`
- animacje: Walk `619a103135f8902347920f04aee7383086e50040029ac2b1a94c7a2bc075b831`, Run `da6f3027993fe9c1c2fdf817725657a76f7b14c63eeb6a098b1dd327816f14dd`, Jump `91198c8657dc4dd63b2c45f29cb8c625452abed55a377c475f936180152549b6`.
- Nie wykonano pozornej podmiany na takie same bajty ani nie usunięto zaszyfrowanego jedynego źródła. Stary **wygenerowany cache postaci** jest invalidowany przez zmianę zależności C# i sceny.

## Weryfikacja przed oceną gotowości
1. Test kompilacji Unity 6000.0.58f2 i generowania sceny; skryptowe kontrole zakresów C# są tylko statycznym preflightem.
2. Play Mode: kolizje ogrodzenia, oba skrzydła wejścia, wejście do 4 domków, brak szczelin, autobus i przejście.
3. Play Mode: 100 m toru z początkiem i metą; owal nie przecina pawilonu ani siłowni; drabinki i ciężary nie wpadają w podłoże.
4. Play Mode: Monika przy postoju, chodzie, biegu i walce, oświetlenie twarzy, brak wiszących stóp. Sprawdzić 6 NPC i 3 areny: trafienia, blokowanie, reakcje, pauzę, drogę do ringów.
5. Android: zmierzyć **rzeczywiste** MiB APK, RAM/VRAM i FPS, ocenić PBR / mipmap streaming i ASTC.
6. Brak zgody właściciela na automatyczny build, publikację i zmianę main. Manualny workflow jest do użycia wyłącznie na osobne polecenie.

**Nie traktować statycznej zmiany C# jako potwierdzenia funkcjonalności na telefonie.**
