using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// HOTEL SOBCZAK T03 — material set of the owner's board 9.11.2026
// ("MUROWANY MODERN BRICK", Poly Haven 2K): Exterior Wall Cladding 02 (brick),
// Granite Tile 03 (podium, ground floor), Concrete Pavement 02 (sidewalks),
// Clean Asphalt (drive), Teak Veneer (reception, interior). Interior finishes reuse
// region slugs. Ids start with "t03_" -> Android ASTC 6x6 (ZASADA-1); maps that
// other buildings already copied are shared (BraxChampionsFoundation texture sets).
public static class BraxChampionsHotelSobczakMaterials
{
    public sealed class Set
    {
        public Material brick,podiumStone,stoneDark,paving,asphalt,kerb,teak,teakDark,marble,marbleWet,blackMarble,oak,carpet,
            carpetRoom,terrazzo,tile,paint,ceiling,concrete,leather,leatherWhite,fabric,curtain,
            brass,darkBronze,steel,glass,clearGlass,mirror,whitePaint,warmLight,ledCool,water,lawn,rubber,
            // premium finish 9.11 (owner: "wszedzie wykonczenie Poly Haven, nic nie jest sztuczne")
            grasscloth,boucle,jersey,linen,linenWhite,satin,velvet,rugRoom,rugLobby,walnut,walnutDark,cognac,parquet,
            whiteTiles,antiSkid,chrome,screen,frosted;
    }

    const string Generated="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    public static readonly string[] RequiredSlugs={
        "exterior_wall_cladding_02","granite_tile_03","concrete_pavement_02","clean_asphalt","teak_veneer",
        "marble_01","oak_wood_planks","poly_wool_herringbone","terrazzo_tiles","tiled_floor_001",
        "beige_wall_001","white_stucco_02","concrete_wall_003","brown_leather","leather_white",
        "fabric_pattern_05","granite_tile","rubber_tiles",
        "hessian_230","curly_teddy_natural","cotton_jersey","terlenka","crepe_satin","velour_velvet","floral_jacquard",
        "smoked_walnut_veneer","fabric_leather_02","herringbone_parquet","long_white_tiles","anti_skid_tiles"
    };

    public static void VerifySources()
    {
        string source="Assets/Source/Mistrzowie/Textures";
        foreach(string slug in RequiredSlugs)
            foreach(string suffix in new[]{"diff_4k.jpg","nor_gl_4k.exr","arm_4k.png"})
            {
                string p=source+"/"+slug+"_"+suffix;
                if(!File.Exists(p))throw new FileNotFoundException(
                    "HOTEL SOBCZAK requires official Poly Haven source prepared by prepare_assets.py: "+p,p);
            }
    }

    public static Set Create()
    {
        VerifySources();
        Directory.CreateDirectory(Generated+"/Materials");
        var s=new Set();
        // exterior (board)
        s.brick=P("brick_facade","exterior_wall_cladding_02",2.0f,new Color(1f,.97f,.95f),.92f);
        s.podiumStone=P("podium_granite","granite_tile_03",1.8f,new Color(.96f,.94f,.90f),.55f);
        s.stoneDark=P("dark_granite","granite_tile",1.2f,new Color(.30f,.31f,.33f),.45f);
        s.paving=P("sidewalk_paving","concrete_pavement_02",1.8f,new Color(.97f,.97f,.96f),1f);
        s.asphalt=P("drive_asphalt","clean_asphalt",2.5f,new Color(.70f,.70f,.70f),1f);
        s.kerb=P("granite_kerb","granite_tile",1.0f,new Color(.62f,.63f,.65f),.6f);
        s.teak=P("teak_veneer","teak_veneer",1.2f,new Color(.95f,.72f,.50f),.45f);
        s.teakDark=P("teak_dark","teak_veneer",1.2f,new Color(.50f,.34f,.23f),.45f);
        // interior
        s.marble=P("cream_marble","marble_01",1.5f,new Color(1f,.98f,.94f),.30f);
        s.marbleWet=P("bath_marble_floor","marble_01",1.0f,new Color(.92f,.90f,.86f),.45f);
        try{s.blackMarble=BraxChampionsFoundation.LibraryMarble("t03_black_marble",new Color(.86f,.80f,.78f),2.5f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI T03_BLACK_MARBLE_MISSING "+e.Message);s.blackMarble=s.stoneDark;}
        s.oak=P("room_oak_floor","oak_wood_planks",1.6f,new Color(.96f,.90f,.82f),.60f);
        s.carpet=P("corridor_carpet","poly_wool_herringbone",.40f,new Color(.40f,.34f,.30f),1f);
        s.carpetRoom=P("room_rug","poly_wool_herringbone",.40f,new Color(.78f,.74f,.68f),1f);
        s.terrazzo=P("spa_terrazzo","terrazzo_tiles",1.6f,new Color(.97f,.96f,.94f),.40f);
        s.tile=P("kitchen_tile","tiled_floor_001",.8f,new Color(.90f,.90f,.90f),.45f);
        s.paint=P("wall_paint","beige_wall_001",2.2f,new Color(1f,.98f,.95f),.95f);
        s.ceiling=P("ceiling_stucco","white_stucco_02",2.0f,new Color(.98f,.98f,.97f),.95f);
        if(s.ceiling.HasProperty("_BumpScale"))s.ceiling.SetFloat("_BumpScale",.15f);
        s.concrete=P("technical_concrete","concrete_wall_003",3.0f,new Color(.82f,.81f,.78f),1f);
        s.leather=P("brown_leather","brown_leather",.35f,new Color(.40f,.24f,.15f),.55f);
        s.leatherWhite=P("white_leather","leather_white",.35f,new Color(.98f,.97f,.94f),.55f);
        s.fabric=P("bed_linen","fabric_pattern_05",.6f,new Color(1.05f,1.03f,1f),1f);
        s.curtain=P("sheer_curtain","fabric_pattern_05",.8f,new Color(.95f,.90f,.82f),1f);
        s.rubber=P("fitness_rubber","rubber_tiles",2.0f,new Color(.30f,.31f,.32f),1f);
        s.lawn=Region("grass.mat")??P("planter_lawn","granite_tile",1f,new Color(.3f,.5f,.2f),1f);
        // premium finish: natural-colour Poly Haven scans (fabric colour is the scan's own, tints stay near 1)
        s.grasscloth=P("grasscloth_wallcovering","hessian_230",.55f,new Color(1.16f,1.12f,1.06f),1f);
        s.boucle=P("boucle_upholstery","curly_teddy_natural",.45f,new Color(1f,.99f,.97f),1f);
        s.jersey=P("taupe_upholstery","cotton_jersey",.55f,new Color(1f,1f,1f),1f);
        s.linen=P("linen_drapes","terlenka",.70f,new Color(1f,.99f,.97f),1f);
        s.linenWhite=P("white_bed_linen","terlenka",.60f,new Color(1.22f,1.22f,1.20f),1f);
        s.satin=P("champagne_satin","crepe_satin",.60f,new Color(1f,.98f,.95f),.85f);
        s.velvet=P("burgundy_velvet","velour_velvet",.50f,new Color(.82f,.78f,.80f),1f);
        s.rugRoom=P("room_rug_jacquard","floral_jacquard",.90f,new Color(1f,1f,1f),1f);
        s.rugLobby=P("lobby_rug_jacquard","floral_jacquard",1.1f,new Color(1.05f,.98f,.92f),1f);
        s.walnut=P("smoked_walnut","smoked_walnut_veneer",1.2f,new Color(.92f,.86f,.80f),.55f);
        s.walnutDark=P("smoked_walnut_dark","smoked_walnut_veneer",1.2f,new Color(.55f,.46f,.40f),.55f);
        s.cognac=P("cognac_leather","fabric_leather_02",.45f,new Color(1f,1f,1f),.8f);
        s.parquet=P("restaurant_parquet","herringbone_parquet",1.8f,new Color(1f,.97f,.94f),.7f);
        s.whiteTiles=P("kitchen_wall_tiles","long_white_tiles",1.0f,new Color(1f,1f,1f),.6f);
        s.antiSkid=P("kitchen_floor_tiles","anti_skid_tiles",1.0f,new Color(.95f,.95f,.95f),1f);
        // Interior irradiance (BraxChampionsInteriorLight): warm interior sky/ground instead of the city's.
        foreach(var m in new[]{s.ceiling,s.grasscloth,s.paint,s.carpet,s.carpetRoom,s.oak,s.marbleWet,s.parquet,s.terrazzo,s.tile,
                               s.whiteTiles,s.antiSkid,s.boucle,s.jersey,s.linen,s.linenWhite,s.satin,s.velvet,s.rugRoom,s.rugLobby,
                               s.walnut,s.walnutDark,s.cognac})
            BraxChampionsInteriorLight.Apply(m,m==s.ceiling?BraxChampionsInteriorLight.Ceilings:BraxChampionsInteriorLight.Walls);
        // façades in shade: ground bounce of the sunlit plaza (BraxChampionsInteriorLight.Facade)
        foreach(var m in new[]{s.brick,s.podiumStone,s.stoneDark,s.teak,s.teakDark})BraxChampionsInteriorLight.Facade(m);
        // surfaces
        s.brass=Surface("polished_brass",new Color(.93f,.70f,.30f),.19f,.98f,false,false);
        s.darkBronze=Surface("dark_bronze_frames",new Color(.10f,.09f,.08f),.30f,.85f,false,false);
        s.steel=Surface("brushed_stainless",new Color(.62f,.64f,.66f),.30f,1.0f,false,false);
        s.glass=Surface("tower_glass",new Color(.36f,.42f,.47f,.45f),.05f,0f,true,false);
        if(s.glass.HasProperty("_ReflectionStrength"))s.glass.SetFloat("_ReflectionStrength",.72f);
        s.clearGlass=Surface("clear_glass",new Color(.80f,.88f,.92f,.20f),.08f,0f,true,false);
        s.mirror=Surface("polished_mirror",new Color(.78f,.82f,.85f),.06f,.95f,false,false);
        s.whitePaint=Surface("sanitary_porcelain",new Color(.97f,.97f,.96f),.25f,0f,false,false);
        s.warmLight=Surface("warm_architectural_light",new Color(1f,.88f,.66f),.36f,0f,false,true);
        s.ledCool=Surface("led_strip",new Color(1f,.93f,.80f),.36f,0f,false,true);
        s.water=Surface("pool_water",new Color(.22f,.52f,.64f,.62f),.05f,0f,true,false);
        s.chrome=Surface("polished_chrome",new Color(.86f,.88f,.90f),.08f,1.0f,false,false);
        s.screen=Surface("tv_black_glass",new Color(.02f,.02f,.025f),.06f,.2f,false,false);
        s.frosted=Surface("frosted_glass_lamp",new Color(1f,.93f,.82f),.40f,0f,false,true);
        EditorUtility.SetDirty(s.glass);
        return s;
    }

    static Material P(string id,string slug,float metres,Color tint,float roughFactor)
    {
        var m=BraxChampionsFoundation.SourceMaterial("t03_"+id,slug,metres,tint);
        m.SetFloat("_Roughness",roughFactor);EditorUtility.SetDirty(m);
        return m;
    }
    static Material Region(string file)
        =>AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/"+file);

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
            fresh.SetColor("_EmissionColor",new Color(.62f,.44f,.20f));
        string path=Generated+"/Materials/"+id+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(fresh,old);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(fresh,path);return fresh;
    }
}
