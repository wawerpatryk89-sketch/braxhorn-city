using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    sealed class WymaganeOsiedleException : Exception
    { public WymaganeOsiedleException(string message):base(message){} }

    static void SprawdzZlecenieOsiedla()
    {
        var errors=new List<string>();
        void Check(string name,Action check)
        {try{check();}catch(Exception e){errors.Add(name+": "+e.Message);}}
        Check("sterowanie",BraxTestSterowaniaOsiedla.Wykonaj);
        Check("loże",DyskotekaLozeKontrola);
        Check("oświetlenie klubu",DyskotekaSwiatlo);
        Check("auto i dłonie Patryka",AutoGraczaKontrola);
        Check("wejście salonu",SalonPrzejscieKontrola);
        Check("wejście sali bokserskiej",SalaBokserskaPrzejscieKontrola);
        Check("drogi",DrogiZlecenieKontrola);
        // Collect independent failures in one report, never replace a missing feature silently.
        System.IO.Directory.CreateDirectory("city-review");
        System.IO.File.WriteAllLines("city-review/osiedle-zlecenie-20261104.txt",errors.Count==0
            ?new[]{"PASS: kontrole generowania sceny; nadal wymagane Play Mode i telefon"}:errors.ToArray());
        if(errors.Count>0)throw new WymaganeOsiedleException(string.Join("\n",errors));
    }

    static void DrogiZlecenieKontrola()
    {
        if(ulice==null || ulice.Count!=8)throw new InvalidOperationException("Niekompletna sieć ulic A");
        foreach(var u in ulice)
            if(u.cw*2f<8f)throw new InvalidOperationException("Nadal zbyt wąska ulica "+u.id+": "+(u.cw*2f).ToString("F2"));
        if(RondoJezdnia-RondoOpaska<8f)throw new InvalidOperationException("Pierścień ronda ma mniej niż 8 m");
        foreach(var obstacle in PrzeszkodyDrog())
        {
            var nearest=new Vector2(Mathf.Clamp(0,obstacle.x0,obstacle.x1),Mathf.Clamp(RondoZ,obstacle.z0,obstacle.z1));
            if(Vector2.Distance(nearest,new Vector2(0,RondoZ))<RondoChodnik+.02f)
                throw new InvalidOperationException("Rondo wchodzi w trwały obrys: "+obstacle.co);
        }
        Debug.Log("BRAXHORN_DROGI_ZLECENIE_OK ulice=8 min_jezdnia=8m rondo_pierscien="+(RondoJezdnia-RondoOpaska).ToString("F2"));
    }
}
