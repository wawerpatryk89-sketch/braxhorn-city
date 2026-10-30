// BRAXHORN CITY: podłoże z prawdziwych fotoskanów (trawa, piasek) z teksturą rzutowaną
// w metrach świata (XZ), więc skala jest poprawna niezależnie od UV starej siatki.
// Dwie skale próbkowania (detal + duża plama) usuwają widoczne powtarzanie kafla.
// Nic nie świeci (brak emisji).
Shader "Braxhorn/ClaudeWorldGround"
{
    Properties
    {
        _MainTex ("Kolor (albedo)", 2D) = "white" {}
        _BumpMap ("Normalna (GL)", 2D) = "bump" {}
        _ARM ("AO / Roughness / Metal", 2D) = "white" {}
        _Color ("Odcień", Color) = (1,1,1,1)
        _Tile ("Metry na kafel", Float) = 2.5
        _MacroTile ("Metry na dużą plamę", Float) = 23
        _MacroMix ("Siła dużej plamy", Range(0,1)) = .35
        _BumpScale ("Siła wypukłości", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _ARM;
        fixed4 _Color;
        float _Tile, _MacroTile, _MacroMix, _BumpScale;
        struct Input { float3 worldPos; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz / _Tile;
            float2 uvM = IN.worldPos.xz / _MacroTile + float2(.37, .11);
            fixed4 c = tex2D(_MainTex, uv);
            fixed3 m = tex2D(_MainTex, uvM).rgb;
            c.rgb = lerp(c.rgb, c.rgb * (m * 1.6), _MacroMix);
            fixed3 arm = tex2D(_ARM, uv).rgb;
            o.Albedo = c.rgb * _Color.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            o.Metallic = 0;
            o.Smoothness = saturate(1 - arm.g);
            o.Occlusion = arm.r;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
