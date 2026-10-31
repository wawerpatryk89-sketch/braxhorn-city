// BRAXHORN R2 — okna domów, do których się nie wchodzi: „pokój za szybą” (interior mapping).
// Szyba sama rysuje pomieszczenie: tylną ścianę z meblami (atlas 8 wariantów), boczne ściany, podłogę z desek
// i sufit z lampą — z prawdziwą perspektywą (promień z kamery biegnie w głąb pokoju). Pokoje to siatka
// w metrach świata (szerokość, wysokość kondygnacji, głębokość), każdy pokój losuje wariant, kolor ścian,
// firanki i żaluzje. Na wierzchu odbicie nieba (gładka szyba). Wnętrze ma jasność dnia za szybą (nie świeci).
Shader "Braxhorn/OsiedleOkno"
{
    Properties
    {
        _Rooms ("Atlas tylnych ścian (4 x 2)", 2D) = "white" {}
        _Variant ("Pierwszy wariant", Float) = 0
        _Variants ("Liczba wariantów", Float) = 5
        _CellW ("Szerokość pokoju (m)", Float) = 3.6
        _CellH ("Wysokość kondygnacji (m)", Float) = 3.4
        _Floor0 ("Poziom pierwszej podłogi (m)", Float) = 0
        _Depth ("Głębokość pokoju (m)", Float) = 4.2
        _RoomLight ("Jasność wnętrza", Range(0,1)) = .48
        _Tint ("Odcień szyby", Color) = (.86,.92,.95,1)
        _Gloss ("Gładkość szyby", Range(0,1)) = .95
        _Curtains ("Udział okien z firankami", Range(0,1)) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _Rooms;
        float _Variant, _Variants, _CellW, _CellH, _Floor0, _Depth, _RoomLight, _Gloss, _Curtains;
        fixed4 _Tint;
        struct Input { float3 worldPos; float3 worldNormal; };

        float Hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = IN.worldNormal;
            float3 N = abs(n.x) > abs(n.z) ? float3(sign(n.x), 0, 0) : float3(0, 0, sign(n.z));
            float3 T = float3(N.z, 0, -N.x);
            float3 V = normalize(IN.worldPos - _WorldSpaceCameraPos);
            float u = dot(IN.worldPos, T) / _CellW, v = (IN.worldPos.y - _Floor0) / _CellH;
            float cu = floor(u), cv = floor(v);
            float3 p = float3(frac(u), frac(v), 0);
            float3 rd = float3(dot(V, T) / _CellW, V.y / _CellH, max(dot(V, -N) / _Depth, 1e-3));
            float tx = rd.x > 0 ? (1 - p.x) / rd.x : -p.x / min(rd.x, -1e-5);
            float ty = rd.y > 0 ? (1 - p.y) / rd.y : -p.y / min(rd.y, -1e-5);
            float tz = 1 / rd.z;
            float t = min(min(tx, ty), tz);
            float3 h = saturate(p + rd * t);
            float id = Hash(float3(cu, cv, floor(dot(IN.worldPos, N) * 2)));
            float3 wallC = lerp(float3(.87, .84, .79), float3(.80, .84, .87), frac(id * 7.31));
            wallC = lerp(wallC, float3(.89, .83, .74), step(.72, frac(id * 13.7)));
            float lod = clamp(log2(distance(IN.worldPos, _WorldSpaceCameraPos) * .3), 0, 6);
            float3 col;
            if (t == tz)
            {
                float k = _Variant + floor(id * _Variants * .9999);
                float2 tile = float2(fmod(k, 4), floor(k / 4));
                float2 uv = float2((tile.x + h.x) * .25, 1 - (tile.y + 1 - h.y) * .5);
                float4 a = tex2Dlod(_Rooms, float4(uv, 0, lod));
                col = lerp(wallC, a.rgb, a.a) * .93;
            }
            else if (t == tx)
            {
                col = wallC * .8;
            }
            else if (rd.y < 0)
            {
                float row = floor(h.z * 3);
                float pk = h.x * 6 + row * .37;
                float3 wood = lerp(float3(.52, .38, .26), float3(.64, .49, .33), Hash(float3(floor(pk), row, cu + cv * 17)));
                col = wood * (frac(pk) < .04 ? .72 : 1);
            }
            else
            {
                col = float3(.93, .93, .92);
                float2 lc = float2(h.x - .5, (h.z - .5) * 1.2);
                col = lerp(col, float3(.74, .73, .71), step(length(lc), .07));
            }
            col *= lerp(1, .55, h.z);
            // firanki po bokach i żaluzje u góry (w części pokoi)
            float cur = step(id, _Curtains) * lerp(.08, .22, frac(id * 3.7));
            float3 fabric = lerp(float3(.93, .91, .86), float3(.72, .68, .62), frac(id * 5.1)) * (.9 + .1 * sin(p.x * 140));
            col = lerp(col, fabric, saturate(step(p.x, cur) + step(1 - cur, p.x)));
            float blind = step(frac(id * 17.3), .3) * .35;
            float slat = .85 + .15 * step(.5, frac(p.y * 40));
            col = lerp(col, float3(.86, .86, .83) * slat, step(1 - blind, p.y) * step(.001, blind));
            float fres = pow(1 - saturate(dot(-V, N)), 4);
            o.Albedo = _Tint.rgb * .02;
            o.Emission = col * _RoomLight * _Tint.rgb * (1 - fres * .8);
            o.Smoothness = _Gloss; o.Metallic = 0; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
