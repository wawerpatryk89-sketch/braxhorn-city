using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W=BraxWnetrzaKit;

// Adapted from BraxClaudeOsiedle.ArenaInterior, exact source recorded in arena-source.json.
// Same octagon, equipment and four-sided bowl; greater seating area, unscaled human-size furniture.
// This class creates only Mistrzowie objects and never calls the estate-wide build or mutates its assets.
public static class BraxChampionsArena
{
    const string Out="Assets/PlayableGenerated/Mistrzowie/Arena";
    static Transform colliders;
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    public static int SeatCount {get;private set;}
    public static int Generate(Transform building)
    {
        Directory.CreateDirectory(Out);materials.Clear();SeatCount=0;W.Wyczysc();
        var root=new GameObject("Champion Arena North | adapted estate bowl").transform;
        colliders=new GameObject("Arena structural collision").transform;colliders.SetParent(root,false);W.Start(root);
        CopiedEstateArena();W.Zapisz("mistrzowie_arena_estate_reuse");
        // Import the owner's actual 2K PBR barrel chairs beside judges.
        // No procedural replacement is made when the original GLB is missing.
        BraxChampionsFoundation.Optional("fotele VIP",()=>SeatCount += BraxChampionsVIPSeats.Generate(root));
        // One premium belt exhibit beside the actual judges, not at the public entrance.
        // This uses the owner's original 3D FBX and original framed logo artwork.
        BraxChampionsFoundation.Optional("gablota pasa",()=>BraxChampionsBeltShowcase.Generate(root));
        // Owner HD murals are a hard visual acceptance requirement. Optional()
        // still prevents an unrelated asset issue from destroying the whole city
        // scene, but the arena itself is explicitly reported FAIL unless 3/3 exist.
        bool entranceMural=false;int sideMurals=0;
        BraxChampionsFoundation.Optional("mural wejściowy",
            ()=>entranceMural=BraxChampionsArenaMural.Generate(root));
        BraxChampionsFoundation.Optional("murale boczne",
            ()=>sideMurals=BraxChampionsArenaSideMurals.Generate(root));
        if(!entranceMural||sideMurals!=2)
            BraxBuildQuality.Warn("A1_MURALS_ACCEPTANCE FAIL entrance="+entranceMural+
                " side="+sideMurals+"/2 required=3/3 ownerHD");
        else
            Debug.Log("A1_MURALS_ACCEPTANCE PASS 3/3 ownerHD left=true right=true entrance=true");
        // Source method uses nominal estate metres. Apply the game's K exactly once to this subtree.
        float k=BraxClaudeOsiedle.K;var rot=Quaternion.Euler(0,180,0);
        root.SetParent(building,false);root.localScale=Vector3.one*k;root.localRotation=rot;
        root.localPosition=-(rot*new Vector3(-86,0,63.3f))*k+Vector3.up*(.22f-.055f)*k;
        var visibility=root.gameObject.AddComponent<BraxWidocznoscWnetrza>();visibility.nazwa="Champion Arena North";
        // Building is temporarily local at zero while saving its reusable prefab.
        var centre=new Vector3(600*k,14*k,800*k);
        visibility.strefy=new[]{new Bounds(centre,new Vector3(180,60,135)*k)};
        if(SeatCount<600)BraxBuildQuality.Issue(new InvalidOperationException("Arena seating fell below the source estate arena"));
        return SeatCount;
    }
    static void CopiedEstateArena()
    {
        const float X0=-146f,X1=-26f,Z0=28.3f,Z1=98.3f,yF=.055f,yC=28f,cx=-86f,cz=63.3f,dA=-88.05f,dB=-83.95f,dH=4.2f;
        W.KolizjaMM(X0,X1,yF-.20f,yF,Z0,Z1);
        // Owner 7.11: the whole arena floor uses the exact greenish Poly Haven Stone Tiles.
        var floorStone = BraxChampionsFoundation.SourceMaterial(
            "arena_floor_stone_tiles_owner", "stone_tiles", 3.2f, Color.white);
        // A1 PREMIUM INTERIOR: real Poly Haven PBR, Android-imported at 2K by the
        // shared SourceMaterial pipeline. Murals remain separate owner artwork.
        var wallConcrete = BraxChampionsFoundation.SourceMaterial(
            "arena_plain_walls_concrete_wall_009", "concrete_wall_009", 2.4f,
            new Color(.54f,.55f,.57f));
        wallConcrete.SetFloat("_Roughness",.54f);
        var wallGranite = BraxChampionsFoundation.SourceMaterial(
            "arena_wall_lower_granite_tile", "granite_tile", .82f,
            new Color(.16f,.17f,.19f));
        wallGranite.SetFloat("_Roughness",.27f);
        var facadePanels = BraxChampionsFoundation.SourceMaterial(
            "arena_premium_rectangular_facade_tiles", "rectangular_facade_tiles", 1.35f,
            new Color(.20f,.21f,.23f));
        facadePanels.SetFloat("_Roughness",.34f);
        var interiorGold = BraxChampionsFoundation.LibraryMetal(
            "arena_interior_premium_gold", new Color(.835f,.639f,.231f));
        interiorGold.SetFloat("_Roughness",.19f);
        interiorGold.SetFloat("_Metallic",.98f);
        var stairVelour = BraxChampionsFoundation.SourceMaterial(
            "arena_stair_deep_red_velour", "velour_velvet", 1.30f,
            new Color(.47f,.035f,.045f));
        stairVelour.SetFloat("_Roughness",.58f);
        stairVelour.SetFloat("_Metallic",0f);
        var panel = facadePanels;
        var wallUp = facadePanels;
        var red = Plain("hala walk czerwień BRAXHORN", new Color(.60f, .04f, .05f), .45f);
        var ceil = Plain("hala walk sufit czarny mat", new Color(.09f, .09f, .10f), .05f);
        var black = Plain("oktagon czarny winyl", new Color(.03f, .03f, .035f), .55f);
        var steel = Plain("oktagon stal ciemna", new Color(.16f, .17f, .18f), .5f, .8f);
        var chrome = Plain("słupki chrom", new Color(.75f, .76f, .78f), .85f, .95f);
        var vipGold = Plain("słupki VIP złote", new Color(.88f, .64f, .18f), .88f, .96f);
        var carpet = Plain("hala walk chodnik czerwony", new Color(.45f, .03f, .04f), .1f);
        var rope = Plain("lina aksamitna czerwona", new Color(.42f, .03f, .05f), .25f);
        var tier = Plain("trybuny beton grafit", new Color(.15f, .15f, .16f), .3f);
        var nose = Plain("trybuny listwa aluminium", new Color(.55f, .56f, .58f), .55f, .6f);
        var seatRed = Plain("krzesełko trybun czerwone", new Color(.55f, .04f, .05f), .4f);
        var seatBlk = Plain("krzesełko trybun czarne", new Color(.04f, .04f, .045f), .4f);
        var leather = Plain("fotel VIP skóra czarna", new Color(.05f, .045f, .045f), .6f);
        var deskTop = Plain("marmur czarny polerowany", new Color(.035f, .035f, .04f), .88f);
        var screen = Plain("ekran wyłączony", new Color(.02f, .02f, .025f), .92f);
        var mat = TexMat("oktagon_mata", 1f, .15f);
        var apron = TexMat("oktagon_fartuch", 1f, .35f);
        var wire = CutoutMat("siatka_oktagonu", .3f);

        // Entire main arena floor: owner-selected green Stone Tiles.
        for (float x = X0; x < X1 - .01f; x += 2f)
            for (float z = Z0; z < Z1 - .01f; z += 2f) Top(floorStone, x, Mathf.Min(x + 2f, X1), z, Mathf.Min(z + 2f, Z1), yF);
        // PREMIUM wall finish. LEFT/RIGHT mural walls remain visually owned by
        // the artwork: these materials are behind the printed surface and are
        // visible only in the lower/upper margins. Gold dividers are never put
        // on top of a mural.
        void Clad(bool alongX, float f, float s, float a0, float a1)
        {
            float g0 = s > 0 ? f + .005f : f - .045f, g1 = g0 + .04f;
            if (alongX)
            {
                // Non-mural wall: 1.20m granite plinth + Concrete Wall 009 above.
                BoxMinMax(wallGranite,a0,a1,yF,1.20f,g0,g1,false);
                BoxMinMax(wallConcrete,a0,a1,1.20f,yC,g0,g1,false);
                // 35mm physical gold divisions every ~3.3m, set 15mm proud.
                for(float a=a0+3.30f;a<a1-.30f;a+=3.30f)
                {
                    float z=s>0?g1+.0075f:g0-.0075f;
                    BoxMinMax(interiorGold,a-.0175f,a+.0175f,1.20f,yC,
                        z-.0075f,z+.0075f,false);
                }
            }
            else
            {
                // Side mural wall: dark premium base behind the original print.
                // Mural itself is later placed in front and is NEVER covered.
                BoxMinMax(wallGranite,g0,g1,yF,1.20f,a0,a1,false);
                BoxMinMax(facadePanels,g0,g1,1.20f,yC,a0,a1,false);
            }
        }
        Clad(false, X0, 1f, Z0, Z1); Clad(false, X1, -1f, Z0, Z1);
        foreach (var (f, s) in new[] { (Z0, 1f), (Z1, -1f) })
        {
            Clad(true, f, s, X0, dA); Clad(true, f, s, dB, X1);
            float g0 = s > 0 ? f + .005f : f - .045f;
            BoxMinMax(wallConcrete, dA, dB, dH, yC, g0, g0 + .04f, false, true);
        }
        BoxMinMax(ceil, X0, X1, yC, yC + .02f, Z0, Z1, false, true);

        // oktagon: wierzchołki co 45° (boki prostopadłe do osi i przekątnych), bok i=1 (V1–V2) patrzy na +z — furtka i schody
        const float P = .6f, Rv = 4.6f; float ap = Rv * Mathf.Cos(Mathf.PI / 8f), yT = yF + P;
        Vector3 V(int i, float y) { float a = Mathf.PI / 8f + i * Mathf.PI / 4f; return new Vector3(cx + Rv * Mathf.Cos(a), y, cz + Rv * Mathf.Sin(a)); }
        Vector2 MatUV(Vector3 p) => new Vector2((cx + Rv - p.x) / (2f * Rv), (cz + Rv - p.z) / (2f * Rv));   // logo czytelne od wejścia (+z)
        float YawOf(Vector3 d) => Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
        var ctr = new Vector3(cx, yT, cz);
        for (int i = 0; i < 8; i++)
        {
            Vector3 a = V(i, yT), b = V(i + 1, yT);
            TriUV(mat, ctr, a, b, MatUV(ctr), MatUV(a), MatUV(b), Vector3.up);
            var n = (a + b) * .5f - ctr; n.y = 0; n.Normalize();
            var right = Vector3.Cross(Vector3.up, -n);
            bool aLeft = Vector3.Dot(a - b, right) < 0; Vector3 l = aLeft ? a : b, r = aLeft ? b : a;
            QuadUV(apron, new Vector3(l.x, yF, l.z), new Vector3(r.x, yF, r.z), new Vector3(r.x, yT, r.z), new Vector3(l.x, yT, l.z),
                   new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), n);
        }
        for (int i = 0; i < 8; i++)
        {
            var p = V(i, yT);
            Cylinder(steel, p, .07f, 1.95f, 12, true, false);
            Cylinder(black, p, .17f, 1.7f, 16, true, true);                           // obicie słupa (z kolizją)
            Cylinder(red, p + Vector3.up * 1.7f, .175f, .06f, 16, true, false);
        }
        for (int i = 0; i < 8; i++)
        {
            Vector3 a = V(i, yT), b = V(i + 1, yT), d = (b - a).normalized, mid = (a + b) * .5f;
            float len = Vector3.Distance(a, b), yw = YawOf(d);
            var n = mid - ctr; n.y = 0; n.Normalize();
            OBox(black, mid + Vector3.up * 1.88f, new Vector3(len - .2f, .14f, .16f), yw);                       // górna rura z obiciem
            if (i == 1) { OBox(steel, mid + Vector3.up * .012f, new Vector3(len - .34f, .02f, .3f), yw); continue; }   // furtka otwarta
            OBox(black, mid + Vector3.up * .11f, new Vector3(len - .3f, .22f, .1f), yw);                            // dolne obicie
            if (wire)
            {
                Vector3 a1 = a + d * .17f, b1 = b - d * .17f; float L = len - .34f, h0 = .22f, h1 = 1.81f;
                foreach (var nn in new[] { n, -n })
                    QuadUV(wire, a1 + Vector3.up * h0, b1 + Vector3.up * h0, b1 + Vector3.up * h1, a1 + Vector3.up * h1,
                           new Vector2(0, h0 / .3f), new Vector2(L / .3f, h0 / .3f), new Vector2(L / .3f, h1 / .3f), new Vector2(0, h1 / .3f), nn);
            }
            var g = new GameObject("kolizja siatki oktagonu"); g.transform.SetParent(colliders, false);
            g.transform.position = mid + Vector3.up * 1.1f; g.transform.rotation = Quaternion.Euler(0, yw, 0);
            g.AddComponent<BoxCollider>().size = new Vector3(len, 2.2f, .14f);
        }
        // kolizja podestu: ośmiokątny graniastosłup (siatka zapisana jako asset)
        var pm = new Mesh { name = "oktagon podest kolizja" };
        var pv = new List<Vector3> { new Vector3(cx, yT, cz), new Vector3(cx, 0f, cz) }; var pi = new List<int>();
        for (int i = 0; i < 8; i++) { pv.Add(V(i, yT)); pv.Add(V(i, 0f)); }
        for (int i = 0; i < 8; i++)
        {
            int t0 = 2 + 2 * i, b0 = t0 + 1, t1 = 2 + 2 * ((i + 1) % 8), b1 = t1 + 1;
            pi.AddRange(new[] { 0, t1, t0, 1, b0, b1, t0, t1, b1, t0, b1, b0 });
        }
        pm.SetVertices(pv); pm.SetTriangles(pi, 0); pm.RecalculateNormals(); pm.RecalculateBounds();
        BraxZapisAssetu.Utworz(pm, Out + "/oktagon_podest_kolizja.asset");
        var pg = new GameObject("kolizja podestu oktagonu"); pg.transform.SetParent(colliders, false); pg.AddComponent<MeshCollider>().sharedMesh = pm;
        // schody do furtki (3 × 0.2 m) + rampa kolizji
        float zs = cz + ap;
        for (int k = 0; k < 3; k++)
        {
            float z0 = zs + (2 - k) * .45f, top = yF + P * (k + 1) / 3f;
            BoxMinMax(black, cx - 1.1f, cx + 1.1f, 0f, top, z0, z0 + .45f, false);
            BoxMinMax(red, cx - 1.1f, cx + 1.1f, top - .03f, top + .003f, z0 + .41f, z0 + .452f, false);
        }
        {
            float run = 1.35f, ang = Mathf.Atan2(P, run) * Mathf.Rad2Deg, L = Mathf.Sqrt(run * run + P * P);
            var rg = new GameObject("rampa schodów oktagonu"); rg.transform.SetParent(colliders, false);
            rg.transform.rotation = Quaternion.Euler(ang, 0, 0);
            rg.transform.position = new Vector3(cx, (yF + yT) * .5f, zs + run * .5f) - rg.transform.up * .05f;
            rg.AddComponent<BoxCollider>().size = new Vector3(2.2f, .1f, L);
        }
        foreach (float sx in new[] { cx - 1.02f, cx + 1.02f })                  // audyt: słupki na stopniach (schody x ±1.1), nie w powietrzu
        {
            Cylinder(chrome, new Vector3(sx, yF + .2f, zs + 1.25f), .025f, .9f, 8, true, false);
            Cylinder(chrome, new Vector3(sx, yT, zs + .1f), .025f, .9f, 8, true, false);
            OBoxQ(chrome, new Vector3(sx, (yF + .2f + yT) * .5f + .9f, zs + .675f), new Vector3(.04f, .04f, 1.3f), Quaternion.Euler(Mathf.Atan2(yT - yF - .2f, 1.15f) * Mathf.Rad2Deg, 0, 0));
        }

        // trybuny: rząd 0.9 m głębokości, 0.4 m wyżej; krzesełko (stopka, siedzisko, oparcie) co 0.56 m
        void Seat(Material m, bool alongX, float a, float pc, float y, float s)
        {
            var p=alongX?new Vector3(a,y,pc):new Vector3(pc,y,a);
            PremiumSeat(m,p,alongX?(s>0?0f:180f):(s>0?90f:-90f));
            SeatCount++;
        }
        void Stand(bool alongX, float front, float s, float a0, float a1, int rows)
        {
            for (int k = 0; k < rows; k++)
            {
                float p0 = front - s * k * 1.05f, p1 = front - s * (k + 1) * 1.05f, top = yF + .4f * (k + 1);
                float lo = Mathf.Min(p0, p1), hi = Mathf.Max(p0, p1), e0 = Mathf.Min(p0, p0 - s * .05f), e1 = Mathf.Max(p0, p0 - s * .05f);
                if (alongX) { BoxMinMax(tier, a0, a1, 0f, top, lo, hi, true); BoxMinMax(nose, a0, a1, top - .03f, top + .004f, e0 - .002f, e1 + .002f, false); }
                else { BoxMinMax(tier, lo, hi, 0f, top, a0, a1, true); BoxMinMax(nose, e0 - .002f, e1 + .002f, top - .03f, top + .004f, a0, a1, false); }
                int j = 0;
                for (float a = a0 + .45f; a < a1 - .3f; a += .68f, j++) Seat((j + k) % 9 == 4 ? seatBlk : seatRed, alongX, a, p0 - s * .525f, top, s);
            }
        }
        // Same four-sided seating scheme as the estate, expanded without scaling furniture.
        // Six long-side blocks and four end blocks; 2m aisles remain clear between blocks.
        foreach(var interval in new[]{new Vector2(cz-23,cz-9),new Vector2(cz-7,cz+7),new Vector2(cz+9,cz+23)})
        { Stand(false,cx-24,1,interval.x,interval.y,16);Stand(false,cx+24,-1,interval.x,interval.y,16); }
        // Four end blocks leave a 6m central FRONT ENTRANCE AISLE all the way
        // down to the octagon. Spectators enter at ground level (no front steps).
        // Two 2m stair gaps are cut in the seating, one on either side.
        foreach(var interval in new[]{
            new Vector2(cx-22,cx-13),new Vector2(cx-11,cx-3),
            new Vector2(cx+3,cx+11),new Vector2(cx+13,cx+22)})
        {
            Stand(true,cz-15,1,interval.x,interval.y,16);
            Stand(true,cz+15,-1,interval.x,interval.y,16);
        }
        // Each stepped aisle is aligned with an actual gap in the seat blocks,
        // not a staircase placed directly across the public entry.
        foreach(float xx in new[]{cx-24,cx+24})
            foreach(float zz in new[]{cz-8,cz+8})
                TierStairs(false,xx,xx<cx?1:-1,zz,stairVelour,interiorGold);
        foreach(float zz in new[]{cz-15,cz+15})
            foreach(float side in new[]{-12f,12f})
                TierStairs(true,zz,zz<cz?1:-1,cx+side,stairVelour,interiorGold);
        // No substitute VIP chairs. The original owner GLB is required by the
        // pre-Unity SHA gate, then instantiated by BraxChampionsVIPSeats.
        // The nearby judges' seating, tables and screens remain unchanged.
        // stół komentatorów (zachód, przodem do klatki) i stół sędziów (wschód)
        foreach (var (x0, x1, s) in new[] { (-94.0f, -93.3f, 1f), (-78.7f, -78.0f, -1f) })
        {
            BoxMinMax(black, x0, x1, yF, .76f, 61.0f, 65.6f, true); BoxMinMax(deskTop, x0 - .05f, x1 + .05f, .76f, .8f, 60.9f, 65.7f, false, true);
            Sign("braxhorn_arena", new Vector3(s > 0 ? x1 + .02f : x0 - .02f, .44f, cz), 2.6f, .65f, new Vector3(s, 0, 0), false);
            foreach (float z in new[] { 61.8f, 63.3f, 64.8f })
            {
                float sx = s > 0 ? x0 + .15f : x1 - .15f;
                BoxMinMax(steel, sx - .02f, sx + .02f, .8f, .9f, z - .04f, z + .04f, false);
                BoxMinMax(screen, sx - .02f, sx + .02f, .9f, 1.2f, z - .26f, z + .26f, false, true);
                Seat(leather, false, z, s > 0 ? x0 - .75f : x1 + .75f, yF, s);
            }
        }
        // czerwony chodnik od drzwi z ulicy do schodów i od wyjścia zawodników do klatki; słupki z liną
        Top(carpet, -87.3f, -84.7f, zs + 1.37f, Z1 - .05f, yF + .006f);
        Top(carpet, -87.3f, -84.7f, Z0 + .05f, cz - ap - .08f, yF + .006f);
        foreach (float x in new[] { -87.55f, -84.45f })
        {
            for (float z = 70.0f; z <= 77.5f; z += 1.5f)
            {
                Cylinder(vipGold, new Vector3(x, yF, z), .14f, .04f, 16, true, false);
                Cylinder(vipGold, new Vector3(x, yF + .04f, z), .03f, .86f, 10, true, false);
                Cylinder(vipGold, new Vector3(x, yF + .9f, z), .05f, .06f, 10, true, false);
            }
            // Phone proof 8.11: straight rigid bars were wrong. Each 1.5m span
            // is a real soft red rope with a 13cm natural sag between GOLD posts.
            for (float z = 70.0f; z < 77.5f - .001f; z += 1.5f)
            {
                const int ropeSegments=6;
                for(int segment=0;segment<ropeSegments;segment++)
                {
                    float t0=segment/(float)ropeSegments,t1=(segment+1)/(float)ropeSegments;
                    float y0=.86f-.13f*4f*t0*(1f-t0);
                    float y1=.86f-.13f*4f*t1*(1f-t1);
                    W.Rura(rope,
                        new Vector3(x,y0,z+1.5f*t0),
                        new Vector3(x,y1,z+1.5f*t1),.022f,8);
                }
            }
        }
        // wejście zawodników: czarna rama z czerwonym pasem
        BoxMinMax(black, dA - .35f, dA, yF, dH + .35f, Z0 + .045f, Z0 + .3f, false); BoxMinMax(black, dB, dB + .35f, yF, dH + .35f, Z0 + .045f, Z0 + .3f, false);
        BoxMinMax(black, dA - .35f, dB + .35f, dH, dH + .35f, Z0 + .045f, Z0 + .3f, false, true);
        BoxMinMax(red, dA - .02f, dB + .02f, dH - .06f, dH, Z0 + .045f, Z0 + .31f, false, true);
        // kratownica nad klatką, reflektory, telebim
        const float tr = 5.6f, ty0 = 6.05f, ty1 = 6.35f;
        BoxMinMax(steel, cx - tr, cx + tr, ty0, ty1, cz - tr, cz - tr + .3f, false, true); BoxMinMax(steel, cx - tr, cx + tr, ty0, ty1, cz + tr - .3f, cz + tr, false, true);
        BoxMinMax(steel, cx - tr, cx - tr + .3f, ty0, ty1, cz - tr, cz + tr, false, true); BoxMinMax(steel, cx + tr - .3f, cx + tr, ty0, ty1, cz - tr, cz + tr, false, true);
        foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            Cylinder(steel, new Vector3(cx + o.x * (tr - .15f), ty1, cz + o.y * (tr - .15f)), .03f, yC - ty1, 8, false, false);
        for (int k = -2; k <= 2; k++)
            foreach (var (ox, oz) in new[] { (k * 2.1f, -tr + .15f), (k * 2.1f, tr - .15f), (-tr + .15f, k * 2.1f), (tr - .15f, k * 2.1f) })
            {
                if (Mathf.Abs(k) == 2 && Mathf.Abs(ox) > tr - .3f && Mathf.Abs(oz) > tr - .3f) continue;
                Cylinder(black, new Vector3(cx + ox, ty0 - .34f, cz + oz), .13f, .34f, 10, true, false);
            }
        BoxMinMax(black, cx - 1.3f, cx + 1.3f, 5.0f, 6.0f, cz - 1.3f, cz + 1.3f, false, true);
        Cylinder(steel, new Vector3(cx, 6.0f, cz), .05f, yC - 6.0f, 8, false, false);
        foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            Sign("braxhorn_arena", new Vector3(cx, 5.5f, cz) + n * 1.305f, 2.4f, .6f, n, false);
        // The owner has approved two full side-wall murals. Remove the ten
        // tiny repeated banners that would cover the originals or z-fight
        // against them. Preserve both the fighters' exit sign and entrance logo.
        Sign("hala_walk", new Vector3(cx, 4.75f, Z0 + .06f), 9.6f, 1.8f, Vector3.forward, false);
        Sign("braxhorn_arena", new Vector3(cx, 4.9f, Z1 - .06f), 6f, 1.5f, Vector3.back, false);
        // Four priority fixtures over the octagon. The built-in mobile CityStaticPBR
        // shader shades up to four ForceVertex local lamps per object. Strengthen the
        // EXISTING four rather than adding expensive shadow-casting spotlights.
        // 13m covers the cage and nearby floor; 2.15 is deliberately below a
        // harsh blown-out stadium floodlight. Check appearance on Android.
        var ringLed = LedDiffuser();
        foreach (var o in new[] { new Vector2(-3.4f, -3.4f), new Vector2(3.4f, -3.4f), new Vector2(-3.4f, 3.4f), new Vector2(3.4f, 3.4f) })
        {
            var lx = cx + o.x; var lz = cz + o.y;
            // Real visible fixture: dark steel housing + warm-white LED diffuser.
            BoxMinMax(steel, lx - .52f, lx + .52f, 5.68f, 5.78f, lz - .52f, lz + .52f, false);
            BoxMinMax(ringLed, lx - .42f, lx + .42f, 5.645f, 5.68f, lz - .42f, lz + .42f, false);
            PointLamp(new Vector3(lx, 5.3f, lz), 13f, 2.15f,
                new Color(1f, .96f, .90f), "Premium octagon overhead light");
        }
        // Architectural continuous light lines on the existing truss, emission-only:
        // no additional real-time lamps, shadow maps or duplicate bright materials.
        foreach (float side in new[] { -1f, 1f })
        {
            BoxMinMax(ringLed, cx - tr + .42f, cx + tr - .42f, 5.91f, 5.96f,
                cz + side * (tr - .15f) - .075f, cz + side * (tr - .15f) + .075f, false);
            BoxMinMax(ringLed, cx + side * (tr - .15f) - .075f,
                cx + side * (tr - .15f) + .075f, 5.91f, 5.96f,
                cz - tr + .42f, cz + tr - .42f, false);
        }
        foreach (var p in new[] { new Vector3(-99f, 4.6f, 57f), new Vector3(-99f, 4.6f, 67f), new Vector3(-73f, 4.6f, 57f), new Vector3(-73f, 4.6f, 67f),
                                  new Vector3(-92f, 4.6f, 75.5f), new Vector3(-80f, 4.6f, 75.5f), new Vector3(-92f, 4.6f, 53.5f), new Vector3(-80f, 4.6f, 53.5f) })
            PointLamp(p, 9f, .8f, new Color(1f, .86f, .7f));
        foreach(float x in new[]{cx-38,cx+38})foreach(float z in new[]{cz-18,cz,cz+18})
        {
            PointLamp(new Vector3(x,10.5f,z),18f,.85f,new Color(1f,.92f,.82f));
            W.Box(steel,new Vector3(x,10.8f,z),new Vector3(.8f,.16f,.45f));
            W.Rura(steel,new Vector3(x,10.88f,z),new Vector3(x,yC,z),.025f,6);
        }
        foreach(float z in new[]{cz-28,cz+28})foreach(float x in new[]{cx-13,cx+13})
        {
            PointLamp(new Vector3(x,10.5f,z),18f,.85f,new Color(1f,.92f,.82f));
            W.Box(steel,new Vector3(x,10.8f,z),new Vector3(.8f,.16f,.45f));
            W.Rura(steel,new Vector3(x,10.88f,z),new Vector3(x,yC,z),.025f,6);
        }
    }

    static Material Plain(string name,Color color,float smooth=.4f,float metal=0)
    {
        if(materials.TryGetValue(name,out var m))return m;
        string id="arena_"+materials.Count;
        if(name.Contains("krzesełko")||name.Contains("skóra")||name.Contains("winyl"))m=BraxChampionsFoundation.LibraryLeather(id,color);
        else if(metal>.1f||name.Contains("aluminium"))m=BraxChampionsFoundation.LibraryMetal(id,color);
        else if(name.Contains("chodnik")||name.Contains("aksamit")||name.Contains("akustyczne"))m=BraxChampionsFoundation.SourceMaterial(id,"fabric_pattern_05",.55f,color);
        // Premium: każda powierzchnia ma własny materiał zamiast jednego granitu.
        // Tylko źródła już pobierane do buildu (BRAX_MISTRZOWIE_PBR_SOURCE).
        else if(name.Contains("posadzka")||name.Contains("schody"))
            m=BraxChampionsFoundation.SourceMaterial(id,"concrete_floor_02",2.2f,Color.Lerp(color,Color.white,.25f));
        else if(name.Contains("trybuny")||name.Contains("sufit"))
            m=BraxChampionsFoundation.SourceMaterial(id,"concrete_wall_008",2.4f,color);
        else if(name.Contains("ściana"))
            m=BraxChampionsFoundation.SourceMaterial(id,"rectangular_facade_tiles",2.0f,color);
        else if(name.Contains("marmur"))
            m=BraxChampionsFoundation.SourceMaterial(id,"granite_tile",1.1f,color);
        else m=BraxChampionsFoundation.SourceMaterial(id,"granite_tile_02",1.5f,color);
        materials.Add(name,m);return m;
    }
    static Material LedDiffuser()
    {
        const string id="Premium octagon LED diffuser";
        if(materials.TryGetValue(id,out var existing))return existing;
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Original CityStaticPBR shader missing");
        // An actual emissive luminaire, not a replacement for the real PBR
        // surface maps of the arena (stone/metal/leather remain original).
        var m=new Material(shader){name=id,enableInstancing=true};
        m.SetColor("_Color",new Color(.83f,.88f,.92f));
        m.SetColor("_EmissionColor",new Color(1.15f,1.05f,.90f));
        m.SetFloat("_Roughness",.32f);
        m.SetFloat("_Metallic",0);
        m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Out+"/arena_led_diffuser.mat");
        materials.Add(id,m);
        return m;
    }

    static Material TexMat(string id,float metres,float gloss=.2f)
    {
        if(materials.TryGetValue(id,out var m))return m;
        string path="Assets/Source/Mistrzowie/ArenaGraphics/"+id+".png";
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!t)throw new FileNotFoundException("Original estate arena graphic required",path);
        m=new Material(Shader.Find(BraxCityMaterials.ShaderName)){name="Mistrzowie copied arena graphic "+id,enableInstancing=true};
        m.mainTexture=t;m.SetFloat("_VertexTint",0);m.SetFloat("_Metallic",0);m.SetFloat("_Roughness",1-gloss);
        AssetDatabase.CreateAsset(m,Out+"/"+id+".mat");materials.Add(id,m);return m;
    }
    static Material CutoutMat(string id,float metres)
    {
        var m=TexMat(id,metres,.45f);m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.SetFloat("_Cull",0);
        m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;return m;
    }
    static void BoxMinMax(Material m,float x0,float x1,float y0,float y1,float z0,float z1,bool solid=true,bool bottom=false)
        =>W.MM(m,x0,x1,y0,y1,z0,z1,solid);
    static void OBox(Material m,Vector3 p,Vector3 size,float yaw)=>W.OBox(m,p,size,Quaternion.Euler(0,yaw,0));
    static void OBoxQ(Material m,Vector3 p,Vector3 size,Quaternion q)=>W.OBox(m,p,size,q);
    static void Cylinder(Material m,Vector3 p,float radius,float height,int segments=16,bool top=true,bool solid=true)
        =>W.Walec(m,p,radius,height,segments,solid);
    static void Top(Material m,float x0,float x1,float z0,float z1,float y)
        =>W.Quad(m,new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0));
    static void QuadUV(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud,Vector3 normal)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0)W.QuadUV(m,d,c,b,a,ud,uc,ub,ua);
        else W.QuadUV(m,a,b,c,d,ua,ub,uc,ud);
    }
    static void TriUV(Material m,Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc,Vector3 normal)
    {
        // Split the triangle at one edge to use the shared quad batch without zero-area triangles.
        var mid=(b+c)*.5f;QuadUV(m,a,b,mid,c,ua,ub,(ub+uc)*.5f,uc,normal);
    }
    static void Sign(string id,Vector3 at,float width,float height,Vector3 n,bool _)
    {
        // Screen-right for a viewer looking opposite the outward normal is n x Up.
        // The previous Up x n mirrored every internal BRAXHORN panel on phone.
        var right=Vector3.Cross(n,Vector3.up)*width*.5f;var up=Vector3.up*height*.5f;
        QuadUV(TexMat(id,1),at-right-up,at+right-up,at+right+up,at-right+up,Vector2.zero,Vector2.right,Vector2.one,Vector2.up,n);
    }
    static void PointLamp(Vector3 p,float range,float intensity,Color color,string name="Arena seating light")
    {
        var o=new GameObject(name);o.transform.SetParent(W.Grupa,false);o.transform.localPosition=p;
        var l=o.AddComponent<Light>();l.type=LightType.Point;l.range=range*BraxClaudeOsiedle.K;l.intensity=intensity;l.color=color;
        l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForceVertex;
    }
    static void PremiumSeat(Material upholstery,Vector3 p,float yaw)
    {
        var q=Quaternion.Euler(0,yaw,0);Vector3 P(float x,float y,float z)=>p+q*new Vector3(x,y,z);
        var metal=Plain("siedziska metalowa podstawa",new Color(.14f,.13f,.11f),.55f,.85f);
        W.Box(metal,P(0,.04f,0),new Vector3(.36f,.08f,.40f));
        W.Rura(metal,P(0,.04f,-.08f),P(0,.43f,-.08f),.045f,8);
        Cushion(upholstery,P(0,.46f,.01f),new Vector3(.54f,.13f,.51f),q);
        Cushion(upholstery,P(0,.79f,-.25f),new Vector3(.54f,.56f,.13f),q*Quaternion.Euler(-8,0,0));
        foreach(float side in new[]{-.29f,.29f})
        {
            W.Rura(metal,P(side,.40f,-.1f),P(side,.66f,-.1f),.022f,6);
            Cushion(upholstery,P(side,.66f,0),new Vector3(.055f,.055f,.36f),q);
        }
    }
    static void Cushion(Material m,Vector3 p,Vector3 size,Quaternion q)
    {
        // Three rounded rings form real padded edges; no rectangular slab used as upholstery.
        int n=16;var rings=new Vector3[3][];float rad=Mathf.Min(size.x,size.z)*.16f;
        for(int layer=0;layer<3;layer++)
        {
            rings[layer]=new Vector3[n];float shrink=layer==1?0:rad*.35f;
            for(int i=0;i<n;i++)
            {
                int corner=i/4;float a=(corner*90+i%4*30)*Mathf.Deg2Rad;
                float sx=corner==0||corner==3?1:-1,sz=corner<2?1:-1;
                var v=new Vector3(sx*(size.x*.5f-rad-shrink)+Mathf.Cos(a)*rad,
                    (layer-1)*size.y*.5f,sz*(size.z*.5f-rad-shrink)+Mathf.Sin(a)*rad);
                rings[layer][i]=p+q*v;
            }
        }
        for(int j=0;j<2;j++)for(int i=0;i<n;i++){int k=(i+1)%n;W.Quad(m,rings[j+1][i],rings[j+1][k],rings[j][k],rings[j][i]);}
        for(int i=0;i<n;i+=2)
        {
            int j=(i+1)%n,k=(i+2)%n;
            W.Quad(m,p+q*Vector3.up*size.y*.5f,rings[2][k],rings[2][j],rings[2][i]);
            W.Quad(m,p-q*Vector3.up*size.y*.5f,rings[0][i],rings[0][j],rings[0][k]);
        }
    }
    static void TierStairs(bool alongX,float front,float facing,float centre,
        Material carpet,Material gold)
    {
        var stone=Plain("schody trybun",new Color(.28f,.27f,.25f));
        const float stairHalf=.85f;       // 1.70m structural aisle
        const float carpetHalf=.65f;      // 1.30m red velour
        const float goldWidth=.020f;
        const float overlay=.009f;
        for(int i=0;i<32;i++)
        {
            float raw0=front-facing*i*.525f,raw1=front-facing*(i+1)*.525f;
            float lo=Mathf.Min(raw0,raw1),hi=Mathf.Max(raw0,raw1);
            float top=.055f+(i+1)*.2f,previous=.055f+i*.2f;
            if(alongX)
            {
                W.MM(stone,centre-stairHalf,centre+stairHalf,0,top,lo,hi,true);
                // Tread carpet: physical overlay, never coplanar.
                W.MM(carpet,centre-carpetHalf,centre+carpetHalf,top+.003f,top+.003f+overlay,
                    lo+.008f,hi-.008f,false);
                // Riser carpet at the approach edge of each step.
                float z=raw0;
                W.MM(carpet,centre-carpetHalf,centre+carpetHalf,previous,top,
                    z-.004f,z+.004f,false);
                foreach(float side in new[]{-1f,1f})
                {
                    float x=centre+side*(carpetHalf+goldWidth*.5f);
                    W.MM(gold,x-goldWidth*.5f,x+goldWidth*.5f,
                        top+.003f,top+.003f+overlay,lo+.008f,hi-.008f,false);
                }
            }
            else
            {
                W.MM(stone,lo,hi,0,top,centre-stairHalf,centre+stairHalf,true);
                W.MM(carpet,lo+.008f,hi-.008f,top+.003f,top+.003f+overlay,
                    centre-carpetHalf,centre+carpetHalf,false);
                float x=raw0;
                W.MM(carpet,x-.004f,x+.004f,previous,top,
                    centre-carpetHalf,centre+carpetHalf,false);
                foreach(float side in new[]{-1f,1f})
                {
                    float z=centre+side*(carpetHalf+goldWidth*.5f);
                    W.MM(gold,lo+.008f,hi-.008f,top+.003f,top+.003f+overlay,
                        z-goldWidth*.5f,z+goldWidth*.5f,false);
                }
            }
        }
        Debug.Log("A1_PREMIUM_TIER_STAIRS width=1.70 carpet=1.30 steps=32 tread=.525 riser=.20"+
            " red_velour=true gold_edges_20mm=true carpet_surface_per_run=30.16m2");
    }
}
