"""Reuse only the existing estate arena graphics; do not rebuild the estate."""
from pathlib import Path
import importlib.util
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'unity-tools/claude/tekstury_b5.py'
spec=importlib.util.spec_from_file_location('existing_estate_graphics',source)
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
out=ROOT/'unity-city/Assets/Source/Mistrzowie/ArenaGraphics';out.mkdir(parents=True,exist_ok=True)
for function,name in [('octagon_mat','oktagon_mata'),('octagon_apron','oktagon_fartuch'),('chain_link','siatka_oktagonu'),('octagon_banner','baner_oktagon'),('brand_sign','braxhorn_arena'),('arena_sign','hala_walk')]:
 getattr(module,function)(out/(name+'.png'))
print('Original estate ring graphics restored: 6')
