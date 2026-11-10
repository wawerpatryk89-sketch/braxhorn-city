// Stand-ins for the editor API used only by the casino T11 generator.
using System;
using System.Collections.Generic;
using UnityEngine;
public static class BraxCityMaterials { public const string ShaderName = "Braxhorn/CityStaticPBR"; public static readonly Vector4 SunDirection = new Vector4(.45f, .78f, -.42f, 0); }
public static class BraxChampionsCasinoPbr2K
{
    public static Material Create(string output, string id, string slug, float metres, Color tint)
        => new Material { name = id + "|" + slug + "|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true, roughness = .6f };
}
