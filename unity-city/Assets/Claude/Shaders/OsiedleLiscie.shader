// BRAXHORN R2 — liście drzew i krzewów osiedla.
// Karty liści oświetlane jak jedna bryła korony (normalne „kuliste” od środka korony zmieszane z normalną karty),
// obie strony karty jednakowo jasne (Cull Off, bez czarnych spodów), światło przechodzące pod słońce,
// ciemniejsze wnętrze i spód korony, lekki ruch liści na wietrze. Ciemna tekstura „island tree”
// przemalowana na świeżą zieleń z zachowaniem żyłek i plam (jasność względem średniej tekstury).
// Nic nie świeci.
Shader "Braxhorn/OsiedleLiscie"
{
    Properties
    {
        _MainTex ("Liście (RGBA)", 2D) = "white" {}
        _Cutoff ("Próg alfy", Range(0,1)) = .42
        _Leaf ("Zieleń liści", Color) = (.30,.45,.15,1)
        _Leaf2 ("Druga zieleń (odmiana drzew)", Color) = (.38,.47,.16,1)
        _MeanGamma ("Średnia jasność tekstury (gamma)", Float) = .118
        _MeanLinear ("Średnia jasność tekstury (linear)", Float) = .0158
        _Recolor ("Przemalowanie (min)", Range(0,1)) = .85
        _Detail ("Siła detalu tekstury", Range(0,1)) = .65
        _Gain ("Rozjaśnienie bez przemalowania", Float) = 1.1
        _Center ("Środek korony (obiekt)", Vector) = (0,0,0,0)
        _Radius ("Promień korony (obiekt)", Vector) = (1,1,1,0)
        _Sphere ("Udział normalnej korony", Range(0,1)) = .75
        _Wrap ("Owinięcie światła", Range(0,1)) = .45
        _Trans ("Prześwit pod słońce", Range(0,1)) = .35
        _Wind ("Ruch liści", Float) = .02
        _NoRecolor ("Bez przemalowania (rośliny z własnym kolorem)", Float) = 0
        _Up ("Normalna w górę (kępy traw)", Range(0,1)) = 0
        _InnerDark ("Ciemniejsze wnętrze korony", Range(0,1)) = .5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Leaf alphatest:_Cutoff addshadow vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Leaf, _Leaf2;
        float _MeanGamma, _MeanLinear, _Recolor, _Detail, _Gain, _Sphere, _Wrap, _Trans, _Wind, _NoRecolor, _Up, _InnerDark;
        float4 _Center, _Radius;
        struct Input { float2 uv_MainTex; float inner; float hue; };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 d = (v.vertex.xyz - _Center.xyz) / max(_Radius.xyz, 1e-3);
            float r = length(d);
            float3 sph = d / max(r, 1e-3);
            v.normal = normalize(lerp(v.normal, sph * max(_Radius.xyz, 1e-3), _Sphere));
            float3 upObj = normalize(mul((float3x3)unity_WorldToObject, float3(0, 1, 0)));
            v.normal = normalize(lerp(v.normal, upObj, _Up));
            o.inner = saturate(r) * saturate(sph.y * .35 + .75);
            float3 root = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
            o.hue = frac(sin(dot(root.xz, float2(12.9898, 78.233))) * 43758.5453);
            float t = _Time.y;
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float sway = sin(t * 1.6 + wp.x * .45 + wp.z * .31) + .5 * sin(t * 3.7 + wp.y * 1.3 + wp.x * .8);
            v.vertex.xyz += float3(.8, .25, .6) * sway * _Wind * saturate(r);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            #ifdef UNITY_COLORSPACE_GAMMA
                float mean = _MeanGamma;
            #else
                float mean = _MeanLinear;
            #endif
            float lum = dot(c.rgb, float3(.3, .59, .11));
            float k = lerp(1, lum / max(mean, 1e-4), _Detail);
            float leafy = saturate((c.g - c.r) * 6 + .5);
            float rec = max(_Recolor, leafy * step(_Recolor, .5) * .9) * (1 - _NoRecolor);
            fixed3 green = lerp(_Leaf.rgb, _Leaf2.rgb, IN.hue) * lerp(.85, 1.12, frac(IN.hue * 7.13));
            fixed3 col = lerp(c.rgb * _Gain, green * k, rec);
            o.Albedo = col * lerp(1 - _InnerDark, 1, IN.inner);
            o.Alpha = c.a;
        }

        half4 LightingLeaf (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half diff = saturate((ndl + _Wrap) / (1 + _Wrap));
            half back = pow(saturate(dot(viewDir, -lightDir)), 4) * _Trans;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (diff + back) * atten;
            c.a = s.Alpha;
            return c;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
