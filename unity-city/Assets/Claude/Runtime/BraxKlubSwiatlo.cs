using UnityEngine;
using UnityEngine.Rendering;

// Local room lighting; original PBR materials and the outdoor environment are preserved.
public sealed class BraxKlubSwiatlo : MonoBehaviour
{
    public Renderer[] powierzchnie;
    public int warstwa;
    public Color ambient = new Color(.025f,.018f,.035f);
    public Bounds sala = new Bounds(new Vector3(86f,3.45f,14f),new Vector3(39.64f,6.78f,29.64f));
    Renderer[] gracz; int[] warstwyGracza; LightProbeUsage[] probesGracza;
    MaterialPropertyBlock[] blokiGracza;
    bool wSrodku; float next;
    MaterialPropertyBlock block;
    SphericalHarmonicsL2[] sh;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        sh=new SphericalHarmonicsL2[1];sh[0].AddAmbientLight(ambient);
        if(powierzchnie!=null)foreach(var r in powierzchnie)Apply(r);
    }
    void Apply(Renderer r)
    {
        if(!r)return;
        if(block==null)block=new MaterialPropertyBlock();
        r.gameObject.layer=warstwa;r.lightProbeUsage=LightProbeUsage.CustomProvided;
        r.GetPropertyBlock(block);block.CopySHCoefficientArraysFrom(sh);
        // Imported models use CityStaticPBR: its authored sun/sky bypass Unity probes.
        // Override illumination only; preserve every original PBR map and UV binding.
        block.SetFloat("_SunIntensity",0f);block.SetFloat("_AmbientIntensity",.08f);block.SetFloat("_ReflectionStrength",.07f);
        r.SetPropertyBlock(block);
    }
    void Update()
    {
        if(Time.time<next)return;next=Time.time+.2f;
        var p=BraxGymPlayerInstance();if(!p)return;
        if(gracz==null)
        {
            gracz=p.visual.GetComponentsInChildren<Renderer>(true);
            warstwyGracza=new int[gracz.Length];probesGracza=new LightProbeUsage[gracz.Length];blokiGracza=new MaterialPropertyBlock[gracz.Length];
            for(int i=0;i<gracz.Length;i++){warstwyGracza[i]=gracz[i].gameObject.layer;probesGracza[i]=gracz[i].lightProbeUsage;blokiGracza[i]=new MaterialPropertyBlock();gracz[i].GetPropertyBlock(blokiGracza[i]);}
        }
        bool inside=sala.Contains(p.transform.position+Vector3.up);
        if(inside==wSrodku)return;wSrodku=inside;
        for(int i=0;i<gracz.Length;i++)if(gracz[i])
        {
            if(inside)Apply(gracz[i]);
            else {gracz[i].gameObject.layer=warstwyGracza[i];gracz[i].lightProbeUsage=probesGracza[i];gracz[i].SetPropertyBlock(blokiGracza[i]);}
        }
    }
    BraxGymPlayer player;
    BraxGymPlayer BraxGymPlayerInstance(){if(!player)player=FindAnyObjectByType<BraxGymPlayer>();return player && player.visual?player:null;}
    void OnDisable()
    {
        if(gracz==null)return;
        for(int i=0;i<gracz.Length;i++)if(gracz[i]){gracz[i].gameObject.layer=warstwyGracza[i];gracz[i].lightProbeUsage=probesGracza[i];gracz[i].SetPropertyBlock(blokiGracza[i]);}
        wSrodku=false;
    }
}
