using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class BraxClaudeOsiedle
{
    static readonly List<string> raportSkaliAut = new List<string>();
    static void RaportSkaliAut(List<AutoSzablon> auta)
    {
        raportSkaliAut.Clear();
        raportSkaliAut.Add("K=" + K.ToString("F4") + "; jednostki świata; metry rzeczywiste = jednostki/K");
        foreach (var a in auta)
            raportSkaliAut.Add("AUTO " + a.id + " długość=" + a.dl.ToString("F3") + " szerokość_z_lusterkami=" + a.szer.ToString("F3") + " wysokość=" + a.wys.ToString("F3"));
        float szer = auta.Count > 0 ? auta.Max(a => a.szer) : 0f;
        void Ulica(string nazwa, float pas)
        {
            float luz = pas - szer;
            raportSkaliAut.Add("DROGA " + nazwa + " jezdnia=" + (2f * pas).ToString("F3") + " pas=" + pas.ToString("F3") + " zapas_na_pas=" + luz.ToString("F3")
                + (luz < .4f * K ? " UWAGA: za mały zapas dla najszerszego auta" : ""));
        }
        if (ulice != null) foreach (var u in ulice) Ulica(u.nazwa ?? u.id, u.cw);
        Ulica("B-główna i B-N-S", Carriage);
        foreach (var line in raportSkaliAut) Debug.Log("BRAXHORN_SKALA_AUT " + line);
    }

    // Raport diagnostyczny; na polecenie właściciela błędy kontroli nie blokują APK do sprawdzenia na Motoroli.
    public static void SprawdzAktualizacje(Scene scene)
    {
        var bledy = new List<string>();
        if (!ruchZweryfikowany) bledy.Add("Nie wykonano testu ruchu");
        if (bladRuchu != null) bledy.Add(bladRuchu);
        bledy.AddRange(rep.notes.Where(n => n.StartsWith("BŁĄD w kroku")));
        var renderery = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
        int szyldy = 0;
        foreach (var r in renderery)
        {
            if (!r.enabled) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf && !mf.sharedMesh) bledy.Add("Brak siatki: " + r.name);
            bool maSiatke = mf && mf.sharedMesh || r is SkinnedMeshRenderer;
            if (!maSiatke) continue;
            if (r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m => !m || !m.shader || m.shader.name == "Hidden/InternalErrorShader"))
                bledy.Add("Brak materiału/shadera: " + r.name);
            if (r.name.StartsWith("szyld "))
            {
                szyldy++;
                if (!r.sharedMaterial || !r.sharedMaterial.mainTexture) bledy.Add("Brak obrazu szyldu: " + r.name);
            }
        }
        foreach (var nazwa in new[] { "salon_szyld", "stacja_pas", "stacja_pylon" })
            if (renderery.Count(r => r.name == "szyld " + nazwa && r.sharedMaterial && r.sharedMaterial.mainTexture) < 2)
                bledy.Add("Brak obu poprawnych stron szyldu " + nazwa);
        var mural = renderery.FirstOrDefault(r => r.name == "DJ: mural PULS CLUB (grafika właściciela)");
        if (!mural || mural.bounds.size.z < 29f || mural.bounds.size.y < 5f) bledy.Add("Mural nie pokrywa całej ściany Puls");
        var sofy = renderery.Where(r => r.transform.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains(": sofa chesterfield"))).ToArray();
        if (sofy.Length != 13) bledy.Add("Niekompletny układ pojedynczych sof Puls: " + sofy.Length + "/13");
        var gracz = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxGymPlayer>(true)).FirstOrDefault();
        if (!gracz || !gracz.visual || gracz.visual.name != "Patryk") bledy.Add("Brak nowego Patryka");
        if (!scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxAutoGracza>(true)).Any()) bledy.Add("Brak dostępnego auta Patryka");
        foreach (var m in MiejscaParkingowe)
            if (m.rozmiar.x < 3f * K - .001f || m.rozmiar.y < 5f * K - .001f) bledy.Add("Za mała zatoka " + m.srodek);
        try { TestTozsamosciMaterialu(); } catch (Exception e) { bledy.Add("Regresja materiału: " + e.Message); }
        try { TestKonfliktuDuzychAut(); } catch (Exception e) { bledy.Add("Regresja obrysów: " + e.Message); }
        System.IO.Directory.CreateDirectory("city-review");
        System.IO.File.WriteAllLines("city-review/skala-aut-i-drog.txt", raportSkaliAut.Concat(MiejscaParkingowe.Select(m =>
            "PARKING " + m.srodek.ToString("F2") + " szerokość=" + m.rozmiar.x.ToString("F3") + " długość=" + m.rozmiar.y.ToString("F3"))));
        System.IO.File.WriteAllLines("city-review/aktualizacja-osiedla.txt", new[] {
            "Kontrola sceny przed zapisem: szyldy=" + szyldy + ", sofy=" + sofy.Length + ", zatoki=" + MiejscaParkingowe.Count,
            "Wynik: " + (bledy.Count == 0 ? "PASS" : "FAIL"),
            "Obraz, animacje, autobus w ruchu i sterowanie: wymagany test APK na urządzeniu."
        }.Concat(bledy));
        if (bledy.Count > 0) Debug.LogWarning("BRAXHORN_AKTUALIZACJA_SCENA FAIL — " + bledy.Count + " błędów:\n" + string.Join("\n", bledy.Take(20)));
        else Debug.Log("BRAXHORN_AKTUALIZACJA_SCENA PASS szyldy=" + szyldy + " sofy=" + sofy.Length + " zatoki=" + MiejscaParkingowe.Count);
    }

    static void TestTozsamosciMaterialu()
    {
        string nazwa = "regresja_szyld_" + Guid.NewGuid().ToString("N");
        string path = null; GameObject go = null;
        try
        {
            var a = SalonZapisz(new Material(Shader.Find("Standard")), nazwa); path = AssetDatabase.GetAssetPath(a);
            go = new GameObject("test pierwszego lica szyldu"); var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = a;
            var b = SalonZapisz(new Material(Shader.Find("Standard")), nazwa);
            if (!r.sharedMaterial || r.sharedMaterial != b) throw new InvalidOperationException("Drugie lico zniszczyło materiał pierwszego szyldu");
        }
        finally { if (go) UnityEngine.Object.DestroyImmediate(go); if (path != null) AssetDatabase.DeleteAsset(path); }
    }

    static void TestKonfliktuDuzychAut()
    {
        var pkt = new List<Vector3>();
        for (int i = 0; i <= 80; i++) pkt.Add(new Vector3(-20f + i * .5f, 0, 0));
        for (int i = 0; i <= 80; i++) pkt.Add(new Vector3(20f - i * .5f, 0, 3f));
        var tr = new[] { new BraxTrasaAut { start = 0, ile = 81, dl = 40, krok = .5f }, new BraxTrasaAut { start = 81, ile = 81, dl = 40, krok = .5f } };
        var zd = new[] { new BraxZdarzenieAut { trasa = 0, typ = 0, wezel = 0, s0 = 18, s1 = 22, wlot = Vector3.right },
            new BraxZdarzenieAut { trasa = 1, typ = 0, wezel = 0, s0 = 18, s1 = 22, wlot = Vector3.left } };
        var sym = new BraxRuchAutSym(pkt.ToArray(), tr, zd, new[] { new BraxWezelAut() }, 1, 3.4f, 7f);
        if (sym.LiczbaKonfliktow != 1) throw new InvalidOperationException("Konflikt aut szerokości 3.4 m został pominięty");
    }
}
