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
            brass,darkBronze,steel,glass,clearGlass,blackGlass,mirror,whitePaint,warmLight,ledCool,water,lawn,rubber,
            // premium finish 9.11 (owner: "wszedzie wykonczenie Poly Haven, nic nie jest sztuczne")
            grasscloth,boucle,jersey,linen,linenWhite,satin,velvet,rugRoom,rugLobby,walnut,walnutDark,cognac,parquet,
            whiteTiles,antiSkid,chrome,screen,frosted;
    }

    const string Generated="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    public static readonly string[] RequiredSlugs={
        "exterior_wall_cladding_02","granite_tile_03","concrete_pavement_02","clean_asphalt","teak_veneer",
        "marble_01","laminate_floor_02","poly_wool_herringbone","terrazzo_tiles","tiled_floor_001",
        "beige_wall_001","white_stucco_02","concrete_wall_003","brown_leather","leather_white",
        "fabric_pattern_05","granite_tile","rubber_tiles",
        "curly_teddy_natural","cotton_jersey","terlenka","crepe_satin","velour_velvet","floral_jacquard",
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
        // owner 9.11 23:29 travertine; 10.11 12:48 on the phone the sand travertine piers read "sraczkowate" (beige):
        // polished white marble podium and piers (Marble021) - classic with the brick tower, no beige anywhere
        try{s.podiumStone=BraxChampionsFoundation.LibraryMarbleWhite("t03_podium_white_marble",new Color(.95f,.95f,.94f),2.4f);s.podiumStone.SetFloat("_Roughness",.32f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI T03_PODIUM_MARBLE_MISSING "+e.Message);}
        s.stoneDark=P("dark_granite","granite_tile",1.2f,new Color(.30f,.31f,.33f),.45f);
        s.paving=P("sidewalk_paving","concrete_pavement_02",1.8f,new Color(1.02f,.97f,.90f),1f);
        s.asphalt=P("drive_asphalt","clean_asphalt",2.5f,new Color(.70f,.70f,.70f),1f);
        s.kerb=P("granite_kerb","granite_tile",1.0f,new Color(.62f,.63f,.65f),.6f);
        // owner 10.11 12:48-12:54: the orange teak soffits and joinery read brown - silver-grey aged teak (as the beach)
        s.teak=P("teak_silver","teak_veneer",1.2f,new Color(.91f,1.20f,1.72f),.45f);
        s.teakDark=P("teak_dark_walnut","smoked_walnut_veneer",1.2f,new Color(.55f,.46f,.40f),.45f);
        // interior
        // owner 9.11 23:00: marble_01 read as beige bathroom tiles and every colour was the same beige.
        // Lobby, counters and bathrooms: polished white marble slabs (ambientCG Marble021), grey-white bath floor.
        try
        {
            s.marble=BraxChampionsFoundation.LibraryMarbleWhite("t03_white_marble",new Color(.88f,.88f,.87f),2.4f);
            s.marbleWet=BraxChampionsFoundation.LibraryMarbleWhite("t03_bath_marble_floor",new Color(.74f,.75f,.77f),1.6f);
            s.marble.SetFloat("_Roughness",.28f);s.marbleWet.SetFloat("_Roughness",.40f);
        }
        catch(Exception e)
        {
            BraxBuildQuality.Warn("DO POPRAWKI T03_WHITE_MARBLE_MISSING "+e.Message);
            s.marble=P("cream_marble","marble_01",1.5f,new Color(1f,.98f,.94f),.30f);
            s.marbleWet=P("bath_marble_floor_fallback","marble_01",1.0f,new Color(.92f,.90f,.86f),.45f);
        }
        try{s.blackMarble=BraxChampionsFoundation.LibraryMarble("t03_black_marble",new Color(.86f,.80f,.78f),2.5f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI T03_BLACK_MARBLE_MISSING "+e.Message);s.blackMarble=s.stoneDark;}
        s.oak=P("room_oak_floor","laminate_floor_02",1.8f,new Color(1f,.98f,.95f),.55f);            // pale natural oak (was orange)
        s.carpet=P("corridor_carpet","floral_jacquard",.90f,new Color(1.20f,1.10f,1.06f),1f);        // charcoal damask (was flat brown)
        s.carpetRoom=P("room_rug_charcoal","poly_wool_herringbone",.40f,new Color(.42f,.43f,.47f),1f);
        s.terrazzo=P("spa_terrazzo","terrazzo_tiles",1.6f,new Color(.97f,.96f,.94f),.40f);
        s.tile=P("kitchen_tile","tiled_floor_001",.8f,new Color(.90f,.90f,.90f),.45f);
        // owner 10.11 12:54 (beige rooms on the phone): white mineral plaster instead of beige_wall_001
        s.paint=P("wall_paint_white","white_stucco_02",2.0f,new Color(1.10f,1.10f,1.09f),.95f);
        if(s.paint.HasProperty("_BumpScale"))s.paint.SetFloat("_BumpScale",.20f);
        s.ceiling=P("ceiling_stucco","white_stucco_02",2.0f,new Color(.98f,.98f,.97f),.95f);
        if(s.ceiling.HasProperty("_BumpScale"))s.ceiling.SetFloat("_BumpScale",.15f);
        s.concrete=P("technical_concrete","concrete_wall_003",3.0f,new Color(.82f,.81f,.78f),1f);
        // owner palette: no brown - black leather (was brown_leather tinted .40/.24/.15)
        try{s.leather=BraxChampionsFoundation.LibraryLeather("t03_black_leather",new Color(.33f,.55f,.70f));}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI T03_LEATHER_MISSING "+e.Message);s.leather=P("black_leather_fallback","brown_leather",.35f,new Color(.12f,.12f,.13f),.55f);}
        s.leatherWhite=P("white_leather","leather_white",.35f,new Color(.98f,.97f,.94f),.55f);
        // owner 10.11 12:56: "Dlaczego firanki nie są z prawdziwej tekstury?" - the plaid fabric_pattern_05 is gone: bed
        // linen and curtains are the linen weave scan (terlenka), white, at a tile that shows the weave up close
        s.fabric=P("bed_linen_white","terlenka",.45f,new Color(1.19f,1.34f,1.54f),1f);
        s.curtain=P("sheer_curtain_linen","terlenka",.32f,new Color(1.19f,1.34f,1.54f),1f);
        s.rubber=P("fitness_rubber","rubber_tiles",2.0f,new Color(.30f,.31f,.32f),1f);
        s.lawn=Region("grass.mat")??P("planter_lawn","granite_tile",1f,new Color(.3f,.5f,.2f),1f);
        // premium finish: natural-colour Poly Haven scans (fabric colour is the scan's own, tints stay near 1)
        // wallcovering: pale natural linen (the hessian read orange and made corridors a tan tunnel)
        s.grasscloth=P("linen_wallcovering","terlenka",.55f,new Color(1.10f,1.20f,1.33f),1f);       // greige linen, not tan
        s.boucle=P("boucle_upholstery_white","curly_teddy_natural",.45f,new Color(1.0f,1.10f,1.24f),1f);   // natural 233/211/187 -> white
        s.jersey=P("taupe_upholstery","cotton_jersey",.55f,new Color(1f,1f,1f),1f);
        s.linen=P("linen_drapes_white","terlenka",.32f,new Color(1.19f,1.34f,1.54f),1f);
        if(s.linen.HasProperty("_BumpScale"))s.linen.SetFloat("_BumpScale",1.6f);                   // the weave reads
        s.linenWhite=P("white_bed_linen","terlenka",.60f,new Color(1.19f,1.34f,1.54f),1f);   // scan 197/175/153 -> white
        s.satin=P("silver_satin","crepe_satin",.60f,new Color(1.0f,1.20f,1.70f),.85f);   // scan 218/172/118 (orange) -> silver
        s.velvet=P("burgundy_velvet","velour_velvet",.50f,new Color(.82f,.78f,.80f),1f);
        s.rugRoom=P("room_rug_jacquard","floral_jacquard",.90f,new Color(1f,1f,1f),1f);
        s.rugLobby=P("lobby_rug_jacquard","floral_jacquard",1.1f,new Color(1.05f,.98f,.92f),1f);
        s.walnut=P("smoked_walnut","smoked_walnut_veneer",1.2f,new Color(.92f,.86f,.80f),.55f);
        s.walnutDark=P("smoked_walnut_dark","smoked_walnut_veneer",1.2f,new Color(.55f,.46f,.40f),.55f);
        s.cognac=P("black_leather_upholstery","fabric_leather_02",.45f,new Color(.16f,.16f,.17f),.8f);   // was cognac (brown)
        s.parquet=P("restaurant_parquet","herringbone_parquet",1.8f,new Color(1f,.97f,.94f),.7f);
        s.whiteTiles=P("kitchen_wall_tiles","long_white_tiles",1.0f,new Color(1f,1f,1f),.6f);
        s.antiSkid=P("kitchen_floor_tiles","anti_skid_tiles",1.0f,new Color(.95f,.95f,.95f),1f);
        // Interior irradiance (BraxChampionsInteriorLight): warm interior sky/ground instead of the city's.
        foreach(var m in new[]{s.ceiling,s.marble,s.grasscloth,s.paint,s.carpet,s.carpetRoom,s.oak,s.marbleWet,s.parquet,s.terrazzo,s.tile,
                               s.whiteTiles,s.antiSkid,s.boucle,s.jersey,s.linen,s.linenWhite,s.satin,s.velvet,s.rugRoom,s.rugLobby,
                               s.walnut,s.walnutDark,s.cognac})
            BraxChampionsInteriorLight.Apply(m,m==s.ceiling?BraxChampionsInteriorLight.Ceilings:BraxChampionsInteriorLight.Walls);
        // façades in shade: ground bounce of the sunlit plaza (BraxChampionsInteriorLight.Facade)
        foreach(var m in new[]{s.brick,s.podiumStone,s.stoneDark,s.teak,s.teakDark})BraxChampionsInteriorLight.Facade(m);
        // surfaces
        s.brass=Surface("polished_brass",new Color(.93f,.70f,.30f),.19f,.98f,false,false);
        // owner 10.11 12:50 "zakaz szarego przy szybach - albo czarne": every frame satin black metal
        s.darkBronze=Surface("black_metal_frames",new Color(.035f,.037f,.042f),.28f,.85f,false,false);
        // lift doors, frames, kitchen: real brushed metal maps of the region (was a flat dark colour)
        // neutral steel: the Metal048A colour map is warm (254,227,156), the tint cancels it
        try{s.steel=BraxChampionsFoundation.LibraryMetal("t03_brushed_stainless",new Color(.74f,.84f,1.22f));}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI T03_BRUSHED_METAL_MISSING "+e.Message);s.steel=Surface("brushed_stainless",new Color(.62f,.64f,.66f),.30f,1.0f,false,false);}
        // owner 10.11 12:50: no grey glass - black reflective architectural glass on the tower and the podium
        s.glass=Surface("tower_glass_black",new Color(.025f,.03f,.035f,.88f),.04f,0f,true,false);
        if(s.glass.HasProperty("_ReflectionStrength"))s.glass.SetFloat("_ReflectionStrength",1f);
        s.blackGlass=Surface("entrance_black_glass",new Color(.025f,.03f,.035f,.94f),.04f,0f,true,false);
        if(s.blackGlass.HasProperty("_ReflectionStrength"))s.blackGlass.SetFloat("_ReflectionStrength",1f);
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
