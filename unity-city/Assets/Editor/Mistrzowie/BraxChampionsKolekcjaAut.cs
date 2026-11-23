using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Owner 17.11 00:20 ("podstaw z 3 auta z naszej kolekcji i puszczaj"): three more cars of the estate collection - the same
// GLBs as the estate's car salon (public/assets/cars-src, copied unchanged by prepare_kolekcja_aut.py) - stand driveable on
// the A1 east car park with the Revuelto, one free bay between each (the left door side of every car stays clear):
// Porsche 918 Spyder, Audi R8, Dodge Challenger. Each is a BraxChampionsCar like the Revuelto (joystick, handbrake, V8,
// AKCJA "WSIĄDŹ"), its real length x K. The front is +Z in every source (headlights, steering wheel on the left - checked
// on the GLBs). The wheels roll on one pivot per axle at the axle's own centre, the round parts of the wheels (tyre, rim,
// disc) under it: the sources' wheel nodes are not centred on their wheel and the R8 has one mesh per axle.
public static class BraxChampionsKolekcjaAut
{
    public const string Folder = "Assets/Source/Mistrzowie/Vehicles/", Prefix = "A1 | KOLEKCJA | ";
    public struct Auto { public string plik, nazwa; public float dlugosc, x; }
    // real lengths: the estate's own table (BraxClaudeOsiedleAutaModel.DlugoscRealna); x = the bay centres of the east row
    // (stall lines 31.45 + i x 3.05 m, the Revuelto at 645.175): 639.075 | free | REVUELTO | free | 651.275 | free | 657.375
    public static readonly Auto[] Auta =
    {
        new Auto { plik = "car_918", nazwa = "PORSCHE 918 SPYDER", dlugosc = 4.64f, x = 651.275f },
        new Auto { plik = "car_r8", nazwa = "AUDI R8", dlugosc = 4.43f, x = 657.375f },
        new Auto { plik = "car_dc", nazwa = "DODGE CHALLENGER", dlugosc = 5.0f, x = 639.075f },
    };
    const float Z = 712f;
    static float K => BraxClaudeOsiedle.K;
    static readonly Regex Kolo = new Regex(@"wheel|tire|tyre|(^|[^t])rim", RegexOptions.IgnoreCase);
    static readonly Regex NieKolo = new Regex(@"steer|caliper|trim|brake|emissive", RegexOptions.IgnoreCase);

    public static void Generate(Transform parent)
    {
        int ok = 0; var opis = new List<string>();
        foreach (var a in Auta)
        {
            GameObject root = null;
            try { root = Zbuduj(parent, a, out string o); ok++; opis.Add(o); }
            catch (Exception e)
            {
                if (root) UnityEngine.Object.DestroyImmediate(root);
                BraxBuildQuality.Warn("DO POPRAWKI KOLEKCJA_AUT " + a.nazwa + " (" + a.plik + ".glb): " + e.Message);
            }
        }
        Debug.Log("KOLEKCJA_AUT cars=" + ok + "/" + Auta.Length + " A1 east car park beside the Revuelto | " + string.Join(" | ", opis));
    }

    static GameObject Zbuduj(Transform parent, Auto a, out string opis)
    {
        string path = Folder + a.plik + ".glb";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if (!source) throw new InvalidOperationException("the collection GLB is not imported: " + path);
        var root = new GameObject(Prefix + a.nazwa + " | WORKING joystick vehicle");
        try
        {
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(a.x, .16f, Z) * K;
            root.transform.rotation = Quaternion.identity;
            var copy = UnityEngine.Object.Instantiate(source, root.transform, false);
            copy.name = "ORIGINAL " + a.plik + " GLB from the estate collection, unchanged mesh + per-car PBR instance";
            copy.transform.localPosition = Vector3.zero; copy.transform.localRotation = Quaternion.identity;
            BraxChampionsCarBuilder.ApplyCarPbr(copy);
            Physics.SyncTransforms();
            BraxChampionsCarBuilder.PlaceWheelsDown(copy, root.transform);
            var bounds = BraxChampionsCarBuilder.BoundsLocal(root.transform);
            if (bounds.size.z <= .01f || bounds.size.x <= .01f) throw new InvalidOperationException("invalid length/width");
            copy.transform.localScale *= a.dlugosc * K / bounds.size.z;          // real length x K, like the Revuelto
            Physics.SyncTransforms();
            bounds = BraxChampionsCarBuilder.BoundsLocal(root.transform);
            float groundLocalY = (.239f - .16f) * K;                             // the car park's asphalt top (as the Revuelto)
            copy.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y - groundLocalY, bounds.center.z);
            Physics.SyncTransforms();
            bounds = BraxChampionsCarBuilder.BoundsLocal(root.transform);
            // 23.11 (owner's photo: the 918 and the Challenger stood on their tails, ~25 m tall): a car goes into the game only
            // lying on its wheels at its real size - longer than wide and than high, 3.8-5.6 m long (x K)
            if (!(bounds.size.z >= 3.8f * K && bounds.size.z <= 5.6f * K && bounds.size.y < bounds.size.z * .45f && bounds.size.x < bounds.size.z * .6f))
                throw new InvalidOperationException("KOLEKCJA_AUT_NIE_STOI_NA_KOLACH " + a.nazwa + " length=" + bounds.size.z.ToString("F2") + " width=" + bounds.size.x.ToString("F2") + " height=" + bounds.size.y.ToString("F2"));

            var kola = Kola(copy, root.transform, bounds, out float rozstaw, out string kolaOpis);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            var box = root.AddComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size;
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.Interpolate;

            // the driver on the LEFT, as the Revuelto (every source has its steering wheel there)
            var seat = new GameObject("Original cabin | LEFT driver seat").transform; seat.SetParent(root.transform, false);
            seat.localPosition = new Vector3(-bounds.extents.x * .39f, bounds.min.y + Mathf.Clamp(bounds.size.y * .28f, .26f, .85f), bounds.extents.z * .14f);
            var enter = new GameObject("Left driver-door approach").transform; enter.SetParent(root.transform, false);
            enter.localPosition = new Vector3(bounds.min.x - .78f * K, bounds.min.y + .04f, seat.localPosition.z - .15f * K);

            var car = root.AddComponent<BraxChampionsCar>();
            car.bodyCollider = box; car.driverSeat = seat; car.doorApproach = enter;
            car.doorHinge = null; car.doorAxis = Vector3.left;                     // no separate door in these sources: none invented
            car.wheelbase = rozstaw > .5f * K ? rozstaw : 2.7f * K;               // measured between the axle pivots
            car.worldScale = K;
            car.maxSpeed = 200f / 3.6f * K; car.reverseMaxSpeed = 30f / 3.6f * K;   // the owner's 200 km/h, as the Revuelto
            car.acceleration = 8.5f * K; car.braking = 11f * K; car.coastDecel = 4.5f * K;
            car.interactionRadius = 3.8f * K;
            var audioAnchor = new GameObject("BRAXHORN engine audio 3D anchor").transform; audioAnchor.SetParent(root.transform, false);
            audioAnchor.localPosition = new Vector3(0f, bounds.min.y + bounds.size.y * .46f, bounds.max.z - bounds.size.z * .18f);
            car.engineAudioAnchor = audioAnchor;
            car.engineIdleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineIdleAudio);
            car.engineRevClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineRevAudio);
            car.engineHighClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineHighAudio);
            car.rollingWheels = kola; car.steeringWheels = new Transform[0];
            opis = a.nazwa + " length=" + bounds.size.z.ToString("F2") + " width=" + bounds.size.x.ToString("F2") + " height=" + bounds.size.y.ToString("F2")
                + " wheelbase=" + car.wheelbase.ToString("F2") + " " + kolaOpis;
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    static string Sciezka(Transform t, Transform stop)
    {
        var s = new List<string>();
        for (var x = t; x && x != stop; x = x.parent) s.Add(x.name);
        return string.Join("/", s);
    }

    static Bounds Lokalne(Renderer r, Transform root)
    {
        var b = r.bounds; var res = new Bounds(root.InverseTransformPoint(b.min), Vector3.zero);
        for (int i = 0; i < 8; i++)
            res.Encapsulate(root.InverseTransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z)));
        return res;
    }

    // one pivot per axle at the centre of its round wheel parts; the parts move under it keeping their place
    static Transform[] Kola(GameObject copy, Transform root, Bounds car, out float rozstaw, out string opis)
    {
        var osie = new Dictionary<bool, List<(Renderer r, Bounds b)>> { { true, new List<(Renderer, Bounds)>() }, { false, new List<(Renderer, Bounds)>() } };
        int odrzucone = 0;
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
        {
            string s = Sciezka(r.transform, copy.transform);
            if (!Kolo.IsMatch(s) || NieKolo.IsMatch(s)) continue;
            var b = Lokalne(r, root);
            float d = Mathf.Max(b.size.y, b.size.z);
            // a rolling part is round in the side view and sits low; anything else (fender liners, badges) stays on the body
            if (d < .04f * car.size.y || Mathf.Abs(b.size.y - b.size.z) > .25f * d || b.center.y > car.min.y + .55f * car.size.y) { odrzucone++; continue; }
            osie[b.center.z > car.center.z].Add((r, b));
        }
        var piwoty = new List<Transform>(); var srodki = new Dictionary<bool, Vector3>(); var liczby = new List<string>();
        foreach (var kv in osie)
        {
            if (kv.Value.Count == 0) { liczby.Add((kv.Key ? "front" : "rear") + "=0"); continue; }
            var u = kv.Value[0].b; foreach (var x in kv.Value) u.Encapsulate(x.b);
            var piv = new GameObject("KOŁA " + (kv.Key ? "PRZÓD" : "TYŁ") + " | pivot at the axle").transform;
            piv.SetParent(root, false); piv.localPosition = new Vector3(car.center.x, u.center.y, u.center.z); piv.localRotation = Quaternion.identity;
            foreach (var x in kv.Value.OrderBy(x => x.r.transform.GetInstanceID())) if (x.r.transform != piv) x.r.transform.SetParent(piv, true);
            piwoty.Add(piv); srodki[kv.Key] = piv.localPosition;
            liczby.Add((kv.Key ? "front" : "rear") + "=" + kv.Value.Count + " parts r=" + (u.size.y * .5f).ToString("F2"));
        }
        rozstaw = srodki.ContainsKey(true) && srodki.ContainsKey(false) ? Mathf.Abs(srodki[true].z - srodki[false].z) : 0f;
        if (piwoty.Count < 2) BraxBuildQuality.Warn("KOLEKCJA_AUT wheels: an axle without round wheel parts (" + string.Join(", ", liczby) + ") - those wheels do not roll");
        opis = "wheels[" + string.Join(", ", liczby) + ", not rolling=" + odrzucone + "]";
        return piwoty.ToArray();
    }
}
