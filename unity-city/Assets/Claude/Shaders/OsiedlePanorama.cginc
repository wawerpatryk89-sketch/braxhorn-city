// BRAXHORN build 14 — panorama dookoła osiedla (wspólne dla nieba i dalekiego terenu).
// Pas horyzontu z prawdziwego zdjęcia 360° (Poly Haven „Alps Field”, CC0; unity-tools/claude/panorama_osiedla.py):
// RGB = krajobraz (góry, lasy, pola, wieś, sylwetka miasta na północy), A = ziemia (1) / niebo (0).
// Pas leży na walcu o promieniu _PasmoPromien wokół środka osiedla — przy chodzeniu po osiedlu góry przesuwają się
// minimalnie (paralaksa jak dla krajobrazu 2.5 km dalej), a nie „jadą” razem z kamerą.
// Kierunek: u = 0.25 + azymut/360 + _PasmoU0 (azymut od północy +z w stronę wschodu +x), v = elewacja w zakresie pasa.
#ifndef BRAXHORN_OSIEDLE_PANORAMA
#define BRAXHORN_OSIEDLE_PANORAMA

sampler2D _Pasmo;
float4 _PasmoSrodek;                 // xyz: środek walca (y = poziom horyzontu zdjęcia)
float _PasmoPromien, _PasmoU0, _PasmoGora, _PasmoDol;
half _PasmoJasnosc, _PasmoNasycenie, _PasmoMgla;
half4 _PasmoMglaKolor;

// rgb = krajobraz w kierunku dir oglądany z punktu cam (liniowo, po korekcie), a = pokrycie ziemią
half4 PanoramaKierunek(float3 cam, float3 dir)
{
    float3 c = cam - _PasmoSrodek.xyz;
    float a = dot(dir.xz, dir.xz);
    if (a < 1e-6) return half4(0, 0, 0, dir.y < 0 ? 1 : 0);
    float b = dot(c.xz, dir.xz);
    float cc = dot(c.xz, c.xz) - _PasmoPromien * _PasmoPromien;
    float t = (-b + sqrt(max(b * b - a * cc, 0.0))) / a;
    float3 p = c + dir * t;
    float az = atan2(p.x, p.z) * (180.0 / UNITY_PI);
    float el = atan2(p.y, _PasmoPromien) * (180.0 / UNITY_PI);
    float u = frac(0.25 + az / 360.0 + _PasmoU0);
    float v = saturate((el - _PasmoDol) / (_PasmoGora - _PasmoDol));
    half4 s = tex2Dlod(_Pasmo, float4(u, v, 0, 0));
    if (el > _PasmoGora) s.a = 0;
    half y = dot(s.rgb, half3(.2126, .7152, .0722));
    s.rgb = max(0, y + (s.rgb - y) * _PasmoNasycenie) * _PasmoJasnosc;
    // mgiełka powietrza przy samym horyzoncie (dalekie stoki lekko jaśniejsze i bardziej niebieskie)
    s.rgb = lerp(s.rgb, _PasmoMglaKolor.rgb, _PasmoMgla * (1 - smoothstep(0.0, 14.0, el)));
    // ziemia tuż pod horyzontem (za zasięgiem kamery) zamglona jak osiedle ~200 m dalej (mgła sceny ~40 %) — brzeg zasięgu
    // kamery w głębi osiedla nie odcina się od zdjęcia
    s.rgb = lerp(s.rgb, _PasmoMglaKolor.rgb, .36 * (1 - smoothstep(-1.5, 1.0, el)));
    return s;
}

#endif
