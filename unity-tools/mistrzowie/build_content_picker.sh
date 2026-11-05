#!/usr/bin/env bash
# Reproducible Android Java picker plug-in for this exact Unity 6 editor.
# Compile BEFORE expensive Unity build; never download or generate a new keystore.
set -euo pipefail
project=/project/unity-city
android_root=/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer
jdk=$android_root/OpenJDK/bin
javac=$jdk/javac
jar=$jdk/jar
test -x "$javac" && test -x "$jar"
platform=$(find "$android_root/SDK/platforms" -name android.jar -type f | sort -V | tail -1)
test -f "$platform"
unity_jar=''
while IFS= read -r candidate; do
  if "$jar" tf "$candidate" | grep -Fxq 'com/unity3d/player/UnityPlayer.class'; then
    unity_jar=$candidate
    break
  fi
done < <(find "$android_root/Variations" -name '*.jar' -type f)
test -n "$unity_jar"
build=/tmp/brax-mistrzowie-picker
mkdir -p "$build/classes"
"$javac" -source 8 -target 8 -encoding UTF-8 -classpath "$platform:$unity_jar" \
  -d "$build/classes" "$project/Assets/Updates/Android/BraxContentPicker.java"
mkdir -p "$project/Assets/Plugins/Android"
# Stable ZIP timestamps give a stable JAR hash for immutable runtime fingerprint.
python3 - "$build/classes" "$project/Assets/Plugins/Android/BraxContentPicker.jar" <<'PY'
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
import sys
root,output=Path(sys.argv[1]),Path(sys.argv[2])
with ZipFile(output,'w') as z:
    for file in sorted(root.rglob('*.class')):
        name=file.relative_to(root).as_posix()
        info=ZipInfo(name,date_time=(2020,1,1,0,0,0))
        info.compress_type=ZIP_DEFLATED
        info.external_attr=0o644<<16
        z.writestr(info,file.read_bytes())
print('BRAX_MISTRZOWIE_PICKER_COMPILED',output.stat().st_size)
PY
