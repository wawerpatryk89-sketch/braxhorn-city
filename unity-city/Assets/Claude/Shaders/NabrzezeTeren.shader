// BRAXHORN CITY — teren nabrzeża i wyspy (przebudowa 2.11): JEDNA siatka, warstwy mieszane płynnie kolorem wierzchołka
// (R = suchy piasek, G = mokry piasek, B = trawa / ściółka, A = skała) zamiast osobnych siatek z „zębami piły” na styku.
// Tekstury rzutowane w metrach świata (XZ); skała dodatkowo z boku (stromizna w UV2.x), żeby urwiska nie były rozciągnięte.
// Duża plama (makro) usuwa widoczne powtarzanie kafla. Mokry piasek ciemniejszy i gładszy (połysk). Nic nie świeci.
Shader "Braxhorn/NabrzezeTeren"
{
    Properties
    {
        _Piasek ("Suchy piasek", 2D) = "white" {}
        _PiasekN ("Suchy piasek normalna", 2D) = "bump" {}
        _Mokry ("Mokry piasek", 2D) = "white" {}
        _Trawa ("Trawa / ściółka", 2D) = "white" {}
        _TrawaN ("Trawa normalna", 2D) = "bump" {}
        _Skala ("Skała", 2D) = "white" {}
        _SkalaN ("Skała normalna", 2D) = "bump" {}
        _KPiasek ("Odcień piasku", Color) = (1,1,1,1)
        _KMokry ("Odcień mokrego", Color) = (.78,.74,.68,1)
        _KTrawa ("Odcień trawy", Color) = (1,1,1,1)
        _KSkala ("Odcień skały", Color) = (1,1,1,1)
        _TPiasek ("Metry na kafel piasku", Float) = 2.4
        _TTrawa ("Metry na kafel trawy", Float) = 2.2
        _TSkala ("Metry na kafel skały", Float) = 4
        _Makro ("Metry na dużą plamę", Float) = 29
        _MakroMix ("Siła dużej plamy", Range(0,1)) = .32
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        sampler2D _Piasek, _PiasekN, _Mokry, _Trawa, _TrawaN, _Skala, _SkalaN;
        fixed4 _KPiasek, _KMokry, _KTrawa, _KSkala;
        float _TPiasek, _TTrawa, _TSkala, _Makro, _MakroMix;
        struct Input { float3 worldPos; float4 color : COLOR; float stromo; };
        // normalne zapisane jako zwykłe RGB (bez kompresji DXT5nm) — dekodowane ręcznie, tak samo na telefonie i w WebGL
        fixed3 Nrm(fixed4 c) { fixed3 n = c.xyz * 2 - 1; n.xy *= .8; return normalize(n); }

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.stromo = v.texcoord1.x;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 w = max(IN.color, 0);
            w /= max(dot(w, float4(1, 1, 1, 1)), .001);
            float2 xz = IN.worldPos.xz;
            float2 uP = xz / _TPiasek, uT = xz / _TTrawa;
            float2 uS = lerp(xz, float2(IN.worldPos.x + IN.worldPos.z, IN.worldPos.y), saturate(IN.stromo)) / _TSkala;
            fixed3 makro = tex2D(_Piasek, xz / _Makro + float2(.37, .11)).rgb * 1.6;
            fixed3 p = tex2D(_Piasek, uP).rgb * _KPiasek.rgb;
            fixed3 m = tex2D(_Mokry, uP * 1.13).rgb * _KMokry.rgb;
            fixed3 t = tex2D(_Trawa, uT).rgb * _KTrawa.rgb;
            fixed3 s = tex2D(_Skala, uS).rgb * _KSkala.rgb;
            fixed3 c = p * w.r + m * w.g + t * w.b + s * w.a;
            c = lerp(c, c * makro, _MakroMix);
            fixed3 nP = Nrm(tex2D(_PiasekN, uP));
            fixed3 nT = Nrm(tex2D(_TrawaN, uT));
            fixed3 nS = Nrm(tex2D(_SkalaN, uS));
            fixed3 nM = lerp(fixed3(0, 0, 1), nP, .35);           // mokry piasek wygładzony przez wodę
            o.Normal = normalize(nP * w.r + nM * w.g + nT * w.b + nS * w.a);
            o.Albedo = c;
            o.Metallic = 0;
            o.Smoothness = .10 * w.r + .52 * w.g + .08 * w.b + .18 * w.a;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
