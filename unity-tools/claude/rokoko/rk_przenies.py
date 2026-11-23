"""Przeniesienie ruchu z obcego nagrania (Rokoko / Mixamo / HumanIK) na NASZA postac.

    blender -b -P rk_przenies.py -- <model.fbx> <katalog_wyjscia> <id> <zlecenie.json>

DLACZEGO NIE „AKCJA WPROST”. Poprzednia droga (zrob_paczke_postaci.doloz_ujecia) wkladala
akcje wprost, bo kosci maja te same nazwy. Nazwy sa te same, ale POZA SPOCZYNKOWA nie:
nagranie Rokoko spoczywa w pozie T, nasze modele z Tripo w pozie A (rece w dol, nogi
w rozkroku), kazdy troche inaczej. Krzywe akcji to obroty WZGLEDEM spoczynku, wiec ten
sam obrot nalozony na inny spoczynek daje inna poze — stad powykrecany Brax.

JAK TO DZIALA TUTAJ. Dla kazdej kosci i kazdej klatki:
  1. licze, o ile kosc zrodla obrocila sie W PRZESTRZENI SWIATA wzgledem swojego spoczynku,
  2. ten sam obrot swiata nakladam na nasza kosc — ale najpierw nasza kosc „prostuje” do
     kierunku, jaki ma kosc zrodla w spoczynku (ramie, przedramie, udo, golen), zeby poza A
     i poza T znaczyly to samo,
  3. przeliczam wynik na obrot lokalny naszej kosci i zapisuje jako zwykla akcje.
Biodra dostaja przesuniecie przeskalowane dlugoscia nog. Nic nie jest poprawiane recznie.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

WYROWNAJ = ("LeftArm", "LeftForeArm", "RightArm", "RightForeArm",
            "LeftUpLeg", "LeftLeg", "RightUpLeg", "RightLeg")
DZIECKO = {"LeftArm": "LeftForeArm", "LeftForeArm": "LeftHand",
           "RightArm": "RightForeArm", "RightForeArm": "RightHand",
           "LeftUpLeg": "LeftLeg", "LeftLeg": "LeftFoot",
           "RightUpLeg": "RightLeg", "RightLeg": "RightFoot"}


def klucz(nazwa):
    n = nazwa.split(":")[-1]
    for p in ("Character1_", "mixamorig_", "mixamorig"):
        if n.startswith(p) and len(n) > len(p):
            n = n[len(p):]
    return n


def kwat(m):
    return m.to_3x3().normalized().to_quaternion()


def uklad(glowy):
    """Os boczna, gorna i przednia postaci w swiecie — z pozycji kosci w spoczynku."""
    gora = (glowy["Neck"] - glowy["Hips"]).normalized()
    bok = glowy["LeftArm"] - glowy["RightArm"]
    bok = (bok - gora * gora.dot(bok)).normalized()
    przod = bok.cross(gora).normalized()
    return Matrix((bok, gora, przod)).transposed()   # kolumny: bok, gora, przod


class Szkielet:
    """Spoczynek szkieletu w swiecie. Gdy biodra NIE sa koscia, tylko samym obiektem szkieletu
    (tak zapisane sa chody Rokoko: obiekt „mixamorig:Hips”, kosci zaczynaja sie od ud i kregoslupa),
    obiekt gra role bioder, a jego spoczynek to postac stojaca prosto na ziemi."""

    def __init__(self, obj):
        self.obj = obj
        self.kosci = {}
        for b in obj.data.bones:
            k = klucz(b.name)
            if k not in self.kosci:
                self.kosci[k] = b
        self.biodra_obiekt = "Hips" not in self.kosci and klucz(obj.name) == "Hips"
        if self.biodra_obiekt:
            lok = {k: b.head_local.copy() for k, b in self.kosci.items()}
            lok["Hips"] = Vector((0, 0, 0))
            os_lok = uklad(lok)
            swiat = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))      # kolumny: bok=X, gora=Z, przod=-Y
            R = swiat @ os_lok.transposed()
            sk = obj.matrix_world.to_scale()
            self.mw = R.to_4x4() @ Matrix.Diagonal((sk.x, sk.y, sk.z, 1.0))
            dol = min((self.mw @ lok[k]).z for k in ("LeftToeBase", "RightToeBase") if k in lok)
            self.mw = Matrix.Translation((0, 0, -dol)) @ self.mw
        else:
            self.mw = obj.matrix_world.copy()
        self.qo = kwat(self.mw)
        self.glowy = {k: self.mw @ b.head_local for k, b in self.kosci.items()}
        self.qspocz = {k: self.qo @ kwat(b.matrix_local) for k, b in self.kosci.items()}
        if self.biodra_obiekt:
            self.glowy["Hips"] = self.mw.translation.copy()
            self.qspocz["Hips"] = self.qo.copy()
        self.os = uklad(self.glowy)

    def nogi(self):
        """(wysokosc bioder nad ziemia przy WYPROSTOWANYCH nogach, wysokosc ziemi) — liczone z kosci."""
        g = self.glowy
        up = self.os.col[1]
        ziemia = min(g[k].dot(up) for k in ("LeftToeBase", "RightToeBase") if k in g)
        kostka = max(0.0, min(g["LeftFoot"].dot(up), g["RightFoot"].dot(up)) - ziemia)
        udo = (g["LeftLeg"] - g["LeftUpLeg"]).length
        golen = (g["LeftFoot"] - g["LeftLeg"]).length
        zawias = (g["Hips"] - g["LeftUpLeg"]).dot(up)
        return zawias + udo + golen + kostka, ziemia


def wczytaj_zrodlo(plik, przed_obj, przed_akcje):
    bpy.ops.import_scene.fbx(filepath=str(plik), automatic_bone_orientation=False,
                             ignore_leaf_bones=False)
    nowe = [o for o in bpy.context.scene.objects if o not in przed_obj]
    rigi = [o for o in nowe if o.type == "ARMATURE"]
    if len(rigi) != 1:
        raise SystemExit("%s: spodziewam sie jednego szkieletu, jest %d" % (plik.name, len(rigi)))
    akcje = [a for a in bpy.data.actions if a.name not in przed_akcje]
    if not akcje:
        raise SystemExit("%s: brak akcji" % plik.name)
    akcja = max(akcje, key=lambda a: len(a.fcurves))
    for o in nowe:
        if o.type == "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    nowe = [o for o in bpy.context.scene.objects if o not in przed_obj]
    return rigi[0], akcja, nowe, akcje


def srednia(wart, okno):
    n = len(wart); pol = okno // 2; out = []
    for i in range(n):
        a, b = max(0, i - pol), min(n, i + pol + 1)
        seg = wart[a:b]
        out.append(sum(seg[1:], seg[0] * 1.0) / len(seg) if not isinstance(seg[0], float) else sum(seg) / len(seg))
    return out


def jeden_cykl_chodu(probki, szk_z, okno=33):
    """Chod w miejscu, twarza stale w jedna strone, przyciety do JEDNEGO pelnego cyklu (dwa kroki).

    Nagranie chodu to czlowiek idacy przez sale: skreca, zawraca, oddala sie. Gra potrzebuje
    petli w miejscu. Odejmuje wiec wygladzony kierunek bioder i wygladzony tor w poziomie
    (zostaje naturalne kolysanie w obrebie kroku), a potem szukam dwoch klatek, w ktorych
    nogi i rece sa w tym samym ukladzie i poruszaja sie tak samo — miedzy nimi jest cykl."""
    up, fwd, bok = szk_z.os.col[1], szk_z.os.col[2], szk_z.os.col[0]
    katy = []
    for pos, D in probki:
        f = D["Hips"] @ fwd
        f = f - up * f.dot(up)
        katy.append(math.atan2(f.dot(bok), f.dot(fwd)))
    for i in range(1, len(katy)):
        while katy[i] - katy[i - 1] > math.pi:
            katy[i] -= 2 * math.pi
        while katy[i] - katy[i - 1] < -math.pi:
            katy[i] += 2 * math.pi
    kat_gl = srednia(katy, okno)
    poz = [pos - up * pos.dot(up) for pos, _ in probki]
    poz_gl = srednia(poz, okno)
    p_ref = poz[0].copy()
    nowe = []
    for i, (pos, D) in enumerate(probki):
        Y = Quaternion(up, -kat_gl[i])
        D2 = {k: Y @ q for k, q in D.items()}
        nowe.append((p_ref + Y @ (poz[i] - poz_gl[i]) + up * pos.dot(up), D2))
    kier = {}
    for k, d in DZIECKO.items():
        kier[k] = (szk_z.glowy[d] - szk_z.glowy[k]).normalized()
    def cecha(i):
        out = []
        for k in kier:
            v = nowe[i][1][k] @ kier[k]
            out += [v.x, v.y, v.z]
        return out
    F = [cecha(i) for i in range(len(nowe))]
    n = len(F)
    # Jak mocno pracuja nogi w kazdej klatce. Bez tego najlepiej „domyka sie” fragment,
    # w ktorym aktor STOI (zawraca, czeka) — a to nie jest chod.
    nogi = [x for x, k in enumerate(kier) if "Leg" in k]
    ruch = [0.0] * n
    for i in range(n - 1):
        e = 0.0
        for kx in nogi:
            for c in range(3):
                d = F[i + 1][kx * 3 + c] - F[i][kx * 3 + c]
                e += d * d
        ruch[i] = math.sqrt(e)
    suma = [0.0]
    for e in ruch:
        suma.append(suma[-1] + e)
    def sredni_ruch(i, j):
        return (suma[j] - suma[i]) / (j - i)
    okna = sorted(sredni_ruch(i, min(n - 1, i + 30)) for i in range(0, max(1, n - 31)))
    prog = 0.8 * okna[int(0.9 * (len(okna) - 1))]
    najl = None
    for i in range(4, n - 28):
        for j in range(i + 24, min(i + 90, n - 4)):
            if sredni_ruch(i, j) < prog:
                continue
            c = 0.0
            for x in range(len(F[i])):
                d = F[i][x] - F[j][x]
                v = (F[i + 3][x] - F[i][x]) - (F[j + 3][x] - F[j][x])
                c += d * d + 2.0 * v * v
            if najl is None or c < najl[0]:
                najl = (c, i, j)
    if najl is None:
        raise SystemExit("nie znalazlem cyklu chodu w nagraniu")
    _, i, j = najl
    return nowe[i:j], j - i


def przenies(cel, szk_c, plik, stan, opcje):
    scena = bpy.context.scene
    przed_obj = set(scena.objects)
    przed_akcje = set(bpy.data.actions.keys())
    zr, akcja, nowe, akcje_zr = wczytaj_zrodlo(plik, przed_obj, przed_akcje)
    if zr.animation_data is None:
        zr.animation_data_create()
    zr.animation_data.action = akcja
    bpy.context.view_layer.update()
    szk_z = Szkielet(zr)
    wsp = [k for k in szk_c.kosci if k in szk_z.qspocz]
    brak = [k for k in ("Hips", "Spine", "Neck", "Head", "LeftArm", "LeftForeArm", "LeftHand",
                        "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot",
                        "RightUpLeg", "RightLeg", "RightFoot") if k not in wsp]
    if brak:
        raise SystemExit("%s: w zrodle brak kosci %s" % (plik.name, ", ".join(brak)))

    # Obrot swiata, ktory ustawia zrodlo twarza tam, gdzie patrzy nasza postac.
    C = (szk_c.os @ szk_z.os.transposed()).to_quaternion()
    Ci = C.inverted()

    # Wyrownanie pozy A do pozy zrodla: najkrotszy obrot kierunku naszej kosci na kierunek zrodla.
    A = {}
    for k in WYROWNAJ:
        d = DZIECKO[k]
        kc = (szk_c.glowy[d] - szk_c.glowy[k]).normalized()
        kz = C @ (szk_z.glowy[d] - szk_z.glowy[k]).normalized()
        A[k] = kc.rotation_difference(kz)
    # Postac o masywnych udach (minotaur) nie moze isc krokiem szczuplego aktora — kolana
    # wchodzilyby w siebie. Zostawiam jej czesc WLASNEGO rozstawu nog z pozy spoczynkowej.
    rozstaw = float(opcje.get("rozstaw_nog", 0.0))
    if rozstaw > 0:
        for k in ("LeftUpLeg", "RightUpLeg"):
            A[k] = Quaternion().slerp(A[k], 1.0 - rozstaw)
    def wyrownanie(k):
        if k in A:
            return A[k]
        b = szk_c.kosci[k]
        # dlon i palce ida sztywno za przedramieniem — zachowuja swoj ksztalt z modelu
        p = b.parent
        while p is not None:
            kp = klucz(p.name)
            if kp in ("LeftForeArm", "RightForeArm"):
                return A[kp]
            if kp in ("LeftLeg", "RightLeg", "LeftUpLeg", "RightUpLeg", "Hips", "Spine"):
                break
            p = p.parent
        return Quaternion()
    QA = {k: wyrownanie(k) for k in szk_c.kosci}

    hc, ziemia_c = szk_c.nogi()
    hz, ziemia_z = szk_z.nogi()
    skala = hc / hz
    up_c = szk_c.os.col[1]
    up_z = szk_z.os.col[1]

    f0, f1 = (int(round(x)) for x in akcja.frame_range)
    klatki = list(range(f0, f1 + 1))

    # --- probkowanie zrodla -------------------------------------------------
    pz = {k: zr.pose.bones[szk_z.kosci[k].name] for k in wsp if k in szk_z.kosci}
    qspz_i = {k: szk_z.qspocz[k].inverted() for k in wsp}
    probki = []          # na klatke: (pozycja bioder w swiecie zrodla, {kosc: obrot swiata wzgl. spoczynku})
    for f in klatki:
        scena.frame_set(f)
        mw_f = zr.matrix_world.copy()
        qo_f = kwat(mw_f)
        D = {}
        for k in wsp:
            q = qo_f @ kwat(pz[k].matrix) if k in pz else qo_f
            D[k] = q @ qspz_i[k]
        biodra = mw_f @ pz["Hips"].matrix.translation if "Hips" in pz else mw_f.translation.copy()
        probki.append((biodra, D))

    # --- przyciecie: nagrania mocap czesto zaczynaja i koncza sie poza T ---
    def w_pozie_t(D):
        return all(D[k].angle < math.radians(14) for k in ("LeftArm", "RightArm", "LeftForeArm", "RightForeArm"))
    a, b = 0, len(klatki) - 1
    while a < b - 30 and w_pozie_t(probki[a][1]):
        a += 1
    while b > a + 30 and w_pozie_t(probki[b][1]):
        b -= 1
    if a > 0:
        a = min(a + 8, b - 30)      # kilka klatek wyjscia z pozy T tez odpada
    if b < len(klatki) - 1:
        b = max(b - 8, a + 30)
    od = opcje.get("od")
    if od is not None:
        a = min(max(a, int(od)), b - 10)
    ile = int(opcje.get("ile", 240))
    b = min(b, a + ile - 1)
    probki = probki[a:b + 1]
    okres = None
    if opcje.get("chod"):
        probki, okres = jeden_cykl_chodu(probki, szk_z)
    n = len(probki)

    # --- nasza postac: kolejnosc od rodzica ---------------------------------
    kolej = []
    def idz(bone):
        kolej.append(bone)
        for c in bone.children:
            idz(c)
    for bone in cel.data.bones:
        if bone.parent is None:
            idz(bone)
    qo_ci = szk_c.qo.inverted()
    mwi = szk_c.mw.inverted()
    spocz = {bone.name: bone.matrix_local.copy() for bone in cel.data.bones}
    lokal = {bone.name: (spocz[bone.parent.name].inverted() @ spocz[bone.name]) if bone.parent else spocz[bone.name]
             for bone in cel.data.bones}
    klucze = {bone.name: klucz(bone.name) for bone in cel.data.bones}

    p0 = probki[0][0]
    srodek_c = szk_c.glowy["Hips"] - up_c * szk_c.glowy["Hips"].dot(up_c)   # rzut bioder na ziemie
    obroty = {bone.name: [] for bone in cel.data.bones if klucze[bone.name] in wsp}
    polozenia = []
    stopy = []
    for pos_z, D in probki:
        # biodra w swiecie naszej postaci
        wzgl = pos_z - p0
        wys = pos_z.dot(up_z) - ziemia_z
        poz = wzgl - up_z * wzgl.dot(up_z)
        cel_swiat = srodek_c + (C @ poz) * skala + up_c * (ziemia_c + wys * skala)
        M = {}
        for bone in kolej:
            nazwa = bone.name
            k = klucze[nazwa]
            if k in D and bone.name == szk_c.kosci[k].name:
                q_swiat = (C @ D[k] @ Ci) @ QA[k] @ szk_c.qspocz[k]
                q_arm = qo_ci @ q_swiat
                if bone.parent is None:
                    q_baza = kwat(spocz[nazwa]).inverted() @ q_arm
                else:
                    q_rodzic = kwat(M[bone.parent.name] @ lokal[nazwa])
                    q_baza = q_rodzic.inverted() @ q_arm
                lista = obroty[nazwa]
                if lista and lista[-1].dot(q_baza) < 0:
                    q_baza = -q_baza
                lista.append(q_baza)
                B = q_baza.to_matrix().to_4x4()
            else:
                B = Matrix.Identity(4)
            if bone.parent is None:
                if k == "Hips":
                    R = spocz[nazwa].to_3x3()
                    cel_arm = mwi @ cel_swiat
                    przes = R.inverted() @ (cel_arm - spocz[nazwa].translation)
                    polozenia.append(przes)
                    B = Matrix.Translation(przes) @ B
                M[nazwa] = spocz[nazwa] @ B
            else:
                M[nazwa] = M[bone.parent.name] @ lokal[nazwa] @ B
        niskie = min((szk_c.mw @ M[szk_c.kosci[s].name].translation).dot(up_c) for s in ("LeftFoot", "RightFoot"))
        stopy.append(niskie)

    # --- stala korekta wysokosci: postawiona stopa ma stac na ziemi ----------
    kostka_spocz = min(szk_c.glowy[s].dot(up_c) for s in ("LeftFoot", "RightFoot"))
    posort = sorted(stopy)
    nisko = posort[max(0, int(0.08 * n) - 1)]
    korekta = kostka_spocz - nisko
    korekta = max(-0.12 * hc, min(0.12 * hc, korekta))
    hips = szk_c.kosci["Hips"]
    Rh = spocz[hips.name].to_3x3()
    dodatek = Rh.inverted() @ (mwi.to_3x3() @ (up_c * korekta))
    polozenia = [p + dodatek for p in polozenia]

    # --- bez odjazdu: usun staly dryf poziomy, zeby petla nie skakala ---------
    if opcje.get("na_miejscu", False) and not opcje.get("chod") and n > 2:
        start, koniec = polozenia[0].copy(), polozenia[-1].copy()
        gora_lok = (Rh.inverted() @ (mwi.to_3x3() @ up_c)).normalized()
        dryf = koniec - start
        dryf = dryf - gora_lok * dryf.dot(gora_lok)
        polozenia = [p - dryf * (i / (n - 1)) for i, p in enumerate(polozenia)]

    # --- miekkie domkniecie petli -------------------------------------------
    domk = int(opcje.get("domkniecie", 5 if opcje.get("chod") else 0))
    if domk and n > domk * 3:
        for i in range(domk):
            w = (i + 1) / (domk + 1)
            w = w * w * (3 - 2 * w)
            j = n - domk + i
            for nazwa, lista in obroty.items():
                q0 = lista[0] if lista[0].dot(lista[j]) >= 0 else -lista[0]
                lista[j] = lista[j].slerp(q0, w)
            polozenia[j] = polozenia[j].lerp(polozenia[0], w)

    # --- zapis akcji ---------------------------------------------------------
    nowa = bpy.data.actions.new(stan)
    nowa.use_fake_user = True
    czasy = [float(i + 1) for i in range(n)]
    def krzywa(sciezka, indeks, wartosci, grupa):
        c = nowa.fcurves.new(sciezka, index=indeks, action_group=grupa)
        c.keyframe_points.add(n)
        plaskie = [0.0] * (2 * n)
        plaskie[0::2] = czasy
        plaskie[1::2] = wartosci
        c.keyframe_points.foreach_set("co", plaskie)
        for kp in c.keyframe_points:
            kp.interpolation = "LINEAR"
        c.update()
    for nazwa, lista in obroty.items():
        sc = 'pose.bones["%s"].rotation_quaternion' % nazwa
        for i in range(4):
            krzywa(sc, i, [q[i] for q in lista], nazwa)
    sc = 'pose.bones["%s"].location' % hips.name
    for i in range(3):
        krzywa(sc, i, [p[i] for p in polozenia], hips.name)

    # --- sprzatanie zrodla ---------------------------------------------------
    for o in nowe:
        if o.name in bpy.data.objects:
            bpy.data.objects.remove(o, do_unlink=True)
    for a_ in akcje_zr:
        if a_.name in bpy.data.actions and a_ != nowa:
            bpy.data.actions.remove(a_)
    for arm in list(bpy.data.armatures):
        if arm.users == 0:
            bpy.data.armatures.remove(arm)
    print("BRAX_RUCH %-16s <- %-40s klatek %d (z %d, okno od %d)%s, skala nog %.3f, korekta wys. %.1f cm, kosci %d"
          % (stan, plik.name[:40], n, len(klatki), a, (", cykl chodu %d kl." % okres) if okres else "",
             skala, korekta * 100, len(obroty)), flush=True)
    return nowa


def main(argv):
    model, wyjscie, ident, zlecenie = argv[:4]
    model = Path(model)
    wyjscie = Path(wyjscie)
    wyjscie.mkdir(parents=True, exist_ok=True)
    spis = json.loads(Path(zlecenie).read_text(encoding="utf-8"))
    klipy = Path(spis["klipy"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.ops.import_scene.fbx(filepath=str(model), automatic_bone_orientation=False,
                             ignore_leaf_bones=False)
    rigi = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    if len(rigi) != 1:
        raise SystemExit("spodziewam sie jednego szkieletu, jest %d" % len(rigi))
    cel = rigi[0]
    moje = set(bpy.context.scene.objects)
    # Wlasne ujecia postaci ZOSTAJA nietkniete — tylko krotka nazwa, jak w reszcie potoku.
    wlasne = []
    for akcja in list(bpy.data.actions):
        akcja.name = akcja.name.rsplit("|", 1)[-1]
        akcja.use_fake_user = True
        wlasne.append(akcja.name)
    for pb in cel.pose.bones:
        pb.rotation_mode = "QUATERNION"
    if cel.animation_data:
        cel.animation_data.action = None
    for pb in cel.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    szk_c = Szkielet(cel)

    stany = []
    for wpis in spis["postacie"][ident]:
        if wpis["stan"] in wlasne:
            raise SystemExit("%s ma juz wlasne ujecie %s — nie nadpisuje" % (ident, wpis["stan"]))
        akcja = przenies(cel, szk_c, klipy / wpis["plik"], wpis["stan"], wpis)
        stany.append({"stan": wpis["stan"], "zrodlo": wpis["plik"], "klatek": int(akcja.frame_range[1])})
    if cel.animation_data is None:
        cel.animation_data_create()
    pierwsza = "Idle" if "Idle" in bpy.data.actions else stany[0]["stan"]
    cel.animation_data.action = bpy.data.actions[pierwsza]
    bpy.context.scene.frame_set(1)

    bpy.ops.object.select_all(action="DESELECT")
    for o in moje:
        o.select_set(True)
    bpy.context.view_layer.objects.active = cel
    cel_fbx = wyjscie / (ident + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(cel_fbx), use_selection=True, object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0, path_mode="STRIP", embed_textures=False)

    # tekstury: gotowy zestaw obok modelu (postac z paczki) albo surowe mapy z Tripo (.fbm)
    import shutil
    if (model.parent / "Color.png").is_file():
        for n in ("Color.png", "Normal.png", "MetalSmooth.png"):
            shutil.copy(model.parent / n, wyjscie / n)
    else:
        sys.path.insert(0, spis["potok"])
        import zrob_paczke_postaci as zp
        fbm = next(p for p in model.parent.glob("*.fbm") if p.is_dir())
        for wzor, nazwa in (("basecolor", "Color.png"), ("color", "Color.png"), ("normal", "Normal.png")):
            zrod = next((p for p in fbm.iterdir() if wzor in p.name.lower()), None)
            if zrod and not (wyjscie / nazwa).exists():
                shutil.copy(zrod, wyjscie / nazwa)
        zp.metalsmooth(fbm, wyjscie / "MetalSmooth.png")
    (wyjscie / "postac.json").write_text(json.dumps(
        {"id": ident, "states": wlasne + [x["stan"] for x in stany], "zrodlo": model.name,
         "ruchy_przeniesione": stany,
         "uwaga": "ruchy Rokoko przeniesione obrotem swiata z wyrownaniem pozy spoczynkowej (rk_przenies.py)"},
        ensure_ascii=False, indent=1), encoding="utf-8")
    print("BRAX_RUCH zapisany %s (%.1f MB), ujec wlasnych %d, przeniesionych %d"
          % (cel_fbx.name, cel_fbx.stat().st_size / 1048576, len(wlasne), len(stany)))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:])
