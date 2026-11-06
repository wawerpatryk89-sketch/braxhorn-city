using System;
using System.Linq;
using UnityEngine;

// BRAXHORN OBÓZ only. Source FBX, bones, skin, clips and their timing remain
// byte-for-byte untouched. This is a small WORLD visual-offset correction
// when the owner-controlled character is idle on a real collider.
[DefaultExecutionOrder(400)]
public sealed class BraxObozIdleFootContact : MonoBehaviour
{
    public BraxGymPlayer player;
    const float K=1.294f;
    const float MaxCorrection=.08f*K;
    const float DesiredClearance=.006f*K;
    SkinnedMeshRenderer[] skins;
    bool warned;

    void LateUpdate()
    {
        if(!player||!player.visual||!player.body||!player.clips)return;
        if(skins==null)skins=player.visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if(skins.Length==0)return;
        var visual=player.visual;
        bool idle=player.body.isGrounded&&player.clips.IsPlaying("Idle");
        float targetOffset=0f;
        if(idle)
        {
            // CharacterController belongs to layer 8; exclude its collider.
            Vector3 origin=player.transform.position+Vector3.up*(.65f*K);
            if(Physics.Raycast(origin,Vector3.down,out var ground,3f*K,
                    ~(1<<8),QueryTriggerInteraction.Ignore))
            {
                float minY=float.PositiveInfinity;
                foreach(var skin in skins)
                    if(skin.enabled && skin.gameObject.activeInHierarchy)
                        minY=Mathf.Min(minY,skin.bounds.min.y);
                if(!float.IsInfinity(minY))
                {
                    float discrepancy=ground.point.y+DesiredClearance-minY;
                    targetOffset=Mathf.Clamp(visual.localPosition.y+discrepancy,
                        -MaxCorrection,MaxCorrection);
                    if(!warned && Mathf.Abs(discrepancy)>.10f*K)
                    {
                        Debug.LogWarning("BRAXHORN_MONIKA_FOOT_CONTACT: source idle pose exceeds " +
                            "8 cm, inspect original animation/rig; no forced bone edit");
                        warned=true;
                    }
                }
            }
        }
        // Do not change the player/capsule movement, root motion or rig. Return
        // visual to zero at normal walking/jumping/attacking transitions.
        var p=visual.localPosition;
        p.y=Mathf.MoveTowards(p.y,targetOffset,
            Mathf.Max(0f,Time.deltaTime)*(.52f*K));
        visual.localPosition=p;
    }
}
