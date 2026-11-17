using System;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// Owner 10.11 00:30: "Wejście do kasyna ma być otwarte, ma być kasyno, a nie, że tam jakaś ściana stoi. Drzwi mają
// być. To, że ona nie jest wyposażona w środku, to nie znaczy, że tam nie można wejść." and, on the render of the
// windows, "jedno poprawiłeś, a drugie zepsułeś": a glowing board 1 m behind the glass read as painted plywood.
// The podium was an EMPTY box: a lobby whose back wall carried a painted "portal", and boards behind every window.
// Now the lobby opens through a real 6 x 4.5 m portal into the casino hall: one 75 x 52 m room, 12.8 m high: black marble
// floor with a field of red marble tiles, a cream coffered ceiling with light lines around the roof lantern, twelve polished
// gold columns, six ring chandeliers, a lit rear wall; since 16.11 128 working machines of five games in banks and on four
// carousels, four roulettes.
// Every window now looks into this real room. Built in nominal metres with the interior kit, scaled by K once.
public static partial class BraxChampionsCasinoT11
{
    const float HallFloor=.215f, HallCeiling=13.0f;
    const float HallX=37.80f, HallZ0=-27.80f, HallZ1=27.62f, LobbyBackZ=-24.0f;
    public const float PortalHalf=3.0f, PortalTop=4.5f;

    static void RingChandelier(Material metal,Material crystal,Vector3 top,float scale)
    {
        W.Rura(metal,top,top+Vector3.down*(3.2f*scale),.045f,8);
        float[] radius={2.6f,1.9f,1.2f},drop={2.0f,2.7f,3.4f};
        for(int r=0;r<3;r++)
        {
            float rr=radius[r]*scale,y=top.y-drop[r]*scale;
            const int n=28;
            for(int i=0;i<n;i++)
            {
                float a0=2*Mathf.PI*i/n,a1=2*Mathf.PI*(i+1)/n;
                var p0=new Vector3(top.x+rr*Mathf.Cos(a0),y,top.z+rr*Mathf.Sin(a0));
                var p1=new Vector3(top.x+rr*Mathf.Cos(a1),y,top.z+rr*Mathf.Sin(a1));
                W.Rura(metal,p0,p1,.045f*scale,6,false);
                if(i%2==0)W.Box(crystal,p0+Vector3.down*(.30f*scale),new Vector3(.06f,.52f*scale,.06f));
            }
            for(int k=0;k<4;k++)
            {
                float a=Mathf.PI*.5f*k+.4f;
                W.Rura(metal,top,new Vector3(top.x+rr*Mathf.Cos(a),y,top.z+rr*Mathf.Sin(a)),.012f,4,false);
            }
        }
        W.Kula(crystal,top+Vector3.down*(3.55f*scale),.30f*scale,12,8);
    }

    // The owner's hidden office (16.11 23:41 "W tym kasynie Wawer na mieście ... prywatne pomieszczenie, do którego ... wejście
    // będzie nikomu nieznane ... przez szafę", 23:44 "dyskretne pomieszczenie, biuro chcę mieć dla siebie"): a walnut block in
    // the hall's rear east corner - inside of the side windows (x 26..33) and against the windowless rear wall -, 4.6 m high,
    // black marble floor, cream ceiling at 4.2 m; its door's opening hides behind the middle of three wardrobes on its hall
    // wall. The wardrobes, the furniture, the open safe with the gold, the monitoring of six hall cameras:
    // BraxChampionsKasynoSprzet.BiuroWlasciciela.
    const float OfX0=26.0f,OfX1=33.0f,OfZ0=19.0f,OfZ1=27.35f,OfT=.2f,OfH=4.6f,OfDrzwi0=28.9f,OfDrzwi1=30.3f,OfDrzwiH=2.35f;
    static readonly BraxChampionsKasynoSprzet.KameraHali[] KameryHali =
    {
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="WEJŚCIE",poz=new Vector3(-10.0f,5.0f,-23.6f),cel=new Vector3(4.0f,1.0f,-19.0f)},
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="RULETKI",poz=new Vector3(13.45f,6.0f,0f),cel=new Vector3(0f,.9f,0f)},
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="KASA",poz=new Vector3(14.1f,6.0f,-14.62f),cel=new Vector3(13.8f,1.2f,-21.0f)},
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="AUTOMATY ZACHÓD",poz=new Vector3(-27.75f,6.0f,-13.55f),cel=new Vector3(-22f,1f,-2f)},
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="AUTOMATY WSCHÓD",poz=new Vector3(27.75f,6.0f,-13.55f),cel=new Vector3(22f,1f,-2f)},
        new BraxChampionsKasynoSprzet.KameraHali{nazwa="KARUZELE",poz=new Vector3(-14.62f,6.0f,14.0f),cel=new Vector3(-21.15f,1.5f,14f)},
    };
    static void BiuroWlasciciela(Transform g,Material marble,Material wall,Material ceiling,Material gold)
    {
        float y=HallFloor+.03f;                                                          // over the red tiles and their gold joints
        W.MM(marble,OfX0,OfX1,HallFloor+.02f,y,OfZ0,OfZ1);
        W.KolizjaMM(OfX0,OfX1,HallFloor,y,OfZ0,OfZ1);
        W.MM(wall,OfX0,OfX0+OfT,y,OfH,OfZ0,OfZ1,true);                                   // west
        W.MM(wall,OfX1-OfT,OfX1,y,OfH,OfZ0,OfZ1,true);                                   // east
        W.MM(wall,OfX0+OfT,OfX1-OfT,y,OfH,OfZ1-OfT,OfZ1,true);                           // south
        W.MM(wall,OfX0+OfT,OfDrzwi0,y,OfH,OfZ0,OfZ0+OfT,true);                           // north, west of the door
        W.MM(wall,OfDrzwi1,OfX1-OfT,y,OfH,OfZ0,OfZ0+OfT,true);                           // north, east of it
        W.MM(wall,OfDrzwi0,OfDrzwi1,OfDrzwiH,OfH,OfZ0,OfZ0+OfT,true);                    // over it
        W.MM(wall,OfX0,OfX1,OfH,OfH+.12f,OfZ0,OfZ1);                                     // the roof
        W.MM(ceiling,OfX0+OfT,OfX1-OfT,4.20f,4.22f,OfZ0+OfT,OfZ1-OfT);                   // the ceiling inside
        W.MM(gold,OfX0-.02f,OfX1,4.46f,4.50f,OfZ0-.02f,OfZ0);                            // a gold line round its top (hall sides)
        W.MM(gold,OfX0-.02f,OfX0,4.46f,4.50f,OfZ0,OfZ1);
        W.MM(gold,OfX1,OfX1+.02f,4.46f,4.50f,OfZ0-.02f,OfZ1);
        BraxChampionsKasynoSprzet.BiuroWlasciciela(g,OfX0+OfT,OfX1-OfT,OfZ0+OfT,OfZ1-OfT,y,OfT,OfDrzwi0,OfDrzwi1,KameryHali);
    }

    static void CasinoHall()
    {
        var g=new GameObject("T11 casino hall | open, finished, unfurnished | marble, red tiles, gold columns, chandeliers").transform;
        g.position=Vector3.zero;g.rotation=Quaternion.identity;g.localScale=Vector3.one;
        W.Start(g);
        try
        {
            // Hall-only materials from the region's prepared sources, lit as an interior (no sun through the roof).
            var marble=BraxChampionsFoundation.LibraryMarble("t11_hall_black_marble_floor",new Color(.96f,.94f,.92f),3.0f);
            var white=BraxChampionsFoundation.LibraryMarbleWhite("t11_hall_white_marble_inlay",new Color(.90f,.89f,.87f),2.4f);
            // Owner 10.11 00:36 on the render: "na podłodze nie wiem co to jest, może być czerwona, ale płytka".
            // No carpet: polished RED marble tiles (the white-marble maps, tinted), 3 m slabs set in gold joints.
            var carpet=BraxChampionsFoundation.LibraryMarbleWhite("t11_hall_red_marble_tiles",new Color(.74f,.10f,.09f),2.4f);
            carpet.SetFloat("_Roughness",.22f);
            var ceiling=BraxChampionsFoundation.SourceMaterial("t11_hall_cream_coffered_ceiling",BraxChampionsFoundation.InteriorPaintSlug,2.0f,BraxChampionsFoundation.PaintTint(new Color(1.0f,.96f,.88f)));
            var wall=BraxChampionsFoundation.SourceMaterial("t11_hall_walnut_wall_panelling","smoked_walnut_veneer",1.6f,new Color(.86f,.74f,.62f));
            foreach(var m in new[]{marble,white,carpet,wall})BraxChampionsInteriorLight.Apply(m,BraxChampionsInteriorLight.Walls);
            BraxChampionsInteriorLight.Apply(ceiling,BraxChampionsInteriorLight.Ceilings);

            // ---- floor: black marble, white marble band, red marble tile field with a gold edge ----
            // the rotunda bay floor (granite, x +-16.75, z -28..-24.2) is not covered again by the hall marble (owner 13:05
            // "zakaz nakładania"): the hall floor starts behind it and only the two wings run forward to the front wall
            W.MM(marble,-HallX,HallX,.17f,HallFloor,-24.2f,HallZ1);
            foreach(float s in new[]{-1f,1f})W.MM(marble,Mathf.Min(s*16.75f,s*HallX),Mathf.Max(s*16.75f,s*HallX),.17f,HallFloor,HallZ0,-24.2f);
            W.KolizjaMM(-HallX,HallX,.05f,HallFloor,HallZ0,HallZ1);
            W.MM(white,-33.6f,33.6f,HallFloor,HallFloor+.004f,-21.6f,24.6f);
            W.MM(carpet,-32.4f,32.4f,HallFloor+.004f,HallFloor+.016f,-20.4f,23.4f);
            foreach(float s in new[]{-1f,1f})
            {
                W.MM(gold,s*32.4f-.04f,s*32.4f+.04f,HallFloor+.004f,HallFloor+.02f,-20.4f,23.4f);
                W.MM(gold,-32.44f,32.44f,HallFloor+.004f,HallFloor+.02f,(s<0?-20.4f:23.4f)-.04f,(s<0?-20.4f:23.4f)+.04f);
            }
            // red tiles run from the portal into the field
            W.MM(carpet,-PortalHalf,PortalHalf,HallFloor+.004f,HallFloor+.016f,LobbyBackZ,-20.4f);
            // gold joints: 3 m tiles
            int joints=0;
            for(float x=-30f;x<=30.1f;x+=3f){W.MM(gold,x-.012f,x+.012f,HallFloor+.016f,HallFloor+.019f,-20.36f,23.36f);joints++;}
            for(float z=-18f;z<=21.1f;z+=3f){W.MM(gold,-32.36f,32.36f,HallFloor+.016f,HallFloor+.019f,z-.012f,z+.012f);joints++;}

            // ---- ceiling with the roof lantern left open (skylight glass x +-12, z +-10) ----
            W.MM(ceiling,-HallX,-12f,HallCeiling,HallCeiling+.08f,HallZ0,HallZ1);
            W.MM(ceiling,12f,HallX,HallCeiling,HallCeiling+.08f,HallZ0,HallZ1);
            W.MM(ceiling,-12f,12f,HallCeiling,HallCeiling+.08f,LobbyBackZ,-10f);
            W.MM(ceiling,-12f,12f,HallCeiling,HallCeiling+.08f,10f,HallZ1);
            foreach(float s in new[]{-1f,1f})
            {
                W.MM(ceiling,s*12f-.06f,s*12f+.06f,HallCeiling-.02f,13.94f,-10f,10f);       // lantern shaft lining
                W.MM(ceiling,-12f,12f,HallCeiling-.02f,13.94f,s*10f-.06f,s*10f+.06f);
                W.MM(led,s*12.25f-.06f,s*12.25f+.06f,HallCeiling-.03f,HallCeiling,-10.3f,10.3f);   // light line around it
                W.MM(led,-12.3f,12.3f,HallCeiling-.03f,HallCeiling,s*10.25f-.06f,s*10.25f+.06f);
            }
            // walnut coffer beams on the column lines, a light line under each (10.11 00:45: not everything gold)
            foreach(float x in new[]{-28.2f,-14.1f,14.1f,28.2f})
            {
                W.MM(wall,x-.30f,x+.30f,HallCeiling-.36f,HallCeiling,HallZ0,HallZ1);
                W.MM(led,x-.05f,x+.05f,HallCeiling-.38f,HallCeiling-.36f,HallZ0+.6f,HallZ1-.6f);
            }
            foreach(float z in new[]{-14f,14f})
            {
                W.MM(wall,-HallX,HallX,HallCeiling-.30f,HallCeiling,z-.30f,z+.30f);
                W.MM(led,-HallX+.6f,HallX-.6f,HallCeiling-.32f,HallCeiling-.30f,z-.05f,z+.05f);
            }

            // ---- twelve polished gold columns on black marble bases ----
            int columns=0;
            foreach(float x in new[]{-28.2f,-14.1f,14.1f,28.2f})
            foreach(float z in new[]{-14f,0f,14f})
            {
                W.MM(marble,x-.80f,x+.80f,HallFloor,HallFloor+.55f,z-.80f,z+.80f,true);
                W.Walec(pillarGold,new Vector3(x,HallFloor+.55f,z),.52f,HallCeiling-.36f-HallFloor-.55f-.40f,24,true);
                W.Walec(gold,new Vector3(x,HallCeiling-.76f,z),.70f,.40f,24);
                W.MM(led,x-.82f,x+.82f,HallFloor+.55f,HallFloor+.58f,z-.82f,z+.82f);          // glow line on the base
                columns++;
            }

            // ---- walls: the facades are windows; the rear wall and the front returns get walnut + gold + light ----
            float rear=HallZ1+.06f;
            W.MM(wall,-HallX,HallX,HallFloor,HallCeiling,rear-.05f,rear,true);
            W.MM(marble,-HallX,HallX,HallFloor,HallFloor+1.0f,rear-.07f,rear-.05f);
            W.MM(gold,-HallX,HallX,HallFloor+1.0f,HallFloor+1.05f,rear-.08f,rear-.05f);
            for(float x=-33f;x<=33.1f;x+=6f)
            {
                W.MM(marble,x-.10f,x+.10f,HallFloor+1.05f,HallCeiling,rear-.09f,rear-.05f);
                W.MM(violet,x+2.94f,x+3.06f,2.0f,11.4f,rear-.075f,rear-.05f);
            }
            // the 6 m delivery opening in the rear wall is closed by a graphite roller gate inside the wall thickness
            W.MM(graphite,-23f,-17f,.20f,5.55f,27.72f,27.80f);
            // lobby side: walnut on the hall face of the lobby wall, around the open portal
            float front=LobbyBackZ;
            W.MM(wall,-16.975f,-PortalHalf-.16f,HallFloor,HallCeiling,front,front+.05f);
            W.MM(wall,PortalHalf+.16f,16.975f,HallFloor,HallCeiling,front,front+.05f);
            W.MM(wall,-PortalHalf-.16f,PortalHalf+.16f,PortalTop+.16f,HallCeiling,front,front+.05f);
            foreach(float s in new[]{-1f,1f})
                W.MM(gold,s*(PortalHalf+.08f)-.08f,s*(PortalHalf+.08f)+.08f,HallFloor,PortalTop+.16f,front,front+.10f);
            W.MM(gold,-PortalHalf-.16f,PortalHalf+.16f,PortalTop,PortalTop+.16f,front,front+.10f);
            W.MM(led,-PortalHalf,PortalHalf,PortalTop-.03f,PortalTop,front+.02f,front+.08f);

            // ---- six ring chandeliers ----
            foreach(float x in new[]{-21.15f,21.15f})
            foreach(float z in new[]{-14f,0f,14f})
                RingChandelier(gold,led,new Vector3(x,HallCeiling,z),z==0f?1.25f:1.0f);

            // ---- the slot machines, working (owner 16.11 18:22: "tak samo tam to wyposażymy ... kasynowawer - dwie roboty w
            // jednym"): the SKY KASYNO's machines (BraxChampionsKasynoSprzet / BraxChampionsAutomaty). Two double banks in each
            // wing, between the column lines (x 14.1 / 28.2) under the chandeliers, eight a row back to back with their stools,
            // mechanical and screen machines in turn in the six themes of the owner's strips (19:13 "co druga różnego typu"), on
            // the red marble; the linked jackpot board hangs over each bank. The middle under the lantern stays for the tables.
            // 16.11 20:28 ("możesz zrobić więcej ... albo lepiej rozstawić ... różne gry w automatach ... pokaż, jak się robi
            // luksus"): five games (BraxChampionsAutomatyGry) - the classic, the video, VIDEO POKER, KENO, KOŁO MISTRZÓW - every
            // neighbour another game; in each wing the two long banks, a short bank before and behind them, and under the front
            // and the back chandelier a CAROUSEL of eight round a lacquered drum with the owner's toppers on its crown (the KOŁO
            // carousels to the entrance, the poker / keno ones at the back): 128 machines
            int slots=0,roulettes=0,carousels=0,obsluga=0,blackjacks=0,bakarats=0,holdems=0;
            BraxChampionsFoundation.Optional("T11 slot machines",()=>
            {
                var body=new BraxChampionsMeshBatch();
                var kas=BraxChampionsKasynoSprzet.Zacznij(g,body,"KASYNO WAWER");
                int start=0;const int Mech=BraxChampionsAutomatyGry.Mech,Video=BraxChampionsAutomatyGry.Video,Poker=BraxChampionsAutomatyGry.Poker,
                    Keno=BraxChampionsAutomatyGry.Keno,Kolo=BraxChampionsAutomatyGry.Kolo;
                var dlugie=new[]{Mech,Video,Kolo,Poker,Mech,Video,Kolo,Keno};var krotkie=new[]{Video,Keno,Kolo,Poker};
                float fl=HallFloor+.016f;
                foreach(float x in new[]{-24.5f,-19.5f,19.5f,24.5f})BraxChampionsKasynoSprzet.Bank(kas,new Vector3(x,fl,0f),90f,8,start++,HallCeiling,dlugie);
                foreach(float x in new[]{-22.0f,22.0f})foreach(float z in new[]{-8.0f,8.0f})
                    BraxChampionsKasynoSprzet.Bank(kas,new Vector3(x,fl,z),0f,4,start++,HallCeiling,krotkie);
                foreach(float x in new[]{-21.15f,21.15f})
                {
                    BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(x,fl,-14f),8,new[]{Kolo,Video},x<0f?0:1,2.95f);
                    BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(x,fl,14f),8,new[]{Poker,Keno},x<0f?0:1,2.95f);
                }
                carousels=kas.karuzele;
                // the entrance (owner 20:29 "porządna recepcja, konkretna, na maksa ... szatnia"), as the real luxury casinos: the
                // security arches between brass posts with red velvet ropes in the portal, the RECEPCJA (the players club desk,
                // the back wall with the owner's logo) left, the SZATNIA right, the KASA beyond it; his RULETKA sign over the pit
                BraxChampionsKasynoSprzet.Bramka(kas,new Vector3(0f,fl,-22.4f),0f);
                foreach(float s in new[]{-1f,1f})
                    BraxChampionsKasynoSprzet.Liny(kas,new[]{new Vector3(s*2.9f,fl,-23.8f),new Vector3(s*2.35f,fl,-23.1f),new Vector3(s*1.85f,fl,-22.4f)});
                BraxChampionsKasynoSprzet.Liny(kas,new[]{new Vector3(-.45f,fl,-22.4f),new Vector3(.45f,fl,-22.4f)});
                BraxChampionsKasynoSprzet.Recepcja(kas,new Vector3(-8.0f,fl,-20.5f),90f,4.0f,true);
                BraxChampionsKasynoSprzet.Szatnia(kas,new Vector3(8.0f,fl,-20.5f),-90f,3.6f,2,true);
                BraxChampionsKasynoSprzet.Kasa(kas,new Vector3(13.8f,fl,-21.4f),0f,3);
                BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(0f,6.0f,0f),BraxChampionsKasynoSprzet.ArkRuletka,2.6f,HallCeiling);
                obsluga=kas.punkty.Count;
                // the roulette pit under the lantern (owner 19:13 "ruletki prawdziwe"): four European tables, the croupiers in the
                // middle, the players facing out
                foreach(float rx in new[]{-4.0f,4.0f})foreach(float rz in new[]{-4.5f,4.5f})
                    BraxChampionsKasynoSprzet.Ruletka(kas,new Vector3(rx,HallFloor+.016f,rz),rz>0f?0f:180f);
                roulettes=kas.ruletki.Count;
                // owner 16.11 ("blackjack grywalny w obu kasynach"): two playable tables between the roulette pit and the slot
                // wings, their players to the entrance, his BLACKJACK sign over each (BraxChampionsKasynoBlackjack)
                foreach(float bx in new[]{-11.5f,11.5f})
                {
                    BraxChampionsKasynoSprzet.Blackjack(kas,new Vector3(bx,HallFloor+.016f,-8.5f),180f);
                    BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(bx,4.6f,-8.9f),BraxChampionsKasynoSprzet.ArkBlackjack,1.5f,HallCeiling);
                }
                blackjacks=kas.blackjack.Count;
                // and two baccarat tables north of them (punto banco, BraxChampionsKasynoBakarat), his BAKARAT sign over each
                foreach(float bx in new[]{-11.5f,11.5f})
                {
                    BraxChampionsKasynoSprzet.Bakarat(kas,new Vector3(bx,HallFloor+.016f,6.0f),180f);
                    BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(bx,4.6f,5.6f),BraxChampionsKasynoSprzet.ArkBakarat,1.5f,HallCeiling);
                }
                bakarats=kas.bakarat.Count;
                // the BIG SIX "KOŁO FORTUNY" at the back of the hall, its betting table before it (BraxChampionsKasynoKolo)
                BraxChampionsKasynoSprzet.KoloFortuny(kas,new Vector3(0f,HallFloor+.016f,23.5f),180f);
                // and the SIC BO table between the roulette pit and the wheel (BraxChampionsKasynoSicBo), its players to the pit
                BraxChampionsKasynoSprzet.SicBo(kas,new Vector3(0f,HallFloor+.016f,13.5f),180f);
                // two CASINO HOLD'EM tables north of the baccarat ones, clear of the gold columns at x 14.1, his POKER sign over each
                foreach(float px in new[]{-11.0f,11.0f})
                {
                    BraxChampionsKasynoSprzet.Holdem(kas,new Vector3(px,HallFloor+.016f,15.5f),180f);
                    BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(px,4.6f,15.1f),BraxChampionsKasynoSprzet.ArkPoker,1.5f,HallCeiling);
                }
                holdems=kas.holdem.Count;
                body.Build(g,"T11 casino hall | slot machines",BraxChampionsKasynoSprzet.Out+"/Meshes",false,1f);
                BraxChampionsKasynoSprzet.Zakoncz(kas);slots=kas.maszyny.Count;
            });
            W.Grupa=g;W.Wnetrze=true;
            // owner 16.11 23:41 / 23:44, 17.11: his hidden office, its door a wardrobe (BiuroWlasciciela below)
            BraxChampionsFoundation.Optional("T11 ukryte biuro właściciela",()=>BiuroWlasciciela(g,marble,wall,ceiling,gold));
            W.Grupa=g;W.Wnetrze=true;
            if(obsluga!=5)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL service points "+obsluga+"/5 (reception, cloakroom, three cashier windows)");
            if(slots!=128||carousels!=4)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL slot machines "+slots+"/128 carousels "+carousels+"/4");
            if(roulettes!=4)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL roulettes "+roulettes+"/4");
            if(blackjacks!=2)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL blackjack tables "+blackjacks+"/2");
            if(bakarats!=2)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL baccarat tables "+bakarats+"/2");
            if(holdems!=2)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL Casino Hold'em tables "+holdems+"/2");

            // ---- light: six warm vertex lamps (no shadows) ----
            foreach(float x in new[]{-21.15f,21.15f})
            foreach(float z in new[]{-14f,0f,14f})
                W.Lampa("T11 hall chandelier warm light",new Vector3(x,9.2f,z),1.5f,24f*K,new Color(1f,.88f,.66f));
            Debug.Log("T11_CASINO_HALL open=true portal=6x4.5 floor=black_marble+red_marble_tiles_3m_gold_joints ceiling=cream_coffered_"+HallCeiling+
                " columns="+columns+"_polished_gold chandeliers=6 lights=6_vertex slots="+slots+"_working carousels="+carousels+" roulettes="+roulettes+"_working scale=K_once");
        }
        catch { W.Wyczysc(); UnityEngine.Object.DestroyImmediate(g.gameObject); throw; }
        W.Zapisz("mistrzowie_t11_casino_hall");
        g.SetParent(shell,false);
        g.localPosition=Vector3.zero;g.localRotation=Quaternion.identity;g.localScale=Vector3.one*K;
        var seen=g.gameObject.AddComponent<BraxWidocznoscWnetrza>();seen.nazwa="Kasyno Wawer - sala";
        // drawn from the whole plot (the hall is what every window shows)
        seen.strefy=new[]{new Bounds(new Vector3(LotX,8f,LotZ)*K,new Vector3(260f,60f,240f)*K)};
    }
}
