using UnityEngine;

// Maska piasku wypieczona z kolorów trójkątów. Nie wymaga mesh.isReadable na Androidzie.
public sealed class BraxPiasekPowierzchnia : MonoBehaviour
{
    public byte[] wagiTrojkatow = new byte[0];
    public bool JestPiasek(RaycastHit hit)
    {
        int i = hit.triangleIndex;
        return i >= 0 && i < wagiTrojkatow.Length && wagiTrojkatow[i] > 153 && hit.normal.y > .85f;
    }
}
