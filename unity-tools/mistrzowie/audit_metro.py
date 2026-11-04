"""Analytical regression checks against the authored metro and real train GLB.

This is not a Unity Physics/renderer/device test. It covers the measured run #4
obstruction, subsequent fixed furniture, the first stair flight and train axes.
Private ticket-gate meshes and complete scene physics remain Unity checks.
"""
import ast
import itertools
import json
import math
from pathlib import Path
import re
import struct

import numpy as np
from layout import K

ROOT = Path(__file__).resolve().parents[2]
METRO = ROOT / 'unity-city/Assets/Editor/Claude/BraxClaudeMetro.cs'
REUSE = ROOT / 'unity-city/Assets/Editor/Mistrzowie/BraxClaudeMetro.Reuse.cs'
TRAIN = ROOT / 'unity-city/Assets/Source/Claude/Metro/metro_train.glb'


def number(expression, values):
    expression = re.sub(r'(?<=[0-9.])f\b', '', expression.strip())
    tree = ast.parse(expression, mode='eval')
    allowed = (ast.Expression, ast.Constant, ast.Name, ast.Load, ast.BinOp,
               ast.UnaryOp, ast.Add, ast.Sub, ast.Mult, ast.Div, ast.USub,
               ast.Subscript, ast.Call)
    if any(not isinstance(n, allowed) for n in ast.walk(tree)):
        raise ValueError('Unsupported numeric source expression: ' + expression)
    return eval(compile(tree, '<metro-numeric-expression>', 'eval'), {'__builtins__': {}}, values)


def source_values(source):
    values = {'K': K}
    for declaration in re.findall(r'const float ([^;]+);', source):
        for item in declaration.split(','):
            key, expression = item.split('=', 1)
            values[key.strip()] = number(expression, values)
    values['EZ'] = [number(n, values) for n in re.search(r'float\[\] EZ = \{([^}]+)', source)[1].split(',')]
    values['ER_STOPIEN'] = number(re.search(r'float ER_STOPIEN = ([^;]+)', source)[1], values)
    for name in ('ER_Os', 'SchodyZ1', 'ZcSchodow'):
        expression = re.search(r'float '+name+r'\(float z0\) => ([^;]+)', source)[1]
        values[name] = lambda z0, expression=expression: number(expression, dict(values, z0=z0))
    return values


def walking_route():
    values = source_values(METRO.read_text())
    method = REUSE.read_text().split('public static Vector3[] ChampionsWalkingRoute()', 1)[1]
    for name, expression in re.findall(r'float (\w+)=([^;]+);', method):
        values[name] = number(expression, values)
    return [np.array([number(n, values) for n in vector.split(',')])
            for vector in re.findall(r'new Vector3\(([^)]+)\)', method)]


def box(name, center, size):
    center, size = np.array(center, dtype=float), np.array(size, dtype=float)
    return name, center-size/2, center+size/2


def fixtures():
    """Read positions from the actual source; dimensions follow W's box contract."""
    source = METRO.read_text()
    v = source_values(source)
    mezzanine = source.split('static void Antresola()', 1)[1].split('static Mesh SkrzydloMesh', 1)[0]
    result = []
    kosz = source.split('static void Kosz(Vector3 at)', 1)[1].split('static void Gasnica', 1)[0]
    r, h = .21*K, .8*K  # asserted against the authored source below
    assert 'r = .21f * K, h = .8f * K' in kosz
    assert 'r, h * .92f, 16, true' in kosz
    kit = (METRO.parent/'Wnetrza/BraxWnetrzaKit.cs').read_text()
    assert 'new Vector3(r * 1.6f, h, r * 1.6f)' in kit
    for vector in re.findall(r'Kosz\(new Vector3\(([^)]+)\)\)', mezzanine):
        p = np.array([number(n, v) for n in vector.split(',')])
        result.append(box('mezzanine bin', p+[0,h*.92/2,0], [r*1.6,h*.92,r*1.6]))
    machines = mezzanine.split('// biletomaty:', 1)[1].split('// kasa biletowa', 1)[0]
    xs = re.search(r'foreach \(float x in new\[\] \{([^}]+)', machines)[1]
    assert 'w = .8f * K, d = .5f * K, h = 1.85f * K' in machines
    for x in [number(n, v) for n in xs.split(',')]:
        for zf, facing in [(v['EZ'][0]+v['EW']+.25, 1), (v['EZ'][1]-.25, -1)]:
            result.append(box('ticket machine', [x,v['yM']+1.85*K/2,zf+facing*.5*K/2], [.8*K,1.85*K,.5*K]))
    platform = source.split('static void Wyposazenie()', 1)[1].split('static void Lawka', 1)[0]
    xs = re.search(r'foreach \(float x in new\[\] \{([^}]+)', platform)[1]
    assert 'Lawka(new Vector3(x, yP, 28.5f - .45f)' in platform
    bench = source.split('static void Lawka(Vector3 at', 1)[1].split('static void Kosz', 1)[0]
    assert 'L = 1.8f * K, hs = .45f * K, gl = .45f * K' in bench
    for x in [number(n, v) for n in xs.split(',')]:
        for z in (28.5-.45,28.5+.45):
            result.append(box('platform bench',[x,v['yP']+.45*K/2,z],[1.8*K,.45*K,.45*K]))
        result.append(box('platform bin',[x+2.2,v['yP']+h*.92/2,28.5],[r*1.6,h*.92,r*1.6]))
    columns = source.split('// kolumny w osi peronu', 1)[1].split('// światła peronu', 1)[0]
    xs = re.search(r'foreach \(float x in new\[\] \{([^}]+)', columns)[1]
    assert 'x - .35f, x + .35f, yP, yHc, 28.15f, 28.85f, true' in columns
    for x in [number(n, v) for n in xs.split(',')]:
        result.append(box('platform column',[x,(v['yP']+v['yHc'])/2,28.5],[.7,v['yHc']-v['yP'],.7]))
    return result


def capsule_overlaps(feet, obstacle, radius=.35):
    _, low, high = obstacle
    a, b = feet+np.array([0,.73,0]), feet+np.array([0,2.02,0])
    dx = max(low[0]-a[0],0,a[0]-high[0])
    dz = max(low[2]-a[2],0,a[2]-high[2])
    dy = max(low[1]-b[1],0,a[1]-high[1])
    return dx*dx+dy*dy+dz*dz < radius*radius


def furniture_hits(route):
    collisions = fixtures()
    hits = set()
    for a,b in zip(route,route[1:]):
        steps = max(1,math.ceil(np.linalg.norm((b-a)[[0,2]])/.05))
        for t in np.linspace(0,1,steps+1):
            p = a+(b-a)*t
            for obstacle in collisions:
                if capsule_overlaps(p,obstacle):
                    hits.add(obstacle[0])
    return sorted(hits)


def stair_clearance():
    v=source_values(METRO.read_text())
    topx, topy=v['WX1'],v['yM']
    bottomx=topx-16*.30*K
    bottomy=topy-16*(v['yM']-v['yP'])/32
    floor=lambda x: bottomy+(topy-bottomy)*(x-bottomx)/(topx-bottomx)
    # Old flat enclosure intersects the route; replacement stays below the
    # lower end of each .25m slice of the same first-flight ramp.
    old_intrusion=max(v['yM']-.4-floor(x) for x in np.linspace(54,topx,200))
    minimum=min(floor(x)-(floor(start)-.4)
                for start in np.arange(54,topx,.25)
                for x in np.linspace(start,min(start+.25,topx),8))
    source=METRO.read_text()
    assert 'SchodyNaPeron(true)' in REUSE.read_text()
    assert 'Mathf.InverseLerp(rampEnd.x, g1.x, x)) - .4f' in source
    return {'oldIntrusionMetres':old_intrusion,'newMinimumBelowRampMetres':minimum}


def train_bounds(reset_root=False):
    with TRAIN.open('rb') as f:
        magic,version,total=struct.unpack('<III',f.read(12))
        if magic!=0x46546c67 or version!=2 or total!=TRAIN.stat().st_size:
            raise ValueError('Invalid original metro GLB')
        size,kind=struct.unpack('<II',f.read(8))
        if kind!=0x4e4f534a:raise ValueError('GLB JSON required')
        data=json.loads(f.read(size))
    points=[]
    def visit(index,parent,is_root=False):
        node=data['nodes'][index]
        if any(k in node for k in ('translation','rotation','scale')):
            raise ValueError('Train source transform format changed; review its TRS')
        local=np.array(node.get('matrix',np.eye(4).T.flatten())).reshape(4,4).T
        if is_root and reset_root:local=np.eye(4)
        world=parent@local
        if 'mesh' in node:
            for primitive in data['meshes'][node['mesh']]['primitives']:
                accessor=data['accessors'][primitive['attributes']['POSITION']]
                corners=np.array([(*p,1) for p in itertools.product(*zip(accessor['min'],accessor['max']))])
                points.extend((world@corners.T).T[:,:3])
        for child in node.get('children',[]):visit(child,world)
    for index in data['scenes'][data.get('scene',0)]['nodes']:
        visit(index,np.eye(4),True)
    p=np.array(points)
    return p.max(axis=0)-p.min(axis=0)


def audit():
    hits=furniture_hits(walking_route())
    clearance=stair_clearance()
    upright=train_bounds();reset=train_bounds(True)
    assert not hits, 'Metro route crosses authored furniture: '+str(hits)
    assert clearance['newMinimumBelowRampMetres']>=.399
    assert upright[2]>upright[1]*3 and upright[1]>upright[0]
    assert reset[1]>reset[2]*3, 'Original root-reset regression fixture changed'
    assert 'PociagModel(source,true)' in REUSE.read_text()
    assert 'Quaternion.Euler(0f, yaw, 0f) * sourceRotation' in METRO.read_text()
    return dict(status='ANALYTICAL_METRO_REGRESSIONS_PASS_UNITY_PENDING',
                furnitureBoxes=len(fixtures()),routePoints=len(walking_route()),
                stair=clearance,originalTrainExtents=upright.tolist(),
                oldRootResetExtents=reset.tolist(),unityPhysicsTested=False,deviceTested=False)


if __name__=='__main__':
    result=audit();out=ROOT/'mistrzowie-evidence';out.mkdir(exist_ok=True)
    (out/'metro-regressions.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))
