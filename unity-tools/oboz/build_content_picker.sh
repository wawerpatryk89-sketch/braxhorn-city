#!/usr/bin/env bash
# Original BRAXHORN Android file picker for content updates.
# Run in exactly unityci/editor:ubuntu-6000.0.58f2-android-3 before Unity.
# Only produces a deterministic Java JAR; NEVER builds or signs an APK.
set -euo pipefail
project=/project/unity-city
android=/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer
jdk="$android/OpenJDK/bin"
javac="$jdk/javac"
jar="$jdk/jar"
test -x "$javac" && test -x "$jar"
platform=$(find "$android/SDK/platforms" -name android.jar -type f | sort -V | tail -1)
test -s "$platform"
unity_jar=''
while IFS= read -r candidate; do
  if "$jar" tf "$candidate" | grep -Fxq 'com/unity3d/player/UnityPlayer.class'; then
    unity_jar=$candidate
    break
  fi
done < <(find "$android/Variations" -name '*.jar' -type f)
test -n "$unity_jar" && test -s "$unity_jar"
classes=/tmp/brax-oboz-content-picker/classes
mkdir -p "$classes"
"$javac" -source 8 -target 8 -encoding UTF-8 -classpath "$platform:$unity_jar" \
  -d "$classes" "$project/Assets/Updates/Android/BraxContentPicker.java"
out="$project/Assets/Plugins/Android/BraxContentPicker.jar"
mkdir -p "$(dirname "$out")"
python3 - "$classes" "$out" <<'PY'
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
import sys
root,dest=Path(sys.argv[1]),Path(sys.argv[2])
names=[f.relative_to(root).as_posix() for f in sorted(root.rglob('*.class'))]
assert 'com/braxhorn/content/BraxContentPicker.class' in names, 'Android picker class missing'
with ZipFile(dest,'w') as jar:
    for name in names:
        info=ZipInfo(name, date_time=(2020,1,1,0,0,0))
        info.compress_type=ZIP_DEFLATED
        info.external_attr=0o644<<16
        jar.writestr(info,(root/name).read_bytes())
assert dest.stat().st_size > 1000
print('BRAX_OBOZ_PICKER_BUILT',dest.stat().st_size)
PY
