#!/bin/bash
# $1 = sha. Pobiera player z claude-region-preview, robi zdjęcia wg shots_region.txt + taras/pawilon z raportów.
set -e
S=/tmp/claude-0/-home-claude/3479927c-d697-5bf0-8311-c78453fde413/scratchpad; SHA=$1
$(dirname "$0")/fetch_player.sh claude-region-preview $SHA
P=$S/player_$SHA/p
python3 - "$P" > $(dirname "$0")/shots_extra_$SHA.txt <<'PY'
import json,sys,os
p=sys.argv[1]; out=[]
try:
    t=json.load(open(os.path.join(p,'terrace.json'))); ry=t['roofY']
    out.append(f"20-taras-front|59,{ry+0.3:.2f},44,0,14,11"); out.append(f"21-taras-tyl|59,{ry+0.3:.2f},41,180,14,11")
except Exception as e: print("# brak terrace.json", e, file=sys.stderr)
try:
    r=json.load(open(os.path.join(p,'region.json'))); a=r.get('pavilionAt',{})
    if a and (a.get('x') or a.get('z')):
        x,y,z=a['x'],a['y'],a['z']
        out.append(f"22-pawilon|{x:.1f},{y+0.3:.2f},{z-14:.1f},180,12,12"); out.append(f"23-pawilon-wnetrze|{x-5:.1f},{y+0.3:.2f},{z-3:.1f},180,12,8")
except Exception as e: print("# brak region.json", e, file=sys.stderr)
print("\n".join(out))
PY
mapfile -t SHOTS < <(grep -v '^#' $(dirname "$0")/shots_region.txt; grep -v '^#' $(dirname "$0")/shots_extra_$SHA.txt)
mkdir -p $(dirname "$0")/region_$SHA
cd $S/qa && node shoot.cjs $P $(dirname "$0")/region_$SHA "${SHOTS[@]}" > $(dirname "$0")/shoot_$SHA.log 2>&1
echo "SHOTS DONE"; ls $(dirname "$0")/region_$SHA
