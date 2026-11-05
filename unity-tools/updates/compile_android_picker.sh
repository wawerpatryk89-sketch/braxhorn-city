#!/usr/bin/env bash
# Run inside the SAME Unity Android image as the bundle validation. This checks
# the actual installed UnityPlayer/Android Java APIs without building another APK.
set -euo pipefail
android_root=/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer
javac_path=$(find "$android_root" -path '*/bin/javac' -type f -print -quit)
jar_path="$(dirname "$javac_path")/jar"
api_jar=$(find "$android_root/SDK/platforms" -name android.jar -type f | sort -V | tail -1)
unity_jar=
while IFS= read -r candidate; do
  "$jar_path" tf "$candidate" > /tmp/brax-jar-entries.txt
  if grep -Fxq 'com/unity3d/player/UnityPlayer.class' /tmp/brax-jar-entries.txt; then
    unity_jar=$candidate
    break
  fi
done < <(find "$android_root/Variations" -name '*.jar' -type f)
test -n "$javac_path" && test -n "$api_jar" && test -n "$unity_jar"
mkdir -p /tmp/brax-java-validation
"$javac_path" -source 8 -target 8 -classpath "$api_jar:$unity_jar" -d /tmp/brax-java-validation \
  /project/unity-city/Assets/Updates/Android/BraxContentPicker.java
echo BRAX_CONTENT_ANDROID_JAVA_PASS
