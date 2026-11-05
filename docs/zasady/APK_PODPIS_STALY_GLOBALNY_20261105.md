# BRAXHORN — obowiązkowy podpis APK we wszystkich rejonach Unity 6

Decyzja właściciela: **każdy build przeznaczony na Androida ma dostarczyć APK podpisany istniejącym stałym kluczem BRAXHORN. NIGDY debug APK.** Obowiązuje Miasto, Osiedle, Miasto Mistrzów, Obóz oraz kolejne rejony i warianty Androida. Nie zastępować APK plikiem AAB bez osobnej decyzji właściciela.

## Wymagania bez wyjątków

1. Wyjście Androida: `.apk` (`EditorUserBuildSettings.buildAppBundle=false`, odpowiedni `BuildTarget.Android`).
2. Klucz: **wyłącznie już używany** `braxhorn.keystore`, alias `braxhorn`, dostarczany w CI z istniejących sekretów repozytorium `BRAXHORN_KEYSTORE_B64` i `BRAXHORN_KEYSTORE_PASS`. Nie generować nowego klucza, nie przechowywać hasła ani keystore w commicie i nie wypisywać sekretów w logach.
3. W przypadku brakującego klucza, hasła lub `braxhorn.versioncode`: **zatrzymać build przed Unity lub podpisaniem APK**, zgłosić konkretny błąd. **Zabroniony** fallback do `useCustomKeystore=false` lub domyślnego debug signing.
4. Unity: `PlayerSettings.Android.useCustomKeystore=true`, `keystoreName` wskazuje istniejący certyfikat, `keyaliasName="braxhorn"`; `bundleVersionCode` jest poprawnym dodatnim numerem aktualizacji.
5. Po buildzie sprawdzić plik `*.apk` przez `apksigner verify` oraz porównać SHA-256 certyfikatu podpisującego ze znanym poprawnym certyfikatem poprzednio zainstalowanej wersji. **Nie deklarować zgodności certyfikatu tylko dlatego, że nazwy sekretów/aliasu są identyczne.**
6. Dla aktualizacji bez odinstalowania Android wymaga jednocześnie **tego samego `applicationId` (package name), zgodnego certyfikatu podpisu i wyższego `versionCode`**. Osobne aplikacje nie aktualizują się wzajemnie nawet przy tym samym kluczu.
7. Zachować aktualne identyfikatory aplikacji: Miasto/Osiedle `com.braxhorn.city`; Miasto Mistrzów `com.braxhorn.mistrzowie`; Obóz `com.braxhorn.oboz`. Nie scalać ich bez osobnej decyzji właściciela.
8. Nie zmieniać formatów kompresji tekstur, scen ani funkcjonalności gry z powodu polityki podpisu.
9. **Nie uruchamiać workflow, nie publikować release ani nie puszczać automatycznie nowego APK bez polecenia właściciela.** Commit kodu i dokumentacji nie jest zgodą na kosztowny build.

## Kontrola istniejącej konfiguracji 5 listopada 2026
- Obóz, Miasto Mistrzów i Miasto: generatory Androida już wymagają istniejącego klucza.
- Osiedle: usunięto stary fallback debug z generatora Unity i workflow GitHub Actions.
- Każde następne APK nadal wymaga osobnej weryfikacji rzeczywistego podpisu i testu aktualizacji na telefonie.

**Reguła bezwzględna: brak stałego certyfikatu = brak APK, a nie APK debug.**
