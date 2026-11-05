# BRAXHORN OBÓZ — INWENTARZ MATERIAŁÓW WŁAŚCICIELA
Aktualizacja 5 listopada 2026. STATUS: MATERIAŁY ZEBRANE DO PLANOWANIA. **Nic nie zaimportowano do Unity.**
Uwagi: repozytorium GitHub jest PUBLICZNE. Nie przesyłać GLB, Blend ani zdjęć do publicznego GitHuba, zanim nie zostaną potwierdzone prawa redystrybucji. Oryginalne paczki źródłowe istnieją jako załączniki rozmowy i archiwum przekazania.

## Sprawdzone materiały 3D — partia 1
| Plik | Zastosowanie | Status |
|---|---|---|
| kitchen_-_assets.glb | kuchnia i stołówka | dostarczono; duży zestaw (ok. 102 tys. trójkątów), nie uruchamiać importu przed audytem |
| glass-food-display-showcase-with-trays.glb | witryna stołówki | dostarczono |
| 24-piece-cutlery-set.glb | sztućce | dostarczono |
| stockpot.glb | garnek | dostarczono |
| model(1).glb | stojak na piłki lekarskie — TO NIE WOREK | dostarczono |
| bath-towel.glb | ręczniki, łazienki | dostarczono |
| picnic_set_1.glb | wypoczynek i ławki | dostarczono |
| rope-coil.glb | zwinięta lina, jako dekoracja — nie gotowa lina wspinaczkowa | dostarczono |
| towel-rail-with-towel.glb | wieszak i ręcznik | dostarczono |
| food_tray.glb | taca | dostarczono |

## Sprawdzone materiały — partia 2
| Plik | Rozmiar | Dane techniczne | Zastosowanie |
|---|---:|---|---|
| leather_punching_bag.glb | 16.10 MB | 26 950 trójkątów; 4 tekstury 2048×2048; SHA256 09aa34aea1d750f5931a07def5bd6547fa29afac4cf4e921783c093fa9190f61 | realistyczny skórzany worek, zachować 100% jakości i fizykę |
| boxing_headgear_helmet_low_poly.glb | 8.87 MB | 13 070 trójkątów; 1 tekstura; SHA256 2829c40ddf28fa936ba2b53a922779508093b353fa7754de853535820887a8a0 | kask bokserski do wyposażenia |
| drawer_cabinet_4k.blend.zip | 16.50 MB | Blender + PBR 4K; SHA256 e08b6d754ab2e44632d82421517f4bbbf59d7e9564940c95d22b60a8aeae4fb0 | szafki / domki |
| drawer_cabinet_4k.blend 2.zip | 16.50 MB | zawartość archiwum identyczna plik po pliku, chociaż sam ZIP ma inny SHA256 | DUPLIKAT MATERIAŁÓW, NIE IMPORTOWAĆ PODWÓJNIE |
| metal_cabinet_low_poly.glb | 27.18 MB | 484 trójkąty; 9 tekstur; SHA256 0f19fbf70278e68b0e1250f062163c26c854dd3c6993a1282acf7764e4e8559c | szafka metalowa; mimo małej liczby trójkątów należy skontrolować 9 map |
| montorfano_4k.hdr | 26.75 MB | 4096×2048, RGBE HDRI 360°; SHA256 d1627fcde19c3b4488b1c8e148738bc1c2f5baad4480cfcaddbd51bccc9b0835 | panoramiczne tło górsko-jeziorne za ogrodzeniem |

Zweryfikowane oficjalne źródło panoramy: https://polyhaven.com/a/montorfano (CC0).
Drugie HDRI: NIE DOSTARCZONO POTWIERDZONEGO PLIKU. Nie wybierać przypadkowo.

## Worki — dokładna zasada
- Wymagane dwa rodzaje prawdziwych skórzanych worków: Leather Punching Bag + Vintage / Old Punching Bag.
- **Tylko Leather Punching Bag jest potwierdzonym uploadem GLB.** Na drugi model w aktualnych załącznikach brak zweryfikowanego pliku; nie wprowadzać fikcyjnej ścieżki.
- ZABRONIONE: decymacja, zmniejszanie tekstur 2K i detali lekkiego worka, zamiana na sztuczną geometrię. Zachować szwy, skórę, łańcuchy, PBR i istniejące źródło.
- W 24 instancjach współdzielić tę samą geometrię i zasoby, a nie tworzyć kopii wysokich tekstur w RAM.
- Zachować istniejące komponenty BraxWorek, kolizję, obszar trafień i bujanie.

## Murale — cztery dostarczone oryginalne grafiki
- 87405F34-A95B-4AD6-8637-A19599AADA61.jpeg — OBÓZ WOJOWNIKÓW
- 35502A48-F7AB-4650-9874-3266881B2DF6.jpeg — BRAXHORN FIGHT ZONE
- 6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg — DROGA MISTRZA
- 6B6E1CA3-605A-47FB-BF0E-F23B1D2C3301(1).jpeg — BRAXHORN TRAINING CAMP

## Referencje wizualne
Trzy filmy właściciela oraz dziesięć fotografii z gry; pokazują za duże puste połacie terenu, pierwotne położenia obiektów, rondo bez białego oznaczenia, starą wiatę autobusową i przerwy w chodnikach.

## Pozostaje do dopisania
Łóżka, meble stołówki i krzesła, pozostałe wyposażenie obozu, drugi brązowy worek, druga panorama. Właściciel zapowiedział kolejne materiały. Każdy nowy plik zinwentaryzować i dopiero potem aktualizować zlecenie.

Nie udawać wykonania prac. Nie uruchamiać kompilacji.