# Obóz — zasady kontynuacji

Przed zmianami procesu aktualizacji przeczytaj `unity-tools/updates/STANDARD-AKTUALIZACJI.md` oraz `unity-tools/updates/OBOZ-WDROZENIE.md`.

Standard właściciela pochodzi z gałęzi `codex/zone-updates-20261104`, commit `38422c5f031b5f9b85a3e1981c1a8199c9f75731`; SHA pliku: `9fd6e212fb4baa72f453af01f316583ccfecdaa4`. Kopia jest niezmieniona. Nowszy uzgodniony standard przenoś z konkretnym SHA; nie twórz alternatywnej wersji wspólnych narzędzi.

Wspólne `unity_cache.py`, `durable_cache.py`, `world_checkpoint.py` i ich testy przeniesiono bez zmian z `1d83c1a6db4f6c71eb670e5a57bab963ce50e5c1`. Build obozu: `unity-tools/oboz/README.md`. `build-request.json` jest jawnym wyzwalaczem buildu tylko na `gpt/oboz`; zwykły zapis kodu go nie uruchamia. Nie twórz zlecenia przed kompletną, zaszyfrowaną dostawą zasobów.

Zakres: tylko obóz. Monika jest graczem; NPC to VORN, ZYRO, RAKK, KADE, GORR i Miś. Modele innych zawodników służą wyłącznie jako źródła animacji. Zachowaj jakość i ustalony wygląd.

Nie zastępuj miasta/osiedla generatorem obozu, nie scalaj całych gałęzi stref i nie restartuj cudzego buildu. Prywatne źródła, eksporty scen, sekrety i cache nie mogą trafić do publicznego gita.

Zachowuj `.meta`, GUID, trwałe adresy i zapisy gry. Nie przedstawiaj podpisu APK, cache importu, rejestru zasobów ani samego kodu jako działającej aktualizacji zawartości. Osobno raportuj kontrolę kodu, uruchomienie Unity i test tej samej instalacji na telefonie.
