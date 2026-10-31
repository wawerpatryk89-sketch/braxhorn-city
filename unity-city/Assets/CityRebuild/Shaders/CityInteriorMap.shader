// BRAXHORN CITY — "interior mapping": fałszywe pokoje za oknami budynków nieużytkowych (decyzja właściciela 31.10).
// Działa na czarnych płytach „Deep window interior” 0,5 m za szkłem elewacji: dla każdego piksela liczy promień
// od kamery do wnętrza sześciennego pokoju (szerokość/wysokość/głębokość w metrach) i próbkuje ścianę, podłogę,
// sufit z fotoskanów Poly Haven (white_plaster_02, wood_floor). Pokoje są oświetlone dziennym światłem z okna
// (jaśniej przy szybie, ciemniej w głębi), część ma zasłonięte rolety, część jest ciemna. Nic nie świeci.
// Siatka pokoi liczona w przestrzeni świata: pion wyrównany do kondygnacji elewacji (3,75 + k·3,2 m).
Shader "Brax/CityInteriorMap"
{
    Properties
    {
        _WallTex("Ściana (albedo)", 2D) = "white" {}
        _FloorTex("Podłoga (albedo)", 2D) = "white" {}
        _CeilTex("Sufit (albedo)", 2D) = "white" {}
        _WallTint("Ściana odcień", Color) = (.93,.91,.87,1)
        _FloorTint("Podłoga odcień", Color) = (.9,.84,.74,1)
        _CeilTint("Sufit odcień", Color) = (.97,.97,.96,1)
        _BlindTint("Rolety odcień", Color) = (.86,.84,.80,1)
        _RoomSize("Pokój: szerokość, wysokość, głębokość (m)", Vector) = (4.8,3.2,5.0,0)
        _RoomOffset("Siatka pokoi: przesunięcie X, Y (m)", Vector) = (0,.55,0,0)
        _TexMetres("Metry na powtórzenie tekstury: ściana, podłoga, sufit", Vector) = (3,3,3,0)
        _RoomLight("Światło dzienne przy oknie", Range(0,2)) = .34
        _BackFalloff("Spadek światła w głębi pokoju", Range(0,1)) = .42
        _BlindsFraction("Udział pokoi z roletami", Range(0,1)) = .28
        _DarkFraction("Udział ciemnych pokoi", Range(0,1)) = .18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _WallTex, _FloorTex, _CeilTex;
            half4 _WallTint, _FloorTint, _CeilTint, _BlindTint;
            float4 _RoomSize, _RoomOffset, _TexMetres;
            half _RoomLight, _BackFalloff, _BlindsFraction, _DarkFraction;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            half3 WorkingColour(half3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return GammaToLinearSpace(c);
                #else
                return c;
                #endif
            }
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y);
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.worldNormal);
                float3 wp = i.worldPos;
                float3 view = normalize(wp - _WorldSpaceCameraPos);
                half3 wallFlat = WorkingColour(_WallTint.rgb) * _RoomLight * .5;
                // Góra/dół płyty albo widok od tyłu: płaska ściana w półcieniu.
                if (abs(n.y) > .7 || dot(view, -n) <= .002)
                {
                    #ifdef UNITY_COLORSPACE_GAMMA
                    wallFlat = LinearToGammaSpace(wallFlat);
                    #endif
                    return half4(wallFlat, 1);
                }
                float3 t = normalize(cross(float3(0, 1, 0), n));
                float3 rs = _RoomSize.xyz;
                float2 facade = float2(dot(wp, t) - _RoomOffset.x, wp.y - _RoomOffset.y);
                float2 cell = floor(facade / rs.xy);
                float2 f = frac(facade / rs.xy) * 2 - 1;
                // Promień w przestrzeni pokoju (x wzdłuż elewacji, y w górę, z w głąb), znormalizowanej do sześcianu [-1,1].
                float3 rd = float3(dot(view, t), view.y, dot(view, -n)) / (rs * .5);
                rd += step(abs(rd), 1e-4) * 1e-4;
                float3 ro = float3(f.x, f.y, -1);
                float3 id = 1.0 / rd;
                float3 k = abs(id) - ro * id;
                float dmin = min(k.x, min(k.y, k.z));
                float3 hit = ro + rd * dmin;
                // Losowość pokoju: jasność, ciepło, rolety, ciemny.
                float seed = dot(n, float3(7.1, 0, 3.7));
                float h1 = Hash(cell + seed), h2 = Hash(cell * 1.7 + seed + 11.3), h3 = Hash(cell * .37 + seed + 29.1), h4 = Hash(cell * 2.3 + seed + 5.7);
                half3 warmth = lerp(half3(.92, .95, 1.0), half3(1.0, .94, .84), h2);
                float level = lerp(.72, 1.12, h1);
                float dark = step(h4, _DarkFraction);
                level *= lerp(1, .22, dark);
                // Metry na powierzchni trafionej ściany (tekstury powtarzane co _TexMetres).
                float3 metres = hit * rs * .5 + float3(cell.x * rs.x, cell.y * rs.y, 0);
                float2 uvBack = float2(metres.x, metres.y) / _TexMetres.x;
                float2 uvSide = float2(metres.z + cell.x * 3.1, metres.y) / _TexMetres.x;
                float2 uvFloor = float2(metres.x, metres.z + cell.y * 2.3) / _TexMetres.y;
                float2 uvCeil = float2(metres.x, metres.z) / _TexMetres.z;
                half3 back = WorkingColour(tex2D(_WallTex, uvBack).rgb) * WorkingColour(_WallTint.rgb);
                half3 side = WorkingColour(tex2D(_WallTex, uvSide).rgb) * WorkingColour(_WallTint.rgb) * .85;
                half3 floorC = WorkingColour(tex2D(_FloorTex, uvFloor).rgb) * WorkingColour(_FloorTint.rgb);
                half3 ceil = WorkingColour(tex2D(_CeilTex, uvCeil).rgb) * WorkingColour(_CeilTint.rgb) * .72;
                float isBack = step(k.z, dmin), isSide = step(k.x, dmin) * (1 - isBack);
                float isY = (1 - isBack) * (1 - isSide);
                float isFloor = isY * step(hit.y, 0), isCeil = isY * (1 - step(hit.y, 0));
                half3 surface = back * isBack + side * isSide + floorC * isFloor + ceil * isCeil;
                // Światło dzienne wpada przez okno: jaśniej przy szybie, ciemniej w głębi; podłoga przy oknie najjaśniejsza.
                float depth = saturate((hit.z + 1) * .5);
                float light = _RoomLight * lerp(1, _BackFalloff, depth) * level;
                light *= lerp(1, 1.15, isFloor * (1 - depth));
                half3 colour = surface * warmth * light;
                // Rolety: płaska tkanina tuż za szybą z delikatnymi poziomymi lamelami, lekko ciemniejsza u dołu.
                float blinds = step(h3, _BlindsFraction);
                float lamella = .92 + .08 * saturate(sin(facade.y * 48.0) * 3);
                half3 blindColour = WorkingColour(_BlindTint.rgb) * _RoomLight * .95 * lamella * lerp(.8, 1, f.y * .5 + .5) * lerp(1, .5, dark);
                colour = lerp(colour, blindColour, blinds);
                #ifdef UNITY_COLORSPACE_GAMMA
                colour = LinearToGammaSpace(max(colour, 0));
                #endif
                return half4(colour, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
