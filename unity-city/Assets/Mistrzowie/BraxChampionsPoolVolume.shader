// THE WATER BODY of a Mistrzowie pool (owner 16.11 07:33: "woda ma być wodą, nie tylko pasek wody, jak wejdę do wody,
// zanurkuję, to ma być woda po całym basenie"). The surface (ChampionsP1PoolWater) is only the top of the water; this shader
// fills the basin: it is drawn on a skin laid 5 cm in front of the pool's walls and floor, facing into the water, and
// colours every tile by the length of the way the light travels UNDER the water between that tile and the eye:
//   eye above the water: from where the ray enters the surface to the tile; eye under the water: from the eye.
// Shallow steps stay clear, the far wall and the deep well turn deep blue, under water the whole basin is water.
// The surface height comes from the mesh (uv.x = depth of the vertex under the surface, nominal metres; _K = world units
// per nominal metre), so one material serves pools of any level. Caustics (moving light net) on the floor and walls.
Shader "Braxhorn/ChampionsPoolVolume"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.015,0.17,0.33,1)
        _ShallowColor ("Shallow water", Color) = (0.08,0.55,0.72,1)
        _Density ("Turbidity per metre", Range(0.02,1.5)) = 0.24
        _Caustics ("Caustics", Range(0,2)) = 0.9
        _K ("World units per metre", Float) = 1
    }
    SubShader
    {
        // after every opaque tile, before the surface (3000) and the glass
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest LEqual
        Cull Back
        Blend One OneMinusSrcAlpha
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
            float _Density, _Caustics, _K;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float surf : TEXCOORD1; float3 n : TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.surf = o.world.y + v.uv.x * _K;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 C = _WorldSpaceCameraPos.xyz, P = i.world;
                float S = i.surf;
                float len = length(P - C);
                float under;
                if (C.y <= S) under = len;
                else under = len * (1.0 - saturate((C.y - S) / max(C.y - P.y, 1e-3)));
                float metres = under / max(_K, 1e-3);
                float depth = max(0.0, (S - P.y) / max(_K, 1e-3));
                float fog = 1.0 - exp(-metres * _Density);
                half3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(metres / 7.0 + depth / 9.0));
                // caustics: the sun's light net through the waves, strongest on the floor, fading with depth and distance
                float t = _Time.y;
                // two crossed layers ~40 cm a cell (a real pool's net at 1-2 m), finer lines, gone by 16 m
                float2 q = P.xz / max(_K, 1e-3) * 2.7;
                float a1 = sin(q.x * 1.7 + sin(q.y * 1.3 + t * .80) * 1.6 + t * .50);
                float a2 = sin(q.y * 1.9 + sin(q.x * 1.1 - t * .70) * 1.7 - t * .60);
                float a3 = sin((q.x + q.y) * 1.2 + sin(q.x * .7 - q.y * .9 + t * .40) * 2.0 + t * .30);
                float2 r = float2(q.x * .8 - q.y * .6, q.x * .6 + q.y * .8) * 1.37;
                float b1 = sin(r.x * 1.5 + sin(r.y * 1.7 - t * .60) * 1.4 - t * .40);
                float b2 = sin(r.y * 1.6 + sin(r.x * 1.2 + t * .55) * 1.5 + t * .45);
                float ridge = saturate(1.0 - abs(a1 + a2 + a3) * .45) * saturate(1.0 - abs(b1 + b2) * .30);
                float up = saturate(normalize(i.n).y);
                float caus = pow(ridge, 6.0) * _Caustics * .45 * (.3 + .7 * up) * exp(-depth * .35)
                    * saturate(1.0 - len / max(_K, 1e-3) / 16.0) * (1.0 - fog * .7);
                // premultiplied: the tile behind keeps (1-fog) of its light, the water adds its own colour and the caustic light
                return half4(water * fog + caus * half3(.62, .70, .66), fog);
            }
            ENDCG
        }
    }
    FallBack Off
}
