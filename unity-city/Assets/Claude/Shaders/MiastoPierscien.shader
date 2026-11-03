// BRAXHORN CITY — PIERŚCIEŃ HORYZONTU 3D (panorama, pkt 7 / 7b / 15d): archipelag wysp na morzu, wzgórza i góry za miastem
// jako prawdziwe bryły 3D (cieniowane słońcem sceny), ustawiane przy kamerze w skali (BraxPierscien) — kąt widzenia jak z
// prawdziwych 2–9 km, bez paralaksy (nie da się dopłynąć). Głębia zapisana tuż przed płaszczyzną daleką: wszystko ze sceny
// rysuje się PRZED pierścieniem, niebo nie zamaluje pierścienia; kolejność wewnątrz pierścienia zachowana (odległość w skali).
// Kolor z wierzchołków (rgb), mgiełka odległości w alfie wierzchołka (zapieczona przy buildzie). Nic nie świeci.
Shader "Braxhorn/MiastoPierscien"
{
    Properties
    {
        _Mgla ("Kolor mgiełki horyzontu", Color) = (.70, .76, .83, 1)
        _Slonce ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        _Jasnosc ("Jasność", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Geometry+450" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Back ZWrite On ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            half4 _Mgla; float4 _Slonce; half _Jasnosc;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; half3 n : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                // głębia: tuż przed płaszczyzną daleką, porządek wg odległości w pierścieniu (0…900 m)
                float dd = saturate(distance(w, _WorldSpaceCameraPos.xyz) / 900.0);
                #if defined(UNITY_REVERSED_Z)
                o.pos.z = o.pos.w * (1e-5 + 9e-5 * (1.0 - dd));
                #else
                o.pos.z = o.pos.w * (1.0 - 1e-5 - 9e-5 * (1.0 - dd));
                #endif
                o.color = v.color;
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                half ndl = saturate(dot(normalize(i.n), normalize(_Slonce.xyz)));
                half3 c = i.color.rgb * (0.48 + 0.62 * ndl) * _Jasnosc;
                c = lerp(c, _Mgla.rgb, i.color.a);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
