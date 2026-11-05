# Obóz / Unity 6 — Monika 4K + sześciu bohaterów 2K

Status: **kod przygotowany na osobnej gałęzi; brak kompilacji Unity, brak uruchomienia APK i brak pomiaru telefonu**.

## Profil polecony przez właściciela

- **Monika:** istniejące UV, oryginalne albedo 4K, oryginalna normalna 4K, **jedna spakowana mapa 4K MR** (R=oryginalna metallic.r, G=oryginalna roughness.r). Trzy mapy zamiast czterech. Nie rozcinamy siatek na głowę/ciało bez osobnego atlasu i testu wizualnego.
- **VORN, ZYRO, RAKK, KADE, GORR, Miś:** dla Androida albedo i normalne **max. 2K**, **jedna spakowana MR 2K**. Trzy mapy zamiast czterech. Miś posiada już normal/roughness/metallic 2K; nie podwyższamy ich sztucznie. Oryginalne 4K pozostają w źródłach (archiwum właściciela), nie są niszczone.
- Zachowane: kości, bind poses, skóra, UV, animacje, proporcje, kamera, kolizje, sterowanie.
- Wbudowany shader ma **bezpieczną ścieżkę legacy PBR** w razie niedostępnego lub niezgodnego pakowania. Zachowuje oryginalne UV i sposób obliczania szorstkości oraz metaliczności.
- Mipmap Streaming obejmuje tylko źródłowe 4K importowane jako 4K (Monika i kwalifikujące się powierzchnie). **Żadna z pozostałych sześciu postaci 2K nie jest strumieniowana do 1K**. Limit redukcji mipmap = 1.
- Dodatkowy pilot asfaltu 4K spakowanego R=roughness, G=AO pochodzi z poprzedniego zadania; nie zmienia tekstur innych powierzchni.
- Kompresja grafiki na Androidzie: **ASTC 4×4**; format paczki gracza **LZ4** w `BuildOptions`, nie mieszać tych dwóch kompresji.
- PBR generator używa Pillow==12.0.0, sprawdza SHA-256 przypięte do źródeł, nie modyfikuje oryginałów, odrzuca próby upscalingu i generuje stabilny indeks. Jeśli generator padnie, CI zgłasza ostrzeżenie, a shader zachowuje stare mapy; nie blokujemy przez tę optymalizację APK.
- W pakowaniu nie ma atlasowania głowy Moniki ani przerabiania UV. Taki krok wymaga inspekcji oryginalnego FBX i osobnej decyzji.

## Orientacyjna pamięć (ASTC 4×4 + pełny łańcuch mipmap)

`evidence/characters.json` i audyt Obóz #13 wskazują:
- **Przed:** Monika ~85,3 MiB; VORN/ZYRO/RAKK/KADE/GORR ~426,7 MiB razem; Miś ~37,3 MiB; wszystkie postacie ~549,3 MiB.
- **Po (teoria przy trzech mapach i wykluczeniu zbędnych oryginalnych zależności):** Monika ~64,0 MiB; pozostałe sześć ~96,0 MiB; wszystkie postacie **~160,0 MiB**.
- **Potencjał różnicy ~389 MiB** samych map postaci. Łączny obóz z bazowych ~1450,8 MiB do teoretycznych ~1061,5 MiB; **nie jest to pomiar GPU, RAM ani APK** i nie uwzględnia ewentualnego dublowania przez prefab/import.

## Uruchamianie tylko na polecenie właściciela

Workflow `.github/workflows/oboz-unity-build.yml` nadal włącza APK tylko na gałęzi `gpt/oboz`, kiedy właściciel osobno zleci build. Na tej gałęzi zmiany nie wysyłają `build-request.json` i nie uruchamiają workflow.

Przy następnym **jawnie zleconym** buildzie po integracji:
1. Odszyfrować źródła i sprawdzić SHA-256; przywrócić publiczne PBR.
2. Sprawdzić testy `test_pbr_pack.py`, wygenerować indeks i osiem map (7×MR, 1×RA).
3. Unity: importer powinien zgłosić `BRAXHORN_PBR_PACK_ACTIVE` siedem razy dla postaci.
4. Zmierzyć `texture-audit.json`: każda z sześciu postaci ma mapy 2K, Monika mapy 4K, a tekstury MR zastępują oddzielne mapy metallic/roughness w materiałach.
5. Na Androidzie porównać wygląd Moniki oraz pozostałych postaci, szczególnie normalne, połysk, oczy i detale stroju. Sprawdzić FPS oraz prawdziwą pamięć w Unity Profiler/Android.
6. Dopiero po porównaniu i odbiorze zaakceptować oszczędność jako potwierdzoną.

Dla przyszłych buildów należy nadal używać **opcjonalnego** testu życia `quality_review=false`; kolizje w APK pozostają.
