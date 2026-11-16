Shader "Braxhorn/Obsada Osiedla"
{
    Properties
    {
        _MainTex ("Kolor źródłowy", 2D) = "white" {}
        _BumpMap ("Normalne", 2D) = "bump" {}
        _MetalSmooth ("Metal w R, gładkość w A", 2D) = "black" {}
        _FaceTex ("Fazy powiek i warg", 2D) = "black" {}
        _Blink ("Powieki", Range(0,1)) = 0
        _Speech ("Usta", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        sampler2D _MainTex, _BumpMap, _MetalSmooth, _FaceTex;
        half _Blink, _Speech;
        struct Input { float2 uv_MainTex; float2 uv2_FaceTex; };
        half4 phase(float2 coord, float n)
        {
            float2 tile = float2(fmod(n,4.0),1.0-floor(n/4.0));
            return tex2D(_FaceTex,(tile+clamp(coord,.002,.998))/float2(4,2));
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            half4 packedMap = tex2D(_MetalSmooth,uv);
            half3 color = tex2D(_MainTex,uv).rgb;
            float2 face = IN.uv2_FaceTex;
            half weight = face.y>.5 ? _Blink : _Speech;
            if (weight>.0001 && all(face>=0) && all(face<=1))
            {
                float f=saturate(weight)*7;
                half4 a=phase(face,floor(f)), b=phase(face,min(7.0,floor(f)+1.0));
                color=lerp(lerp(color,a.rgb,a.a),lerp(color,b.rgb,b.a),frac(f));
            }
            o.Albedo=color;
            o.Normal=UnpackNormal(tex2D(_BumpMap,uv));
            o.Metallic=packedMap.r;
            o.Smoothness=packedMap.a;
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
