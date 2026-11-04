# Monika — podstawowe ruchy właściciela, przygotowane bez uruchomienia

Decyzja właściciela: przygotować następną poprawkę obozu, niczego nie uruchamiać.
Zamiast dotychczasowych podstawowych ruchów wybrać te z przesłanej paczki;
pozostałe animacje pozostawić. Baza: `gpt/oboz`,
`2fe912e4df449e1c15d5a139c51b3c27096b7693`.

## Zakres przygotowanej zmiany

| Nazwa w grze | Plik z paczki | Odtwarzanie |
| --- | --- | --- |
| Walk | Brutal To Happy Walking.fbx | Pętla, ruch postaci kontroluje BraxGymPlayer |
| Run | Running.fbx | Pętla, ruch postaci kontroluje BraxGymPlayer |
| Jump | Jump.fbx | Cały klip w tempie 1×, wybicie dopasowane do źródła |

Źródło: `Monika anim(1).zip`, SHA-256
`2de9fa1ec1c72e6d49f5622a56bb7d9cf6de131728152ae9a7a0ff3fdc0f0d60`.
Wcześniejsze `Monika anim.zip` ma identyczne bajty. `Entering Car.fbx`
pozostaje w oryginalnej paczce; nie jest podłączony ani dostarczany do obozu.

Oryginalny model Moniki zweryfikowano względem manifestu:
`b817b4ed779a71f765c3a28909be43ff81cd663131479803ce216d10f17c9e88`.
Nowe źródła mają ten sam zestaw 65 kości, zgodne powiązania kości kończyn
i różnicę długości kości poniżej 0.000053 jednostki źródłowej. Sprawdzono
skończone wartości transformacji przy próbkowaniu 60 Hz: Walk 4810, Run 2535,
Jump 10140 próbek kości. Źródła mają odpowiednio 1.2167, 0.6333 i 2.5833 s.
Maksymalna różnica obrotów pierwszej/ostatniej pozy w pętlach wynosi ok.
0.1324° dla chodu i 0.0002° dla biegu. To kontrola źródeł, nie odbiór wyglądu.

Pozy spoczynkowe źródeł różnią się od modelu. Nie kopiować surowych krzywych
rotacji bez retargetingu. Wszystkie trzy wybrane klipy korzystają z istniejącej
ścieżki `directCalibrated`; importer Humanoid oraz bake pozostają bez zmian.
`inPlace=true` zachowuje dotychczasowe zarządzanie ruchem przez kontroler.
Nazwy Walk/Run/Jump i docelowe pliki `.anim` nie zmieniają się; zapis zachowuje
istniejące GUID. Zmiana specyfikacji i hashy źródeł unieważnia cache Moniki.

Właściciel potwierdził, że dostarczone podstawowe animacje są dobre i idealne.
Są zatwierdzonym wzorcem; nie poprawiać ich choreografii ani nie zastępować
podstawowymi ruchami innego dawcy. Bajty źródłowych FBX pozostają identyczne.
Idle, CombatIdle, ciosy, kopnięcia, uniki, inne ruchy, model, siatka, bind poses
i wzrost nie są zmieniane przez tę podmianę. Osobna poprawka jakości importu
tekstur obozu zachowuje oryginalne obrazy i kolory.

Kontynuacja po `8ee5fbe`: kontroler miał limit akcji Jump 1,2 s i wybicie
najpóźniej po 0,3 s. To ucinało nowy klip oraz uruchamiało fizykę w trakcie
przysiadu. Odczyt źródła ufbx (bez zmiany FBX): palce są przy podłożu do ok.
0,75 s i wracają ok. 1,42 s; pełne odzyskanie pozy trwa do 2,583 s.
Monika w obozie dostaje `ConfigureAuthoredJump(1f,.75f)`: pełne odtwarzanie,
bez wczesnego wyzwolenia z odczytu bioder. Siła/grawitacja pozostają takie same.
Nowa opcja wspólnego kontrolera jest domyślnie wyłączona dla innych graczy.
To dopasowanie obsługi do animacji, nie edycja ruchu źródłowego.

## Dostawa i kontrola statyczna

Trzy oryginalne FBX bez modyfikacji zapakowano z kontrolą SHA-256 i zaszyfrowano
istniejącym odbiorcą obozu: CMS AES-256-GCM / RSA-OAEP-SHA256. Publiczny git
zawiera wyłącznie szyfrogram, metadane i kod; nie zawiera surowych animacji.
`motion_candidates.py`, wywoływany już przez istniejące workflow, przywróci
stary pakiet (m.in. niezmieniony Idle) oraz nowy pakiet właściciela. Obydwa są
weryfikowane przed zastąpieniem plików; błąd nowej dostawy nie przełącza
po cichu na stare ruchy. `.meta` i model nie są nadpisywane.

Sześć lokalnych testów dostawy przeszło: pełne odszyfrowanie na kluczu testowym,
zachowanie bajtów i `.meta`, brak szyfrogramu, błędny hash, błędny plik wewnętrzny
bez częściowego nadpisania, naruszona integralność GCM oraz odrzucenie obcej
animacji, duplikatu, wyjścia poza katalog i innego odbiorcy. Sprawdzono składnię
Pythona i porównanie manifestów: zmienione wyłącznie Walk/Run/Jump Moniki.
To nie jest odszyfrowanie nowej paczki rzeczywistym kluczem CI; ten pozostaje
w Actions i nie został pobrany.

## Czego jeszcze nie potwierdzono

Nie wykonano nowego importu/bake w Unity, Play Mode, APK ani testu telefonu.
Nie deklarować naprawionych skręconych nóg na podstawie samych kości lub hashy.
Przy następnym jawnie zleconym sprawdzeniu obejrzeć chód i bieg z przodu, tyłu
i boku, kolana/stopy oraz przejścia Idle–Walk–Run–stop. Oddzielnie sprawdzić
nowy Jump: przysiad, pojedyncze wybicie, lądowanie i powrót do Idle.
`ReviewCampLife` ma teraz próbę skoku rzeczywistą komendą gracza, zapis klatek
z kamery gry i trzech stron, wymaganie odbicia, lądowania i dalszego odtwarzania
fazy powrotu po 2 s. Próba nie została jeszcze uruchomiona. Sam zielony wynik
starszego `ReviewMonikaRuntime` nie potwierdza skoku. Model i stroje pozostają oryginalne.

Nie zmieniono żadnego workflow ani pliku zlecenia. Sprawdzono wyzwalacze
56 workflow w bazie: na `gpt/oboz` push uruchamia build tylko po zmianie
`unity-tools/oboz/build-request.json`, którego ta poprawka nie dotyka.
Nie wykonano dispatch, retry ani żadnego startu Unity.
