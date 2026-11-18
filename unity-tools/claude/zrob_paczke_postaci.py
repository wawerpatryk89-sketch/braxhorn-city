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
# ZESTAWY UJĘĆ. Właściciel 18.11, 15:08: „dobierz mu 30 animacji, walecznych,
# kopania rękami, żeby nie był wolny, chodzenia, wszystkie podstawowe".
#
# Nazwa po lewej to stan, którego szuka gra; po prawej nazwa nagrania w globalnej
# bibliotece (pozycja A). Idle, Walk, Run i Talk muszą być, bo na nich stoi pokój.
PODSTAWA = {
    "Idle": "Fighting Idle",
    "Walk": "Walking",
    "Run": "Running",
    "Talk": "Taunt Gesture",
}

ZAWODNIK = dict(PODSTAWA, **{
    # ruch
    "WalkBack": "Walking Backwards",
    "StepBack": "Step Backward",
    "StepFwd": "Long Step Forward",
    "SideStep": "Short Left Side Step",
    "Sprint": "Standing Sprint Forward",
    "Jump": "Jump",
    # pięści
    "Jab": "Lead Jab",
    "JabCross": "Jab Cross",
    "Hook": "Hook",
    "Uppercut": "Uppercut",
    "UppercutJab": "Uppercut Jab",
    "BodyJab": "Body Jab Cross",
    "Combo": "Punch Combo",
    "Punching": "Punching",
    # nogi i kolana
    "KickMma": "Mma Kick",
    "KickRound": "Roundhouse Kick",
    "KickSide": "Side Kick",
    "Kicking": "Kicking",
    "Knee": "Illegal Knee",
    "Elbow": "Illegal Elbow Punch",
    # obrona
    "Block": "Blocking",
    "BlockCenter": "Center Block",
    "BlockLeft": "Left Block",
    "BlockRight": "Right Block",
    "Dodge": "Dodging",
    "Duck": "Ducking",
    # reakcje i pokaz
    "HitHead": "Head Hit",
    "HitBody": "Hit To Body",
    "Stumble": "Stumble Backwards",
    "WarmUp": "Warming Up",
    "Taunt": "Taunt",
    "Defeat": "Defeat",
})

ZESTAWY = {"brax": ZAWODNIK, "nyxa": ZAWODNIK}


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


def doloz_ujecia(rig, katalog, moje, zestaw):
    """Wciąga akcje z plików animacji i nazywa je stanami, których szuka potok."""
    bpy = blender()
    pliki = {p.stem: p for p in Path(katalog).glob("*.fbx")}
    dodane = []
    for stan, zrodlo in zestaw.items():
        plik = pliki.get(zrodlo)
        if plik is None:
            print("BRAX_PACZKA BRAK nagrania %s dla stanu %s" % (zrodlo, stan))
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
    """R = metaliczność, A = gładkość (1 − szorstkość). Tak czyta shader obsady.

    Liczone w BLENDERZE, nie w PIL. Build 44287173708 wywrócił się na
    „No module named 'PIL'" — Python serwera go nie ma, Blender ma numpy i własne
    wczytywanie obrazów, więc robota idzie tam, gdzie i tak już jesteśmy.
    """
    import numpy as np
    bpy = blender()
    pliki = list(Path(katalog_map).iterdir())

    def znajdz(*klucze):
        for p in pliki:
            if any(k in p.name.lower() for k in klucze):
                return p
        return None

    szor = znajdz("roughness", "rough")
    if not szor:
        raise SystemExit("brak mapy szorstkości w " + str(katalog_map))
    met = znajdz("metallic", "metalness")

    def wczytaj(sciezka):
        im = bpy.data.images.load(str(sciezka), check_existing=False)
        im.colorspace_settings.name = "Non-Color"
        px = np.empty(len(im.pixels), np.float32)
        im.pixels.foreach_get(px)
        w, h = im.size
        bpy.data.images.remove(im)
        return px.reshape(h, w, 4)

    s = wczytaj(szor)
    h, w = s.shape[:2]
    m = wczytaj(met) if met else None
    if m is not None and m.shape[:2] != (h, w):
        # Dopasowanie bez skalowania obrazu: próbkowanie najbliższego piksela.
        yi = (np.arange(h) * m.shape[0] // h).clip(0, m.shape[0] - 1)
        xi = (np.arange(w) * m.shape[1] // w).clip(0, m.shape[1] - 1)
        m = m[yi][:, xi]

    out = np.zeros((h, w, 4), np.float32)
    out[:, :, 0] = m[:, :, 0] if m is not None else 0.0
    out[:, :, 3] = 1.0 - s[:, :, 0]
    nowa = bpy.data.images.new("MetalSmooth", width=w, height=h, alpha=True, float_buffer=False)
    nowa.colorspace_settings.name = "Non-Color"
    nowa.pixels.foreach_set(out.reshape(-1))
    nowa.filepath_raw = str(cel)
    nowa.file_format = "PNG"
    nowa.save()
    bpy.data.images.remove(nowa)
    print("BRAX_PACZKA MetalSmooth %dx%d: metalicznosc z %s, gladkosc z %s"
          % (w, h, met.name if met else "zera", szor.name))


def main(argv):
    model, animacje, wyjscie, ident = argv[:4]
    wyjscie = Path(wyjscie)
    wyjscie.mkdir(parents=True, exist_ok=True)

    rig = wczytaj_model(model)
    moje = set(__import__('bpy').context.scene.objects)
    zestaw = ZESTAWY.get(ident, PODSTAWA)
    stany = doloz_ujecia(rig, animacje, moje, zestaw)

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
    metalsmooth(fbm, wyjscie / "MetalSmooth.png")

    (wyjscie / "postac.json").write_text(json.dumps(
        {"id": ident, "states": stany, "zrodlo": os.path.basename(model),
         "uwaga": "paczka zlozona z surowej konwersji Tripo + globalna biblioteka ruchow"},
        ensure_ascii=False, indent=1), encoding="utf-8")
    print("BRAX_PACZKA gotowe: %s, ujecia: %s" % (ident, ", ".join(stany)))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:])
