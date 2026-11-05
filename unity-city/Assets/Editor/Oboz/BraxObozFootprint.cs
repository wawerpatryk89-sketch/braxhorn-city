using System;
using System.Collections.Generic;
using UnityEngine;

// Jedyny obrys obozu: współrzędne generatora. NIGDY nie skalujemy świata ani postaci.
// K=1.294 pozostaje w niezmienionych oryginalnych źródłach obozu.
// Zachowanie bramy w (0,-125) jest obowiązkowe.
public static class BraxObozFootprint
{
    public const float West = -78f;
    public const float East = 78f;
    public const float South = -125f;
    public const float North = 65f;
    public const float Corner = 22f;
    public const float WorldK = 1.294f;
    public const float PreviousWidth = 252f;
    public const float PreviousDepth = 240f;

    // Okrągłe naroża; zawartość 2D jest oddzielna od skali świata.
    public static float AreaBeforeK => (East-West)*(North-South) -
        (4f-Mathf.PI)*Corner*Corner;
    public static float AreaReduction => 1f-AreaBeforeK/(PreviousWidth*PreviousDepth);

    public static bool Contains(float x,float z,float insideMargin=0f)
    {
        if(insideMargin<0f) throw new ArgumentOutOfRangeException(nameof(insideMargin));
        float west=West+insideMargin, east=East-insideMargin;
        float south=South+insideMargin,north=North-insideMargin;
        if(x<west || x>east || z<south || z>north) return false;
        float radius=Mathf.Max(.001f,Corner-insideMargin);
        float cx=Mathf.Clamp(x,west+radius,east-radius);
        float cz=Mathf.Clamp(z,south+radius,north-radius);
        return (x-cx)*(x-cx)+(z-cz)*(z-cz)<=radius*radius+.0001f;
    }

    // Ciągła trasa (bez osobnych, rozłącznych prostokątów).
    // Zawsze ten sam układ wierzchołków przy różnych insetach: można tworzyć strip bieżni.
    public static List<Vector2> Outline(float inset,int arcSteps=12)
    {
        if(inset<0||inset>=Corner)throw new ArgumentOutOfRangeException(nameof(inset));
        arcSteps=Mathf.Max(4,arcSteps);
        float west=West+inset,east=East-inset;
        float south=South+inset,north=North-inset;
        float r=Corner-inset;
        var pts=new List<Vector2>(4*(arcSteps+1)+2) {
            new Vector2(west+r,south),new Vector2(east-r,south)
        };
        void Arc(float cx,float cz,float from,float to)
        {
            for(int i=1;i<=arcSteps;i++)
            {
                float a=Mathf.Lerp(from,to,i/(float)arcSteps)*Mathf.Deg2Rad;
                pts.Add(new Vector2(cx+Mathf.Cos(a)*r,cz+Mathf.Sin(a)*r));
            }
        }
        Arc(east-r,south+r,-90f,0f);
        pts.Add(new Vector2(east,north-r));
        Arc(east-r,north-r,0f,90f);
        pts.Add(new Vector2(west+r,north));
        Arc(west+r,north-r,90f,180f);
        pts.Add(new Vector2(west,south+r));
        Arc(west+r,south+r,180f,270f);
        // Zamknięta pętla; ostatni punkt = pierwszy.
        if(Vector2.Distance(pts[0],pts[pts.Count-1])>.001f)
            throw new InvalidOperationException("Nieciągły obwód obozu");
        return pts;
    }
}
