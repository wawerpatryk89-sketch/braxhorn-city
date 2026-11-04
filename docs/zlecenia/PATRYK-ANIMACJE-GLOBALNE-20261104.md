# Animacje właściciela — wspólne źródła, 4 listopada 2026

Polecenie: nowy chód, bieg i skok Patryka we wszystkich rejonach; zachować wszystkie dotychczasowe ruchy. Późniejsze doprecyzowanie: chód i bieg jako wspólne źródła dla większości postaci. Chód jest kandydatem — właściciel może dostarczyć inny, zwykły chód. Dostępność źródła nie oznacza automatycznego retargetingu całej obsady. Wejście do auta i oba ruchy bramkarza są wspólnymi źródłami dla kolejnych postaci, do późniejszego retargetingu i podłączenia akcji.

Oryginał: `Patryk anima.zip`, SHA-256 `24878e281548000edb2bbf51bc8bbf5cccedf0eb9a26ad76b168dd9537c78556`. Sześć FBX 7700, każdy 65 kości o tych samych nazwach Mixamo, 315 krzywych animacji, bez geometrii. To kontrola struktury plików, nie poprawności deformacji na docelowym szkielecie.

| Akcja | Źródło | Czas źródła | Przeznaczenie |
| --- | --- | ---: | --- |
| Walk | Strut Walking.fbx | 1,4333 s | wspólne, kandydat |
| Run | Fast Run.fbx | 0,5167 s | wspólne |
| Jump | Jump.fbx | 2,1667 s | Patryk |
| EnteringCar | Entering Car.fbx | 5,4500 s | wspólne, późniejsze użycie |
| GoalkeeperDivingSave | Goalkeeper Diving Save.fbx | 3,2333 s | wspólne, późniejsze użycie |
| GoalkeeperCatch | Goalkeeper Catch.fbx | 2,7333 s | wspólne, późniejsze użycie |

## Integracja

`unity-tools/claude/import_patryk_animations.py` odszyfrowuje paczkę, sprawdza hash całości i wszystkich oryginalnych FBX, a następnie kopiuje sześć plików do podkatalogu `patryk_owner` wskazanego katalogu ruchów. Nazwa podkatalogu oznacza pochodzenie, nie ograniczenie użycia. Rejestr `owner-animation-catalog.json` wskazuje zakres każdego klipu.

Zwykły import `prywatne.py` wywołuje importer do `Assets/Source/Prywatne/ruchy`. Miasto Mistrzów wywołuje go do izolowanego `Assets/Source/Mistrzowie/Player/ruchy`. Generatory Patryka wybierają nowe Walk/Run/Jump w pierwszej kolejności; dotychczasowe źródła pozostają. Zachowano stare wpisy animacji. Miastu Mistrzów przywrócono również osiem dodatkowych pozycji obecnych w starszym generatorze: Boxing, Hook, Kicking, HeadHit, HitToBody, GettingUp, Dodging i Block, oraz ich źródła. Brakującego pliku historycznego nie zastępuje się wymyślonym ruchem.

Trzy wspólne dodatkowe klipy są wpisane do katalogu animacji Patryka, ale ta zmiana NIE dodaje automatycznego wsiadania do auta ani mechaniki bramkarza. Dla innej postaci należy przenieść klip na jej oryginalny szkielet, dopasować punkt wejścia/siadania, synchronizację i przejścia. Nie kopiować gotowego klipu Legacy Patryka na odmienny rig. Sterowanie, model, tekstury i aktualny wybór postaci pozostają bez zmian. W obozie graczem nadal jest Monika; źródła i kod wspólny są dostępne, lecz workflow obozu nie uruchamia importu Patryka.

Import ręczny w środowisku CI z istniejącym sekretem KLUCZ:

```sh
python3 unity-tools/claude/import_patryk_animations.py --destination unity-city/Assets/Source/Prywatne/ruchy
```

## Ochrona źródeł

Oryginalny ZIP zaszyfrowano AES-256-CBC/PBKDF2-SHA256, 200000 iteracji. Losowe hasło paczki jest opakowane RSA-OAEP-SHA256 publicznym kluczem wcześniejszego importu. Zaszyfrowany klucz prywatny jest identyczny z istniejącym obiektem Git `b4a4dbaa0527f5cb7dcafbb4228cebc63068679b`; używa dotychczasowego sekretu CI. Publiczny klucz pochodzi z udanego zadania 41726732013, artefakt 13275771246. Hasło paczki i jawny klucz prywatny nie są w repo. 24 fragmenty mają osobne rozmiary i SHA-256. Sprawdzono lokalnie odszyfrowanie AES do identycznego ZIP; rzeczywiste odszyfrowanie RSA w CI pozostaje do potwierdzenia.

## Sprawdzone i niesprawdzone

- Odczyt struktury wszystkich sześciu FBX i integralność oryginalnego ZIP/FBX.
- 6 testów importera: dokładne bajty, uszkodzony ZIP, błędny hash FBX, zachowanie dawnych klipów/GUID, brak klucza, pełny import szyfrowany z jednorazowym kluczem testowym oraz uszkodzony szyfrogram.
- 43 testy Miasta Mistrzów i kontrola składni C# bez wykrytych błędów.
- Nie uruchomiono Unity, nowego APK ani testu telefonu. Parser C# nie sprawdza typów/API.
- Do odbioru: Idle → Walk → Run → Idle, skok i lądowanie, pętle oraz przejścia, brak dryfu/stóp pod ziemią/deformacji. Nowe tempa źródeł trzeba ocenić z istniejącymi prędkościami gracza.
- Istniejący importer może przejść do starego kandydata po błędzie retargetingu; logi wyboru źródeł muszą potwierdzić użycie `patryk_owner` dla Walk/Run/Jump. Zielony import plików nie dowodzi użycia nowych klipów w playerze.

Zapis zmian nie uruchamia buildów, nie zmienia main i nie scala rejonów. Trwające buildy używają wcześniejszych SHA.
