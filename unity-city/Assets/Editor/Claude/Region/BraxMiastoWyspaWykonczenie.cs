using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal static partial class BraxMiastoWyspa
{
    public static void WykonczOprawy(UnityEngine.SceneManagement.Scene scene)
    {
        // Globalny krok BezSwiecenia gasi także szyldy i oprawy areny.
        // Przywracamy umiarkowaną emisję TYLKO oznaczonych materiałów areny na wyspie.
        // Nie dodaje to świateł ani bloom; nie dotyka oświetlenia miasta/osiedla/obozu.
        // The premium arena uses BOTH "Arena:" and "Arena premium:" source
        // names. Check the four required lighting roles explicitly: a count of
        // arbitrary emissive materials alone cannot prove the signs survived.
        var required = new[] {
            "A świeci: Arena: pas LED zielony (marka)",
            "A świeci: Arena: litery szyldu (podświetlane)",
            "A świeci: Arena premium: znak wyjścia (zielony)",
            "A świeci: Arena premium: panel LED sufitowy"
        };
        var seen = new bool[required.Length];
        var done = new HashSet<Material>();
        var islands = BraxClaudeObszary.PoNazwach(scene, "CLAUDE wyspa*");
        if (islands.Count != 1)
            throw new InvalidOperationException("Arena: brak jednego źródłowego korzenia wyspy");
        foreach (var island in islands)
        foreach (var renderer in island.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
        {
            if (!material || !material.name.StartsWith("A świeci: Arena", StringComparison.Ordinal) || !done.Add(material))
                continue;
            if (!material.HasProperty("_EmissionColor") || !material.HasProperty("_Color"))
                throw new InvalidOperationException("Arena: oprawa bez obsługi emisji " + material.name);
            for (int i = 0; i < required.Length; i++)
                if (material.name.StartsWith(required[i], StringComparison.Ordinal)) seen[i] = true;
            // Retain the earlier moderate arena-only glow. Do not affect any
            // city, estate or camp material, and do not create extra lights.
            material.SetColor("_EmissionColor", material.GetColor("_Color") * .8f);
            material.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(material);
        }
        var missing = required.Where((name, i) => !seen[i]).ToArray();
        if (missing.Length != 0)
            throw new InvalidOperationException("Arena premium: brak konkretnych materiałów opraw i szyldów: " +
                string.Join(", ", missing));
        Debug.Log("BRAXHORN_ARENA_OPRAWY premium=4/4 wszystkie_emisyjne=" + done.Count);
    }

    static void ApWalkaPokazowa()
    {
        // Właściciel: kopiujemy zawodników Z OBOZU. Te same źródła, importer, PBR,
        // most Humanoid→Legacy i skala. Nie uruchamiamy ani nie zmieniamy sceny obozu.
        var manifest = BraxObozCharacters.ReadManifest();
        var holder = new GameObject("BRAXHORN ARENA: walka pokazowa KADE / ZYRO"); holder.transform.SetParent(root, false);
        var pokaz = holder.AddComponent<BraxArenaPokaz>();
        pokaz.srodek = apC + Vector3.up * (apF + .9f * K);
        pokaz.promienBezpieczny = 4.55f * K - 1.3f;
        var ids = new[] { "kade", "zyro" };
        var sourceNames = new[] { "CombatIdle", "Walk", "Punch", "Dodge", "HeadHit" };
        var showNames = new[] { "Garda", "Krok", "Kombinacja", "Unik", "Trafienie" };
        for (int i = 0; i < ids.Length; i++)
        {
            var spec = manifest.characters.Single(c => c.id == ids[i]);
            if (!spec.hasGloves || !spec.glovesVerified) throw new InvalidOperationException("Pokaz: niepotwierdzone rękawice " + spec.name);
            var fighter = new GameObject("Pokaz: " + spec.name); fighter.transform.SetParent(holder.transform, false);
            fighter.transform.position = pokaz.srodek + Vector3.forward * (i == 0 ? -.95f : .95f);
            fighter.transform.rotation = Quaternion.Euler(0, i == 0 ? 0 : 180, 0);
            // Dokładnie ten sam tryb i wzrost co obecne instancje NPC w obozie.
            // sourceReviewOnly nie oznacza zatwierdzenia jakości; raport nadal żąda oględzin w Unity.
            var visual = BraxObozCharacters.CreateCached(spec, K, sourceReviewOnly: true);
            visual.transform.SetParent(fighter.transform, false);
            var anim = visual.GetComponent<Animation>();
            if (!anim) throw new InvalidOperationException("Pokaz: model obozu bez Animation " + spec.name);
            var clips = new AnimationClip[showNames.Length];
            for (int j = 0; j < clips.Length; j++)
            {
                var source = anim.GetClip(sourceNames[j]);
                if (!source) throw new InvalidOperationException("Pokaz: " + spec.name + " bez " + sourceNames[j]);
                clips[j] = KlipPokazu(visual, source, showNames[j], spec.id);
            }
            // W scenie wyspy potrzebnych jest pięć klipów, nie cały repertuar obozu.
            foreach (string name in anim.Cast<AnimationState>().Select(s => s.name).ToArray()) anim.RemoveClip(name);
            for (int j = 0; j < clips.Length; j++) anim.AddClip(clips[j], showNames[j]);
            anim.clip = clips[0]; anim.playAutomatically = true; anim.cullingType = AnimationCullingType.AlwaysAnimate;
            anim.Play("Garda"); anim.Sample();
            foreach (var tr in visual.GetComponentsInChildren<Transform>(true))
            { tr.gameObject.layer = 0; GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0); }
            foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.sharedMesh || smr.bones.Length == 0 || smr.bones.Any(b => !b)) throw new InvalidOperationException("Pokaz: niepełny szkielet " + smr.name);
                smr.updateWhenOffscreen = false;
            }
            float h = (spec.realHeight > 0 ? spec.realHeight : spec.previewHeight) * K;
            var box = fighter.AddComponent<CapsuleCollider>(); box.radius = .34f * K; box.height = h; box.center = Vector3.up * h * .5f;
            var body = fighter.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            if (i == 0) { pokaz.zawodnikA = fighter.transform; pokaz.animacjaA = anim; }
            else { pokaz.zawodnikB = fighter.transform; pokaz.animacjaB = anim; }
        }
    }

    static AnimationClip KlipPokazu(GameObject model, AnimationClip source, string name, string character)
    {
        var clip = Object.Instantiate(source); clip.name = name; clip.legacy = true;
        clip.wrapMode = name == "Garda" || name == "Krok" ? WrapMode.Loop : WrapMode.ClampForever;
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        int channels = 0;
        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            if (b.type != typeof(Transform)) continue;
            var tr = string.IsNullOrEmpty(b.path) ? model.transform : model.transform.Find(b.path);
            if (!tr) throw new InvalidOperationException("Pokaz: animacja " + name + " nie pasuje do kości " + b.path);
            channels++;
            // XZ korzenia/bioder pozostaje w miejscu. Całą pozycją w klatce zarządza choreografia.
            if ((string.IsNullOrEmpty(b.path) || tr.name.EndsWith("Hips", StringComparison.Ordinal)) &&
                (b.propertyName == "m_LocalPosition.x" || b.propertyName == "m_LocalPosition.z" || b.propertyName == "localPosition.x" || b.propertyName == "localPosition.z"))
            {
                var c = AnimationUtility.GetEditorCurve(clip, b); float v = c.Evaluate(0);
                AnimationUtility.SetEditorCurve(clip, b, AnimationCurve.Constant(0, clip.length, v));
            }
        }
        if (channels < 12) throw new InvalidOperationException("Pokaz: za mało kanałów szkieletu " + name);
        BraxZapisAssetu.Utworz(clip, BraxMiastoA.Out + "/Wyspa/pokaz_" + character + "_" + name + ".anim");
        return clip;
    }

    static Vector3[] BrzegWyspy()
    {
        var brzeg = new Vector3[192];
        for (int i = 0; i < brzeg.Length; i++)
        {
            float deg = i * 360f / brzeg.Length, rad = deg * Mathf.Deg2Rad;
            brzeg[i] = new Vector3(CX + Mathf.Cos(rad) * LiniaWody(deg), W, CZ + Mathf.Sin(rad) * LiniaWody(deg));
        }
        return brzeg;
    }

    static void WykonczenieBrzegu()
    {
        var brzeg = BrzegWyspy(); var mesh = new Mesh { name = "Wyspa: pas piany przy naturalnym brzegu" };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
        float along = 0;
        for (int i = 0; i <= brzeg.Length; i++)
        {
            var p = brzeg[i % brzeg.Length]; if (i > 0) along += Vector3.Distance(p, brzeg[(i - 1) % brzeg.Length]);
            var dir = (p - new Vector3(CX, W, CZ)).normalized;
            for (int j = 0; j < 4; j++)
            {
                float f = j / 3f; var q = p + dir * Mathf.Lerp(2f, -3.2f, f);
                q.y = Mathf.Max(W, Y(q.x, q.z)) + .025f;
                vertices.Add(q); uv.Add(new Vector2(along, f)); colors.Add(new Color(1, 1, 1, j == 0 || j == 3 ? 0 : .55f));
                if (i < brzeg.Length && j < 3) { int a = i * 4 + j; triangles.AddRange(new[] { a, a + 1, a + 5, a, a + 5, a + 4 }); }
            }
        }
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        BraxZapisAssetu.Utworz(mesh, BraxMiastoA.Out + "/Wyspa/piana_brzegu.asset");
        var foam = BraxMiastoA.Obiekt(root, "Wyspa: delikatna piana brzegu", mesh, BraxNabrzeze.Piana("Wyspa: spokojny brzeg", false, 9f));
        foam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        // Kamienie pozostają istniejącymi modelami CC0. Drobne muszle: własne siatki,
        // scalone w jeden materiał, poza wyznaczonymi ciągami pieszymi i bez colliderów.
        var shells = new UlSiatka(); var random = new System.Random(20261104);
        for (int i = 0; i < 70; i++)
        {
            float angle = 205f + (float)random.NextDouble() * 125f, rad = angle * Mathf.Deg2Rad;
            float r = LiniaWody(angle) - 4f - (float)random.NextDouble() * 7f;
            var p = new Vector3(CX + Mathf.Cos(rad) * r, 0, CZ + Mathf.Sin(rad) * r);
            p.y = Y(p.x, p.z); if (Zajete(p.x, p.z, .6f)) continue;
            float size = .045f + (float)random.NextDouble() * .035f;
            var normal = new Vector3(Y(p.x - .2f, p.z) - Y(p.x + .2f, p.z), .4f, Y(p.x, p.z - .2f) - Y(p.x, p.z + .2f)).normalized;
            var rot = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, i * 137.5f, 0);
            for (int k = 0; k < 10; k++)
            {
                float a = Mathf.PI * k / 10f, b = Mathf.PI * (k + 1) / 10f;
                var v0 = p + rot * new Vector3(Mathf.Cos(a) * size, .008f, Mathf.Sin(a) * size * 1.25f);
                var v1 = p + rot * new Vector3(Mathf.Cos(b) * size, .008f, Mathf.Sin(b) * size * 1.25f);
                shells.Tri(p + normal * .024f, v0, v1, normal);
            }
        }
        var go = BraxMiastoA.Obiekt(root, "Wyspa: muszle (detal brzegu)", shells, BraxMiastoA.Mat("Wyspa: muszle kremowe", new Color(.77f, .72f, .63f), .8f));
        var renderer = go.GetComponent<Renderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off;
        // Zasięg dla scalonego detalu, nie dla całej plaży.
        var lod = go.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.35f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
    }

    static void AmbientWyspy()
    {
        var g = new GameObject("Wyspa: ambient morza, wiatru i gali"); g.transform.SetParent(root, false);
        var ambient = g.AddComponent<BraxWyspaAmbient>();
        ambient.brzeg = BrzegWyspy(); ambient.srodekAreny = apC; ambient.apotemaAreny = apAp - .6f; ambient.podlogaAreny = apF;
        ambient.morze = Zrodlo("Morze", 0, ambient.brzeg[120], 100f, 1f);
        ambient.wiatr = Zrodlo("Wiatr", 1, new Vector3(CX, W + 6f, CZ), 260f, 0f);
        ambient.kibice = Zrodlo("Kibice", 2, apC + Vector3.up * (apF + 5f), 40f, .8f);
        AudioSource Zrodlo(string name, int rodzaj, Vector3 pos, float range, float blend)
        {
            var o = new GameObject("Ambient: " + name); o.transform.SetParent(g.transform, false); o.transform.position = pos;
            var source = o.AddComponent<AudioSource>(); source.clip = PetlaWyspy(name, rodzaj); source.loop = true;
            source.playOnAwake = false; source.spatialBlend = blend; source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 8f; source.maxDistance = range; source.dopplerLevel = 0; source.volume = 0; source.priority = 180;
            return source;
        }
    }

    static AudioClip PetlaWyspy(string name, int rodzaj)
    {
        // Oryginalna synteza na etapie przygotowania sceny, bez nagrań z internetu.
        // Stały seed, przenikanie końca w początek; tylko WAV wynikowy trafia do importera.
        const int hz = 22050, n = hz * 16, fade = hz;
        var samples = new float[n + fade]; var random = new System.Random(104 + rodzaj);
        float low = 0, mid = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float white = (float)random.NextDouble() * 2f - 1f, t = i / (float)hz;
            low += .008f * (white - low); mid += .12f * (white - mid);
            float env = .55f + .25f * Mathf.Sin(t * 2f * Mathf.PI / 8f) + .15f * Mathf.Sin(t * 2f * Mathf.PI / 5.333333f);
            samples[i] = rodzaj == 0 ? (low * 1.8f + mid * .25f) * env : rodzaj == 1 ? low * .7f : Tlum(t, white, mid, low);
        }
        for (int i = 0; i < fade; i++) samples[i] = Mathf.Lerp(samples[n + i], samples[i], i / (float)fade);
        float peak = .001f; for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        string path = BraxMiastoA.Out + "/Wyspa/ambient_" + name + ".wav";
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + n * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(hz); writer.Write(hz * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(n * 2);
            for (int i = 0; i < n; i++) writer.Write((short)(samples[i] / peak * 24000f));
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is AudioImporter importer)
        {
            var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .55f;
            importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (!clip) throw new InvalidOperationException("Ambient: brak klipu " + path);
        return clip;
    }

    static float Tlum(float t, float noise, float mid, float low)
    {
        // Własna, niskopoziomowa synteza głosów i oklasków. Bez tekstu, nagrań
        // i cudzych praw. Jakość atmosfery wymaga odsłuchu w docelowej arenie.
        float voices = 0f;
        for (int v = 0; v < 7; v++)
        {
            float phase = Mathf.Repeat(t + v * 1.73f, 8f);
            float envelope = Mathf.Sin(Mathf.Clamp01(phase / 1.7f) * Mathf.PI);
            float fundamental = 107f + v * 19f;
            float voiced = 0f;
            for (int harmonic = 1; harmonic <= 12; harmonic++)
            {
                float frequency = fundamental * harmonic;
                float formant = Mathf.Exp(-Mathf.Pow((frequency - 700f) / 210f, 2f))
                    + .6f * Mathf.Exp(-Mathf.Pow((frequency - 1150f) / 250f, 2f));
                voiced += Mathf.Sin(t * frequency * 2f * Mathf.PI + v) * formant / harmonic;
            }
            voices += voiced * envelope * .012f;
        }
        float clap = Mathf.Repeat(t, 4f) < 1.2f ? Mathf.Exp(-Mathf.Repeat(t, .19f) * 95f) : 0f;
        return (mid - low) * .045f + voices + noise * clap * .012f;
    }
}
