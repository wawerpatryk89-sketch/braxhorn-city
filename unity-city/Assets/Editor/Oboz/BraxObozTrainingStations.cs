using System;
using UnityEngine;

// Camp station interiors only. The existing campus positions and rubber bases
// stay with the main generator; equipment comes from verified owner meshes.
public static partial class BraxObozEnvironment
{
    static void OwnerStationEquipment(Transform parent,string item,Vector3 at,float yaw=0f)
    {Optional("oryginalny sprzęt "+item,()=>BraxObozGymEquipment.Place(parent,item,at,yaw));}

    static void OwnerTrainingStations()
    {
        Station("Rozgrzewka i mobilność",-12,30,14,8,t=>{
            for(int i=0;i<6;i++)OwnerStationEquipment(t,"yoga_mat_"+(i%3+1),
                new Vector3(-5+i*2,.08f,0),90f);
            Optional("oryginalny stojak na piłki",()=>BraxObozServices.OwnerFixtureSourceMetres(t,"model(1).glb",
                new Vector3(5.35f,.08f,2.05f),new Vector3(1.55f,1.60f,1.50f)));
        });
        Station("Drążki",35,34,12,10,t=>{
            OwnerStationEquipment(t,"horizontal_bar_1",new Vector3(-4,.08f,0),90f);
            OwnerStationEquipment(t,"horizontal_bar_2",new Vector3(-1.4f,.08f,0),90f);
            OwnerStationEquipment(t,"horizontal_bar_3",new Vector3(1.2f,.08f,0),90f);
            OwnerStationEquipment(t,"pull_up_rack",new Vector3(4,.08f,0));
            Optional("oryginalny zwój liny przy drążkach",()=>
                BraxObozServices.OwnerFixtureSourceMetres(t,"rope-coil.glb",
                    new Vector3(-4f,.08f,3.2f),new Vector3(.6f,.2f,.5f)));
        });
        Station("Poręcze",35,19,12,10,t=>{
            OwnerStationEquipment(t,"parallel_bars_1",new Vector3(-3,.08f,0),90f);
            OwnerStationEquipment(t,"parallel_bars_2",new Vector3(3,.08f,0),90f);
        });
        Station("Stopnie treningowe",35,4,12,10,t=>{
            for(int i=0;i<3;i++)OwnerStationEquipment(t,"step_platform",
                new Vector3(-3.2f+i*3.2f,.08f,0));
            BraxBuildQuality.Warn("OBÓZ H: dostarczono step_platform 0,20 m. "+
                "Brak oryginalnych skrzyń plyometrycznych 0,30/0,45/0,60 m; nie zbudowano atrap.");
        });
        Station("Pchanie sań — dwa tory",35,-18,12,26,t=>{
            for(int i=0;i<3;i++)Box(t,"Linia toru",new Vector3(-5+i*5,.086f,0),
                new Vector3(.06f,.004f,23),campusMark,false);
            for(int i=0;i<5;i++)Box(t,"Oznaczenie odległości",new Vector3(0,.088f,-10+i*5),
                new Vector3(10,.004f,.05f),campusMark,false);
            BraxBuildQuality.Warn("OBÓZ H: brak dostarczonego modelu sań treningowych; "+
                "pozostawiono zatwierdzone tory, bez zastępczych płóz, uchwytów i ciężarów.");
        });
        Station("Opony do przewracania — ORYGINALNY MODEL 3D",35,-45,12,10,t=>{
            foreach(float x in new[]{-2.5f,2.5f})
                Optional("oryginalna opona",()=>BraxObozServices.OwnerFixtureSourceMetres(t,"OwnerExtras/tractor_tire.glb",
                    new Vector3(x,.08f,0),new Vector3(1.90f,.95f,1.90f)));
            BraxBuildQuality.Warn("OBÓZ TRENING OPON: oryginalne modele; interakcja przewracania wymaga osobnego testu.");
        });
        Station("Liny treningowe",12,28,12,10,t=>{
            foreach(float x in new[]{-3f,3f})
                Optional("oryginalny zwój liny",()=>BraxObozServices.OwnerFixtureSourceMetres(t,"rope-coil.glb",
                    new Vector3(x,.08f,.3f),new Vector3(2.15f,.85f,2.15f)));
            BraxBuildQuality.Warn("OBÓZ H: rope-coil jest prawdziwym zwojem liny. "+
                "Brak dostarczonej rozwiniętej battle rope i mocowania; nie wstawiono atrap.");
        });
    }

    static void OwnerStrengthInteriors(Transform gym)
    {
        void Place(string item,float x,float z,float yaw=0f)
        {
            Optional("oryginalne urządzenie "+item,()=>
                BraxObozGymEquipment.Place(gym,item,new Vector3(x,.22f,z),yaw));
        }
        Place("bench_press",-7.5f,-3.5f);
        Place("loaded_weight_rack",0,-3.5f);
        Place("treadmill",7.5f,-3.5f);
        Place("exercise_bike",7.5f,3.5f);
        Place("abdominal_bench",-7.5f,3.5f);
        Optional("oryginalna ławka z kaskiem",()=>{
            var bench=BraxObozGymEquipment.Place(gym,"bench",new Vector3(0,.22f,3.5f));
            Optional("oryginalny kask na ławce",()=>{
                var geometry=bench.GetComponent<BraxObozEquipmentGeometry>();
                if(!geometry.TryMeasureBounds(out var bounds))
                    throw new InvalidOperationException("Brak pomiaru powierzchni oryginalnej ławki");
                var target=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
                var helmet=BraxObozServices.OwnerFixture(gym,"boxing_headgear_helmet_low_poly.glb",
                    gym.InverseTransformPoint(target),new Vector3(.38f,.34f,.39f));
                if(!BraxObozPlacement.OnSupport(helmet.transform,helmet.GetComponentsInChildren<Renderer>(true),
                    bench,target,"kask na oryginalnej ławce"))
                    throw new InvalidOperationException("Nie potwierdzono kontaktu kasku z ławką");
            });
        });
        for(int i=1;i<=6;i++)Place("kettlebell_"+i,-6f+(i-1)*1f,-6.5f);
        for(int i=1;i<=3;i++)Place("dumbbell_"+i,2.5f+(i-1)*1.5f,-6.5f);
        Place("horizontal_bar_4",-10.5f,0,90f);
    }
}
