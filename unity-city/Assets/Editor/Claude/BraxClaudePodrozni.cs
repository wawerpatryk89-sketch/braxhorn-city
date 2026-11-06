using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PODRÓŻNI PARKINGÓW: mieszkaniec podchodzi do zaparkowanego auta, wsiada, siedzi, wysiada i wraca (BraxPodroznyAuta).
// Build 45 (log #44: „Nie znaleziono bezpiecznego miejsca do wsiadania NPC na parkingach”, 0 podróżnych). Przyczyny:
//  1) filtr prześwitu kabiny ≥ 0.92 m × K — przy prześwitach 0.76–1.00 przechodziły tylko Żuk i van;
//  2) czubek głowy liczony z BakeMesh + TransformPoint (skala osoby liczona DWA razy, ×~1.26) — osoba „nie mieściła się”
//     w żadnej kabinie i była usuwana; 3) tylko drzwi kierowcy (lewe) — przy zatokach wzdłuż jezdni lewe drzwi są od
//     jezdni (NaJezdni), a na parkingu prostopadłym obok stoi inne auto.
// Teraz: bez filtra prześwitu — poza w kabinie z BraxClaudeKabiny (zagłębienie, odchylenie, głowa; skala 1.0); drzwi
// kierowcy albo pasażera (ta strona, która jest wolna i nie na jezdni); punkt „domu” 2–4.5 m od drzwi w 5 kierunkach.
// Błąd (0 miejsc) = DO POPRAWKI w logu z liczbami odrzuceń — nie przerywa budowy.
public static partial class BraxClaudeOsiedle
{
    const int PodrozniMax = 8;

    static void PodrozniParkingow(System.Random rnd)
    {
        int made = 0, nAut = 0, odrNaJezdni = 0, odrDrzwiZajete = 0, odrBrakDojscia = 0, odrBrakFotela = 0, odrKlipy = 0, odrKabina = 0;
        List<BoxCollider> zaparkowane = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
            .Where(b => b.name.StartsWith("Auto zaparkowane") && !b.GetComponent<BraxAutoGracza>())
            .OrderBy(b => b.transform.position.x).ThenBy(b => b.transform.position.z).ToList();
        foreach (var box in zaparkowane)
        {
            if (made >= PodrozniMax) break;
            nAut++;
            var fotele = box.GetComponentsInChildren<BraxFotelAuta>();
            Transform seat = null; Vector3 door = default, home = default; bool found = false, drzwiOk = false;
            foreach (int strona in new[] { -1, 1 })   // -1: drzwi kierowcy (lewe), +1: pasażera
            {
                var f = fotele.FirstOrDefault(q => q.name == (strona < 0 ? FotelKierowcy : FotelPasazera));
                if (!f) continue;
                float zLok = box.transform.InverseTransformPoint(f.transform.position).z;
                var d = box.transform.TransformPoint(new Vector3(box.center.x + strona * (box.size.x * .5f + .65f), 0, zLok));
                if (Physics.Raycast(d + Vector3.up * 1.5f, Vector3.down, out var fl, 3f, ~(1 << 29), QueryTriggerInteraction.Ignore)) d.y = fl.point.y + .03f;
                if (NaJezdni(d)) { odrNaJezdni++; continue; }
                if (!Wolne(d, .3f)) { odrDrzwiZajete++; continue; }
                drzwiOk = true;
                var bok = box.transform.right * strona;
                foreach (var kier in new[] { bok, (bok + box.transform.forward).normalized, (bok - box.transform.forward).normalized, box.transform.forward, -box.transform.forward })
                    foreach (float odl in new[] { 3f, 2f, 4.5f })
                    {
                        var c = d + kier * odl;
                        if (!Wolne(c, .3f) || PrzezJezdnie(d, c) || !WidacProsto(d, c)) continue;
                        if (!Physics.Raycast(c + Vector3.up, Vector3.down, out var floor, 2, ~(1 << 29), QueryTriggerInteraction.Ignore)) continue;
                        if (Mathf.Abs(floor.point.y - d.y) > .2f) continue;
                        home = new Vector3(c.x, floor.point.y + .03f, c.z); found = true; break;
                    }
                if (found) { seat = f.transform; door = d; break; }
            }
            if (!fotele.Any()) { odrBrakFotela++; continue; }
            if (!found) { if (drzwiOk) odrBrakDojscia++; continue; }

            var o = NowaOsoba(RolaKierowca, box.transform, "Mieszkaniec przy samochodzie", home, box.transform.eulerAngles.y, rnd);
            if (o == null) continue;
            var idle = Klip(o, "idle_neutral_01"); var walk = Klip(o, "walk_neutral_01"); var sit = Klip(o, "sit_table_breathe_01");
            if (idle == null || walk == null || sit == null) { UnityEngine.Object.DestroyImmediate(o.root); rbLudzie--; odrKlipy++; continue; }
            DodajKlipy(o, new[] { idle, walk, sit });
            var standing = o.model.transform.localPosition;
            // na siedzeniu: biodra na fotelu (jak kierowcy aut w ruchu), potem dobór pozy w kabinie (skala 1.0)
            o.root.transform.SetPositionAndRotation(seat.position, seat.rotation); Pozuj(o, sit, 0);
            var hips = o.model.GetComponentsInChildren<Transform>().First(t => t.name == "Bip01 Pelvis");
            o.model.transform.position += seat.position + Vector3.up * .08f * K - hips.position;
            var m = o.root.AddComponent<BraxMieszkaniec>(); m.anim = o.anim; m.klipy = new[] { sit }; m.rola = "przechodzień";
            m.wzrost = o.wzrost; m.glowa = o.glowa; m.szyja = o.szyja; m.kregoslup = o.kregoslup; m.ton = o.p.kobieta ? 1.35f : .95f;
            var korzen = o.root.transform.position;
            // Nie pozostawiaj podróżnego w kabinie, jeżeli skóra głowy przecina dach.
            // Usuwamy tylko tę nieudaną instancję, zachowujemy wszystkie oryginalne modele.
            if (!DopasujKabine(m, seat))
            {
                UnityEngine.Object.DestroyImmediate(o.root);
                rbLudzie--;
                odrKabina++;
                continue;
            }
            var sitting = o.model.transform.localPosition + o.root.transform.InverseTransformVector(o.root.transform.position - korzen);
            float odch = m.odchylenie, poch = m.pochylenieGlowy; var kreg = m.kregoslupDol;
            m.odchylenie = 0f; m.pochylenieGlowy = 0f; m.kregoslupDol = null; m.klipy = new[] { idle };
            o.root.transform.SetPositionAndRotation(home, box.transform.rotation); o.model.transform.localPosition = standing; Pozuj(o, idle, 0);
            Zamknij(o, true);
            var travel = o.root.AddComponent<BraxPodroznyAuta>(); travel.osoba = m; travel.samochod = box; travel.fotel = seat;
            travel.dom = home; travel.drzwi = door; travel.siedzenieModel = sitting; travel.chod = walk; travel.spokoj = idle; travel.siedzi = sit;
            travel.odchylenie = odch; travel.pochylenieGlowy = poch; travel.kregoslupDol = kreg;
            made++; Physics.SyncTransforms();
        }
        string log = "BRAXHORN_PODROZNI miejsca=" + made + " (maks. " + PodrozniMax + ") aut_sprawdzonych=" + nAut + " z " + zaparkowane.Count
            + " odrzucone: drzwi_na_jezdni=" + odrNaJezdni + " drzwi_zajęte=" + odrDrzwiZajete + " brak_dojścia=" + odrBrakDojscia
            + " brak_fotela=" + odrBrakFotela + " brak_klipów=" + odrKlipy
            + " zły_prześwit_dachu=" + odrKabina;
        Debug.Log(log);
        rep.notes.Add(log + "; podejście, wsiadanie, siedzenie, wysiadanie, powrót. Prywatne auto Patryka pominięte.");
        if (made == 0) Debug.LogError("BRAXHORN_PODROZNI DO POPRAWKI: 0 miejsc do wsiadania NPC — " + log);
    }
}
