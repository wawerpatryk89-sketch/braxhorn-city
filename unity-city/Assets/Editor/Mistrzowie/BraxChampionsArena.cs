using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W=BraxWnetrzaKit;

// Adapted from BraxClaudeOsiedle.ArenaInterior, exact source recorded in arena-source.json.
// Same octagon, equipment and four-sided bowl; greater seating area. Real sizes, root scaled by K once.
// This class creates only Mistrzowie objects and never calls the estate-wide build or mutates its assets.
public static class BraxChampionsArena
{
    const string Out="Assets/PlayableGenerated/Mistrzowie/Arena";
    // Structural hall wall depth in nominal metres (x K = 0.58 m in game).
    public const float WallThickness=.45f;
    // Lobby face of the front (Z1=98.3) wall: inner face 98.255 + depth.
    public const float FrontWallLobbyFaceZ=98.3f-.045f+WallThickness;
    // Finish heights of the hall (bowl metres). The exterior roof slab's underside is at 26.885: the designed
    // ceiling hangs just below it, so nothing of the finish is hidden inside the roof.
    // Octagon side with the gate (V5-V6 faces -z: the fighters' side). Owner 10.11 12:38.
    public const int GateSide=5;
    public const float WallStoneTop=7.80f, WallPanelTop=23.90f, CeilingY=26.62f, CeilingBeamDrop=.62f, StandStoneTop=1.05f;
    // Owner 10.11 07:25, from the material board: "A28 ściany w środku, tam gdzie jest oktagon" = ambientCG Leather013,
    // deep oxblood quilted (Chesterfield) leather. A 1.20 m black marble plinth below it (nobody kicks leather).
    public const float WallPlinthTop=1.20f, WallLeatherTile=3.6f;
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
        // 9.11: the belt vitrine moved to the lobby (BraxChampionsArenaArrival).
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
        // yC: top of the hall walls and of the plate over the designed ceiling. It was 28 (bowl) = 28.165 m outside, 0.8 m
        // OVER the exterior roof (top 27.35): a 120 x 70 m black box stood on the roof, seen from the Hotel Sobczak and hiding
        // the BRAXHORN bull mark (owner 10.11 19:14). Now the plate's top is flush under the roof slab (underside 26.885 bowl).
        const float X0=-146f,X1=-26f,Z0=28.3f,Z1=98.3f,yF=.055f,yC=26.865f,cx=-86f,cz=63.3f,dA=-88.05f,dB=-83.95f,dH=4.2f;
        W.KolizjaMM(X0,X1,yF-.20f,yF,Z0,Z1);
        // Owner 7.11: the whole arena floor uses the exact greenish Poly Haven Stone Tiles.
        var floorStone = BraxChampionsFoundation.SourceMaterial(
            "arena_floor_stone_tiles_owner", "stone_tiles", 3.2f, Color.white);
        // A1 PREMIUM INTERIOR: real PBR maps, Android-imported at 2K by the shared SourceMaterial pipeline.
        // Murals remain separate owner artwork.
        // Clean premium gold: no photographed/scratched metal texture.
        var interiorGold = Plain("arena interior premium gold",
            new Color(.835f,.639f,.231f),.19f,.98f);
        var stairVelour = BraxChampionsFoundation.SourceMaterial(
            "arena_stair_deep_red_velour", "velour_velvet", 1.30f,
            new Color(.47f,.035f,.045f));
        stairVelour.SetFloat("_Roughness",.58f);
        stairVelour.SetFloat("_Metallic",0f);
        BraxChampionsGold.Apply(interiorGold);   // owner 10.11: "wszystko, co złote, ma być złote"
        // Owner 9.11 23:43-23:46: "każda ściana ma być wykończona, te trybuny z boku, wszystko. Sufit tak samo."
        // Owner 10.11 00:51-01:10 on the walnut version: "te kolory takie obsrane ... skończ z tym sraczkowatym
        // kolorem", and on the lobby wall with the belt: "tylko to wygląda premium" (whole wall like that).
        // So the WHOLE hall speaks the language of that wall: polished black marble (Marble016, the very material of
        // the lobby wall) in 3 m slabs, thin gold lines and warm light lines. No walnut, no beige, no brown anywhere.
        var acoustic = Plain("hala walk panele akustyczne czarne", new Color(.105f,.105f,.115f), .1f);
        var lobbyMarble = BraxChampionsFoundation.LibraryMarble(
            "arena_lobby_wall_black_marble", new Color(.95f,.93f,.91f), 3.0f);
        var marble = lobbyMarble;                       // one material for every marble face of the hall
        var wallLeather = BraxChampionsFoundation.LibraryAcg(
            "arena_hall_wall_quilted_leather_A28", "Leather013", Color.white, WallLeatherTile);
        var beamBlack = Plain("hala walk belki sufitu czarna stal satyna", new Color(.055f,.055f,.06f), .55f, .6f);
        var warmLine = WarmLine();
        var red = Plain("hala walk czerwień BRAXHORN", new Color(.60f, .04f, .05f), .45f);
        var ceil = Plain("hala walk sufit czarny mat", new Color(.09f, .09f, .10f), .05f);
        var black = Plain("oktagon czarny winyl", new Color(.03f, .03f, .035f), .55f);
        var steel = Plain("oktagon stal ciemna", new Color(.16f, .17f, .18f), .5f, .8f);
        var chrome = Plain("słupki chrom", new Color(.75f, .76f, .78f), .85f, .95f);
        // Owner 10.11 15:24-15:31 ("najebane tego złota ... nie znaczy, że chcę mieć całą grę w złocie"; asked which hall
        // gold goes: "Poręcze schodów trybun"): the aisle handrails and their posts are black satin steel now.
        var railBlack = Plain("trybuny poręcze czarna stal satyna", new Color(.045f,.045f,.05f), .55f, .65f);
        var vipGold = Plain("słupki VIP złote", new Color(.88f, .64f, .18f), .88f, .96f);
        BraxChampionsGold.Apply(vipGold);
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
        // Owner 9.11: "the wall with the mural behind the glass is paper-thin".
        // Every hall wall is now a real structural wall WallThickness deep. Its
        // INNER face stays exactly where the old 4 cm cladding face was, so the
        // stands, stairs, signs and side murals keep their positions; the extra
        // depth grows OUTWARD (lobby / concourse side). Solid = player collision.
        void Clad(bool alongX, float f, float s, float a0, float a1)
        {
            float inner = s > 0 ? f + .045f : f - .045f;
            float outer = inner - s * WallThickness;
            float g0 = Mathf.Min(inner, outer), g1 = Mathf.Max(inner, outer);
            if (alongX)
            {
                // Long wall: black marble plinth (the lobby wall's material) with a gold skirting, the owner's quilted
                // leather A28 up to 23.90 m, black acoustic fabric under the ceiling. Light line + gold beads at both joints.
                BoxMinMax(marble,a0,a1,yF,WallPlinthTop,g0,g1,true);
                BoxMinMax(wallLeather,a0,a1,WallPlinthTop,WallPanelTop,g0,g1,true);
                BoxMinMax(acoustic,a0,a1,WallPanelTop,yC,g0,g1,true);
                {
                    float face=s>0?g1:g0;
                    BoxMinMax(interiorGold,a0,a1,yF,yF+.14f,s>0?face:face-.012f,s>0?face+.012f:face,false);   // gold skirting
                }
                {
                    float face=s>0?g1:g0,z0=s>0?face:face-.035f,z1=s>0?face+.035f:face;
                    foreach(float y in new[]{WallPlinthTop,WallPanelTop})
                    {
                        BoxMinMax(warmLine,a0,a1,y-.05f,y+.05f,z0,z1,false);
                        BoxMinMax(interiorGold,a0,a1,y+.05f,y+.075f,s>0?face:face-.045f,s>0?face+.045f:face,false);
                        BoxMinMax(interiorGold,a0,a1,y-.075f,y-.05f,s>0?face:face-.045f,s>0?face+.045f:face,false);
                    }
                }
                // Owner 10.11 15:22-15:24 (APK photo of the hall): "te pionowe kreski, których nikt nie chciał ... na tym
                // pikowanym ... wszystkie w pizdu", "najebane tego złota". The 35 mm vertical gold divisions every 3.3 m
                // on the quilted leather are gone: the leather runs unbroken from the plinth to the acoustic band.
            }
            else
            {
                // Side mural wall: the same marble plinth and quilted leather behind and around the owner's print (the
                // print hangs in front and is NEVER covered), gold skirting, black fabric above the ceiling line.
                BoxMinMax(marble,g0,g1,yF,WallPlinthTop,a0,a1,true);
                BoxMinMax(wallLeather,g0,g1,WallPlinthTop,CeilingY,a0,a1,true);
                BoxMinMax(acoustic,g0,g1,CeilingY,yC,a0,a1,true);
                {
                    float face=s>0?g1:g0;
                    BoxMinMax(interiorGold,s>0?face:face-.012f,s>0?face+.012f:face,yF,yF+.14f,a0,a1,false);   // gold skirting
                }
            }
        }
        // Side walls run past both end walls by the wall depth: closed corners.
        Clad(false, X0, 1f, Z0 - WallThickness, Z1 + WallThickness);
        Clad(false, X1, -1f, Z0 - WallThickness, Z1 + WallThickness);
        foreach (var (f, s) in new[] { (Z0, 1f), (Z1, -1f) })
        {
            Clad(true, f, s, X0, dA); Clad(true, f, s, dB, X1);
            // Full-depth lintel over the 4.10 m doorway: the opening shows a
            // real 45 cm reveal on both sides and on top, not a 4 cm edge.
            float inner = s > 0 ? f + .045f : f - .045f, outer = inner - s * WallThickness;
            float w0=Mathf.Min(inner, outer),w1=Mathf.Max(inner, outer);
            BoxMinMax(wallLeather, dA, dB, dH, WallPanelTop, w0, w1, true, true);
            BoxMinMax(acoustic, dA, dB, WallPanelTop, yC, w0, w1, true, true);
            float lf=s>0?w1:w0;
            foreach(float y in new[]{WallPanelTop})
                BoxMinMax(warmLine,dA,dB,y-.05f,y+.05f,s>0?lf:lf-.035f,s>0?lf+.035f:lf,false);
        }
        BoxMinMax(ceil, X0, X1, yC, yC + .02f, Z0, Z1, false, true);
        HallCeiling(X0,X1,Z0,Z1,cx,cz,acoustic,beamBlack,warmLine,interiorGold);
        LobbyWallMarble(X0,X1,Z1,yF,dA,dB,dH,lobbyMarble,interiorGold,warmLine);

        // oktagon: wierzchołki co 45° (boki prostopadłe do osi i przekątnych). Owner 10.11 12:38 (APK 37): "tędy wchodzą
        // kibice, a tu jest wejście na ring. Wejście na ring powinno być z drugiej strony, tam gdzie wchodzą zawodnicy.
        // Sens, logika zachowana wszędzie." The gate and its steps face -z (bok i=5, V5-V6): the fighters' door (Z0),
        // their red-carpet walkout and the gate are one line; the public (Z1) goes to the stands, never to the cage.
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
            if (i == GateSide) { OBox(steel, mid + Vector3.up * .012f, new Vector3(len - .34f, .02f, .3f), yw); continue; }   // furtka otwarta (strona zawodników)
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
        // schody do furtki (3 × 0.2 m) + rampa kolizji, od strony zawodników (-z, ku drzwiom Z0)
        float zs = cz - ap;
        for (int k = 0; k < 3; k++)
        {
            float z0 = zs - (3 - k) * .45f, top = yF + P * (k + 1) / 3f;
            BoxMinMax(black, cx - 1.1f, cx + 1.1f, 0f, top, z0, z0 + .45f, false);
            BoxMinMax(red, cx - 1.1f, cx + 1.1f, top - .03f, top + .003f, z0 - .002f, z0 + .04f, false);   // red nosing on the outer edge
        }
        {
            float run = 1.35f, ang = Mathf.Atan2(P, run) * Mathf.Rad2Deg, L = Mathf.Sqrt(run * run + P * P);
            var rg = new GameObject("rampa schodów oktagonu"); rg.transform.SetParent(colliders, false);
            rg.transform.rotation = Quaternion.Euler(-ang, 0, 0);
            rg.transform.position = new Vector3(cx, (yF + yT) * .5f, zs - run * .5f) - rg.transform.up * .05f;
            rg.AddComponent<BoxCollider>().size = new Vector3(2.2f, .1f, L);
        }
        foreach (float sx in new[] { cx - 1.02f, cx + 1.02f })                  // audyt: słupki na stopniach (schody x ±1.1), nie w powietrzu
        {
            Cylinder(chrome, new Vector3(sx, yF + .2f, zs - 1.25f), .025f, .9f, 8, true, false);
            Cylinder(chrome, new Vector3(sx, yT, zs - .1f), .025f, .9f, 8, true, false);
            OBoxQ(chrome, new Vector3(sx, (yF + .2f + yT) * .5f + .9f, zs - .675f), new Vector3(.04f, .04f, 1.3f), Quaternion.Euler(-Mathf.Atan2(yT - yF - .2f, 1.15f) * Mathf.Rad2Deg, 0, 0));
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
                // Owner 10.11 13:05 (zero flicker, nothing laid on anything): the aluminium nosing is a real 40 x 58 mm bar
                // standing 10 mm above the tread and 8 mm proud of the riser, its ends 6 mm inside the block ends - no face
                // of it lies in the plane of the concrete.
                float lo = Mathf.Min(p0, p1), hi = Mathf.Max(p0, p1), e0 = Mathf.Min(p0 + s * .008f, p0 - s * .05f), e1 = Mathf.Max(p0 + s * .008f, p0 - s * .05f);
                if (alongX) { BoxMinMax(tier, a0, a1, 0f, top, lo, hi, true); BoxMinMax(nose, a0 + .006f, a1 - .006f, top - .03f, top + .010f, e0, e1, false); }
                else { BoxMinMax(tier, lo, hi, 0f, top, a0, a1, true); BoxMinMax(nose, e0, e1, top - .03f, top + .010f, a0 + .006f, a1 - .006f, false); }
                int j = 0;
                for (float a = a0 + .45f; a < a1 - .3f; a += .68f, j++) Seat((j + k) % 9 == 4 ? seatBlk : seatRed, alongX, a, p0 - s * .525f, top, s);
            }
            // Back of the block (6.45 m of bare tier concrete toward the rear concourse): black marble + light line + gold cap.
            float back=front-s*rows*1.05f,topAll=yF+.4f*rows,b0=s>0?back-.04f:back,b1=s>0?back:back+.04f;
            void BackBox(Material m,float y0,float y1,float extra=0f)
            {
                float e0=s>0?b0-extra:b0,e1=s>0?b1:b1+extra;
                if(alongX)BoxMinMax(m,a0,a1,y0,y1,e0,e1,false);else BoxMinMax(m,e0,e1,y0,y1,a0,a1,false);
            }
            BackBox(marble,yF,StandStoneTop);BackBox(marble,StandStoneTop+.06f,topAll);
            BackBox(warmLine,StandStoneTop,StandStoneTop+.06f,.012f);BackBox(interiorGold,topAll,topAll+.05f,.03f);
        }
        // Side of a block toward an open aisle (owner photo: a black wall beside the red carpet). Every row gets its
        // own black marble panel 4 cm proud, a gold edge on the tread line, a light line at the foot and a black
        // satin-steel handrail on posts that follows the rake (owner 10.11: no gold handrails).
        void StandSide(bool alongX,float front,float s,float edge,float into,int rows)
        {
            // edge: coordinate of the side face along the block; into: +1 / -1 = direction from the face into the aisle
            float e0=into>0?edge:edge-.04f,e1=into>0?edge+.04f:edge;
            void Side(Material m,float y0,float y1,float lo,float hi,float extra=0f)
            {
                float f0=into>0?e0:e0-extra,f1=into>0?e1+extra:e1;
                if(alongX)BoxMinMax(m,f0,f1,y0,y1,lo,hi,false);else BoxMinMax(m,lo,hi,y0,y1,f0,f1,false);
            }
            Vector3 At(float along,float y)=>alongX?new Vector3(edge+into*.10f,y,along):new Vector3(along,y,edge+into*.10f);
            for(int k=0;k<rows;k++)
            {
                float p0=front-s*k*1.05f,p1=front-s*(k+1)*1.05f,top=yF+.4f*(k+1),lo=Mathf.Min(p0,p1),hi=Mathf.Max(p0,p1);
                Side(marble,yF,Mathf.Min(top,StandStoneTop),lo,hi);
                if(top>StandStoneTop+.06f)Side(marble,StandStoneTop+.06f,top,lo,hi);
                // gold edge of the tread and of the riser: 6 mm proud of the riser face and stopping 6 mm short of the
                // block's back, the riser strip ends under the tread strip - no gold face in a marble plane (zero flicker)
                float tf=p0+s*.006f,tb=p1+s*.006f;
                Side(interiorGold,top-.035f,top+.012f,Mathf.Min(tf,tb),Mathf.Max(tf,tb),.012f);
                Side(interiorGold,top-.4f,top-.035f,Mathf.Min(tf,p0-s*.03f),Mathf.Max(tf,p0-s*.03f),.012f);
                // handrail post on every row and the raked rail to the next one
                var foot=At(p0-s*.525f,top);var head=foot+Vector3.up*.95f;
                W.Rura(railBlack,foot,head,.022f,8);
                if(k+1<rows)W.Rura(railBlack,head,head+new Vector3(alongX?0f:-s*1.05f,.4f,alongX?-s*1.05f:0f),.026f,8);
            }
            float backP=front-s*rows*1.05f;
            // the light line starts behind the gold riser strip of row 3 (never in the same face as the gold)
            float lineFront=front-s*(1.05f*2f+.03f);
            Side(warmLine,StandStoneTop,StandStoneTop+.06f,Mathf.Min(lineFront,backP),Mathf.Max(lineFront,backP),.012f);
        }
        // Owner 9.11: "the stands must join at the corners". The long sides
        // (front line x=cx±24) and the ends (front line z=cz±15) now meet in a
        // continuous bowl: each straight block stops exactly on the other side's
        // front line and a quarter-ring of the SAME 16 rows (1.05 m deep, 0.40 m
        // rise) fills the corner around the point where the two front lines cross.
        // All aisles (cz±8 on the sides, cx±12 and the 6 m centre on the ends)
        // and both octagon walkways keep their exact positions.
        const int Rows=16;const float RowDepth=1.05f,RowRise=.4f,SideFront=24f,EndFront=15f;
        foreach(var interval in new[]{new Vector2(cz-EndFront,cz-9),new Vector2(cz-7,cz+7),new Vector2(cz+9,cz+EndFront)})
        { Stand(false,cx-SideFront,1,interval.x,interval.y,Rows);Stand(false,cx+SideFront,-1,interval.x,interval.y,Rows); }
        // Four end blocks leave a 6m central FRONT ENTRANCE AISLE all the way
        // down to the octagon. Spectators enter at ground level (no front steps).
        // Two 2m stair gaps are cut in the seating, one on either side.
        foreach(var interval in new[]{
            new Vector2(cx-SideFront,cx-13),new Vector2(cx-11,cx-3),
            new Vector2(cx+3,cx+11),new Vector2(cx+13,cx+SideFront)})
        {
            Stand(true,cz-EndFront,1,interval.x,interval.y,Rows);
            Stand(true,cz+EndFront,-1,interval.x,interval.y,Rows);
        }
        // Both 6 m centre aisles (public entrance and fighters' walk): the four tall stand sides.
        foreach(float side in new[]{-1f,1f})
        {
            StandSide(true,cz-EndFront,1,cx+side*3f,-side,Rows);
            StandSide(true,cz+EndFront,-1,cx+side*3f,-side,Rows);
        }
        int cornerSeats=0;
        foreach(float sx in new[]{-1f,1f})
            foreach(float sz in new[]{-1f,1f})
            {
                // Corner centre: crossing of the side and end FRONT lines.
                var c=new Vector3(cx+sx*SideFront,0f,cz+sz*EndFront);
                for(int k=0;k<Rows;k++)
                {
                    float r0=k*RowDepth,r1=(k+1)*RowDepth,rm=(r0+r1)*.5f,top=yF+RowRise*(k+1);
                    // Tier mass: angular segments no wider than ~1.2 m at the back edge.
                    int parts=Mathf.Max(2,Mathf.CeilToInt(Mathf.PI*.5f*r1/1.2f));
                    float step=90f/parts;
                    for(int p=0;p<parts;p++)
                    {
                        float a=(p+.5f)*step*Mathf.Deg2Rad;
                        var d=new Vector3(sx*Mathf.Cos(a),0f,sz*Mathf.Sin(a));
                        float chord=2f*r1*Mathf.Sin(step*.5f*Mathf.Deg2Rad)+.02f;
                        var q=Quaternion.LookRotation(d,Vector3.up);
                        W.OBox(tier,c+d*rm+Vector3.up*(top*.5f),new Vector3(chord,top,RowDepth),q,true);
                        if(k==Rows-1)
                        {
                            // curved back of the corner bowl: same black marble / light line / gold cap as the straight blocks
                            float bc=2f*(r1+.04f)*Mathf.Sin(step*.5f*Mathf.Deg2Rad)+.02f;var at=c+d*(r1+.02f);
                            W.OBox(marble,at+Vector3.up*((yF+StandStoneTop)*.5f),new Vector3(bc,StandStoneTop-yF,.04f),q,false);
                            W.OBox(warmLine,at+d*.006f+Vector3.up*(StandStoneTop+.03f),new Vector3(bc,.06f,.052f),q,false);
                            W.OBox(marble,at+Vector3.up*((StandStoneTop+.06f+top)*.5f),new Vector3(bc,top-StandStoneTop-.06f,.04f),q,false);
                            W.OBox(interiorGold,at+d*.015f+Vector3.up*(top+.025f),new Vector3(bc,.05f,.07f),q,false);
                        }
                        if(k>0)
                        {
                            // Aluminium nosing on the front (inner) edge of the step.
                            // same real bar as the straight blocks: 10 mm above the tread, 8 mm proud of the riser
                            float noseChord=2f*r0*Mathf.Sin(step*.5f*Mathf.Deg2Rad)+.01f;
                            W.OBox(nose,c+d*(r0+.021f)+Vector3.up*(top-.01f),new Vector3(noseChord,.04f,.058f),q,false);
                        }
                    }
                    // Seats along the row centre-line, every .68 m of arc, facing the
                    // corner centre (toward the octagon diagonal), never on the joints
                    // with the straight blocks (half a seat clear of each end).
                    float rs=r0+.525f,arc=Mathf.PI*.5f*rs;
                    int seats=Mathf.FloorToInt((arc-.68f)/.68f)+1;
                    if(arc<1.36f)continue;
                    for(int j=0;j<seats;j++)
                    {
                        float t=(.68f*.5f+j*.68f+(arc-.68f*seats)*.5f)/arc;
                        float a=t*Mathf.PI*.5f;
                        var d=new Vector3(sx*Mathf.Cos(a),0f,sz*Mathf.Sin(a));
                        var face=-d;
                        PremiumSeat((j+k)%9==4?seatBlk:seatRed,c+d*rs+Vector3.up*top,
                            Mathf.Atan2(face.x,face.z)*Mathf.Rad2Deg);
                        SeatCount++;cornerSeats++;
                    }
                }
            }
        Debug.Log("A1_STANDS_CONTINUOUS_CORNERS corners=4 rows="+Rows+" cornerSeats="+cornerSeats+
            " sideFront="+SideFront+" endFront="+EndFront+" rowDepth="+RowDepth+" rise="+RowRise+
            " aislesUnchanged=true");
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
        // Owner 10.11 12:38: logic everywhere. FIGHTERS: red carpet from their door (Z0) straight to the gate steps (-z),
        // gold stanchions with red ropes on both sides of the walkout. PUBLIC (Z1): carpet down the 6 m aisle only to the
        // front of the stands, then left and right along the stand front to the stairs at cx +-12 (TierStairs); a rope line
        // keeps the public off the cage floor. The public carpet never reaches the octagon.
        float gateFoot = zs - 1.35f, standFront = cz + EndFront, crossZ0 = standFront - 1.45f, crossZ1 = standFront - .05f;
        // 10.11 13:05: real 12 mm carpets standing on the floor (a slab with edges), never a sheet floating over it
        W.MM(carpet, -87.3f, -84.7f, yF, yF + .012f, Z0 + .05f, gateFoot - .03f);                            // walkout
        W.MM(carpet, -87.3f, -84.7f, yF, yF + .012f, standFront, Z1 - .05f);                                // public aisle
        W.MM(carpet, cx - 12.65f, cx + 12.65f, yF, yF + .012f, crossZ0, crossZ1);                            // to both stand stairs
        void Rope(Vector3 a, Vector3 b)
        {
            // Phone proof 8.11: a real soft red rope with a 13 cm natural sag between GOLD posts, never a rigid bar.
            const int ropeSegments=6;
            for(int segment=0;segment<ropeSegments;segment++)
            {
                float t0=segment/(float)ropeSegments,t1=(segment+1)/(float)ropeSegments;
                W.Rura(rope,Vector3.Lerp(a,b,t0)+Vector3.up*(.86f-.13f*4f*t0*(1f-t0)),
                            Vector3.Lerp(a,b,t1)+Vector3.up*(.86f-.13f*4f*t1*(1f-t1)),.022f,8);
            }
        }
        void Post(Vector3 p)
        {
            Cylinder(vipGold, new Vector3(p.x, yF, p.z), .14f, .04f, 16, true, false);
            Cylinder(vipGold, new Vector3(p.x, yF + .04f, p.z), .03f, .86f, 10, true, false);
            Cylinder(vipGold, new Vector3(p.x, yF + .9f, p.z), .05f, .06f, 10, true, false);
        }
        int walkoutPosts = 0, publicPosts = 0;
        foreach (float x in new[] { -87.55f, -84.45f })                                                       // walkout lane
        {
            Vector3? last = null;
            for (float z = cz - EndFront + .7f; z <= gateFoot - .25f; z += 1.5f)
            { var p = new Vector3(x, 0f, z); Post(p); walkoutPosts++; if (last.HasValue) Rope(last.Value, p); last = p; }
        }
        {
            Vector3? last = null;                                                                              // public rope line
            for (float x = cx - 12.6f; x <= cx + 12.61f; x += 1.575f)
            { var p = new Vector3(x, 0f, crossZ0 - .25f); Post(p); publicPosts++; if (last.HasValue) Rope(last.Value, p); last = p; }
        }
        Debug.Log("A1_WALKOUT_LOGIC gate=-z (fighters' door Z0) walkoutPosts=" + walkoutPosts + " publicCarpet=aisle+cross to stairs cx+-12" +
                  " publicRopePosts=" + publicPosts + " publicCarpetReachesCage=false");
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
        // 2 cm in front of the gold wall lines (they stand 6 cm off the wall): no gold face in the sign plane
        Sign("hala_walk", new Vector3(cx, 4.75f, Z0 + .08f), 9.6f, 1.8f, Vector3.forward, false);
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
        // Owner 9.11: "obicia siedzisk powinny być ze skóry". Every spectator and
        // VIP upholstery uses the Poly Haven grained leather (same source as the
        // VIP chairs and the hotel), tiled at 30 cm so the grain and creases
        // read on a 0.5 m cushion; tint gives red / black leather.
        if(name.Contains("krzesełko")||name.Contains("skóra"))
            m=BraxChampionsFoundation.SourceMaterial(id,"leather_white",.30f,color);
        else if(name.Contains("winyl"))m=BraxChampionsFoundation.LibraryLeather(id,color);
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
    // Warm 3000 K light line (emissive only: no realtime lamp, no shadow pass).
    static Material WarmLine()
    {
        const string id="Arena warm architectural light line";
        if(materials.TryGetValue(id,out var existing))return existing;
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Original CityStaticPBR shader missing");
        var m=new Material(shader){name=id,enableInstancing=true};
        m.SetColor("_Color",new Color(.95f,.80f,.56f));
        m.SetColor("_EmissionColor",new Color(1.10f,.80f,.44f));
        m.SetFloat("_Roughness",.40f);m.SetFloat("_Metallic",0);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Out+"/arena_warm_light_line.mat");
        materials.Add(id,m);
        return m;
    }
    // Designed ceiling under the roof slab: black acoustic field, black satin-steel beams in a 10 x 11.7 m grid with a
    // warm light line and two thin gold beads under every beam, and a lit ring over the octagon (owner 10.11: no brown).
    static void HallCeiling(float X0,float X1,float Z0,float Z1,float cx,float cz,Material acoustic,Material beam,Material line,Material gold)
    {
        float y1=CeilingY,y0=CeilingY-CeilingBeamDrop;
        BoxMinMax(acoustic,X0,X1,y1,y1+.04f,Z0,Z1,false,true);
        int beams=0;
        for(int i=-5;i<=5;i++)
        {
            float x=cx+i*10f;
            BoxMinMax(beam,x-.35f,x+.35f,y0,y1,Z0,Z1,false,true);
            BoxMinMax(line,x-.07f,x+.07f,y0-.02f,y0,Z0+.4f,Z1-.4f,false);
            foreach(float e in new[]{-.33f,.33f})BoxMinMax(gold,x+e-.015f,x+e+.015f,y0-.012f,y0,Z0,Z1,false);
            beams++;
        }
        foreach(float dz in new[]{-23.33f,-11.67f,11.67f,23.33f})
        {
            float z=cz+dz;
            BoxMinMax(beam,X0,X1,y0+.10f,y1,z-.35f,z+.35f,false,true);
            BoxMinMax(line,X0+.4f,X1-.4f,y0+.08f,y0+.10f,z-.07f,z+.07f,false);
            beams++;
        }
        // Light ring over the cage: 32 straight pieces on a 9 m radius, hung under the beams.
        const int n=32;const float r=9f;
        for(int i=0;i<n;i++)
        {
            float a0=2f*Mathf.PI*i/n,a1=2f*Mathf.PI*(i+1)/n,am=(a0+a1)*.5f;
            var mid=new Vector3(cx+r*Mathf.Cos(am),y0-.20f,cz+r*Mathf.Sin(am));
            float len=2f*r*Mathf.Sin(Mathf.PI/n)+.02f;
            var q=Quaternion.Euler(0,-am*Mathf.Rad2Deg,0);
            W.OBox(gold,mid,new Vector3(.34f,.16f,len),q,false);
            W.OBox(line,mid+Vector3.down*.085f,new Vector3(.24f,.02f,len),q,false);
        }
        foreach(float a in new[]{45f,135f,225f,315f})
        {
            var p=new Vector3(cx+r*Mathf.Cos(a*Mathf.Deg2Rad),y0-.12f,cz+r*Mathf.Sin(a*Mathf.Deg2Rad));
            W.Rura(gold,p,new Vector3(p.x,y1,p.z),.03f,8);
        }
        Debug.Log("A1_HALL_CEILING acoustic=black_fabric y="+CeilingY+" blackSteelBeams="+beams+" lightLines="+beams+" ring=r9_32seg roofUnderside=26.885 realtimeLights=0");
    }
    // Lobby face of the hall front wall (was a bare concrete box behind the 80 m print): polished black marble in
    // 3 m slabs, gold skirting and inlay lines, a warm light slot either side of the doorway. The owner's mural
    // hangs on it WHOLE above the door (BraxChampionsArenaMural), the belt beside the door.
    public const float LobbyMarbleDepth=.025f;
    static void LobbyWallMarble(float X0,float X1,float Z1,float yF,float dA,float dB,float dH,Material marble,Material gold,Material line)
    {
        float z0=FrontWallLobbyFaceZ,z1=z0+LobbyMarbleDepth,top=BraxChampionsArenaMural.LobbyRoofUndersideBowlY;
        BoxMinMax(marble,X0,dA,yF,top,z0,z1,false);BoxMinMax(marble,dB,X1,yF,top,z0,z1,false);
        BoxMinMax(marble,dA,dB,dH,top,z0,z1,false);
        foreach(var (a0,a1) in new[]{(X0,dA-.20f),(dB+.20f,X1)})
            BoxMinMax(gold,a0,a1,yF,yF+.14f,z1,z1+.012f,false);                    // gold skirting
        int inlays=0;
        for(float x=X0+6f;x<X1-1f;x+=6f)
        {
            if(x>dA-1.2f&&x<dB+1.2f)continue;                                      // clear of the portal
            if(x>BraxChampionsArenaMural.FrameLeft-.4f&&x<BraxChampionsArenaMural.FrameRight+.4f)
            {
                // under and over the framed mural only
                BoxMinMax(gold,x-.012f,x+.012f,yF+.14f,BraxChampionsArenaMural.FrameBottom-.25f,z1,z1+.008f,false);
                BoxMinMax(gold,x-.012f,x+.012f,BraxChampionsArenaMural.FrameTop+.25f,top,z1,z1+.008f,false);
            }
            else BoxMinMax(gold,x-.012f,x+.012f,yF+.14f,top,z1,z1+.008f,false);
            inlays++;
        }
        foreach(float x in new[]{dA-1.05f,dB+1.05f})
            BoxMinMax(line,x-.035f,x+.035f,yF+.30f,dH+.20f,z1,z1+.014f,false);     // light slots beside the doorway
        Debug.Log("A1_LOBBY_WALL_MARBLE Marble016 slabs=3m depth="+LobbyMarbleDepth+" goldInlays="+inlays+" lightSlots=2 doorClear=4.10x4.20");
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
        var marker=new GameObject("A1 PREMIUM STAIR CARPET | 32 steps | width 1.30m");
        marker.transform.SetParent(colliders.parent,false);
        marker.transform.localPosition=Vector3.zero;
        var stone=Plain("schody trybun",new Color(.28f,.27f,.25f));
        const float stairHalf=.85f;       // 1.70m structural aisle
        const float carpetHalf=.65f;      // 1.30m red velour
        const float goldWidth=.020f;
        const float overlay=.009f;
        // Owner 10.11 13:05 (zero flicker): the riser carpet hangs 9 mm IN FRONT of the stone riser (never inside the tread
        // plane) from the lower tread carpet up to the underside of its own tread carpet; the tread carpet starts on the
        // front edge of the riser carpet and stops 8 mm short of the next riser. No carpet or gold face lies in a stone face.
        for(int i=0;i<32;i++)
        {
            float raw0=front-facing*i*.525f,raw1=front-facing*(i+1)*.525f;
            float top=.055f+(i+1)*.2f,previous=.055f+i*.2f;
            // the run climbs toward -facing: the riser front (where you come from) is on the +facing side
            float outer=raw0+facing*.009f,back=raw1+facing*.008f;
            float t0=Mathf.Min(outer,back),t1=Mathf.Max(outer,back),r0=Mathf.Min(outer,raw0),r1=Mathf.Max(outer,raw0);
            float riserBottom=i==0?previous:previous+.003f+overlay,riserTop=top+.003f;
            // u = across the stair (centre +- half), v = along the run
            void Run(Material m,float u0,float u1,float y0,float y1,float v0,float v1)
            {
                if(alongX)W.MM(m,u0,u1,y0,y1,v0,v1,false);else W.MM(m,v0,v1,y0,y1,u0,u1,false);
            }
            if(alongX)W.MM(stone,centre-stairHalf,centre+stairHalf,0,top,Mathf.Min(raw0,raw1),Mathf.Max(raw0,raw1),true);
            else W.MM(stone,Mathf.Min(raw0,raw1),Mathf.Max(raw0,raw1),0,top,centre-stairHalf,centre+stairHalf,true);
            Run(carpet,centre-carpetHalf,centre+carpetHalf,top+.003f,top+.003f+overlay,t0,t1);          // tread
            Run(carpet,centre-carpetHalf,centre+carpetHalf,riserBottom,riserTop,r0,r1);                // riser
            foreach(float side in new[]{-1f,1f})
            {
                float u=centre+side*(carpetHalf+goldWidth*.5f);
                Run(gold,u-goldWidth*.5f,u+goldWidth*.5f,top+.003f,top+.003f+overlay,t0,t1);
                // continuous 20 mm gold edge down the riser too
                Run(gold,u-goldWidth*.5f,u+goldWidth*.5f,riserBottom,riserTop,r0,r1);
            }
        }
        Debug.Log("A1_PREMIUM_TIER_STAIRS width=1.70 carpet=1.30 steps=32 tread=.525 riser=.20"+
            " red_velour=true gold_edges_20mm_continuous_tread_and_riser=true carpet_surface_per_run=30.16m2");
    }
}
