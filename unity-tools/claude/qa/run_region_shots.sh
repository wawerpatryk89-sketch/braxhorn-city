#!/bin/bash
# $1 = sha. Pobiera player z claude-region-preview, robi zdjęcia wg shots_region.txt + taras/pawilon z raportów.
set -e
S=/tmp/claude-0/-home-claude/3479927c-d697-5bf0-8311-c78453fde413/scratchpad; SHA=$1
$S/qa/fetch_player.sh claude-region-preview $SHA
P=$S/player_$SHA/p
python3 - "$P" > $S/qa/shots_extra_$SHA.txt <<'PY'
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
        # front pawilonu patrzy na boiska (na zachód): kamera od zachodu patrzy na +X (yaw 270)
        out.append(f"22-pawilon|{x:.1f},{y+1.0:.2f},{z:.1f},270,10,16"); out.append(f"23-pawilon-wnetrze|{x+3:.1f},{y+1.2:.2f},{z:.1f},90,12,5")
        out.append(f"24-pawilon-szatnia|{x-5:.1f},{y+1.2:.2f},{z:.1f},270,12,4")
except Exception as e: print("# brak region.json", e, file=sys.stderr)
print("\n".join(out))
PY
mapfile -t SHOTS < <(grep -v '^#' $S/qa/shots_region.txt; grep -v '^#' $S/qa/shots_extra_$SHA.txt)
mkdir -p $S/qa/region_$SHA
cd $S/qa && node shoot.cjs $P $S/qa/region_$SHA "${SHOTS[@]}" > $S/qa/shoot_$SHA.log 2>&1
echo "SHOTS DONE"; ls $S/qa/region_$SHA
