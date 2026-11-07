using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable] public sealed class BraxContentFile
{
    public string name, sha256;
    public long bytes;
}

[Serializable] public sealed class BraxContentManifest
{
    public int format = 1;
    public string zone, baseId, runtimeId, version, catalog, firstScene;
    public bool externalBase;
    public string[] scenes;
    public BraxContentFile[] files;
}

// The package is cumulative from its immutable APK baseline. SHA-256 detects
// corruption, not publisher identity: import only files from the project owner.
public static class BraxContentPackage
{
    public const long MaxBytes = 8L * 1024 * 1024 * 1024;
    static readonly Regex Name = new Regex(@"^[a-zA-Z0-9][a-zA-Z0-9_.-]{0,180}$");
    static readonly Regex Hash = new Regex("^[a-f0-9]{64}$");

    public static string Sha(string path)
    {
        using (var input = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }

    public static void Validate(BraxContentManifest m)
    {
        if (m == null || m.format != 1 || !Safe(m.zone) || !Safe(m.baseId) ||
            !Safe(m.version) || m.runtimeId == null || !Hash.IsMatch(m.runtimeId) ||
            !Safe(m.catalog) || !(m.catalog.EndsWith(".json", StringComparison.Ordinal) || m.catalog.EndsWith(".bin", StringComparison.Ordinal)) ||
            m.files == null || m.files.Length == 0 || m.files.Length > 50000 ||
            m.scenes == null || m.scenes.Length == 0 || !m.scenes.Contains(m.firstScene) ||
            m.scenes.Any(s => !Safe(s)) || m.scenes.Distinct().Count() != m.scenes.Length)
            throw new InvalidDataException("Nieprawidłowy manifest aktualizacji.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var f in m.files)
        {
            if (f == null || !Safe(f.name) || f.name == "manifest.json" ||
                !(f.name.EndsWith(".bundle", StringComparison.Ordinal) || f.name == m.catalog || f.name == Path.ChangeExtension(m.catalog, ".hash")) ||
                !names.Add(f.name) || f.sha256 == null || !Hash.IsMatch(f.sha256) || f.bytes < 0 || f.bytes > MaxBytes)
                throw new InvalidDataException("Nieprawidłowy plik w manifeście.");
            total = checked(total + f.bytes);
            if (total > MaxBytes) throw new InvalidDataException("Paczka przekracza limit 8 GiB.");
        }
        if (!names.Contains(m.catalog)) throw new InvalidDataException("Brak katalogu scen.");
    }

    public static bool Safe(string value) => value != null && Name.IsMatch(value) && !value.Contains("..");
    public static bool Same(BraxContentFile a, BraxContentFile b) => a != null && b != null && a.name == b.name && a.sha256 == b.sha256 && a.bytes == b.bytes;

    public static void Compatible(BraxContentManifest basis, BraxContentManifest update)
    {
        Validate(basis); Validate(update);
        if (basis.zone != update.zone || basis.baseId != update.baseId || basis.runtimeId != update.runtimeId ||
            basis.firstScene != update.firstScene || basis.externalBase != update.externalBase)
            throw new InvalidDataException("Ta aktualizacja wymaga innej bazy APK lub należy do innej strefy.");
    }

    public static BraxContentManifest ReadZipManifest(string zip)
    {
        using (var archive = ZipFile.OpenRead(zip))
        {
            var entries = archive.Entries.Where(e => e.FullName == "manifest.json").ToArray();
            if (entries.Length != 1 || entries[0].Length > 8 * 1024 * 1024) throw new InvalidDataException("Brak lub za duży manifest.");
            using (var reader = new StreamReader(entries[0].Open()))
            {
                var m = JsonUtility.FromJson<BraxContentManifest>(reader.ReadToEnd());
                Validate(m); return m;
            }
        }
    }

    public static void ExtractBase(string zip, string staging, BraxContentManifest basis)
    {
        Validate(basis);
        var m = ReadZipManifest(zip);
        Compatible(basis, m);
        if (!basis.externalBase || m.version != "base")
            throw new InvalidDataException("To nie jest zewnętrzna bazowa paczka BRAXHORN.");
        if (Directory.Exists(staging)) throw new IOException("Katalog docelowy już istnieje.");
        var expected = m.files.ToDictionary(f => f.name);
        Directory.CreateDirectory(staging);
        try
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in archive.Entries)
                {
                    if (!Safe(entry.FullName) || !seen.Add(entry.FullName))
                        throw new InvalidDataException("Niedozwolona ścieżka lub duplikat w bazowej paczce.");
                    if (entry.FullName == "manifest.json") continue;
                    if (!expected.TryGetValue(entry.FullName, out var f) || entry.Length != f.bytes)
                        throw new InvalidDataException("Plik bazowy spoza manifestu lub niezgodny rozmiar.");
                    string path = Path.Combine(staging, f.name);
                    using (var input = entry.Open())
                    using (var output = new FileStream(path, FileMode.CreateNew))
                    {
                        var buffer = new byte[131072]; long count = 0; int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            count += read;
                            if (count > f.bytes) throw new InvalidDataException("Nieprawidłowy rozmiar pliku bazowego.");
                            output.Write(buffer, 0, read);
                        }
                        if (count != f.bytes) throw new InvalidDataException("Przerwany plik bazowy.");
                    }
                    if (Sha(path) != f.sha256) throw new InvalidDataException("Błędna suma SHA-256 bazy: " + f.name);
                }
                foreach (var f in m.files)
                    if (!seen.Contains(f.name)) throw new InvalidDataException("Brakuje pliku bazowego: " + f.name);
            }
            File.WriteAllText(Path.Combine(staging, "manifest.json"), JsonUtility.ToJson(m, true));
        }
        catch { Directory.Delete(staging, true); throw; }
    }

    // The caller uses a new staging directory. Nothing in the active version is
    // overwritten; interruption can only leave an unreferenced staging directory.
    public static void Extract(string zip, string staging, BraxContentManifest basis, BraxContentManifest m)
    {
        Compatible(basis, m);
        if (Directory.Exists(staging)) throw new IOException("Katalog docelowy już istnieje.");
        var baseline = basis.files.ToDictionary(f => f.name);
        var expected = m.files.ToDictionary(f => f.name);
        Directory.CreateDirectory(staging);
        try
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in archive.Entries)
                {
                    if (!Safe(entry.FullName) || !seen.Add(entry.FullName)) throw new InvalidDataException("Niedozwolona ścieżka lub duplikat w ZIP.");
                    if (entry.FullName == "manifest.json") continue;
                    if (!expected.TryGetValue(entry.FullName, out var f) || entry.Length != f.bytes)
                        throw new InvalidDataException("Plik spoza manifestu lub niezgodny rozmiar.");
                    string path = Path.Combine(staging, f.name);
                    using (var input = entry.Open())
                    using (var output = new FileStream(path, FileMode.CreateNew))
                    {
                        var buffer = new byte[131072]; long count = 0; int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            count += read;
                            if (count > f.bytes) throw new InvalidDataException("Nieprawidłowy rozmiar po rozpakowaniu.");
                            output.Write(buffer, 0, read);
                        }
                        if (count != f.bytes) throw new InvalidDataException("Przerwany plik.");
                    }
                    if (Sha(path) != f.sha256) throw new InvalidDataException("Błędna suma SHA-256: " + f.name);
                }
                foreach (var f in m.files)
                    if (!seen.Contains(f.name) && (f.name == m.catalog || !baseline.TryGetValue(f.name, out var b) || !Same(f, b)))
                        throw new InvalidDataException("Brakuje zmienionego pliku: " + f.name);
            }
            File.WriteAllText(Path.Combine(staging, "manifest.json"), JsonUtility.ToJson(m, true));
        }
        catch { Directory.Delete(staging, true); throw; }
    }
}
