using UnityEngine;

// BRAXHORN — LUSTRO Z PRAWDZIWYM ODBICIEM (właściciel 10.11 00:15: „lustro ma być lustrem, a nie naklejką na ścianie”).
// Tafla = płaski prostokąt (Quad Unity: widoczna strona patrzy w −Z obiektu) z materiałem Braxhorn/Lustro. Gdy kamera gracza
// jest przed taflą i bliżej niż `zasieg`, scena jest rysowana drugi raz z kamery ODBITEJ w płaszczyźnie lustra (macierz odbicia,
// ukośna płaszczyzna przycinania na tafli — nic zza ściany) do tekstury `rozmiar` px; shader pokazuje ją we współrzędnych ekranu,
// więc odbicie jest geometrycznie prawdziwe (postać, sala, sprzęt — wszystko, co stoi przed lustrem). Dalej niż `zasieg` lustro
// pokazuje odbicie z sondy otoczenia (jak dotąd) — bez kosztu. Telefon: najwyżej `MaksNaKlatke` luster na klatkę, bez cieni
// w odbiciu, tekstura 16-bit głębi, zwalniana przy wyłączeniu. Błąd rysowania = lustro wraca do sondy i więcej nie próbuje.
// Klasa = plik (Unity zapisuje skrypt w scenie tylko wtedy).
public sealed class BraxLustro : MonoBehaviour
{
    public float zasieg = 16f;
    public int rozmiar = 512;
    const int MaksNaKlatke = 2;

    static readonly int TexId = Shader.PropertyToID("_OdbicieTex"), JestId = Shader.PropertyToID("_OdbicieJest");
    static bool wTrakcie; static int klatka, wKlatce;
    Camera kam; RenderTexture rt; Renderer rend; MaterialPropertyBlock blok; bool awaria; int ostatnia = -100;

    void OnEnable() { rend = GetComponent<Renderer>(); blok = new MaterialPropertyBlock(); Ustaw(0f); }

    void OnDisable()
    {
        Ustaw(0f);
        if (kam) { Destroy(kam.gameObject); kam = null; }
        if (rt) { rt.Release(); Destroy(rt); rt = null; }
    }

    void Ustaw(float jest)
    {
        if (!rend || blok == null) return;
        rend.GetPropertyBlock(blok);
        if (rt) blok.SetTexture(TexId, rt);
        blok.SetFloat(JestId, jest);
        rend.SetPropertyBlock(blok);
    }

    void Update()
    {   // odbicie nie było rysowane od kilku klatek (kamera daleko / za taflą) — wróć do sondy
        if (!awaria && rt && Time.frameCount - ostatnia > 3 && ostatnia > -100) { Ustaw(0f); ostatnia = -100; }
    }

    void OnWillRenderObject()
    {
        if (awaria || wTrakcie || !rend || !enabled) return;
        var cam = Camera.current;
        if (!cam || cam.cameraType != CameraType.Game || cam == kam) return;
        Vector3 n = -transform.forward, p0 = transform.position, cp = cam.transform.position;
        if (Vector3.Dot(cp - p0, n) < .03f || (cp - p0).sqrMagnitude > zasieg * zasieg) return;
        if (klatka != Time.frameCount) { klatka = Time.frameCount; wKlatce = 0; }
        if (wKlatce >= MaksNaKlatke) return;
        wKlatce++; wTrakcie = true;
        float cienie = QualitySettings.shadowDistance; bool odwr = GL.invertCulling;
        try
        {
            if (!rt)
            {
                rt = new RenderTexture(rozmiar, rozmiar, 16) { name = "BraxLustro " + name, useMipMap = false, antiAliasing = 1, wrapMode = TextureWrapMode.Clamp };
                rt.hideFlags = HideFlags.DontSave;
            }
            if (!kam)
            {
                var g = new GameObject("BraxLustro kamera (" + name + ")") { hideFlags = HideFlags.HideAndDontSave };
                kam = g.AddComponent<Camera>(); kam.enabled = false;
            }
            kam.CopyFrom(cam);
            kam.targetTexture = rt; kam.useOcclusionCulling = false; kam.depthTextureMode = DepthTextureMode.None; kam.rect = new Rect(0f, 0f, 1f, 1f);
            // odbicie świata w płaszczyźnie n · x + d = 0
            float d = -Vector3.Dot(n, p0);
            var odb = Matrix4x4.identity;
            odb.m00 = 1f - 2f * n.x * n.x; odb.m01 = -2f * n.x * n.y; odb.m02 = -2f * n.x * n.z; odb.m03 = -2f * d * n.x;
            odb.m10 = -2f * n.y * n.x; odb.m11 = 1f - 2f * n.y * n.y; odb.m12 = -2f * n.y * n.z; odb.m13 = -2f * d * n.y;
            odb.m20 = -2f * n.z * n.x; odb.m21 = -2f * n.z * n.y; odb.m22 = 1f - 2f * n.z * n.z; odb.m23 = -2f * d * n.z;
            kam.worldToCameraMatrix = cam.worldToCameraMatrix * odb;
            // ukośna płaszczyzna przycinania na tafli (w przestrzeni odbitej kamery): rysuje się tylko to, co przed lustrem
            var w2c = kam.worldToCameraMatrix;
            Vector3 pk = w2c.MultiplyPoint(p0 + n * .02f), nk = w2c.MultiplyVector(n).normalized;
            kam.projectionMatrix = cam.CalculateObliqueMatrix(new Vector4(nk.x, nk.y, nk.z, -Vector3.Dot(pk, nk)));
            kam.transform.position = odb.MultiplyPoint(cp);
            QualitySettings.shadowDistance = 0f;
            GL.invertCulling = !odwr;
            kam.Render();
            ostatnia = Time.frameCount;
            Ustaw(1f);
        }
        catch (System.Exception e)
        {
            awaria = true; Ustaw(0f);
            Debug.LogWarning("BRAXHORN_LUSTRO " + name + ": odbicie wyłączone (" + e.Message + ") — lustro pokazuje sondę otoczenia");
        }
        finally
        {
            GL.invertCulling = odwr; QualitySettings.shadowDistance = cienie; wTrakcie = false;
        }
    }
}
