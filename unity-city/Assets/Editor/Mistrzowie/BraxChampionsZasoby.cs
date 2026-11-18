using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>18.11.2026: #47 and #49 lost their runner mid-generation with nothing in the log. In a batch-mode build this
/// logs, every two minutes from a background timer, the machine's available memory and free swap (/proc/meminfo, the
/// host's inside the container), the project disk's free space and Unity's own resident memory - one line
/// "BRAX_RES ...", so the last lines before any death say what ran out. Off in the interactive editor.</summary>
[InitializeOnLoad]
static class BraxChampionsZasoby
{
    static System.Threading.Timer timer;

    static BraxChampionsZasoby()
    {
        if (!Application.isBatchMode) return;
        try
        {
            timer = new System.Threading.Timer(_ => Log(), null, 0, 120000);
            AssemblyReloadEvents.beforeAssemblyReload += () => { try { timer?.Dispose(); timer = null; } catch { } };
            EditorApplication.quitting += () => { try { timer?.Dispose(); timer = null; } catch { } };
        }
        catch (Exception e) { Debug.Log("BRAX_RES off: " + e.Message); }
    }

    static void Log()
    {
        try
        {
            string mem = "?", swap = "?";
            if (File.Exists("/proc/meminfo"))
                foreach (var l in File.ReadAllLines("/proc/meminfo"))
                {
                    if (l.StartsWith("MemAvailable:")) mem = l.Substring(13).Trim();
                    else if (l.StartsWith("SwapFree:")) swap = l.Substring(9).Trim();
                }
            long disk = new DriveInfo(Path.GetFullPath(".")).AvailableFreeSpace / 1073741824L;
            long rss = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1048576L;
            Debug.Log("BRAX_RES " + DateTime.UtcNow.ToString("HH:mm:ss") + " mem_avail=" + mem + " swap_free=" + swap + " disk_free=" + disk + "G unity_rss=" + rss + "M");
        }
        catch (Exception e) { Debug.Log("BRAX_RES n/a: " + e.Message); }
    }
}
