# Obóz Unity 6 — tekstury 2K blisko, mniej szczegółów z daleka

## Uzgodniony profil właściciela (5 listopada 2026)

- **Otoczenie widziane z bliska**: 2K na Androidzie dla asfaltu, kostki brukowej (concrete_pavers), trawnika Grass001, żwiru, ściółki leśnej, drewna, desek, ścian wykonanych z materiału wood, drewnianych podłóg, metalu i tkanin. Oryginalne pliki 4K zostają zachowane w źródłach.
- **Monika**: 4K, trzy mapy PBR z połączonym metallic/roughness; zachowane UV, rig, animacje i oryginalne źródła.
- **Pozostałe sześć postaci**: 2K na Androidzie (kolor, normalna, spakowane metallic/roughness). Miś nie jest sztucznie skalowany w górę.
- **Drzewa i trawa**: współdzielone prototypy/siatki i materiały; warianty liści 2K ASTC 6×6, wyjątkowy czterowidokowy atlas billboardu zachowuje limit do 8192 (cztery widoki w jednym arkuszu). Mipmapy mają działać bez zmiany geometrii i naturalnej kolorystyki.
- **Tiling**: asfalt i kostka używają materiałów `PublicPbr` z powtarzaniem `Repeat` oraz `mainTextureScale = 1/widthMetres`; kostka bazuje na oryginalnym wzorze co około 1,9 m, asfalt co około 2,1 m. Proceduralne ściany/deski mają UV proporcjonalne do rzeczywistej wielkości powierzchni, a nie rozciągnięcie obrazu na całość.
- **Mipmaps**: `mipmapEnabled=true`, `Trilinear`, `aniso=8` dla zwykłych PBR. Mniejszy mip jest samplowany zgodnie z wielkością obiektu na ekranie. To poprawia ostrość i stabilność szczegółów w ruchu.
- **Mipmap Streaming**: dodatkowo ogranicza rezydentne mipy (VRAM) w zależności od widoczności, jeśli system/grafika telefonu to wspiera. Włączony globalnie w scenie obozu; `streamingMipmapsMaxLevelReduction=1` ogranicza redukcję przez streaming do jednego poziomu (2K→1K, 4K→2K). Oryginalne źródła nie są edytowane.
- **Ograniczenie streamingu**: żwir, Grass001 i podłoże leśne są składnikami wielowarstwowego shadera o własnych transformacjach UV; ich zwykłe mipmapy działają, lecz *streaming tych konkretnych map nie został aktywowany*, żeby uniknąć błędnej selekcji mipów. Nie deklarujemy oszczędności VRAM tych map przez streaming. Weryfikacja wymaga Unity/Android.
- **Format GPU**: ASTC 4×4 dla zwykłych normalnych i ważniejszych map, ASTC 6×6 dla części teł oraz masek liści; nie mylić z kompresją pliku APK / LZ4.

## Wynik i granice dowodu

Zmiany istnieją **na izolowanej gałęzi** `codex/oboz-pbr-pack-streaming-20261105`, a nie na `gpt/oboz`. Nie ma jeszcze potwierdzenia kompilacji C#, kompilacji shadera, sceny Unity 6, rozmiaru APK, pomiaru RAM, FPS ani jakości na telefonie. Nie ma gwarancji, że wszystkie powierzchnie będą idealnie ostre z bliska — to zależy od jakości oryginału, UV i testu na urządzeniu.

**Nie uruchamiać buildu ani nie zmieniać aktywnej gałęzi bez odrębnego zlecenia.** Test życia obozu ma pozostać opcjonalny; kolizje fizyczne bez zmian.
