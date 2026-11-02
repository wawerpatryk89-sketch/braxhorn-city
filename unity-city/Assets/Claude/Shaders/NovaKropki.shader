// CLUB NOVA (przebudowa 2.11, właściciel: „KULA ma błyskać”) — plamki światła od kuli lustrzanej na ścianach, suficie
// i podłodze sali. Bez cząsteczek i bez świateł: jedna siatka z N kwadratów; każdy kwadrat niesie kierunek lusterka kuli
// (NORMAL) i narożnik (UV0). Shader wierzchołków obraca kierunek o kąt obrotu kuli (_NovaKat, globalny z BraxNovaEfekty),
// przecina promień ze ścianami sali (prostopadłościan _SalaMin.._SalaMax) i kładzie plamkę 3 cm przed trafioną płaszczyzną
// (odstęp od powierzchni — bez migotania). Meble zasłaniają plamki zwykłym testem głębi. Mieszanie addytywne.
Shader "Braxhorn/NovaKropki"
{
    Properties
    {
        _Srodek("Środek kuli (świat)", Vector) = (42,3.6,50,0)
        _SalaMin("Sala min (świat)", Vector) = (32.6,0.15,41.8,0)
        _SalaMax("Sala max (świat)", Vector) = (51.4,4.55,61.4,0)
        _Rozmiar("Rozmiar plamki na 1 m odległości", Float) = 0.022
        _Moc("Jasność", Range(0,4)) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Srodek, _SalaMin, _SalaMax;
            float _Rozmiar, _Moc;
            float _NovaKat, _NovaCzas, _NovaAktywny, _NovaBeat;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; half4 kol:TEXCOORD1; };

            float3 Paleta(float s)
            {
                float3 c = float3(1, 1, 1);
                if (s > .72) c = float3(1, .25, .85);
                if (s > .82) c = float3(.3, .55, 1);
                if (s > .92) c = float3(.4, 1, .55);
                return c;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float a = _NovaKat;
                float ca = cos(a), sa = sin(a);
                float3 d = normalize(v.normal);
                d = float3(d.x * ca - d.z * sa, d.y, d.x * sa + d.z * ca);
                float3 c = _Srodek.xyz;
                float3 dd = d + step(abs(d), 1e-4) * 1e-4;
                float3 tx = lerp(_SalaMin.xyz - c, _SalaMax.xyz - c, step(0, dd)) / dd;
                float t = min(tx.x, min(tx.y, tx.z));
                float3 n = float3(0, 0, -sign(dd.z));
                if (tx.y <= t + 1e-4) n = float3(0, -sign(dd.y), 0);
                if (tx.x <= t + 1e-4) n = float3(-sign(dd.x), 0, 0);
                float3 hit = c + d * t + n * 0.03;
                float3 u = abs(n.y) > .5 ? float3(1, 0, 0) : normalize(cross(float3(0, 1, 0), n));
                float3 w = cross(n, u);
                float s = _Rozmiar * t * (0.8 + 0.4 * frac(v.uv1.x * 7.31));
                float3 p = hit + (u * v.uv.x + w * v.uv.y) * s;
                o.pos = mul(UNITY_MATRIX_VP, float4(p, 1));
                o.uv = v.uv;
                // migotanie lusterek (każda plamka własna faza) + puls na bit
                float migot = 0.55 + 0.45 * sin(_NovaCzas * (5.0 + 9.0 * v.uv1.x) + v.uv1.x * 40.0);
                float bit = 0.75 + 0.25 * (1 - frac(_NovaBeat));
                float spad = saturate(1.6 / max(t, 1.0));
                o.kol = half4(Paleta(v.uv1.y) * migot * bit * (0.45 + spad) * _Moc * _NovaAktywny, 1);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half r = length(i.uv);
                half m = saturate(1 - r); m = m * m;
                return fixed4(i.kol.rgb * m, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
