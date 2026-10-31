using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// BRAXHORN R2 — rozmiar paczki liczony z raportu buildu (nie na oko): po zbudowaniu playera zapisuje
// city-review/osiedle-rozmiary.json: pliki wyjściowe (bajty), zasoby spakowane zsumowane per kategoria
// (tekstury osiedla per zestaw, modele, siatki/materiały osiedla, kopia siłowni, reszta) i 80 największych.
// Tylko odczyt raportu; nic nie zmienia w buildzie.
public class BraxClaudeSizeReport : IPostprocessBuildWithReport
{
    public int callbackOrder => 10000;

    static string Cat(string p)
    {
        if (string.IsNullOrEmpty(p)) return "(wbudowane)";
        const string T = "Assets/Source/Claude/Osiedle/textures/", M = "Assets/Source/Claude/Osiedle/models/";
        if (p.StartsWith(T)) return "osiedle tekstury: " + p.Substring(T.Length).Split('/')[0];
        if (p.StartsWith(M)) return "osiedle modele: " + p.Substring(M.Length).Split('/')[0];
        if (p.StartsWith("Assets/PlayableGenerated/ClaudeOsiedle")) return "osiedle: siatki i materiały wygenerowane";
        if (p.Contains("braxhorn-original-gym")) return "siłownia (oryginał / kopia)";
        var parts = p.Split('/');
        return string.Join("/", parts.Take(Math.Min(3, parts.Length - 1)));
    }

    static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

    public void OnPostprocessBuild(BuildReport report)
    {
        try
        {
            var rows = new List<(string path, ulong size, string type)>();
            foreach (var pa in report.packedAssets)
                foreach (var c in pa.contents)
                    rows.Add((c.sourceAssetPath, c.packedSize, c.type != null ? c.type.Name : "?"));
            var byCat = rows.GroupBy(r => Cat(r.path)).Select(g => (cat: g.Key, size: g.Aggregate(0UL, (a, r) => a + r.size), n: g.Count()))
                            .OrderByDescending(x => x.size).ToList();
            var byPath = rows.GroupBy(r => r.path ?? "").Select(g => (path: g.Key, size: g.Aggregate(0UL, (a, r) => a + r.size), type: g.First().type))
                             .OrderByDescending(x => x.size).Take(80).ToList();
            var files = new List<(string f, long b)>();
            string outDir = report.summary.outputPath;
            if (Directory.Exists(outDir))
                foreach (var f in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories)) files.Add((f.Substring(outDir.Length).TrimStart('/', '\\'), new FileInfo(f).Length));
            ulong packed = rows.Aggregate(0UL, (a, r) => a + r.size);
            long outBytes = files.Sum(f => f.b);
            var sb = new StringBuilder();
            sb.Append("{\n  \"wynik\": \"").Append(report.summary.result).Append("\",\n");
            sb.Append("  \"plikiWyjscioweMB\": ").Append((outBytes / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"zasobySpakowaneMB\": ").Append((packed / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"pliki\": [\n").Append(string.Join(",\n", files.OrderByDescending(f => f.b).Take(30).Select(f => "    {\"plik\": \"" + Esc(f.f) + "\", \"MB\": " + (f.b / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "}"))).Append("\n  ],\n");
            sb.Append("  \"kategorie\": [\n").Append(string.Join(",\n", byCat.Select(c => "    {\"kategoria\": \"" + Esc(c.cat) + "\", \"MB\": " + (c.size / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ", \"obiekty\": " + c.n + "}"))).Append("\n  ],\n");
            sb.Append("  \"najwieksze\": [\n").Append(string.Join(",\n", byPath.Select(c => "    {\"zasob\": \"" + Esc(c.path) + "\", \"typ\": \"" + Esc(c.type) + "\", \"MB\": " + (c.size / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "}"))).Append("\n  ]\n}\n");
            Directory.CreateDirectory("city-review");
            File.WriteAllText("city-review/osiedle-rozmiary.json", sb.ToString());
            Debug.Log("BRAXHORN_SIZE pliki wyjściowe " + (outBytes / 1048576.0).ToString("0.0") + " MB, zasoby spakowane " + (packed / 1048576.0).ToString("0.0") + " MB, kategorii " + byCat.Count
                      + (rows.Count == 0 ? " (raport bez listy zasobów)" : ""));
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_SIZE nie udało się: " + e.Message); }
    }
}
