#!/usr/bin/env python3
"""Cross-plane V8 engine loops for the owner's car (owner 9.11: "zrób mi V8").

No CC0 V8 recording is reachable from the build environment (Wikimedia rate
limit, Freesound login), so the sound is SYNTHESISED from the engine itself,
not drawn by ear:
  * 4-stroke V8: 8 firings per 2 crank revolutions, firing order 1-8-4-3-6-5-7-2,
    GM banks (odd cylinders left, even right). The cross-plane crank gives each
    bank UNEVEN intervals - that is the V8 "burble".
  * each firing = exhaust pressure pulse into its bank pipe; pipe and muffler
    resonances (quarter-wave of ~2.2 m hot pipe, ~500 m/s) as resonators;
    the two banks meet in an X-pipe with 0.6 ms path difference;
  * small cycle-to-cycle variation (combustion), cam lope at idle, intake hiss.
Three seamless loops at fixed RPM; the game pitches/crossfades them by RPM.
Usage: synth_v8_engine.py  (writes Assets/Source/Mistrzowie/Audio/BraxCar/v8_*.ogg)
"""
from pathlib import Path
import subprocess
import tempfile
import wave

import numpy as np
from scipy.signal import butter, iirpeak, lfilter, sosfilt

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "unity-city/Assets/Source/Mistrzowie/Audio/BraxCar"
SR = 44100
FIRING_ORDER = [1, 8, 4, 3, 6, 5, 7, 2]
LOOPS = {"v8_idle_850": 850, "v8_low_2200": 2200, "v8_high_4800": 4800}


def synth(rpm, seconds=4.0, seed=7):
    rng = np.random.default_rng(seed + rpm)
    cycle = 120.0 / rpm                       # two revolutions
    cycles = max(4, int(round(seconds / cycle)))
    n = int(round(cycles * cycle * SR))
    pad = int(.25 * SR)
    banks = np.zeros((2, n + pad))
    # exhaust pulse: fast rise, decay shortening with rpm
    tau = np.interp(rpm, [800, 5000], [.0060, .0022])
    pl = int(8 * tau * SR)
    t = np.arange(pl) / SR
    pulse = (1 - np.exp(-t / .00025)) * np.exp(-t / tau)
    lope = 1 + (.22 if rpm < 1200 else .0) * np.sin(2 * np.pi * np.arange(cycles) / 3.0)
    for c in range(cycles):
        for k, cyl in enumerate(FIRING_ORDER):
            when = c * cycle + k * cycle / 8 + rng.normal(0, .00004)
            i = int(when * SR)
            # small fixed cylinder-to-cylinder differences (real engines)
            amp = lope[c] * (1 + .06 * np.sin(cyl * 1.7)) * (1 + rng.normal(0, .04))
            bank = 0 if cyl % 2 else 1
            seg = banks[bank, i:i + pl]
            seg += amp * pulse[:len(seg)]
    # bank pipes: quarter-wave resonances of the hot exhaust + muffler body
    out = np.zeros(n + pad)
    # Dual exhaust heard from behind the car: the far bank is quieter and its
    # pipe 0.5 ms longer -> the cross-plane bank pattern stays audible (burble).
    for b, (f1, delay, gain) in enumerate(((115.0, 0, 1.0), (121.0, int(.0005 * SR), .74))):
        x = banks[b]
        y = np.zeros_like(x)
        # broad (low-Q) pipe/muffler resonances colour the sound but must not
        # override the firing frequency (measured: Q 3.5 made 120 Hz dominate)
        for mult, q, g in ((1, 1.4, .55), (3, 1.8, .30), (5, 2.2, .16)):
            bb, aa = iirpeak(min(f1 * mult, SR / 2 - 100), q, fs=SR)
            y += g * lfilter(bb, aa, x)
        y += 1.0 * x                           # direct pressure wave dominates
        out[delay:] += gain * y[:len(y) - delay]
    # firing-frequency body (cross-plane pairs) and crank rumble come from the pattern itself
    cutoff = np.interp(rpm, [800, 5000], [900, 3200])
    out = sosfilt(butter(4, cutoff, "low", fs=SR, output="sos"), out)
    drive = np.interp(rpm, [800, 5000], [2.2, 1.2])            # growl; cleaner at high rpm
    out = np.tanh(drive * out / (np.max(np.abs(out)) + 1e-9))
    # intake / valvetrain hiss riding on the firing envelope
    env = sosfilt(butter(2, 40, "low", fs=SR, output="sos"), np.abs(banks.sum(0)))
    hiss = sosfilt(butter(2, [1500, 5000], "band", fs=SR, output="sos"), rng.normal(0, 1, n + pad))
    out += np.interp(rpm, [800, 5000], [.025, .07]) * hiss * env / (env.max() + 1e-9)
    out = sosfilt(butter(2, 28, "high", fs=SR, output="sos"), out)
    # seamless loop: crossfade the tail into the head (integer number of cycles)
    fade = int(.06 * SR)
    loop = out[:n].copy()
    w = np.linspace(0, 1, fade)
    loop[:fade] = loop[:fade] * w + out[n:n + fade] * (1 - w)
    loop *= .89 / (np.max(np.abs(loop)) + 1e-9)                 # -1 dBFS
    return loop.astype(np.float32), cycles


def write_ogg(samples, path):
    with tempfile.TemporaryDirectory() as tmp:
        wav = Path(tmp) / "x.wav"
        with wave.open(str(wav), "wb") as f:
            f.setnchannels(1)
            f.setsampwidth(2)
            f.setframerate(SR)
            f.writeframes((samples * 32767).astype("<i2").tobytes())
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libvorbis",
                        "-q:a", "6", str(path)], check=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, rpm in LOOPS.items():
        loop, cycles = synth(rpm)
        write_ogg(loop, OUT / (name + ".ogg"))
        print(f"BRAX_V8_LOOP {name} rpm={rpm} cycles={cycles} seconds={len(loop) / SR:.3f}")


if __name__ == "__main__":
    main()
