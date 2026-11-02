// CLUB NOVA — błyski na kuli lustrzanej (przebudowa 2.11: „KULA ma błyskać”). Siatka z małych kwadratów osadzonych na
// powierzchni kuli (dziecko kuli — obraca się razem z nią); każdy kwadrat ustawiany przodem do kamery w shaderze
// wierzchołków (billboard), gwiazdka czterech promieni, błysk krótki i rzadki (każda iskra własna faza). Addytywnie.
Shader "Braxhorn/NovaIskry"
{
    Properties
    {
        _Rozmiar("Rozmiar iskry (m)", Float) = 0.22
        _Moc("Jasność", Range(0,6)) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent+6" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Rozmiar, _Moc;
            float _NovaCzas, _NovaAktywny;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; half jas:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 c = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 r = UNITY_MATRIX_V[0].xyz, u = UNITY_MATRIX_V[1].xyz;
                float faza = frac(_NovaCzas * (0.35 + 0.5 * v.uv1.x) + v.uv1.y);
                float blysk = pow(saturate(1 - abs(faza - .5) * 14.0), 2);
                float s = _Rozmiar * (0.3 + blysk);
                float3 p = c + (r * v.uv.x + u * v.uv.y) * s;
                o.pos = mul(UNITY_MATRIX_VP, float4(p, 1));
                o.uv = v.uv; o.jas = blysk * _Moc * _NovaAktywny;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 a = abs(i.uv);
                float gw = saturate(1 - a.x * 8) * saturate(1 - a.y) + saturate(1 - a.y * 8) * saturate(1 - a.x);
                float rdz = saturate(1 - length(i.uv) * 2.2);
                float m = saturate(gw * .8 + rdz * rdz);
                return fixed4(float3(1, .97, .92) * m * i.jas, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
