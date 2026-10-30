#!/bin/bash
# Pobiera player-<sha>.zip z ukrytego wydania, rozpakowuje i przygotowuje do serwowania lokalnie (bez gzip).
set -e
REL="$1"; SHA="$2"; S=/tmp/claude-0/-home-claude/3479927c-d697-5bf0-8311-c78453fde413/scratchpad
D=$S/player_$SHA; rm -rf $D; mkdir -p $D; cd $D
id=$(gh api repos/wawerpatryk89-sketch/braxhorn-city/releases --jq ".[] | select(.tag_name==\"$REL\") | .assets[] | select(.name==\"player-$SHA.zip\") | .id")
[ -n "$id" ] || { echo "brak player-$SHA.zip w $REL"; exit 1; }
gh api -H "Accept: application/octet-stream" repos/wawerpatryk89-sketch/braxhorn-city/releases/assets/$id > player.zip
unzip -o -q player.zip -d p && rm player.zip
cd p/Build; for f in *.framework.js.unityweb *.wasm.unityweb; do gzip -dc "$f" > "${f%.unityweb}"; rm "$f"; done; cd ..
B=$(ls Build/*.loader.js | sed -E 's#Build/(.*)\.loader\.js#\1#')
sed -i "s#frameworkUrl:'Build/$B.framework.js.unityweb'#frameworkUrl:'Build/$B.framework.js'#; s#codeUrl:'Build/$B.wasm.unityweb'#codeUrl:'Build/$B.wasm'#" index.html
ls *.json | head -20; echo "PLAYER OK: $D/p"
