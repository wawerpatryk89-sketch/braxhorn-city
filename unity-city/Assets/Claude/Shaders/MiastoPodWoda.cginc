// MIASTO R1 18.11 (nurkowanie, właściciel 13:04 „tak”): ŚWIATŁO W WODZIE OD KAMERY POD LUSTREM — wspólne dla shaderów miasta
// (Braxhorn/CityStaticPBR: boje, pale, mury, kadłuby; Braxhorn/NabrzezeTeren: dno z piasku). Globalne, ustawiane w grze przez
// BraxMiastoNurek tylko wtedy, gdy kamera jest pod lustrem strefy kąpieli (_BraxPodWodaJest = 1); nieustawione = 0 = bez zmian.
// Przepuszczalność T = exp(−k · droga w wodzie) na kanał (te same współczynniki co shader wody HarbourWater), w miejsce
// straconego światła — światło rozproszone w wodzie. Punkt nad lustrem: liczy się tylko odcinek drogi pod lustrem.
// Kamera nad wodą — nic się nie zmienia (żadnej mgły).
#ifndef MIASTO_POD_WODA_INCLUDED
#define MIASTO_POD_WODA_INCLUDED
float _BraxPodWodaJest, _BraxPodWodaLustro;
float4 _BraxPodWodaPochlanianie, _BraxPodWodaRozproszenie;

// c — kolor liniowy; rozproszone = false w przebiegach dodatkowych świateł (światło rozproszone dodaje tylko przebieg podstawowy)
half3 BraxPodWoda(float3 world, half3 c, bool rozproszone)
{
    float3 cam = _WorldSpaceCameraPos.xyz;
    if (_BraxPodWodaJest < 0.5 || cam.y >= _BraxPodWodaLustro) return c;
    float3 d = world - cam;
    float wWodzie = d.y > 0.0 ? saturate((_BraxPodWodaLustro - cam.y) / max(d.y, 1e-4)) : 1.0;
    float3 T = exp(-_BraxPodWodaPochlanianie.rgb * (length(d) * wWodzie));
    return c * T + (rozproszone ? _BraxPodWodaRozproszenie.rgb * (1.0 - T) : float3(0.0, 0.0, 0.0));
}
#endif
