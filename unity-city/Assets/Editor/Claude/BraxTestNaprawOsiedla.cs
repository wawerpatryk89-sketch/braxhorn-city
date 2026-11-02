using System;
using System.Collections.Generic;
using UnityEngine;

// Regression checks run in the real Unity editor before estate generation.
// A failed check aborts generation; these do not replace gameplay/device validation.
public static class BraxTestNaprawOsiedla
{
    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Regresja osiedla: " + message); }

    static float Area(Mesh m)
    {
        var p = m.vertices; float area = 0;
        for (int sub = 0; sub < m.subMeshCount; sub++)
        {
            var t = m.GetTriangles(sub);
            for (int i = 0; i < t.Length; i += 3) area += Vector3.Cross(p[t[i+1]]-p[t[i]], p[t[i+2]]-p[t[i]]).magnitude * .5f;
        }
        return area;
    }
    public static void Wykonaj()
    {
        Require(BraxAuto.KatKola(0,3,.8f)==0, "koła prosto");
        Require(BraxAuto.KatKola(25,3,.8f)>BraxAuto.KatKola(25,3,-.8f), "wewnętrzne koło skręca mocniej");
        Require(Mathf.Abs(BraxAuto.KatKola(25,3,.8f)+BraxAuto.KatKola(-25,3,-.8f))<.0001f, "symetria skrętu");
        var traffic = new BraxRuchAutSym(null,null,null,null,1);
        traffic.zewnetrzne.Add(new BraxRuchAutSym.ObrysZewnetrzny { srodek=Vector3.zero, przod=Vector3.forward,polSzer=1.2f,polDl=3f });
        Require(traffic.ZajeteZewnetrznie(new Vector3(0,0,4),Vector3.forward,1,2),"zderzak auta gracza");
        Require(traffic.ZajeteZewnetrznie(new Vector3(2,0,0),Vector3.right,1,2),"auto gracza stojące w poprzek");
        Require(!traffic.ZajeteZewnetrznie(new Vector3(6,0,0),Vector3.forward,1,2),"wolny sąsiedni pas");
        var mesh = new Mesh { name = "test starej nawierzchni", subMeshCount = 2 };
        Mesh cut = null, empty = null;
        try
        {
            mesh.vertices = new[] { new Vector3(-2,0,-2), new Vector3(-2,0,2), new Vector3(2,0,2), new Vector3(2,0,-2) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(-2,-2), new Vector2(-2,2), new Vector2(2,2), new Vector2(2,-2) };
            mesh.SetTriangles(new[] {0,1,2}, 0); mesh.SetTriangles(new[] {0,2,3}, 1);
            // All original vertices are outside this strip. The old full-containment test removed nothing.
            var strip = new Bounds(Vector3.zero, new Vector3(2,2,6));
            cut = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.identity, new[] { strip }, out int changed);
            Require(cut && changed == 2, "trójkąty przecinające drogę");
            Require(Mathf.Abs(Area(cut)-8) < .0001f && Mathf.Abs(Area(mesh)-16) < .0001f, "pole wycięcia / nienaruszone źródło");
            Require(cut.subMeshCount == 2, "materiały po wycięciu");
            var vertices = cut.vertices; var uv = cut.uv;
            Require(uv.Length == vertices.Length && cut.normals.Length == vertices.Length, "kanały wierzchołków");
            for (int i = 0; i < vertices.Length; i++)
                Require((uv[i]-new Vector2(vertices[i].x,vertices[i].z)).sqrMagnitude < 1e-8f, "interpolacja UV");
            empty = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.identity, new[] { new Bounds(Vector3.zero, Vector3.one * 10) }, out changed);
            Require(empty && empty.vertexCount == 0 && empty.subMeshCount == 2, "całkowite wycięcie / puste materiały");
            var unchanged = BraxWycinanieSiatki.Wytnij(mesh, Matrix4x4.Translate(Vector3.right*100), new[] {strip}, out changed);
            Require(!unchanged && changed == 0, "wycięcie w współrzędnych świata");
            var parts = BraxWycinanieSiatki.Odejmij(new Bounds(Vector3.zero,new Vector3(4,2,1)), new[] {strip});
            float volume = 0; foreach (var b in parts) volume += b.size.x*b.size.y*b.size.z;
            Require(parts.Count == 2 && Mathf.Abs(volume-4) < .0001f, "kolizja długiego płotu poza jezdnią");
            Debug.Log("BRAXHORN_REGRESJA wycinanie: PASS (granice, pole, UV, submeshes, pusty wynik, transform, kolizje)");
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); if (cut) UnityEngine.Object.DestroyImmediate(cut); if (empty) UnityEngine.Object.DestroyImmediate(empty); }
        Build45();
    }

    // ---------------- build 45: testy nowych funkcji (bez sceny) ----------------
    static void Build45()
    {
        // E3c: poślizg auta po przeszkodzie i skręt zależny od prędkości
        var wzdluz = BraxAutoGracza.Slizg(new Vector3(.3f, 0, 1).normalized, Vector3.left, out float utrata);   // ściana po prawej (normalna w lewo), jazda lekko skosem
        Require(wzdluz.x == 0f && wzdluz.z > .3f && utrata > .15f && utrata < .85f, "ślizg wzdłuż ściany (bez wjazdu w ścianę, część prędkości)");
        BraxAutoGracza.Slizg(Vector3.forward, Vector3.back, out float czolo);
        var styczne = BraxAutoGracza.Slizg(Vector3.forward * 2f, Vector3.right, out float bok);
        Require(czolo > .8f && bok < .2f && Mathf.Abs(styczne.z - 2f * (1f - bok)) < 1e-4f, "czołowo prawie stop, stycznie mała strata");
        Require(BraxAutoGracza.Slizg(Vector3.forward, new Vector3(0, 1, -1).normalized, out _).y == 0f, "ślizg bez składowej pionowej");
        Require(Mathf.Abs(BraxAutoGracza.MaksSkret(0f, 12f) - 38f) < .01f && Mathf.Abs(BraxAutoGracza.MaksSkret(12f, 12f) - 12f) < .01f
                && BraxAutoGracza.MaksSkret(6f, 12f) < 38f && BraxAutoGracza.MaksSkret(-1f, 12f) == 38f, "skręt 38° przy małej prędkości, maleje z prędkością");
        // E6: próby pozy w kabinie — od najmniejszej zmiany, maks. odchylenie i pochylenie głowy na końcu, skala osoby nietknięta
        var proby = BraxClaudeOsiedle.KabinaProby(25f, 30f, 1.294f);
        Require(proby.Count == 1 + 3 + 5 + 2 && proby[0].odch == 0f && proby[0].zagl == 0f && proby[0].poch == 0f && proby[1].odch == 25f && Mathf.Abs(proby[1].zagl - .03f * 1.294f) < 1e-5f
                && proby[proby.Count - 1].odch == 30f && proby[proby.Count - 1].poch == 10f, "kolejność prób pozy w kabinie (najpierw poza z #44 bez zmian)");
        BraxFotelAuta.Rodzaj(1.17f, out var k1, out var b1, out var m1); BraxFotelAuta.Rodzaj(1.45f, out var k2, out _, out _); BraxFotelAuta.Rodzaj(2.1f, out var k3, out var b3, out var m3);
        Require(k1 == "sportowe" && b1 == 25f && m1 == 30f && k2 == "osobowe" && k3 == "dostawcze" && b3 == 8f && m3 == 12f, "rodzaj kabiny z wysokości auta");
        var go = new GameObject("test fotela"); var go2 = new GameObject("test kręgosłupa"); var glowa = new GameObject("test głowy");
        try
        {
            var f = go.AddComponent<BraxFotelAuta>(); f.przeswit = .9f; f.mapaNx = 2; f.mapaNz = 3; f.mapaKrok = .5f; f.mapaX0 = -.25f; f.mapaZ0 = -.5f;
            f.mapa = new[] { 1.0f, 1.0f, .8f, .9f, 1.2f, 1.2f };          // z −0.5: 1.0; z 0: 0.8/0.9; z 0.5: 1.2
            Require(Mathf.Abs(f.Dach(0f, -.25f) - .8f) < 1e-5f && Mathf.Abs(f.Dach(0f, .4f) - .8f) < 1e-5f && Mathf.Abs(f.Dach(5f, 5f) - .8f) < 1e-5f, "mapa dachu: minimum sąsiadów, brzeg");
            f.mapa = new float[0]; Require(f.Dach(0, 0) == .9f, "bez mapy — prześwit nad biodrem");
            // poza w aucie: odchylenie przesuwa głowę do tyłu i w dół
            glowa.transform.SetParent(go2.transform, false); glowa.transform.localPosition = new Vector3(0, .8f, 0);
            var przed = glowa.transform.position;
            BraxMieszkaniec.PozaWAucie(go2.transform, glowa.transform, Vector3.right, 25f, 0f);
            var po = glowa.transform.position;
            Require(po.z < przed.z - .3f && po.y < przed.y - .05f, "odchylenie tułowia: głowa do tyłu i niżej");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(go2); }
        // E4: głos — rozmowa z graczem bliżej i wyraźniej niż tło
        BraxGlosPostaci.Ustawienia(false, 3f, out float bl1, out float gl1); BraxGlosPostaci.Ustawienia(false, 20f, out float bl2, out float gl2); BraxGlosPostaci.Ustawienia(true, 0f, out float bl3, out _);
        Require(bl1 < 1f && bl2 == 1f && bl3 == 0f && gl1 > gl2 && BraxGlosPostaci.MinOdl > 5f, "głos: rozmowa blisko (spatialBlend < 1), tło 3D, gracz 2D, min 4 m × K");
        Require(BraxZrodloPetli.Spadek(.05f, .1f) == 1f && BraxZrodloPetli.Spadek(1f, .1f) < 1e-4f && BraxZrodloPetli.Spadek(.5f, .1f) < BraxZrodloPetli.Spadek(.3f, .1f), "fontanna: spadek do zera, malejący");
        Require(Mathf.Abs(BraxGlosyImport.DlugoscOgg(new byte[10]) + 1f) < 1e-6f, "Ogg: zły nagłówek = −1");
        // E5: odejmowanie trójkątów — kwadrat 4×4 (grunt) minus płyta 2×2 nad środkiem → 12 m², oryginał nietknięty, UV z interpolacji
        var grunt = new Mesh { name = "test gruntu" }; Mesh wynik = null;
        try
        {
            grunt.vertices = new[] { new Vector3(-2, 0, -2), new Vector3(-2, 0, 2), new Vector3(2, 0, 2), new Vector3(2, 0, -2) };
            grunt.uv = new[] { new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, 2), new Vector2(2, -2) };
            grunt.triangles = new[] { 0, 1, 2, 0, 2, 3 }; grunt.RecalculateNormals();
            var plyta = new List<Vector2[]> { new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1) }, new[] { new Vector2(-1, -1), new Vector2(1, 1), new Vector2(1, -1) } };
            wynik = BraxWycinanieSiatki.OdejmijTrojkaty(grunt, Matrix4x4.identity, -1, 1, 1, 0f, .002f, plyta, out int zm, out float p0, out float p1);
            Require(wynik && zm == 2 && Mathf.Abs(p0 - 16f) < 1e-3f && Mathf.Abs(p1 - 12f) < 1e-3f && Mathf.Abs(Area(wynik) - 12f) < 1e-3f && Mathf.Abs(Area(grunt) - 16f) < 1e-4f, "wycięcie gruntu dokładnie pod płytą");
            var vv = wynik.vertices; var uu = wynik.uv;
            for (int i = 0; i < vv.Length; i++) Require((uu[i] - new Vector2(vv[i].x, vv[i].z)).sqrMagnitude < 1e-8f, "UV po odejmowaniu");
            var n = wynik.normals; for (int i = 0; i < n.Length; i++) Require(n[i].y > .99f, "normalne po odejmowaniu");
            Require(!BraxWycinanieSiatki.OdejmijTrojkaty(grunt, Matrix4x4.Translate(Vector3.up * .5f), -1, 1, 1, 0f, .002f, plyta, out _, out _, out _), "inna płaszczyzna — bez zmian");
            Require(!BraxWycinanieSiatki.OdejmijTrojkaty(grunt, Matrix4x4.identity, -1, 1, 0, 0f, .002f, plyta, out _, out _, out _), "przeciwny zwrot — bez zmian");
        }
        finally { UnityEngine.Object.DestroyImmediate(grunt); if (wynik) UnityEngine.Object.DestroyImmediate(wynik); }
        // E4b: kto zagaduje — ~1 na 6 (stała osobowość), limity osoby/globalny, nie gdy gracz biegnie/jedzie; krzywa głośności
        int zag = 0; for (int i = 0; i < 6000; i++) if (BraxLudzie.Zagadujacy(BraxLudzie.Ziarno("Przechodzień " + i, new Vector3(i * .7f, 0, -i * 1.3f)))) zag++;
        Require(zag > 6000 / 8 && zag < 6000 / 5, "zagaduje ~1 na 6 osób (" + zag + "/6000)");
        Require(BraxLudzie.Zagadujacy(BraxLudzie.Ziarno("a", Vector3.one)) == BraxLudzie.Zagadujacy(BraxLudzie.Ziarno("a", Vector3.one)), "stała osobowość");
        Require(BraxLudzie.MozeZagadac(true, 100f, 0f, 0f, 0f, 1f, false) && !BraxLudzie.MozeZagadac(false, 100f, 0f, 0f, 0f, 1f, false), "niezagadujący milczy");
        Require(!BraxLudzie.MozeZagadac(true, 100f, 120f, 0f, 0f, 1f, false) && !BraxLudzie.MozeZagadac(true, 100f, 0f, 105f, 0f, 1f, false) && !BraxLudzie.MozeZagadac(true, 100f, 0f, 0f, 101f, 1f, false), "limit osoby / globalny / ktoś już mówi");
        Require(!BraxLudzie.MozeZagadac(true, 100f, 0f, 0f, 0f, 5f, false) && !BraxLudzie.MozeZagadac(true, 100f, 0f, 0f, 0f, 1f, true), "nie gdy gracz biegnie albo jedzie");
        Require(BraxGlosPostaci.Krzywa(1f) == 1f && BraxGlosPostaci.Krzywa(BraxGlosPostaci.Cisza + 1f) == 0f && BraxGlosPostaci.Krzywa(8f) > BraxGlosPostaci.Krzywa(12f) && BraxGlosPostaci.Krzywa(12f) > 0f, "głośność: wyraźnie blisko, płynnie ciszej, cisza dalej");
        BraxGlosPostaci.Ustawienia(BraxGlosPostaci.Rodzaj.Mijanie, false, out _, out float gm); BraxGlosPostaci.Ustawienia(BraxGlosPostaci.Rodzaj.Tlo, false, out _, out float gt); BraxGlosPostaci.Ustawienia(BraxGlosPostaci.Rodzaj.Rozmowa, false, out _, out float gr);
        Require(gr > gm && gm > gt, "rozmowa głośniej niż mijanie, mijanie głośniej niż tło");
        // E22: kamera za plecami — martwa strefa, ograniczona prędkość, zbieżność, przejście przez ±180°
        Require(BraxGymPlayer.KrokZaPlecy(0f, 3f, 1f / 60f) == 0f, "kamera: martwa strefa 4°");
        float yaw = 0f; for (int i = 0; i < 120; i++) { float prev = yaw; yaw = BraxGymPlayer.KrokZaPlecy(yaw, 90f, 1f / 60f); Require(yaw - prev <= BraxGymPlayer.ZaPlecamiMaks / 60f + 1e-4f, "kamera: maks. 220°/s"); }
        Require(Mathf.Abs(90f - yaw) <= BraxGymPlayer.ZaPlecamiMartwa + .01f, "kamera: wraca za plecy w ~2 s");
        Require(BraxGymPlayer.KrokZaPlecy(170f, -170f, 1f / 60f) > 170f, "kamera: najkrótsza droga przez 180°");
        Debug.Log("BRAXHORN_REGRESJA build45: PASS (ślizg auta, skręt, próby kabiny, rodzaj kabiny, mapa dachu, poza w aucie, głos, fontanna, Ogg, odejmowanie trójkątów, kamera za plecami, kto zagaduje, krzywa głosu)");
    }
}
