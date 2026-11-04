using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Camp-only construction in the original metre coordinates, scaled once by K.
public static partial class BraxObozEnvironment
{
    static Material campusAsphalt, campusPaving, campusSteel, campusRubber, campusMark;
    static void CampusMaterials()
    {
        campusAsphalt = BraxObozMaterials.PublicPbr("clean_asphalt", 2.1f);
        campusPaving = BraxObozMaterials.PublicPbr("concrete_pavers", 1.9f);
        campusSteel = BraxObozMaterials.Plain("campus-antracyt", new Color(.045f,.055f,.06f), .38f, .7f);
        campusRubber = BraxObozMaterials.Plain("campus-guma", new Color(.045f,.055f,.05f), .12f);
        campusMark = BraxObozMaterials.Plain("campus-oznaczenia", new Color(.88f,.86f,.72f), .15f);
    }
    static void TrainingCampus()
    {
        var pavilion=Group("Pawilon treningowy — otwarty, bez starej siłowni",root,P(0,0,51));
        Box(pavilion,"Podest z kostki",new Vector3(0,.055f,0),new Vector3(42,.11f,16),campusPaving);
        foreach(float x in new[]{-20f,-10f,0f,10f,20f})
        foreach(float z in new[]{-7.5f,7.5f})
        {
            Box(pavilion,"Stopa słupa",new Vector3(x,.10f,z),new Vector3(.65f,.2f,.65f),campusSteel);
            Box(pavilion,"Słup stalowy",new Vector3(x,2.8f,z),new Vector3(.22f,5.6f,.22f),campusSteel);
        }
        foreach(float z in new[]{-7.5f,7.5f})
            Box(pavilion,"Belka oczepowa",new Vector3(0,5.45f,z),new Vector3(42,.3f,.22f),wood);
        var roofing=Group("Dach dwuspadowy i wiązary",pavilion,Vector3.zero);
        roofing.localRotation=Quaternion.Euler(0,90,0);
        MetalRoof(roofing,17,43,5.6f,2.2f);
        Ring("Ring 2",-12,51);
        int first=bags.Count; Bags(12,51,false,2);
        foreach(var bag in bags.Skip(first)) covered.Add(bag.transform);
        Sign(pavilion,"BRAXHORN · OBÓZ TRENINGOWY",new Vector3(0,4.85f,-7.7f),12);
        Station("Rozgrzewka i mobilność",-12,30,14,8,t=>{
            for(int i=0;i<6;i++)Box(t,"Mata treningowa",new Vector3(-5+i*2,.10f,0),new Vector3(1.4f,.04f,2.2f),burgundy);
        });
        Station("Drążki",35,34,12,10,t=>{
            for(int i=0;i<3;i++){
                float x=-3.8f+i*3.8f,h=2.25f+i*.15f;
                foreach(float side in new[]{-1f,1f})Cylinder(t,"Słupek drążka",new Vector3(x+side*1.35f,.08f,0),new Vector3(x+side*1.35f,h,0),.065f,campusSteel,true);
                Cylinder(t,"Drążek chwytowy",new Vector3(x-1.35f,h,0),new Vector3(x+1.35f,h,0),.018f,metal,true);
            }
        });
        Station("Poręcze",35,19,12,10,t=>{
            foreach(float lane in new[]{-3f,3f})foreach(float side in new[]{-.32f,.32f}){
                Cylinder(t,"Poręcz chwytowa",new Vector3(lane+side,1.25f,-1.5f),new Vector3(lane+side,1.25f,1.5f),.026f,metal,true);
                foreach(float z in new[]{-1.3f,1.3f})Cylinder(t,"Podpora poręczy",new Vector3(lane+side,.05f,z),new Vector3(lane+side,1.25f,z),.05f,campusSteel,true);
            }
        });
        Station("Wskoki — trzy wysokości",35,4,12,10,t=>{
            for(int i=0;i<3;i++){
                float h=.3f+i*.15f,x=-3.2f+i*3.2f;
                Box(t,"Skrzynia plyometryczna",new Vector3(x,.08f+h*.5f,0),new Vector3(.9f,h,.8f),wood);
                Box(t,"Powierzchnia antypoślizgowa",new Vector3(x,.09f+h,0),new Vector3(.9f,.025f,.8f),campusRubber);
            }
        });
        Station("Pchanie sań — dwa tory",35,-18,12,26,t=>{
            for(int i=0;i<3;i++)Box(t,"Linia toru",new Vector3(-5+i*5,.086f,0),new Vector3(.06f,.004f,23),campusMark,false);
            for(int i=0;i<5;i++)Box(t,"Oznaczenie odległości",new Vector3(0,.088f,-10+i*5),new Vector3(10,.004f,.05f),campusMark,false);
            foreach(float x in new[]{-2.5f,2.5f}){
                Box(t,"Sanie — płozy",new Vector3(x,.16f,-9),new Vector3(1.2f,.14f,1.7f),campusSteel);
                foreach(float side in new[]{-.45f,.45f})Cylinder(t,"Uchwyt sań",new Vector3(x+side,.2f,-9.5f),new Vector3(x+side,1.2f,-9.5f),.03f,metal,true);
                for(int j=0;j<3;j++)Cylinder(t,"Obciążenie sań",new Vector3(x,.3f+j*.09f,-8.8f),new Vector3(x,.37f+j*.09f,-8.8f),.28f,campusRubber);
            }
        });
        Station("Liny treningowe",12,28,12,10,t=>{
            for(int lane=0;lane<2;lane++)foreach(float side in new[]{-.2f,.2f}){
                float x=-3+lane*6+side;
                for(int j=0;j<16;j++){
                    float z=-3+j*.4f,next=z+.4f;
                    Cylinder(t,"Lina",new Vector3(x+Mathf.Sin(j*.7f)*.12f,.12f,z),new Vector3(x+Mathf.Sin((j+1)*.7f)*.12f,.12f,next),.025f,rope);
                }
                Box(t,"Kotwa lin",new Vector3(x,.25f,3.5f),new Vector3(.18f,.5f,.18f),campusSteel);
            }
        });
    }
    static void Station(string label,float x,float z,float width,float depth,Action<Transform> build)
    {
        var t=Group("Stanowisko · "+label,root,P(x,0,z));
        Box(t,"Podkład",new Vector3(0,.035f,0),new Vector3(width,.07f,depth),campusPaving);
        Box(t,"Nawierzchnia gumowa",new Vector3(0,.075f,0),new Vector3(width-.3f,.01f,depth-.3f),campusRubber,false);
        build(t);
        float signWidth=Mathf.Min(width-1,8);
        foreach(float side in new[]{-1f,1f})
            Box(t,"Podpora tablicy",new Vector3(side*signWidth*.42f,.7f,depth*.5f),new Vector3(.065f,1.4f,.065f),campusSteel);
        Sign(t,label,new Vector3(0,1.6f,depth*.5f),signWidth);
        // This marker records geometry only. Exercise animations are not implemented.
        var station=t.gameObject.AddComponent<BraxObozTrainingStation>();station.label=label;
        station.footprint=new Vector2(width,depth)*K;
    }
    static void Sign(Transform parent,string label,Vector3 p,float width)
    {
        Box(parent,"Tablica · "+label,p,new Vector3(width,.65f,.08f),campusSteel,false);
        var text=Group("Napis · "+label,parent,(p+new Vector3(0,0,-.046f))*K).gameObject.AddComponent<TextMesh>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text=label;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=72;
        text.characterSize=.38f*K*10/72;text.color=new Color(.9f,.87f,.73f);
        text.font.RequestCharactersInTexture(label,text.fontSize,text.fontStyle);
        float advance=0;
        foreach(char c in label)if(text.font.GetCharacterInfo(c,out var glyph,text.fontSize,text.fontStyle))advance+=glyph.advance;
        float measured=advance*text.characterSize/10;
        if(measured>(width-.3f)*K)text.characterSize*=((width-.3f)*K)/measured;
        text.GetComponent<MeshRenderer>().sharedMaterial=BraxObozMaterials.SignText(text.font);
        text.gameObject.AddComponent<BraxCitySignText>();
        // Lettering sits on the south face; local +X stays left-to-right there.
        text.transform.localRotation=Quaternion.identity;
    }
    static void CampusArrival()
    {
        var t=Group("Dojazd i plac autobusowy",root,Vector3.zero);
        Box(t,"Droga dojazdowa",new Vector3(0,.045f,-167),new Vector3(7,.09f,76),campusAsphalt);
        // Turning forecourt: outer radius 18 m, island radius 6 m; the design
        // envelope is a 12 x 2.6 m coach, not a claim of a tested driving model.
        Disc(t,"Plac zawracania",new Vector3(0,.095f,-112),18,campusAsphalt,true);
        Disc(t,"Wyspa centralna",new Vector3(0,.16f,-112),6,campusPaving,true);
        Box(t,"Zatoka autobusu",new Vector3(21,.055f,-113),new Vector3(4,.11f,20),campusAsphalt);
        Box(t,"Połączenie zatoki",new Vector3(14,.05f,-113),new Vector3(12,.10f,24),campusAsphalt);
        Box(t,"Dojście pasażerów",new Vector3(26,.08f,-124),new Vector3(2.8f,.16f,52),campusPaving);
        Box(t,"Dojście od furtki",new Vector3(16,.08f,-147),new Vector3(22,.16f,2.8f),campusPaving);
        Box(t,"Dojście piesze przed bramą",new Vector3(6.8f,.08f,-162),new Vector3(1.8f,.16f,24),campusPaving);
        Box(t,"Dojście od przystanku",new Vector3(15,.06f,-98),new Vector3(25,.12f,2.8f),campusPaving);
        foreach(float x in new[]{-3.2f,3.2f})Box(t,"Linia krawędzi drogi",new Vector3(x,.092f,-169),new Vector3(.1f,.004f,70),campusMark,false);
        for(int i=0;i<8;i++)Box(t,"Pas przejścia",new Vector3(-2.8f+i*.8f,.098f,-139),new Vector3(.4f,.004f,2.8f),campusMark,false);
        Sign(t,"BRAXHORN · PRZYJAZDY",new Vector3(26,2.1f,-103),5);
        foreach(float x in new[]{24f,28f})Box(t,"Podpora tablicy przyjazdów",new Vector3(x,1,-103),new Vector3(.08f,2,.08f),campusSteel);
        foreach(float z in new[]{-146f,-134f,-122f,-110f})Lamp(t,new Vector3(28,0,z));
        // Footpath protection, leaving boarding openings at both coach doors.
        foreach(float z in new[]{-120f,-114f,-106f})Cylinder(t,"Słupek ochronny",new Vector3(24,.08f,z),new Vector3(24,1.05f,z),.075f,campusSteel,true);
        foreach(float z in new[]{-120f,-114f})Bench(t,new Vector3(27,0,z));
        var shelter=Group("Wiata dla przyjeżdżających",t,P(27,0,-114));
        foreach(float x in new[]{-1.4f,1.4f})foreach(float z in new[]{-4f,4f})Box(shelter,"Słup wiaty",new Vector3(x,1.45f,z),new Vector3(.12f,2.9f,.12f),campusSteel);
        foreach(float x in new[]{-1.4f,1.4f}) {
            Box(shelter,"Oczep wiaty",new Vector3(x,2.76f,0),new Vector3(.16f,.22f,8.3f),wood,false);
            foreach(float z in new[]{-4f,4f}) {
                var brace=Box(shelter,"Miecz usztywniający",new Vector3(x,2.42f,z-Mathf.Sign(z)*.28f),new Vector3(.12f,.8f,.12f),wood,false);
                brace.transform.localRotation=Quaternion.Euler(-Mathf.Sign(z)*45,0,0);
            }
        }
        MetalRoof(shelter,3.3f,8.6f,2.9f,.5f);
        var route=Group("Trasa kontrolna autobusu — bez automatycznej jazdy",t,Vector3.zero);
        Group("Dojazd",route,P(0,0,-195));Group("Brama",route,P(0,0,-150));
        for(int i=0;i<=32;i++){
            float a=Mathf.PI+i*Mathf.PI*2/32;
            Group("Łuk "+i,route,P(Mathf.Sin(a)*12,0,-112+Mathf.Cos(a)*12));
        }
    }
    static void Disc(Transform parent,string name,Vector3 center,float radius,Material mat,bool solid)
    {
        const int n=96;var vertices=new Vector3[n+1];var uv=new Vector2[n+1];var triangles=new int[n*3];
        vertices[0]=center*K;uv[0]=new Vector2(center.x,center.z)*K;
        for(int i=0;i<n;i++){
            float a=i*Mathf.PI*2/n;Vector3 p=center+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);
            vertices[i+1]=p*K;uv[i+1]=new Vector2(p.x,p.z)*K;triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%n+1;
        }
        var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var g=Group(name,parent,Vector3.zero).gameObject;mesh=SaveMesh(mesh);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void CampusPerimeter()
    {
        var fence=Group("Ogrodzenie całego obozu",root,Vector3.zero);
        FenceLine(fence,new Vector2(-150,-150),new Vector2(-4.2f,-150));
        FenceLine(fence,new Vector2(4.2f,-150),new Vector2(5.8f,-150));
        FenceLine(fence,new Vector2(7.8f,-150),new Vector2(150,-150));
        FenceLine(fence,new Vector2(150,-150),new Vector2(150,150));
        FenceLine(fence,new Vector2(150,150),new Vector2(-150,150));
        FenceLine(fence,new Vector2(-150,150),new Vector2(-150,-150));
        var gate=Group("Brama przesuwna · 8,4 m światła",root,P(0,0,-150));
        foreach(float x in new[]{-4.5f,4.5f}){
            Box(gate,"Filar bramy",new Vector3(x,1.65f,0),new Vector3(.5f,3.3f,.65f),campusSteel);
            Box(gate,"Okładzina filara",new Vector3(x,1.6f,-.34f),new Vector3(.34f,2.8f,.06f),wood,false);
        }
        Sign(gate,"BRAXHORN · OBÓZ TRENINGOWY",new Vector3(0,5.5f,0),10);
        foreach(float x in new[]{-5.5f,5.5f})Box(gate,"Słup oznakowania",new Vector3(x,2.85f,.5f),new Vector3(.15f,5.7f,.15f),campusSteel);
        var leaf=Group("Skrzydło bramy",gate,Vector3.zero);
        Box(leaf,"Rama bramy",new Vector3(0,1.15f,0),new Vector3(8.2f,2.3f,.12f),campusSteel);
        for(int i=0;i<54;i++)Box(leaf,"Lamele bramy",new Vector3(-4+i*.15f,1.2f,-.09f),new Vector3(.095f,2.1f,.07f),wood,false);
        CombineDecoration(leaf);
        var controller=gate.gameObject.AddComponent<BraxObozSlidingGate>();controller.leaf=leaf;
        controller.openOffset=Vector3.left*(8.7f*K);controller.detectionRadius=24*K;
        var foot=Group("Furtka piesza",root,P(6.8f,0,-150));
        var footLeaf=Group("Skrzydło furtki",foot,Vector3.zero);
        Box(footLeaf,"Furtka",new Vector3(0,1.1f,0),new Vector3(1.8f,2.2f,.1f),wood);
        var footController=foot.gameObject.AddComponent<BraxObozSlidingGate>();footController.leaf=footLeaf;
        footController.openOffset=Vector3.right*(2.1f*K);footController.detectionRadius=5*K;
        // Road end is visibly closed; there is no collider across the main gate.
        Box(root,"Bariera końca odcinka drogi",new Vector3(0,.5f,-205),new Vector3(7.5f,1,.35f),campusSteel);
    }
    static void FenceLine(Transform parent,Vector2 a,Vector2 b)
    {
        float length=Vector2.Distance(a,b);int chunks=Mathf.CeilToInt(length/12);
        for(int chunk=0;chunk<chunks;chunk++){
            Vector2 p=Vector2.Lerp(a,b,chunk/(float)chunks),q=Vector2.Lerp(a,b,(chunk+1)/(float)chunks);float span=Vector2.Distance(p,q);
            var t=Group("Przęsło ogrodzenia",parent,P((p.x+q.x)*.5f,0,(p.y+q.y)*.5f));
            t.localRotation=Quaternion.Euler(0,-Mathf.Atan2(q.y-p.y,q.x-p.x)*Mathf.Rad2Deg,0);
            Box(t,"Podmurówka",new Vector3(0,.12f,0),new Vector3(span,.24f,.24f),campusPaving,false);
            foreach(float y in new[]{.45f,2.15f})Box(t,"Rygiel",new Vector3(0,y,0),new Vector3(span,.08f,.10f),campusSteel,false);
            int posts=Mathf.CeilToInt(span/3);
            for(int i=0;i<=posts;i++)Box(t,"Słupek",new Vector3(-span*.5f+span*i/posts,1.3f,0),new Vector3(.1f,2.6f,.1f),campusSteel,false);
            int slats=Mathf.FloorToInt(span/.15f);
            for(int i=0;i<slats;i++)Box(t,"Lamel drewniany",new Vector3(-span*.5f+(i+.5f)*span/slats,1.35f,-.08f),new Vector3(.095f,2.2f,.065f),wood,false);
            t.gameObject.AddComponent<BoxCollider>().center=P(0,1.3f,0);t.GetComponent<BoxCollider>().size=P(span,2.6f,.25f);
            CombineDecoration(t);
        }
    }
    static void CombineDecoration(Transform parent)
    {
        var filters=parent.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()).ToArray();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial)){
            var mesh=new Mesh{name="Geometria zbiorcza",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            mesh.RecalculateBounds();var target=Group("Geometria · "+group.Key.name,parent,Vector3.zero).gameObject;
            target.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);target.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
    }
    static void Lamp(Transform parent,Vector3 p)
    {
        Cylinder(parent,"Słup lampy",p,p+Vector3.up*3.5f,.055f,campusSteel,true);
        Box(parent,"Oprawa",p+Vector3.up*3.5f,new Vector3(.5f,.14f,.5f),campusSteel,false);
        var fixture=Group("Światło dojścia",parent,(p+Vector3.up*3.35f)*K).gameObject;
        var light=fixture.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.84f,.65f);light.intensity=1.5f;light.range=8*K;light.shadows=LightShadows.None;
        fixture.AddComponent<BraxObozLocalLight>().source=light;
    }
    static void Bench(Transform parent,Vector3 p)
    {
        for(int i=0;i<4;i++)Box(parent,"Deska siedziska",p+new Vector3(0,.48f,-.22f+i*.145f),new Vector3(1.8f,.07f,.12f),wood);
        foreach(float x in new[]{-.65f,.65f}) {
            Box(parent,"Noga ławki",p+new Vector3(x,.23f,0),new Vector3(.08f,.46f,.5f),campusSteel);
            Box(parent,"Wspornik oparcia",p+new Vector3(x,.69f,.24f),new Vector3(.06f,.62f,.06f),campusSteel,false);
            Box(parent,"Podłokietnik",p+new Vector3(x,.7f,0),new Vector3(.075f,.06f,.47f),wood,false);
        }
        for(int i=0;i<3;i++)Box(parent,"Deska oparcia",p+new Vector3(0,.65f+i*.14f,.27f),new Vector3(1.8f,.11f,.055f),wood,false);
    }
    static void PavedPaths(List<Rect> areas)
    {
        var t=Group("Chodniki brukowane z obrzeżem",root,Vector3.zero);
        var xs=areas.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(v=>v).ToArray();
        var zs=areas.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(v=>v).ToArray();
        bool Inside(float x,float z)=>areas.Any(r=>r.Contains(new Vector2(x,z)));
        for(int i=0;i<xs.Length-1;i++)for(int j=0;j<zs.Length-1;j++) {
            float x=(xs[i]+xs[i+1])*.5f,z=(zs[j]+zs[j+1])*.5f;
            if(!Inside(x,z))continue;
            float w=xs[i+1]-xs[i],d=zs[j+1]-zs[j];
            var slab=Box(t,"Kostka — ciągła skala i fuga",new Vector3(x,.025f,z),new Vector3(w,.05f,d),campusPaving,false);
            // Absolute UV origin keeps the paving bond aligned across all cells.
            var mf=slab.GetComponent<MeshFilter>();var mesh=Object.Instantiate(mf.sharedMesh);var uv=mesh.uv;
            for(int v=0;v<uv.Length;v++)uv[v]+=new Vector2(x,z)*K;
            mesh.uv=uv;mf.sharedMesh=SaveMesh(mesh);
            if(!Inside(xs[i]-.001f,z))Box(t,"Obrzeże",new Vector3(xs[i]-.04f,.028f,z),new Vector3(.08f,.056f,d),gravel,false);
            if(!Inside(xs[i+1]+.001f,z))Box(t,"Obrzeże",new Vector3(xs[i+1]+.04f,.028f,z),new Vector3(.08f,.056f,d),gravel,false);
            if(!Inside(x,zs[j]-.001f))Box(t,"Obrzeże",new Vector3(x,.028f,zs[j]-.04f),new Vector3(w,.056f,.08f),gravel,false);
            if(!Inside(x,zs[j+1]+.001f))Box(t,"Obrzeże",new Vector3(x,.028f,zs[j+1]+.04f),new Vector3(w,.056f,.08f),gravel,false);
        }
        // One renderer per material, with flush walkable surface over terrain.
        CombineDecoration(t);
        // The slab is above the soil; physics must follow its visible top.
        foreach(var mf in t.GetComponentsInChildren<MeshFilter>())
            if(mf.GetComponent<MeshRenderer>().sharedMaterial==campusPaving)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
    }
    static void MetalRoof(Transform parent,float width,float depth,float eave,float rise)
    {
        var t=Group("Blacha na rąbek — dach, konstrukcja, odwodnienie",parent,Vector3.zero);
        float half=width*.5f, slope=Mathf.Sqrt(half*half+rise*rise);
        float angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
        for(int side=-1;side<=1;side+=2) {
            var plane=Group("Połać",t,P(side*half*.5f,eave+rise*.5f,0));
            plane.localRotation=Quaternion.Euler(0,0,-side*angle);
            Box(plane,"Pokrycie blaszane",Vector3.zero,new Vector3(slope,.035f,depth),metal,false);
            int seams=Mathf.CeilToInt(depth/.5f);
            for(int i=0;i<=seams;i++)
                Box(plane,"Rąbek stojący",new Vector3(0,.038f,-depth*.5f+i*depth/seams),new Vector3(slope,.04f,.018f),metal,false);
            foreach(float z in new[]{-depth*.5f,depth*.5f})
                Box(plane,"Wiatrownica",new Vector3(0,-.02f,z),new Vector3(slope,.16f,.055f),metal,false);
            Box(t,"Deska okapowa",new Vector3(side*half,eave-.1f,0),new Vector3(.1f,.22f,depth),wood,false);
            // Open U-channel gutter; downpipes discharge outside the footprint.
            float gutterX=side*(half+.09f);
            Box(t,"Dno rynny",new Vector3(gutterX,eave-.16f,0),new Vector3(.15f,.015f,depth),metal,false);
            foreach(float lip in new[]{-.075f,.075f})
                Box(t,"Krawędź rynny",new Vector3(gutterX+lip,eave-.105f,0),new Vector3(.015f,.11f,depth),metal,false);
            foreach(float z in new[]{-depth*.5f+.25f,depth*.5f-.25f}) {
                Cylinder(t,"Rura spustowa",new Vector3(gutterX,eave-.16f,z),new Vector3(gutterX,.28f,z),.045f,metal);
                Cylinder(t,"Wylewka",new Vector3(gutterX,.28f,z),new Vector3(gutterX+side*.25f,.12f,z),.045f,metal);
            }
        }
        Box(t,"Obróbka kalenicy",new Vector3(0,eave+rise+.03f,0),new Vector3(.18f,.08f,depth+.08f),metal,false);
        // Repeated timber trusses and longitudinal purlins visible from below.
        foreach(float side in new[]{-1f,1f})
            Box(t,"Płatew",new Vector3(side*half*.5f,eave+rise*.5f-.32f,0),new Vector3(.16f,.22f,depth),wood,false);
        int bays=Mathf.CeilToInt(depth/3);
        for(int i=0;i<=bays;i++) {
            float z=-depth*.5f+.25f+(depth-.5f)*i/bays;
            Box(t,"Pas dolny wiązara",new Vector3(0,eave-.18f,z),new Vector3(width,.24f,.14f),wood,false);
            foreach(float side in new[]{-1f,1f}) {
                var beam=Box(t,"Krokiew",new Vector3(side*half*.5f,eave+rise*.5f-.15f,z),new Vector3(slope,.18f,.12f),wood,false);
                beam.transform.localRotation=Quaternion.Euler(0,0,-side*angle);
            }
            Box(t,"Słupek wiązara",new Vector3(0,eave+rise*.5f-.18f,z),new Vector3(.12f,rise,.12f),wood,false);
        }
        CombineDecoration(t);
    }
    static void CabinFinish(Transform cabin)
    {
        var trim=Group("Wykończenie domku",cabin,Vector3.zero);
        MetalRoof(trim,7.1f,9.1f,2.85f,1.15f);
        Box(trim,"Cokół fundamentowy",new Vector3(0,-.035f,0),new Vector3(6.3f,.15f,8.3f),gravel,false);
        foreach(float x in new[]{-3.13f,3.13f})foreach(float z in new[]{-4.13f,4.13f})
            Box(trim,"Deska narożna",new Vector3(x,1.5f,z),new Vector3(.17f,2.7f,.17f),wood,false);
        foreach(float z in new[]{-4.05f,4.05f}) {
            var mesh=new Mesh{name="Zamknięty szczyt"};
            mesh.vertices=new[]{P(-3.15f,2.78f,z),P(3.15f,2.78f,z),P(3.15f,2.985f,z),P(0,4,z),P(-3.15f,2.985f,z)};
            mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x,v.y)).ToArray();
            mesh.vertices=mesh.vertices.Concat(mesh.vertices).ToArray();mesh.uv=mesh.uv.Concat(mesh.uv).ToArray();
            mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,5,6,7,5,7,8,5,8,9};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var g=Group("Szczyt z desek",trim,Vector3.zero).gameObject;
            g.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);g.AddComponent<MeshRenderer>().sharedMaterial=wood;
        }
        var glass=BraxObozMaterials.Plain("szyby-domkow",new Color(.65f,.75f,.79f,.2f),.9f);
        glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",1);glass.SetInt("_DstBlend",10);
        glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;
        foreach(float side in new[]{-1f,1f}) {
            Box(trim,"Zamknięcie pod okapem",new Vector3(side*3,2.93f,0),new Vector3(.14f,.24f,8.2f),wood,false);
            foreach(float z in new[]{-1.1f,0,1.1f})Box(trim,"Rama okienna",new Vector3(side*3.08f,1.55f,z),new Vector3(.16f,1.08f,.07f),wood,false);
            foreach(float y in new[]{1.03f,2.07f})Box(trim,"Rama pozioma",new Vector3(side*3.08f,y,0),new Vector3(.16f,.08f,2.26f),wood,false);
            Box(trim,"Szyba",new Vector3(side*3.07f,1.55f,0),new Vector3(.015f,.96f,2.14f),glass,false);
            Box(trim,"Parapet",new Vector3(side*3.12f,1.0f,0),new Vector3(.36f,.055f,2.4f),metal,false);
        }
        foreach(float x in new[]{-.7f,.7f})Box(trim,"Ościeżnica",new Vector3(x,1.3f,-4.12f),new Vector3(.09f,2.4f,.12f),wood,false);
        Box(trim,"Nadproże drzwi",new Vector3(0,2.52f,-4.12f),new Vector3(1.5f,.12f,.12f),wood,false);
        CombineDecoration(trim);
    }
    public static void ReplaceServiceRoof(Transform sourceGroup,Transform model,float width,float depth)
    {
        // The authored GLB joins walls, roof and furniture in StaticVisual.
        // Strip only the known upper shell in a generated mesh copy; never
        // edit the imported source asset or the articulated door hierarchy.
        int removed=0;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            bool isStatic=false;
            for(var node=filter.transform;node && node!=model.parent;node=node.parent)
                if(node.name=="StaticVisual"){isStatic=true;break;}
            if(!isStatic)continue;
            var original=filter.sharedMesh;
            if(!original || !original.isReadable)throw new InvalidOperationException("Nieczytelna siatka zaplecza: "+sourceGroup.name);
            var vertices=original.vertices;
            var matrix=sourceGroup.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var points=vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
            var mesh=Object.Instantiate(original);int removedHere=0;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                if(mesh.GetTopology(sub)!=MeshTopology.Triangles)throw new InvalidOperationException("Nieobsługiwana siatka zaplecza");
                var triangles=mesh.GetTriangles(sub);var kept=new List<int>(triangles.Length);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=points[triangles[i]];var b=points[triangles[i+1]];var c=points[triangles[i+2]];
                    float high=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
                    bool outside=Mathf.Max(Mathf.Abs(a.x),Mathf.Max(Mathf.Abs(b.x),Mathf.Abs(c.x)))>width*.5f+.13f ||
                        Mathf.Max(Mathf.Abs(a.z),Mathf.Max(Mathf.Abs(b.z),Mathf.Abs(c.z)))>depth*.5f+.13f;
                    if(high>2.901f || (high>2.80f && outside)){removedHere++;continue;}
                    kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
                }
                mesh.SetTriangles(kept,sub);
            }
            if(removedHere==0){Object.DestroyImmediate(mesh);continue;}
            mesh.RecalculateBounds();filter.sharedMesh=SaveMesh(mesh);removed+=removedHere;
        }
        if(removed==0)throw new InvalidOperationException("Nie znaleziono starego dachu zaplecza: "+sourceGroup.name);
        var shell=Group(sourceGroup.name+" — nowy dach",sourceGroup.parent,sourceGroup.localPosition);
        float half=width*.5f+.45f,rise=half*.38f,eave=2.94f;
        MetalRoof(shell,half*2,depth+.8f,eave,rise);
        foreach(float z in new[]{-depth*.5f,depth*.5f})
        {
            float wallTop=eave+rise*(1-width*.5f/half);
            var mesh=new Mesh{name="Szczyt zaplecza"};
            var v=new[]{P(-width*.5f,2.9f,z),P(width*.5f,2.9f,z),
                P(width*.5f,wallTop,z),P(0,eave+rise,z),P(-width*.5f,wallTop,z)};
            mesh.vertices=v.Concat(v).ToArray();mesh.uv=mesh.vertices.Select(p=>new Vector2(p.x,p.y)).ToArray();
            mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,5,6,7,5,7,8,5,8,9};
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var face=Group("Szczyt z desek",shell,Vector3.zero).gameObject;
            face.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);face.AddComponent<MeshRenderer>().sharedMaterial=wood;
        }
        foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})
            Box(shell,"Wykończenie narożnika",new Vector3(x,1.52f,z),new Vector3(.17f,2.74f,.17f),wood,false);
        CombineDecoration(shell);
        Debug.Log("BRAXHORN_OBOZ wymieniono dach: "+sourceGroup.name+" usunięte trójkąty="+removed);
    }
}

