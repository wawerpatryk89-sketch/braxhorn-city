"""Sample each Polish voice recording into a compact 50 Hz mouth-opening track."""

from array import array
from concurrent.futures import ThreadPoolExecutor
import math
from pathlib import Path
import subprocess
import sys


def mouth_track(audio):
    pcm = array("h")
    pcm.frombytes(audio)
    if sys.byteorder != "little":
        pcm.byteswap()
    strengths = []
    rounding = []
    # Two vocal-tract bands distinguish back/rounded vowels from spread/front vowels.
    frequencies = (700, 900, 1100, 1500, 1800, 2100, 2400)
    coefficients = [2 * math.cos(2 * math.pi * f / 8000) for f in frequencies]
    for i in range(0, len(pcm), 160):
        window = pcm[i : i + 160]
        rms = math.sqrt(sum(v * v for v in window) / len(window)) / 32768
        crossings = sum((a < 0) != (b < 0) for a, b in zip(window, window[1:])) / len(
            window
        )
        power = []
        for coef in coefficients:
            a = b = 0.0
            for v in window:
                a, b = v + coef * a - b, a
            power.append(max(0.0, a * a + b * b - coef * a * b))
        low, high = sum(power[:3]) / 3, sum(power[3:]) / 4
        ratio = low / (low + high + 1.0)
        rounding.append(
            max(0.0, min(1.0, (ratio - 0.50) / 0.42)) * max(0.0, 1 - crossings * 3)
        )
        strengths.append(rms * (1 - min(0.65, max(0, crossings - 0.13) * 1.6)))
    if not strengths:
        raise ValueError("Empty voice recording")
    peak = max(0.055, sorted(strengths)[int((len(strengths) - 1) * 0.90)])
    frames = bytearray()
    for energy, rounded in zip(strengths, rounding):
        opened = min(1.0, max(0.0, (energy - 0.009) / peak) ** 0.74)
        frames.extend(
            (round(255 * opened), round(255 * rounded * min(1.0, opened * 2)))
        )
    return bytes(frames)


def prepare(source, output, pattern="*.ogg"):
    output.mkdir(parents=True, exist_ok=True)

    def convert(path):
        pcm = subprocess.check_output(
            [
                "ffmpeg",
                "-nostdin",
                "-v",
                "error",
                "-i",
                str(path),
                "-ac",
                "1",
                "-ar",
                "8000",
                "-f",
                "s16le",
                "-",
            ]
        )
        (output / (path.stem + ".bytes")).write_bytes(mouth_track(pcm))

    files = sorted(source.glob(pattern))
    if not files:
        raise ValueError("No Osiedle voice recordings to align")
    with ThreadPoolExecutor(3) as workers:
        list(workers.map(convert, files))


if __name__ == "__main__":
    prepare(*(Path(p) for p in sys.argv[1:]))
