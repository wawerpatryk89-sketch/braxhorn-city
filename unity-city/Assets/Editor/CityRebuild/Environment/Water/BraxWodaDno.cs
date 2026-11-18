using UnityEngine;

// MIASTO R1 — DNO POD WODĄ MIASTA (właściciel 18.11 12:28–12:41: „nie możemy zrobić bardziej realistycznej wody?”, „ta woda to
// jest woda, a nie pasek… widać nic”, „Ta woda nie może wyglądać realistycznie?”). Głębokość wody w każdym punkcie morza
// miasta — z TYCH SAMYCH funkcji, które stawiają teren pod wodą:
//   • plaża miejska i marina (siatka piasku x 64…260, z 100…178): BraxNabrzezePlaza.Wysokosc — płycizna 1:11, rewa, falochron;
//   • wyspa z areną (BraxMiastoWyspa.H): plaża wyspy i jej szelf do 12 m;
//   • reszta (nabrzeża portu, promenada, otwarte morze): model — przy murze nabrzeża od razu dno kolizji (y −7, BraxRegionSea),
//     dalej łagodnie głębiej, na otwartym morzu do 20 m; na północ od cypla ten sam spadek co piasek plaży.
// Z tego tekstura głębokości dla shadera wody (HarbourWater): kolor wody po drodze światła w wodzie, dno z piasku widoczne pod
// płytką wodą (załamanie światła, kaustyki), głęboka woda ciemnieje. Zapis: R8 = sqrt(głębokość / 20 m) (dokładniej przy brzegu).
public static class BraxWodaDno
{
    /// <summary>Prostokąt tekstury głębokości (x0, z0, x1, z1): port, promenada, plaża, marina, morze do wyspy i cała wyspa.</summary>
    public static readonly Vector4 Prostokat = new Vector4(60f, -452f, 1468f, 572f);
    public const int TexW = 1024, TexH = 768;                 // 1.375 × 1.333 m na piksel (filtr dwuliniowy)
    public const float MaxGlebokosc = 20f;
    public const float DnoKolizjiY = -7f;                     // wierzch kolizji dna morza (BraxRegionSea: środek y −7.5, grubość 1 m)
    public static readonly Rect Plaza = Rect.MinMaxRect(64f, 100f, 260f, 178f);   // siatka piasku plaży (BraxNabrzezePlaza, od 18.11 do x 260)
    public const float PlazaPrzejscie = 8f;                   // przejście teren plaży → model poza siatką piasku
    public const float WyspaZasieg = 600f;                    // promień, w którym liczy się dno wyspy (dalej jej szelf = 12 m)

    /// <summary>Głębokość wody (m, 0…20) w punkcie świata (x, z); 0 = ląd.</summary>
    public static float Glebokosc(float x, float z)
    {
        float d = Model(x, z);
        float m = Wplyw(x, z, Plaza, PlazaPrzejscie);
        if (m > 0f) d = Mathf.Lerp(d, Mathf.Max(0f, BraxCityWaterBuild.WaterY - BraxNabrzezePlaza.Wysokosc(x, z)), m);
        float lx = x - BraxMiastoWyspa.CX, lz = z - BraxMiastoWyspa.CZ;
        if (lx * lx + lz * lz < WyspaZasieg * WyspaZasieg) d = Mathf.Min(d, Mathf.Max(0f, -BraxMiastoWyspa.H(lx, lz)));
        return Mathf.Clamp(d, 0f, MaxGlebokosc);
    }

    /// <summary>Model dna poza terenem plaży i wyspy (od linii brzegu miasta BraxMiastoGranice.Brzeg).</summary>
    public static float Model(float x, float z)
    {
        float s = x - BraxMiastoGranice.Brzeg(z);
        if (s <= 0f) return 0f;
        float port = (BraxCityWaterBuild.WaterY - DnoKolizjiY) + 4f * (1f - Mathf.Exp(-s / 150f));   // mur nabrzeża: 5.3 m, dalej do 9.3 m
        float plaza = Mathf.Min(s < 14f ? .09f * s : 1.26f + .05f * (s - 14f), port);              // jak piasek plaży: 1:11, potem 1:20, aż zrówna się z portem
        // przejście port → plaża przy cyplu (z 100…118) rozszerza się z odległością od brzegu (bez prostej granicy aż po horyzont)
        float zc = (BraxMiastoGranice.BrzegCypelZ0 + BraxMiastoGranice.BrzegCypelZ1) * .5f, szer = (BraxMiastoGranice.BrzegCypelZ1 - BraxMiastoGranice.BrzegCypelZ0) + 1.5f * s;
        float t = Mathf.Clamp01((z - zc) / szer + .5f);
        t = t * t * (3f - 2f * t);
        float d = Mathf.Lerp(port, plaza, t);
        d += 10f * (1f - Mathf.Exp(-Mathf.Max(0f, s - 400f) / 400f));                                // otwarte morze
        return d;
    }

    static float Wplyw(float x, float z, Rect r, float pas)
    {
        float dx = Mathf.Max(0f, Mathf.Max(r.xMin - x, x - r.xMax)), dz = Mathf.Max(0f, Mathf.Max(r.yMin - z, z - r.yMax));
        float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dz * dz) / pas);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Tekstura głębokości (R8, liniowa, bez mipmap, brzeg przedłużony — poza prostokątem wartość krawędzi).</summary>
    public static Texture2D Tekstura(out float plytkoM2)
    {
        var tex = new Texture2D(TexW, TexH, TextureFormat.R8, false, true)
        { name = "Dno morza miasta (sqrt głębokości / 20 m)", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 0 };
        var px = new byte[TexW * TexH];
        float sx = (Prostokat.z - Prostokat.x) / TexW, sz = (Prostokat.w - Prostokat.y) / TexH;
        plytkoM2 = 0f;
        for (int j = 0; j < TexH; j++)
        {
            float z = Prostokat.y + (j + .5f) * sz;
            for (int i = 0; i < TexW; i++)
            {
                float x = Prostokat.x + (i + .5f) * sx, d = Glebokosc(x, z);
                if (d > 0f && d < 3f) plytkoM2 += sx * sz;
                px[j * TexW + i] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(d / MaxGlebokosc) * 255f), 0, 255);
            }
        }
        // mury nabrzeży: piksel lądu tuż za pionowym murem dostaje głębokość wody przed murem (filtr dwuliniowy nie robi wtedy
        // z 5-metrowej wody przy murze „płycizny z piaskiem”); łagodne brzegi (plaża, wyspa) zostają, bo sąsiad nie jest głęboki
        var kopia = (byte[])px.Clone(); byte prog = (byte)Mathf.RoundToInt(Mathf.Sqrt(2.5f / MaxGlebokosc) * 255f);
        for (int j = 0; j < TexH; j++) for (int i = 0; i < TexW; i++)
        {
            if (kopia[j * TexW + i] != 0) continue;
            byte m = 0;
            for (int dj = -2; dj <= 2; dj++) for (int di = -2; di <= 2; di++)
            {
                int jj = j + dj, ii = i + di; if (jj < 0 || jj >= TexH || ii < 0 || ii >= TexW) continue;
                if (kopia[jj * TexW + ii] > m) m = kopia[jj * TexW + ii];
            }
            if (m >= prog) px[j * TexW + i] = m;
        }
        tex.SetPixelData(px, 0);
        tex.Apply(false, false);
        return tex;
    }
}
