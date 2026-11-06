Shader "BRAXHORN/ObozFootprint"
{
    Properties
    {
        _MainTex ("Sole alpha", 2D) = "white" {}
        _Color ("Soil tint and fade", Color) = (0.32,0.25,0.18,0.76)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            UNITY_INSTANCING_BUFFER_START(Stamp)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Stamp)
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position=UnityObjectToClipPos(input.vertex);
                output.uv=input.uv;
                UNITY_TRANSFER_FOG(output,output.position);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                fixed4 tint=UNITY_ACCESS_INSTANCED_PROP(Stamp,_Color);
                fixed4 color=tex2D(_MainTex,input.uv)*tint;
                UNITY_APPLY_FOG(input.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
