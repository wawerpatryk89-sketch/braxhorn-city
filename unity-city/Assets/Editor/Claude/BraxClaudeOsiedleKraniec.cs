using System.Collections.Generic;
using UnityEngine;

// KRANIEC MAPY (audyt filmów właściciela 1.11: za płotem i murkiem granicznym „pustka” — niebo poniżej horyzontu,
// osiedle wisi w powietrzu; F01 0:02, F02 0:26–0:42, F07, F08, F10 0:00). Dookoła osiedla (poza płotem, bez kolizji —
// gracz tam nie wejdzie): łąki do 700 m, pasy lasu 30–260 m za płotem (proste stożki — z daleka i we mgle czytają się jak
// las, koszt kilku tysięcy trójkątów), wzgórze nad tunelem do miasta (portal nie stoi już jak ściana w polu) i pierścień
// niskich wzgórz 300–600 m (zamyka horyzont). Wszystko poniżej poziomu trawy osiedla (y −0.03), bez cieni.
public static partial class BraxClaudeOsiedle
{
    const float KrNorth = 101f;   // północna krawędź trawy osiedla A (portal tunelu z = 99.5)

    static void KraniecMapy()
    {
        group = "Kraniec mapy (teren dookoła)";
        var meadow = Grass;
        var forest = Plain("las w oddali", new Color(.13f, .24f, .12f), .05f);
        var forest2 = Plain("las w oddali jasny", new Color(.20f, .33f, .15f), .05f);
        var hillMat = Plain("wzgórza w oddali", new Color(.30f, .42f, .22f), .05f);
        float ix0 = X0 - .5f, ix1 = X1 + .5f, iz0 = SouthZ - .5f, iz1 = KrNorth;
        const float R = 700f, y = -.03f;
        // 1) łąki: cztery prostokąty dookoła prostokąta osiedla (Quad — bez rejestru Hard/Paved, to poza grą)
        void Plaski(Material m, float x0, float x1, float z0, float z1)
        { Quad(m, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up); }
        Plaski(meadow, -R, R, iz1, R);           // północ
        Plaski(meadow, -R, R, -R - 300f, iz0);   // południe
        Plaski(meadow, -R, ix0, iz0, iz1);       // zachód
        Plaski(meadow, ix1, R, iz0, iz1);        // wschód

        // 2) wzgórze nad tunelem do miasta: zaczyna się za portalem (z 101.5 — nic nie wchodzi na osiedle), do 24 m już od
        //    z ≈ 112 (strop tunelu 6.3 m schowany), x −95…95, opada ku bokom i na północ
        WzgorzeTunelu(meadow, -95f, 95f, 101.5f, 215f, 24f);

        // 3) pasy lasu: stożki w skupiskach 30–260 m za płotem (poza wzgórzem nad tunelem)
        var rnd = new System.Random(4711);
        int stozki = 0;
        for (int k = 0; k < 0; k++)   // 1.11, 20:29 (właściciel): stożki-„choinki” usunięte — żadnych sztucznych brył; las w oddali dopiero z prawdziwych drzew
        {
            float side = (float)rnd.NextDouble();
            float d = 30f + Mathf.Pow((float)rnd.NextDouble(), 1.6f) * 230f;
            Vector3 p;
            if (side < .3f) p = new Vector3(Mathf.Lerp(-R * .45f, R * .45f, (float)rnd.NextDouble()), 0, iz0 - d);          // południe
            else if (side < .55f) p = new Vector3(ix0 - d, 0, Mathf.Lerp(iz0 - 120f, iz1 + 120f, (float)rnd.NextDouble()));  // zachód
            else if (side < .8f) p = new Vector3(ix1 + d, 0, Mathf.Lerp(iz0 - 120f, iz1 + 120f, (float)rnd.NextDouble()));   // wschód
            else p = new Vector3(Mathf.Lerp(-R * .45f, R * .45f, (float)rnd.NextDouble()), 0, iz1 + d);                     // północ
            if (p.z > iz1 && Mathf.Abs(p.x) < 100f && p.z < 215f) continue;      // wzgórze tunelu
            float h = 7f + (float)rnd.NextDouble() * 9f, r = h * (.22f + (float)rnd.NextDouble() * .1f);
            Stozek(rnd.NextDouble() < .6 ? forest : forest2, p + Vector3.up * y, r, h, 7);
            stozki++;
        }
        // 4) pierścień wzgórz 300–600 m — zamyka horyzont (mgła exp2 0.0042 robi resztę)
        for (int k = 0; k < 14; k++)
        {
            float a = k * Mathf.PI * 2f / 14f + (float)rnd.NextDouble() * .2f;
            float dist = 380f + (float)rnd.NextDouble() * 200f;
            var c = new Vector3(Mathf.Cos(a) * dist, 0, -67f + Mathf.Sin(a) * dist);
            Wzgorze(hillMat, null, c, 110f + (float)rnd.NextDouble() * 90f, 90f + (float)rnd.NextDouble() * 70f, 22f + (float)rnd.NextDouble() * 30f, 12, false);
        }
        rep.notes.Add("kraniec mapy: łąki do " + R + " m, " + stozki + " drzew lasu w oddali, wzgórze nad tunelem, 14 wzgórz na horyzoncie");
        // ruch (ludzie, auta) i mapa — po drogach A (DrogiA), przed trawą 3D; błąd w jednym nie zatrzymuje reszty
        Guard("ruch: przechodnie i auta", RuchNaOsiedlu);
        Guard("mapa osiedla (menu)", MapaOsiedla);
        Guard("sport: piłki i gry", Sport);
        Guard("dźwięki: silniki aut i tło (ptaki, wiatr, miasto)", DzwiekiOsiedla);   // po RuchNaOsiedlu (auta już stoją)
    }

    static void WzgorzeTunelu(Material m, float xa, float xb, float za, float zb, float h)
    {
        const int nx = 24, nz = 18;
        float S(float e0, float e1, float v) { float t = Mathf.Clamp01((v - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }
        float H(float x, float z) => h * S(xb, xb - 35f, x) * S(xa, xa + 35f, x) * S(za, za + 10.5f, z) * S(zb, zb - 30f, z);
        Vector3 P(int i, int j) { float x = Mathf.Lerp(xa, xb, i / (float)nx), z = Mathf.Lerp(za, zb, j / (float)nz); return new Vector3(x, -.03f + H(x, z), z); }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                var p00 = P(i, j); var p10 = P(i + 1, j); var p11 = P(i + 1, j + 1); var p01 = P(i, j + 1);
                var nrm = Vector3.Cross(p01 - p00, p10 - p00).normalized; if (nrm.y < 0) nrm = -nrm;
                Quad(m, p00, p10, p11, p01, nrm);
            }
    }

    // Stożek (drzewo iglaste z daleka): boki bez podstawy.
    static void Stozek(Material m, Vector3 baza, float r, float h, int seg)
    {
        var top = baza + Vector3.up * h;
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
            var p0 = baza + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r;
            var p1 = baza + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
            var n = (new Vector3(Mathf.Cos((a0 + a1) * .5f), r / h, Mathf.Sin((a0 + a1) * .5f))).normalized;
            Tri(m, p0, top, p1, n);
        }
    }

    // Wzgórze: eliptyczna kopuła z siatki (rx × rz, wysokość h), profil cos² — gładki stok do zera na brzegu.
    // trawaNaGorze: wierzchołki blisko osiedla dostają materiał łąki (ta sama trawa co na osiedlu).
    static void Wzgorze(Material m, Material trawa, Vector3 c, float rx, float rz, float h, int n, bool trawaNaGorze)
    {
        float H(float u, float v) { float r2 = u * u + v * v; if (r2 >= 1f) return 0f; float q = Mathf.Cos(Mathf.Sqrt(r2) * Mathf.PI * .5f); return h * q * q; }
        Vector3 P(int i, int j) { float u = -1f + 2f * i / n, v = -1f + 2f * j / n; return new Vector3(c.x + u * rx, -.03f + H(u, v), c.z + v * rz); }
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                var p00 = P(i, j); var p10 = P(i + 1, j); var p11 = P(i + 1, j + 1); var p01 = P(i, j + 1);
                if (p00.y <= -.02f && p10.y <= -.02f && p11.y <= -.02f && p01.y <= -.02f) continue;   // płasko — łąka już jest
                var nrm = Vector3.Cross(p01 - p00, p10 - p00).normalized; if (nrm.y < 0) nrm = -nrm;
                var mat = trawaNaGorze && trawa ? trawa : m;
                Quad(mat, p00, p10, p11, p01, nrm);
            }
    }
}
