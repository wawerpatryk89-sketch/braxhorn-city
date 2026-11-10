"""Restore the owner's private packages the region uses (encrypted in unity-city/Prywatne, key only in CI).

miasto_palmy + miasto_silownia_metro are required (missing = this build fails: no substitute palms).
miasto_sklepy (owner 10.11: "budki wyposażyć w kebaby, w pizzę ... tam mamy w katalogu ... wszystko jest") is the city's
shop pack: only the models the promenade kiosks use are extracted (the whole pack is ~32 MB of supermarket goods); every
member name is listed in the log. If it cannot be restored the kiosks stay without those models and say so in the log.
osiedle_wnetrza (owner 10.11 22:31: "asortyment alkoholu oryginał z mojej biblioteki - na osiedlu w dyskotece jest") is
the estate disco's interior pack: E1's bar takes his back-bar units with bottles, whisky and wine, leather bar stools, bar
pendants and glasses from it (BraxChampionsE1Premium). Only those models are extracted; without the pack E1 builds its
stand-ins and the log says so.
"""
from pathlib import Path
import importlib.util,os
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('owner_private',ROOT/'unity-tools/claude/prywatne.py')
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
if not p.klucz():raise RuntimeError('Missing repository source key; no substitute palms allowed')
for package in ['miasto_palmy','miasto_silownia_metro']:
 tar=p.odszyfruj(package)
 if tar is None:raise RuntimeError('Existing '+package+' package cannot be restored')
 with tar:
  count=p.rozpakuj(tar,p.CEL/'Miasto'/package)
 if count<1:raise RuntimeError('Empty existing package: '+package)
 print('BRAX_MISTRZOWIE_PRIVATE_OK package='+package+' files='+str(count))

# Promenade kiosk equipment (BraxChampionsPromenadeEquipment): food, bottles, fridges, tills, menu boards.
KIOSK_MODELS=['kebab','szawarma','pizza','hamburger','frytki','kurczak','losos','owoce_skrzynka','lodowka','kasa',
              'menu_2','menu_3','menu_4']+['but_%02d'%i for i in range(1,19)]


def wanted(name):
 n=name[2:] if name.startswith('./') else name
 return any(n==m+'.glb' or n.startswith('_tekstury/'+m+'/') for m in KIOSK_MODELS)


tar=p.odszyfruj('miasto_sklepy')
if tar is None:
 print('::warning::BRAX_MISTRZOWIE_PRIVATE_MISSING package=miasto_sklepy - promenade kiosks without the owner food models')
else:
 with tar:
  members=[m for m in tar.getmembers() if m.isfile()]
  print('BRAX_MISTRZOWIE_PRIVATE_LIST miasto_sklepy '+' '.join(sorted(m.name for m in members if m.name.endswith('.glb'))))
  cel=(p.CEL/'Miasto'/'miasto_sklepy').resolve();count=0
  for m in members:
   if not wanted(m.name):continue
   dst=(cel/m.name).resolve()
   if cel not in dst.parents:continue
   dst.parent.mkdir(parents=True,exist_ok=True)
   with tar.extractfile(m) as src,open(dst,'wb') as out:out.write(src.read())
   count+=1
  got=sorted(n for n in KIOSK_MODELS if (cel/(n+'.glb')).exists())
  print('BRAX_MISTRZOWIE_PRIVATE_OK package=miasto_sklepy files='+str(count)+' models='+','.join(got)
        +' missing='+','.join(n for n in KIOSK_MODELS if n not in got))


# E1 bar (BraxChampionsE1Premium): the owner's own models from the estate disco pack, CC BY (Assets/Source/Mistrzowie/E1/CREDITS.md)
E1_MODELS=['bar_polki_butelki_windupbird','bar_butelki_helyeouka','bar_hoker_skora_nosteam','bar_lampa_markpeters','bar_szklo_saandy']


def wanted_e1(name):
 n=name[2:] if name.startswith('./') else name
 return any(n==m+'.glb' or n.startswith('_tekstury/'+m+'/') for m in E1_MODELS)


tar=p.odszyfruj('osiedle_wnetrza')
if tar is None:
 print('::warning::BRAX_MISTRZOWIE_PRIVATE_MISSING package=osiedle_wnetrza - E1 bar with stand-ins instead of the owner models')
else:
 with tar:
  members=[m for m in tar.getmembers() if m.isfile()]
  print('BRAX_MISTRZOWIE_PRIVATE_LIST osiedle_wnetrza '+' '.join(sorted(m.name for m in members if m.name.endswith('.glb'))))
  cel=(p.CEL/'Miasto'/'osiedle_wnetrza').resolve();count=0
  for m in members:
   if not wanted_e1(m.name):continue
   dst=(cel/m.name).resolve()
   if cel not in dst.parents:continue
   dst.parent.mkdir(parents=True,exist_ok=True)
   with tar.extractfile(m) as src,open(dst,'wb') as out:out.write(src.read())
   count+=1
  got=sorted(n for n in E1_MODELS if (cel/(n+'.glb')).exists())
  print('BRAX_MISTRZOWIE_PRIVATE_OK package=osiedle_wnetrza files='+str(count)+' models='+','.join(got)
        +' missing='+','.join(n for n in E1_MODELS if n not in got))
