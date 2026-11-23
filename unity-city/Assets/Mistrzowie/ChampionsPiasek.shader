// BRAXHORN MIASTO MISTRZÓW — piasek plaży (właściciel 23.11 19:12: "chcę taki piasek"; 19:27: "te ścieżki usuń z plaży
// całkowicie"; 19:36: "ten piasek tu lepiej wyglądał ... taki nierówny"). Jeden materiał na suchą i mokrą plażę,
// rzutowany w jednostkach świata (XZ), bez szwu między nimi:
// - suchy: Poly Haven Aerial Sand (15 m zdeptanego piasku plaży, ślady stóp wszędzie) bez śladów kół (morze_plaza.py
//   złożył mapę z kawałków bez kół), obrócony drugi raz i mieszany dużym szumem, żeby powtórzenie nie ułożyło się w linie;
// - mokry: tam, dokąd dobiega fala (do 0,27 j. nad morzem i trochę wyżej, brzeg postrzępiony szumem) ten sam zdeptany
//   piasek, ciemniejszy i szarszy, ślady wygładzone, z drobną fakturą Poly Haven Damp Beach Sand (2 m); nad nim wilgotny,
//   nieco ciemniejszy pas; mokry piasek błyszczy (odbija niebo);
// - jasny, kremowy piasek plaży (średnia suchego 216,205,186 sRGB).
Shader "Braxhorn/ChampionsPiasek"
{
    Properties
    {
        _MainTex ("Suchy: Aerial Sand bez kół", 2D) = "white" {}
        _BumpMap ("Suchy: normalna (GL, RGB)", 2D) = "bump" {}
        _Mokry ("Mokry: Damp Beach Sand", 2D) = "gray" {}
        _MokryBump ("Mokry: normalna (GL, RGB)", 2D) = "bump" {}
        _Szum ("Szum (G)", 2D) = "gray" {}
        _SuchyTint ("Suchy: mnożnik (liniowo)", Vector) = (1.86,2.55,2.16,1)
        _MokryTint ("Mokry: mnożnik (liniowo)", Vector) = (2.99,3.29,4.03,1)
        _Kafle ("j. na kafel: suchy, suchy obrócony, -, mokry", Vector) = (19.42,14.24,0,2.589)
        _Poziom ("Poziom morza (y)", Float) = -2.589
        _Mokro ("Mokry do, wilgotny do (j. nad morzem)", Vector) = (.29,.47,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _Mokry, _MokryBump, _Szum;
        float4 _SuchyTint, _MokryTint, _Kafle, _Mokro;
        float _Poziom;
        struct Input { float3 worldPos; };

        float3 Gl(float4 t) { return t.xyz * 2 - 1; }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 w = IN.worldPos.xz;
            float h = IN.worldPos.y - _Poziom;
            // the dry beach: two turns of the 15 m map mixed by a large noise
            float2 u1 = w / _Kafle.x;
            float2 u2 = float2(dot(w, float2(.292, .956)), dot(w, float2(-.956, .292))) / _Kafle.y + float2(.31, .67);
            float mixk = smoothstep(.35, .65, tex2D(_Szum, w / 90).g);
            float2 uw = w / _Kafle.w;
            float3 dry = lerp(tex2D(_MainTex, u1).rgb, tex2D(_MainTex, u2).rgb, mixk) * _SuchyTint.rgb;
            float3 wet = tex2D(_Mokry, uw).rgb * _MokryTint.rgb;
            // normals: the photo's (the second turn turned back into the mesh's frame: u along +x, v along +z)
            float3 n1 = Gl(tex2D(_BumpMap, u1)), n2 = Gl(tex2D(_BumpMap, u2));
            n2.xy = float2(.292 * n2.x - .956 * n2.y, .956 * n2.x + .292 * n2.y);
            float3 nd = normalize(lerp(n1, n2, mixk));
            float3 nw = Gl(tex2D(_MokryBump, uw));
            // where the waves reach: wet, a frayed edge; above it a damp, slightly darker band
            float n = tex2D(_Szum, w / 9).g;
            float wk = 1 - smoothstep(_Mokro.x - .04, _Mokro.x + .04, h + (n - .5) * .10);
            float damp = 1 - smoothstep(_Mokro.x + .04, _Mokro.y + .13, h + (n - .5) * .16);
            // the wet beach keeps the footprints (softened), darker and greyer, with the damp sand's own fine texture
            float3 wetAlb = dry * float3(.60, .61, .65) * lerp(float3(1, 1, 1), wet / max(dot(wet, float3(.333, .333, .333)), .001), .35);
            o.Albedo = lerp(dry * lerp(1, .90, damp), lerp(wetAlb, wet, .25), wk);
            float3 nn = normalize(lerp(nd, normalize(lerp(nd, float3(0, 0, 1), .45) + nw * float3(.5, .5, 0)), wk));
            o.Normal = normalize(lerp(float3(0, 0, 1), nn, .8));
            o.Metallic = 0;
            o.Smoothness = lerp(.08, .62, wk);
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
