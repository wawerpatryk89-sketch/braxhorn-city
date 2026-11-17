"""Wygładzanie wag skinningu — żeby łokcie i kolana się nie załamywały.

Właściciel 17.11, 14:46: „jak najmniej deformacji, kanciaków".

ZMIERZONE NA CAŁEJ OBSADZIE (audyt_postaci.py, 19 postaci z paczki wejściowej):
  - normalne są w porządku: rozciętych 0–0,8 %, więc kanciaki NIE biorą się
    z twardego cieniowania;
  - wagi są znormalizowane: 0 % wierzchołków poza sumą 1;
  - ale od 43 % do 68 % wierzchołków trzyma JEDNA ALBO DWIE kości, a cztery
    i więcej ma tylko 20–39 %. Najgorzej: imprezowiczka_11baefe5 68 %,
    imprezowiczka_13e8d41d 65 %, kiler 62 %, wiechu 61 %.

Jedna kość na wierzchołek znaczy, że przy zgięciu ten wierzchołek jedzie sztywno
za kością, a jego sąsiad za drugą — i na zgięciu robi się załamanie zamiast fałdy.
To jest najczęstsza przyczyna „dziwnych" deformacji i jedyna z tej trójki, którą
da się naprawić w potoku, bez nowych modeli ze źródła.

Co robi ta funkcja, po kolei:
  1. wygładza wagi po sąsiadach w siatce (tylko tam, gdzie spotykają się kości —
     czyli przy stawach; reszta zostaje bez zmian),
  2. ogranicza do czterech kości na wierzchołek, bo tyle bierze Unity,
  3. normalizuje, żeby suma dalej wynosiła 1.

Wygładzanie jest umiarkowane celowo: za mocne rozmywa palce i rysy twarzy.
"""
import bpy

# Palce i twarz zostają nietknięte — tam sąsiednie kości są o milimetry od siebie
# i wygładzanie zrobiłoby z dłoni rękawicę.
POMIJANE = ("Hand", "Thumb", "Index", "Middle", "Ring", "Pinky", "Head", "Eye", "Jaw")


def wygladz_wagi(mesh, rig, sila=0.45, powtorzen=2, limit=4):
    """Rozmywa wagi na stawach, ogranicza do `limit` kości i normalizuje.

    Zwraca krotkę (przed, po) ze średnią liczbą kości na wierzchołek, żeby
    w logu było widać, czy coś w ogóle drgnęło.
    """
    def srednia_kosci():
        ile = 0
        for v in mesh.data.vertices:
            ile += sum(1 for g in v.groups if g.weight > 1e-4)
        return ile / max(1, len(mesh.data.vertices))

    przed = srednia_kosci()

    poprzedni = bpy.context.view_layer.objects.active
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh

    # Grupy palców i twarzy blokujemy — zablokowana grupa nie bierze udziału
    # ani w wygładzaniu, ani w normalizacji.
    zablokowane = []
    for g in mesh.vertex_groups:
        krotka = g.name.split(":")[-1]
        if any(k in krotka for k in POMIJANE):
            g.lock_weight = True
            zablokowane.append(g.name)

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.vertex_group_smooth(
        group_select_mode="ALL", factor=sila, repeat=powtorzen, expand=0.0
    )
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=limit)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)

    for g in mesh.vertex_groups:
        g.lock_weight = False

    bpy.context.view_layer.objects.active = poprzedni
    po = srednia_kosci()
    print(
        "BRAX_WAGI %s: kości na wierzchołek %.2f -> %.2f (wygładzone %d grup, "
        "pominięte %d: palce i twarz)"
        % (mesh.name, przed, po, len(mesh.vertex_groups) - len(zablokowane), len(zablokowane))
    )
    return przed, po
