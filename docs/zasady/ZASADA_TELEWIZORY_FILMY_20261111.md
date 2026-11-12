# ZASADA GLOBALNA: każdy telewizor działa i gra filmy — 11.11.2026 (właściciel, 20:46–20:58)

> „żeby w tych telewizorach one leciały … w każdym jednym telewizorze, żeby coś leciało” (20:46)
> „Nawet może być gorszej jakości, żeby nie ważyło … Niech się zapętli” (20:47)
> „9 na 16 … żeby było 16 na 9 jakoś i żeby leciały w tych telewizorach … albo na telefonach” (20:53)
> „w hotelach czy w apartamentach … globalnie zrób, żeby były te filmiki krótkie dostępne … w całej grze” (20:57)
> „zrób zasadę w repo, że tu są filmy dostępne do telewizorów … każdy telewizor ma być włączany, ma mieć opcję włączenia,
> wyłączenia i ma w nim lecieć filmik. Obecnie te, które mamy, a później będzie jeszcze robił.” (20:57)

Obowiązuje w całej grze, we wszystkich rejonach i u wszystkich wykonawców: w mieście, na osiedlu, w hotelach, apartamentach,
galerii, barach i biurach.

> „filmy … tylko i wyłącznie apartamenty, hotele, kino … tam, gdzie są telewizory. W żadnych ulicach telewizorów nie ma być …
> na razie te filmiki tylko mają być w pomieszczeniach użytkowych” (21:50)

**Tylko wnętrza.** Filmy grają wyłącznie na telewizorach w pomieszczeniach: mieszkania, apartamenty, pokoje hotelowe,
sklepy i salony w budynkach, biura. Kino ma własne filmy. Na ulicach, placach, promenadzie i elewacjach nie ma ekranów
z filmami (tablice informacyjne, repertuar i menu zostają). Obowiązuje do odwołania przez właściciela. Dotyczy każdego nowego i poprawianego telewizora, monitora reklamowego i totemu.
Telewizor z samym obrazkiem nie jest skończony (ZASADA_FUNKCJONALNE_JAK_W_ZYCIU_20261111).

## 1. Każdy telewizor

- Ma przycisk **AKCJA**, który pokazuje „WŁĄCZ TV” albo „WYŁĄCZ TV”. Działa w zasięgu ok. 2,6 m × K od ekranu, przez wspólny rejestr `BraxInterakcje`.
- Włączony gra filmy z biblioteki po kolei, w pętli. Każdy telewizor zaczyna od innego filmu. Dźwięk wychodzi z telewizora (3D, cichnie po kilku metrach).
- Wyłączony to czarna szyba.
- Odtwarzacze są dwa wspólne na całą grę: filmy grają tylko na dwóch najbliższych włączonych telewizorach w promieniu 26 m. Pozostałe włączone pokazują swój nieruchomy obraz z kodu. Dzięki temu na telefonie nie ma spadku klatek.
- Jeśli nie ma pliku (build bez paczki), telewizor go pomija. Jeśli nie ma żadnego, telewizor zostaje przy nieruchomym obrazie. Gra się nie wywraca.

## 2. Biblioteka filmów — gdzie jest

| co | gdzie |
|---|---|
| katalog nazw (jedyne źródło prawdy) | `unity-city/Assets/BraxhornWideo/BraxTelewizor.cs` → `BraxWideo.Tv`, `BraxWideo.Pion` |
| pliki w grze | `Assets/StreamingAssets/BraxhornWideo/tv/*.mp4` i `.../pion/*.mp4` (umieszczane w CI, **nie w repo**) |
| paczka w repo (zaszyfrowana) | `unity-city/Prywatne/braxhorn-wideo-20261111/` (`part-*.cms`, `manifest.json` z SHA-256) |
| rozpakowanie w CI (wspólne dla całej gry) | `unity-tools/wideo/install_braxhorn_wideo.py` (krok z sekretem `KLUCZ`; u Mistrzów wołane z `prepare_assets.py` → `install_owner_media.py`) |
| oryginały 16:9 | `unity-city/Prywatne/mistrzowie-reklamy-20261111/` (`oryginaly/`) |
| oryginały 9:16 | w paczce biblioteki (`oryginaly/`) |

Repozytorium jest **publiczne**. Filmy właściciela trafiają do repo wyłącznie zaszyfrowane (ZASADA: assety właściciela pakować od razu). Nigdy nie commitujemy surowych `.mp4`; `.gitignore` w `StreamingAssets/BraxhornWideo` tego pilnuje.

Obecne filmy (11.11.2026):

| lista | pliki |
|---|---|
| `tv` (16:9, 640 × 360) | `tv_01_puma`, `tv_02_alpy`, `tv_03_roboty`, `tv_04_gangsterzy`, `tv_05_fabryka`, `tv_06_korytarz`, `tv_07_auto`, `tv_08_basen`, `tv_09_hala`, `tv_10_teledysk` (nasz teledysk) |
| `pion` (9:16, 360 × 640) | `pion_06_korytarz`, `pion_07_auto`, `pion_08_basen`, `pion_09_hala`, `pion_10_teledysk` |

- Filmy 9:16 w wersji `tv` mają po bokach rozmyty powiększony własny obraz, a nie czarne pasy.
- Format: H.264 baseline 3.1, 30 kl./s, AAC mono 64 kb/s. Lekko, zgodnie z zasadą „gorszej jakości, żeby nie ważyło”.
- Dwa przesłane pliki „hala” były identyczne co do bajtu, więc w bibliotece jest jeden.

Filmy kina G1 (`Mistrzowie/Kino`, paczka `mistrzowie-kino-filmy-20261111`) to osobna sprawa. Nie są w bibliotece telewizorów.

## 3. Podpięcie w innym rejonie (miasto, osiedle, obóz)

Wszystko jest w gałęzi Mistrzów (`gpt/miasto-mistrzow-kasyno-t11-20261105`). Każdy wykonawca robi trzy kroki u siebie:

1. Pobiera z gałęzi:
   - `unity-city/Assets/BraxhornWideo/` (z metą folderu) — kod telewizora;
   - `unity-city/Assets/StreamingAssets/BraxhornWideo/.gitignore` (z metą folderu);
   - `unity-city/Prywatne/braxhorn-wideo-20261111/` — paczkę;
   - `unity-tools/wideo/install_braxhorn_wideo.py`.
2. W swoim workflow budującym APK, w kroku z `KLUCZ` przed Unity, dopisuje jedną linijkę: `python3 unity-tools/wideo/install_braxhorn_wideo.py`. Zmiana workflow to nie jest zgoda na build: build dalej tylko na słowo właściciela.
3. Podpina swoje telewizory: `AddComponent<BraxTelewizor>()` na rendererze ekranu albo telewizor z katalogu `BraxKatalog.Tv` (wymaga `Assets/Editor/Mistrzowie/BraxKatalog*.cs`).

## 4. Jak postawić telewizor (każdy rejon)

Kod edytora (`Assets/Editor/**`, jedno wspólne assembly bez asmdef), gotowe modele z galerii G1, która jest katalogiem wyposażenia całej gry:

```csharp
var c = new BraxKatalog.Ctx { m = batch, root = parent, cols = colliders, unit = K, water = ..., seat = ... };
BraxKatalog.Tv(c, pozycja, kierunekEkranu, 55f, obrazek /*0 jezioro, 1 mecz, 2 miasto nocą, 3 BRAXHORN*/, naNozce, "nazwa");
BraxKatalog.MediaUnit(...);  // szafka RTV z telewizorem i soundbarem
```

- Własny ekran (totem, monitor, ściana wideo): `BraxKatalog.WorkingScreen(c, materiałObrazu, środek, normalna, szer, wys, pion, "nazwa", start)`. Ekran dostaje własny renderer, bez flag statycznych, i komponent `BraxTelewizor`.
- W runtime: `AddComponent<BraxTelewizor>()` na rendererze ekranu, a potem ustawić `obraz`, `wylaczony`, `pion`, `start`.
- Ekrany pionowe (totemy, telefony) mają `pion = true` i grają listę `pion` natywnie.
- Nie robimy telewizorów-atrap z samym obrazkiem. Nie robimy osobnych odtwarzaczy w rejonach: wszystko idzie przez `BraxTelewizor`, żeby limit dwóch odtwarzaczy obowiązywał w całej grze.

## 5. Nowe filmy od właściciela („później będzie jeszcze robił”)

1. Oryginał od razu do zaszyfrowanej paczki, w tej samej sesji.
2. Wersje do gry:
   - 16:9: `ffmpeg -i IN -vf "scale=640:360:force_original_aspect_ratio=decrease,pad=640:360:(ow-iw)/2:(oh-ih)/2,fps=30" -c:v libx264 -profile:v baseline -level 3.1 -pix_fmt yuv420p -crf 28 -movflags +faststart -c:a aac -b:a 64k -ac 1 -ar 44100 tv_NN_nazwa.mp4`
   - 9:16 natywnie: to samo z `360:640` → `pion_NN_nazwa.mp4`
   - 9:16 na telewizor: tło `scale=640:360:force_original_aspect_ratio=increase,crop=640:360,boxblur=18:2,eq=brightness=-0.08`, na nim film `scale=-2:360` na środku → `tv_NN_nazwa.mp4`
3. Nowa paczka z datą (`braxhorn-wideo-RRRRMMDD`), zawierająca całą bibliotekę i manifest. Szyfrowanie: `openssl cms -encrypt -binary -aes256 -stream -outform DER` do `recipient.crt` (certyfikat transferowy CI właściciela, „Braxhorn camp input transfer”). Plik `recipient.pem.enc` jest kopiowany obok. Części mają poniżej 90 MB.
4. Dopisać nazwy do `BraxWideo.Tv` / `BraxWideo.Pion` i przestawić `PACK` w `unity-tools/wideo/install_braxhorn_wideo.py` na nową paczkę.
5. Starych paczek nie usuwamy bez listy i zgody właściciela.

Certyfikat transferowy jest ważny do 4.12.2026. Po tej dacie nowe paczki wymagają nowego certyfikatu od właściciela; stare dalej się otwierają kluczem z `KLUCZ`.

## 6. Kontrola przed oddaniem

- `unity-tools/mistrzowie/test_g1_20261111.py::G1OwnerMediaTests::test_every_tv_works` sprawdza, że telewizory z katalogu mają `BraxTelewizor`, że są włącz/wyłącz, że jest biblioteka i że istnieje ta zasada.
- `install_braxhorn_wideo.py` w logu CI wypisuje `BRAX_WIDEO_OK` / `BRAX_WIDEO_MISSING` dla każdej nazwy z katalogu i na końcu `BRAX_WIDEO_STAGED n`.
- Render kontrolny pokazuje ekran z obrazem i ramką.
- Narzędzie kontroli migotania pokazuje 0 par. W `BraxKatalog.Tv` obraz leży 14 mm przed tylną płytą panelu, a ramka wystaje 2 mm przed obraz.

## 7. Reklamy przed seansem w kinie G1 (12.11.2026)

Właściciel 02:49: trzy krótkie filmy (6923 festiwal, 6924 port wycieczkowców, 6925 łódka) „możesz dać przed kinem jako reklama”.

- Grają **w sali kina, przed filmem**, jak w prawdziwym kinie. Zasada z 11.11 obowiązuje dalej: żadnych filmów na ulicy, więc nie ma ich na ekranach przed budynkiem.
- Kolejność reklam zmienia się przy każdym seansie. AKCJA w sali w trakcie reklam = „POMIŃ REKLAMY” i od razu startuje film. Brakująca albo uszkodzona reklama jest pomijana, a film i tak rusza.
- Prawdziwe logotypy są rozmyte narzędziem `unity-tools/wideo/zamaz_logo.py`, które śledzi ruch obrazu. Zamazane są:
  - logo armatora na kadłubach i kominach statków;
  - czerwony napis na statku;
  - emblemat taksówki;
  - nadruk na koszulce;
  - herb i sponsor na koszulce klubowej;
  - emblemat na beczce.

  Boksy z kluczowych klatek leżą w paczce (`maski/*.json`).
- Paczka `unity-city/Prywatne/mistrzowie-kino-reklamy-20261112` zawiera:
  - `kino/reklama_0[1-3].mp4`;
  - oryginały;
  - maski.

  CI rozpakowuje ją przez `install_owner_media.py` do `StreamingAssets/Mistrzowie/Kino`. Lista jest w `repertuar.json` → `"reklamy"`.
- Test: `unity-tools/mistrzowie/test_kino_reklamy_20261112.py`.
