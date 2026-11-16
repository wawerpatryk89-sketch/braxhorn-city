using System.Collections.Generic;
using UnityEngine;

// NURKOWANIE I WIDOK POD WODĄ w basenach Mistrzów (właściciel 16.11 07:33: "jak wejdę do wody, zanurkuję, to ma być woda
// po całym basenie"). One per aquapark (the generator puts it on the aquapark root).
// - NURKUJ (button over SKOK while swimming in a Mistrzowie pool; C / Ctrl on a keyboard): while held, the region's own water
//   zone (BraxChampionsPoolSwimVolume) hands the player a surface lowered by Glebokosc - the SHARED buoyancy of BraxGymPlayer
//   then carries the body down to the floor (the deep well of the lagoon: 4 m) and back up when let go. The shared
//   swimming code, animations and touch controls are not changed.
// - when the camera itself is under a pool's surface, a blue veil (Braxhorn/ChampionsPodWodaEkran) covers the screen and
//   joins the water body drawn by Braxhorn/ChampionsPoolVolume: under the water everything is water.
public sealed class BraxChampionsPodWoda : MonoBehaviour
{
    public Material ekran;
    // the dive (world units the surface is lowered by): the floor stops the body long before
    public static float Glebokosc { get; private set; }
    public static bool Pod { get; private set; }
    internal static readonly List<BraxChampionsPoolSwimVolume> Strefy = new List<BraxChampionsPoolSwimVolume>();
    internal static float PlywaDo;
    const float W_Dol = 40f;

    static BraxChampionsPodWoda jedyny;
    GameObject zaslona; Camera dlaKamery; bool trzyma; GUIStyle st; float stRozmiar;

    void OnEnable() { if (!jedyny) jedyny = this; }
    void OnDisable() { if (jedyny == this) { jedyny = null; Glebokosc = 0f; Pod = false; } if (zaslona) zaslona.SetActive(false); }

    bool Plywa => Time.time < PlywaDo;

    // the button: over SKOK in the column of CIOS (the same measures as the shared touch layout), screen pixels y up
    void Przycisk(out Vector2 srodek, out float r)
    {
        var g = BraxGui.BezpiecznyProstokat();
        var sa = new Rect(g.x, Screen.height - g.yMax, g.width, g.height);
        float s = Mathf.Min(Screen.width, Screen.height), k = BraxUstawienia.SkalaPrzyciskow;
        float m = s * .045f, gap = s * .024f * k, rC = s * .074f * k, rS = s * .056f * k;
        var cCios = new Vector2(sa.xMax - m - rC, sa.yMin + m + rC);
        var cSkok = cCios + new Vector2(Mathf.Cos(97f * Mathf.Deg2Rad), Mathf.Sin(97f * Mathf.Deg2Rad)) * (rC + gap + rS);
        r = rS * 1.05f;
        srodek = new Vector2(cCios.x, cSkok.y + rS + gap * 1.8f + r);
    }

    void Update()
    {
        if (jedyny != this) return;
        trzyma = false;
        if (Plywa)
        {
            Przycisk(out var c, out float r);
            float r2 = r * 1.25f * r * 1.25f;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled && (t.position - c).sqrMagnitude <= r2) trzyma = true;
            }
            if (Input.touchCount == 0 && Input.GetMouseButton(0) && ((Vector2)Input.mousePosition - c).sqrMagnitude <= r2) trzyma = true;
            if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) trzyma = true;
        }
        Glebokosc = trzyma ? W_Dol : 0f;
    }

    void LateUpdate()
    {
        if (jedyny != this) return;
        var cam = Camera.main;
        bool pod = false;
        if (cam)
        {
            var p = cam.transform.position;
            foreach (var z in Strefy)
            {
                if (!z || !z.waterSurface) continue;
                var b = z.Obszar;
                if (p.y >= z.waterSurface.position.y - .02f || p.y < b.min.y - 1f) continue;
                if (p.x < b.min.x - .5f || p.x > b.max.x + .5f || p.z < b.min.z - .5f || p.z > b.max.z + .5f) continue;
                pod = true; break;
            }
        }
        Pod = pod;
        if (pod && ekran)
        {
            if (!zaslona || dlaKamery != cam) Zaslona(cam);
            if (!zaslona.activeSelf) zaslona.SetActive(true);
        }
        else if (zaslona && zaslona.activeSelf) zaslona.SetActive(false);
    }

    // a quad drawn straight in clip space by its shader: whole screen whatever the camera's field of view
    void Zaslona(Camera cam)
    {
        if (zaslona) Destroy(zaslona);
        dlaKamery = cam;
        zaslona = new GameObject("pod wodą: zasłona ekranu");
        zaslona.transform.SetParent(cam.transform, false);
        var mesh = new Mesh { name = "pod wodą" };
        mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        zaslona.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = zaslona.AddComponent<MeshRenderer>();
        r.sharedMaterial = ekran;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        zaslona.SetActive(false);
    }

    void OnGUI()
    {
        if (jedyny != this || !Plywa || Event.current.type != EventType.Repaint) return;
        Przycisk(out var c, out float r);
        var g = new Vector2(c.x, Screen.height - c.y);
        BraxGui.PrzyciskKolowy(g, r, null, trzyma, new Color(.20f, .62f, 1f, 1f));
        if (st == null || stRozmiar != r) { stRozmiar = r; st = BraxGui.Styl(Mathf.RoundToInt(r * .36f), true, TextAnchor.MiddleCenter); }
        BraxGui.Napis(new Rect(g.x - r, g.y - r, r * 2f, r * 2f), "NURKUJ", st, BraxGui.Biel);
    }
}
