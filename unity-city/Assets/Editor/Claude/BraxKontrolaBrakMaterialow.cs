using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// KONTROLA KOŃCOWA: RÓŻOWE OBIEKTY (C1, właściciel 2.11 — jachty, motorówka, skutery, drzewa przy plaży, budka ratownika,
// żurawie portu, przedmioty przy arenie były magenta). Magenta = renderer z materiałem NULL (plik materiału usunięty /
// nadpisany po przypisaniu) albo materiał bez shadera / z shaderem błędu (Hidden/InternalErrorShader).
//   1) Pamiec(scena) — przed zapisem sceny miasta: wszystkie renderery w pamięci (ścieżki w hierarchii).
//   2) Pliki(sceny) — po podziale na obszary: zapisane pliki scen (.unity, tekst YAML) — odwołania m_Materials do plików,
//      których nie ma, do materiałów, których nie ma w pliku modelu, i do materiałów bez shadera. Bez otwierania scen
//      (miasto ma setki MB — drugi raz w pamięci = ryzyko braku RAM w CI).
// Wynik: BRAXHORN_MATERIALY_BRAK etap=… liczba=… (cel 0) [przykłady]. Nie przerywa buildu.
public static class BraxKontrolaBrakMaterialow
{
    const string BladShadera = "Hidden/InternalErrorShader";

    static string Sciezka(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static string Zly(Material m)
    {
        if (!m) return "materiał NULL";
        if (!m.shader) return "materiał „" + m.name + "” bez shadera";
        if (m.shader.name == BladShadera) return "materiał „" + m.name + "” z shaderem błędu";
        return null;
    }

    public static int Pamiec(Scene scena, string etap)
    {
        int renderery = 0, zle = 0, wylaczone = 0; var przyklady = new List<string>(); var powody = new Dictionary<string, int>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r) continue;
                renderery++;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string z = Zly(mats[i]); if (z == null) continue;
                    zle++; if (!r.enabled || !r.gameObject.activeInHierarchy) wylaczone++;
                    string klucz = z.StartsWith("materiał NULL") ? "null" : z.Contains("bez shadera") ? "bez_shadera" : "shader_bledu";
                    powody[klucz] = powody.TryGetValue(klucz, out var n) ? n + 1 : 1;
                    if (przyklady.Count < 30) przyklady.Add(Sciezka(r.transform) + " [slot " + i + "] " + z + (r.enabled && r.gameObject.activeInHierarchy ? "" : " (wyłączony)") + " @" + r.bounds.center.ToString("F0"));
                }
            }
        Debug.Log("BRAXHORN_MATERIALY_BRAK etap=" + etap + " liczba=" + zle + " (cel 0) renderery=" + renderery + " w_wylaczonych=" + wylaczone +
                  " powody=" + string.Join(",", powody.Select(kv => kv.Key + ":" + kv.Value)) +
                  " zapis_assetow: " + BraxZapisAssetu.Podsumowanie() +
                  (przyklady.Count > 0 ? " [" + string.Join(" | ", przyklady) + "]" : ""));
        return zle;
    }

    // ---------------- pliki scen ----------------
    static readonly Dictionary<string, Dictionary<long, string>> plikiModeli = new Dictionary<string, Dictionary<long, string>>();   // guid → (fileID → null = dobry, tekst = zły)

    static string SprawdzOdwolanie(long fileId, string guid)
    {
        if (guid == "0000000000000000e000000000000000" || guid == "0000000000000000f000000000000000") return null;   // wbudowane
        if (!plikiModeli.TryGetValue(guid, out var mapa))
        {
            mapa = new Dictionary<long, string>(); plikiModeli[guid] = mapa;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { mapa[long.MinValue] = "plik materiału nie istnieje (guid " + guid + ")"; return mapa[long.MinValue]; }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var m = o as Material; if (!m) continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string g, out long lid)) mapa[lid] = Zly(m) is string z ? z + " (" + path + ")" : null;
            }
            mapa[long.MaxValue] = path;
        }
        if (mapa.TryGetValue(long.MinValue, out var brakPliku)) return brakPliku;
        if (mapa.TryGetValue(fileId, out var wynik)) return wynik;
        return "w pliku " + mapa[long.MaxValue] + " nie ma materiału " + fileId;
    }

    static long Liczba(string s, string klucz)
    {
        int i = s.IndexOf(klucz, StringComparison.Ordinal); if (i < 0) return 0;
        i += klucz.Length; int j = i; while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '-')) j++;
        long.TryParse(s.Substring(i, j - i), out long v); return v;
    }
    static string Guid(string s)
    {
        int i = s.IndexOf("guid: ", StringComparison.Ordinal); if (i < 0) return null;
        i += 6; int j = i; while (j < s.Length && Uri.IsHexDigit(s[j])) j++;
        return s.Substring(i, j - i);
    }

    public static int Pliki(IEnumerable<string> sceny, string etap)
    {
        int zle = 0, renderery = 0, plikow = 0; var przyklady = new List<string>();
        foreach (var scena in sceny)
        {
            if (string.IsNullOrEmpty(scena) || !File.Exists(scena)) continue;
            plikow++;
            var obiekty = new HashSet<long>(); var nazwy = new Dictionary<long, string>();
            var lokalneZle = new Dictionary<long, string>();   // materiały zapisane w scenie bez shadera
            var odwolania = new List<(long go, int slot, long fid, string guid)>();
            int klasa = 0; long id = 0, go = 0; bool wMat = false; int slot = 0;
            using (var rd = new StreamReader(scena, Encoding.UTF8, false, 1 << 20))
            {
                string l;
                while ((l = rd.ReadLine()) != null)
                {
                    if (l.StartsWith("--- !u!", StringComparison.Ordinal))
                    {
                        wMat = false; int amp = l.IndexOf(" &", StringComparison.Ordinal);
                        int.TryParse(l.Substring(7, (amp > 7 ? amp : l.Length) - 7), out klasa);
                        id = amp > 0 ? Liczba(l, " &") : 0; obiekty.Add(id); go = 0;
                        continue;
                    }
                    if (wMat)
                    {
                        if (l.StartsWith("  - {", StringComparison.Ordinal)) { odwolania.Add((go, slot++, Liczba(l, "fileID: "), Guid(l))); continue; }
                        wMat = false;
                    }
                    if (l.StartsWith("  m_GameObject: ", StringComparison.Ordinal)) { go = Liczba(l, "fileID: "); continue; }
                    if (klasa == 1 && l.StartsWith("  m_Name: ", StringComparison.Ordinal)) { nazwy[id] = l.Substring(10); continue; }
                    if (klasa == 21 && l.StartsWith("  m_Shader: ", StringComparison.Ordinal))
                    {
                        long sf = Liczba(l, "fileID: "); string sg = Guid(l);
                        if (sf == 0) lokalneZle[id] = "materiał w scenie bez shadera";
                        else if (sg != null && sg != "0000000000000000e000000000000000" && sg != "0000000000000000f000000000000000" && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(sg)))
                            lokalneZle[id] = "materiał w scenie — plik shadera nie istnieje";
                        continue;
                    }
                    if (l.StartsWith("  m_Materials:", StringComparison.Ordinal)) { renderery++; slot = 0; wMat = !l.EndsWith("[]"); continue; }
                }
            }
            foreach (var o in odwolania)
            {
                string z;
                if (o.fid == 0) z = "materiał NULL";
                else if (string.IsNullOrEmpty(o.guid)) z = obiekty.Contains(o.fid) ? (lokalneZle.TryGetValue(o.fid, out var lz) ? lz : null) : "materiał w scenie nie istnieje";
                else z = SprawdzOdwolanie(o.fid, o.guid);
                if (z == null) continue;
                zle++;
                if (przyklady.Count < 30) przyklady.Add(Path.GetFileNameWithoutExtension(scena) + ": „" + (nazwy.TryGetValue(o.go, out var n) ? n : "?") + "” [slot " + o.slot + "] " + z);
            }
        }
        Debug.Log("BRAXHORN_MATERIALY_BRAK etap=" + etap + " liczba=" + zle + " (cel 0) renderery=" + renderery + " sceny=" + plikow +
                  (przyklady.Count > 0 ? " [" + string.Join(" | ", przyklady) + "]" : ""));
        plikiModeli.Clear();
        return zle;
    }
}
