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


# --- PALCE -------------------------------------------------------------------------------
# Pomiar 23.11 (rk_mierz.py): przy przenoszeniu obrotu swiata krawedzie siatki palcow zapadaly
# sie do 1-10 % dlugosci (zatwierdzona obsada nigdy ponizej 15 %) — palce skladaly sie przez
# dlon. Powod: kosci palcow w modelach z Tripo maja inaczej ustawione osie niz rekawice Rokoko.
# Dlatego z nagrania biore TYLKO KAT ZGIECIA kazdego stawu palca (w plaszczyznie dloni)
# i zginam nasz palec o ten kat wokol jego wlasnej osi zgiecia. Palec nie moze sie juz
# wykrecic na boki ani przejsc przez dlon; kat jest dodatkowo ograniczony do zakresu stawu.
PALCE = ("Index", "Middle", "Ring", "Pinky", "Thumb")
OS_Y = Vector((0, 1, 0))


def kat_zgiecia(p, d, n):
    c = n.cross(p)
    if c.length < 1e-6:
        return 0.0
    c.normalize()
    return math.atan2(p.cross(d).dot(c), p.dot(d))


def dlon_spoczynek(szk, st):
    g = szk.glowy
    if any(st + k not in g for k in ("Hand", "HandIndex1", "HandPinky1", "HandMiddle1")):
        return None
    n = (g[st + "HandIndex1"] - g[st + "Hand"]).cross(g[st + "HandPinky1"] - g[st + "Hand"]).normalized()
    kier = (g[st + "HandMiddle1"] - g[st + "Hand"]).normalized()
    lancuchy = []
    for p in PALCE:
        lan = [st + "Hand" + p + str(i) for i in (1, 2, 3) if st + "Hand" + p + str(i) in g]
        if lan:
            lancuchy.append((p, lan))
    return n, kier, lancuchy


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
    # OBROT DLONI WOKOL OSI PRZEDRAMIENIA. Wlasciciel 23.11, 15:14: „dlonie strasznie powykrecane
    # w spoczynku”. Zmierzone: po samym wyrownaniu kierunku wnetrze dloni naszej postaci jest
    # obrocone o 83–93° wzgledem aktora (poza A z Tripo vs poza T nagrania) — w kazdym ruchu
    # dlon stoi wnetrzem do przodu zamiast do uda.
    # Obrot robi PRZEDRAMIE, nie ramie — tak jak u czlowieka (nawracanie/odwracanie dloni).
    # Pierwsza proba (15:20) krecila tez ramieniem: dlon dobra, ale bark i rekaw skrecone o 90°
    # (pasek na bluzie tancerki przeszedl na przod). Teraz: lokiec bierze polowe obrotu,
    # nadgarstek druga polowe — zadna skora nie skreca sie bardziej niz o ~45°.
    lokiec = float(opcje.get("skret_lokiec", 0.5))
    for st in ("Left", "Right"):
        dz, dc = dlon_spoczynek(szk_z, st), dlon_spoczynek(szk_c, st)
        if not dz or not dc:
            continue
        k = st + "ForeArm"
        os_k = C @ (szk_z.glowy[DZIECKO[k]] - szk_z.glowy[k]).normalized()
        nc, nz = A[k] @ dc[0], C @ dz[0]
        a, b = nc - os_k * nc.dot(os_k), nz - os_k * nz.dot(os_k)
        if a.length < 1e-6 or b.length < 1e-6:
            continue
        a.normalize(); b.normalize()
        kat = math.atan2(a.cross(b).dot(os_k), a.dot(b))
        A[st + "Hand"] = Quaternion(os_k, kat) @ A[k]
        A[k] = Quaternion(os_k, kat * lokiec) @ A[k]
        print("BRAX_DLON %s %s: wnetrze dloni obrocone o %.0f° (lokiec %.0f°, nadgarstek %.0f°)"
              % (stan, st, math.degrees(kat), math.degrees(kat * lokiec), math.degrees(kat * (1 - lokiec))), flush=True)
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

    # --- palce: kat zgiecia kazdego stawu z nagrania, os zgiecia z naszej dloni ---
    palce_kat = {}      # klucz kosci -> lista katow (rad) na klatke
    palce_os = {}       # klucz kosci -> os zgiecia w lokalnym ukladzie NASZEJ kosci
    for st in ("Left", "Right"):
        dz = dlon_spoczynek(szk_z, st)
        dc = dlon_spoczynek(szk_c, st)
        if not dz or not dc:
            continue
        nz, kz, lz = dz
        nc, kc_, lc = dc
        wsp_lan = {p: [k for k in lan if k in wsp and k in szk_c.kosci] for p, lan in lz}
        # os zgiecia naszej kosci (w jej ukladzie spoczynkowym)
        for p, lan in lc:
            poprz = kc_
            for k in lan:
                d_r = szk_c.qspocz[k] @ OS_Y
                c = nc.cross(poprz)
                if c.length > 1e-6 and k in wsp_lan.get(p, []):
                    palce_os[k] = szk_c.qspocz[k].inverted() @ c.normalized()
                poprz = d_r
        # kat zgiecia z nagrania, klatka po klatce
        serie = {}
        for pos_z, D in probki:
            n_cur = D[st + "Hand"] @ nz
            poprz_c, poprz_r = D[st + "Hand"] @ kz, kz
            for p, lan in lz:
                poprz_c, poprz_r = D[st + "Hand"] @ kz, kz
                for k in lan:
                    d_r = szk_z.qspocz[k] @ OS_Y
                    d_c = D[k] @ d_r if k in D else d_r
                    serie.setdefault(k, []).append(kat_zgiecia(poprz_c, d_c, n_cur) - kat_zgiecia(poprz_r, d_r, nz))
                    poprz_c, poprz_r = d_c, d_r
        # w ktora strone jest zgiecie do srodka dloni: w nagraniu palce zginaja sie duzo,
        # a prostuja do tylu malo — znak przewazajacych duzych katow to znak zgiecia
        duze = [v for p in ("Index", "Middle", "Ring", "Pinky") for v in serie.get(st + "Hand" + p + "1", [])
                if abs(v) > math.radians(12)]
        znak = 1.0 if sum(duze) >= 0 else -1.0
        for k, vals in serie.items():
            if k not in palce_os:
                continue
            kciuk = "Thumb" in k
            # Gleboka piesc (90°+ w kazdym stawie) zgniata siatke palca z Tripo przy kostce do
            # 7 % dlugosci krawedzi — pomiar 23.11. Do 55° w stawie dlon zostaje cala, a z odleglosci
            # telefonu luzno zacisnieta piesc czyta sie tak samo.
            pmax = float(opcje.get("palce_max", 55))
            lo, hi = (math.radians(-10), math.radians(min(30, pmax))) if kciuk else (math.radians(-8), math.radians(pmax))
            mn = 0.5 if kciuk else 1.0
            palce_kat[k] = [znak * min(hi, max(lo, znak * v * mn)) for v in vals]
        # PALCE ZGINAJA SIE RAZEM. Pomiar 23.11 (tancerka, Radosc): skora miedzy srodkowym
        # a serdecznym rozciagnieta x3,4–3,9, bo nagranie zgina je kazdy inaczej. W dloni z Tripo
        # palce maja wspolne wagi, wiec rozny kat dwoch sasiednich palcow rwie skore miedzy nimi.
        # Kat kazdego stawu idzie w strone sredniej czterech palcow na tym samym poziomie.
        razem = float(opcje.get("palce_razem", 0.7))
        for seg in "123":
            kl = [st + "Hand" + p + seg for p in ("Index", "Middle", "Ring", "Pinky") if st + "Hand" + p + seg in palce_kat]
            if len(kl) < 2:
                continue
            sr = [sum(palce_kat[x][i] for x in kl) / len(kl) for i in range(len(palce_kat[kl[0]]))]
            for x in kl:
                palce_kat[x] = [razem * a + (1 - razem) * v for a, v in zip(sr, palce_kat[x])]

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
    for nr_kl, (pos_z, D) in enumerate(probki):
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
                if k in palce_kat:
                    q_baza = Quaternion(palce_os[k], palce_kat[k][nr_kl])
                elif k.startswith(("LeftHand", "RightHand")) and k not in ("LeftHand", "RightHand"):
                    q_baza = Quaternion()          # palec bez pary w nagraniu: zostaje jak w modelu
                else:
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


# --- RECE NIE WCHODZA W CIALO ----------------------------------------------------------
# Pomiar 23.11: przedramiona i dlonie wchodzily w tulow i uda do 13 cm (zatwierdzona obsada:
# do ~3,5 cm, i to przy piciu). Powod: ruch nagrany na szczuplym aktorze, a nasze postacie
# sa szersze (kulturysta, niedzwiedzica). Kat w stawie barkowym jest przeniesiony wiernie,
# ale tam, gdzie aktor mial wolne miejsce, nasza postac ma juz brzuch albo udo.
# Poprawka: w kazdej klatce sprawdzam na odksztalconej siatce, czy przedramie lub dlon sa pod
# skora tulowia, glowy albo ud. Jesli tak, odwodze ramie w bok (obrot w barku na zewnatrz)
# o najmniejszy kat, przy ktorym reka juz nie przenika — najwyzej 35°. Kat wygladzam w czasie,
# zeby reka nie skakala. Nic nie jest ustawiane recznie.
RDZEN_CIALA = {"Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftUpLeg", "RightUpLeg",
               "LeftShoulder", "RightShoulder"}


def klasy_siatki(siatki):
    """Dla kazdej siatki: twarze rdzenia ciala i wierzcholki przedramion/dloni (lewe, prawe)."""
    out = []
    for m in siatki:
        grupy = {g.index: klucz(g.name) for g in m.vertex_groups}
        klasa = []
        for v in m.data.vertices:
            best, w = "", 0.0
            for g in v.groups:
                if g.weight > w and g.group in grupy:
                    best, w = grupy[g.group], g.weight
            klasa.append(best)
        rdzen = [p.index for p in m.data.polygons if all(klasa[i] in RDZEN_CIALA for i in p.vertices)]
        reka = {st: [i for i, k in enumerate(klasa) if k.startswith((st + "ForeArm", st + "Hand"))][::2]
                for st in ("Left", "Right")}
        out.append((m, rdzen, reka))
    return out


def wewnatrz(bvh, p, nrm, H):
    """Punkt „za” najblizsza scianka tulowia jest naprawde W SRODKU tylko wtedy, gdy idac dalej
    w glab (przeciwnie do normalnej) trafiamy na druga scianke OD SRODKA. Bez tego dziury
    w platku tulowia (wierzcholki przy pachach przypisane do ramienia) daja falszywe
    „przenikanie” na glebokosc calej klatki piersiowej — reka za plecami liczy sie jak w piersi."""
    kier = -nrm
    hit = bvh.ray_cast(p + kier * (0.002 * H), kier, 0.6 * H)
    return hit[0] is not None and hit[1].dot(kier) > 0


def glebokosc(bvh, punkty, H, kierunek=False):
    g = 0.0
    suma = Vector((0, 0, 0))
    for p in punkty:
        loc, nrm, idx, dist = bvh.find_nearest(p)
        if loc is None or dist > 0.15 * H:
            continue
        pod = -(p - loc).dot(nrm)
        if pod > 0 and not wewnatrz(bvh, p, nrm, H):
            continue
        if pod > 0:
            suma += nrm * pod
        if pod > g:
            g = pod
    if kierunek:
        return g, (suma.normalized() if suma.length > 1e-9 else None)
    return g


def odsun_rece(cel, akcja, klasy, szk_c, H, co_ile=2, maks=math.radians(35)):
    from mathutils.bvhtree import BVHTree
    scena = bpy.context.scene
    cel.animation_data.action = akcja
    prog = 0.012 * H
    n = int(round(akcja.frame_range[1]))
    bok = szk_c.os.col[0]
    mw = cel.matrix_world
    kosc = {st: {"Arm": szk_c.kosci[st + "Arm"].name, "Hand": szk_c.kosci[st + "Hand"].name} for st in ("Left", "Right")}
    probki = {"Left": [], "Right": []}
    klatki = list(range(1, n + 1, co_ile))
    if klatki[-1] != n:
        klatki.append(n)
    przed = {"Left": 0.0, "Right": 0.0}
    kierunki = {"Left": [], "Right": []}
    for f in klatki:
        scena.frame_set(f)
        dg = bpy.context.evaluated_depsgraph_get()
        wsp, twarze, reka = [], [], {"Left": [], "Right": []}
        for (m, rdzen, rk) in klasy:
            e = m.evaluated_get(dg); me = e.to_mesh()
            co = [m.matrix_world @ v.co for v in me.vertices]
            baza = len(wsp)
            wsp.extend(co)
            twarze.extend([[baza + i for i in me.polygons[j].vertices] for j in rdzen])
            for st in reka:
                reka[st].extend(co[i] for i in rk[st])
            e.to_mesh_clear()
        if not twarze:
            return
        bvh = BVHTree.FromPolygons(wsp, twarze, all_triangles=False)
        for st, zew in (("Left", bok), ("Right", -bok)):
            pb_a = cel.pose.bones[kosc[st]["Arm"]]; pb_h = cel.pose.bones[kosc[st]["Hand"]]
            bark = mw @ pb_a.head; dlon_p = mw @ pb_h.head
            v = dlon_p - bark
            os_ = v.cross(zew)
            g0, nrm = glebokosc(bvh, reka[st], H, kierunek=True)
            przed[st] = max(przed[st], g0)
            # Reka ma wyjsc z ciala ta strona, ktora weszla: przy gardzie (przedramie przed klatka)
            # do przodu, przy udzie w bok. Kierunek = srednia normalna skory w miejscach przenikania.
            if nrm is not None:
                kier = (nrm * 0.75 + zew * 0.25).normalized()
                os_ = v.cross(kier)
            kierunki[st].append(nrm * 0.75 + zew * 0.25 if nrm is not None else zew.copy())
            if g0 <= prog or os_.length < 1e-6:
                probki[st].append(0.0); continue
            os_.normalize()
            def g(alfa):
                R = Matrix.Rotation(alfa, 3, os_)
                return glebokosc(bvh, [bark + R @ (p - bark) for p in reka[st]], H)
            if g(maks) > prog:
                probki[st].append(maks); continue
            lo, hi = 0.0, maks
            for _ in range(6):
                mid = (lo + hi) / 2
                if g(mid) > prog:
                    lo = mid
                else:
                    hi = mid
            probki[st].append(hi)
    # wygladzenie: najpierw „poszerzenie” (max w oknie), potem srednia — bez skokow
    def gladko(vals):
        k = 3
        dil = [max(vals[max(0, i - k):i + k + 1]) for i in range(len(vals))]
        return [sum(dil[max(0, i - k):i + k + 1]) / len(dil[max(0, i - k):i + k + 1]) for i in range(len(dil))]
    katy = {}
    for st in probki:
        gl = gladko(probki[st])
        pelne = []
        for f in range(1, n + 1):
            j = min(range(len(klatki)), key=lambda x: abs(klatki[x] - f))
            if klatki[j] == f or j == len(klatki) - 1:
                pelne.append(gl[j])
            else:
                j0 = j if klatki[j] < f else j - 1
                j1 = j0 + 1
                t = (f - klatki[j0]) / (klatki[j1] - klatki[j0])
                pelne.append(gl[j0] * (1 - t) + gl[j1] * t)
        katy[st] = pelne
    # wpisanie do klatek kluczowych ramienia
    for st, zew in (("Left", bok), ("Right", -bok)):
        if max(katy[st]) < math.radians(0.5):
            continue
        nazwa = kosc[st]["Arm"]
        krzywe = [akcja.fcurves.find('pose.bones["%s"].rotation_quaternion' % nazwa, index=i) for i in range(4)]
        pb_a = cel.pose.bones[nazwa]; pb_h = cel.pose.bones[kosc[st]["Hand"]]
        w2a = cel.matrix_world.inverted().to_3x3()
        kier_s = kierunki[st]
        poprz = None
        for f in range(1, n + 1):
            alfa = katy[st][f - 1]
            scena.frame_set(f)
            if alfa > math.radians(0.2):
                j = max(0, min(len(klatki) - 2, next((x for x in range(len(klatki) - 1) if klatki[x + 1] >= f), len(klatki) - 2)))
                t = 0.0 if klatki[j + 1] == klatki[j] else min(1.0, max(0.0, (f - klatki[j]) / (klatki[j + 1] - klatki[j])))
                kw = kier_s[j].lerp(kier_s[j + 1], t)
                if kw.length < 1e-6:
                    kw = zew.copy()
                zew_arm = (w2a @ kw).normalized()
                v = pb_h.head - pb_a.head
                os_ = v.cross(zew_arm)
                if os_.length > 1e-6:
                    os_.normalize()
                    piv = pb_a.head.copy()
                    R = Matrix.Translation(piv) @ Matrix.Rotation(alfa, 4, os_) @ Matrix.Translation(-piv)
                    pb_a.matrix = R @ pb_a.matrix
            q = pb_a.rotation_quaternion.copy()
            if poprz is not None and poprz.dot(q) < 0:
                q = -q
            poprz = q
            for i in range(4):
                krzywe[i].keyframe_points[f - 1].co[1] = q[i]
        for c in krzywe:
            c.update()
    po = {}
    for st in ("Left", "Right"):
        po[st] = round(math.degrees(max(katy[st])), 1)
    print("BRAX_RECE %-16s przenikanie przed: L %.1f cm, P %.1f cm | odwiedzenie ramienia max: L %.0f°, P %.0f°"
          % (akcja.name, przed["Left"] * 100, przed["Right"] * 100, po["Left"], po["Right"]), flush=True)


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

    siatki_c = [o for o in moje if o.type == "MESH"]
    klasy = klasy_siatki(siatki_c)
    pkt = [m.matrix_world @ v.co for m in siatki_c for v in list(m.data.vertices)[::9]]
    H = max(p.z for p in pkt) - min(p.z for p in pkt)

    stany = []
    for wpis in spis["postacie"][ident]:
        if wpis["stan"] in wlasne:
            raise SystemExit("%s ma juz wlasne ujecie %s — nie nadpisuje" % (ident, wpis["stan"]))
        akcja = przenies(cel, szk_c, klipy / wpis["plik"], wpis["stan"], wpis)
        if cel.animation_data is None:
            cel.animation_data_create()
        if not wpis.get("bez_odsuwania"):
            odsun_rece(cel, akcja, klasy, szk_c, H)
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
