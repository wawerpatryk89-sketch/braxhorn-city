# BRAXHORN CITY — JEDEN UNITY 6 / SEPARACJA OBSZARÓW (TEST)

## Zakres zaakceptowany przez operatora
- **Jedyny silnik uruchomieniowy:** Unity 6000.0.58f2, C# + WebGL.
- **Główny gracz:** oryginalny Brax, oryginalne animacje i jeden wspólny `BraxGymPlayer`.
- **GYM:** zamrożona/zaakceptowana konfiguracja siłowni Unity; zakaz przypadkowego polerowania.
- **CITY:** oryginalna geometria miasta 1:1; bez dodawania ruchu ulicznego i NPC.
- **ESTATE:** oryginalne osiedle 1:1 jako scena Unity; remont premium R2 testowany osobno.
- **Misje, bossowie i dodatkowi zawodnicy:** nie przenosić z tymczasowego Three.js; tylko przyszły master użytkownika.
- **Biblioteka** (ChatGPT + wszystkie `asset-library/`, `dev/models/`, `dev/source-assets/`, `dev/osiedle/assets/`, `public/assets/`, `public/osiedle/assets/`) — nie usuwać ani nadpisywać.

## Wspólny silnik i granice
- Jeden projekt `unity-city/`, jedna kompilacja docelowa `BraxGymBuild.BuildIntegrated`.
- `Assets/PlayableGenerated/BraxhornCity.unity`: siłownia + miasto.
- `Assets/PlayableGenerated/BraxhornEstate.unity`: osiedle z oryginalnym Braxem.
- Dwie sceny w Unity BuildSettings, z wejściem od siłowni i rzeczywistym przejściem do osiedla.
- `Assets/UnityDistricts/registry.json`: kontrolowana lista **realnych** scen. Kolejna dzielnica pojawia się w buildzie dopiero po eksporcie/modelach/QA oraz wpisaniu jej sceny do rejestru.
- Każdy Work zmienia tylko swój obszar; wspólny plik `Assets/BraxGymPlayer.cs` zablokowany do zmian bez osobnej zgody.
- Żadnych atrap scen, zastępczych domów, eksportów lub fikcyjnych filmów.

## Weryfikacja i wydanie
- CI: Unity 6 WebGL, oryginalne siatki 724/553, siłownia i Brax; QA sprawdza siłownię, miasto, osiedle, drzwi, przejście i powrót.
- Test w Chromium/WebGL2 nie zastępuje prawdziwego testu na iPhone.
- Zielone CI nie oznacza wizualnej akceptacji; film prawdziwego spaceru i użytkownikowa akceptacja osobno.
- Żadnych zapisów do `main`, podmian domen czy produkcyjnego Vercel bez osobnego polecenia.
- Źródłowe eksporty Unity są obecnie pobierane z historycznych GitHub Actions; do trwałego pipeline’u trzeba je zachować jako trwałe źródła importowe, zanim artefakty wygasną.
- Stare źródła Three.js wycofywane w odizolowanej gałęzi integracji bez naruszenia assetów. W `main` stary runtime pozostaje do końcowego cutoveru.
