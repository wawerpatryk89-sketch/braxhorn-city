// Owner 10.11 12:29-12:52: "Wszystkiego skalę sprawdzać ... globalnie, arena tak samo jak hotel Sobczak". Every Poly Haven
// model of the region was measured from its glTF bounds (10.11, scratch gltf_dims.py) against the real piece it stands for.
// Only the ones that are not furniture-size in the file get a correction here, used by every generator that places them
// (arena lobby, Hotel Sobczak, apartments), so one model has one size everywhere.
//   sofa_02  1.81 x .71 x .82 m - a loveseat; a lobby Chesterfield is ~2.2 x .80 x .95 m  -> x1.22
// Checked and real-size as authored: modern_arm_chair_01 .82 x 1.02 x .99, dining_chair_02 .43 x .97 x .58 (seat .46),
// bar_chair_round_01 h .75 (counter 1.10), mid_century_lounge_chair 1.01 x 1.17, coffee_table_round_01 1.30 x .49,
// side_table_01 .55, Ottoman_01 .88 x .62, vintage_suitcase two cases of ~.70 x .53, potted plants .84 / 1.35 m.
public static class BraxChampionsRealSize
{
    public static float Scale(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath)) return 1f;
        if (modelPath.Contains("sofa_02/")) return 1.22f;
        return 1f;
    }
}
