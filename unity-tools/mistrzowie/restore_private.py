"""Restore only the existing owner palm package; missing source must fail this build."""
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
