using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Odbiór danych generatora wyspy. PASS tutaj nie oznacza testu przejścia gracza ani Androida.
// R1 (zasada właściciela, AGENTS.md): twardy błąd TYLKO gdy bez niego nie ma gry; wyspa jest rejonem doczytywanym,
// więc KAŻDY brak tutaj = ostrzeżenie BRAXHORN_WYSPA_DO_POPRAWKI (+ wpis w Logs/wyspa-p0-odbior.json), build idzie dalej.
public static class BraxWyspaOdbior
{
    const string Prefiks = "BRAXHORN_WYSPA_DO_POPRAWKI ";
    [Serializable] sealed class Raport
    {
        public string status = "NIEGOTOWE", unity, commit;
        public bool testGraczaUnity, testAndroid;
        public List<string> kroki = new List<string>(), bledy = new List<string>();
        public int renderery, collidery, swiatla, skinnedMeshes;
        public long trojkaty;
    }
    static Raport raport;
    public static void Rozpocznij()
    {
        raport = new Raport { unity = Application.unityVersion, commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "lokalny" };
        Zapisz();
    }
    static void Zapisz()
    {
        try
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/wyspa-p0-odbior.json", JsonUtility.ToJson(raport, true));
        }
        catch (Exception e) { Debug.LogWarning(Prefiks + "zapis raportu odbioru: " + e.Message); }
    }
    /// <summary>Ostrzeżenie odbioru: do konsoli (z prefiksem) i do raportu JSON. Nigdy nie przerywa buildu.</summary>
    public static void Ostrzez(string tekst)
    {
        if (raport == null) Rozpocznij();
        raport.status = "NIEGOTOWE"; raport.bledy.Add(tekst);
        Debug.LogWarning(Prefiks + tekst);
        Zapisz();
    }
    /// <summary>Krok generatora: wyjątek = ostrzeżenie z pełnym śladem stosu, budowa wyspy idzie dalej (bez rethrow).</summary>
    public static void Krok(string nazwa, Action action)
    {
        if (raport == null) Rozpocznij();
        try { action(); raport.kroki.Add(nazwa + ": OK (generator)"); }
        catch (Exception e)
        {
            raport.status = "NIEGOTOWE"; raport.bledy.Add(nazwa + ": " + e);
            Debug.LogWarning(Prefiks + nazwa + ": " + e);
        }
        finally { Zapisz(); }
    }
    /// <summary>Braki shadera/tekstur terenu plaży — lista (pusta = OK), bez wyjątków.</summary>
    public static List<string> BrakiTekstur(Material material)
    {
        var braki = new List<string>();
        if (!material || !material.shader || material.shader.name != "Braxhorn/NabrzezeTeren")
        { braki.Add("Plaża: brak właściwego shadera terenu (Braxhorn/NabrzezeTeren)"); return braki; }
        foreach (string slot in new[] { "_Piasek", "_PiasekN", "_Mokry", "_Trawa", "_TrawaN", "_Skala", "_SkalaN" })
        {
            var texture = material.HasProperty(slot) ? material.GetTexture(slot) : null;
            if (!texture || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
                braki.Add("Plaża: brak źródłowej tekstury " + slot);
        }
        return braki;
    }
    public static void TeksturyTerenu(Material material)
    {
        foreach (var b in BrakiTekstur(material)) Ostrzez(b);
    }
    public static void SprawdzBramki(Transform root, int wymagane)
    {
        try
        {
            if (wymagane == 0) { Ostrzez("Arena: brak istniejących modeli bramek biletowych (bramki z kodu)"); return; }
            var gates = root.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true).Where(d => d.etykieta != null && d.etykieta.StartsWith("bramka metra ")).ToArray();
            if (gates.Length != wymagane || gates.Any(d => d.skrzydla.Length != 4 || d.skrzydla.Any(s => !s)))
                Ostrzez("Arena: niekompletne bramki, oczekiwano " + wymagane + " modeli z czterema klapami, jest " + gates.Length);
        }
        catch (Exception e) { Ostrzez("Arena: kontrola bramek: " + e); }
    }
    public static void SprawdzScene(Scene scene)
    {
        try
        {
            var roots = BraxClaudeObszary.PoNazwach(scene, "CLAUDE wyspa*");
            if (roots.Count != 1) { Ostrzez("oczekiwano jednego korzenia wyspy w scenie, jest " + roots.Count); return; }
            Krok("końcowa kontrola przed wydzieleniem sceny", () => Sprawdz(roots[0], true));
        }
        catch (Exception e) { Ostrzez("kontrola sceny wyspy: " + e); }
    }
    public static void SprawdzPaczke(string[] scenePaths)
    {
        try
        {
            var path = scenePaths.FirstOrDefault(p => p != null && Path.GetFileNameWithoutExtension(p) == "BraxhornCity_Wyspa");
            if (path == null) { Ostrzez("paczka miasta bez osobnej sceny wyspy (BraxhornCity_Wyspa)"); return; }
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var roots = BraxClaudeObszary.PoNazwach(scene, "CLAUDE wyspa*");
                if (roots.Count != 1) Ostrzez("Wyspa: niepoprawny korzeń zapisanej sceny (korzeni: " + roots.Count + ")");
                else Krok("kontrola zapisanej sceny (także z pamięci)", () => Sprawdz(roots[0], true));
                if (EditorSceneManager.DetectCrossSceneReferences(scene))
                    Ostrzez("referencje poza sceną wyspy (DetectCrossSceneReferences)");
            }
            finally { if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
        }
        catch (Exception e) { Ostrzez("kontrola paczki wyspy: " + e); }
    }
    /// <summary>Kontrola geometrii/danych wyspy. Zwraca listę braków (pusta = OK); każdy brak = ostrzeżenie, nic nie rzuca.</summary>
    public static List<string> Sprawdz(Transform root, bool pelnaScena = false)
    {
        var errors = new List<string>();
        if (!root) { Ostrzez("Brak korzenia wyspy do kontroli"); errors.Add("Brak korzenia wyspy"); return errors; }
        try
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) errors.Add("brak skryptu: " + t.name);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var checkedMaterials = new HashSet<Material>();
            foreach (var r in renderers)
            {
                if (r.sharedMaterials.Length == 0) errors.Add("brak materiałów: " + r.name);
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || !m.shader || m.shader.name == "Hidden/InternalErrorShader") { errors.Add("brak materiału/shadera: " + r.name); continue; }
                    if (!checkedMaterials.Add(m)) continue;
                    // Niezadane opcjonalne sloty są legalne; zgubiona referencja PPtr nie jest.
                    var so = new SerializedObject(m); var saved = so.FindProperty("m_SavedProperties.m_TexEnvs");
                    if (saved == null) continue;
                    for (int i = 0; i < saved.arraySize; i++)
                    {
                        var entry = saved.GetArrayElementAtIndex(i);
                        var tex = entry.FindPropertyRelative("second.m_Texture");
                        if (tex != null && tex.objectReferenceValue == null && tex.objectReferenceInstanceIDValue != 0)
                            errors.Add("brak tekstury: " + m.name + "/" + entry.FindPropertyRelative("first").stringValue);
                    }
                }
            }
            foreach (var m in root.GetComponentsInChildren<MeshFilter>(true)) if (!m.sharedMesh) errors.Add("brak siatki: " + m.name);
            foreach (var c in root.GetComponentsInChildren<MeshCollider>(true)) if (!c.sharedMesh) errors.Add("brak kolizji siatki: " + c.name);
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (!skin.sharedMesh || skin.bones.Length == 0 || skin.bones.Any(b => !b)) errors.Add("niepełny szkielet: " + skin.name);
            var terrain = transforms.FirstOrDefault(t => t.name == "Wyspa: teren");
            if (!terrain || !terrain.GetComponent<MeshCollider>() || !terrain.GetComponent<BraxPiasekPowierzchnia>()) errors.Add("brak terenu/kolizji/maski piasku");
            else { var rt = terrain.GetComponent<Renderer>(); if (rt && rt.sharedMaterials.Length > 0) errors.AddRange(BrakiTekstur(rt.sharedMaterials[0])); }
            var show = root.GetComponentInChildren<BraxArenaPokaz>(true);
            // 12.11: walka z paczki globalnej postaci (BraxArenaWalka) zastępuje pokaz z obozu — komplet = obaj zawodnicy z animacją
            var walka = root.GetComponentInChildren<BraxArenaWalka>(true);
            if (walka && walka.zawodnikA && walka.zawodnikB && walka.animacjaA && walka.animacjaB)
            {
                if (raport == null) Rozpocznij();
                raport.kroki.Add("walka w klatce: postacie globalne " + walka.zawodnikA.name + " / " + walka.zawodnikB.name + " (klipy A " + walka.animacjaA.GetClipCount() + ", B " + walka.animacjaB.GetClipCount() + ")");
                Debug.Log("BRAXHORN_WYSPA_WALKA_GLOBALNA A=" + walka.zawodnikA.name + " B=" + walka.zawodnikB.name + " klipy=" + walka.animacjaA.GetClipCount() + "/" + walka.animacjaB.GetClipCount());
            }
            else if (!show || !show.zawodnikA || !show.zawodnikB || !show.animacjaA || !show.animacjaB)
            {
                // Pokaz walki kopiuje zawodników Z OBOZU (BraxObozCharacters) — w gałęzi miasta ich nie ma, pokaz jest
                // świadomie pominięty (BRAXHORN_ARENA_POKAZ w BraxMiastoWyspaWykonczenie.cs); to nie jest błąd geometrii wyspy.
                if (!BraxMiastoWyspa.ZawodnicyObozuDostepni) { if (raport == null) Rozpocznij(); raport.kroki.Add("walka pokazowa: POMINIĘTA (zawodnicy z obozu poza gałęzią miasta)"); }
                else errors.Add("brak kompletnej walki pokazowej PITBULL / DOBERMAN");
            }
            else
            {
                // Nie wolno podstawić KADE zamiast oryginalnego Pitbulla RAKK.
                if (show.zawodnikA.name != "Pokaz: PITBULL (RAKK)" ||
                    show.zawodnikB.name != "Pokaz: DOBERMAN (ZYRO)")
                    errors.Add("niewłaściwi zawodnicy: oczekiwano PITBULL/RAKK i DOBERMAN/ZYRO");
                var wymaganeRuchy = new[]
                {
                    "Garda", "Krok", "Kombinacja", "Unik", "Trafienie",
                    "Kopniecie", "Blok", "TrafienieKorpus", "Wstanie", "Zaczepka"
                };
                foreach (var anim in new[] { show.animacjaA, show.animacjaB })
                {
                    foreach (string clip in wymaganeRuchy)
                        if (!anim.GetClip(clip)) errors.Add("brak klipu areny " + anim.name + ": " + clip);
                    if (!anim.GetComponentInChildren<SkinnedMeshRenderer>(true))
                        errors.Add("brak realnego modelu 3D animowanego zawodnika " + anim.name);
                }
                if (!show.zawodnikA.IsChildOf(root) || !show.zawodnikB.IsChildOf(root))
                    errors.Add("zawodnicy poza sceną wyspy");
                foreach (var fighter in new[] { show.zawodnikA, show.zawodnikB })
                {
                    var c = fighter.GetComponent<CapsuleCollider>();
                    var rb = fighter.GetComponent<Rigidbody>();
                    if (!c || c.height < 1.4f || !rb || !rb.isKinematic)
                        errors.Add("zawodnik bez bezpiecznej kapsuły/kinematycznego ciała: " + fighter.name);
                }
                // Stan testu runtime jest jawny: same klipy nie potwierdzają kontaktu.
                Debug.Log("BRAXHORN_WYSPA_PITBULL_DOBERMAN modelA=" + show.zawodnikA.name +
                    " modelB=" + show.zawodnikB.name + " klipy=" + wymaganeRuchy.Length +
                    " kontakt_dłonie_stopy_głowa_korpus=do_testu_Unity test_APK=nie");
            }
            var ambient = root.GetComponentInChildren<BraxWyspaAmbient>(true);
            if (!ambient || !ambient.morze || !ambient.wiatr || !ambient.kibice || !ambient.morze.clip || !ambient.wiatr.clip || !ambient.kibice.clip)
                errors.Add("niekompletny ambient");
            if (!transforms.Any(t => t.name == "Wyspa: delikatna piana brzegu")) errors.Add("brak piany brzegu");
            var kontrola = root.GetComponent<BraxWyspaKontrola>();
            if (!kontrola || kontrola.wersja != 1) errors.Add("brak aktualnych danych odbioru w scenie; wymagane ponowne generowanie");
            var places = new Dictionary<string, Vector3>();
            if (kontrola && kontrola.punkty != null) foreach (var p in kontrola.punkty) if (!string.IsNullOrEmpty(p.nazwa)) places[p.nazwa] = p.pozycja;
            foreach (var key in new[] { "arena_srodek", "arena_wejscie", "arena_prog", "arena_hol", "arena_klatka", "arena_tunel", "arena_szatnia_braxa", "arena_wc_damska", "arena_wc_meska", "wyspa_przystan" })
                if (!places.ContainsKey(key)) errors.Add("brak punktu komunikacji: " + key);
            foreach (var room in new[] { "arena_wc_damska", "arena_wc_meska", "arena_szatnia_braxa", "arena_szatnia_gosci" })
                foreach (var suffix in new[] { "_prog", "_wnetrze" })
                    if (!places.ContainsKey(room + suffix)) errors.Add("brak wejścia pomieszczenia: " + room + suffix);
            foreach (var key in new[] { "arena_schody_klatka", "arena_klatka_wnetrze" })
                if (!places.ContainsKey(key)) errors.Add("brak dojścia klatki: " + key);
            if (pelnaScena)
            {
                var markers = transforms.FirstOrDefault(t => t.name.StartsWith("Wyspa: miejsca ludzi", StringComparison.Ordinal));
                var people = transforms.FirstOrDefault(t => t.name == "Wyspa: ludzie (kibice, plaża, obsada)");
                int expected = markers ? markers.childCount : 0;
                int actual = people ? people.GetComponentsInChildren<BraxMieszkaniec>(true).Length : 0;
                if (expected == 0 || actual < expected) errors.Add("niepełna obsada wyspy: " + actual + "/" + expected + " NPC");
                // Kontrola zadania: kibice siedzą na trybunach i są animowani (klip siedzenia).
                // Same nazwy nie są dowodem koloru/ubrania — wymagana kontrola w APK.
                // Widz = siedzący mieszkaniec postawiony ze znacznika „Trybuny ARENY” / loży / stołów
                // (BraxRegionRuchMiasta.LudzieZMiejsc nazywa postać „gdzie — rola”).
                var ludzie = people ? people.GetComponentsInChildren<BraxMieszkaniec>(true) : new BraxMieszkaniec[0];
                var widzowie = ludzie.Where(m => m && m.siedzi &&
                    (m.name.StartsWith("Trybuny ARENY", StringComparison.Ordinal) || m.name.StartsWith("Loża VIP", StringComparison.Ordinal) ||
                     m.name.StartsWith("Stół komentatorów", StringComparison.Ordinal) || m.name.StartsWith("Stół sędziów", StringComparison.Ordinal))).ToArray();
                int zlyWidz = widzowie.Count(m =>
                    m.klipy == null || m.klipy.Length == 0 ||
                    m.klipy.Any(k => k == null || !k.Contains("sit_chair")) ||
                    !m.anim || !m.GetComponentInChildren<SkinnedMeshRenderer>(true));
                int plazowi = ludzie.Count(m => m && (m.name.StartsWith("Plaża wyspy", StringComparison.Ordinal) || m.name.StartsWith("Woda przy plaży", StringComparison.Ordinal) ||
                    m.name.StartsWith("Brzeg plaży", StringComparison.Ordinal) || m.name.StartsWith("Bar plażowy", StringComparison.Ordinal)));
                if (zlyWidz > 0)
                    errors.Add("WYSPA_R1_STROJE_OBSADA: widzowie bez klipu siedzenia / animacji / modelu: " + zlyWidz);
                if (widzowie.Length == 0 || plazowi == 0)
                    errors.Add("WYSPA_R1_STROJE_OBSADA: brak widowni na trybunach lub plażowiczów po obsadzeniu wyspy (widownia " + widzowie.Length + ", plaża " + plazowi + ")");
                Debug.Log("WYSPA_R1_STROJE_OBSADA widzowie=" + widzowie.Length +
                    " widzowie_bledni=" + zlyWidz + " plazowi=" + plazowi +
                    " stroje=do_oceny_w_APK test_Android=OCZEKUJE");
            }
            // Właściciel 9.11 11:39: „obok sceny ringi stały — do usunięcia” → ŻADNEGO ringu plenerowego obok areny
            // (ani brył/kolizji, ani akcji wyspa_ring_*). Walki wyłącznie w BRAXHORN ARENA.
            var rings = root.GetComponentsInChildren<Collider>(true)
                .Where(c => c && (c.name == "kolizja: ring plenerowy" || c.name == "kolizja: liny ringu")).ToArray();
            if (rings.Length > 0)
                errors.Add("nieusunięty ring plenerowy obok areny (kolizje: " + rings.Length + ") — właściciel: ringi do usunięcia");
            if (transforms.Any(t => t && t.name.Contains("wyspa_ring_", StringComparison.Ordinal)))
                errors.Add("nieusunięty punkt interakcji ringu plenerowego wyspa_ring_*");
            var seats = root.GetComponentsInChildren<BraxSeat>(true);
            if (seats.Length < 12) errors.Add("brak dostępnych siedzisk widowni");
            var doors = root.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true);
            var entrance = doors.Where(d => d.etykieta == "BRAXHORN ARENA — wejście").ToArray();
            if (entrance.Length != 1) errors.Add("wejście: oczekiwano jednej centralnej pary drzwi");
            foreach (var d in doors)
                if (d.skrzydla.Length == 0 || d.skrzydla.Any(s => !s) || d.przesuniecie.Length != d.skrzydla.Length || d.blokady.Any(c => !c))
                    errors.Add("niekompletne drzwi: " + d.etykieta);
            if (entrance.Length == 1 && places.ContainsKey("wyspa_przystan") && Mathf.Abs(entrance[0].transform.position.z - places["wyspa_przystan"].z) > .05f)
                errors.Add("wejście poza osią przystani");
            // Próba kapsuły w geometrii w stanie drzwi otwartych. To nie jest test ruchu gracza.
            if (errors.Count == 0)
            {
                try { Droznosc(root, doors, places, errors); }
                catch (Exception e) { errors.Add("test drożności nieudany: " + e.Message); }
            }
            if (raport == null) Rozpocznij();
            raport.renderery = renderers.Length; raport.collidery = root.GetComponentsInChildren<Collider>(true).Length;
            raport.swiatla = root.GetComponentsInChildren<Light>(true).Length;
            raport.skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            raport.trojkaty = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh).Sum(f =>
                Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(i => (long)f.sharedMesh.GetIndexCount(i) / 3));
        }
        catch (Exception e) { errors.Add("kontrola wyspy przerwana wyjątkiem: " + e); }
        if (raport == null) Rozpocznij();
        var lista = errors.Distinct().ToList();
        if (lista.Count > 0)
        {
            raport.bledy.AddRange(lista); raport.status = "NIEGOTOWE"; Zapisz();
            Debug.LogWarning(Prefiks + "kontrola wyspy (" + lista.Count + " braków, build idzie dalej):\n" + string.Join("\n", lista));
        }
        else { raport.status = "GEOMETRIA_SPRAWDZONA_TEST_GRACZA_I_ANDROID_OCZEKUJE"; Zapisz(); }
        return lista;
    }

    static void Droznosc(Transform root, BraxDrzwiPrzesuwne[] doors, Dictionary<string, Vector3> p, List<string> errors)
    {
        var positions = new Dictionary<Transform, Vector3>(); var colliders = new Dictionary<Collider, bool>();
        try
        {
            foreach (var d in doors)
            {
                for (int i = 0; i < d.skrzydla.Length; i++)
                { var s = d.skrzydla[i]; positions[s] = s.localPosition; s.localPosition += d.przesuniecie[i]; }
                // Odtworzenie Ustaw() dla stanu=1, z przywróceniem w finally.
                foreach (var c in d.blokady) { colliders[c] = c.enabled; c.enabled = false; }
            }
            Physics.SyncTransforms();
            var center = p["arena_srodek"]; float floor = p["arena_hol"].y;
            var approach = p["arena_wejscie"]; approach.x -= 6f;
            Linia(approach, p["arena_hol"], "plac–rampa–drzwi–hol");
            Linia(p["arena_hol"], new Vector3(center.x - 8.2f, floor, center.z), "hol–bramki–widownia");
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                float b = (i + 1) * Mathf.PI * 2f / 32f;
                Linia(new Vector3(center.x + Mathf.Cos(a) * 8.2f, floor, center.z + Mathf.Sin(a) * 8.2f),
                    new Vector3(center.x + Mathf.Cos(b) * 8.2f, floor, center.z + Mathf.Sin(b) * 8.2f), "obejście klatki");
            }
            Linia(new Vector3(center.x + 8.2f, floor, center.z), p["arena_tunel"], "widownia–tunel");
            Linia(p["arena_schody_klatka"], p["arena_klatka"], "schody klatki");
            Linia(p["arena_klatka"], p["arena_klatka_wnetrze"], "drzwi klatki");
            foreach (var room in new[] { "arena_wc_damska", "arena_wc_meska", "arena_szatnia_braxa", "arena_szatnia_gosci" })
            {
                var start = room.StartsWith("arena_wc") ? p["arena_hol"] : p["arena_tunel"];
                Linia(start, p[room + "_prog"], "dojście " + room);
                Linia(p[room + "_prog"], p[room + "_wnetrze"], "wejście " + room);
            }
            void Linia(Vector3 a, Vector3 b, string name)
            {
                int n = Mathf.CeilToInt(Vector3.Distance(a, b) / .25f);
                for (int i = 0; i <= n; i++) Punkt(Vector3.Lerp(a, b, i / (float)Mathf.Max(1, n)), name);
            }
            void Punkt(Vector3 pos, string name)
            {
                // Początek nisko nad spodziewaną posadzką, żeby nie łapać dachu i daszka.
                if (!Physics.Raycast(pos + Vector3.up * .5f, Vector3.down, out var ground, 1f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore))
                { errors.Add("brak podłoża: " + name + " " + pos.ToString("F2")); return; }
                if (Mathf.Abs(ground.point.y - pos.y) > .36f) errors.Add("stopień > stepOffset: " + name);
                // Dół powyżej stepOffset=.36: stopień możliwy do pokonania nie jest ścianą.
                Vector3 low = ground.point + Vector3.up * .73f, high = ground.point + Vector3.up * 1.90f;
                foreach (var hit in Physics.OverlapCapsule(low, high, .35f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore))
                    if (hit.transform.IsChildOf(root) && hit != ground.collider)
                        errors.Add("blokada trasy " + name + ": " + hit.name + " " + pos.ToString("F2"));
            }
        }
        finally
        {
            foreach (var saved in positions) if (saved.Key) saved.Key.localPosition = saved.Value;
            foreach (var c in colliders) if (c.Key) c.Key.enabled = c.Value;
            Physics.SyncTransforms();
        }
    }
}
