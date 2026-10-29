using UnityEngine;

// Keep dynamic font atlases bound to a depth-tested world material, so signs
// are occluded by walls instead of appearing as game overlays.
[RequireComponent(typeof(TextMesh))]
public sealed class BraxCitySignText : MonoBehaviour
{
    private TextMesh text;
    private MeshRenderer surface;
    private Material original, instance;

    private void OnEnable()
    {
        text=GetComponent<TextMesh>();surface=GetComponent<MeshRenderer>();
        original=surface.sharedMaterial;instance=new Material(original);
        surface.sharedMaterial=instance;
        Font.textureRebuilt+=RefreshAtlas;
        text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
        RefreshAtlas(text.font);
    }

    private void RefreshAtlas(Font font)
    {
        if(instance&&text&&font==text.font)instance.mainTexture=font.material.mainTexture;
    }

    private void OnDisable()
    {
        Font.textureRebuilt-=RefreshAtlas;
        if(surface)surface.sharedMaterial=original;
        if(instance)Destroy(instance);
        instance=null;
    }
}
