# BRAXHORN CITY — jeden zasób, wiele instancji

Status: **instrukcja globalna i kod audytu przygotowane, nieprzetestowane w Unity**. Bez zmian w działających buildach i bez automatycznego usuwania tekstur, materiałów czy modeli.

## Zasada we wszystkich rejonach Unity 6

- Powtarzalne drzewa, domki, płoty, słupy, ławki, samochody i urządzenia tworzyć z **tego samego prefab / źródłowego mesh**. Kolejne ustawienia to instancje z własnym transform (i opcjonalnie indywidualnym stanem), a nie kolejne importy modelu.
- Korzystać z `renderer.sharedMaterials`/`renderer.sharedMaterial` i `meshFilter.sharedMesh`. Nie wywoływać `renderer.material` w pętlach rozstawiania, ponieważ Unity tworzy wtedy egzemplarze materiałów.
- Dla zmian koloru/parametru pojedynczej instancji używać `MaterialPropertyBlock`, kiedy shader i renderer to obsługują, bez modyfikowania globalnego materiału.
- Nie duplikować plików PNG/JPG dla wielu instancji tego samego zasobu. Jeden plik może być referencją wielu prefabów i materiałów. Wyjątek: odrębne role importu (albedo sRGB vs dane liniowe, atlas z innym alpha cutoff, różne wymagane ustawienia Androida), kiedy współdzielenie zmieniłoby wygląd.
- `LODGroup`, GPU instancing i strumieniowanie mipmap są **odrębnymi mechanizmami**. Oszczędności tekstur nie wolno przypisywać samemu kopiowaniu prefabów, gdy one już współdzielą Texture2D.
- Przed zmianą referencji porównywać rzeczywiste bajty źródła oraz komplet importera i nadpisania Androida. Nazwa pliku, podobny kolor albo identyczny obrazek na zrzucie nie wystarczają.
- Oryginalne mapy PBR i standard 4K z bliska / 2K dalej pozostają obowiązujące. Brak automatycznych zmian w rozdzielczości, UV, barwach, materiałach, modelach, kolizjach i animacjach bez dowodu zgodności.

## Ustalenia dla obozu z kodu i manifestów

- `Assets/Editor/Oboz/VegetationLayout.json`: 1712 rozstawionych drzew z 6 prototypów, `BraxObozVegetation.Populate` instancjonuje prototyp raz na pozycję; `BraxObozVegetation.Material` pamięta materiały.
- `BraxObozStableMesh.Save` deduplikuje powtarzalne siatki statyczne według fingerprintu geometrii.
- `BraxObozVegetation.Grass` używa wspólnej siatki, materiału oraz danych instancji źdźbeł.
- `BraxCityMaterials` ma słowniki cache materiałów i tekstur; sąsiednie strefy także mają przypadki współdzielenia.
- `BraxObozMaterials.WriteTextureAudit` liczy 146 odrębnych referencji `Texture2D` (z logu #13) przez `Distinct()`, a nie 1712 osobnych pakietów map drzew. Suma pełnego łańcucha mipmap wynosiła 1 521 271 184 B jako oszacowanie Androida, **nie** pomiar użycia RAM.
- W manifeście `unity-tools/oboz/private-inputs.json`: 204 pliki, 200 różnych SHA-256, cztery pary identycznych bajtów zajmują łącznie około 1,65 MB nadmiarowych danych źródłowych. Nie potwierdza to, że wszystkie cztery pary są w finalnej scenie ani ile zajmują w GPU.
- Specjalne wersje liści (próg alpha coverage) i kopie map autobusu mają osobne role w importerze i w checkpointach. Nie usuwać ich tylko dlatego, że źródłowy PNG/JPG wygląda identycznie.

## Narzędzie audytu

`Assets/Editor/BraxSharedResourceAudit.cs`, menu **BRAXHORN → Audyt → Wspoldzielone modele, materialy i tekstury (bez zmian)**:
- Bada **wszystkie aktualnie otwarte sceny** (nie skanuje automatycznie nieotwartych gałęzi ani nie działa w tle).
- Liczy instancje prefabów, referencje siatek/materiałów/tekstur i ich unikalną liczbę.
- Dla samodzielnych obrazów oblicza SHA-256 źródła, a identyczne pliki grupuje, dodatkowo porównując ważne ustawienia importu i Androida.
- Zapisuje `build/SharedResourceAudit/shared-assets.json`, bez zmian plików ani scen.
- `sourceBytesRepeated` oznacza nadmiar surowych bajtów źródłowych, nie gwarantowane oszczędności APK/RAM. Grupy są kandydatami do inspekcji i ręcznego przekierowania referencji.
- Nie importuje ponownie tekstur i nie wywołuje `SaveAssets`. Nie ma automatycznego mechanizmu usuwania duplikatów przed pomiarem/odbiorami.

**Dalsza decyzja techniczna:** na podstawie raportu i kadru z Androida zidentyfikować rzeczywiste powtórzenia w używanych zasobach, a potem przekierować tylko identyczne bajtowo i importowo mapy do jednej referencji. Następnie osobno sprawdzić 4K PBR, liczbę unikalnych map, mip streaming i pamięć telefonu. Nie wolno twierdzić, że sama instancjacja usunie całe 1,52 GB.

Obóz #14 nie używa tej gałęzi. Wymagane jest osobne testowanie Unity i zatwierdzenie przed przeniesieniem.
