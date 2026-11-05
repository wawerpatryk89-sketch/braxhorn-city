# BRAXHORN Obóz: aktualizacja ostatniego udanego APK #18

Udana baza: GitHub Actions 41983757537 (5 listopada 2026), commit e2ee759be31532fa283a3dd7e3f98acb12e9f256, com.braxhorn.oboz, versionCode 216024998. Podpis i APK zweryfikowano w CI, zapisano w szkicu oboz-review-latest jako braxhorn-oboz-216024998-e2ee759.apk.

Właściwy najbliższy typ aktualizacji to podpisane APK instalowane bez odinstalowania poprzedniego, z TYM SAMYM applicationId i certyfikatem oraz WYŻSZYM versionCode. Ostatnie APK #18 raportuje contentUpdatesReady=false, więc sama paczka .braxupdate.zip obecnie nie wystarczy. Mniejsza aktualizacja zasobów wymaga osobnej zatwierdzonej bazy i testu Android.

Ręczny start PRZYGOTOWANY, ALE NIEURUCHOMIONY: GitHub Actions > Obóz — Unity 6 APK do testu > Run workflow > gałąź codex/oboz-przebudowa-pbr-20261105; first_install=false; content_action=none (lub puste, jeśli nie ma pola na stronie main). Job odmówi startu, jeśli źródłem poprzedniego APK nie jest dokładnie #18. Po Unity weryfikacja apksigner/aapt i SHA-256 nadal obowiązuje. Push gałęzi nie uruchamia tego buildu.

Przebudowa wymaga rzeczywistej kompilacji Unity i testu telefonu. Poprzedni preflight #42094285958 przeszedł: 127+7+2 testów, składnia 283 plików C# bez błędów. Żadne postacie, animacje i ich materiały nie są zmieniane przez tę poprawkę workflow.
