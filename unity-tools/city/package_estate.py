"""Package original Unity city player with ESTATE as first scene. No controller or material rewrites."""
import runpy,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
runpy.run_path(str(root/'unity-tools/city/package.py'),run_name='__main__')
p=root/'unity-preview/index.html'
s=p.read_text()
s=s.replace('<title>BRAXHORN • Miasto Unity</title>','<title>BRAXHORN • Osiedle — oryginalny Brax</title>')
s=s.replace('ORYGINALNE MIASTO • UNITY','ORYGINALNE OSIEDLE • UNITY')
s=s.replace('Twoje miasto.<br>Twoje zasady.','Osiedle domków.<br>Ten sam Brax.')
s=s.replace('WEJDŹ DO MIASTA','WEJDŹ NA OSIEDLE')
s=s.replace('CITY<br>UNITY / MIASTO','ESTATE<br>UNITY / OSIEDLE')
s=s.replace('Brax. Siłownia. Miasto. Osiedle.','Oryginalny Brax i identyczne sterowanie z miasta. Inna plansza.')
s=s.replace('Wczytuję oryginalne miasto i Braxa…','Wczytuję osiedle i oryginalnego Braxa…')
s=s.replace('Siłownia → ulica → osiedle. Kamerę obracasz sam.','Osiedle domków. Ten sam joystick i kamera co w mieście.')
p.write_text(s)
assert 'WEJDŹ NA OSIEDLE' in s and "BraxSend" in s
print('ESTATE_PREVIEW_USES_EXACT_CITY_PLAYER_AND_UI',p.stat().st_size)
