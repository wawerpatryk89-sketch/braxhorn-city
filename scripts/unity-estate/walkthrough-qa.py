#!/usr/bin/env python3
"""Check actual Unity6 walkthrough with the original Brax on an iPhone-size browser."""
import asyncio, http.server, json, os, socketserver, threading
from pathlib import Path
from playwright.async_api import async_playwright
ROOT=Path(os.environ["ESTATE_WEBGL_ROOT"]).resolve()
OUT=Path(os.environ.get("ESTATE_QA_OUTPUT","/tmp/estate-walk-qa")).resolve()
OUT.mkdir(parents=True,exist_ok=True)
class Handler(http.server.SimpleHTTPRequestHandler):
 def __init__(self,*a,**k): super().__init__(*a,directory=str(ROOT),**k)
 def guess_type(self,path):
  if path.endswith(".wasm.gz"): return "application/wasm"
  if path.endswith(".framework.js.gz"): return "application/javascript"
  if path.endswith(".data.gz"): return "application/octet-stream"
  return super().guess_type(path)
 def end_headers(self):
  if self.path.split("?")[0].startswith("/Build/") and self.path.split("?")[0].endswith(".gz"):
   self.send_header("Content-Encoding","gzip")
  super().end_headers()
 def log_message(self,*args): pass
class Server(socketserver.ThreadingTCPServer): allow_reuse_address=True
async def main():
 assert (ROOT/"index.html").is_file()
 server=Server(("127.0.0.1",18986),Handler)
 threading.Thread(target=server.serve_forever,daemon=True).start()
 report={"passed":False,"errors":[],"platform":"Chromium emulated iPhone, NOT physical iPhone"}
 try:
  async with async_playwright() as p:
   browser=await p.chromium.launch(headless=True,args=["--use-gl=angle","--use-angle=swiftshader","--enable-unsafe-swiftshader","--ignore-gpu-blocklist","--enable-webgl"])
   try:
    page=await browser.new_page(viewport={"width":844,"height":390},is_mobile=True,has_touch=True,device_scale_factor=1,user_agent="Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1")
    page.on("pageerror",lambda e:report["errors"].append(str(e)[:500]))
    page.on("console",lambda m:report["errors"].append(m.text[:500]) if m.type=="error" else None)
    await page.goto("http://127.0.0.1:18986/",wait_until="domcontentloaded",timeout=30000)
    await page.wait_for_function("() => window.__brxEstateUnity?.playing === true && window.__brxEstateUnity?.sourceReady === true && !!window.__brxEstateUnity?.unity",timeout=260000)
    state=await page.evaluate("""() => ({
      hero:window.__brxEstateUnity.hero,
      playing:window.__brxEstateUnity.playing,
      sourceReady:window.__brxEstateUnity.sourceReady,
      walkDispatched:window.__brxEstateUnity.walkDispatched,
      heroName:document.getElementById('meName')?.textContent,
      selectorHidden:document.getElementById('select')?.hidden,
      canvas:!!document.getElementById('unity-canvas'),
      doorActionVisible:!document.getElementById('btnAct')?.hidden,
      combatButtonsHidden:[...document.querySelectorAll('#touch [data-act="punch"],#touch [data-act="kick"],#touch [data-act="special"],#touch [data-act="throw"],#touch [data-act="dodge"],#touch [data-hold="block"]')].every(x=>x.style.display==="none")
    })""")
    report["state"]=state
    # Verify joystick and camera messages really reach the active Unity instance.
    await page.evaluate("""() => {
      window.__walkInputMessages=[];
      const unity=window.__brxEstateUnity.unity;
      const original=unity.SendMessage.bind(unity);
      unity.SendMessage=(...args)=>{window.__walkInputMessages.push(args);return original(...args)};
    }""")
    stick=await page.locator("#joy").bounding_box()
    assert stick is not None, "Fixed joystick is not visible"
    sx,sy=stick["x"]+stick["width"]/2,stick["y"]+stick["height"]/2
    await page.mouse.move(sx,sy)
    await page.mouse.down()
    await page.mouse.move(sx+40,sy-34,steps=8)
    await page.wait_for_timeout(500)
    await page.mouse.up()
    await page.mouse.move(650,310)
    await page.mouse.down()
    await page.mouse.move(710,270,steps=6)
    await page.mouse.up()
    inputs=await page.evaluate("() => window.__walkInputMessages")
    report["inputMessages"]=[x for x in inputs if x[1] in ("OnWebJoystick","OnWebLook")][:12]
    report["inputPass"]=(
      any(x[0]=="BRAXHORN_ESTATE_ORIGINAL_MIGRATION" and x[1]=="OnWebJoystick" and x[2].endswith(",1") for x in inputs)
      and any(x[0]=="BRAXHORN_ESTATE_ORIGINAL_MIGRATION" and x[1]=="OnWebLook" for x in inputs)
    )
    await page.screenshot(path=str(OUT/"estate-brax-walk-iphone-emulation.png"))
    report["passed"]=bool(state["hero"]=="brax" and state["playing"] and state["sourceReady"] and state["walkDispatched"] and state["heroName"]=="Brax" and state["selectorHidden"] and state["canvas"] and state["doorActionVisible"] and state["combatButtonsHidden"] and report["inputPass"] and not report["errors"])
   finally: await browser.close()
 finally:
  server.shutdown()
  (OUT/"walkthrough-qa.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n")
  print(json.dumps(report,ensure_ascii=False,indent=2))
 if not report["passed"]: raise SystemExit("Original Brax walkthrough QA FAILED")
if __name__=="__main__": asyncio.run(main())
