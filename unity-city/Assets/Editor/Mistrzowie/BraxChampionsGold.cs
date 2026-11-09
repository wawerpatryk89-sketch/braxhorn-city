using UnityEditor;
using UnityEngine;

// Owner 10.11 00:36-00:37: "Ten dolar ma być złoty, świecący się. Jasne złoto, żółte takie" and, with a photo of
// fine-gold bars, "Wszystko, co złote, ma być złote. Już ci pokażę, jak wygląda złoto."
// The region's golds were "champagne" tints on a fully metallic surface. CityStaticPBR has no reflection probe: a
// full metal has no diffuse colour and only mirrors the broad sky x .4, so every gold read brown or olive.
// One gold for the whole region, taken from the owner's photo: saturated yellow, half-metallic (it keeps its own
// colour in sun and in shade), polished, with a faint glow of its own so it shines even on a wall the sun never reaches.
public static class BraxChampionsGold
{
    public static readonly Color Colour=new Color(1f,.80f,.20f), Deep=new Color(.93f,.68f,.13f);
    public static readonly Color Glow=new Color(.34f,.25f,.045f), DeepGlow=new Color(.22f,.15f,.025f);
    public const float Metallic=.55f, Roughness=.20f;
    // deep: the returns / shadow faces of solid gold letters, a shade darker so the bevel still reads
    public static Material Apply(Material m,bool deep=false)
    {
        if(!m)return m;
        m.SetColor("_Color",deep?Deep:Colour);
        m.SetFloat("_Metallic",Metallic);
        m.SetFloat("_Roughness",Roughness);
        m.SetColor("_EmissionColor",deep?DeepGlow:Glow);
        EditorUtility.SetDirty(m);
        return m;
    }
}
