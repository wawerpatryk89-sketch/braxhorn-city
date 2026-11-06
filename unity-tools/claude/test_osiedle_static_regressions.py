#!/usr/bin/env python3
"""Fast source-level regressions for the Unity 6 estate; not a device test.

Never claim a passed source test proves rendered appearance, navigation or APK.
"""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
PRYWATNE = ROOT / "unity-city/Prywatne"


def source(name):
    return (EDITOR / name).read_text(encoding="utf-8")


class EstateSourceRegressions(unittest.TestCase):
    def test_owner_animation_transfer_private_only(self):
        manifest = json.loads((ROOT / "unity-tools/claude/patryk-owner-22-manifest.json").read_text())
        transfer = PRYWATNE / "import-patryk-animations-20261106"
        self.assertTrue((transfer / "manifest.json").is_file(), "Encrypted owner FBX not committed")
        self.assertFalse((PRYWATNE / "Patryk_22_oryginalne_ruchy.zip").exists(),
                         "Plain owner FBX must not be left in the current public branch")
        self.assertEqual(manifest["animation_archive"]["file_count"], 22)
        script = (ROOT / "unity-tools/claude/prywatne.py").read_text()
        self.assertIn('install_patryk_owner_22(CEL / "ruchy")', script)
        self.assertNotIn('install_extra16(CEL / "ruchy")', script)
        self.assertIn('digest != clip["sha256"]', script)

    def test_skinned_car_geometry_not_destroyed_without_baking(self):
        car = source("BraxClaudeOsiedleAutaModel.cs")
        self.assertIn("sk.BakeMesh(baked)", car)
        self.assertIn("AddComponent<MeshFilter>()", car)
        self.assertIn("AddComponent<MeshRenderer>()", car)
        self.assertIn("if (A.ileKol != 4", car)
        self.assertIn("mesh.vertexCount < 12", car)
        self.assertIn("MaterialAutaAndroid", car)

    def test_cars_use_only_persistent_compatible_materials(self):
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        car = source("BraxClaudeOsiedleAutaModel.cs")
        self.assertIn("!m.shader.isSupported", scene)
        self.assertIn('shader.IndexOf("Universal Render Pipeline"', scene)
        self.assertIn('shader.IndexOf("Shader Graphs/"', scene)
        self.assertIn("BraxCityMaterials.Imported", car)

    def test_audit_does_not_count_arena_group_as_missing_wheels(self):
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        self.assertIn('t.name.StartsWith("Auto Patryka · ", StringComparison.Ordinal)', scene)
        wheel_audit = scene.split("var wszystkieAuta =", 1)[1].split("foreach (var auto in wszystkieAuta)", 1)[0]
        self.assertNotIn('t.name.StartsWith("Auto Patryka", StringComparison.Ordinal)', wheel_audit)
        self.assertIn('auto.Find("koło " + id + "/obrót")', scene)
        self.assertIn("patryki.Length != 1", scene)

    def test_npc_cabin_requires_real_head_and_anim_frames(self):
        cabins = source("BraxClaudeKabiny.cs")
        people = source("BraxClaudeOsiedleLudzieMiejsca.cs")
        passengers = source("BraxClaudePodrozni.cs")
        self.assertIn("if (klatki == 0) w.zapas = float.NegativeInfinity", cabins)
        self.assertIn("skora.Count == 0", cabins)
        self.assertIn("osoba.klipy.Length == 0", cabins)
        self.assertIn("if (!DopasujKabine(m, f))", people)
        self.assertIn("if (!DopasujKabine(m, seat))", passengers)

    def test_player_car_has_owner_clips_and_clearance_checks(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        private_car = source("BraxClaudeAutoPatryka.cs")
        self.assertIn('PK("EnteringCar"', player)
        self.assertIn('PK("Walk"', player)
        self.assertIn('PK("Run"', player)
        self.assertIn('if (wybor.t == null)', private_car)
        self.assertIn('car.drzwi', private_car)
        self.assertIn("ZapasPozy(skora", private_car)
        self.assertIn("brak wolnego stanowiska pod areną", private_car)

    def test_estate_scene_calls_all_fundamental_procedures(self):
        project = (ROOT / "unity-city/Assets/Editor/BraxCityBuild.cs").read_text()
        estate = source("BraxClaudeOsiedle.cs")
        for function in ("PatrykBohater", "AutoGraczaKontrola", "SalonPrzejscieKontrola",
                         "SportGryKontrola", "SprawdzAktualizacje"):
            self.assertIn(function + "(", project)
        for function in ("RuchNaOsiedlu", "LudzieNaOsiedlu", "DrzwiBezSciany", "SprawdzZlecenieOsiedla"):
            self.assertIn(function, estate)
        self.assertIn("BraxBuildQuality.Warn", source("BraxClaudeOsiedleKontrolaZlecenia.cs"))

    def test_no_unmeasured_patryk_headroom_or_invalid_roof(self):
        car = source("BraxClaudeAutoPatryka.cs")
        roof = source("BraxClaudeKabiny.cs")
        self.assertIn("skora.Count == 0", car)
        self.assertIn("skora.Sum(s => s.v.Length) < 8", car)
        self.assertIn("float.IsNaN(dach)", roof)
        self.assertIn("wl.zapas = float.NegativeInfinity", roof)

    def test_patryk_face_4k_import_not_silently_degraded(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        self.assertIn("if (!texture)", player)
        self.assertIn("texture.width != limit || texture.height != limit", player)
        self.assertIn("throw new InvalidOperationException(\"Patryk: nieprawidłowa tekstura", player)

    def test_player_car_does_not_block_its_own_doors(self):
        runtime = (ROOT / "unity-city/Assets/Claude/Runtime/BraxAutoGracza.cs").read_text()
        self.assertIn('!hits[i].collider.transform.IsChildOf(transform)', runtime)
        self.assertIn('if (Obcy(overlaps[i]))', runtime)
        self.assertIn('Physics.OverlapCapsuleNonAlloc', runtime)

    def test_parking_dimensions_and_traffic_qa_reset(self):
        vehicles = source("BraxClaudeOsiedleAuta.cs")
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        self.assertIn('miejsca.Add((m.p, -doSciany, 5f * K, 3f * K, true))', vehicles)
        self.assertIn('if (nrM++ % 2 != 0)', vehicles)
        self.assertNotIn('if (nrM++ % 3 != 0)', vehicles)
        self.assertIn('ruchZweryfikowany = false;', vehicles)
        self.assertIn('bladRuchu = null;', vehicles)
        self.assertIn('MiejscaParkingowe.Count == 0', scene)

    def test_all_missing_resources_reach_scene_qa(self):
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        contractual = source("BraxClaudeOsiedleKontrolaZlecenia.cs")
        doors = source("BraxClaudeOsiedleDrzwi.cs")
        self.assertIn('n.StartsWith("DO POPRAWKI:", StringComparison.OrdinalIgnoreCase)', scene)
        self.assertIn('rep.notes.Add("BŁĄD w kroku kontrola zlecenia: " + error)', contractual)
        self.assertIn('if (doPoprawki.Count > 0)', doors)

    def test_guest_door_connectivity_never_ignores_solid_wall(self):
        doors = source("BraxClaudeOsiedleWejscia.cs")
        self.assertIn('var kolideryDrzwi = new HashSet<Collider>', doors)
        self.assertIn('!kolideryDrzwi.Contains(buforKolizji[i])', doors)
        self.assertIn('if (!Wolne(sx, sz))', doors)
        self.assertIn('obszar poza limitem wiarygodnego testu przejścia', doors)
        self.assertIn('brak celów pozwalających zweryfikować dostęp', doors)
        self.assertIn('foreach (var d in zamknieteTu)', doors)
        self.assertIn('if (cofniete > 0)', doors)

    def test_bus_line_quality_and_mobile_pbr_are_verified(self):
        bus = source("BraxClaudeOsiedleAutobus.cs")
        self.assertIn('busModel = "kod";', bus)
        self.assertIn('busSkala = 1f;', bus)
        self.assertIn('materials[i] = MaterialAutaAndroid(materials[i])', bus)
        self.assertIn('" punktów osi trasy przebiega poza jezdnią', bus)
        self.assertIn('if (lista.Count == 0)', bus)
        self.assertIn('rep.notes.Add("DO POPRAWKI: autobus', bus)

    def test_missing_npc_and_parking_passengers_are_reported(self):
        people = source("BraxClaudeOsiedleLudzieMiejsca.cs")
        travelers = source("BraxClaudePodrozni.cs")
        self.assertIn("DO POPRAWKI: brak paczki Rocketbox", people)
        self.assertIn("DO POPRAWKI: obsada dyskoteki", people)
        self.assertIn("DO POPRAWKI: rozmowy przechodniów", people)
        self.assertIn('rep.notes.Add("DO POPRAWKI: podróżni parkingów', travelers)

    def test_sports_controls_use_only_latest_patryk_originals(self):
        player = (ROOT / "unity-city/Assets/BraxGymPlayer.cs").read_text(encoding="utf-8")
        rig = (ROOT / "unity-city/Assets/Claude/Runtime/BraxRuchySport.cs").read_text(encoding="utf-8")
        sport = source("BraxClaudeOsiedleSport.cs")
        court = (ROOT / "unity-city/Assets/Claude/Runtime/BraxSportBoisko.cs").read_text(encoding="utf-8")
        self.assertIn("if (explorationOnly && (key==\"punch\" || key==\"kick\"))", player)
        self.assertIn("!boisko || !boisko.Na", player)
        for clip in ("GoalkeeperDropKick", "GoalkeeperPlacingBall", "GoalkeeperCatch"):
            self.assertIn('"' + clip + '"', player)
            self.assertIn('"' + clip + '"', sport)
        self.assertIn('OnCios?.Invoke(transform.position,transform.forward,actionIsKick)', player)
        self.assertIn('if (p.explorationOnly)', rig)
        self.assertIn('BraxSport.Rakieta && rHand && rFore', rig)
        self.assertIn('BraxGymPlayer.OnCios += Cios', court)
        self.assertNotIn('gracz.clips.GetClip("Punch")', sport)
        self.assertNotIn('gracz.clips.GetClip("Kick")', sport)

    def test_owner_sports_and_required_facility_errors_reach_central_qa(self):
        sources = (
            source("BraxClaudeOsiedleSport.cs"),
            source("BraxClaudeOsiedleSalon.cs"),
            source("BraxClaudeOsiedleBoks.cs"),
            source("BraxClaudeOsiedleDyskoteka.cs"),
            source("BraxClaudeAutoPatryka.cs"),
        )
        for src in sources:
            self.assertIn('rep.notes.Add("DO POPRAWKI:', src)
            self.assertIn('BraxBuildQuality.Issue(qualityError)', src)
        self.assertIn('BraxSportBoisko.Gra.Siatkowka', sources[0])

    def test_owner_sport_buttons_are_available_on_mobile(self):
        controls = (ROOT / "unity-city/Assets/Claude/BraxTouchControls.cs").read_text(encoding="utf-8")
        self.assertIn('b == PCios ? "punch" : b == PKop ? "kick"', controls)
        self.assertIn('BraxIkony.Piesc', controls)
        self.assertIn('BraxIkony.But', controls)
        self.assertIn("if (OnFoot)", controls)
        self.assertIn('player && !player.vessel && !player.seat && !player.autoGracza', controls)

    def test_showroom_does_not_freeze_duplicate_patryk(self):
        salon = source("BraxClaudeOsiedleSalon.cs")
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        self.assertIn('an.GetClip("Typing")', salon)
        self.assertIn('an.Play("Typing")', salon)
        self.assertIn('Patryk (sprzedawca)', salon)
        self.assertIn('Nieautoryzowana dodatkowa kopia modelu Patryka', scene)
        self.assertIn('stanowisko sprzedawcy stoi bez oryginalnego FBX Typing', scene)

    def test_estate_content_update_editor_matches_source_version(self):
        project = (ROOT / "unity-city/ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
        workflow = (ROOT / ".github/workflows/zone-content-update.yml").read_text(encoding="utf-8")
        self.assertIn("m_EditorVersion: 6000.0.85f2", project)
        self.assertIn('if [ "$ZONE" = osiedle ]; then', workflow)
        self.assertIn("test \"$UNITY\" = 6000.0.85f2", workflow)
        self.assertIn("UNITY=6000.0.58f2", workflow)  # other regions unchanged
        self.assertIn("unityVersion: ${{ steps.profile.outputs.unity_version }}", workflow)
        self.assertIn('echo "unity_version=$UNITY" >> "$GITHUB_OUTPUT"', workflow)

    def test_ai_spawn_checks_scene_obstacles_before_placing_cars(self):
        sim = (ROOT / "unity-city/Assets/Claude/Runtime/BraxRuchAutSym.cs").read_text()
        adapter = (ROOT / "unity-city/Assets/Claude/Runtime/BraxRuchAut.cs").read_text()
        spawn = sim.split("public void UstawStart()", 1)[1].split("bool DalekoOdWezlow", 1)[0]
        self.assertIn("if (Nachodzi(a, p, kk, .5f, 4f, null)) continue;", spawn)
        self.assertIn("if (ZajeteZewnetrznie(p, kk", spawn)
        initial = adapter.split("void Start()", 1)[1].split("bool Widoczne", 1)[0]
        self.assertLess(initial.index("sym.przeszkodaSceny = PrzeszkodaSceny;"),
                        initial.index("OdswiezPrzeszkody();"))
        self.assertLess(initial.index("OdswiezPrzeszkody();"),
                        initial.index("sym.UstawStart();"))
        self.assertIn("sym.zewnetrzne.Clear();", adapter)
        self.assertIn("BraxAutoGracza.Wszystkie", adapter)
        self.assertIn("BraxAutobus.Wszystkie", adapter)

    def test_pedestrian_escape_rejects_walls_and_occupied_destinations(self):
        people = (ROOT / "unity-city/Assets/Claude/Runtime/BraxPieszy.cs").read_text()
        self.assertIn("Physics.SphereCastNonAlloc", people)
        self.assertIn("if (n == trafieniaPrzeszkod.Length) return true;", people)
        self.assertIn("if (gracz && hit.transform.IsChildOf(gracz)) continue;", people)
        self.assertIn("if (!Przeszkoda(transform.forward, droga + .2f) && !Zajete(p))", people)
        self.assertIn("else nastSprawdz = 0f;", people)

    def test_traffic_uses_synced_colliders_and_corner_speed_limit(self):
        traffic = (ROOT / "unity-city/Assets/Claude/Runtime/BraxRuchAutSym.cs").read_text()
        adapter = (ROOT / "unity-city/Assets/Claude/Runtime/BraxRuchAut.cs").read_text()
        self.assertIn("Krzywizna(a.trasa, a.s + v * dt)", traffic)
        self.assertIn("v = Mathf.Min(v, vZakret);", traffic)
        self.assertEqual(adapter.count("Physics.SyncTransforms();"), 2)
        self.assertIn("sym.UstawStart();", adapter)

    def test_crossing_never_ignores_a_nearby_moving_car(self):
        people = (ROOT / "unity-city/Assets/Claude/Runtime/BraxPieszy.cs").read_text()
        car = (ROOT / "unity-city/Assets/Claude/Runtime/BraxAuto.cs").read_text()
        self.assertIn("public float PredkoscAktualna { get; private set; }", car)
        self.assertIn("PredkoscAktualna = st.v;", car)
        self.assertIn("if (m < 3.5f && a.PredkoscAktualna < .2f) continue;", people)
        self.assertNotIn("if (m < 3.5f || m > 16f) continue;", people)

    def test_pedestrians_check_each_move_even_in_distant_lod(self):
        pedestrian = (ROOT / "unity-city/Assets/Claude/Runtime/BraxPieszy.cs").read_text()
        self.assertIn("if (Przeszkoda(dir, krok + .08f) || Zajete(p))", pedestrian)
        self.assertIn("if (Przeszkoda(ds, kr + .08f) || Zajete(next))", pedestrian)
        self.assertIn("Physics.RaycastNonAlloc(p + Vector3.up * 1.4f", pedestrian)
        self.assertIn("hit.transform.IsChildOf(transform)", pedestrian)
        self.assertIn("Physics.OverlapCapsuleNonAlloc(p + Vector3.up * .45f", pedestrian)
        self.assertNotIn("Physics.OverlapCapsule(p + Vector3.up * .45f", pedestrian)

    def test_parked_car_residents_cannot_enter_without_reaching_door(self):
        traveler = (ROOT / "unity-city/Assets/Claude/Runtime/BraxPodroznyAuta.cs").read_text()
        self.assertIn("if (bezPostepu >= 4f)", traveler)
        self.assertIn("if (!doszedl)", traveler)
        self.assertIn("yield break;", traveler)
        self.assertLess(traveler.index("if (!doszedl)"), traveler.index("kapsula.enabled=false;"))
        self.assertIn("yield return Idz(dom);", traveler)
        self.assertIn("powrót do domu zablokowany", traveler)

    def test_no_fake_static_player_substitution(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        motion = source("BraxClaudePatrykHumanoid.cs")
        self.assertNotIn("PatrykSpacerAwaryjny(", player + motion)
        self.assertNotIn("PatrykPozaStala(", player + motion)
        self.assertIn("PatrykWlasnyFBX(hero, kosci", player)
        self.assertIn("Quaternion.Inverse(sourceRest[name])", motion)


if __name__ == "__main__":
    unittest.main()
