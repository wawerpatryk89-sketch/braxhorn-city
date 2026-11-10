using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Owner 9.11 23:36: "Szyldu nie ma nadal. Jeżeli nie ma szyldu, zrób sam szyld porządny. Tak jak się ma nazywać
// kasyno Wawer. Zrób zajebisty szyld, złoty." The owner PNG on the rotunda carrier was a Quad turned 180 degrees:
// its face looked into the building, so from the street the carrier stood empty (the TAXI board too).
// Now a REAL 3D gold sign from the region generator (build_casino_sign.py -> CasinoWawerSign.bytes, built in CI):
// WAWER + KASYNO in solid bevelled gold glyphs with light strips and a double arch, and PARKING over the garage.
public static partial class BraxChampionsCasinoT11
{
    const string Sign3DPath="Assets/Source/Mistrzowie/CasinoWawerSign.bytes";
    [Serializable] sealed class Sign3DFile{public float scaleK;public Sign3DData[] signs;}
    [Serializable] sealed class Sign3DData{public string id;public BraxChampionsFoundation.MeshData[] meshes;}
    static Sign3DFile sign3D;
    static int sign3DParts;
    static Material signGold,signGoldDeep,signRed,signBlack,signSparkle;

    // Bright solid gold that stays gold in shade: Metal048A brushed maps, half-metallic (a full mirror metal under
    // CityStaticPBR only reflects the sky x .4 and reads brown, like the arena sign) and a faint warm self-glow.
    static void SignGoldMaterials()
    {
        if(signGold&&signGoldDeep&&signRed&&signBlack&&signSparkle)return;
        signGold=BraxChampionsFoundation.LibraryMetal("T11_3D_sign_bright_gold",new Color(1f,.80f,.42f));
        signGold.SetFloat("_Metallic",.62f);signGold.SetFloat("_Roughness",.24f);
        signGold.SetColor("_EmissionColor",new Color(.22f,.15f,.05f));EditorUtility.SetDirty(signGold);
        BraxChampionsGold.Apply(signGold);
        signGoldDeep=BraxChampionsFoundation.LibraryMetal("T11_3D_sign_deep_gold_returns",new Color(.88f,.64f,.30f));
        signGoldDeep.SetFloat("_Metallic",.72f);signGoldDeep.SetFloat("_Roughness",.20f);
        signGoldDeep.SetColor("_EmissionColor",new Color(.10f,.065f,.02f));EditorUtility.SetDirty(signGoldDeep);
        BraxChampionsGold.Apply(signGoldDeep,true);
        // KASYNO WAWER crown (owner 10.11 16:45): glossy red lacquered aluminium outline, piano-black spade and club,
        // white sparkles that glow
        signRed=Surface("sign_red_lacquered_aluminium",new Color(.80f,.035f,.05f),false,false);
        signRed.SetFloat("_Metallic",.08f);signRed.SetFloat("_Roughness",.16f);
        signRed.SetColor("_EmissionColor",new Color(.10f,.004f,.006f));EditorUtility.SetDirty(signRed);
        signBlack=Surface("sign_piano_black_lacquer",new Color(.018f,.018f,.022f),false,false);
        signBlack.SetFloat("_Metallic",.05f);signBlack.SetFloat("_Roughness",.10f);EditorUtility.SetDirty(signBlack);
        signSparkle=Surface("sign_sparkle_white_glow",new Color(1f,.99f,.95f),false,true);
        signSparkle.SetColor("_EmissionColor",new Color(1.10f,1.05f,.95f));EditorUtility.SetDirty(signSparkle);
    }

    // position in nominal metres (x K here), the sign's own vertices already carry K; front toward -Z with yaw 180
    static bool Place3DSign(string id,Transform parent,string label,Vector3 nominal,float yaw,float scale)
    {
        if(sign3D==null)
        {
            if(!File.Exists(Sign3DPath)){BraxBuildQuality.Warn("DO POPRAWKI T11_3D_SIGN_MISSING "+Sign3DPath+" (build_casino_sign.py)");return false;}
            using(var f=File.OpenRead(Sign3DPath))using(var zip=new GZipStream(f,CompressionMode.Decompress))using(var r=new StreamReader(zip))
                sign3D=JsonUtility.FromJson<Sign3DFile>(r.ReadToEnd());
        }
        var sign=sign3D?.signs?.FirstOrDefault(s=>s.id==id);
        if(sign==null){BraxBuildQuality.Warn("DO POPRAWKI T11_3D_SIGN_EMPTY "+id);return false;}
        SignGoldMaterials();
        var g=Group(parent,label);
        g.localPosition=nominal*K;g.localRotation=Quaternion.Euler(0,yaw,0);g.localScale=Vector3.one*scale;
        foreach(var m in sign.meshes)
        {
            int count=m.positions==null?0:m.positions.Length/3;if(count==0||m.triangles==null)continue;
            var v=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                v[i]=new Vector3(m.positions[3*i],m.positions[3*i+1],m.positions[3*i+2]);
                uv[i]=m.uv!=null&&m.uv.Length>=2*count?new Vector2(m.uv[2*i],m.uv[2*i+1]):Vector2.zero;
            }
            var mesh=new Mesh{name="T11 3D sign "+m.id,indexFormat=count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=m.triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_3D_sign_"+m.id+".asset");
            var o=new GameObject(label+" | "+m.role);o.transform.SetParent(g,false);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;
            // every face gold (owner: "złoty"): ivory/gold faces champagne gold, accents polished gold
            o.AddComponent<MeshRenderer>().sharedMaterial=m.role=="light"?(id=="KASYNO_WAWER_crown"?signSparkle:led):m.role=="dark"?signBacking:
                m.role=="accent"?signGoldDeep:m.role=="red"?signRed:m.role=="black"?signBlack:signGold;
            GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
            sign3DParts++;
        }
        return true;
    }

    // Casino marquee: warm bulbs every 42 cm on the carrier's gold ring (owner 10.11 00:22: the sign was "not premium").
    // One shared bevelled mesh, emissive only: no realtime light.
    static void SignMarquee()
    {
        var bulb=Surface("sign_marquee_warm_bulb",new Color(1f,.93f,.72f),false,true);
        bulb.SetColor("_EmissionColor",new Color(1.25f,.96f,.52f));
        EditorUtility.SetDirty(bulb);
        int n=SignCarrierOuter.Length,count=0;float carry=0f;const float step=.42f;
        for(int i=0;i<n;i++)
        {
            Vector2 a=(SignCarrierOuter[i]+SignCarrierInner[i])*.5f,b=(SignCarrierOuter[(i+1)%n]+SignCarrierInner[(i+1)%n])*.5f;
            float len=(b-a).magnitude,d=carry;
            for(;d<len;d+=step)
            {
                var p=Vector2.Lerp(a,b,d/len);
                BeveledBox(signRoot,"T11 sign marquee warm bulb",bulb,p.x,12.0f+p.y,-36.31f,.115f,.115f,.06f,.04f,false);
                count++;
            }
            carry=d-len;
        }
        Debug.Log("T11_SIGN_MARQUEE bulbs="+count+" spacing=.42 onGoldRing=true emissiveOnly=true");
    }

    // Garage entrance (owner 23:36 "na wjeździe do garażu zobacz, co jest"): graphite fascia on the champagne beam
    // with 3D gold PARKING letters and a ringed P, lit from below.
    static void GarageSign()
    {
        B(signRoot,"Garage portal graphite sign fascia",signBacking,44.19f,5.70f,-22f,6.4f,1.10f,.30f,true);
        B(signRoot,"Garage portal fascia champagne top edge",gold,44.19f,6.27f,-22f,6.5f,.04f,.34f,false);
        Place3DSign("PARKING_garage",signRoot,"T11 3D gold PARKING sign | garage portal",new Vector3(44.19f,5.44f,-22.16f),180f,1f);
    }
}
