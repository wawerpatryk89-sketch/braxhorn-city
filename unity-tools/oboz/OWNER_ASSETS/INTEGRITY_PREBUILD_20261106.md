# OBÓZ — audyt rzeczywistych materiałów przed buildem (6 listopada 2026)

**Zakres wyłącznie OBÓZ, Unity 6 / Android; gałąź `codex/oboz-przebudowa-pbr-20261105`.**
To raport krótkiej weryfikacji prywatnych oryginałów, a nie protokół odbioru sceny lub APK.
Nie uruchomiono długiej kompilacji Unity, nie modyfikowano modeli postaci, miasta ani osiedla.

## 1. Bezpośrednio wykonane testy oryginalnych paczek

Badanie plików dostarczonych w katalogu roboczym `/mnt/data/files`, bez podmian,
modyfikacji, importu do projektu ani przesyłania prywatnych bajtów do publicznego Git.

| Wejście | Sprawdzony stan |
|---|---|
| `BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip` | 345 273 037 bajtów; SHA-256 `b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006`; pełny CRC ZIP **PASS** |
| Archiwum główne | 31 wpisów, w tym manifest; **17 GLB, 7 źródłowych ZIP, 1 HDR, 5 JPEG = 30 materiałów** |
| `oboz_silownia_opony.zip` | 7 298 373 bajty; SHA-256 `8fee0543521a8ae6043a0c4179e26f2bcd69f1cd778904e2d7b177f7cb22661f`; pełny CRC **PASS** |
| Dodatkowa paczka | Wszystkie wpisy zgodne z manifestem: rozmiar + SHA-256 **PASS** |
| `gym_environment.fbx` | Nagłówek rzeczywistego FBX; 7 wymaganych nazw wyposażenia obecnych w źródle **PASS** |
| `gym_environment.png` | Oryginalny atlas **2048 × 2048** — PASS |
| `tractor_tire.glb` | Oryginalny model obecny w paczce; integralność zgodna z manifestem |
| `kitchen_-_assets.glb` | **99 węzłów / 48 siatek**; źródłowe grupy: stół z nogami i półką (5 części), kuchenka (6), butla z zaworem (2), stojak (3), umywalka z baterią/odpływem/nogami (5) — **wszystkie nazwy rzeczywiście istnieją** |
| `model(1).glb` | Węzeł `medicine-ball-rack` występuje, to nie worek bokserski |
| `picnic_set_1.glb` | Węzeł `FNPC0101_Timber_bench`; **to ławka, nie stół** |

Zweryfikowano trzy oryginały Blender w osobnych archiwach, w tym dekodowanie
zstd/gzip, rozmiar, SHA-256 i rzeczywiste źródłowe rozdzielczości trawy:

| Oryginał | Wersja pliku | SHA-256 oryginału | Wynik |
|---|---|---|---|
| `grass_medium_01_2k.blend` | `BLENDER-v404` (Blender 4.4) | `87f1d30de8fb49a5146a1ea2f1070533c753b484ee5fc26bf7a9ce83f7315fb9` | PASS; 5 map źródłowych 2K |
| `grass_medium_02_2k.blend` | `BLENDER-v404` (Blender 4.4) | `493063e65b09c9f8fb79d764e550dd3ca54ebea119859b41a171354fc8306e8f` | PASS; 5 map źródłowych 2K |
| `drawer_cabinet_4k.blend` | `BLENDER-v293` (Blender 2.93) | `70a2d48a6d0f2d27f51f4bc739831f97a6fbdd256a83c1dc116e773ec0a1df5e` | PASS; 4 mapy PBR |

**Łóżko `ikea_idanas_single_bed.glb`:** jego bajtów nie było w tych dwóch lokalnych archiwach.
Obecność, rozmiar 5 374 568 bajtów i SHA-256
`26efbc9f32157c909d1cd9bde9b1692e4734728a0dcde7466739fe2ce5ff449a`
potwierdzono **z metadanych** GitHub Release 435486352, a nie lokalnym testem pełnej geometrii.

## 2. Błędy wykryte i już poprawione w aktualnym repozytorium

- Oryginalne trawy mają format **Blender 4.4**, natomiast domyślny Ubuntu 24.04
  instaluje Blender **4.0.2**. Aktualny workflow został zmieniony na oficjalny,
  przypięty **Blender 4.5.15 LTS** z weryfikacją sumy SHA-256 oraz kontrolą wersji
  wejściowego `.blend`; bez zmiany źródeł.
- Oryginalne elementy kuchni GLB rozdzielono na siatki potomne i sąsiadujące:
  w bieżącym kodzie generator domków i stołówki składa stół wraz z nogami,
  kuchenkę z rusztami oraz zlew z baterią i odpływem. Nazwy w oryginalnym GLB
  zweryfikowano bezpośrednio.
- Raporty jakości kolizji / NavMesh / sceny nie mają zatrzymywać buildu.
  Błędy rzeczywistego importu, braku plików, kompilacji i podpisu pozostają techniczne.
  Zweryfikowany APK ma być zachowany przed opcjonalnymi kopiami.

## 3. Niezależne potwierdzenia GitHub Actions (bez Unity)

- [Najnowsze szybkie testy kodu obozu, PASS](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42230398798).
- [Weryfikacja oficjalnej sumy SHA-256 Blendera 4.5.15 i dostępności URL, PASS](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42229825752).
  **Ten test nie pobrał całego archiwum 378 MB i NIE wyeksportował modeli.**
- [Pięć niezależnych krótkich kontroli: składnia, wyposażenie, przejścia, C#, prywatne metadane, PASS](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42220151581).

## 4. Rzeczy nadal niepotwierdzone

1. **Cały eksport Blender 4.5.15 → GLB i import do Unity** nie były wykonane w tej kontroli.
   Możliwy techniczny problem jest wykrywany **przed** uruchomieniem kompilacji Unity.
2. Skala, rozmieszczenie, kolizje, NavMesh, animacje Moniki i wszystkich NPC,
   efekty materiałowe, łaźnia, wejścia i faktyczne FPS na Androidzie
   wymagają testu sceny oraz telefonu. Statyczny PASS ich nie potwierdza.
3. Właściciel wymaga drugiego rodzaju worka i długich pracujących lin.
   Jeden oryginalny skórzany worek oraz zwinięte liny są potwierdzone;
   źródeł drugiego worka i kompletnych długich lin **nie potwierdzono**.
4. Istniejące APK #22 pochodzi ze starszego commita `b9e1ba0e` i miało
   **868 481 053 bajty** według GitHub Release — nowy APK może przekroczyć
   docelowy limit mobilny, a rozmiar trzeba zweryfikować po buildzie.
5. Builder nadal używa `com.braxhorn.oboz`; bez testu zgodnej bazy Addressables
   nie można deklarować, że paczka jest aktualizacją delta głównej gry.

## 5. Decyzja odbiorowa

**Materiały wejściowe sprawdzone; szybkie testy kodu PASS; pełna scena i Android NIEODEBRANE.**
Nie wolno wystawić fałszywego „całość gotowa” lub „test telefonu PASS”.
Długi build Unity 6 nie został uruchomiony w ramach tego audytu.
