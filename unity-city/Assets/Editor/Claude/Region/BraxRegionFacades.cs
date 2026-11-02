using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ULICE OD WYJŚCIA Z SIŁOWNI (przebudowa 2.11, właściciel po teście 13.1: „budynki migotają — elewacja do zmiany”,
/// „tylko ściana, elewacja, nic nie zrobiona”). Krok regionu „elewacje ulicy” jest teraz wejściem do nowych etapów ulic
/// (każdy osobno zabezpieczony — błąd jednego nie przerywa reszty):
///   • BraxUlicePierzeje — pierzeje od nowa: ściany z prawdziwymi otworami okiennymi (głębia, ościeża, rama, parapet,
///     pokój za szybą), gzymsy, cokoły, witryny parteru z wnętrzem, drzwi z numerami, szyldy, markizy, rynny;
///   • BraxUliceChodniki — chodniki z płyt w skali Braxa, krawężniki, przejścia z obniżeniami, barierki, słupki, kosze,
///     latarnie, znaki, drzewa w kratach, wiaty śmietnikowe;
///   • BraxUliceSport — BRAXHORN SPORT z zewnątrz (fasada, witryna, szyld, wejście).
/// USUNIĘTE (2.11): dawna okładzina z cegły/tynku stawiana 2 cm przed ścianami oryginału według bounds rendererów —
/// to była WISZĄCA ELEWACJA przy BRAXHORN TOWER (x 66..83, bez parteru i bryły), cegła z białymi plamami i migotanie
/// (okładzina w odstępie 2 cm od elewacji z zestawu nad bramą podwórka x 6..18). Nie ma jej zastępcy w tym miejscu:
/// plac przy Tower/metrze zostaje otwarty (decyzja: „usunąć i zbudować budynek albo nic” → nic).
/// </summary>
public static class BraxRegionFacades
{
    public static void Apply(Transform cityRoot)
    {
        Krok("pierzeje ulicy od nowa", () => BraxUlicePierzeje.Apply(cityRoot));
        Krok("obraz w lobby BRAXHORN TOWER", () => LobbyPicture(cityRoot));
    }

    static void Krok(string nazwa, Action a)
    {
        try { a(); BraxRegion.Note("ulice: " + nazwa + ": OK"); }
        catch (Exception e) { BraxRegion.Note("ulice: " + nazwa + ": BŁĄD " + e.Message); Debug.LogError("BRAXHORN_ULICE " + nazwa + ": " + e); }
    }

    // Obraz właściciela (mural Brax) w ramie na ścianie lobby BRAXHORN TOWER (decyzja 31.10, 17:05: „pusto, tylko recepcja”).
    static void LobbyPicture(Transform root)
    {
        const string path = "Assets/CityRebuild/Brand/nutrition-mural.jpg";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!tex) { BraxRegion.Note("lobby: brak obrazu " + path); return; }
        Physics.SyncTransforms();
        var from = new Vector3(59f, 2.0f, 44f); RaycastHit hit; Vector3 at = Vector3.zero, normal = Vector3.zero; float best = 99f;
        foreach (var d in new[] { Vector3.forward, Vector3.left, Vector3.right })
            if (Physics.Raycast(from, d, out hit, 14f, ~(1 << 8), QueryTriggerInteraction.Ignore) && hit.distance < best && Mathf.Abs(hit.normal.y) < .3f) { best = hit.distance; at = hit.point; normal = hit.normal; }
        if (best > 50f) { BraxRegion.Note("lobby: nie znaleziono ściany na obraz"); return; }
        float w = 3.2f, h = w * tex.height / tex.width;
        var frameM = BraxCityMaterials.Surface("Lobby: rama obrazu", new Color(.12f, .11f, .1f), .5f, .2f);
        var canvas = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Lobby: obraz Brax" };
        canvas.SetTexture("_MainTex", tex); canvas.SetColor("_Color", Color.white); canvas.SetFloat("_Roughness", .85f); canvas.SetFloat("_Metallic", 0f);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials"); BraxZapisAssetu.Utworz(canvas, BraxRegion.Out + "/Materials/lobby-picture.mat");
        var rot = Quaternion.LookRotation(-normal, Vector3.up);
        var g = new GameObject("Lobby: obraz Brax w ramie"); g.transform.SetParent(root, false);
        g.transform.position = at + normal * .06f + Vector3.up * (2.1f - 2.0f); g.transform.rotation = rot;
        BraxCityArchitecture.Box("rama", g.transform, new Vector3(0, 0, -.03f), new Vector3(w + .16f, h + .16f, .06f), frameM, false);
        var pic = BraxCityArchitecture.Box("płótno", g.transform, new Vector3(0, 0, .005f), new Vector3(w, h, .01f), canvas, false, Mathf.Max(w, h));
        var mf = pic.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {   // UV płótna: cała tekstura na przedniej ścianie
            var m = mf.sharedMesh; var v = m.vertices; var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++) uv[i] = new Vector2(Mathf.InverseLerp(-w * .5f, w * .5f, v[i].x), Mathf.InverseLerp(-h * .5f, h * .5f, v[i].y));
            m.uv = uv;
        }
        BraxRegion.Note("lobby: obraz " + w.ToString("F1") + "×" + h.ToString("F1") + " m na ścianie " + at);
    }
}
