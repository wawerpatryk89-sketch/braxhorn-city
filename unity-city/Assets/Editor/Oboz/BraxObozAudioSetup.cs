using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxObozAudioSetup
{
    public const string Source="Assets/Source/Prywatne/Oboz/audio/";
    [Serializable] sealed class Catalog { public string engine; public bool paidApiUsed,qualityApproved; public Entry[] clips; }
    [Serializable] sealed class Entry { public string id,voice,file; }
    public static void Configure(BraxObozSceneInfo scene,BraxObozTrainingRoutine routine)
    {
        var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Source+"dialogue.json"));
        if(catalog.engine!="piper-tts==1.3.0"||catalog.paidApiUsed||catalog.clips.Length!=47)
            throw new InvalidOperationException("Nieprawidłowy katalog wspólnego Pipera");
        var audio=scene.gameObject.AddComponent<BraxObozCampAudio>();audio.routine=routine;routine.audio=audio;
        audio.lines=catalog.clips.Select(entry=>{
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Source+entry.file);
            if(!clip||clip.length<.4f||clip.channels!=1)throw new InvalidOperationException("Brak poprawnego nagrania: "+entry.file);
            return new BraxObozCampAudio.Line{id=entry.id,voice=entry.voice,clip=clip};
        }).ToArray();
        foreach(var npc in routine.participants)
            npc.gameObject.AddComponent<BraxObozContactAudio>().player=scene.player;
        scene.player.gameObject.AddComponent<BraxObozContactAudio>().player=scene.player;
        Debug.Log("BRAXHORN_OBOZ_AUDIO engine=piper_local clips=47 bubbles=false quality_approved=false");
    }
}

public sealed class BraxObozVoiceImport : AssetPostprocessor
{
    public override uint GetVersion()=>1;
    void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith(BraxObozAudioSetup.Source,StringComparison.Ordinal))return;
        var importer=(AudioImporter)assetImporter;importer.forceToMono=true;
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.9f;
        settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData=true;importer.defaultSampleSettings=settings;
        importer.ClearSampleSettingOverride("Android");
    }
}
