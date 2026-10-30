# Zdjęcia z gry bez telefonu (QA)

1. `fetch_player.sh <wydanie> <sha>` pobiera `player-<sha>.zip` z ukrytego wydania i rozpakowuje do `scratch/player_<sha>/p`.
2. `run_region_shots.sh <sha>` robi zdjęcia wg `shots_region.txt` (nazwa|x,y,z,yaw,pitch,dystans) plus taras i pawilon z raportów.
3. `shoot.cjs <folder playera> <wyjście> "nazwa|x,y,z,yaw,pitch,dystans"...` serwuje player lokalnie, odpala Chromium
   (SwiftShader), klika START, czeka na `BraxhornState.frames > 30`, potem `BraxSend('ReviewView', ...)` i screenshot
   (timeout 300 s, bo SwiftShader renderuje klatkę kilka sekund).

Konwencja kamery: kierunek patrzenia = (-sin yaw, -cos yaw): yaw 0 patrzy na -Z (południe), 90 na -X (zachód),
180 na +Z (północ), 270 na +X (wschód, w stronę morza).
