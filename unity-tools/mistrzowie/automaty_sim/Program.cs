// automaty_sim: the casinos' slot machines checked without Unity (owner 16.11 18:22 "te maszyny mają funkcjonować").
//  1. the pay tables over the full cycle: the mechanical machine's 8 000 drum combinations, the screen machine's 3 200 000
//     (every stop of five reels, five lines) - return to player and win frequency;
//  2. the real game component (BraxChampionsAutomaty) played spin after spin with a frame clock: the stake leaves the
//     credit, the drums / screen reels end exactly on a stop, the win paid is the pay table's for what stands on the line,
//     the jackpot grows by 2 % of the stake and pays out.
// usage: dotnet run -- [spins]   (exit 1 on any mismatch)
using System;
using System.Reflection;
using UnityEngine;
using Z = BraxChampionsAutomatyZasady;

static class Program
{
    static int fails;
    static void Check(bool ok, string what) { if (!ok) { fails++; if (fails < 20) Console.WriteLine("FAIL " + what); } }

    static int Main(string[] args)
    {
        int spins = args.Length > 0 ? int.Parse(args[0]) : 3000;
        // ---- 1. the full cycle
        long win = 0, hits = 0, jack = 0; var pm = new[] { Z.Pasek(0, false), Z.Pasek(1, false), Z.Pasek(2, false) };
        for (int a = 0; a < Z.Stopy; a++) for (int b = 0; b < Z.Stopy; b++) for (int c = 0; c < Z.Stopy; c++)
        {
            int m = Z.Mechaniczny(pm[0][a], pm[1][b], pm[2][c]);
            if (m < 0) jack++; else if (m > 0) { win += m; hits++; }
        }
        double n3 = Math.Pow(Z.Stopy, 3), jackX = BraxChampionsKasyno.JackpotStart / 10.0;
        Console.WriteLine("MECHANICAL base RTP {0:F3} + jackpot (start {1}x) {2:F3} = {3:F3}; a win every {4:F1} spins; jackpot 1 in {5}",
            win / n3, jackX, jack * jackX / n3, win / n3 + jack * jackX / n3, n3 / hits, n3 / Math.Max(1, jack));
        long sw = 0, sh = 0, sj = 0; var st = new int[5];
        for (st[0] = 0; st[0] < Z.Stopy; st[0]++) for (st[1] = 0; st[1] < Z.Stopy; st[1]++) for (st[2] = 0; st[2] < Z.Stopy; st[2]++)
        for (st[3] = 0; st[3] < Z.Stopy; st[3]++) for (st[4] = 0; st[4] < Z.Stopy; st[4]++)
        {
            int m = Z.Ekran(Z.Okno(st), out int linie, out bool j); sw += m; if (m > 0) sh++; if (j) sj++;
        }
        double n5 = Math.Pow(Z.Stopy, 5);
        Console.WriteLine("SCREEN RTP {0:F3}; a win on {1:F1} % of spins; jackpot 1 in {2:F0}", sw / n5 / 5.0, 100.0 * sh / n5, n5 / Math.Max(1, sj));
        double rtpM = win / n3 + jack * jackX / n3, rtpS = sw / n5 / 5.0;
        Check(rtpM > .85 && rtpM < .98, "mechanical RTP " + rtpM); Check(rtpS > .85 && rtpS < .98, "screen RTP " + rtpS);

        // ---- 2. the game component, played
        PlayerPrefs.DeleteAll();
        var host = new GameObject("kasyno"); var gra = host.AddComponent<BraxChampionsAutomaty>(); gra.stawka = 10;
        var mat = new Material { name = "ekran" };
        BraxChampionsAutomaty.Maszyna Maszyna(bool ekran, Vector3 at)
        {
            var kor = new GameObject(ekran ? "ekran" : "mech").transform; kor.localPosition = at;
            var m = new BraxChampionsAutomaty.Maszyna { korpus = kor, ekran = ekran };
            var lic = new GameObject("licznik"); lic.transform.SetParent(kor, false); var r = lic.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; m.licznik = r;
            if (ekran)
            {
                m.kolumny = new MeshFilter[5];
                for (int i = 0; i < 5; i++) { var o = new GameObject("kol"); o.transform.SetParent(kor, false); m.kolumny[i] = o.AddComponent<MeshFilter>(); }
            }
            else
            {
                m.bebny = new Transform[3];
                for (int i = 0; i < 3; i++) { var o = new GameObject("beben"); o.transform.SetParent(kor, false); m.bebny[i] = o.transform; }
                var d = new GameObject("dzwignia"); d.transform.SetParent(kor, false); m.dzwignia = d.transform;
            }
            return m;
        }
        gra.maszyny = new[] { Maszyna(false, new Vector3(0, 0, 0)), Maszyna(true, new Vector3(10, 0, 0)) };
        var top = new GameObject("jackpot").AddComponent<MeshRenderer>(); top.sharedMaterial = mat; gra.jackpoty = new Renderer[] { top };
        var T = typeof(BraxChampionsAutomaty); const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        T.GetMethod("Start", F).Invoke(gra, null);
        var update = T.GetMethod("Update", F); var stanF = T.GetField("stan", F);
        var gracz = new GameObject("gracz").AddComponent<BraxGymPlayer>(); BraxChampionsGry.Gracz = gracz;
        Time.time = 1f; Time.deltaTime = 1f / 60f;
        for (int k = 0; k < 2; k++)
        {
            bool ekran = k == 1; var m = gra.maszyny[k];
            gracz.transform.localPosition = m.korpus.TransformPoint(new Vector3(0, 0, 1.2f));
            long staked = 0, paid = 0;
            for (int s = 0; s < spins; s++)
            {
                if (BraxChampionsKasyno.Kredyt < 10) BraxChampionsKasyno.Wplac(1000);
                int kredyt = BraxChampionsKasyno.Kredyt; long j0 = BraxChampionsKasyno.Jackpot;
                string ctx = gra.Kontekst(gracz.transform.position);
                Check(ctx.StartsWith(ekran ? "GRAJ" : "POCIĄGNIJ"), "context '" + ctx + "'");
                Check(gra.Wykonaj(gracz.transform.position), "play");
                Check(BraxChampionsKasyno.Kredyt == kredyt - 10, "stake taken");
                staked += 10;
                var stan = (Array)stanF.GetValue(gra); var sObj = stan.GetValue(k); var sT = sObj.GetType();
                int frames = 0;
                while ((bool)sT.GetField("kreci").GetValue(sObj) && frames < 2000) { Time.time += Time.deltaTime; update.Invoke(gra, null); frames++; }
                Check(frames < 2000, "spin ends");
                Check(frames > 60 && frames < 260, "spin length " + frames / 60f + " s");
                var poz = (float[])sT.GetField("poz").GetValue(sObj); int w = (int)sT.GetField("wygrana").GetValue(sObj);
                foreach (var p in poz) Check(Math.Abs(p - Math.Round(p)) < 1e-4, "reel ends on a stop " + p);
                int expect; bool jp;
                if (ekran)
                {
                    var stops = new int[5]; for (int r = 0; r < 5; r++) stops[r] = (int)Math.Round(poz[r]);
                    expect = Z.Ekran(Z.Okno(stops), out _, out jp) * 10 / 5;
                    var v = m.kolumny[2].sharedMesh.vertices; Check(v.Length == 16 && Math.Abs(v[4].y - (-.5f * m.komorka)) < 1e-4, "screen reel cell at the middle row");
                }
                else
                {
                    int a = Z.Symbol(Z.Pasek(0, false), (int)Math.Round(poz[0])), b = Z.Symbol(Z.Pasek(1, false), (int)Math.Round(poz[1])), c = Z.Symbol(Z.Pasek(2, false), (int)Math.Round(poz[2]));
                    int mm = Z.Mechaniczny(a, b, c); jp = mm < 0; expect = mm > 0 ? mm * 10 : 0;
                    for (int r = 0; r < 3; r++)
                    {
                        var q = m.bebny[r].localRotation; double ang = 2 * Math.Atan2(q.x, q.w) * 180 / Math.PI;
                        double want = Math.Round(poz[r]) % Z.Stopy * 18; double dd = ((ang - want) % 360 + 540) % 360 - 180;
                        Check(Math.Abs(dd) < .01, "drum " + r + " shows stop " + poz[r] + " (angle " + ang + ")");
                    }
                    Check(m.dzwignia.localRotation.w > .9999f, "lever back up");
                }
                if (jp) { Check(w == j0, "jackpot paid " + w + " of " + j0); Console.WriteLine("  JACKPOT " + w + (ekran ? " (screen)" : " (mechanical)")); }
                else Check(w == expect, "win " + w + " = pay table " + expect);
                Check(BraxChampionsKasyno.Kredyt == kredyt - 10 + w, "win credited");
                paid += w;
            }
            Console.WriteLine("{0} played {1} spins: staked {2}, paid {3} (RTP {4:F3}), jackpot now {5}", ekran ? "SCREEN" : "MECHANICAL", spins, staked, paid, (double)paid / staked, BraxChampionsKasyno.Jackpot);
        }
        // ---- 3. the roulette: every outside bet, the ball ends in the drawn number's pocket, even money pays 1:1, zero takes it
        PlayerPrefs.DeleteAll();
        var rh = new GameObject("ruletka"); var rul = rh.AddComponent<BraxChampionsRuletka>(); rul.stawka = 10;
        var rst = new GameObject("stol").transform; var rot = new GameObject("rotor").transform; rot.SetParent(rst, false);
        var kul = new GameObject("kulka").transform; kul.SetParent(rst, false);
        var kolo = new Vector3(.88f, .78f, 0f); rot.localPosition = kolo;
        var zet = new Transform[6]; var polaX = new float[6];
        for (int e = 0; e < 6; e++) { zet[e] = new GameObject("z").transform; zet[e].SetParent(rst, false); polaX[e] = .13f - .23f * e; }
        rul.stoly = new[] { new BraxChampionsRuletka.Stol { stol = rst, rotor = rot, kulka = kul, kolo = kolo, zetony = zet, polaX = polaX } };
        var RT = typeof(BraxChampionsRuletka); RT.GetMethod("Start", F).Invoke(rul, null);
        var rUpd = RT.GetMethod("Update", F); var rStan = RT.GetField("stan", F);
        int spinsR = Math.Max(200, spins / 10); long rStake = 0, rPaid = 0; var seen = new int[37];
        for (int sp = 0; sp < spinsR; sp++)
        {
            int e = sp % 6; gracz.transform.localPosition = rst.TransformPoint(new Vector3(polaX[e], 0, 1.0f));
            if (BraxChampionsKasyno.Kredyt < 10) BraxChampionsKasyno.Wplac(1000);
            int kr = BraxChampionsKasyno.Kredyt;
            string ctx = rul.Kontekst(gracz.transform.position);
            Check(ctx == "POSTAW 10 · " + BraxChampionsRuletka.Zasady.Pola[e], "roulette context '" + ctx + "' for box " + e);
            Check(rul.Wykonaj(gracz.transform.position), "roulette bet");
            var so = ((Array)rStan.GetValue(rul)).GetValue(0); var sT = so.GetType();
            int wynik = (int)sT.GetField("wynik").GetValue(so); seen[wynik]++;
            Check(zet[e].gameObject.activeSelf, "the chip lies on the box");
            int fr = 0; while ((bool)sT.GetField("kreci").GetValue(so) && fr < 2000) { Time.time += Time.deltaTime; rUpd.Invoke(rul, null); fr++; }
            Check(fr > 400 && fr < 560, "roulette spin length " + fr / 60f);
            // the ball in the pocket of the number drawn: its angle on the rotor = the pocket's angle
            var pos = kul.localPosition - kolo; float ang = (float)(Math.Atan2(pos.z, pos.x) * 180 / Math.PI);
            float rotorKat = (float)sT.GetField("rotorKat").GetValue(so);
            float rel = Mathf.DeltaAngle(BraxChampionsRuletka.Zasady.KatKieszeni(wynik), ang - rotorKat);
            Check(Math.Abs(rel) < 360f / 37f * .5f, "ball in pocket " + wynik + " (" + rel + " deg off)");
            float rr = new Vector2(pos.x, pos.z).magnitude; Check(Math.Abs(rr - rul.kieszenR) < .002f, "ball at the pocket radius " + rr);
            // the rotor turned counter-clockwise by rotorKat
            var q = rot.localRotation; float rk = (float)(-2 * Math.Atan2(q.y, q.w) * 180 / Math.PI);
            Check(Math.Abs(Mathf.DeltaAngle(rk, rotorKat)) < .01f, "rotor at its angle");
            int expect = BraxChampionsRuletka.Zasady.Wygrywa(e, wynik) ? 20 : 0;
            Check(BraxChampionsKasyno.Kredyt == kr - 10 + expect, "roulette pays " + expect + " on " + wynik + " " + BraxChampionsRuletka.Zasady.Pola[e]);
            Check(!zet[e].gameObject.activeSelf, "the chip taken / paid");
            rStake += 10; rPaid += expect;
        }
        int zeros = seen[0], reds = 0; for (int n = 1; n <= 36; n++) if (BraxChampionsRuletka.Zasady.Czerwony(n)) reds += seen[n];
        Console.WriteLine("ROULETTE played {0} spins: staked {1}, paid {2} (RTP {3:F3}; exact 36/37 = 0.973), zeros {4}, reds {5}", spinsR, rStake, rPaid, (double)rPaid / rStake, zeros, reds);
        Check(new System.Collections.Generic.HashSet<int>(BraxChampionsRuletka.Zasady.Kolo).Count == 37 && BraxChampionsRuletka.Zasady.Kolo[0] == 0, "the wheel: 0-36 once each");

        // the jackpot: 2 % of a stake of 10 = 0.20 a spin
        PlayerPrefs.DeleteAll(); for (int i = 0; i < 50; i++) BraxChampionsKasyno.Dorzuc(10);
        Check(BraxChampionsKasyno.Jackpot == BraxChampionsKasyno.JackpotStart + 10, "jackpot +2 % (" + BraxChampionsKasyno.Jackpot + ")");
        Check(BraxChampionsKasyno.WezJackpot() == BraxChampionsKasyno.JackpotStart + 10 && BraxChampionsKasyno.Jackpot == BraxChampionsKasyno.JackpotStart, "jackpot pays and restarts");
        Console.WriteLine(fails == 0 ? "AUTOMATY_SIM OK" : "AUTOMATY_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
