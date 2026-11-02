// Promień lasera / snop światła w klubie NOVA (wyjątek „glow” tylko w klubie): siatka (płaskie pasy), mieszanie
// addytywne, jasność gaśnie wzdłuż promienia (uv.y: 0 przy głowicy → 1 na końcu) i ku brzegom pasa (uv.x).
// Bez cząsteczek (moduł ParticleSystem jest w projekcie wyłączony), bez zapisu głębi, obustronnie.
Shader "Braxhorn/WnetrzaPromien"
{
    Properties
    {
        _Tint("Barwa promienia", Color) = (0.2,1,0.3,1)
        _Moc("Jasność", Range(0,4)) = 1.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Tint; half _Moc;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                half brzeg = 1 - abs(i.uv.x * 2 - 1);
                half wzdluz = saturate(1 - i.uv.y);
                half a = brzeg * brzeg * (0.25 + 0.75 * wzdluz) * _Moc;
                return fixed4(_Tint.rgb * a, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
