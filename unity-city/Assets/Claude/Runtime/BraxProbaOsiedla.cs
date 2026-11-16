#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using UnityEngine;

// Used only when the installed APK is launched with the explicit Android QA intent.
public static class BraxProbaOsiedla
{
    public static void UruchomJesliWybrana(BraxGymPlayer player)
    {
        using(var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using(var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using(var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            if(intent.Call<bool>("getBooleanExtra","brax_osiedle_qa",false))
                player.StartCoroutine(Sprawdz(player));
        }
    }

    static IEnumerator Sprawdz(BraxGymPlayer p)
    {
        // The second area can stream in after Start. Each check below uses the normal player and interaction.
        for(int i=0;i<120 && (!p.bar || BraxAutoGracza.Wszystkie.Count<14);i++)
            yield return new WaitForSeconds(.5f);
        if(!p.bar || p.bar.stanowiska.Length==0) { Debug.LogError("BX_QA_FAIL brak baru"); yield break; }
        var bar=p.bar;var b=bar.stanowiska[0];
        Vector3 dir=b.barman.position-b.punktOdbioru;
        p.TravelTo(b.punktOdbioru+Vector3.up*.05f,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg+180f);
        Debug.Log("BX_QA_START bar");yield return new WaitForSeconds(2f);
        if(!bar.Wykonaj(p.transform.position)){Debug.LogError("BX_QA_FAIL zamówienie");yield break;}
        float end=Time.time+15f;
        while(bar.Kontekst(p.transform.position)!="NAPIJ SIĘ" && Time.time<end)yield return null;
        if(!bar.Wykonaj(p.transform.position) || !p.szklanka || !p.szklanka.gameObject.activeInHierarchy)
        {Debug.LogError("BX_QA_FAIL odbiór szklanki");yield break;}
        yield return new WaitForSeconds(p.clips["Drink"].length+1f);
        Debug.Log("BX_QA_PASS bar");yield return new WaitForSeconds(4f);

        for(int nr=1;nr<=14;nr++)
        {
            string plate="BX "+(10000+nr);
            var car=BraxAutoGracza.Wszystkie.Find(c=>c && c.rejestracja==plate);
            if(!car){Debug.LogError("BX_QA_FAIL brak "+plate);yield break;}
            Debug.Log("BX_QA_START BX"+(10000+nr));yield return new WaitForSeconds(2f);
            p.TravelTo(car.Podejscie+Vector3.up*.06f,car.transform.eulerAngles.y+150f);
            Physics.SyncTransforms();
            if(!car.Wykonaj(p.transform.position)){Debug.LogError("BX_QA_FAIL wsiadanie "+plate);yield break;}
            end=Time.time+18f;
            while(car.WTrakcie && Time.time<end)yield return null;
            if(car.WTrakcie || p.autoGracza!=car){Debug.LogError("BX_QA_FAIL fotel "+plate);yield break;}
            Vector3 start=car.transform.position;Quaternion wheel=car.kola[0].localRotation;
            float roll=0f,steer=0f,column=0f;end=Time.time+2f;
            while(Time.time<end)
            {
                p.MoveInput(Time.time<end-1f?"0,.5":".45,.6");
                roll=Mathf.Max(roll,Quaternion.Angle(wheel,car.kola[0].localRotation));
                steer=Mathf.Max(steer,Quaternion.Angle(Quaternion.identity,car.skret[0].localRotation));
                column=Mathf.Max(column,Quaternion.Angle(car.spoczynkowaKierownicy,car.wieniec.localRotation));
                yield return null;
            }
            p.MoveInput("0,0");end=Time.time+8f;
            while(Mathf.Abs(car.Predkosc)>.1f && Time.time<end)yield return null;
            float distance=Vector3.Distance(start,car.transform.position);
            Debug.Log($"BX_QA_POMIAR {plate} droga={distance:F2} kolo={roll:F1} skret={steer:F1} kierownica={column:F1}");
            if(distance<.3f || roll<5f || steer<2f || column<5f)
            {Debug.LogError("BX_QA_FAIL jazda "+plate);yield break;}
            car.Wykonaj(p.transform.position);end=Time.time+18f;
            while(p.autoGracza && Time.time<end)yield return null;
            if(p.autoGracza){Debug.LogError("BX_QA_FAIL wysiadanie "+plate);yield break;}
            Debug.Log("BX_QA_PASS BX"+(10000+nr));yield return new WaitForSeconds(4f);
        }
        Debug.Log("BX_QA_GOTOWE");
    }
}
#endif
