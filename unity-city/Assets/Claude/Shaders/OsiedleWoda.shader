// BRAXHORN R2 — woda w basenach i fontannie: ruchoma tafla (suma kilku fal kierunkowych w normalnej),
// głęboki turkus z jaśniejszą płycizną przy patrzeniu z góry, odbicie nieba (gładkość, Fresnel w Standard).
// Nic nie świeci.
Shader "Braxhorn/OsiedleWoda"
{
    Properties
    {
        _Color ("Głębia", Color) = (.07,.24,.30,1)
        _Shallow ("Płycizna", Color) = (.20,.52,.58,1)
        _Gloss ("Gładkość", Range(0,1)) = .94
        _Wave ("Siła fal", Range(0,2)) = .55
        _Scale ("Gęstość fal (1/m)", Float) = 2.2
        _Speed ("Prędkość fal", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color, _Shallow;
        half _Gloss, _Wave, _Scale, _Speed;
        struct Input { float3 worldPos; float3 viewDir; };

        float2 Wave(float2 p, float2 dir, float freq, float speed, float t)
        {
            float ph = dot(p, dir) * freq + t * speed;
            return dir * freq * cos(ph);
        }
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 p = IN.worldPos.xz * _Scale; float t = _Time.y * _Speed;
            float2 g = Wave(p, float2(.80, .60), 1.0, 1.3, t) * .50
                     + Wave(p, float2(-.45, .89), 1.7, 1.9, t) * .30
                     + Wave(p, float2(.97, -.24), 2.9, 2.6, t) * .14
                     + Wave(p, float2(-.70, -.71), 4.3, 3.4, t) * .07;
            float3 n = normalize(float3(-g.x * _Wave * .12, -g.y * _Wave * .12, 1));
            o.Normal = n;
            float facing = saturate(dot(normalize(IN.viewDir), n));
            o.Albedo = lerp(_Color.rgb, _Shallow.rgb, facing * facing);
            o.Smoothness = _Gloss; o.Metallic = 0; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
