using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

internal static class BraxGymProfiles
{
    [Serializable]
    private sealed class Catalog { public Profile[] profiles; }
    [Serializable]
    private sealed class Profile { public string id; public string[] exercises; }

    internal const string ResidentClips = "Assets/Source/Prywatne/Gym/";
    internal const string PlayerClips = "Assets/Source/Prywatne/GymPatryk/";

    internal static string[] For(string characterId)
    {
        string path = Path.Combine(Application.dataPath, "../../Postacie/silownia/gym.json");
        var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
        var profile = catalog.profiles.SingleOrDefault(p => p.id == characterId);
        if (profile == null) return Array.Empty<string>();
        var ids = profile.exercises;
        if (ids == null || ids.Length != 3 || new HashSet<string>(ids).Count != 3)
            throw new InvalidDataException("Profil siłowni wymaga trzech różnych ćwiczeń: " + characterId);
        return ids;
    }
}
