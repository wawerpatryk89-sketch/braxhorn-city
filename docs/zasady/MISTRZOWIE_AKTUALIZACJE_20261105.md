# Miasto Mistrzów — baza Unity 6 i następne aktualizacje bez APK

**Zakres:** gałąź `codex/miasto-mistrzow-20261104`, Unity `6000.0.85f2`,
Android ARM64. Poniższy stan to **implementacja źródłowa**, nie certyfikat
udanego uruchomienia na telefonie.

## Dlaczego poprzednie APK nie mogły odbierać aktualizacji

Buildy #9–#14 nie miały dołączonego runtime Addressables ani stałego
`baseId`. Same podpisane pliki APK i sam cache Unity Library nie umożliwiają
podmiany scen bez budowania nowej aplikacji. W szczególności #14 uruchomiono
przed wdrożeniem tego kodu.

## Bazowe APK — raz

1. Ręcznie uruchomić `Miasto Mistrzów — APK`, branch
   `codex/miasto-mistrzow-20261104`, etap `apk`. Nie uruchamiać nowego
   buildu podczas równoległego #14.
2. Unity zapisuje scenę `Mistrzowie_Explore.unity` z oryginalnymi obiektami
   i przygotowuje grupy Addressables oraz ich zależności.
3. `BraxContentBuild.Baseline("mistrzowie", ...)` zapisuje oryginalne GUID-y,
   `addressables_content_state.bin` i `manifest.json`. Pierwsza scena
   aplikacji jest lekka, a miasto dostarczane w lokalnych bundle'ach w APK.
4. CI zapisuje *zaszyfrowany* snapshot scen, zasobów, `.meta` i stanu
   Addressables do prywatnego draft release `content-base-<baseId>`, **przed**
   weryfikacją podpisu. Nie wolno publikować APK jako bazy aktualizacyjnej,
   jeśli snapshot się nie zapisze.
5. Dopiero potem APK podlega obowiązkowej kontroli starego klucza BRAXHORN.
   W razie błędu podpisu zachowywać wynik kompilacji jako niezweryfikowany
   artefakt do ręcznego odzyskania — nie generować ponownie świata.

Zapisz `baseId` z podsumowania workflow i przetestuj bazową instalację na
telefonie. Adres paczki startowej jest w APK; nie trzeba pobierać jej osobno.

## Aktualizacja zawartości — bez BuildPlayer

1. Na tej samej gałęzi dopisz kontrolowane zmiany w
   `unity-city/content-patch.json` (kumulatywna lista `edits`) albo podmień
   wyłącznie istniejące zasoby z zachowanymi GUID-ami. Nie zmieniaj kodu C#,
   pakietów, skryptów Java ani konfiguracji runtime.
2. Otwórz workflow `mistrzowie-apk.yml`, wybierz etap **aktualizacja**,
   wpisz `base_id` w formacie `mistrzowie-<32 znaków hex>`.
3. Pipeline przywraca niezmienną bazę i nie uruchamia generatora Miasta Mistrzów.
   `BraxContentBuild.UpdateFromBaseline` buduje zmienione paczki Addressables
   i katalog, a następnie tworzy `*.braxupdate.zip`. Nie wykonuje BuildPlayer.
4. Paczka trafia do prywatnego draft release tej samej bazy. Zalogowany
   właściciel pobiera ZIP, w aplikacji wybiera
   **USTAWIENIA → WCZYTAJ AKTUALIZACJĘ** i wskazuje plik.
5. Po imporcie uruchom grę ponownie. Loader sprawdza zone/baseId/runtimeId,
   rozmiary i SHA-256, zachowuje poprzednią wersję i wycofuje nieudaną próbę.

**Granice:** zmiana kodu, nowych typów komponentów, pakietów Unity lub schematu
wymaga nowego APK bazowego. Modyfikacja pojedynczej sceny może przebudować
cały bundle tej sceny; wielkości paczki nie da się zagwarantować bez pomiaru.
Importer nie aktualizuje starego `com.braxhorn.city` bez jego własnego,
zintegrowanego loadera. Pierwszy bazowy APK Miasta Mistrzów nadal ma odrębny
identyfikator `com.braxhorn.mistrzowie`. Integracja jako jedna aplikacja
BRAXHORN CITY to osobny etap po potwierdzonym działaniu i migracji scen.

**Weryfikacja przed odbiorem:** build z nowym Addressables, pełna kontrola
podpisu i publikacji APK, instalacja i restart na Androidzie, jedna realna
zmiana obiektu w ZIP, test wycofania i kontrola rozmiarów. Tego jeszcze
nie potwierdzono.

Oficjalna dokumentacja:
- https://docs.unity3d.com/6000.0/Manual/com.unity.addressables.html
- https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/content-update-builds-overview.html
