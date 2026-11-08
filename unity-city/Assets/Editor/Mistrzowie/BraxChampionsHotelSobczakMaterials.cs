using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// HOTEL SOBCZAK T03 — one authoritative material set.
// Poly Haven source maps live in Assets/Source/Mistrzowie/Textures and are
// preserved at 4K; BraxChampionsTextureImport keeps Android runtime copies at
// max 2K ASTC 4x4 with mipmaps/streaming.
public static class BraxChampionsHotelSobczakMaterials
{
    public sealed class Set
    {
        public Material facade,sandstone,stucco,roof,marble,oak,parquet,terrazzo,rubber,carpet,tile,
            granite,graniteOutdoor,antiSlip,garage,concrete,leather,leatherWhite,asphalt,
            brass,darkBronze,steel,glass,whitePaint,parkingPaint,warmLight,carpetRed,water,foliage;
    }

    const string Generated="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    static readonly string[] RequiredSlugs={
        "beige_wall_001","sandstone_blocks_08","white_stucco_02","roof_slates_03","marble_01",
        "oak_wood_planks","herringbone_parquet","terrazzo_tiles","rubber_tiles","poly_wool_herringbone",
        "tiled_floor_001","granite_tile_04","anti_slip_concrete","garage_floor",
        "concrete_wall_003","brown_leather","leather_white","clean_asphalt"
    };

    public static void VerifySources()
    {
        string source="Assets/Source/Mistrzowie/Textures";
        foreach(string slug in RequiredSlugs)
        {
            foreach(string suffix in new[]{"diff_4k.jpg","nor_gl_4k.exr","arm_4k.png"})
            {
                string p=source+"/"+slug+"_"+suffix;
                if(!File.Exists(p))throw new FileNotFoundException(
                    "HOTEL SOBCZAK requires official Poly Haven source prepared by prepare_assets.py: "+p,p);
            }
        }
    }

    public static Set Create()
    {
        VerifySources();
        Directory.CreateDirectory(Generated+"/Materials");
        var s=new Set();
        s.facade=P("facade_beige","beige_wall_001",3.0f,new Color(.97f,.91f,.80f));
        s.sandstone=P("plinth_sandstone","sandstone_blocks_08",3.0f,new Color(.98f,.93f,.82f));
        s.stucco=P("architectural_stucco","white_stucco_02",1.8f,new Color(1f,.96f,.86f));
        s.roof=P("mansard_slates","roof_slates_03",3.0f,new Color(.42f,.44f,.47f));
        s.marble=P("cream_marble","marble_01",1.5f,new Color(1f,.97f,.89f));
        s.oak=P("varnished_oak","oak_wood_planks",1.2f,new Color(.92f,.78f,.58f));
        s.parquet=P("herringbone_parquet","herringbone_parquet",3.4f,new Color(.91f,.78f,.58f));
        s.terrazzo=P("spa_terrazzo","terrazzo_tiles",2.0f,new Color(.96f,.91f,.82f));
        s.rubber=P("fitness_rubber","rubber_tiles",2.0f,new Color(.30f,.31f,.32f));
        s.carpet=P("corridor_wool","poly_wool_herringbone",.30f,new Color(.44f,.39f,.34f));
        s.tile=P("kitchen_tile","tiled_floor_001",1.5f,new Color(.87f,.81f,.72f));
        s.granite=P("interior_granite","granite_tile_04",2.0f,new Color(.86f,.84f,.79f));
        s.graniteOutdoor=P("exterior_granite","granite_tile_04",2.0f,new Color(.78f,.74f,.66f));
        s.antiSlip=P("ramp_antislip","anti_slip_concrete",2.5f,new Color(.66f,.66f,.64f));
        s.garage=P("parking_floor","garage_floor",3.0f,new Color(.82f,.82f,.80f));
        s.concrete=P("technical_concrete","concrete_wall_003",3.0f,new Color(.82f,.81f,.76f));
        s.leather=P("brown_leather","brown_leather",.30f,new Color(.52f,.30f,.18f));
        s.leatherWhite=P("spa_white_leather","leather_white",.30f,new Color(.98f,.97f,.94f));
        s.asphalt=P("driveway_asphalt","clean_asphalt",2.5f,new Color(.62f,.62f,.61f));

        s.brass=Surface("polished_brass",new Color(.93f,.70f,.30f),.19f,.98f,false,false);
        s.darkBronze=Surface("dark_bronze_frames",new Color(.12f,.095f,.07f),.24f,.90f,false,false);
        s.steel=Surface("brushed_stainless",new Color(.58f,.60f,.62f),.34f,1.0f,false,false);
        s.glass=Surface("clear_graphite_glass",new Color(.58f,.67f,.72f,.22f),.10f,0f,true,false);
        s.whitePaint=Surface("clean_white_paint",new Color(.96f,.955f,.93f),.72f,0f,false,false);
        s.parkingPaint=Surface("parking_marking",new Color(.97f,.97f,.91f),.72f,0f,false,false);
        s.warmLight=Surface("warm_architectural_light",new Color(.96f,.84f,.64f),.36f,0f,false,true);
        s.carpetRed=Surface("entrance_red_carpet",new Color(.46f,.035f,.045f),.78f,0f,false,false);
        s.water=Surface("fountain_water",new Color(.28f,.47f,.57f,.50f),.08f,0f,true,false);
        s.foliage=Surface("ornamental_foliage",new Color(.16f,.30f,.11f),.82f,0f,false,false);
        return s;
    }

    static Material P(string id,string slug,float metres,Color tint)
        =>BraxChampionsFoundation.SourceMaterial("sobczak_"+id,slug,metres,tint);

    static Material Surface(string id,Color color,float rough,float metal,bool transparent,bool emissive)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("HOTEL SOBCZAK requires CityStaticPBR shader");
        var fresh=new Material(shader){name="HOTEL SOBCZAK "+id,enableInstancing=true};
        fresh.SetColor("_Color",color);fresh.SetFloat("_Roughness",rough);fresh.SetFloat("_Metallic",metal);
        fresh.SetFloat("_VertexTint",0);fresh.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        if(transparent)
        {
            fresh.SetFloat("_Transparent",1);
            fresh.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            fresh.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            fresh.SetFloat("_ZWrite",0);fresh.SetFloat("_Cull",2);
            fresh.SetOverrideTag("RenderType","Transparent");fresh.renderQueue=3000;
        }
        if(emissive && fresh.HasProperty("_EmissionColor"))
            fresh.SetColor("_EmissionColor",new Color(.50f,.31f,.11f));
        string path=Generated+"/Materials/"+id+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(fresh,old);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(fresh,path);return fresh;
    }
}
