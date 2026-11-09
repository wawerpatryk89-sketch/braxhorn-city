using UnityEditor;
using UnityEngine;

// Interior irradiance for CityStaticPBR materials (owner photos 9.11.2026: mauve walls, dark brown ceilings).
// CityStaticPBR has no GI on mobile: away from the lamps an indoor surface takes the city sky (.60,.68,.78)
// and city ground (.42,.40,.36) x .65 as its only light, so a beige wall reads mauve and a white ceiling
// dark grey-brown. Indoors the lower hemisphere is the lit floor and the upper one the white ceiling:
// interior-only materials get a warm interior sky/ground (per material, as the nutrition shop does).
// Sun, lamps and exterior materials are unchanged.
public static class BraxChampionsInteriorLight
{
    public static readonly Color Sky=new Color(.86f,.85f,.81f),Ground=new Color(.80f,.77f,.72f);
    public const float Walls=.70f,Ceilings=.85f;
    public static void Apply(Material m,float ambient)
    {
        if(!m)return;
        m.SetColor("_SkyColor",Sky);m.SetColor("_GroundColor",Ground);m.SetFloat("_AmbientIntensity",ambient);
        EditorUtility.SetDirty(m);
    }
    // Shaded façades and soffits: the shader's ground irradiance (.42,.40,.36) is darker than a sunlit lawn and
    // paving, so white render in shade read dark blue-grey/lavender (owner photos 9.11). Façade materials of the
    // Mistrzowie buildings take the real bounce of a sunlit ground; the sky stays the city sky.
    public static readonly Color FacadeGround=new Color(.60f,.57f,.50f);
    public const float FacadeAmbient=.80f;
    public static void Facade(Material m)
    {
        if(!m)return;
        m.SetColor("_GroundColor",FacadeGround);m.SetFloat("_AmbientIntensity",FacadeAmbient);
        EditorUtility.SetDirty(m);
    }
}
