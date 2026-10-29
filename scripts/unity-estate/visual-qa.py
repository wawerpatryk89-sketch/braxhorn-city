#!/usr/bin/env python3
"""Actual WebGL visual QA: the Unity result must NOT be a magenta/pink screen.
Only captures pixels rendered by Unity's WebGL player; no generated mockups.
The original source geometry/GLBs/visual content are not edited."""
import asyncio
import http.server
import json
import os
import socketserver
import sys
import threading
from pathlib import Path

from PIL import Image
from playwright.async_api import async_playwright

ROOT = Path(os.environ.get("ESTATE_WEBGL_ROOT", "")).expanduser().resolve()
REPORT_DIR = Path(os.environ.get("ESTATE_QA_OUTPUT", "/tmp/estate-qa")).resolve()
REPORT_DIR.mkdir(parents=True, exist_ok=True)
PORT = 18976

class WebGLHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def guess_type(self, path):
        if path.endswith(".wasm.gz"):
            return "application/wasm"
        if path.endswith(".framework.js.gz"):
            return "application/javascript"
        if path.endswith(".data.gz"):
            return "application/octet-stream"
        return super().guess_type(path)

    def end_headers(self):
        if self.path.split("?")[0].startswith("/Build/") and self.path.split("?")[0].endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Access-Control-Allow-Origin", "*")
        super().end_headers()

    def log_message(self, fmt, *args):
        msg = fmt % args
        if " 404 " in msg or " 500 " in msg:
            print("HTTP ERROR", msg, flush=True)

class Server(socketserver.ThreadingTCPServer):
    allow_reuse_address = True

async def verify():
    if not (ROOT / "index.html").is_file():
        raise RuntimeError("A real Unity WebGL index.html was not found")
    print("Testing ORIGINAL Unity WebGL rendering:", ROOT, flush=True)
    server = Server(("127.0.0.1", PORT), WebGLHandler)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    result = {"loaded": False, "pink_ratio": None, "pixel_check": "NOT_EXECUTED", "warnings": [],
              "errors": [], "visualPass": False, "platform": "Chromium/SwiftShader WebGL, not iPhone"}
    try:
        async with async_playwright() as playwright:
            browser = await playwright.chromium.launch(
                headless=True,
                args=["--use-gl=angle", "--use-angle=swiftshader",
                      "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist",
                      "--enable-webgl", "--disable-dev-shm-usage"]
            )
            try:
                page = await browser.new_page(viewport={"width": 1280, "height": 720},
                                              device_scale_factor=1)
                scene_ready = asyncio.Event()
                result["runtimeSceneReady"] = False
                result["sourceModelCount"] = None
                result["runtimeSceneSummary"] = None
                def collect_console(msg):
                    message = msg.text
                    if msg.type in ("error", "warning"):
                        result["warnings"].append((msg.type + ": " + message)[:600])
                    # Emitted by ORIGINAL Unity code after all GLB imports, doors,
                    # vegetation AND AddOriginalBrax() have finished.
                    if "[Estate] Models " in message:
                        result["runtimeSceneSummary"] = message[:800]
                        match = __import__("re").search(
                            r"\[Estate\] Models (\d+)/21; doors (\d+)/24; collisions (\d+)", message)
                        if match:
                            result["sourceModelCount"] = int(match.group(1))
                            result["sourceDoorCount"] = int(match.group(2))
                            result["sourceCollisionCount"] = int(match.group(3))
                        result["runtimeSceneReady"] = True
                        scene_ready.set()
                page.on("console", collect_console)
                page.on("pageerror", lambda error: result["errors"].append(str(error)[:800]))
                page.on("requestfailed", lambda request: result["errors"].append(
                    "HTTP " + request.url + " " + str(request.failure)
                ))
                # Verify the original Brax binary remains gzip-compressed over HTTP.
                # Unity itself decompresses brax.glb.gz; applying Content-Encoding to
                # this asset silently corrupts the avatar in a browser.
                asset_response = await page.request.get(
                    f"http://127.0.0.1:{PORT}/StreamingAssets/Estate/brax.glb.gz",
                    timeout=30000)
                original = (ROOT / "StreamingAssets/Estate/brax.glb.gz").read_bytes()
                received = await asset_response.body()
                result["braxGzipTransportIntact"] = (
                    asset_response.status == 200 and received == original and
                    received[:2] == bytes([0x1f,0x8b]))
                if not result["braxGzipTransportIntact"]:
                    raise RuntimeError(
                        "Brax original GLB.gz was altered by HTTP transport; refuse screenshot PASS")
                await page.goto(f"http://127.0.0.1:{PORT}/",
                                wait_until="domcontentloaded", timeout=30000)
                try:
                    await page.wait_for_function("""() => {
                        const bar = document.querySelector('#unity-loading-bar');
                        return !!bar && bar.style.display === 'none';
                    }""", timeout=120000)
                    result["loaded"] = True
                    # Unity's loading bar only confirms engine startup, not the
                    # original estate. Wait for C# runtime completion log.
                    try:
                        await asyncio.wait_for(scene_ready.wait(), timeout=210)
                    except asyncio.TimeoutError:
                        result["errors"].append(
                            "Original estate never announced completion of 21 model slots")
                    await page.wait_for_timeout(3500)
                except Exception as error:
                    result["errors"].append("Unity player did not finish startup: " + str(error)[:500])

                screenshot = REPORT_DIR / "unity-estate-real-browser.png"
                await page.screenshot(path=str(screenshot), timeout=30000)
                box = await page.locator("#unity-canvas").bounding_box()
                if box is None:
                    result["errors"].append("Unity canvas missing")
                else:
                    img = Image.open(screenshot).convert("RGB")
                    x0 = max(0, round(box["x"])); y0 = max(0, round(box["y"]))
                    x1 = min(img.width, round(box["x"] + box["width"]))
                    y1 = min(img.height, round(box["y"] + box["height"]))
                    if x1 <= x0 or y1 <= y0:
                        result["errors"].append("Invalid canvas rectangle")
                    else:
                        pixels = img.crop((x0,y0,x1,y1))
                        total = pixels.width * pixels.height
                        magenta = sum(1 for red, green, blue in pixels.getdata()
                                      if red > 205 and blue > 175 and green < 105)
                        result["pink_ratio"] = round(magenta/total,5)
                        result["pixel_check"] = ("FAIL_MAGENTA" if magenta/total > .07
                                                 else "NO_MASS_MAGENTA")
                        print("ACTUAL UNITY CANVAS MAGENTA FRACTION:", result["pink_ratio"],flush=True)
                result["errors"] += (await page.locator("#unity-warning").inner_text()).splitlines()[:8] \
                    if await page.locator("#unity-warning").count() else []
                fatal = [x for x in result["warnings"] + result["errors"]
                         if any(token in x.lower() for token in
                         ["shader not found", "shader is missing", "webgl context lost",
                          "exception", "fatal", "failed to fetch", "out of memory"])]
                result["fatal"] = fatal[:12]
                result["visualPass"] = bool(result["loaded"] and
                   result["pixel_check"] == "NO_MASS_MAGENTA" and
                   result.get("braxGzipTransportIntact") is True and
                   result.get("runtimeSceneReady") is True and
                   result.get("sourceModelCount") == 19 and
                   result.get("sourceDoorCount") == 24 and
                   result.get("sourceCollisionCount") == 1013 and not fatal)
            finally:
                await browser.close()
    finally:
        server.shutdown()
        (REPORT_DIR / "visual-qa-report.json").write_text(
            json.dumps(result, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
        print(json.dumps(result, ensure_ascii=False, indent=2)[:7000], flush=True)
    if not result["visualPass"]:
        raise SystemExit("Actual Unity WebGL screenshot QA FAILED. NO PUBLIC PREVIEW PASS.")

if __name__ == "__main__":
    asyncio.run(verify())
