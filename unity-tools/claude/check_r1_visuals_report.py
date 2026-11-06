#!/usr/bin/env python3
"""Nonblocking R1 visual preflight — not a substitute for Unity/Android play mode.

Scope requested by owner on 2026-11-06: supermarket BRAXHORN -> tire shop,
courts, port, beach, and the entire island; cockpit/hand grip on all boats.
This reports conditions only. It never prevents APK generation.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "unity-city" / "Assets"


def source(path):
    p = SRC / path
    return p.read_text(encoding="utf-8") if p.exists() else ""


def check(s, pattern):
    return bool(re.search(pattern, s, flags=re.S))


def main():
    build = source("Editor/BraxGymBuild.cs")
    island = source("Editor/Claude/Region/BraxMiastoWyspa.cs")
    proxy = source("Claude/Runtime/BraxWyspaDaleko.cs")
    stream = source("Claude/BraxObszary.cs")
    lod = source("Claude/Runtime/BraxZasiegDrobnych.cs")
    marina = source("Editor/Claude/Region/BraxNabrzezeMarina.cs")
    vessel = source("Claude/BraxVessel.cs")
    pose = source("Claude/BraxPose.cs")
    flicker = source("Editor/Claude/BraxKontrolaMigotania.cs")
    ground = source("Editor/Claude/Region/BraxNabrzezePlaza.cs")
    domki = source("Editor/Claude/Region/BraxMiastoWyspaDomki.cs")
    collision = source("Editor/Claude/BraxKontrolaKolizji.cs")

    probes = {
        "R1.port_beach_prefetch_175m": check(
            build, r"BraxhornCity_Nabrzeze.{0,300}175f,350f,Generated"),
        "R1.island_prefetch_350m": check(
            build, r"BraxhornCity_Wyspa.{0,220}350f,580f,Generated"),
        "R1.island_proxy_scene_event": check(proxy, r"sceneLoaded.*Wczytano") and
            check(proxy, r"sceneUnloaded.*Zwolniono"),
        "R1.island_no_distance_vanish": "d.sqrMagnitude > blizej" not in proxy,
        "R1.island_original_PBR_same_weights": check(
            island, r"WagiTerenu") and
            check(island, r"r.sharedMaterial = wyspaMaterialTerenu"),
        "R1.beach_old_mesh_cut": check(
            ground, r"Wytnij\(.{0,160}plaza_od_nowa"),
        "R1.old_surfaces_prioritized_for_cut": check(
            flicker, r"aStary != bStary \? aStary") and
            check(flicker, r"BraxClaudeGround.SourceCity"),
        "R1.old_mesh_collider_kept_in_sync": check(
            flicker, r"c.sharedMesh == oryginal\) c.sharedMesh = mesh"),
        "R1.quality_cut_bounded_nonblocking": check(
            flicker, r"miasto\" \? 90 : 240") and
            check(flicker, r"miasto\" \? 320 : 600"),
        "R1.island_in_full_cut_scope": check(
            flicker, r"BraxMiastoWyspa\.CX - 242f") and
            check(flicker, r"BraxMiastoWyspa\.CZ \+ 242f"),
        "R1.no_false_zero_flicker": check(
            flicker, r"BRAXHORN_R1_NAWIERZCHNIE_WYMAGA_TESTU") and
            check(flicker, r"pominieteCiezkie"),
        "R1.cottages_physical_openings_not_solid": check(
            domki, r"SegmentSciany\(xPoprzedni, otwor\.xMin") and
            check(domki, r"wejsciaDomkow\.Add\(\(tabliczka") and
            not check(domki, r'BraxNabrzeze\.Kol\(kol, "Domek " \+ tabliczka, P\(0, h \* \.5f'),
        "R1.each_cottage_physically_audited": check(
            domki, r"WYSPA_DOMKI_WEJSCIA") and
            check(domki, r"foreach \(var wejscie in wejsciaDomkow\)"),
        "R1.beach_wc_and_changing_open": check(
            domki, r"WC plazy otwarte drzwi") and
            check(domki, r"Przebieralnia otwarte drzwi") and
            not check(domki, r'BraxNabrzeze\.Kol\(kol, "WC plaży",'),
        "R1.island_no_synthetic_box_over_doors": check(
            collision, r"j\.rs\.All\(AutorskaSiatkaWyspy\)"),
        "R1.courts_keep_original_LOD0_visible": check(
            lod, r"ForceLOD\(pokaz \? 0 : -1\)"),
        "R1.courts_LOD_activated_in_city_only": check(
            stream, r"if \(r1 && !GetComponent<BraxZasiegDrobnych>\(\)\)"),
        "R1.boats_cockpit_yacht_and_motorboat": check(
            vessel, r"HasPilotCamera => rider && AtHelm && label != \"SKUTER\""),
        "R1.boats_original_helm_search": check(
            marina, r"IndexOf\(\"wheel\"") and
            check(marina, r"IndexOf\(\"guidon\""),
        "R1.boats_grips_connected": check(
            marina, r"vessel.leftGrip = Uchwyt") and
            check(vessel, r"pose.SetGrips\(leftGrip, rightGrip\)"),
        "R1.boats_arm_IK_no_mesh_scaling": check(
            pose, r"static void DlonNaUchwyt") and
            check(pose, r"dl1 \+ dl2 - .025f"),
        "R1.boats_durable_scene_on_board": check(
            vessel, r"MoveGameObjectToScene\(gameObject, riderScene\)"),
        "R1.boats_no_unload_when_occupied": check(
            stream, r"v.Occupied && v.gameObject.scene == sc"),
    }
    issues = [name for name, ok in probes.items() if not ok]
    report = {
        "scope": "BRAXHORN CITY R1 supermarket -> tire shop, courts, port, beach, whole island",
        "checks": len(probes),
        "source_checks_passed": len(probes) - len(issues),
        "source_findings": issues,
        "unity_editor_test": "NOT_RUN",
        "android_gameplay_flicker_test": "NOT_RUN",
        "guaranteed_zero_flicker": False,
        "blocks_apk": False,
    }
    print("BRAXHORN_R1_WIZUAL_PRZED_APK " + json.dumps(report, ensure_ascii=False))
    # Wymóg właściciela: kontrola jakości TYLKO DO RAPORTU.
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
