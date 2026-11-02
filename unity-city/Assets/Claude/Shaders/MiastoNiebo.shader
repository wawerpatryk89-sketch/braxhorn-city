// BRAXHORN CITY — niebo i panorama 360° miasta (wykonawca „horyzont”, 2.11). Zastępuje Skybox/Panoramic z obrazem
// Kloofendal Pure Sky, którego dolna połowa (odbite niebo) była „nieskończonym lustrem” za granicą miasta.
// Cała logika w MiastoTlo.cginc: niebo z chmurami u góry; w dół ziemia (ląd y = 0 / morze y = −1.7 wg linii brzegu),
// grzbiety wzgórz w 2 planach, góry ze zdjęcia 360°, mgiełka powietrza. Nic nie świeci, bez świateł, jeden przebieg.
Shader "Braxhorn/MiastoNiebo"
{
    Properties
    {
        [NoScaleOffset] _NieboTex ("Niebo (panorama 2:1)", 2D) = "grey" {}
        _NieboObrot ("Obrót nieba (rad)", Float) = 0
        _NieboEkspozycja ("Ekspozycja", Float) = 1
        _NieboNasycenie ("Nasycenie", Float) = 1.3
        _NieboChmury ("Ruch chmur (rad/s)", Float) = .0035
        _SloKierunek ("Kierunek do słońca", Vector) = (-.45, .78, -.42, 0)
        [NoScaleOffset] _Pasmo ("Góry ze zdjęcia (RGBA)", 2D) = "black" {}
        _PasmoJest ("Góry włączone", Float) = 0
        _PasmoSrodek ("Środek walców", Vector) = (17, 0, 49, 0)
        _PasmoPromien ("Promień walca gór (m)", Float) = 2600
        _PasmoU0 ("Przesunięcie u gór", Float) = -.23
        _PasmoGora ("Górna elewacja pasa", Float) = 35
        _PasmoDol ("Dolna elewacja pasa", Float) = -10
        _PasmoJasnosc ("Jasność gór", Float) = 1
        _PasmoNasycenie ("Nasycenie gór", Float) = 1.15
        _PasmoMgla ("Mgiełka gór przy horyzoncie", Range(0, 1)) = .18
        [NoScaleOffset] _Grzbiety ("Profile grzbietów (liniowa)", 2D) = "black" {}
        _GrzbietyR ("R1 R2 H1 H2", Vector) = (850, 1450, 160, 320)
        _GrzbietKolor1 ("Grzbiet 1: las", Color) = (.11, .17, .08, 1)
        _GrzbietKolor2 ("Grzbiet 2: las", Color) = (.10, .15, .09, 1)
        [NoScaleOffset] _ZiemiaMakro ("Ziemia z daleka (RGB, A = las)", 2D) = "grey" {}
        _ZiemiaMakroSkala ("Ziemia: metrów na kafel", Float) = 1600
        [NoScaleOffset] _ZiemiaTrawa ("Trawa z bliska", 2D) = "grey" {}
        _ZiemiaTrawaSkala ("Trawa: metrów na kafel", Float) = 3.2
        _ZiemiaOdcien ("Odcień trawy", Color) = (.9, 1, .85, 1)
        _Brzeg1 ("Brzeg: x płd, x płn, z0, z1 cypla", Vector) = (84, 150, 100, 118)
        _Brzeg2 ("Brzeg: łuk N, łuk S, nachylenie, poziom morza", Vector) = (460, -340, .9, -1.7)
        _MorzeKolor ("Morze z daleka", Color) = (.035, .085, .11, 1)
        _MgielkaKolor ("Mgiełka", Color) = (.62, .68, .76, 1)
        _MgielkaGestosc ("Mgiełka: gęstość (1/m)", Float) = .00042
        _MgielkaNieboUdzial ("Mgiełka: udział koloru nieba", Range(0, 1)) = .6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "MiastoTlo.cginc"
            struct appdata_t { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 c = MiastoTlo(_WorldSpaceCameraPos, d, MT_KatPiksela());
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
