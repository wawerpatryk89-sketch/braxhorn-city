Shader "Braxhorn/MistrzowieBeachFoam"
{
    Properties
    {
        _FoamTint ("Warm white natural shore foam", Color) = (.98,.99,.96,1)
        _WaveStrength ("Foam wash movement", Range(0,1.5)) = .72
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+15" "IgnoreProjector"="True" }
        LOD 110
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _FoamTint;
            float _WaveStrength;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };
            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = mul(unity_ObjectToWorld,v.vertex).xyz;
                // Only the watery part of the foam follows the low water swell.
                // The dry part remains on the sculpted shore and has no collider.
                float waterSide=1.0-saturate(v.uv.y);
                float t=_Time.y;
                float phase=dot(pos.xz,float2(.018,.052))-t*.68;
                pos.y += (sin(phase)*.06+sin(phase*1.71+.4)*.023)
                       * waterSide * _WaveStrength;
                o.pos=mul(UNITY_MATRIX_VP,float4(pos,1));
                o.uv=v.uv;
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float t=_Time.y;
                float shore=i.uv.y;
                float wave=sin(i.uv.x*.65-t*1.08)
                          +.36*sin(i.uv.x*1.81-t*.75+1.4);
                float crest=.48+.13*wave;
                float breakup=.45+.55*abs(sin(i.uv.x*3.11-t*.25)
                                      *cos(i.uv.x*1.36+t*.19));
                // No hard, opaque white stripe. Active broken froth slowly
                // advances and recedes across real wet sand.
                float width=.17+.07*breakup;
                float band=1.0-smoothstep(0.0,width,abs(shore-crest));
                float taper=smoothstep(.02,.12,shore)
                             *(1.0-smoothstep(.85,1.0,shore));
                float bubbles=.63+.37*sin(i.uv.x*7.0-t*1.4)*sin(i.uv.x*2.71+t*.8);
                float alpha=saturate(band*taper*breakup*bubbles*.72*_WaveStrength);
                fixed4 outColor=fixed4(_FoamTint.rgb,alpha);
                UNITY_APPLY_FOG(i.fogCoord,outColor);
                return outColor;
            }
            ENDCG
        }
    }
    Fallback Off
}
