Shader "Braxhorn/CityStaticPBR"
{
    Properties
    {
        [MainTexture] _MainTex("Base colour (sRGB)", 2D) = "white" {}
        [MainColor] _Color("Base colour factor", Color) = (1,1,1,1)
        _MainTex_Rotation("Base UV off-diagonal terms", Vector) = (0,0,0,0)
        _MainTex_texCoord("Base UV set", Float) = 0
        _BumpMap("Normal (linear RGB or Unity packed)", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,2)) = 1
        _BumpMap_Rotation("Normal UV off-diagonal terms", Vector) = (0,0,0,0)
        _BumpMap_texCoord("Normal UV set", Float) = 0
        _NormalEncoding("0 RGB XYZ / 1 Unity packed", Float) = 0
        _ARM("Linear AO / Roughness / Metallic", 2D) = "white" {}
        _ARM_Rotation("ARM UV off-diagonal terms", Vector) = (0,0,0,0)
        _ARM_texCoord("ARM UV set", Float) = 0
        _Roughness("Roughness factor", Range(0,1)) = .85
        _Metallic("Metallic factor", Range(0,1)) = 0
        _UseARMForAO("Use ARM red for AO", Float) = 0
        _OcclusionMap("Linear occlusion", 2D) = "white" {}
        _OcclusionMap_Rotation("AO UV off-diagonal terms", Vector) = (0,0,0,0)
        _OcclusionMap_texCoord("AO UV set", Float) = 0
        _OcclusionStrength("Texture AO strength", Range(0,1)) = 1
        _EmissionMap("Actual emissive surface texture", 2D) = "white" {}
        _EmissionMap_Rotation("Emission UV off-diagonal terms", Vector) = (0,0,0,0)
        _EmissionMap_texCoord("Emission UV set", Float) = 0
        [HDR] _EmissionColor("Actual emissive surface factor", Color) = (0,0,0,1)
        _VertexTint("Preserved glTF vertex colour", Float) = 1
        _Cutoff("Alpha cutoff (zero = opaque/blend)", Range(0,1)) = 0
        _AlphaClip("Alpha mask enabled", Float) = 0
        _Cull("Cull mode", Float) = 2
        [HideInInspector] _SrcBlend("Source blend", Float) = 1
        [HideInInspector] _DstBlend("Destination blend", Float) = 0
        [HideInInspector] _ZWrite("Depth write", Float) = 1
        [HideInInspector] _Transparent("Preserve blend alpha", Float) = 0
        _SunDirection("Surface towards city sun", Vector) = (-.45,.78,-.42,0)
        _SunColor("City daylight colour", Color) = (1,.98,.94,1)
        _SunIntensity("City sun irradiance", Range(0,5)) = 2.4
        _SkyColor("City sky irradiance", Color) = (.60,.68,.78,1)
        _GroundColor("City ground irradiance", Color) = (.42,.40,.36,1)
        _AmbientIntensity("City ambient irradiance", Range(0,2)) = .65
        _ReflectionStrength("Broad sky reflection", Range(0,1)) = .4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Pass
        {
            Name "CITY_STATIC_PBR"
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex, _BumpMap, _ARM, _OcclusionMap, _EmissionMap;
            float4 _MainTex_ST, _BumpMap_ST, _ARM_ST, _OcclusionMap_ST, _EmissionMap_ST;
            float4 _MainTex_Rotation, _BumpMap_Rotation, _ARM_Rotation;
            float4 _OcclusionMap_Rotation, _EmissionMap_Rotation;
            float _MainTex_texCoord, _BumpMap_texCoord, _ARM_texCoord;
            float _OcclusionMap_texCoord, _EmissionMap_texCoord;
            half4 _Color, _SunColor, _SkyColor, _GroundColor, _EmissionColor;
            float4 _SunDirection;
            half _BumpScale, _NormalEncoding, _Roughness, _Metallic, _UseARMForAO;
            half _OcclusionStrength, _Cutoff, _AlphaClip, _Transparent, _VertexTint;
            half _SunIntensity, _AmbientIntensity, _ReflectionStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 bake3 : TEXCOORD3;
                float4 bake7 : TEXCOORD7;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half3 normal : TEXCOORD2;
                half4 tangent : TEXCOORD3;
                half2 baked : TEXCOORD4;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float2 MapUV(float4 uv, float4 st, float2 rotation, float set)
            {
                float2 p = lerp(uv.xy, uv.zw, saturate(set));
                return float2(p.x * st.x + p.y * rotation.y,
                              p.x * rotation.x + p.y * st.y) + st.zw;
            }
            half3 WorkingColour(half3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return GammaToLinearSpace(c);
                #else
                return c;
                #endif
            }
            half Pow5(half v) { half s=v*v; return s*s*v; }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = mul((float3x3)unity_ObjectToWorld,v.tangent.xyz);
                worldTangent *= rsqrt(max(dot(worldTangent,worldTangent),1e-12));
                o.tangent = half4(worldTangent,
                                 v.tangent.w * unity_WorldTransformParams.w);
                o.uv = float4(v.uv, v.uv1);
                o.color = v.color;
                // No global/material bake toggle: unbaked instances can share a material.
                // A reserved marker distinguishes lighting from an existing model UV set.
                float has3 = step(abs(v.bake3.w - 2718.0), .1) * step(.99, v.bake3.z);
                float has7 = step(abs(v.bake7.w - 2718.0), .1) * step(.99, v.bake7.z);
                o.baked = lerp(half2(1,1), v.bake3.xy, has3);
                o.baked = lerp(o.baked, v.bake7.xy, has7);
                return o;
            }
            half4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 uvBase = MapUV(i.uv, _MainTex_ST, _MainTex_Rotation.xy, _MainTex_texCoord);
                half4 baseSample = tex2D(_MainTex, uvBase);
                half4 vertexTint = lerp(half4(1,1,1,1), i.color, _VertexTint);
                half alpha = baseSample.a * _Color.a * vertexTint.a;
                // OPAQUE deliberately ignores texture alpha; MASK keeps its true threshold.
                clip(lerp(1, alpha - _Cutoff, _AlphaClip));
                half3 albedo = WorkingColour(baseSample.rgb) * WorkingColour(_Color.rgb) * vertexTint.rgb;
                float2 uvNormal = MapUV(i.uv, _BumpMap_ST, _BumpMap_Rotation.xy, _BumpMap_texCoord);
                half4 packed = tex2D(_BumpMap, uvNormal);
                half3 normalTS = lerp(packed.xyz * 2 - 1, UnpackNormal(packed), _NormalEncoding);
                normalTS.xy *= _BumpScale;
                normalTS = normalize(normalTS);
                half3 n = normalize(i.normal) * (facing >= 0 ? 1 : -1);
                half3 t = i.tangent.xyz;
                half3 b;
                // Preserve the supplied tangent frame. UV1 or absent tangents use a
                // cotangent frame; no source mesh or UV needs to be rewritten.
                if (dot(t,t) > .1 && abs(i.tangent.w) > .1 && _BumpMap_texCoord < .5)
                {
                    t = normalize(t - n * dot(n,t));
                    b = cross(n,t) * i.tangent.w;
                }
                else
                {
                    float3 dx = ddx(i.worldPos), dy = ddy(i.worldPos);
                    float2 ux = ddx(uvNormal), uy = ddy(uvNormal);
                    float3 dyPerp = cross(dy,n), dxPerp = cross(n,dx);
                    t = dyPerp * ux.x + dxPerp * uy.x;
                    b = dyPerp * ux.y + dxPerp * uy.y;
                    float frameScale = rsqrt(max(max(dot(t,t),dot(b,b)), 1e-12));
                    t *= frameScale; b *= frameScale;
                }
                n = normalize(t * normalTS.x + b * normalTS.y + n * normalTS.z);
                half3 arm = tex2D(_ARM, MapUV(i.uv,_ARM_ST,_ARM_Rotation.xy,_ARM_texCoord)).rgb;
                // glTF metallic-roughness channels: G roughness, B metallic.
                half roughness = clamp(arm.g * _Roughness, .085, 1);
                half metallic = saturate(arm.b * _Metallic);
                half aoMap = tex2D(_OcclusionMap, MapUV(i.uv,_OcclusionMap_ST,
                                      _OcclusionMap_Rotation.xy,_OcclusionMap_texCoord)).r;
                half textureAO = lerp(1, lerp(aoMap,arm.r,_UseARMForAO), _OcclusionStrength);
                half ambientAO = lerp(.4,1,saturate(i.baked.y)) * textureAO;
                float3 view = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half3 light = normalize(_SunDirection.xyz);
                half3 h = normalize(view + light);
                half nl = saturate(dot(n,light)), nv = max(saturate(dot(n,view)), .001);
                half nh = saturate(dot(n,h)), vh = saturate(dot(view,h));
                half3 f0 = lerp(half3(.04,.04,.04), albedo, metallic);
                half3 fresnel = f0 + (1-f0) * Pow5(1-vh);
                float a = roughness*roughness, a2 = a*a;
                float denom = nh*nh*(a2-1)+1;
                float distribution = a2 / max(UNITY_PI*denom*denom, 1e-6);
                half k = (roughness+1)*(roughness+1)*.125;
                float geometry = (nv/(nv*(1-k)+k)) * (nl/(nl*(1-k)+k));
                half3 specular = fresnel * (distribution*geometry/max(4*nv*nl,.001));
                half3 diffuse = (1-fresnel)*(1-metallic)*albedo/UNITY_PI;
                half3 direct = (diffuse+specular) * WorkingColour(_SunColor.rgb)
                             * (_SunIntensity * nl * saturate(i.baked.x));
                half3 sky = WorkingColour(_SkyColor.rgb), ground = WorkingColour(_GroundColor.rgb);
                half3 ambient = lerp(ground,sky,n.y*.5+.5) * _AmbientIntensity;
                half3 colour = direct + albedo*(1-metallic)*ambient*ambientAO;
                // Broad, roughness-filtered sky/ground reflection; no camera render,
                // screen-space reflection, real-time probe, or extra light is involved.
                half3 reflected = reflect(-view,n);
                half3 environment = lerp(ground,sky,reflected.y*.5+.5);
                half3 viewFresnel = f0 + (1-f0)*Pow5(1-nv);
                colour += environment*viewFresnel*(1-.65*roughness)*_ReflectionStrength*ambientAO;
                if (dot(_EmissionColor.rgb,_EmissionColor.rgb) > 0)
                    colour += WorkingColour(tex2D(_EmissionMap, MapUV(i.uv,_EmissionMap_ST,
                                      _EmissionMap_Rotation.xy,_EmissionMap_texCoord)).rgb)
                              * WorkingColour(_EmissionColor.rgb);
                #ifdef UNITY_COLORSPACE_GAMMA
                colour = LinearToGammaSpace(max(colour,0));
                #endif
                return half4(colour, lerp(1,alpha,_Transparent));
            }
            ENDCG
        }
    }
    Fallback Off
}
