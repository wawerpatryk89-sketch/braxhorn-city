"""Confirm the uploaded region exists before optional logs/cache can fail.
No build dispatch, release publication, deletion or credential printing.
"""
import hashlib,json,os,urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'mistrzowie-artifacts/manifest.json').read_text())
for part in manifest['parts']:
 p=ROOT/'mistrzowie-artifacts'/part['file']
 assert p.stat().st_size==part['bytes']
 h=hashlib.sha256()
 with p.open('rb') as f:
  for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
 assert h.hexdigest()==part['sha256']
aid=os.environ['REGION_ARTIFACT_ID'];assert aid.isdigit()
url='https://api.github.com/repos/'+os.environ['GITHUB_REPOSITORY']+'/actions/artifacts/'+aid
req=urllib.request.Request(url,headers={'Authorization':'Bearer '+os.environ['GH_TOKEN'],'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'})
with urllib.request.urlopen(req,timeout=60) as response:artifact=json.load(response)
assert artifact['id']==int(aid) and not artifact['expired'] and artifact['size_in_bytes']>0
assert artifact['workflow_run']['head_sha']==manifest['commit']
report=dict(status='ENCRYPTED_REGION_DELIVERY_CONFIRMED',commit=manifest['commit'],runId=manifest['runId'],attempt=manifest['attempt'],
 variant=manifest['variant'],artifactId=artifact['id'],archiveBytes=artifact['size_in_bytes'],archiveDigest=artifact.get('digest'),
 artifactUrl=os.environ['REGION_ARTIFACT_URL'],fileHashes=manifest['parts'],requiresGitHubAccess=True,
 secondaryRelease='NOT_CONFIGURED_WORKFLOW_CONTENTS_READ_ONLY',apk='NOT_BUILT')
(ROOT/'mistrzowie-evidence/delivery.json').write_text(json.dumps(report,indent=2))
with open(os.environ['GITHUB_STEP_SUMMARY'],'a') as f:
 f.write('## Miasto Mistrzów — pliki zabezpieczone\n\n')
 f.write('Scena i paczki Android; **to nie APK**. Commit `'+manifest['commit']+'`.\n\n')
 f.write('[Pobierz zaszyfrowany wynik]('+report['artifactUrl']+') — dostęp przez GitHub, retencja 30 dni.\n\n')
 f.write('Manifest w paczce zawiera rozmiary i SHA-256 części. Dodatkowe wydanie Release nie jest skonfigurowane; workflow ma uprawnienia contents: read.\n')
print(json.dumps(report))
