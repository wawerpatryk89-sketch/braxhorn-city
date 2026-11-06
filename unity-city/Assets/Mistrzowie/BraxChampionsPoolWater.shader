Shader "Braxhorn/ChampionsP1PoolWater"
{
    Properties
    {
        _DeepColor ("Deep azure", Color) = (0.035,0.23,0.43,1)
        _ShallowColor ("Sunlit blue", Color) = (0.10,0.61,0.84,1)
        _Opacity ("Water transparency", Range(0,1)) = 0.72
        _RippleStrength ("Ripple intensity", Range(0,2)) = 0.8
        _Bubbles ("Jacuzzi aeration 0-1", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 150
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _DeepColor, _ShallowColor;
            half _Opacity, _RippleStrength, _Bubbles;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float2 uv : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;
                // No vertex height displacement: the collisionless, tiled surface
                // must never visibly open a gap at the pool's waterproof coping.
                o.world=w;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p=i.world.xz;
                float t=_Time.y;
                // Analytic moving waves: no texture downloads or repeated maps.
                float a=dot(p,float2(.71,.704))*7.5-t*1.75;
                float b=dot(p,float2(-.38,.925))*11.0+t*1.32;
                float c=dot(p,float2(.96,-.28))*4.4-t*.95;
                float2 slope=(_RippleStrength*.018)*(
                    cos(a)*7.5*float2(.71,.704)+
                    cos(b)*11.0*float2(-.38,.925)+
                    cos(c)*4.4*float2(.96,-.28));
                half3 n=normalize(half3(-slope.x,1,-slope.y));
                half3 v=normalize(_WorldSpaceCameraPos.xyz-i.world);
                half3 sun=normalize(half3(.37,.82,-.44));
                half3 h=normalize(v+sun);
                half fresnel=pow(1.0h-saturate(dot(v,n)),5.0h);
                half caustic=.5h+.5h*sin(a*.75+b*.35);
                half3 color=lerp(_DeepColor.rgb,_ShallowColor.rgb,.34h+.34h*caustic);
                color+=pow(saturate(dot(n,h)),58.0h)*half3(.25,.30,.35);
                color=lerp(color,half3(.68,.90,1.0),fresnel*.38);
                // Fine bubbling and foam are explicitly reserved for the owner's
                // real jacuzzi, never drawn over an absent or unapproved tub.
                if(_Bubbles>.01)
                {
                    float g=sin(p.x*18.9+t*3.2)*sin(p.y*16.2-t*2.7);
                    half foam=smoothstep(.80, .98, g)*_Bubbles;
                    color=lerp(color,half3(.90,.98,1.0),foam*.82);
                }
                return half4(color,saturate(_Opacity+.08h*fresnel+.08h*_Bubbles));
            }
            ENDCG
        }
    }
    FallBack Off
}
