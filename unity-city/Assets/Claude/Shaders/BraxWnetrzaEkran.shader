// Ekran / podświetlana tablica / klosz oprawy we wnętrzach (metro, lobby, klub, sklep): obraz bez oświetlenia sceny,
// bez emisji i bez poświaty (zasada „nic nie świeci” dotyczy materiałów z emisją — tu jasność = kolor tekstury × _Tint,
// tak jak wyświetlacz LED wygląda w dzień i w nocy tak samo). Bez przezroczystości, zapis głębi, mipmapy z tekstury.
Shader "Braxhorn/WnetrzaEkran"
{
    Properties
    {
        _MainTex("Obraz", 2D) = "white" {}
        _Tint("Jasność i barwa", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Tint;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = tex2D(_MainTex, i.uv) * _Tint; c.a = 1; return c; }
            ENDCG
        }
    }
    Fallback Off
}
