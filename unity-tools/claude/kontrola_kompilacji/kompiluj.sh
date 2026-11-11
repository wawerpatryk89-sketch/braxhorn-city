#!/bin/bash
# KONTROLA KOMPILACJI (MIASTO R1, 11.11.2026): cały kod C# z unity-city/Assets kompilowany poza Unity z prawdziwymi
# referencjami Unity (NuGet: UnityEngine.Modules 2021.3.33 + Unity3D.SDK 2021.1.14 → UnityEditor.dll). Wynik: lista błędów
# kompilacji — ma być 0. API dodane dopiero w Unity 6 (linearVelocity, PhysicsMaterial, NamedBuildTarget…) nie ma
# w referencjach 2021, więc te błędy są odfiltrowane (lista WZORCE_UNITY6) — w Unity 6 kompilują się poprawnie.
#   ./kompiluj.sh        → bledy.txt (wszystkie), wynik na ekranie (bez API Unity 6)
set -e
cd "$(dirname "$0")"
A="$(cd ../../../unity-city/Assets && pwd)"
REF="${BRAXHORN_REF:-$HOME/.cache/braxhorn-unity-ref}"
mkdir -p "$REF"
pobierz() { # id wersja
  local d="$REF/$1_$2"; [ -d "$d" ] && return 0
  curl -sSL "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg" -o "$REF/$1.$2.nupkg"
  mkdir -p "$d" && (cd "$d" && unzip -q -o "../$1.$2.nupkg")
}
pobierz unityengine.modules 2021.3.33
pobierz unity3d.sdk 2021.1.14.1
M="$REF/unityengine.modules_2021.3.33/lib/netstandard2.0"; S="$REF/unity3d.sdk_2021.1.14.1/lib"
mkdir -p obj_k
cat > obj_k/kompil.csproj <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><OutputType>Library</OutputType><Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS0162;CS0168;CS0219;CS0414;CS0649;CS0169;CS8321;CS0618;CS0612;CS0108;CS0114;CS8981;CS1701;CS1702</NoWarn>
    <ImplicitUsings>disable</ImplicitUsings><DefineConstants>UNITY_EDITOR;UNITY_2021_3_OR_NEWER;UNITY_ANDROID</DefineConstants>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks><ProduceReferenceAssembly>false</ProduceReferenceAssembly>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$A/**/*.cs" />
    <Reference Include="$M/*.dll" />
    <Reference Include="$S/UnityEditor.dll" />
  </ItemGroup>
</Project>
XML
dotnet build obj_k/kompil.csproj -c Release -o obj_k/bin -v q -nologo 2>&1 | grep -E " error " | sed -E 's|^.*unity-city/Assets/||; s| \[.*||' | sort -u > bledy.txt || true
WZORCE_UNITY6='linearVelocity|linearDamping|angularDamping|PhysicsMaterial|globalTextureMipmapLimit|webGLBuildSubtarget|WebGLTextureSubtarget|SetIl2CppCodeGeneration|NamedBuildTarget|Il2CppCodeGeneration|overrideTextureCompression|overrideMaxTextureSize|OverrideTextureCompression|customReflectionTexture|GetSourceTextureWidthAndHeight'
grep -vE "$WZORCE_UNITY6" bledy.txt > bledy_r1.txt || true
n=$(wc -l < bledy_r1.txt); u=$(grep -cE "$WZORCE_UNITY6" bledy.txt || true)
cat bledy_r1.txt
echo "KONTROLA KOMPILACJI: błędów $n (pominięte API Unity 6: $u)"
[ "$n" -eq 0 ]
