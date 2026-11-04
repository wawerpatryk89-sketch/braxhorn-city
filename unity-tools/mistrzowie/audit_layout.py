"""Plan placement checks; reservations are not completed buildings."""
import gzip,json,itertools
from pathlib import Path
import numpy as np
from shapely.geometry import box,Point
from layout import X_ROADS,Z_ROADS,ENTRANCE_HOLES,METRO,TRAM_STOPS,BUS_STOPS,K
ROOT=Path(__file__).resolve().parents[2]


def audit(buildings):
    errors=[];shapes={b['id']:box(b['x']-b['w']/2,b['z']-b['d']/2,b['x']+b['w']/2,b['z']+b['d']/2) for b in buildings}
    if len(shapes)!=len(buildings):errors.append('Duplicate building IDs')
    for a,b in itertools.combinations(shapes,2):
        if shapes[a].intersection(shapes[b]).area>1e-6:errors.append('Overlapping reservations: '+a+'/'+b)
    for b in buildings:
        if not box(0,0,1200,1000).covers(shapes[b['id']]):errors.append('Outside core: '+b['id'])
        for x in X_ROADS:
            if shapes[b['id']].intersection(box(x-17,-70,x+17,935)).area>1e-6:errors.append('Road/sidewalk X: '+b['id'])
        for z in Z_ROADS:
            if shapes[b['id']].intersection(box(70,z-17,1130,z+17)).area>1e-6:errors.append('Road/sidewalk Z: '+b['id'])
        for x,z,w,d in ENTRANCE_HOLES:
            if shapes[b['id']].intersection(box(x,z,x+w,z+d)).area>1e-6:errors.append('Metro opening: '+b['id'])
    ids={b['id']:b for b in buildings}
    for west,east in [('H1','H2'),('H3','H4'),('A2','A1'),('T01','T02')]:
        if ids[west]['x']>=ids[east]['x']:errors.append('Plan ordering: '+west+'/'+east)
    if ids['A2']['z']>=ids['A1']['z']:errors.append('A2 must be southwest of A1')
    return errors


def main():
    buildings=json.loads(Path(__file__).with_name('plan_inventory.json').read_text())['buildings']
    errors=audit(buildings)
    data=json.loads(gzip.decompress((ROOT/'unity-city/Assets/Source/Mistrzowie/Exteriors.bytes').read_bytes()))
    generated=[]
    for b in data['buildings']:
        plan=next(p for p in buildings if p['id']==b['id'])
        if (b['x'],b['z'])!=(plan['x'],plan['z']):errors.append('Generated centre differs: '+b['id'])
        vertices=np.concatenate([np.asarray(m['positions']).reshape(-1,3) for m in b['lod0']])/K
        lo=vertices.min(axis=0);hi=vertices.max(axis=0)
        if lo[0]<-plan['w']/2-.01 or hi[0]>plan['w']/2+.01 or lo[2]<-plan['d']/2-.01 or hi[2]>plan['d']/2+.01:errors.append('Generated footprint outside plot: '+b['id'])
        generated.append(dict(id=b['id'],centre=[b['x'],b['z']],localBounds=[lo.tolist(),hi.tolist()]))
    report=dict(status='STATIC_CHECKS_PASS_UNITY_PENDING' if not errors else 'FAILED',errors=errors,
                reservations=len(buildings),generated=generated,metro=METRO,tramStops=len(TRAM_STOPS),busPlatforms=len(BUS_STOPS)*2,
                missingBuildings=[b['id'] for b in buildings if b['id'] not in {v['id'] for v in generated}],
                planDifferences=['A1 hall 192x118 instead of PDF 220x150; reservation 220x150 instead of PDF site approx 320x240',
                                 'A2 reservation 220x150 instead of PDF site approx 280x230; hall not generated',
                                 'Metro centres adjusted for existing source model, not literal diagram markers',
                                 'PDF TR01 near M1; code TR01 moved to x460 on east-west track'],
                runtime='NOT_RUN')
    (ROOT/'mistrzowie-evidence/layout-audit.json').write_text(json.dumps(report,indent=2,ensure_ascii=False))
    print(json.dumps(dict(status=report['status'],reservations=len(buildings),generated=len(generated),errors=errors)))
    if errors:raise SystemExit(1)

if __name__=='__main__':main()
