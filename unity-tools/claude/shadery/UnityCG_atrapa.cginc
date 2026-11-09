#ifndef STUB_UNITYCG
#define STUB_UNITYCG
#define UNITY_VERTEX_INPUT_INSTANCE_ID
#define UNITY_VERTEX_OUTPUT_STEREO
#define UNITY_INITIALIZE_OUTPUT(t, o) o = (t)0;
#define UNITY_SETUP_INSTANCE_ID(v)
#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o)
#define fixed float
#define fixed2 float2
#define fixed3 float3
#define fixed4 float4
#define half float
#define half2 float2
#define half3 float3
#define half4 float4
float4x4 unity_ObjectToWorld, unity_MatrixVP, unity_CameraProjection; float4 _ScreenParams, _Time, _ProjectionParams; float3 _WorldSpaceCameraPos;
float4 UnityObjectToClipPos(float4 v) { return mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(v.xyz, 1))); }
float3 UnityObjectToWorldNormal(float3 n) { return normalize(mul((float3x3)unity_ObjectToWorld, n)); }
float4 ComputeScreenPos(float4 p) { float4 o = p * .5; o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w; o.zw = p.zw; return o; }
samplerCUBE unity_SpecCube0; float4 unity_SpecCube0_HDR;
#define UNITY_SAMPLE_TEXCUBE_LOD(tex, coord, lod) texCUBE(tex, coord)
#define tex2Dlod(s, c) tex2D(s, (c).xy)
#define tex2Dgrad(s, uv, dx, dy) tex2D(s, (uv) + ((dx) + (dy)) * 0)
float3 DecodeHDR(float4 d, float4 h) { return d.rgb * h.x; }
#endif
