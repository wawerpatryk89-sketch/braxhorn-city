// BRAXHORN R2 — niebo: panorama 360° (jak Skybox/Panoramic: to samo mapowanie i obrót) z podbitym nasyceniem błękitu.
// Nasycenie liczone w przestrzeni liniowej wokół luminancji: c' = Y + (c - Y) * _Saturation — chmury (szare/białe,
// c ≈ Y) zostają białe, błękit robi się żywszy. Wartość z pomiaru zdjęć z gry (build 2: błękit sRGB 109/126/152,
// nasycenie 0.28) → cel nasycenie 0.50 (sRGB 87/127/176) → _Saturation 2.1. Nic nie świeci (to niebo, tło).
// Build 14 (właściciel: „za płotami realna panorama — góry, lasy, pola, z każdej strony inny widok, żadnych sztucznych
// rzeczy”): pod niebem pas krajobrazu z prawdziwego zdjęcia 360° (OsiedlePanorama.cginc) sklejony po masce ziemi —
// nad górami nasze niebo z płynącymi chmurami i słońcem gry (góry zasłaniają słońce), poniżej linii gór krajobraz.
// Bez pasa (_PasmoJest 0) jak dawniej: poniżej horyzontu kolor mgły.
Shader "Braxhorn/OsiedleNiebo"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Panorama (2D, równoodległościowa)", 2D) = "grey" {}
        _Exposure ("Ekspozycja", Range(0, 8)) = 1.05
        _Rotation ("Obrót (stopnie)", Range(0, 360)) = 40
        _Saturation ("Nasycenie (mnożnik wokół luminancji)", Range(0, 4)) = 2.1
        _Chmury ("Ruch chmur (stopnie na sekundę)", Range(0, 2)) = .22
        _SloRozmiar ("Słońce: promień (stopnie)", Range(0, 5)) = 1.1
        _SloKolor ("Słońce: kolor", Color) = (1, .97, .88, 1)
        _SloPoswiata ("Słońce: poświata", Range(0, 2)) = .55
        [NoScaleOffset] _Pasmo ("Pas krajobrazu (RGBA, A = ziemia)", 2D) = "black" {}
        _PasmoJest ("Pas krajobrazu włączony", Float) = 0
        _PasmoSrodek ("Środek walca panoramy (xyz)", Vector) = (0, 0, -67, 0)
        _PasmoPromien ("Promień walca (m)", Float) = 2500
        _PasmoU0 ("Przesunięcie u (obrót krajobrazu)", Float) = .02
        _PasmoGora ("Górna elewacja pasa (st.)", Float) = 35
        _PasmoDol ("Dolna elewacja pasa (st.)", Float) = -10
        _PasmoJasnosc ("Jasność krajobrazu", Float) = 1
        _PasmoNasycenie ("Nasycenie krajobrazu", Float) = 1.2
        _PasmoMgla ("Mgiełka przy horyzoncie", Range(0, 1)) = .12
        _PasmoMglaKolor ("Kolor mgiełki", Color) = (.6, .66, .75, 1)
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
            #include "OsiedlePanorama.cginc"
            sampler2D _MainTex;
            half _Exposure, _Saturation, _SloPoswiata;
            float _Rotation, _Chmury, _SloRozmiar, _PasmoJest;
            half4 _SloKolor;
            struct appdata_t { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float3 texcoord : TEXCOORD0; float3 ruch : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa; sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }
            inline float2 ToRadialCoords (float3 coords)
            {
                float3 n = normalize(coords);
                float latitude = acos(n.y);
                float longitude = atan2(n.z, n.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }
            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                // chmury płyną (właściciel 1.11, 18:25): górna część nieba obraca się powoli; dół (horyzont panoramy) stoi
                o.ruch = RotateAroundYInDegrees(v.vertex.xyz, -fmod(_Time.y * _Chmury, 360.0));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                half ny0 = normalize(i.texcoord).y;
                half3 c = lerp(tex2D(_MainTex, ToRadialCoords(i.texcoord)).rgb, tex2D(_MainTex, ToRadialCoords(i.ruch)).rgb, smoothstep(.10, .32, ny0));
                half y = dot(c, half3(.2126, .7152, .0722));
                c = max(0, y + (c - y) * _Saturation) * _Exposure;
                // słońce (właściciel 1.11: „słońce powinno być gdzieś”) w kierunku światła dziennego (RenderSettings.sun):
                // tarcza z miękką krawędzią + poświata; kierunek świata = wierzchołek obrócony jak obraz nieba
                float3 wd = normalize(RotateAroundYInDegrees(i.texcoord, _Rotation));
                float cs = dot(wd, normalize(_WorldSpaceLightPos0.xyz));
                float rr = radians(_SloRozmiar);
                half tarcza = smoothstep(cos(rr * 1.35), cos(rr), cs);
                half poswiata = (pow(saturate(cs), 48.0) * .22 + pow(saturate(cs), 700.0) * .65) * _SloPoswiata;
                c = lerp(c, _SloKolor.rgb * 1.5, tarcza) + _SloKolor.rgb * poswiata;
                if (_PasmoJest > .5)
                {
                    // krajobraz z prawdziwego zdjęcia: góry, lasy, pola (maska A) — zasłania niebo i słońce
                    half4 land = PanoramaKierunek(_WorldSpaceCameraPos, wd);
                    c = lerp(c, land.rgb, land.a);
                }
                else
                {
                    // poniżej horyzontu (za płotem / poza zasięgiem kamery na NISKIEJ): kolor mgły zamiast nieba „pod ziemią”
                    half ny = normalize(i.texcoord).y;
                    c = lerp(c, unity_FogColor.rgb, saturate((0.03 - ny) / 0.16));
                }
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
