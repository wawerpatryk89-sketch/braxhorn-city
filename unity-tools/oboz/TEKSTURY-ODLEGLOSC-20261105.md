# Obóz — ostre tekstury blisko, lżejsze mipmapy daleko (5 listopada 2026)

Status: **poprawka kodu na osobnej gałęzi; Unity/Android nieprzetestowane**. Nie jest to zgoda na build, zmiana wyglądu ani ocena jakości.

## Wymagania właściciela

Źródło obowiązujące: `docs/zasady/STANDARD_PRACY_UNITY6_20261104.md`, §3.
- 4K z bliska, minimum 2K w źródłowych materiałach dalszego planu.
- Bez zwiększania małych źródeł przez sztuczne skalowanie.
- Bez zamiany poprawnych map PBR, UV, barw, obiektów, modeli, animacji i oznaczeń.
- Bez automatycznego obniżania oryginalnego importu do 1K/512.

## Zmiana techniczna (tylko obóz)

- `BraxObozMaterials.ConfigureStreaming` w importerze i audycie sceny wykrywa **rzeczywiste** tekstury o obu bokach co najmniej 4096 px; tylko dla nich dopuszcza Mipmap Streaming. Pliki źródłowe, górne limity 4K/8K i format ASTC pozostają niezmienione.
- Mapy o źródle 2K i mniejszym pozostają **niestreamowane**: nie obniżamy ich maksymalnej rozdzielczości. Standardowe mipmapy nadal służą antyaliasingowi na odległość.
- Chronione bez streamingu: wielowarstwowe podłoże (oryginalne igliwie, żwir, Grass001), roślinność importowana z GLB (własne transformacje UV i wycinanie alfa), proceduralnie instancjonowane źdźbła bermuda i niestandardowe 4-widokowe atlasy billboardów. Drzewa nadal korzystają ze swoich LOD, które zmniejszają szczegółowość z odległości. Mipmap streaming dla tych grup wymaga osobnego testu Unity.
- Streamowane źródła 4K mogą zwalniać **najwyższy poziom** mipmapy, czyli przejściowo 4K→2K. Parametr `streamingMipmapsMaxLevelReduction = 1` nie pozwala strumieniowaniu usunąć więcej niż jednego poziomu. Nie wymusza rozmycia przy bliskiej kamerze — o rozdzielczości decyduje potrzeba ekranu i budżet pamięci Unity.
- Postacie otrzymują wyższy priorytet (2), typowe materiały bliskiego otoczenia priorytet (1), pozostałe 0. Priorytet to preferencja, nie gwarancja wysokiego mipa w przypadku presji pamięci.
- Dla obozu ustawiamy globalny limit mipmap na 0 (pełna rozdzielczość źródła dostępna blisko); przy wyjściu ze sceny przywracamy poprzedni limit. Nie zmienia to źródeł pozostałych stref.
- `BraxObozSceneInfo` aktywuje Mipmap Streaming w czasie Play Mode dla dostępnego sprzętu, a po wyjściu ze sceny przywraca poprzednie ustawienia globalne. W Android Build konfigurację również stosuje `BraxObozBuild.Configure`.
- Na Androidzie `BuildOptions.CompressWithLz4` włącza wymaganą przez Unity kompresję danych wspierającą asynchroniczne pobieranie mipmap. To **inna** kompresja niż ASTC na GPU. Rozmiar finalnego APK może się zmienić — jeszcze niezmierzony.
- `texture-audit.json` raportuje `streamingMipmaps` i priorytet dla każdej tekstury oraz liczbę aktywnych streamerów. Suma pełnych mipmap nadal nie jest pomiarem pamięci Androida.

## Co wymaga osobnej kontroli

1. Kompilacja C# oraz shaderów Unity 6000.0.58f2 (brak dostępnego edytora w tej sesji).
2. Play Mode: `BRAXHORN_OBOZ_MIP active=true supported=true max_reduction=1` i `BRAXHORN_OBOZ_TEXTURES ... streamed_textures=N`. Sam log ustawień nie potwierdza oszczędności.
3. Android APK: sprawdzić uruchomienie i pomiary `Texture.currentTextureMemory`, `Texture.desiredTextureMemory`, liczby wczytanych mipmap i FPS na urządzeniu, z udziałem gracza przy ścianie, drzewie, workach, Monice oraz w dalekim planie.
4. Obejrzeć te same kadry blisko/daleko i wykluczyć rozmycie, migotanie trawy/liści oraz nagły przeskok szczegółów.
5. Z raportu wcześniejszej próby: 146 map, 87 poniżej standardu źródłowego, oszacowanie pełnych mipmap 1 521 271 184 B. Mipmap Streaming **nie tworzy** brakujących źródeł 4K i nie stanowi automatycznego zatwierdzenia wyglądu.

## Dokumentacja Unity 6.0

- https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/texture-loading/texture-streaming/configure
- https://docs.unity3d.com/kr/6000.0/ScriptReference/QualitySettings-streamingMipmapsActive.html
- https://docs.unity3d.com/ja/6000.0/ScriptReference/TextureImporter-streamingMipmaps.html
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/buildoptions

Bez nowego jawnego zlecenia nie uruchamiać workflow, nie aktualizować `build-request.json`, nie przenosić zmian na `gpt/oboz`, `main` ani do innych stref.
