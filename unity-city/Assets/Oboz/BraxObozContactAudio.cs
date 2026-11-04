using UnityEngine;

// Short synthetic glove/body contacts, separate from voice and bag sounds.
// Triggered only at the validated contact frame, never merely by starting a punch.
public sealed class BraxObozContactAudio : MonoBehaviour
{
    public BraxGymPlayer player;
    AudioSource source;
    static AudioClip[] clips;
    bool paused;
    int sequence;
    void Awake()
    {
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
        source.spatialBlend=1;source.minDistance=2;source.maxDistance=22;source.dopplerLevel=0;
        source.rolloffMode=AudioRolloffMode.Linear;source.volume=.48f;source.priority=120;
        if(clips!=null)return;
        clips=new AudioClip[6];
        for(int variant=0;variant<6;variant++)
        {
            const int rate=44100;var data=new float[rate/5];var random=new System.Random(117+variant);
            float filtered=0,peak=.001f;bool block=variant>=3;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;
                filtered+=((float)random.NextDouble()*2-1-filtered)*(block?.28f:.12f);
                float transient=filtered*Mathf.Exp(-t*(block?110:75))*(block?1.6f:1.2f);
                float body=Mathf.Sin(2*Mathf.PI*(block?165:92)*t)*Mathf.Exp(-t*(block?55:28))*(block?.3f:.7f);
                data[i]=(transient+body)*Mathf.Min(1,t/.0007f);peak=Mathf.Max(peak,Mathf.Abs(data[i]));
            }
            for(int i=0;i<data.Length;i++)data[i]*=.8f/peak;
            clips[variant]=AudioClip.Create(block?"Kontakt rękawic":"Trafienie w sparingu",data.Length,1,rate,false);
            clips[variant].SetData(data,0);
        }
    }
    public void Contact(bool block)
    {
        if(!source||(player&&player.Paused))return;
        source.pitch=.98f+(sequence%3)*.02f;
        source.PlayOneShot(clips[(block?3:0)+(sequence++%3)],block?.8f:1);
    }
    void Update()
    {
        bool value=player&&player.Paused;
        if(value==paused)return;paused=value;
        if(paused)source.Pause();else source.UnPause();
    }
    void OnDisable(){if(source)source.Stop();}
}
