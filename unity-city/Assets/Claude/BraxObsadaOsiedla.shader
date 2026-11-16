Shader "Braxhorn/Obsada Osiedla"
{
    Properties
    {
        _MainTex ("Kolor źródłowy", 2D) = "white" {}
        _BumpMap ("Normalne", 2D) = "bump" {}
        _MetalSmooth ("Metal w R, gładkość w A", 2D) = "black" {}
        _FaceTex ("Fazy powiek i warg", 2D) = "black" {}
        _Blink ("Powieki", Range(0,1)) = 0
        _Speech ("Usta", Range(0,1)) = 0
        // Mnożnik gładkości. 1 = dokładnie to, co jest w mapie (zachowanie bez zmian).
        // Zmierzone 16.11: u Patryka 52 % powierzchni ma gładkość 0,4–0,5, a odchylenie w całej
        // mapie to 0,11 — czyli skóra, koszula, spodnie i buty odbijają światło prawie tak samo.
        // Oko czyta jeden połysk na wszystkim jako plastik. Ten mnożnik ścina poziom, zachowując
        // różnice tam, gdzie w mapie są. Metaliczności NIE rusza.
        _Gladkosc ("Mnożnik gładkości (1 = jak w mapie)", Range(0,1)) = 1
        // Metaliczność. Zmierzone 16.11: imprezowiczka ma 0,60 na CAŁEJ postaci — skóra
        // i tkanina nie są metalem, a przy takiej wartości materiał traci własny kolor
        // i staje się ciemnym lustrem. To znany artefakt generatorów 3D (Tripo).
        // 0 = metaliczność wyłączona, 1 = dokładnie to, co w mapie.
        _MetalMnoznik ("Mnożnik metaliczności", Range(0,1)) = 1
        // Siła mapy normalnych. Zmierzone: u Ewy 74 % pikseli nie ma żadnej faktury
        // (odchylenie 0,021), u Patryka, bramkarza i imprezowiczki niewiele lepiej.
        // Płaska mapa normalnych nie rozbija odblasku, więc skóra odbija jak lakier.
        // Podbicie XY zwiększa mikrofakturę bez ruszania map.
        _SilaNormalnych ("Siła mapy normalnych", Range(0.5,3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        sampler2D _MainTex, _BumpMap, _MetalSmooth, _FaceTex;
        half _Blink, _Speech;
        half _Gladkosc, _MetalMnoznik, _SilaNormalnych;
        struct Input { float2 uv_MainTex; float2 uv2_FaceTex; };
        half4 phase(float2 coord, float n)
        {
            float2 tile = float2(fmod(n,4.0),1.0-floor(n/4.0));
            return tex2D(_FaceTex,(tile+clamp(coord,.002,.998))/float2(4,2));
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            half4 packedMap = tex2D(_MetalSmooth,uv);
            half3 color = tex2D(_MainTex,uv).rgb;
            float2 face = IN.uv2_FaceTex;
            half weight = face.y>.5 ? _Blink : _Speech;
            if (weight>.0001 && all(face>=0) && all(face<=1))
            {
                float f=saturate(weight)*7;
                half4 a=phase(face,floor(f)), b=phase(face,min(7.0,floor(f)+1.0));
                color=lerp(lerp(color,a.rgb,a.a),lerp(color,b.rgb,b.a),frac(f));
            }
            o.Albedo=color;
            half3 nrm = UnpackNormal(tex2D(_BumpMap,uv));
            nrm.xy *= _SilaNormalnych;
            o.Normal = normalize(nrm);
            o.Metallic=packedMap.r*_MetalMnoznik;
            o.Smoothness=packedMap.a*_Gladkosc;
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
