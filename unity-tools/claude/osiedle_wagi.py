"""Wygładzanie wag skinningu — żeby łokcie i kolana się nie załamywały.

Właściciel 17.11, 14:46: „jak najmniej deformacji, kanciaków".

ZMIERZONE NA CAŁEJ OBSADZIE (audyt_postaci.py, 19 postaci z paczki wejściowej):
  - normalne są w porządku: rozciętych 0–0,8 %, więc kanciaki NIE biorą się
    z twardego cieniowania, tylko z liczby trójkątów;
  - wagi są znormalizowane: 0 % wierzchołków poza sumą 1;
  - ale od 43 % do 68 % wierzchołków trzyma JEDNA ALBO DWIE kości, a cztery
    i więcej ma tylko 20–39 %. Najgorzej: imprezowiczka_11baefe5 68 %,
    imprezowiczka_13e8d41d 65 %, kiler 62 %, wiechu 61 %.

Jedna kość na wierzchołek znaczy, że przy zgięciu ten wierzchołek jedzie sztywno
za kością, a jego sąsiad za drugą — i na zgięciu robi się załamanie zamiast fałdy.
To jedyna z przyczyn deformacji, którą da się naprawić w potoku, bez nowych modeli.

Liczone WPROST NA DANYCH, bez operatorów Blendera: bpy.ops.object.vertex_group_smooth
wymaga trybu edycji i zaznaczenia, i w buildzie 44096850422 wywrócił się na
„context is incorrect". Ręczne uśrednianie po sąsiadach robi dokładnie to samo,
jest przewidywalne i nie zależy od tego, co akurat jest aktywne w scenie.
"""
from collections import defaultdict

# Palce i twarz zostają nietknięte — tam sąsiednie kości są o milimetry od siebie
# i wygładzanie zrobiłoby z dłoni rękawicę, a z twarzy maskę.
POMIJANE = ("Hand", "Thumb", "Index", "Middle", "Ring", "Pinky", "Head", "Eye", "Jaw")
PROG = 1e-4


def wygladz_wagi(mesh, rig=None, sila=0.45, powtorzen=2, limit=4):
    """Uśrednia wagi po sąsiadach w siatce, ogranicza do `limit` kości, normalizuje.

    Zwraca (przed, po) — średnią liczbę kości na wierzchołek — żeby w logu było
    widać, czy cokolwiek drgnęło.
    """
    dane = mesh.data
    nietykalne = {
        g.index for g in mesh.vertex_groups
        if any(k in g.name.split(":")[-1] for k in POMIJANE)
    }

    # 1. wagi do słowników
    wagi = [dict() for _ in dane.vertices]
    for v in dane.vertices:
        for g in v.groups:
            if g.weight > PROG:
                wagi[v.index][g.group] = g.weight
    przed = sum(len(w) for w in wagi) / max(1, len(wagi))

    # 2. sąsiedzi z krawędzi siatki (nie z odległości — inaczej lewa noga
    #    oddawałaby wagi prawej tam, gdzie się stykają)
    sasiedzi = defaultdict(list)
    for e in dane.edges:
        a, b = e.vertices
        sasiedzi[a].append(b)
        sasiedzi[b].append(a)

    # 3. uśrednianie — tylko grupy ruchome, grupy nietykalne zostają jak były
    for _ in range(powtorzen):
        nowe = []
        for i in range(len(wagi)):
            stare = wagi[i]
            stale = {g: w for g, w in stare.items() if g in nietykalne}
            budzet = 1.0 - sum(stale.values())
            lista = sasiedzi.get(i)
            if budzet <= 1e-3 or not lista:
                nowe.append(stare)
                continue
            zebrane = defaultdict(float)
            for s in lista:
                for g, w in wagi[s].items():
                    if g not in nietykalne:
                        zebrane[g] += w
            n = len(lista)
            ruchome = {g: w for g, w in stare.items() if g not in nietykalne}
            for g in set(ruchome) | set(zebrane):
                ruchome[g] = (1 - sila) * ruchome.get(g, 0.0) + sila * (zebrane.get(g, 0.0) / n)
            suma = sum(w for w in ruchome.values() if w > PROG)
            if suma <= 0:
                nowe.append(stare)
                continue
            wynik = dict(stale)
            for g, w in ruchome.items():
                if w > PROG:
                    wynik[g] = w * budzet / suma
            nowe.append(wynik)
        wagi = nowe

    # 4. ograniczenie do `limit` kości i normalizacja do 1
    for i, w in enumerate(wagi):
        if len(w) > limit:
            najwieksze = sorted(w.items(), key=lambda t: -t[1])[:limit]
            w = dict(najwieksze)
        suma = sum(w.values())
        if suma > 0:
            w = {g: v / suma for g, v in w.items()}
        wagi[i] = w
    po = sum(len(w) for w in wagi) / max(1, len(wagi))

    # 5. zapis z powrotem
    grupy = {g.index: g for g in mesh.vertex_groups}
    for g in mesh.vertex_groups:
        g.remove([v.index for v in dane.vertices])
    przypisania = defaultdict(list)
    for i, w in enumerate(wagi):
        for g, v in w.items():
            przypisania[(g, round(v, 4))].append(i)
    for (g, v), lista in przypisania.items():
        if g in grupy:
            grupy[g].add(lista, v, "REPLACE")

    print(
        "BRAX_WAGI %s: kości na wierzchołek %.2f -> %.2f (nietykalne grupy: %d — palce i twarz)"
        % (mesh.name, przed, po, len(nietykalne))
    )
    return przed, po
