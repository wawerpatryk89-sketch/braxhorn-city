# E1 "Centrum Rozrywki" preview (without Unity)

Runs the real `BraxChampionsE1` generator on the editor stubs, with the real foundation round the plot (roads Z-70 / Z70,
sidewalks, the promenade, the beach, the sea), and dumps it for `../hotel_sobczak_preview/geom2bin.py` + `render3d.js`.

```
python3 legacy_foundation.py            # then move Foundation.json to <work>/Assets/Source/Mistrzowie and clean the evidence
dotnet build e1_preview/e1_preview.csproj
cd <work> && dotnet <repo>/unity-tools/mistrzowie/e1_preview/bin/Debug/net8.0/e1_preview.dll Assets/Source/Mistrzowie/Foundation.json
python3 hotel_sobczak_preview/geom2bin.py e1_geometry.json bin && python3 hotel_sobczak_preview/fetch_preview_textures.py bin/scene.json tex
PREVIEW_PICTURES=<work> node hotel_sobczak_preview/render3d.js bin <three> e1_preview/views.json <work> tex
```

The work dir needs `Assets/Source/Mistrzowie/LetterKit.bytes` (build_letter_kit.py), `HarbourSigns.bytes` and the Poly Haven
flower models (prepare_assets.py). Z-fighting: dump the E1 meshes to quads and run `apartments_preview/zfight.py`.
