#!/usr/bin/env python3
"""Create a review-only gallery from real Unity MP4 and PNG test artifacts."""
import hashlib
import html
import json
import os
from pathlib import Path
import shutil

source = Path("tower-district-qa")
target = Path("tower-gallery")
sha = os.environ.get("GITHUB_SHA", "")
report = json.loads((source / "report.json").read_text())
checks = report.get("checks", [])
if not (len(sha) == 40 and report.get("sourceCommit") == sha):
    raise RuntimeError("Cannot publish mismatched Unity evidence")
if report.get("errors") or len([x for x in checks if x.get("name", "").startswith("Real office scene anchor ") and x.get("passed")]) != 8:
    raise RuntimeError("Unity evidence incomplete; no gallery will be published")
video = source / "braxhorn-eight-real-offices.mp4"
if not video.is_file() or video.stat().st_size < 100000:
    raise RuntimeError("No actual Unity MP4; refuse image-only fake gallery")
images = sorted(source.glob("??-office-*.png"))
if len(images) != 8 or any(p.stat().st_size < 10000 for p in images):
    raise RuntimeError("Eight actual Unity screenshots required")
target.mkdir(exist_ok=True)
for file in [video, *images]:
    shutil.copyfile(file, target / file.name)
cards = "".join(
    f'<figure><a href="{html.escape(p.name)}"><img loading="lazy" src="{html.escape(p.name)}" alt="Unity: {html.escape(p.stem)}"></a>'
    f'<figcaption>{html.escape(p.stem.split("-office-")[-1])}</figcaption></figure>'
    for p in images
)
page = f"""<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><meta name="robots" content="noindex,nofollow"><title>BRAXHORN CITY — wieżowce, prawdziwy test Unity</title>
<style>html,body{{margin:0;background:#0b0e0a;color:#f4f6f1;font:16px system-ui}}main{{max-width:1100px;margin:auto;padding:18px}}h1{{font-size:clamp(22px,4vw,34px)}}p{{line-height:1.5;color:#d1d9cc}}video{{width:100%;aspect-ratio:16/9;background:#000;border-radius:10px}}a{{color:#a8f76d}}small{{color:#aab2a5}}.shots{{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:12px;margin-top:24px}}.shots img{{width:100%;border-radius:8px}}figure{{margin:0}}figcaption{{font-size:13px;padding:5px}}</style></head>
<body><main><h1>BRAXHORN CITY — osiem wieżowców, przegląd Unity</h1><p>Prawdziwe nagranie Unity 6 z testu WebGL i osiem rzeczywistych ujęć lobby biurowych. To przegląd prac, nie potwierdzenie zakończenia ani odbiór na fizycznym telefonie. Oryginalny Brax i źródłowe materiały projektu.</p>
<video controls playsinline preload="metadata" src="braxhorn-eight-real-offices.mp4"></video><p><a href="braxhorn-eight-real-offices.mp4" download>Pobierz nagranie MP4</a></p><small>TEST · źródło {sha[:12]} · produkcja niezmieniona · wejścia i pełne piętra wymagają dalszej kontroli</small><section class="shots">{cards}</section></main></body></html>"""
(target / "index.html").write_text(page)
(target / "robots.txt").write_text("User-agent: *\nDisallow: /\n")
(target / "vercel.json").write_text(json.dumps({"framework": None, "buildCommand": "", "installCommand": "", "outputDirectory": "."}))
(target / "evidence.json").write_text(json.dumps({"sourceCommit": sha, "actualUnityRuntime": True, "screenshots": len(images), "videoSha256": hashlib.sha256(video.read_bytes()).hexdigest(), "visualAccepted": False, "physicalPhoneAccepted": False}, indent=2))
print("TOWER_REAL_UNITY_GALLERY_READY", sha, len(images), video.stat().st_size)
