using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Camp lighting only. Original PBR images and the Montorfano source are kept.
public static class BraxObozAtmosphere
{
    public static readonly Color Burgundy=new Color32(0x7A,0x1E,0x1E,0xFF);
    public static readonly Color WoodReference=new Color32(0x5A,0x3A,0x22,0xFF);
    public static readonly Color MudReference=new Color32(0x4A,0x37,0x26,0xFF);
    public static readonly Color GrassReference=new Color32(0x4E,0x8A,0x2E,0xFF);
    public static readonly Color CanopyReference=new Color32(0x1F,0x4D,0x2B,0xFF);

    public static void Apply(Transform camp,Light sun)
    {
        // Low warm afternoon sun. It is the only source casting shadows.
        sun.type=LightType.Directional;
        sun.transform.rotation=Quaternion.Euler(27f,-35f,0);
        sun.useColorTemperature=true;sun.colorTemperature=4800f;
        sun.color=Color.white;sun.intensity=.93f;
        sun.renderMode=LightRenderMode.ForcePixel;sun.shadows=LightShadows.Soft;
        RenderSettings.sun=sun;
        RenderSettings.ambientMode=AmbientMode.Skybox;
        RenderSettings.ambientIntensity=.8f;
        RenderSettings.reflectionIntensity=.85f;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
        RenderSettings.fogDensity=.0025f;
        RenderSettings.fogColor=new Color(.43f,.49f,.43f);
        var local=camp.GetComponentsInChildren<Light>(true)
            .Where(l=>l&&l!=sun).ToArray();
        foreach(var light in local)
        {
            light.shadows=LightShadows.None;
            light.renderMode=LightRenderMode.ForceVertex;
        }
        Debug.Log("BRAXHORN_OBOZ_ATMOSPHERE sunKelvin=4800 sunElevation=27"+
            " localVertexLights="+local.Length+" localShadows=0 fog=0.0025"+
            " originalTextureColoursPreserved=true phoneReview=pending");
    }
}
