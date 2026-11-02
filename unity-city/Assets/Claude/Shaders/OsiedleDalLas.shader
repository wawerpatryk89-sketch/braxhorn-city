// BRAXHORN build 14 — drzewa w oddali za płotem (dalsze plany lasu na wzgórzach): jedna karta na drzewo, zawsze
// zwrócona do kamery (obrót wokół pionu w shaderze), obraz drzewa złożony z tych samych gałązek co korony drzew
// osiedla (unity-tools/claude/drzewo_klaster.py, las.png). Światło i cień korony w obrazie; z odległością kolor
// przechodzi w panoramę za drzewem (jak teren — OsiedleDal), więc dalsze plany lasu wtapiają się w góry na zdjęciu.
// Wierzchołek: POSITION = podstawa drzewa (wszystkie 4 rogi), TEXCOORD1.xy = przesunięcie rogu (szerokość, wysokość).
Shader "Braxhorn/OsiedleDalLas"
{
    Properties
    {
        _MainTex ("Drzewa (RGBA)", 2D) = "white" {}
        _Color ("Odcień", Color) = (1, 1, 1, 1)
        _Cutoff ("Próg alfy", Range(0, 1)) = .45
        _MglaOd ("Panorama od (ułamek końca)", Float) = .58
        _MglaDo ("Panorama w pełni: najdalej (m)", Float) = 420
        _PasmoJest ("Panorama włączona", Float) = 1
        [NoScaleOffset] _Pasmo ("Pas krajobrazu", 2D) = "black" {}
        _PasmoSrodek ("Środek walca panoramy", Vector) = (0, 0, -67, 0)
        _PasmoPromien ("Promień walca (m)", Float) = 2500
        _PasmoU0 ("Przesunięcie u", Float) = .02
        _PasmoGora ("Górna elewacja", Float) = 35
        _PasmoDol ("Dolna elewacja", Float) = -10
        _PasmoJasnosc ("Jasność krajobrazu", Float) = 1
        _PasmoNasycenie ("Nasycenie krajobrazu", Float) = 1.2
        _PasmoMgla ("Mgiełka przy horyzoncie", Range(0, 1)) = .12
        _PasmoMglaKolor ("Kolor mgiełki", Color) = (.6, .66, .75, 1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" "DisableBatching"="True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alphatest:_Cutoff finalcolor:panorama nofog noshadow
        #pragma target 3.0
        #include "OsiedlePanorama.cginc"
        sampler2D _MainTex;
        fixed4 _Color;
        float _MglaOd, _MglaDo, _PasmoJest;
        struct Input { float2 uv_MainTex; float3 worldPos; };

        void vert (inout appdata_full v)
        {
            // siatka w układzie świata (obiekt bez przesunięcia i obrotu): podstawa + róg obrócony do kamery
            float3 baza = v.vertex.xyz;
            float3 doKam = _WorldSpaceCameraPos - baza; doKam.y = 0;
            float l = length(doKam);
            float3 f = l > 1e-3 ? doKam / l : float3(0, 0, 1);
            float3 r = float3(f.z, 0, -f.x);
            v.vertex.xyz = baza + r * v.texcoord1.x + float3(0, v.texcoord1.y, 0);
            v.normal = normalize(f * .35 + float3(0, 1, 0));   // korona oświetlona od góry (światło w obrazie)
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        void panorama (Input IN, SurfaceOutput o, inout fixed4 color)
        {
            float3 v = IN.worldPos - _WorldSpaceCameraPos;
            float d = length(v);
            half4 land = PanoramaKierunek(_WorldSpaceCameraPos, v / max(d, 1e-3));
            // bliżej: zwykła mgła sceny (jak budynki osiedla), dalej: kolor zdjęcia dokładnie za tym miejscem; koniec przejścia
            // tuż przed zasięgiem kamery trybu grafiki (NISKA 220 m → 121…209 m, ŚREDNIA/WYSOKA → 244…420 m) — brzeg niewidoczny
            half fs = 1 - exp2(-(unity_FogParams.x * d) * (unity_FogParams.x * d));
            float koniec = min(_ProjectionParams.z * .95, _MglaDo), poczatek = koniec * _MglaOd;
            half3 cel = lerp(unity_FogColor.rgb, land.rgb, smoothstep(poczatek * .4, koniec, d) * _PasmoJest);
            half k = max(fs, smoothstep(poczatek, koniec, d) * _PasmoJest);
            #ifdef UNITY_PASS_FORWARDADD
                color.rgb *= 1 - k;
            #else
                color.rgb = lerp(color.rgb, cel, k);
            #endif
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
