using UnityEngine;

// WYGLĄD POSTACI (dyspozytor 01:28: „żeby nie było klonów”). Ustawiany przy buildzie (BraxClaudeOsiedleLudzie), stosowany
// przy starcie gry przez MaterialPropertyBlock — bez kopii materiałów (wydajność; bloków nie da się zapisać w scenie,
// dlatego dane leżą w tym komponencie):
//  • odcień UBRAŃ: shader Braxhorn/LudzieUbranie przebarwia tylko piksele z maską ubrania (alfa tekstury ciała,
//    fetch_rocketbox.py) — skóra, twarz i włosy bez zmian; mundury (policja, medycy, ochrona, kucharz) bez odcienia
//    albo w kolorze roli;
//  • kolory akcesoriów zbudowanych kodem (czapka, plecak, torba, kubek) — _Color materiału Standard.
public sealed class BraxWyglad : MonoBehaviour
{
    public Renderer cialo;
    public int[] indeksyUbran = new int[0];
    public Color odcien = Color.white;
    public float sila;
    public Renderer[] akcesoria = new Renderer[0];
    public Color[] koloryAkcesoriow = new Color[0];

    static int idOdcien, idSila, idKolor;

    void Awake() { Zastosuj(); }

    public void Zastosuj()
    {
        if (idOdcien == 0) { idOdcien = Shader.PropertyToID("_Odcien"); idSila = Shader.PropertyToID("_OdcienSila"); idKolor = Shader.PropertyToID("_Color"); }
        var b = new MaterialPropertyBlock();
        if (cialo && sila > 0f && indeksyUbran != null)
            foreach (int i in indeksyUbran)
            {
                if (i < 0 || i >= cialo.sharedMaterials.Length) continue;
                cialo.GetPropertyBlock(b, i); b.SetColor(idOdcien, odcien); b.SetFloat(idSila, sila); cialo.SetPropertyBlock(b, i);
            }
        if (akcesoria != null && koloryAkcesoriow != null)
            for (int k = 0; k < akcesoria.Length && k < koloryAkcesoriow.Length; k++)
            {
                var r = akcesoria[k]; if (!r) continue;
                b.Clear(); r.GetPropertyBlock(b, 0); b.SetColor(idKolor, koloryAkcesoriow[k]); r.SetPropertyBlock(b, 0);
            }
    }
}
