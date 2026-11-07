using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// MIASTO R1, właściciel 6.11: tylko stary napis nad NOVA i wejście do klubu.
// Wywołanie PO kontrolach dodających/przesuwających collidery, PRZED odebraniem
// odczytu siatkom. Nie wyłącza kolizji klubu: wycina wyłącznie światło przejścia.
internal static class BraxNovaEntranceFinish
{
    const string NovaRoot = "CLAUDE A: CLUB NOVA (od nowa)";
    const string DoorLabel = "CLUB NOVA — wejście";
    const string Out = "Assets/PlayableGenerated/ClaudeRegion/NovaEntrance";
    const float Floor = .15f;
    static readonly Bounds Passage = Box(40.82f, 43.18f, Floor + .003f, 2.95f, 34.0f, 43.6f);
    static readonly Bounds OldLetters = Box(38.2f, 45.8f, 3.50f, 4.98f, 35.60f, 35.98f);

    [Serializable] sealed class Receipt
    {
        public string status = "NOT_VALIDATED";
        public bool phoneValidated = false, doorConfigured;
        public int removedLetterTriangles, remainingLetterTriangles, cutColliders, pathSamples, groundGaps;
        public float playerRadius, playerHeight;
        public List<string> changed = new List<string>(), blockers = new List<string>(), warnings = new List<string>();
    }

    static Bounds Box(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b;
    }
    static void Warn(Receipt r, string text)
    { r.warnings.Add(text); Debug.LogWarning("BRAXHORN_NOVA_ENTRANCE " + text); }
    static string PathOf(Transform t)
    { return t.parent ? PathOf(t.parent) + "/" + t.name : t.name; }

    public static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        var roots = scene.GetRootGameObjects();
        var clubs = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == NovaRoot).ToArray();
        if (clubs.Length == 0) return; // Osiedle, obóz, siłownia i inne sceny nietknięte.
        var rep = new Receipt();
        Directory.CreateDirectory(Out);
        if (clubs.Length != 1) { Warn(rep, "Niejednoznaczny korzeń NOVA; bez zmian."); Save(rep); return; }
        var club = clubs[0];
        try { RemoveOldLetters(club, rep); }
        catch (Exception e) { Warn(rep, "Napis: " + e); }
        try { RemoveUnlabelledSourceSign(scene, club, rep); }
        catch (Exception e) { Warn(rep, "Stare napisy źródłowe: " + e); }
        var doors = club.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true)
            .Where(d => d.etykieta == DoorLabel).ToArray();
        if (doors.Length != 1) { Warn(rep, "Brak pojedynczych drzwi automatycznych NOVA; bez zgadywania wejścia."); Save(rep); return; }
        var door = doors[0];
        try { ConfigureDoor(door, rep); }
        catch (Exception e) { Warn(rep, "Drzwi: " + e); }
        Physics.SyncTransforms();
        foreach (var c in roots.SelectMany(g => g.GetComponentsInChildren<Collider>(true)).ToArray())
        {
            if (!c || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger || c.gameObject.layer == 29) continue;
            if (!Overlaps(c.bounds, Passage) || c.GetComponentInParent<BraxDrzwiPrzesuwne>() == door) continue;
            // Nie zmieniamy gracza, NPC ani ruchomych obiektów.
            if (c is CharacterController || c.attachedRigidbody || c.GetComponentInParent<BraxGymPlayer>() ||
                c.GetComponentInParent<Animator>() || c.GetComponentInParent<Animation>()) continue;
            try
            {
                string name = PathOf(c.transform) + " [" + c.GetType().Name + "] " + c.bounds;
                bool changed = false;
                if (c is BoxCollider box && AxisAligned(box.transform)) changed = CutBox(box);
                else if (c is BoxCollider || c is MeshCollider) changed = CutMesh(c);
                else { Warn(rep, "Nieobsługiwany collider w przejściu (pozostawiony): " + name); continue; }
                if (changed) { rep.cutColliders++; rep.changed.Add(name); }
            }
            catch (Exception e) { Warn(rep, "Kolizja " + PathOf(c.transform) + ": " + e); }
        }
        Physics.SyncTransforms();
        try { Probe(scene, door, rep); }
        catch (Exception e) { Warn(rep, "Test drożności: " + e); }
        rep.status = rep.doorConfigured && rep.pathSamples > 0 && rep.remainingLetterTriangles == 0 &&
            rep.blockers.Count == 0 && rep.groundGaps == 0 && rep.warnings.Count == 0 ? "PASS_SCENE_GEOMETRY" : "REQUIRES_REVIEW";
        Save(rep);
    }

    static bool LetterMaterial(Material m)
    {
        // Nowy kolorowy szyld, panel i obwódka nie są tymi materiałami.
        return m && (m.name.Contains("NOVA: litery szyldu (podświetlane)") || m.name.Contains("NOVA: boki liter (grafit)"));
    }
    // Zatwierdzony szyld: kolorowy NOVA CLUB właściciela; nie ruszamy jego siatek, materiałów ani UV.
    static bool ApprovedNovaSign(Transform t)
    {
        for (var p = t; p; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.StartsWith("nova szyld oryginalny właściciela") ||
                n == "mural club_nova" || n == "mural nova_club" || n == "mural nova" ||
                n == "obraz: mural club_nova" || n == "obraz: mural nova_club" ||
                n == "obraz: mural nova") return true;
        }
        return false;
    }

    static bool LegacyNovaText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.ToUpperInvariant().Trim();
        return text.Contains("CLUB NOVA") || text.Contains("NOVA DYSKOTEKA") ||
               text.Contains("NOVA DVSKOTEKA");
    }

    static void RemoveOldLetters(Transform club, Receipt rep)
    {
        // POPRZEDNI BŁĄD: skanowano wyłącznie nowo utworzony klub. Stary napis
        // może należeć do oryginalnej sceny lub starej renowacji obok nowego klubu.
        var all = club.gameObject.scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        int textObjects = 0, oldNames = 0, skippedOwner = 0;
        foreach (var r in all)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!r.bounds.Intersects(OldLetters)) continue;
            if (ApprovedNovaSign(r.transform)) { skippedOwner++; continue; }
            var txt = r.GetComponent<TextMesh>();
            if (txt && LegacyNovaText(txt.text))
            {
                // Usuwamy tylko samodzielny, stary TextMesh, NIE rodzica/fasadę.
                Object.DestroyImmediate(txt.gameObject);
                textObjects++;
                continue;
            }
            var f = r.GetComponent<MeshFilter>(); var source = f ? f.sharedMesh : null;
            if (!source) continue;
            var mats = r.sharedMaterials;
            bool named = LegacyNovaText(r.name) ||
                (r.transform.parent && r.transform.parent != club &&
                 LegacyNovaText(r.transform.parent.name));
            bool legacyMaterial = mats.Any(LetterMaterial);
            if (!named && !legacyMaterial) continue;

            var vertices = source.vertices; var parts = new List<int[]>(); int removed = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var src = source.GetTriangles(sub); var keep = new List<int>(src.Length);
                bool oldSubmesh = named || sub < mats.Length && LetterMaterial(mats[sub]);
                for (int i = 0; i < src.Length; i += 3)
                {
                    var center = f.transform.TransformPoint(
                        (vertices[src[i]] + vertices[src[i + 1]] + vertices[src[i + 2]]) / 3f);
                    if (oldSubmesh && OldLetters.Contains(center)) { removed++; continue; }
                    keep.Add(src[i]); keep.Add(src[i + 1]); keep.Add(src[i + 2]);
                }
                parts.Add(keep.ToArray());
            }
            if (removed == 0) continue;
            var copy = Object.Instantiate(source);
            copy.name = source.name + "_without_legacy_nova_sign";
            for (int sub = 0; sub < parts.Count; sub++) copy.SetTriangles(parts[sub], sub);
            copy.RecalculateBounds();
            BraxZapisAssetu.Utworz(copy, AssetDatabase.GenerateUniqueAssetPath(Out + "/legacy_letters.asset"));
            f.sharedMesh = copy;
            bool empty = parts.All(x => x.Length == 0);
            foreach (var mc in f.GetComponents<MeshCollider>()) if (mc.sharedMesh == source)
            { if (empty) mc.enabled = false; else mc.sharedMesh = copy; }
            if (empty) r.enabled = false;
            rep.removedLetterTriangles += removed;
            if (named) oldNames++;
        }

        // Osobne źródła nie powinny zostać rozpoznane jako zatwierdzony szyld.
        // Odbiór sprawdza CAŁĄ scenę, nie tylko grupę nowego klubu.
        foreach (var r in club.gameObject.scene.GetRootGameObjects()
                     .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy ||
                !r.bounds.Intersects(OldLetters) || ApprovedNovaSign(r.transform)) continue;
            var txt = r.GetComponent<TextMesh>();
            if (txt && LegacyNovaText(txt.text)) { rep.remainingLetterTriangles++; continue; }
            var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh) continue;
            var m = f.sharedMesh; var vertices = m.vertices; var mats = r.sharedMaterials;
            bool named = LegacyNovaText(r.name) ||
                (r.transform.parent && r.transform.parent != club &&
                 LegacyNovaText(r.transform.parent.name));
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                if (!named && (sub >= mats.Length || !LetterMaterial(mats[sub]))) continue;
                var triangles = m.GetTriangles(sub);
                for (int i = 0; i < triangles.Length; i += 3)
                    if (OldLetters.Contains(f.transform.TransformPoint(
                        (vertices[triangles[i]] + vertices[triangles[i + 1]] +
                         vertices[triangles[i + 2]]) / 3f))) rep.remainingLetterTriangles++;
            }
        }
        Debug.Log("BRAXHORN_NOVA_STARY_NAPIS usuniete_obiekty_text=" + textObjects +
            " usuniete_trojkaty=" + rep.removedLetterTriangles +
            " stare_nazwane=" + oldNames + " chronione_oryginalne=" + skippedOwner +
            " pozostale=" + rep.remainingLetterTriangles);
        if (rep.remainingLetterTriangles > 0)
            Warn(rep, "Stary napis wciąż ma " + rep.remainingLetterTriangles + " trójkątów/tekstów");
    }

    // Stary napis w APK #108 pozostawał widoczny, mimo "stare_nazwane=0":
    // część oryginalnego miasta jest złączona w CityMesh_### i NIE ma nazwy napisu.
    // Wycinamy TYLKO wąski fragment ZAKRYTY zatwierdzonym panelem NOVA CLUB.
    // Nie dotykamy nowego klubu, źródła jego grafiki, drzwi ani dolnych ścian.
    static void RemoveUnlabelledSourceSign(Scene scene, Transform club, Receipt rep)
    {
        // Wnętrze zatwierdzonego panelu: x 38.4..45.6, y 3.62..4.86;
        // cięcie z marginesem *wewnątrz* panelu, poza przejściem na y<3.
        var stripe = Box(38.74f, 45.26f, 3.66f, 4.72f, 35.48f, 36.40f);
        int altered = 0;
        foreach (var r in scene.GetRootGameObjects()
                     .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray())
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy ||
                !r.bounds.Intersects(stripe) || r.transform.IsChildOf(club) ||
                ApprovedNovaSign(r.transform)) continue;
            if (r.GetComponentInParent<BraxGymPlayer>() ||
                r.GetComponentInParent<Animator>() || r.GetComponentInParent<Animation>()) continue;
            var f = r.GetComponent<MeshFilter>();
            if (!f || !f.sharedMesh || !f.sharedMesh.isReadable) continue;
            // Tnie tylko geometrię starszej sceny w obrysie nowego szyldu.
            // Szerokość bramy klubu oraz wszystkie collidery na poziomie gracza zostają.
            int n = BraxCityMeshCut.Subtract(f.transform, stripe, Out, "nova_legacy_white_text");
            if (n <= 0) continue;
            altered += n;
            rep.changed.Add("Stary nienazwany fragment szyldu: " + PathOf(f.transform));
        }
        Debug.Log("BRAXHORN_NOVA_NIENAZWANY_SZYLD usuniete_fragmenty_starych_siatek=" +
            altered + " przy_zachowaniu_oryginalnego_NOVA_CLUB=true");
    }

    static void ConfigureDoor(BraxDrzwiPrzesuwne d, Receipt rep)
    {
        if (d.skrzydla == null || d.skrzydla.Length != 2 || d.skrzydla.Any(t => !t) ||
            d.przesuniecie == null || d.przesuniecie.Length != 2)
        { Warn(rep, "Niekompletne skrzydła drzwi NOVA."); return; }
        // Oryginalny portal generatora, nie nowe drzwi ani zmiana ich wyglądu.
        d.transform.SetPositionAndRotation(new Vector3(42f, Floor, 36.28f), Quaternion.identity);
        d.enabled = true; d.gameObject.SetActive(true); d.promien = Mathf.Max(d.promien, 3.2f);
        // Dodatkowa kolizja przypięta przez kontrolę do skrzydła też musi się wyłączać po otwarciu.
        d.blokady = d.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).Distinct().ToArray();
        rep.doorConfigured = d.blokady.Length >= 2;
        if (!rep.doorConfigured) Warn(rep, "Brak kolizji skrzydeł NOVA.");
        EditorUtility.SetDirty(d);
    }

    static bool Overlaps(Bounds a, Bounds b)
    {
        // MeshCollider może być płaską siatką (zerowa grubość bounds), która nadal blokuje.
        return a.min.x < b.max.x - .00001f && a.max.x > b.min.x + .00001f &&
            a.min.y < b.max.y - .00001f && a.max.y > b.min.y + .00001f &&
            a.min.z < b.max.z - .00001f && a.max.z > b.min.z + .00001f;
    }
    static bool AxisAligned(Transform t)
    {
        foreach (var axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
        {
            var v = t.TransformVector(axis).normalized;
            if (Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z))) < .999999f) return false;
        }
        return true;
    }
    internal static List<Bounds> Remainders(Bounds original, Bounds cut)
    {
        if (!Overlaps(original, cut)) return new List<Bounds> { original };
        var lo = Vector3.Max(original.min, cut.min); var hi = Vector3.Min(original.max, cut.max);
        var b = original; var pieces = new List<Bounds>();
        void Add(Vector3 a, Vector3 z)
        {
            var s = z - a; if (s.x <= .00001f || s.y <= .00001f || s.z <= .00001f) return;
            var p = new Bounds(); p.SetMinMax(a, z); pieces.Add(p);
        }
        Add(b.min, new Vector3(lo.x, b.max.y, b.max.z));
        Add(new Vector3(hi.x, b.min.y, b.min.z), b.max);
        Add(new Vector3(lo.x, b.min.y, b.min.z), new Vector3(hi.x, lo.y, b.max.z));
        Add(new Vector3(lo.x, hi.y, b.min.z), new Vector3(hi.x, b.max.y, b.max.z));
        Add(new Vector3(lo.x, lo.y, b.min.z), new Vector3(hi.x, hi.y, lo.z));
        Add(new Vector3(lo.x, lo.y, hi.z), new Vector3(hi.x, hi.y, b.max.z));
        return pieces;
    }
    static bool CutBox(BoxCollider c)
    {
        var pieces = Remainders(c.bounds, Passage); var created = new List<BoxCollider>();
        try
        {
            foreach (var b in pieces)
            {
                var a = c.transform.InverseTransformPoint(b.min); var z = c.transform.InverseTransformPoint(b.max);
                var p = c.gameObject.AddComponent<BoxCollider>(); created.Add(p);
                EditorUtility.CopySerialized(c, p);
                p.center = (a + z) * .5f; p.size = Vector3.Max(a, z) - Vector3.Min(a, z);
            }
            c.enabled = false; return true;
        }
        catch { foreach (var p in created) if (p) Object.DestroyImmediate(p); throw; }
    }
    static Mesh BoxMesh(BoxCollider b)
    {
        var s = new UlSiatka(); s.Box(b.center - b.size * .5f, b.center + b.size * .5f);
        return s.Zbuduj("NOVA_rotated_box_collision");
    }
    static bool CutMesh(Collider c)
    {
        var mc = c as MeshCollider;
        Mesh source = mc ? mc.sharedMesh : BoxMesh((BoxCollider)c);
        if (!source) return false;
        GameObject temp = null;
        try
        {
            temp = new GameObject("NOVA temporary collision cut"); temp.transform.SetParent(c.transform, false);
            var f = temp.AddComponent<MeshFilter>(); f.sharedMesh = source;
            var r = temp.AddComponent<MeshRenderer>();
            int changed = BraxCityMeshCut.Subtract(temp.transform, Passage, Out, "nova_passage");
            if (changed == 0) return false;
            if (!r.enabled) { c.enabled = false; return true; }
            if (mc)
            {
                // Otwór w siatce nie może być ponownie zamknięty otoczką convex.
                mc.convex = false; mc.sharedMesh = f.sharedMesh;
            }
            else
            {
                var replacement = c.gameObject.AddComponent<MeshCollider>();
                try
                {
                    replacement.sharedMesh = f.sharedMesh; replacement.convex = false;
                    replacement.sharedMaterial = c.sharedMaterial; replacement.contactOffset = c.contactOffset;
                    c.enabled = false;
                }
                catch { Object.DestroyImmediate(replacement); throw; }
            }
            return true;
        }
        finally
        {
            if (temp) Object.DestroyImmediate(temp);
            if (!mc && source && !EditorUtility.IsPersistent(source)) Object.DestroyImmediate(source);
        }
    }

    static void Probe(Scene scene, BraxDrzwiPrzesuwne door, Receipt rep)
    {
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxGymPlayer>(true)).FirstOrDefault();
        if (!player || !player.body) { Warn(rep, "Brak rzeczywistej kapsuły gracza; test nie wykonany."); return; }
        var body = player.body; var scale = body.transform.lossyScale;
        float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(2f * radius, body.height * Mathf.Abs(scale.y));
        rep.playerRadius = radius; rep.playerHeight = height;
        float lane = Passage.extents.x - radius - .12f;
        if (lane <= 0f || height > Passage.size.y - .04f) { Warn(rep, "Kapsuła gracza nie mieści się w portalu NOVA."); return; }
        int mask = 0;
        for (int i = 0; i < 32; i++) if (i != 29 && !Physics.GetIgnoreLayerCollision(body.gameObject.layer, i)) mask |= 1 << i;
        var leaves = door.skrzydla.Select(t => t.localPosition).ToArray();
        var cols = door.blokady.Concat(player.GetComponentsInChildren<Collider>(true)).Distinct().ToArray();
        var enabled = cols.Select(c => c && c.enabled).ToArray();
        var blockers = new HashSet<string>();
        void Add(Collider c) { if (c && c.gameObject.scene == scene) blockers.Add(PathOf(c.transform) + " [" + c.GetType().Name + "]"); }
        try
        {
            for (int i = 0; i < door.skrzydla.Length; i++) door.skrzydla[i].localPosition = leaves[i] + door.przesuniecie[i];
            foreach (var c in cols) if (c) c.enabled = false;
            Physics.SyncTransforms();
            // Trzy tory, w obie strony: rzeczywisty promień/wysokość postaci, nie cienki raycast.
            foreach (float dx in new[] { -lane * .65f, 0f, lane * .65f })
            {
                var a = new Vector3(42f + dx, Floor + .025f + radius, 34.1f);
                var b = a + Vector3.up * (height - 2f * radius);
                foreach (var c in Physics.OverlapCapsule(a, b, radius, mask, QueryTriggerInteraction.Ignore)) Add(c);
                foreach (var h in Physics.CapsuleCastAll(a, b, radius, Vector3.forward, 9.4f, mask, QueryTriggerInteraction.Ignore)) Add(h.collider);
                a.z += 9.4f; b.z += 9.4f;
                foreach (var c in Physics.OverlapCapsule(a, b, radius, mask, QueryTriggerInteraction.Ignore)) Add(c);
                foreach (var h in Physics.CapsuleCastAll(a, b, radius, Vector3.back, 9.4f, mask, QueryTriggerInteraction.Ignore)) Add(h.collider);
                for (int i = 0; i <= 38; i++)
                {
                    float z = Mathf.Lerp(34.1f, 43.5f, i / 38f);
                    bool floor = Physics.RaycastAll(new Vector3(42f + dx, Floor + .45f, z), Vector3.down, .8f, mask, QueryTriggerInteraction.Ignore)
                        .Any(h => h.collider.gameObject.scene == scene && h.normal.y > .7f && Mathf.Abs(h.point.y - Floor) < .04f);
                    if (!floor) rep.groundGaps++;
                    rep.pathSamples++;
                }
            }
        }
        finally
        {
            for (int i = 0; i < door.skrzydla.Length; i++) if (door.skrzydla[i]) door.skrzydla[i].localPosition = leaves[i];
            for (int i = 0; i < cols.Length; i++) if (cols[i]) cols[i].enabled = enabled[i];
            Physics.SyncTransforms();
        }
        rep.blockers.AddRange(blockers.OrderBy(s => s));
        if (rep.blockers.Count > 0) Warn(rep, "Pozostałe blokady: " + string.Join(" | ", rep.blockers));
        if (rep.groundGaps > 0) Warn(rep, "Brak ciągłej podłogi w próbkach: " + rep.groundGaps);
    }
    static void Save(Receipt rep)
    {
        string json = JsonUtility.ToJson(rep, true);
        File.WriteAllText(Out + "/receipt.json", json);
        Debug.Log("BRAXHORN_NOVA_ENTRANCE " + json);
        AssetDatabase.SaveAssets();
    }
}
