Shader "Braxhorn/ControlledBrandPrint"
{
    Properties
    {
        _MainTex("Original project brand pixels",2D)="white"{}
        _Color("Print tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 pixel=tex2D(_MainTex,i.uv);
                // Original project logos have a black rectangular backing.
                // Key only that backing at draw time; preserve the source asset
                // and all mark proportions, colours and typography.
                half key=smoothstep(.003,.025,max(pixel.r,max(pixel.g,pixel.b)));
                return fixed4(pixel.rgb*_Color.rgb,pixel.a*_Color.a*key);
            }
            ENDCG
        }
    }
    Fallback Off
}
