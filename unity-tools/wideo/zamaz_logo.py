#!/usr/bin/env python3
"""Blur real brand marks in an owner's video (the game shows only the BRAXHORN brand).

usage: zamaz_logo.py <in.mp4> <tracks.json> <out.mp4> [--podglad dir]

tracks.json: {"name": {"0.0": [x0, y0, x1, y1], "0.5": [...], ...}, ...} in the video's pixels; the keys are seconds
(or "f<n>" = frame n, exact when the anchors were read off frames picked by index).
Between two anchors the box follows the picture: dense optical flow (Farneback) carried forward from the earlier anchor
and backward from the later one, blended by time (so a hand-placed anchor every ~0.5 s is enough even while the camera
walks and pans). Before the first and after the last anchor the box is carried on for up to 0.5 s or until it leaves the
frame. Each box is padded, pixelated and blurred under a feathered rounded mask - the mark is unreadable, the picture
stays calm. The sound is copied from the input. With --podglad every 10th frame is saved with the boxes drawn.
"""
import json
import subprocess
import sys
from pathlib import Path

import cv2
import numpy as np

PAD = 7
EXT = .5


def flows(grays):
    fw, bw = [], []
    for a, b in zip(grays[:-1], grays[1:]):
        args = (None, .5, 3, 21, 3, 5, 1.1, 0)
        fw.append(cv2.calcOpticalFlowFarneback(a, b, *args))
        bw.append(cv2.calcOpticalFlowFarneback(b, a, *args))
    return fw, bw


def shift(box, flow, W, H):
    x0, y0, x1, y1 = box
    cx0, cy0, cx1, cy1 = max(0, int(x0)), max(0, int(y0)), min(W, int(x1)), min(H, int(y1))
    if cx1 - cx0 < 3 or cy1 - cy0 < 3:
        return None
    if (cx1 - cx0) * (cy1 - cy0) < .12 * (x1 - x0) * (y1 - y0):
        return None
    f = flow[cy0:cy1, cx0:cx1].reshape(-1, 2)
    dx, dy = np.median(f[:, 0]), np.median(f[:, 1])
    return [x0 + dx, y0 + dy, x1 + dx, y1 + dy]


def carry(box, a, b, fw, bw, W, H):
    """the box anchored at frame a carried to frame b (either direction); None once it left the picture"""
    out = {a: box}
    cur = box
    step = 1 if b > a else -1
    for f in range(a, b, step):
        cur = shift(cur, fw[f] if step > 0 else bw[f - 1], W, H) if cur else None
        out[f + step] = cur
    return out


def boxes_for(track, n, fps, fw, bw, W, H):
    anchors = sorted((min(n - 1, int(t[1:]) if t.startswith('f') else int(round(float(t) * fps))), [float(v) for v in b])
                     for t, b in track.items())
    res = {}
    for (a, ba), (b, bb) in zip(anchors[:-1], anchors[1:]):
        f1, f2 = carry(ba, a, b, fw, bw, W, H), carry(bb, b, a, fw, bw, W, H)
        for f in range(a, b + 1):
            w = (f - a) / max(1, b - a)
            p, q = f1.get(f), f2.get(f)
            size = [(1 - w) * (ba[2] - ba[0]) + w * (bb[2] - bb[0]), (1 - w) * (ba[3] - ba[1]) + w * (bb[3] - bb[1])]
            if p and q:
                cx = (1 - w) * (p[0] + p[2]) / 2 + w * (q[0] + q[2]) / 2
                cy = (1 - w) * (p[1] + p[3]) / 2 + w * (q[1] + q[3]) / 2
            elif p or q:
                r = p or q
                cx, cy = (r[0] + r[2]) / 2, (r[1] + r[3]) / 2
            else:
                cx = (1 - w) * (ba[0] + ba[2]) / 2 + w * (bb[0] + bb[2]) / 2
                cy = (1 - w) * (ba[1] + ba[3]) / 2 + w * (bb[1] + bb[3]) / 2
            res[f] = [cx - size[0] / 2, cy - size[1] / 2, cx + size[0] / 2, cy + size[1] / 2]
    ext = int(EXT * fps)
    a, ba = anchors[0]
    for f, bx in carry(ba, a, max(0, a - ext), fw, bw, W, H).items():
        if bx: res.setdefault(f, bx)
    b, bb = anchors[-1]
    for f, bx in carry(bb, b, min(n - 1, b + ext), fw, bw, W, H).items():
        if bx: res.setdefault(f, bx)
    return res


def blur(img, box):
    H, W = img.shape[:2]
    x0, y0, x1, y1 = box[0] - PAD, box[1] - PAD, box[2] + PAD, box[3] + PAD
    m = int(max(x1 - x0, y1 - y0) * .35) + 8
    X0, Y0, X1, Y1 = max(0, int(x0) - m), max(0, int(y0) - m), min(W, int(x1) + m), min(H, int(y1) + m)
    if X1 - X0 < 4 or Y1 - Y0 < 4:
        return
    roi = img[Y0:Y1, X0:X1].astype(np.float32)
    h = max(8., min(x1 - x0, y1 - y0))
    k = max(4, int(h / 5))
    small = cv2.resize(roi, (max(1, (X1 - X0) // k), max(1, (Y1 - Y0) // k)), interpolation=cv2.INTER_AREA)
    soft = cv2.resize(small, (X1 - X0, Y1 - Y0), interpolation=cv2.INTER_LINEAR)
    soft = cv2.GaussianBlur(soft, (0, 0), max(3., h / 4))
    mask = np.zeros((Y1 - Y0, X1 - X0), np.float32)
    r = int(min(x1 - x0, y1 - y0) * .3)
    cv2.rectangle(mask, (int(x0) - X0 + r, int(y0) - Y0), (int(x1) - X0 - r, int(y1) - Y0), 1., -1)
    cv2.rectangle(mask, (int(x0) - X0, int(y0) - Y0 + r), (int(x1) - X0, int(y1) - Y0 - r), 1., -1)
    for cx, cy in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)):
        cv2.circle(mask, (int(cx) - X0, int(cy) - Y0), r, 1., -1)
    mask = cv2.GaussianBlur(mask, (0, 0), max(2., h / 7))[..., None]
    img[Y0:Y1, X0:X1] = np.clip(roi * (1 - mask) + soft * mask, 0, 255).astype(np.uint8)


def main():
    src, tracks_path, out = sys.argv[1], sys.argv[2], sys.argv[3]
    podglad = Path(sys.argv[sys.argv.index('--podglad') + 1]) if '--podglad' in sys.argv else None
    tracks = json.loads(Path(tracks_path).read_text())
    cap = cv2.VideoCapture(src)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30.
    n = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    W, H = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH)), int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    # the optical flow only over the frames the tracks need (memory: a 20 s 720p clip would not fit whole)
    keys = [min(n - 1, int(t[1:]) if t.startswith('f') else int(round(float(t) * fps))) for tr in tracks.values() for t in tr]
    lo, hi = max(0, min(keys) - int(EXT * fps) - 1), min(n - 1, max(keys) + int(EXT * fps) + 1)
    grays = {}
    for i in range(hi + 1):
        ok, fr = cap.read()
        if not ok: break
        if i >= lo: grays[i] = cv2.cvtColor(fr, cv2.COLOR_BGR2GRAY)
    cap.release()
    idx = sorted(grays)
    fwl, bwl = flows([grays[i] for i in idx])
    fw = {i: f for i, f in zip(idx[:-1], fwl)}
    bw = {i: f for i, f in zip(idx[:-1], bwl)}
    per_frame = [[] for _ in range(n)]
    for name, tr in tracks.items():
        for f, bx in boxes_for(tr, n, fps, fw, bw, W, H).items():
            per_frame[f].append((name, bx))
    if podglad: podglad.mkdir(parents=True, exist_ok=True)
    enc = subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "bgr24", "-s", "%dx%d" % (W, H),
                            "-r", "%.6f" % fps, "-i", "-", "-i", src, "-map", "0:v", "-map", "1:a?", "-c:v", "libx264",
                            "-profile:v", "high", "-pix_fmt", "yuv420p", "-preset", "veryfast", "-crf", "20",
                            "-c:a", "aac", "-b:a", "96k", "-movflags", "+faststart", "-shortest", out], stdin=subprocess.PIPE)
    cap = cv2.VideoCapture(src)
    for i in range(n):
        ok, fr = cap.read()
        if not ok: break
        for _, bx in per_frame[i]:
            blur(fr, bx)
        enc.stdin.write(fr.tobytes())
        if podglad and i % 10 == 0:
            v = fr.copy()
            for name, bx in per_frame[i]:
                cv2.rectangle(v, (int(bx[0]), int(bx[1])), (int(bx[2]), int(bx[3])), (0, 0, 255), 1)
                cv2.putText(v, name, (int(bx[0]), int(bx[1]) - 2), 0, .35, (0, 0, 255), 1)
            cv2.putText(v, "%d t=%.2f" % (i, i / fps), (8, H - 8), 0, .5, (0, 255, 255), 1)
            cv2.imwrite(str(podglad / ("%04d.jpg" % i)), v)
    enc.stdin.close()
    enc.wait()
    print("ZAMAZ_LOGO %s frames=%d boxes=%d -> %s" % (Path(src).name, n, sum(len(p) for p in per_frame), out))


if __name__ == "__main__":
    main()
