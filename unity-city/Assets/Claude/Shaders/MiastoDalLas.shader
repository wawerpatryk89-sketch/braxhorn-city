// BRAXHORN CITY — las na wzgórzach za granicą miasta (wykonawca „horyzont”, 2.11): jedna karta na drzewo (2 trójkąty),
// zawsze zwrócona do kamery (obrót wokół pionu w shaderze), obraz drzewa z gałązek (unity-tools/claude/drzewo_klaster.py,
// las.png — ten sam co na osiedlu). Światło i cień korony są w obrazie; mnożnik jasności jak teren miasta. Z odległością
// mgiełka powietrza i przejście w tło (MiastoTlo) przed końcem zasięgu kamery — dalsze plany lasu wtapiają się w grzbiety.
// Wierzchołek: POSITION = podstawa drzewa (4 rogi), TEXCOORD1.xy = przesunięcie rogu (szerokość, wysokość).
Shader "Braxhorn/MiastoDalLas"
{
    Properties
    {
        _MainTex ("Drzewa (RGBA)", 2D) = "white" {}
        _Color ("Odcień", Color) = (1, 1, 1, 1)
        _Cutoff ("Próg alfy", Range(0, 1)) = .45
        _Jasnosc ("Jasność", Float) = 1
        _MglaOd ("Przejście w tło od (ułamek końca)", Float) = .6
        _MglaDo ("Przejście w tło najdalej (m)", Float) = 470
        _NieboTex ("Niebo", 2D) = "grey" {}
        _NieboObrot ("Obrót nieba", Float) = 0
        _NieboEkspozycja ("Ekspozycja", Float) = 1
        _NieboNasycenie ("Nasycenie", Float) = 1.3
        _NieboChmury ("Ruch chmur", Float) = .0035
        _SloKierunek ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        _Pasmo ("Góry", 2D) = "black" {}
        _PasmoJest ("Góry włączone", Float) = 0
        _PasmoSrodek ("Środek walców", Vector) = (17, 0, 49, 0)
        _PasmoPromien ("Promień gór", Float) = 2600
        _PasmoU0 ("u0", Float) = -.23
        _PasmoGora ("Góra", Float) = 35
        _PasmoDol ("Dół", Float) = -10
        _PasmoJasnosc ("Jasność gór", Float) = 1
        _PasmoNasycenie ("Nasycenie gór", Float) = 1.15
        _PasmoMgla ("Mgiełka gór", Float) = .18
        _Grzbiety ("Grzbiety", 2D) = "black" {}
        _GrzbietyR ("R1 R2 H1 H2", Vector) = (850, 1450, 160, 320)
        _GrzbietKolor1 ("Grzbiet 1", Color) = (.11, .17, .08, 1)
        _GrzbietKolor2 ("Grzbiet 2", Color) = (.10, .15, .09, 1)
        _ZiemiaMakro ("Ziemia makro", 2D) = "grey" {}
        _ZiemiaMakroSkala ("Makro: m na kafel", Float) = 1600
        _ZiemiaTrawa ("Trawa tła", 2D) = "grey" {}
        _ZiemiaTrawaSkala ("Trawa tła: m", Float) = 3.2
        _ZiemiaOdcien ("Odcień trawy tła", Color) = (.9, 1, .85, 1)
        _Brzeg1 ("Brzeg 1", Vector) = (84, 150, 100, 118)
        _Brzeg2 ("Brzeg 2", Vector) = (460, -340, .9, -1.7)
        _MorzeKolor ("Morze", Color) = (.035, .085, .11, 1)
        _MgielkaKolor ("Mgiełka", Color) = (.62, .68, .76, 1)
        _MgielkaGestosc ("Mgiełka gęstość", Float) = .00042
        _MgielkaNieboUdzial ("Mgiełka: niebo", Float) = .6
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" "DisableBatching"="True" }
        LOD 200
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "MiastoTlo.cginc"
            sampler2D _MainTex;
            half4 _Color;
            half _Cutoff, _Jasnosc;
            float _MglaOd, _MglaDo;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 rog : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                // siatka w układzie świata (obiekt bez przesunięcia i obrotu): podstawa + róg obrócony do kamery
                float3 baza = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 doKam = _WorldSpaceCameraPos - baza; doKam.y = 0;
                float l = length(doKam);
                float3 f = l > 1e-3 ? doKam / l : float3(0, 0, 1);
                float3 r = float3(f.z, 0, -f.x);
                o.wp = baza + r * v.rog.x + float3(0, v.rog.y, 0);
                o.pos = mul(UNITY_MATRIX_VP, float4(o.wp, 1));
                o.uv = v.uv;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                half4 t = tex2D(_MainTex, i.uv) * _Color;
                clip(t.a - _Cutoff);
                float3 v = i.wp - _WorldSpaceCameraPos;
                float d = max(length(v), 1e-3);
                float3 dir = v / d;
                half3 c = t.rgb * _Jasnosc;
                c = lerp(c, MT_Mgielka(dir), MT_MgielkaIle(d));
                float koniec = min(_ProjectionParams.z * .95, _MglaDo), poczatek = koniec * _MglaOd;
                half k = smoothstep(poczatek, koniec, d);
                if (k > 0.001) c = lerp(c, MiastoTlo(_WorldSpaceCameraPos, dir, MT_KatPiksela()), k);
                return half4(c, 1);
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
