// BRAXHORN R2 — jedno przejście koloru na końcu kadru (zasada kolorów 31.10: „lekki grading: nasycenie +10…+20 %,
// kontrast +5…+10 %, bez tintu; jedyny post-process, bez bloomu”). Nasycenie liniowo wokół luminancji (biel i szarości
// zostają neutralne), kontrast w przestrzeni postrzeganej (gamma 2.2) wokół szarości 0.5. Jedna próbka tekstury na piksel.
Shader "Braxhorn/OsiedleKolor"
{
    Properties
    {
        _MainTex ("Kadr", 2D) = "white" {}
        _Nasycenie ("Nasycenie (mnożnik)", Float) = 1.15
        _Kontrast ("Kontrast (mnożnik)", Float) = 1.06
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half _Nasycenie, _Kontrast;
            half4 frag (v2f_img i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                half y = dot(c.rgb, half3(.2126, .7152, .0722));
                half3 s = max(0, y + (c.rgb - y) * _Nasycenie);
                half3 g = pow(s, 1 / 2.2);
                g = saturate((g - .5) * _Kontrast + .5);
                return half4(pow(g, 2.2), c.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
