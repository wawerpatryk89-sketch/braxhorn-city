// BRAXHORN build 14 — teren za płotem osiedla (łąki, pola, wzgórze nad tunelem).
// Trawa (fotoskan Grass004) z plamami koloru w dużej skali (bez powtarzalnego wzoru), skała na stromych miejscach
// (kolor wierzchołka R), ziemia/ściółka pod drzewami i na ścieżkach (G), odcień pola (B: 0 = łąka, 1 = suchsze pole).
// Z odległością teren przechodzi DOKŁADNIE w kolor panoramy za nim (ten sam pas krajobrazu co w niebie,
// OsiedlePanorama.cginc) — granica zasięgu kamery i koniec siatki są niewidoczne, łąka wtapia się w pola na zdjęciu.
// Bliżej zwykła mgła sceny (jak reszta osiedla). Bez świecenia.
Shader "Braxhorn/OsiedleDal"
{
    Properties
    {
        _MainTex ("Trawa (albedo)", 2D) = "white" {}
        _Color ("Odcień trawy", Color) = (.9, 1, .88, 1)
        _Tile ("Kafel trawy (m)", Float) = 3.5
        _RockTex ("Skała (albedo)", 2D) = "grey" {}
        _RockColor ("Odcień skały", Color) = (.8, .78, .74, 1)
        _RockTile ("Kafel skały (m)", Float) = 4
        _SoilTex ("Ziemia ze ściółką (albedo)", 2D) = "grey" {}
        _SoilColor ("Odcień ziemi", Color) = (.75, .7, .62, 1)
        _SoilTile ("Kafel ziemi (m)", Float) = 2.5
        _FieldColor ("Odcień pola (suchsza trawa)", Color) = (1.08, 1.0, .72, 1)
        _MacroTile ("Plamy koloru (m)", Float) = 37
        _MacroMix ("Siła plam", Range(0, 1)) = .35
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert finalcolor:panorama nofog
        #pragma target 3.0
        #include "OsiedlePanorama.cginc"
        sampler2D _MainTex, _RockTex, _SoilTex;
        fixed4 _Color, _RockColor, _SoilColor, _FieldColor;
        float _Tile, _RockTile, _SoilTile, _MacroTile, _MacroMix, _MglaOd, _MglaDo, _PasmoJest;
        struct Input { float3 worldPos; float4 color : COLOR; float3 worldNormal; };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 xz = IN.worldPos.xz;
            fixed3 g = tex2D(_MainTex, xz / _Tile).rgb * _Color.rgb;
            // plamy w dwóch skalach z tej samej tekstury (bez dodatkowej mapy)
            fixed m1 = dot(tex2D(_MainTex, xz / _MacroTile + .31).rgb, fixed3(.3, .59, .11));
            fixed m2 = dot(tex2D(_MainTex, xz / (_MacroTile * 3.7) + .77).rgb, fixed3(.3, .59, .11));
            g *= lerp(1, (m1 + m2) * 2.2, _MacroMix);
            g = lerp(g, g * _FieldColor.rgb, IN.color.b);
            // skała: rzut trójplanarny (strome zbocza bez rozciągnięcia)
            fixed3 c = g;
            if (IN.color.g > .01) c = lerp(c, tex2D(_SoilTex, xz / _SoilTile).rgb * _SoilColor.rgb, IN.color.g);
            if (IN.color.r > .01)
            {
                float3 n = abs(normalize(IN.worldNormal)); n /= (n.x + n.y + n.z);
                fixed3 r = (tex2D(_RockTex, IN.worldPos.zy / _RockTile).rgb * n.x + tex2D(_RockTex, xz / _RockTile).rgb * n.y + tex2D(_RockTex, IN.worldPos.xy / _RockTile).rgb * n.z) * _RockColor.rgb;
                c = lerp(c, r, IN.color.r);
            }
            o.Albedo = c;
            o.Alpha = 1;
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
    FallBack "Diffuse"
}
