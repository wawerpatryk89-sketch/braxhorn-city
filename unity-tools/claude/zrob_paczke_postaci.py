"""Surowy model z Tripo -> paczka w kształcie, jakiego oczekuje potok obsady.

Brax i Nyxa leżą w bibliotece jako surowe konwersje z Tripo: mają pełny szkielet
(65 kości Mixamo, nazwy zgodne co do jednej), ale ZERO ujęć ruchu, a tekstury są
w osobnych plikach, nie w zestawie, którego potok szuka.

Ten skrypt robi z nich paczkę taką samą jak te 46 przerobionych:
  1. wczytuje model i dokłada mu wybrane ujęcia z globalnej biblioteki ruchów
     (kości mają te same nazwy, więc akcja wchodzi wprost, bez przestrajania),
  2. składa MetalSmooth.png z osobnych map metaliczności i szorstkości
     (R = metaliczność, A = gładkość, czyli 1 - szorstkość) — tak czyta shader obsady,
  3. zapisuje Color.png, Normal.png, MetalSmooth.png i postac.json.

Uruchamiane przez Blendera:
  blender -b -P zrob_paczke_postaci.py -- <model.fbx> <katalog_animacji> <wyjscie> <id>
"""
import json
import os
import shutil
import sys
from pathlib import Path

# Minimum, żeby pokój prób działał. To NIE jest docelowy zestaw ruchów postaci —
# właściciel 18.11, 11:30: „chodzi ogólnie o jakość postaci, jak wygląda,
# a nie o przypisanie teraz animacji".
UJECIA = {
    "Idle": ["Idle", "Fighting Idle", "Breathing Idle"],
    "Walk": ["Walking", "Walk"],
    "Run": ["Running", "Run"],
    "Talk": ["Taunt Gesture", "Taunt", "Talking"],
}


def blender():
    import bpy
    return bpy


def wczytaj_model(sciezka):
    bpy = blender()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(sciezka), automatic_bone_orientation=True)
    rigi = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    if len(rigi) != 1:
        raise SystemExit("spodziewam się jednego szkieletu, jest %d" % len(rigi))
    return rigi[0]


def doloz_ujecia(rig, katalog, moje):
    """Wciąga akcje z plików animacji i nazywa je stanami, których szuka potok."""
    bpy = blender()
    pliki = {p.stem: p for p in Path(katalog).glob("*.fbx")}
    dodane = []
    for stan, kandydaci in UJECIA.items():
        plik = next((pliki[k] for k in kandydaci if k in pliki), None)
        if plik is None:
            print("BRAX_PACZKA BRAK ujęcia dla %s (szukałem: %s)" % (stan, ", ".join(kandydaci)))
            continue
        przed = set(bpy.data.actions.keys())
        bpy.ops.import_scene.fbx(filepath=str(plik), automatic_bone_orientation=True)
        nowe = [a for a in bpy.data.actions if a.name not in przed]
        if not nowe:
            print("BRAX_PACZKA %s nie wniósł żadnej akcji" % plik.name)
            continue
        akcja = max(nowe, key=lambda a: len(a.fcurves))
        akcja.name = stan
        akcja.use_fake_user = True
        dodane.append((stan, plik.stem, len(akcja.fcurves)))
        # Po imporcie zostaje sama akcja. Obiekty, które przyszły z plikiem animacji,
        # lecą — rozpoznaję je po tym, że NIE BYŁO ICH przed importem. Zgadywanie po
        # rodzicu wycinało wcześniej także siatkę postaci i z 34 MB robiło się 0,9 MB.
        for o in list(bpy.context.scene.objects):
            if o not in moje:
                bpy.data.objects.remove(o, do_unlink=True)
    # Bez animation_data na szkielecie eksporter FBX nie wypisuje ŻADNEGO ujęcia,
    # nawet z bake_anim_use_all_actions — sprawdzone: pierwszy eksport miał 0 ujęć.
    if dodane:
        if rig.animation_data is None:
            rig.animation_data_create()
        rig.animation_data.action = bpy.data.actions[dodane[0][0]]
    for stan, zrodlo, ile in dodane:
        print("BRAX_PACZKA ujecie %-6s z %s (%d krzywych)" % (stan, zrodlo, ile))
    return [d[0] for d in dodane]


def metalsmooth(katalog_map, cel):
    """R = metaliczność, A = gładkość (1 - szorstkość). Tak czyta shader obsady."""
    from PIL import Image
    pliki = list(Path(katalog_map).glob("*"))

    def znajdz(*klucze):
        for p in pliki:
            n = p.name.lower()
            if any(k in n for k in klucze):
                return p
        return None

    met = znajdz("metallic", "metalness")
    szor = znajdz("roughness", "rough")
    if not szor:
        raise SystemExit("brak mapy szorstkości w " + str(katalog_map))
    s = Image.open(szor).convert("L")
    m = Image.open(met).convert("L").resize(s.size) if met else Image.new("L", s.size, 0)
    from PIL import ImageChops
    gladkosc = ImageChops.invert(s)
    puste = Image.new("L", s.size, 0)
    Image.merge("RGBA", (m, puste, puste, gladkosc)).save(cel)
    print("BRAX_PACZKA MetalSmooth %s: metaliczność z %s, gładkość z %s"
          % (s.size, met.name if met else "zera", szor.name))


def main(argv):
    model, animacje, wyjscie, ident = argv[:4]
    wyjscie = Path(wyjscie)
    wyjscie.mkdir(parents=True, exist_ok=True)

    rig = wczytaj_model(model)
    moje = set(__import__('bpy').context.scene.objects)
    stany = doloz_ujecia(rig, animacje, moje)

    bpy = blender()
    bpy.ops.object.select_all(action="DESELECT")
    for o in moje:
        o.select_set(True)
    bpy.context.view_layer.objects.active = rig
    cel_fbx = wyjscie / (ident + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(cel_fbx), use_selection=True, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0, path_mode="COPY", embed_textures=False)
    print("BRAX_PACZKA zapisany %s (%.1f MB)" % (cel_fbx.name, cel_fbx.stat().st_size / 1048576))

    # tekstury
    fbm = next((p for p in Path(model).parent.glob("*.fbm") if p.is_dir()), None)
    if fbm is None:
        raise SystemExit("brak katalogu .fbm z teksturami obok " + model)
    for wzor, cel in (("basecolor", "Color.png"), ("color", "Color.png"),
                      ("normal", "Normal.png")):
        zrod = next((p for p in fbm.iterdir() if wzor in p.name.lower()), None)
        if zrod and not (wyjscie / cel).exists():
            shutil.copy(zrod, wyjscie / cel)
            print("BRAX_PACZKA %s <- %s" % (cel, zrod.name))
    # MetalSmooth skladam osobnym krokiem: Python Blendera nie ma PIL.
    (wyjscie / "mapy_zrodlowe.txt").write_text(str(fbm), encoding="utf-8")

    (wyjscie / "postac.json").write_text(json.dumps(
        {"id": ident, "states": stany, "zrodlo": os.path.basename(model),
         "uwaga": "paczka zlozona z surowej konwersji Tripo + globalna biblioteka ruchow"},
        ensure_ascii=False, indent=1), encoding="utf-8")
    print("BRAX_PACZKA gotowe: %s, ujecia: %s" % (ident, ", ".join(stany)))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:])
