# D2 "Dworzec kolejowo-autobusowy" preview (without Unity)

Runs the real `BraxChampionsD2` generator on the editor stubs, with the real foundation round the block X70..X400 /
Z70..Z330 (roads, sidewalks, lawns, the D2 cut from `layout.py`) and the railway hill west of the region
(`BraxChampionsHorizon.RailHill`, drawn in grass here; the game draws it in the far land's vertex albedo), and dumps it for
`../hotel_sobczak_preview/geom2bin.py` + `render3d.js`.

```
python3 legacy_foundation.py            # then move Foundation.json to <work>/Assets/Source/Mistrzowie and clean the evidence
dotnet build d2_preview/d2_preview.csproj
cd <work> && dotnet <repo>/unity-tools/mistrzowie/d2_preview/bin/Debug/net8.0/d2_preview.dll Assets/Source/Mistrzowie/Foundation.json
python3 hotel_sobczak_preview/geom2bin.py d2_geometry.json bin && python3 hotel_sobczak_preview/fetch_preview_textures.py bin/scene.json tex
PREVIEW_PICTURES=<work> node hotel_sobczak_preview/render3d.js bin <three> d2_preview/views.json <work> tex
```

The work dir needs `Assets/Source/Mistrzowie/LetterKit.bytes`, the Poly Haven flower models, `Assets/Source/Claude/Library/
drzewo_verdant.glb` (the street tree) and `Assets/Source/Claude/Metro/metro_train.glb` (the train). Z-fighting: dump the D2
meshes to quads and run `apartments_preview/zfight.py` (0 pairs on 11.11).
