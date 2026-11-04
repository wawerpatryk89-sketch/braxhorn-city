using System.Collections.Generic;
using UnityEngine;

// BRAXHORN — ruchy sportowe Braxa z kodu (właściciel 1.11, 18:18: „nie zapomnij dodać ruchów do postaci”).
// Brax ma tylko klipy Idle/Walk/Run/Punch/Kick/Jump, więc ruchy są nakładane po animacji (LateUpdate) na kości szkieletu
// tak jak BraxPose: każda kość celowana (FromToRotation) w kierunek względem postaci — niezależnie od osi rigu Mixamo.
//   • piłka w rękach (kosz): obie dłonie po bokach piłki przed biodrami; kozioł: prawa dłoń „pompuje” za piłką, lekki skłon;
//   • rzut: piłka nad głowę, wyskok (biodra w górę), w chwili wypuszczenia ręce w górę i wyrzut z nadgarstka;
//   • siatkówka: odbicie dolne (złączone proste ręce w dół-przód, ugięte kolana) albo górne (ręce nad głową);
//   • tenis: forhend (zamach prawej ręki z rakietą + skręt tułowia) albo bekhend, gdy piłka z lewej;
//   • piłka nożna: strzał = klip Kick, podanie = krótszy Kick (klip od 0.2 s, przyspieszony do chwili kontaktu).
// Chwila kontaktu = chwila trafienia ciosu w BraxGymPlayer (klip Punch, 0.43 s) — wtedy piłka odlatuje (OnCios),
// a ruch jest tak rozłożony w czasie, żeby właśnie wtedy ręka / rakieta była przy piłce.
public sealed class BraxRuchySport : MonoBehaviour
{
    BraxGymPlayer p;
    Transform hips, spine, chest, lUp, lLeg, lFoot, rUp, rLeg, rFoot, lArm, lFore, lHand, rArm, rFore, rHand;
    bool szukane, bylo;
    BraxRuch ruch; float t0, tK, wTrzym, wKoz;
    GameObject rakieta;
    Vector3 ostPilka; float ostR = .12f;   // ostatnia pozycja trzymanej piłki — poza trzymania wygasa płynnie po rzucie
    Vector3 biodra, biodraLok, biodraZap;   // przesunięcie bioder w tej klatce (wyskok / przysiad) i co zapisano

    public static BraxRuchySport Get(BraxGymPlayer g)
    {
        if (!g) return null;
        var r = g.GetComponent<BraxRuchySport>();
        if (!r) { r = g.gameObject.AddComponent<BraxRuchySport>(); r.p = g; }
        return r;
    }

    Transform Find(string key)
    {
        if (!p || !p.visual) return null;
        Transform fallback = null;
        foreach (var t in p.visual.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
            if (n == key) return t;
            if (fallback == null && n.EndsWith(key)) fallback = t;
        }
        return fallback;
    }

    void Szukaj()
    {
        szukane = true;
        hips = Find("Hips"); spine = Find("Spine"); chest = Find("Spine2") ?? Find("Spine1");
        lUp = Find("LeftUpLeg"); lLeg = Find("LeftLeg"); lFoot = Find("LeftFoot");
        rUp = Find("RightUpLeg"); rLeg = Find("RightLeg"); rFoot = Find("RightFoot");
        lArm = Find("LeftArm"); lFore = Find("LeftForeArm"); lHand = Find("LeftHand");
        rArm = Find("RightArm"); rFore = Find("RightForeArm"); rHand = Find("RightHand");
        Debug.Log("BRAXHORN_SPORT kości: ręce=" + ((bool)lArm && (bool)lFore && (bool)lHand && (bool)rArm && (bool)rFore && (bool)rHand) + " nogi=" + ((bool)lUp && (bool)lLeg && (bool)rUp && (bool)rLeg) + " biodra=" + (bool)hips);
    }

    static void Aim(Transform bone, Transform child, Vector3 want, float w)
    {
        if (!bone || !child || w <= .001f || want.sqrMagnitude < 1e-6f) return;
        var cur = child.position - bone.position;
        if (cur.sqrMagnitude < 1e-8f) return;
        var q = Quaternion.FromToRotation(cur.normalized, want.normalized);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, q, Mathf.Clamp01(w)) * bone.rotation;
    }
    static float Gladko(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    void LateUpdate()
    {
        if (!p) p = GetComponent<BraxGymPlayer>();
        if (!p) return;
        if (!szukane) Szukaj();
        bool zajety = p.seat || p.vessel || p.autoGracza; // nie wykręcaj ramion Patryka ani rakiety w samochodzie
        if (!rArm || !rFore || !rHand || !lArm || !lFore || !lHand || zajety)
        {
            BraxSport.RzutFaza = 0f; ruch = BraxRuch.Brak;
            if (rakieta && rakieta.activeSelf) rakieta.SetActive(false);
            return;
        }
        float now = Time.time, dt = Time.deltaTime;
        // klip nie nadpisał bioder (brak krzywej pozycji) — zdejmij przesunięcie z poprzedniej klatki, żeby się nie sumowało
        if (hips && biodraLok != Vector3.zero && (hips.localPosition - biodraZap).sqrMagnitude < 1e-10f) hips.localPosition -= biodraLok;
        biodraLok = Vector3.zero; biodra = Vector3.zero;
        // ---- start ciosu: jaki ruch sportowy zagrać ----
        var clips = p.clips; AnimationState st = clips ? clips["Punch"] : null;
        bool cios = st != null && clips.IsPlaying("Punch") && st.time < .3f && st.weight > .01f;
        if (cios && !bylo)
        {
            ruch = BraxSport.Zamiar(p.transform.position, p.transform.forward);
            t0 = now; tK = now + Mathf.Max(0f, .43f - st.time) / Mathf.Max(.05f, st.speed);
            if (ruch == BraxRuch.Podanie)   // podanie = krótszy kop, kontakt w tej samej chwili co cios
            {
                var k = clips["Kick"];
                if (k != null)
                {
                    k.wrapMode = WrapMode.Once; clips.CrossFade("Kick", .06f, PlayMode.StopAll);
                    k.time = .2f; k.speed = (.6f - .2f) / Mathf.Max(.15f, tK - now);
                }
                ruch = BraxRuch.Brak;
            }
        }
        bylo = cios;
        float wR = 0f;
        if (ruch != BraxRuch.Brak)
        {
            if (now > tK + .45f) ruch = BraxRuch.Brak;
            else wR = Mathf.Min(Gladko((now - t0) / .1f), Gladko((tK + .45f - now) / .2f));
        }
        BraxSport.RzutFaza = ruch == BraxRuch.Rzut && now < tK + .02f ? Gladko((now - t0) / Mathf.Max(.05f, tK - t0 - .03f)) : 0f;

        Vector3 U = Vector3.up, F = Vector3.ProjectOnPlane(p.transform.forward, U).normalized, R = Vector3.Cross(U, F);
        // ---- piłka w rękach / kozioł ----
        var ball = BraxSport.PilkaWRekach;
        wTrzym = Mathf.MoveTowards(wTrzym, ball ? 1f : 0f, dt * 6f);
        wKoz = Mathf.MoveTowards(wKoz, ball && BraxSport.Kozluje ? 1f : 0f, dt * 5f);
        if (ball) { ostPilka = ball.position; ostR = ball.lossyScale.x * .5f; }
        if (wTrzym > .001f)
        {
            var bp = ostPilka; float rr = ostR;
            float wo = wTrzym * (1f - wKoz);
            if (wo > .001f)
            {
                var lT = bp - R * (rr + .03f); var rT = bp + R * (rr + .03f);
                Aim(lArm, lFore, (lT - lArm.position).normalized - U * .3f - R * .25f, wo);   // łokcie w dół i na zewnątrz
                Aim(lFore, lHand, lT - lFore.position, wo);
                Aim(rArm, rFore, (rT - rArm.position).normalized - U * .3f + R * .25f, wo);
                Aim(rFore, rHand, rT - rFore.position, wo);
            }
            float wk = wTrzym * wKoz;
            if (wk > .001f)
            {
                Aim(spine, chest, U * .95f + F * .22f, wk);                                   // lekki skłon
                var top = bp + U * (rr + .04f);
                Aim(rArm, rFore, (top - rArm.position).normalized - U * .15f + R * .3f, wk);  // prawa dłoń nad piłką — pompuje za nią
                Aim(rFore, rHand, top - rFore.position, wk);
                Aim(lArm, lFore, -U * .7f + F * .35f - R * .45f, wk);                         // lewa osłania
                Aim(lFore, lHand, F * .85f - R * .2f - U * .1f, wk);
            }
        }
        // ---- ruchy na cios ----
        if (wR > .001f)
            switch (ruch)
            {
                case BraxRuch.Rzut:
                    {
                        float lot = Mathf.Sin(Mathf.PI * Mathf.Clamp01((now - (tK - .2f)) / .55f));   // wyskok: szczyt tuż po wypuszczeniu
                        biodra += U * .3f * lot * wR;
                        Aim(spine, chest, U, wR * .5f);
                        if (now >= tK)   // po wypuszczeniu: ręce w górę-przód, wyrzut z nadgarstka
                        {
                            float f = Gladko((now - tK) / .12f) * wR;
                            Aim(rArm, rFore, U * .9f + F * .4f + R * .1f, f); Aim(rFore, rHand, U * .55f + F * .85f, f);
                            Aim(lArm, lFore, U * .85f + F * .35f - R * .2f, f); Aim(lFore, lHand, U * .7f + F * .6f, f);
                            rHand.rotation = Quaternion.AngleAxis(55f * f, R) * rHand.rotation;
                        }
                    }
                    break;
                case BraxRuch.OdbicieDolne:
                    {
                        biodra -= U * .12f * wR;
                        Aim(lUp, lLeg, -U * .8f + F * .5f - R * .1f, wR); Aim(rUp, rLeg, -U * .8f + F * .5f + R * .1f, wR);
                        Aim(lLeg, lFoot, -U * .95f - F * .3f, wR); Aim(rLeg, rFoot, -U * .95f - F * .3f, wR);
                        Aim(spine, chest, U * .85f + F * .4f, wR);
                        Aim(lArm, lFore, F * .8f - U * .55f + R * .12f, wR); Aim(rArm, rFore, F * .8f - U * .55f - R * .12f, wR);
                        Aim(lFore, lHand, F * .85f - U * .45f + R * .04f, wR); Aim(rFore, rHand, F * .85f - U * .45f - R * .04f, wR);
                    }
                    break;
                case BraxRuch.OdbicieGorne:
                    {
                        float lot = Mathf.Sin(Mathf.PI * Mathf.Clamp01((now - (tK - .15f)) / .45f));
                        biodra += U * .12f * lot * wR;
                        Aim(spine, chest, U * .95f - F * .12f, wR * .6f);
                        Aim(lArm, lFore, U * .9f + F * .3f - R * .22f, wR); Aim(rArm, rFore, U * .9f + F * .3f + R * .22f, wR);
                        Aim(lFore, lHand, U * .7f + F * .6f + R * .18f, wR); Aim(rFore, rHand, U * .7f + F * .6f - R * .18f, wR);
                    }
                    break;
                case BraxRuch.Forhend:
                case BraxRuch.Bekhend:
                    {
                        bool fh = ruch == BraxRuch.Forhend;
                        float a0 = fh ? -75f : 250f, ak = fh ? 35f : 145f, a1 = fh ? 150f : 30f, th;
                        if (now < tK) th = Mathf.Lerp(a0, ak, Mathf.Pow(Mathf.Clamp01((now - t0) / Mathf.Max(.05f, tK - t0)), 2.2f));   // zamach przyspiesza do kontaktu
                        else th = Mathf.Lerp(ak, a1, Gladko((now - tK) / .3f));
                        float tw = (ak - th) * .35f;                                                   // skręt tułowia
                        if (spine) spine.rotation = Quaternion.AngleAxis(tw * wR, U) * spine.rotation;
                        Vector3 Kier(float deg) => R * Mathf.Cos(deg * Mathf.Deg2Rad) + F * Mathf.Sin(deg * Mathf.Deg2Rad);
                        Aim(rArm, rFore, Kier(th) * .9f - U * .25f, wR);
                        Aim(rFore, rHand, Kier(th + (fh ? 15f : -15f)) * .95f - U * .1f, wR);
                        if (fh) Aim(lArm, lFore, F * .7f + R * .2f - U * .15f, wR * (now < tK ? 1f : .5f));   // lewa wskazuje piłkę
                        else Aim(lArm, lFore, -U * .6f + F * .4f - R * .3f, wR);
                    }
                    break;
            }
        if (hips && biodra.sqrMagnitude > 1e-8f) { var przed = hips.localPosition; hips.position += biodra; biodraZap = hips.localPosition; biodraLok = biodraZap - przed; }
        // ---- rakieta w prawej dłoni (tylko na korcie) ----
        if (BraxSport.Rakieta)
        {
            if (!rakieta) rakieta = Rakieta();
            if (!rakieta.activeSelf) rakieta.SetActive(true);
            var along = (rHand.position - rFore.position).normalized;
            var dir = (along * .75f + U * .25f + F * .15f).normalized;
            if (ruch == BraxRuch.Forhend || ruch == BraxRuch.Bekhend) dir = Vector3.Slerp(dir, (along * .9f + U * .1f).normalized, wR);
            var nrm = Vector3.ProjectOnPlane(F, dir); if (nrm.sqrMagnitude < 1e-4f) nrm = Vector3.ProjectOnPlane(U, dir);
            rakieta.transform.SetPositionAndRotation(rHand.position + along * .07f - dir * .06f, Quaternion.LookRotation(dir, nrm.normalized));
        }
        else if (rakieta && rakieta.activeSelf) rakieta.SetActive(false);
    }

    // Rakieta z prymitywów: rama (16 odcinków elipsy) + gardło, naciąg (spłaszczony walec z siatką strun), uchwyt.
    // Scalona w 3 siatki (3 wywołania rysowania), bez cieni rzucanych, bez kolizji. Oś Z = długość, Y = normalna naciągu.
    GameObject Rakieta()
    {
        var root = new GameObject("rakieta tenisowa"); root.transform.SetParent(transform, false);
        var tmpK = GameObject.CreatePrimitive(PrimitiveType.Cube); var tmpW = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var wzor = tmpK.GetComponent<Renderer>();
        Mesh kostka = tmpK.GetComponent<MeshFilter>().sharedMesh, walec = tmpW.GetComponent<MeshFilter>().sharedMesh;
        var rama = new List<CombineInstance>(); var chwyt = new List<CombineInstance>(); var naciag = new List<CombineInstance>();
        void Dodaj(List<CombineInstance> l, Mesh m, Vector3 pos, Quaternion q, Vector3 s) => l.Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, q, s) });
        const float ax = .125f, bz = .155f, zc = .455f;
        Vector3 E(float a) => new Vector3(Mathf.Cos(a) * ax, 0f, zc + Mathf.Sin(a) * bz);
        for (int k = 0; k < 16; k++)
        {
            Vector3 a = E(k * Mathf.PI / 8f), b = E((k + 1) * Mathf.PI / 8f);
            Dodaj(rama, kostka, (a + b) * .5f, Quaternion.LookRotation(b - a, Vector3.up), new Vector3(.016f, .022f, (b - a).magnitude * 1.1f));
        }
        foreach (float s in new[] { -1f, 1f })   // gardło
        {
            Vector3 a = new Vector3(s * .012f, 0f, .25f), b = new Vector3(s * .075f, 0f, zc - bz + .012f);
            Dodaj(rama, kostka, (a + b) * .5f, Quaternion.LookRotation(b - a, Vector3.up), new Vector3(.014f, .02f, (b - a).magnitude));
        }
        Dodaj(rama, kostka, new Vector3(0f, 0f, zc - bz + .01f), Quaternion.identity, new Vector3(.15f, .018f, .012f));   // mostek pod naciągiem
        Dodaj(naciag, walec, new Vector3(0f, 0f, zc), Quaternion.identity, new Vector3(ax * 2f * .96f, .002f, bz * 2f * .96f));
        Dodaj(chwyt, walec, new Vector3(0f, 0f, .13f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.034f, .135f, .03f));
        Dodaj(chwyt, walec, new Vector3(0f, 0f, -.005f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.04f, .008f, .036f));     // kołnierz na końcu uchwytu
        // naciąg: jasne struny na ciemnym tle (opaque — bez przezroczystości, wariant shadera zawsze w buildzie)
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "naciąg rakiety" };
        var px = new Color32[32 * 32];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) px[y * 32 + x] = (x % 4 == 0 || y % 4 == 0) ? new Color32(232, 232, 220, 255) : new Color32(38, 44, 40, 255);
        tex.SetPixels32(px); tex.Apply(true, true);
        var mNac = BraxSport.Mat(wzor, Color.white, tex, .2f); mNac.mainTextureScale = new Vector2(2f, 2f);
        Czesc(root.transform, "rama", rama, BraxSport.Mat(wzor, new Color(.07f, .08f, .09f), null, .65f));
        Czesc(root.transform, "naciąg", naciag, mNac);
        Czesc(root.transform, "uchwyt", chwyt, BraxSport.Mat(wzor, new Color(.88f, .88f, .85f), null, .2f));
        Destroy(tmpK); Destroy(tmpW);
        return root;
    }

    static void Czesc(Transform root, string n, List<CombineInstance> l, Material m)
    {
        var go = new GameObject(n); go.transform.SetParent(root, false);
        var mesh = new Mesh { name = "rakieta: " + n }; mesh.CombineMeshes(l.ToArray(), true, true);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
