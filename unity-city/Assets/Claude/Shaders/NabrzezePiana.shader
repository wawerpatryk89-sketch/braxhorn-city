// BRAXHORN CITY — piana morska BEZ cząsteczek (moduł ParticleSystem jest w projekcie wyłączony): siatka + przesuw w czasie.
// Pas przy brzegu: UV.x = metry wzdłuż brzegu, UV.y = 0 od strony morza … 1 na najdalszym zasięgu fali. Fronty fal
// wbiegają na piasek, zostawiają koronkę piany i znikają; szum z tekstury (generowanej w edytorze) łamie regularność.
// Ten sam shader rysuje falę dziobową i ślad za łodzią (_Moc ustawia skrypt łodzi wg prędkości, _Bieg = kierunek ruchu).
// Kolor wierzchołka: alfa = wygaszanie krawędzi paska. Nic nie świeci (kolor × światło otoczenia + słońce).
Shader "Braxhorn/NabrzezePiana"
{
    Properties
    {
        _Szum ("Szum piany", 2D) = "gray" {}
        _Color ("Kolor piany", Color) = (.93,.95,.95,1)
        _Okres ("Okres fali (s)", Float) = 7
        _Szer ("Szerokość frontu", Float) = .09
        _Moc ("Siła (0..1)", Range(0,1)) = 1
        _Bieg ("Prędkość przesuwu wzdłuż UV.y", Float) = 0
        _Tryb ("0 = brzeg, 1 = łódź", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Offset -1, -2
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _Szum; fixed4 _Color; float _Okres, _Szer, _Moc, _Bieg, _Tryb;
            fixed4 _LightColor0;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos); return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float x = i.uv.x, y = i.uv.y;
                float foam;
                if (_Tryb < .5)
                {
                    float n1 = tex2D(_Szum, float2(x * .045, y * .55 - t * .035)).r;
                    float n2 = tex2D(_Szum, float2(x * .11 + .3, y * 1.3 - t * .07)).r;
                    // dwa fronty na okres, przesunięte wzdłuż brzegu (fala nie dochodzi wszędzie naraz)
                    float f1 = frac(t / _Okres + x * .011 + n1 * .12);
                    float f2 = frac(t / _Okres + .5 + x * .007 + n2 * .1);
                    float d1 = y - pow(f1, .7) * .95, d2 = y - pow(f2, .7) * .75;
                    float b1 = exp(-d1 * d1 / (_Szer * _Szer)) * (1 - f1 * .8);
                    float b2 = exp(-d2 * d2 / (_Szer * _Szer)) * (1 - f2 * .85) * .8;
                    // koronka piany zostająca za frontem (przy wodzie gęstsza)
                    float lace = smoothstep(.56, .74, n2 + n1 * .35) * saturate(1.15 - y * 1.4) * .55;
                    float stale = saturate(1 - abs(y - .12) * 6) * smoothstep(.45, .7, n1) * .5;   // pas przy linii wody
                    foam = saturate((b1 + b2) * smoothstep(.3, .62, n2 + .15) + lace + stale);
                }
                else
                {
                    // łódź: UV.y biegnie od dziobu/rufy na zewnątrz; piana płynie z prędkością _Bieg i rozpada się
                    float n1 = tex2D(_Szum, float2(x * .5, y * .35 - t * _Bieg * .12)).r;
                    float n2 = tex2D(_Szum, float2(x * 1.1 + .4, y * .8 - t * _Bieg * .2)).r;
                    float core = saturate(1 - abs(x - .5) * 2.2);
                    foam = saturate(smoothstep(.38, .72, n1 * .7 + n2 * .5) * (core * .6 + .4) * saturate(1.2 - y));
                }
                float sun = .78 + .22 * saturate(_LightColor0.r);
                fixed4 c = fixed4(_Color.rgb * sun, foam * i.color.a * _Moc * _Color.a);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
