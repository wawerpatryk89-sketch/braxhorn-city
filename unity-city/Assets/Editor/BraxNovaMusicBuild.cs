using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class BraxNovaMusicBuild
{
    public static void Attach(BraxGymPlayer player)
    {
        if (!player || !player.cityWorld || player.cityWorld.area != "city" ||
            !player.cityWorld.points.Any(p => p.id == "club_dance"))
            throw new InvalidOperationException("NOVA audio requires the actual city and its original club anchor");
        const string path = "Assets/StreamingAssets/NOVA/IndustrialPulseClub.mp3";
        if (!File.Exists(path) || new FileInfo(path).Length < 100000)
            throw new InvalidOperationException("Owner-approved trimmed Industrial Pulse is missing: " + path);
        if (UnityEngine.Object.FindObjectsByType<BraxNovaMusic>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Duplicate NOVA music controller");
        var anchor = player.cityWorld.points.Single(p => p.id == "club_dance");
        var speakers = new GameObject("NOVA approved Industrial Pulse venue audio");
        speakers.transform.position = anchor.position + Vector3.up * 2.6f;
        var source = speakers.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = true; source.volume = 0;
        source.spatialBlend = .65f; source.dopplerLevel = 0;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 6; source.maxDistance = 25;
        var music = player.gameObject.AddComponent<BraxNovaMusic>();
        music.player = player; music.venueSource = source;
        // Coordinates are Unity metres, from the preserved club floor/entry.
        // The doorstep and nearby street receive a quiet doorway spill;
        // Nutrition, Tower and the basement are not in the music zones.
        music.hall = new Bounds(new Vector3(43.5f, 1.6f, 48.5f), new Vector3(25, 3.8f, 15));
        music.vestibule = new Bounds(new Vector3(42, 1.6f, 39.5f), new Vector3(4.6f, 3.8f, 7));
        music.doorway = new Vector3(42, .15f, 36.08f);
        music.entranceOutside = new Bounds(new Vector3(42, 1.6f, 29.04f), new Vector3(28, 3.8f, 14.08f));
        Debug.Log("NOVA_APPROVED_OWNER_MUSIC_ATTACHED originalPlayerSourceUnchanged=true automaticDoorwayApproach=true continuousParty=true");
    }
}
