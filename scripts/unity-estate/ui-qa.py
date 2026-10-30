#!/usr/bin/env python3
"""Test the ACTUAL compiled BRAXHORN Unity WebGL HTML + scene + both-source UI flow.
Not an iPhone/WebKit acceptance test. Fails closed; keeps real screenshots."""
import asyncio
import http.server
import json
import os
import re
import socketserver
import threading
from pathlib import Path
from PIL import Image
from playwright.async_api import async_playwright

ROOT = Path(os.environ["ESTATE_WEBGL_ROOT"]).resolve()
OUT = Path(os.environ.get("ESTATE_QA_OUTPUT", "/tmp/estate-ui-qa")).resolve()
OUT.mkdir(parents=True, exist_ok=True)
PORT = 18977

class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)
    def guess_type(self, path):
        if path.endswith(".wasm.gz"): return "application/wasm"
        if path.endswith(".framework.js.gz"): return "application/javascript"
        if path.endswith(".data.gz"): return "application/octet-stream"
        return super().guess_type(path)
    def end_headers(self):
        name = self.path.split("?")[0]
        if name.startswith("/Build/") and name.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Access-Control-Allow-Origin", "*")
        super().end_headers()
    def log_message(self, fmt, *args):
        if " 404 " in (fmt % args) or " 500 " in (fmt % args):
            print("HTTP:", fmt % args, flush=True)

class Server(socketserver.ThreadingTCPServer):
    allow_reuse_address = True

async def visual_ratio(page, image_path):
    await page.screenshot(path=str(image_path), timeout=30000)
    box = await page.locator("#unity-canvas").bounding_box()
    if not box: raise AssertionError("Unity source canvas absent")
    img = Image.open(image_path).convert("RGB")
    x0, y0 = max(0,round(box["x"])), max(0,round(box["y"]))
    x1, y1 = min(img.width,round(box["x"]+box["width"])), min(img.height,round(box["y"]+box["height"]))
    pix = img.crop((x0,y0,x1,y1))
    magenta = sum(1 for r,g,b in pix.getdata() if r>205 and b>175 and g<105)
    return round(magenta/max(1,pix.width*pix.height),6)

async def run():
    if not (ROOT/"index.html").is_file(): raise AssertionError("Missing compiled index")
    if not (ROOT/"StreamingAssets/Estate/nyxa.glb.gz").is_file():
        raise AssertionError("Original Nyxa GLB missing from Unity build")
    server = Server(("127.0.0.1", PORT), Handler)
    threading.Thread(target=server.serve_forever,daemon=True).start()
    result = {
        "unity6": True, "realBrowser": "Chromium SwiftShader WebGL",
        "screenshotIsSynthetic": False, "iPhoneTested": False,
        "startScreen": False, "selectScreen": False,
        "nyxaSelected": False, "nyxaLoaded": False,
        "controlsPresent": False, "pauseWorks": False,
        "sceneLoaded": False, "models": None, "doors": None,
        "colliders": None, "pinkFraction": None, "visualPass": False,
        "errors": [], "warnings": []
    }
    try:
        async with async_playwright() as pw:
            browser = await pw.chromium.launch(
                headless=True, args=["--use-gl=angle","--use-angle=swiftshader",
                    "--enable-unsafe-swiftshader","--ignore-gpu-blocklist",
                    "--enable-webgl","--disable-dev-shm-usage"])
            try:
                page = await browser.new_page(viewport={"width":1280,"height":720},
                    device_scale_factor=1, is_mobile=True, has_touch=True)
                def console(msg):
                    line=msg.text
                    if msg.type=="error": result["errors"].append(line[:700])
                    if msg.type=="warning": result["warnings"].append(line[:700])
                    m=re.search(r"\[Estate\] Models (\d+)/21; doors (\d+)/24; collisions (\d+)",line)
                    if m:
                        result["models"],result["doors"],result["colliders"]=map(int,m.groups())
                        result["sceneLoaded"]=True
                    if "[Estate] Original nyxa source model and WebP textures loaded." in line:
                        result["nyxaLoaded"]=True
                page.on("console",console)
                page.on("pageerror",lambda e:result["errors"].append("JS: "+str(e)[:700]))
                page.on("requestfailed",lambda req:result["errors"].append("NETWORK: "+req.url))
                # Asset binary must not be transparently decompressed in HTTP.
                response=await page.request.get(
                    f"http://127.0.0.1:{PORT}/StreamingAssets/Estate/nyxa.glb.gz",
                    timeout=30000)
                expected=(ROOT/"StreamingAssets/Estate/nyxa.glb.gz").read_bytes()
                received=await response.body()
                if response.status!=200 or received!=expected or received[:2]!=bytes([31,139]):
                    raise AssertionError("Original Nyxa gzip binary corrupted by HTTP transfer")
                await page.goto(f"http://127.0.0.1:{PORT}/",
                    wait_until="domcontentloaded",timeout=30000)
                await page.wait_for_function(
                    "() => !!window.__brxEstateUnity?.sourceReady && !!window.__brxEstateUnity?.unity",
                    timeout=190000)
                await page.wait_for_timeout(1500)
                result["startScreen"]=await page.locator("#title").is_visible()
                result["startTitle"]=(await page.locator("#title").inner_text())[:250]
                await visual_ratio(page,OUT/"01-title-original-ui.png")
                if not result["startScreen"]: raise AssertionError("Original GRAJ title never appeared")
                await page.locator("#btnPlay").click(timeout=12000)
                result["selectScreen"]=await page.locator("#select").is_visible()
                await page.locator('#heroCards .hero[data-k="nyxa"]').click(timeout=12000)
                result["nyxaSelected"]=await page.evaluate(
                    "() => window.__brxEstateUnity?.hero === 'nyxa'")
                await visual_ratio(page,OUT/"02-original-hero-select.png")
                if not result["selectScreen"] or not result["nyxaSelected"]:
                    raise AssertionError("Original hero selection did not work")
                await page.locator("#btnFight").click(timeout=15000)
                await page.wait_for_function(
                    "() => window.__brxEstateUnity?.playing === true && document.body.classList.contains('playing')",
                    timeout=130000)
                await page.wait_for_timeout(2500)
                result["controlsPresent"]=all([
                    await page.locator("#touch").is_visible(),
                    await page.locator("#joy").count()==1,
                    await page.locator("#touch [data-act='punch']").count()==1,
                    await page.locator("#touch [data-act='kick']").count()==1,
                    await page.locator("#touch [data-act='jump']").count()==1,
                    await page.locator("#touch [data-act='act']").count()==1,
                    await page.locator("#touch [data-hold='block']").count()==1])
                result["pinkFraction"]=await visual_ratio(page,OUT/"03-nyxa-actual-unity.png")
                await page.locator("#bMenu").click(timeout=12000)
                result["pauseWorks"]=await page.locator("#pause").is_visible()
                await page.locator("#bResume").click(timeout=12000)
                result["resumeWorks"]=not await page.locator("#pause").is_visible()
                result["visualPass"]=bool(
                    result["startScreen"] and result["selectScreen"] and
                    result["nyxaSelected"] and result["nyxaLoaded"] and
                    result["sceneLoaded"] and result["models"]==19 and
                    result["doors"]==24 and result["colliders"]==1013 and
                    result["controlsPresent"] and result["pauseWorks"] and
                    result["resumeWorks"] and result["pinkFraction"]<.07 and
                    not any(("exception" in e.lower() or "shader" in e.lower()
                        or "webgl context lost" in e.lower()) for e in result["errors"]))
            finally:
                await browser.close()
    except Exception as e:
        result["errors"].append("QA FAILED: "+str(e)[:900])
    finally:
        server.shutdown()
        (OUT/"ui-qa.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n")
        print(json.dumps(result,ensure_ascii=False,indent=2)[:8000],flush=True)
    if not result["visualPass"]:
        raise SystemExit("SOURCE UNITY UI QA FAILED. Do not publish this build.")

if __name__=="__main__": asyncio.run(run())
