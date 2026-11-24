// MIASTO R1 — DOBA: noc i dzień (24.11, 16:31 właściciel: „noc i dzień”). Poziom i barwa światła dnia ustawiane globalnie
// przez BraxObrazRejonu (Assets/Claude/Runtime): _BraxSwiatlo 1 = dzień, ok. 0,1 = noc; _BraxSwiatloKolor — barwa (biel dnia,
// pomarańcz zmierzchu, chłodny błękit nocy; a = 1, gdy ustawiona). Dla shaderów bez świateł Unity (niebo, panorama, teren
// i las za granicą, szkło fasad, piana nabrzeża, linie boiska). Nieustawione (inne rejony, edytor) = dzień — bez zmian.
#ifndef BRAX_SWIATLO_INCLUDED
#define BRAX_SWIATLO_INCLUDED

float _BraxSwiatlo;
half4 _BraxSwiatloKolor;

inline half BraxSwiatloPoziom() { return _BraxSwiatlo > 0 ? _BraxSwiatlo : 1; }

inline half3 BraxDoba(half3 c)
{
    half3 k = _BraxSwiatloKolor.a > 0 ? _BraxSwiatloKolor.rgb : half3(1, 1, 1);
    return c * k * BraxSwiatloPoziom();
}

#endif
