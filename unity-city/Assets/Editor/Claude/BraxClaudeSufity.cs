using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// SUFITY DLA KAMERY (właściciel 1.11, 18:19: „jak jesteś w pomieszczeniu, żeby nie można było zrobić kamery tak, że jesteś
// nad pomieszczeniem — blokada do sufitu”). Kamera gracza zatrzymuje się na kolizjach (SphereCast w BraxGymPlayer, maska bez
// warstwy gracza), ale wiele sufitów ma tylko siatkę bez kolizji — kamera przechodziła przez strop i pokazywała salę z góry.
// Tu: płyty kolizji na warstwie 29 (tylko dla kamery — gracz jej nie dotyka: Physics.IgnoreLayerCollision(8, 29), podłoga
// i łodzie mają maskę bez 29) pod każdym stropem:
//  • siatki gotowe (glTF Codexa, siłownia z .glb, miasto): cienkie poziome bryły ≥ 2.5 × 2.5 m, spód ≥ 2.2 m nad ziemią;
//  • osiedle (generator): czworokąty zwrócone w dół (spód stropu) zebrane w BraxClaudeOsiedle.Quad.
public static class BraxClaudeSufity
{
    public const int Warstwa = 29;

    static bool Sufit(Bounds b) => b.size.y < .6f && b.size.x > 2.5f && b.size.z > 2.5f && b.size.x * b.size.z > 9f && b.min.y > 2.2f && b.max.y < 40f;

    // Płyta 0.25 m nad spodem stropu (kamera staje tuż pod sufitem). Zwraca liczbę płyt.
    public static int Plyty(IEnumerable<Bounds> spody, Transform rodzic)
    {
        int n = 0; var byly = new HashSet<Vector3Int>();
        foreach (var b in spody)
        {
            var klucz = Vector3Int.RoundToInt(new Vector3(b.center.x * 4f, b.min.y * 4f, b.center.z * 4f) + new Vector3(b.size.x, 0, b.size.z) * 40f);
            if (!byly.Add(klucz)) continue;
            var g = new GameObject("sufit dla kamery") { layer = Warstwa };
            if (rodzic) g.transform.SetParent(rodzic, false);
            g.transform.position = new Vector3(b.center.x, b.min.y + .125f, b.center.z);
            var c = g.AddComponent<BoxCollider>(); c.size = new Vector3(b.size.x, .25f, b.size.z);
            g.isStatic = true; n++;
        }
        return n;
    }

    // Siatki gotowe w scenie (glTF Codexa, siłownia z .glb, miasto). Bryły bywają scalone dla wielu budynków naraz, więc nie
    // prostopadłościan siatki, tylko jej trójkąty zwrócone w dół (spód stropu) ≥ 2.2 m: siatka 1 m, komórki łączone w pasy
    // wzdłuż x. Pomijane: roślinność (LODGroup, liście/trawa — shader z przezroczystością), postacie, siatki scalone przez
    // generator osiedla (scal_* — te mają już płyty z BraxClaudeOsiedle.SpodStropu).
    public static int Renderery(Scene scena, Transform rodzic = null)
    {
        var kom = new HashSet<(int x, int z, int y)>();
        int siatki = 0;
        foreach (var root in scena.GetRootGameObjects())
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(false))
            {
                var r = mf ? mf.GetComponent<MeshRenderer>() : null; var mesh = mf ? mf.sharedMesh : null;
                if (!r || !r.enabled || !mesh || mf.gameObject.layer == Warstwa || mf.GetComponentInParent<LODGroup>()) continue;
                if (r.bounds.max.y < 2.2f || r.bounds.min.y > 60f) continue;
                string sciezka = UnityEditor.AssetDatabase.GetAssetPath(mesh);
                if (sciezka.Contains("/scal_")) continue;
                bool liscie = false;
                foreach (var m in r.sharedMaterials)
                    if (m && (m.renderQueue >= 2450 || m.shader.name.Contains("Liscie") || m.shader.name.Contains("Kep") || m.shader.name.Contains("Trawa"))) { liscie = true; break; }
                // nazwy roślin (stare korony Codexa bywają nieprzezroczyste): bez płyt pod drzewami
                string nz = (mf.name + " " + (r.sharedMaterial ? r.sharedMaterial.name : "")).ToLowerInvariant();
                foreach (var k in new[] { "leaf", "leaves", "liś", "lisc", "foliage", "crown", "koron", "tree", "drzew", "bush", "krzew", "grass", "traw", "hedge", "żywop", "palm", "plant", "rośl" })
                    if (nz.Contains(k)) { liscie = true; break; }
                if (liscie) continue;
                Vector3[] v; int[] t;
                try { v = mesh.vertices; t = mesh.triangles; } catch { continue; }
                if (v == null || t == null || v.Length == 0) continue;
                var tr = mf.transform; bool cos = false;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = tr.TransformPoint(v[t[i]]), b = tr.TransformPoint(v[t[i + 1]]), c = tr.TransformPoint(v[t[i + 2]]);
                    var n = Vector3.Cross(b - a, c - a); float pole = n.magnitude * .5f;
                    if (pole < .05f || n.y / (2f * pole) > -.9f) continue;                  // tylko w dół i nie drobiazgi
                    float y = Mathf.Min(a.y, Mathf.Min(b.y, c.y)); if (y < 2.2f) continue;
                    int yk = Mathf.RoundToInt(y * 4f);
                    int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - .001f);
                    int z0 = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z))), z1 = Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - .001f);
                    if ((x1 - x0 + 1) * (z1 - z0 + 1) > 4000) continue;                     // podejrzanie wielki trójkąt
                    for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++) kom.Add((x, z, yk));
                    cos = true;
                }
                if (cos) siatki++;
            }
        if (!rodzic) { var g = new GameObject("BRAXHORN sufity dla kamery"); SceneManager.MoveGameObjectToScene(g, scena); rodzic = g.transform; }
        // pasy wzdłuż x (kolejne komórki o tym samym z i wysokości), potem pasy o tym samym zakresie x w kolejnych z → prostokąty
        var lista = new List<(int x, int z, int y)>(kom); lista.Sort((p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.z != q.z ? p.z.CompareTo(q.z) : p.x.CompareTo(q.x));
        var pasy = new List<(int y, int x0, int x1, int z)>();
        for (int i = 0; i < lista.Count;)
        {
            int j = i; while (j + 1 < lista.Count && lista[j + 1].y == lista[i].y && lista[j + 1].z == lista[i].z && lista[j + 1].x == lista[j].x + 1) j++;
            pasy.Add((lista[i].y, lista[i].x, lista[j].x, lista[i].z)); i = j + 1;
        }
        pasy.Sort((p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.x0 != q.x0 ? p.x0.CompareTo(q.x0) : p.x1 != q.x1 ? p.x1.CompareTo(q.x1) : p.z.CompareTo(q.z));
        var spody = new List<Bounds>();
        for (int i = 0; i < pasy.Count;)
        {
            int j = i; while (j + 1 < pasy.Count && pasy[j + 1].y == pasy[i].y && pasy[j + 1].x0 == pasy[i].x0 && pasy[j + 1].x1 == pasy[i].x1 && pasy[j + 1].z == pasy[j].z + 1) j++;
            float y = pasy[i].y / 4f, sx = pasy[i].x1 - pasy[i].x0 + 1, sz = pasy[j].z - pasy[i].z + 1;
            if (sx * sz >= 4f) spody.Add(new Bounds(new Vector3(pasy[i].x0 + sx * .5f, y, pasy[i].z + sz * .5f), new Vector3(sx, 0f, sz)));   // drobne skrawki (< 4 m²) pomijane
            i = j + 1;
        }
        int nPlyt = Plyty(spody, rodzic);
        Debug.Log("BRAXHORN_SUFITY " + scena.name + ": " + nPlyt + " płyt dla kamery (" + kom.Count + " m² stropów z " + siatki + " siatek)");
        return nPlyt;
    }
}
