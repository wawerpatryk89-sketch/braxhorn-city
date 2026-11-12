// The arena preview compiles BraxChampionsBuildings.cs (the whole region's build order) but draws only the arena: the other
// region generators it calls are empty stand-ins here (each has its own preview project with the real code).
public static partial class BraxChampionsE1 { public static void Generate(string o) { } }
public static partial class BraxChampionsD2 { public static void Generate(string o) { } public static void GenerateG1(string o) { } }
public static class BraxChampionsPostacie { public static int Generate() => 0; }
public static class BraxChampionsPlakaty { public static int Generate() => 0; }
public static partial class BraxChampionsAquapark { public static int Generate() => 0; }
