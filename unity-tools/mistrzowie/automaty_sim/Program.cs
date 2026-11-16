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
    // video poker, keno, the wheel machine: exact returns where they can be counted, the poker's AUTO HOLD over many hands,
    // then the real component plays each machine
    static void Gry(int spins, BraxGymPlayer gracz, BindingFlags F)
    {
        var P = typeof(BraxChampionsAutomatyGry.PokerGra);
        int[] H(params string[] k)
        {
            var r = new int[5];
            for (int i = 0; i < 5; i++) { string t = k[i]; int rank = "23456789TJQKA".IndexOf(t[0]), suit = "shdc".IndexOf(t[1]); r[i] = suit * 13 + rank; }
            return r;
        }
        void U(string what, int want, params string[] k) => Check(BraxChampionsAutomatyGry.PokerGra.Ocen(H(k)) == want, "poker " + what);
        U("royal", 9, "Ts", "Js", "Qs", "Ks", "As"); U("straight flush", 8, "9h", "Th", "Jh", "Qh", "Kh"); U("wheel straight flush", 8, "Ad", "2d", "3d", "4d", "5d");
        U("quads", 7, "7c", "7d", "7h", "7s", "2c"); U("full", 6, "Kc", "Kd", "Kh", "3s", "3c"); U("flush", 5, "2c", "9c", "Jc", "4c", "Kc");
        U("straight A high", 4, "Tc", "Jd", "Qh", "Ks", "Ac"); U("straight wheel", 4, "Ac", "2d", "3h", "4s", "5c"); U("no straight K-A-2", 0, "Qc", "Kd", "Ah", "2s", "3c");
        U("trips", 3, "5c", "5d", "5h", "Ks", "2c"); U("two pair", 2, "5c", "5d", "9h", "9s", "Kc"); U("jacks", 1, "Jc", "Jd", "2h", "7s", "9c");
        U("tens are nothing", 0, "Tc", "Td", "2h", "7s", "9c");
        int Hold(params string[] k) => BraxChampionsAutomatyGry.PokerGra.Trzymaj(H(k));
        Check(Hold("Ts", "Js", "Qs", "Ks", "2h") == 15, "hold four to a royal");
        Check(Hold("Ts", "Js", "Qs", "Ks", "9s") == 31, "a straight flush stays");
        Check(Hold("Jc", "Jd", "2h", "7s", "9c") == 3, "hold the high pair");
        Check(Hold("4c", "4d", "As", "Kh", "9c") == 3, "a low pair over two high cards");
        Check(Hold("2c", "5c", "8c", "Jc", "Kd") == 15, "four to a flush");
        Check(Hold("5c", "6d", "7h", "8s", "Kc") == 15, "four to an outside straight");
        Check(Hold("Ac", "Kd", "Qh", "3s", "8c") == 6, "two unsuited high: the lowest two");
        Check(Hold("2c", "4d", "6h", "8s", "Tc") == 0, "draw five");
        // the poker over many hands with the AUTO HOLD
        var los = new System.Random(7); var talia = new int[52]; var reka = new int[5]; long pw = 0; int nh = Math.Max(200000, spins * 200); var ile = new long[10];
        for (int h = 0; h < nh; h++)
        {
            BraxChampionsAutomatyGry.PokerGra.Tasuj(los, talia); Array.Copy(talia, reka, 5);
            int t = BraxChampionsAutomatyGry.PokerGra.Trzymaj(reka), w = 5;
            for (int i = 0; i < 5; i++) if ((t >> i & 1) == 0) reka[i] = talia[w++];
            int u = BraxChampionsAutomatyGry.PokerGra.Ocen(reka); ile[u]++; pw += BraxChampionsAutomatyGry.PokerGra.Wyplata[u];
        }
        double rtpP = (double)pw / nh;
        Console.WriteLine("VIDEO POKER 9/6 AUTO HOLD over {0} hands: RTP {1:F4} (best play 0.9954, the simple strategy 0.9946); royals {2}, a win on {3:F1} % of hands",
            nh, rtpP, ile[9], 100.0 * (nh - ile[0]) / nh);
        Check(rtpP > .975 && rtpP < 1.01, "video poker RTP " + rtpP);
        // keno exactly (20 of 80, six picked)
        double C(int n, int k) { double r = 1; for (int i = 1; i <= k; i++) r = r * (n - k + i) / i; return r; }
        double rtpK = 0; for (int c = 0; c <= 6; c++) rtpK += BraxChampionsAutomatyGry.KenoGra.Wyplata[c] * C(6, c) * C(74, 20 - c) / C(80, 20);
        Console.WriteLine("KENO 6 OF 80 RTP {0:F4} exactly", rtpK);
        Check(Math.Abs(rtpK - .9202) < .0005, "keno RTP " + rtpK);
        // the wheel machine exactly: its line pays over the 8 000 combinations + KOŁO on the third reel x the wheel's average
        long lw = 0, nk = 0;
        for (int a = 0; a < 20; a++) for (int b = 0; b < 20; b++) for (int c = 0; c < 20; c++)
        {
            int x = BraxChampionsAutomatyGry.Pasek(4, 0)[a], y = BraxChampionsAutomatyGry.Pasek(4, 1)[b], z = BraxChampionsAutomatyGry.Pasek(4, 2)[c];
            lw += BraxChampionsAutomatyGry.KoloGra.Linia(x, y, z); if (z == BraxChampionsAutomatyGry.KoloGra.Symbol) nk++;
        }
        double sred = 0; foreach (int v in BraxChampionsAutomatyGry.KoloGra.Segmenty) sred += v / 10.0; sred /= BraxChampionsAutomatyGry.KoloGra.Segmenty.Length;
        double rtpW = lw / 8000.0 + nk / 8000.0 * sred;
        Console.WriteLine("KOŁO MISTRZÓW RTP {0:F4} = line {1:F4} + the wheel 1 in {2} spins x {3:F2}", rtpW, lw / 8000.0, 8000 / Math.Max(1, nk), sred);
        Check(rtpW > .90 && rtpW < .98, "wheel machine RTP " + rtpW);

        // ---- the machines played by the real component
        PlayerPrefs.DeleteAll();
        var host = new GameObject("kasyno gry"); var gra = host.AddComponent<BraxChampionsAutomaty>(); gra.stawka = 10;
        var mat = new Material { name = "ekran" };
        BraxChampionsAutomaty.Maszyna M(int g, Vector3 at)
        {
            var kor = new GameObject("g" + g).transform; kor.localPosition = at;
            var m = new BraxChampionsAutomaty.Maszyna { korpus = kor, ekran = BraxChampionsAutomatyGry.Ekranowa(g), gra = g };
            var lic = new GameObject("licznik"); lic.transform.SetParent(kor, false); var r = lic.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; m.licznik = r;
            if (g == 4)
            {
                m.bebny = new Transform[3]; for (int i = 0; i < 3; i++) { var o = new GameObject("b"); o.transform.SetParent(kor, false); m.bebny[i] = o.transform; }
                var d = new GameObject("d"); d.transform.SetParent(kor, false); m.dzwignia = d.transform;
                var k = new GameObject("kolo"); k.transform.SetParent(kor, false); m.kolo = k.transform;
                var w = new GameObject("wskaznik"); w.transform.SetParent(kor, false); m.wskaznik = w.transform;
            }
            else { var o = new GameObject("warstwa"); o.transform.SetParent(kor, false); m.warstwa = o.AddComponent<MeshFilter>(); }
            return m;
        }
        gra.maszyny = new[] { M(2, new Vector3(0, 0, 0)), M(3, new Vector3(10, 0, 0)), M(4, new Vector3(20, 0, 0)) };
        var T = typeof(BraxChampionsAutomaty); T.GetMethod("Start", F).Invoke(gra, null);
        var update = T.GetMethod("Update", F); var stanF = T.GetField("stan", F);
        object St(int i) => ((Array)stanF.GetValue(gra)).GetValue(i);
        T0 Pole<T0>(object o, string n) => (T0)o.GetType().GetField(n).GetValue(o);
        int Klatki(object s, string flag)
        {
            int fr = 0; while (Pole<bool>(s, flag) && fr < 4000) { Time.time += Time.deltaTime; update.Invoke(gra, null); fr++; }
            return fr;
        }
        int games = Math.Max(300, spins / 5);
        // video poker: deal, the AUTO HOLD, draw, the hand pays
        {
            var m = gra.maszyny[0]; gracz.transform.localPosition = m.korpus.TransformPoint(new Vector3(0, 0, 1.0f)); long st = 0, pd = 0;
            for (int g = 0; g < games; g++)
            {
                if (BraxChampionsKasyno.Kredyt < 10) BraxChampionsKasyno.Wplac(1000);
                int kr = BraxChampionsKasyno.Kredyt; var s = St(0);
                Check(gra.Kontekst(gracz.transform.position).StartsWith("VIDEO POKER · ROZDAJ"), "poker context deal");
                Check(gra.Wykonaj(gracz.transform.position) && BraxChampionsKasyno.Kredyt == kr - 10, "poker stake taken");
                int f1 = Klatki(s, "kreci"); Check(f1 > 20 && f1 < 120, "deal turns the cards over " + f1);
                var karty = (int[])Pole<int[]>(s, "karty").Clone();
                Check(Pole<int>(s, "faza") == 1 && Pole<int>(s, "trzymaj") == BraxChampionsAutomatyGry.PokerGra.Trzymaj(karty), "AUTO HOLD marked");
                Check(gra.Kontekst(gracz.transform.position).StartsWith("WYMIEŃ KARTY"), "poker context draw");
                int hold = Pole<int>(s, "trzymaj");
                gra.Wykonaj(gracz.transform.position); Klatki(s, "kreci");
                var po = Pole<int[]>(s, "karty");
                for (int i = 0; i < 5; i++) if ((hold >> i & 1) != 0) Check(po[i] == karty[i], "held card stays");
                Check(new System.Collections.Generic.HashSet<int>(po).Count == 5, "five different cards");
                int u = BraxChampionsAutomatyGry.PokerGra.Ocen(po), w = Pole<int>(s, "wygrana");
                Check(w == BraxChampionsAutomatyGry.PokerGra.Wyplata[u] * 10, "poker pays " + w + " for " + u);
                Check(BraxChampionsKasyno.Kredyt == kr - 10 + w, "poker win credited");
                Check(m.warstwa.sharedMesh.vertices.Length >= 20, "poker screen layer");
                st += 10; pd += w;
            }
            Console.WriteLine("VIDEO POKER played {0} hands: staked {1}, paid {2} (RTP {3:F3})", games, st, pd, (double)pd / st);
        }
        // keno: six picked, twenty balls one by one, the catches pay
        {
            var m = gra.maszyny[1]; gracz.transform.localPosition = m.korpus.TransformPoint(new Vector3(0, 0, 1.0f)); long st = 0, pd = 0;
            for (int g = 0; g < games; g++)
            {
                if (BraxChampionsKasyno.Kredyt < 10) BraxChampionsKasyno.Wplac(1000);
                int kr = BraxChampionsKasyno.Kredyt; var s = St(1);
                Check(gra.Kontekst(gracz.transform.position).StartsWith("KENO 6 Z 80"), "keno context");
                gra.Wykonaj(gracz.transform.position); Check(BraxChampionsKasyno.Kredyt == kr - 10, "keno stake taken");
                int fr = Klatki(s, "kreci"); Check(fr > 150 && fr < 300, "keno draw length " + fr / 60f + " s");
                var typy = Pole<int[]>(s, "typy"); var kule = Pole<int[]>(s, "kule");
                Check(new System.Collections.Generic.HashSet<int>(typy).Count == 6 && new System.Collections.Generic.HashSet<int>(kule).Count == 20, "keno numbers distinct");
                foreach (int k in kule) Check(k >= 1 && k <= 80, "ball 1..80");
                int t = BraxChampionsAutomatyGry.KenoGra.Trafione(typy, kule, 20), w = Pole<int>(s, "wygrana");
                Check(w == BraxChampionsAutomatyGry.KenoGra.Wyplata[t] * 10 && BraxChampionsKasyno.Kredyt == kr - 10 + w, "keno pays " + w + " for " + t);
                Check(m.warstwa.sharedMesh.vertices.Length == 4 * (20 + 6 - t + 1 + 2), "keno layer " + m.warstwa.sharedMesh.vertices.Length / 4 + " quads");
                st += 10; pd += w;
            }
            Console.WriteLine("KENO played {0} games: staked {1}, paid {2} (RTP {3:F3})", games, st, pd, (double)pd / st);
        }
        // the wheel machine: the drums, KOŁO on the third reel spins the wheel to the drawn segment
        {
            var m = gra.maszyny[2]; gracz.transform.localPosition = m.korpus.TransformPoint(new Vector3(0, 0, 1.0f)); long st = 0, pd = 0; int kola = 0;
            for (int g = 0; g < games * 3; g++)
            {
                if (BraxChampionsKasyno.Kredyt < 10) BraxChampionsKasyno.Wplac(1000);
                int kr = BraxChampionsKasyno.Kredyt; long j0 = BraxChampionsKasyno.Jackpot; var s = St(2);
                Check(gra.Kontekst(gracz.transform.position).StartsWith("POCIĄGNIJ"), "wheel machine context");
                gra.Wykonaj(gracz.transform.position);
                Check(BraxChampionsKasyno.Jackpot == j0, "the wheel machine is not on the linked jackpot");
                Klatki(s, "kreci");
                var poz = Pole<float[]>(s, "poz"); int a = BraxChampionsAutomatyGry.Pasek(4, 0)[(int)Math.Round(poz[0]) % 20], b = BraxChampionsAutomatyGry.Pasek(4, 1)[(int)Math.Round(poz[1]) % 20],
                    c = BraxChampionsAutomatyGry.Pasek(4, 2)[(int)Math.Round(poz[2]) % 20];
                int linia = BraxChampionsAutomatyGry.KoloGra.Linia(a, b, c) * 10, w = linia;
                if (c == BraxChampionsAutomatyGry.KoloGra.Symbol)
                {
                    Check(Pole<bool>(s, "koloKreci"), "KOŁO starts the wheel");
                    Check(gra.Kontekst(gracz.transform.position).StartsWith("KOŁO MISTRZÓW"), "wheel context");
                    int fr = Klatki(s, "koloKreci"); Check(fr > 380 && fr < 460, "the wheel runs " + fr / 60f + " s");
                    int seg = Pole<int>(s, "koloSeg"); float kat = Pole<float>(s, "koloKat"); w += BraxChampionsAutomatyGry.KoloGra.Wygrana(seg, 10);
                    Check(Math.Abs(Mathf.DeltaAngle(kat, BraxChampionsAutomatyGry.KoloGra.KatSegmentu(seg))) < .01f, "segment " + seg + " under the pointer (" + kat + ")");
                    var q = m.kolo.localRotation; float rk = (float)(2 * Math.Atan2(q.z, q.w) * 180 / Math.PI);
                    Check(Math.Abs(Mathf.DeltaAngle(rk, kat)) < .05f, "the wheel turned to its angle");
                    Check(m.wskaznik.localRotation.w > .9999f, "the pointer rests");
                    kola++;
                }
                Check(Pole<int>(s, "wygrana") == w && BraxChampionsKasyno.Kredyt == kr - 10 + w, "wheel machine pays " + w);
                st += 10; pd += w;
            }
            Console.WriteLine("KOŁO MISTRZÓW played {0} spins: staked {1}, paid {2} (RTP {3:F3}), the wheel {4} times", games * 3, st, pd, (double)pd / st, kola);
            Check(kola > 0, "the wheel was spun");
        }
    }

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
        // ---- 2b. the other games (owner 16.11 20:28 "różne gry w automatach"): their tables and the machines played
        Gry(spins, gracz, F);
        // ---- 2c. the service at the entrance: the club card's welcome once, the coat check's tag, the cashier
        {
            PlayerPrefs.DeleteAll();
            var oh = new GameObject("obsluga"); var ob = oh.AddComponent<BraxChampionsKasynoObsluga>(); ob.nazwa = "KASYNO";
            Transform Mi(Vector3 at) { var t = new GameObject("m").transform; t.localPosition = at; return t; }
            var kurtka = new GameObject("kurtka").transform;
            ob.punkty = new[] { new BraxChampionsKasynoObsluga.Punkt { rodzaj = 0, miejsce = Mi(new Vector3(100, 0, 0)) },
                new BraxChampionsKasynoObsluga.Punkt { rodzaj = 1, miejsce = Mi(new Vector3(110, 0, 0)), kurtka = kurtka },
                new BraxChampionsKasynoObsluga.Punkt { rodzaj = 2, miejsce = Mi(new Vector3(120, 0, 0)) } };
            typeof(BraxChampionsKasynoObsluga).GetMethod("Start", F).Invoke(ob, null);
            Check(!kurtka.gameObject.activeSelf, "no jacket hangs before");
            var at = new Vector3(100.3f, 0, 0); int k0 = BraxChampionsKasyno.Kredyt;
            Check(ob.Kontekst(at).StartsWith("RECEPCJA · ZAŁÓŻ KARTĘ"), "reception context " + ob.Kontekst(at));
            ob.Wykonaj(at); Check(BraxChampionsKasyno.Kredyt == k0 + 1000 && BraxChampionsKasynoObsluga.Klub, "the welcome free play");
            ob.Wykonaj(at); Check(BraxChampionsKasyno.Kredyt == k0 + 1000, "the welcome only once");
            at = new Vector3(110.4f, 0, 0);
            Check(ob.Kontekst(at) == "SZATNIA · ODDAJ KURTKĘ", "cloakroom context");
            ob.Wykonaj(at); int n = BraxChampionsKasynoObsluga.Numerek;
            Check(n >= 1 && n <= 250 && kurtka.gameObject.activeSelf && ob.Kontekst(at).EndsWith("NUMEREK " + n), "the tag " + n + ", the jacket hangs");
            ob.Wykonaj(at); Check(BraxChampionsKasynoObsluga.Numerek == 0 && !kurtka.gameObject.activeSelf, "the jacket back for the tag");
            at = new Vector3(120.2f, 0, 0); int k1 = BraxChampionsKasyno.Kredyt;
            ob.Wykonaj(at); Check(BraxChampionsKasyno.Kredyt == k1 + 1000, "the cashier changes 1000");
            Check(ob.Kontekst(new Vector3(105f, 0, 0)) == "", "nothing between the counters");
            Console.WriteLine("OBSLUGA reception (welcome once) / cloakroom (tag " + n + ") / cashier: OK");
        }
        // ---- 3. the roulette: every outside bet, the ball ends in the drawn number's pocket, even money pays 1:1, zero takes it
        PlayerPrefs.DeleteAll();
        var rh = new GameObject("ruletka"); var rul = rh.AddComponent<BraxChampionsRuletka>(); rul.stawka = 10;
        var rst = new GameObject("stol").transform; var rot = new GameObject("rotor").transform; rot.SetParent(rst, false);
        var kul = new GameObject("kulka").transform; kul.SetParent(rst, false);
        var kolo = new Vector3(.88f, .78f, 0f); rot.localPosition = kolo;
        var zet = new Transform[6]; var polaX = new float[6];
        for (int e = 0; e < 6; e++) { zet[e] = new GameObject("z").transform; zet[e].SetParent(rst, false); polaX[e] = .13f - .23f * e; }
        var wys = new GameObject("wyswietlacz").AddComponent<MeshFilter>();
        rul.stoly = new[] { new BraxChampionsRuletka.Stol { stol = rst, rotor = rot, kulka = kul, kolo = kolo, zetony = zet, polaX = polaX, wyswietlacz = wys } };
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
            // the display: the big disc shows the number drawn (its atlas cell)
            BraxChampionsRuletka.Wyswietlacz.KrazekPx(wynik, out int ax, out int ay);
            var duv = wys.sharedMesh.uv[0]; Check(Math.Abs(duv.x - (ax + .5f) / 1024f) < 1e-4 && Math.Abs(duv.y - (ay + .5f) / 1024f) < 1e-4, "display shows " + wynik);
            Check(wys.sharedMesh.vertices.Length >= 4 * (1 + 12 + 8), "display layer complete");
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
