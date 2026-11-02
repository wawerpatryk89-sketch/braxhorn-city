// CLUB NOVA — parkiet LED i ściana LED za DJ-em (przebudowa 2.11: „większy parkiet”, premium). Płytki 1 m (siatka w metrach
// świata: XZ dla parkietu, XY/ZY dla ściany — _Pion), fuga ciemna, każda płytka świeci własnym kolorem według programu,
// który zmienia się co 8 bitów (fala po przekątnej, błyski losowych płytek, pierścienie od środka). Czas i bit z BraxNovaEfekty
// (globalne _NovaCzas, _NovaBeat), więc poza zasięgiem gracza parkiet stoi (zero pracy). Pod płytką ciemne szkło z połyskiem.
Shader "Braxhorn/NovaParkiet"
{
    Properties
    {
        _Plytka("Rozmiar płytki (m)", Float) = 1.0
        _Srodek("Środek wzoru (świat XZ / XY)", Vector) = (42,50,0,0)
        _Pion("Ściana (0 = podłoga XZ, 1 = ściana XY, 2 = ściana ZY)", Float) = 0
        _Moc("Jasność", Range(0,4)) = 1.2
        _Baza("Szkło pod płytką", Color) = (.03,.03,.04,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Plytka, _Pion, _Moc; float4 _Srodek; fixed4 _Baza;
            float _NovaCzas, _NovaBeat, _NovaAktywny;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 wp:TEXCOORD0; float3 n:TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.wp = mul(unity_ObjectToWorld, v.vertex).xyz; o.n = UnityObjectToWorldNormal(v.normal); return o; }
            float H(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float3 Hsv(float h) { float3 k = saturate(abs(frac(h + float3(0, 2.0/3, 1.0/3)) * 6 - 3) - 1); return k; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = _Pion < .5 ? i.wp.xz : (_Pion < 1.5 ? i.wp.xy : i.wp.zy);
                float2 q = p / _Plytka; float2 id = floor(q); float2 f = frac(q);
                float fuga = step(.04, f.x) * step(f.x, .96) * step(.04, f.y) * step(f.y, .96);
                float beat = _NovaBeat; float faza = frac(beat); float prog = floor(beat / 8.0);
                int tryb = (int)fmod(prog, 3.0);
                float2 c = (id + .5) * _Plytka - _Srodek.xy;
                float jas; float barwa;
                if (tryb == 0)      { float w = id.x + id.y - beat * 2.0; jas = pow(saturate(.5 + .5 * sin(w * 1.2)), 3); barwa = frac(prog * .17 + (id.x + id.y) * .03); }
                else if (tryb == 1) { float r = H(id + floor(beat)); jas = step(.62, r) * (1 - faza * .8); barwa = frac(r * 3.1 + prog * .23); }
                else                { float d = length(c); jas = pow(saturate(.5 + .5 * cos(d * 1.6 - beat * 3.14159)), 4); barwa = frac(prog * .29 + d * .05); }
                float3 kol = Hsv(barwa) * jas * _Moc * fuga * _NovaAktywny;
                // lekka poświata wewnątrz płytki (dyfuzor)
                float2 g = abs(f - .5) * 2; float dyf = 1 - .35 * max(g.x, g.y);
                float3 baza = _Baza.rgb * (1 - fuga * .3);
                return fixed4(baza + kol * dyf, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
