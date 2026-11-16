"""Install the signed APK, capture the Unity player and verify each recorded drive."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import os
import re
import subprocess
import sys
import tempfile
import uuid


def run(*args):
    return subprocess.check_output(args, text=True).strip()


def start_recording(case):
    remote = f"/sdcard/osiedle-{case}-{uuid.uuid4().hex}.mp4"
    process = subprocess.Popen(["adb", "shell", "screenrecord", "--bit-rate", "3500000", remote])
    return process, remote


def stop_recording(process):
    if process.poll() is not None:
        raise RuntimeError(f"screenrecord zakończył się przed końcem próby (kod {process.returncode})")
    try:
        run("adb", "shell", "pkill", "-INT", "-x", "screenrecord")
        status = process.wait(timeout=20)
        if status not in (0, 130):
            raise RuntimeError(f"screenrecord zwrócił kod {status}")
    finally:
        if process.poll() is None:
            process.kill()
            process.wait()


def verify_video(path):
    probe = run("ffprobe", "-v", "error", "-select_streams", "v:0", "-count_packets",
                "-show_entries", "stream=duration,nb_read_packets", "-of", "json", path)
    streams = json.loads(probe)["streams"]
    if not streams or float(streams[0].get("duration", 0)) <= 0 or int(streams[0].get("nb_read_packets", 0)) < 2:
        raise ValueError(f"Brak sekwencji klatek w nagraniu: {path.name}")
    run("ffmpeg", "-nostdin", "-v", "error", "-xerror", "-i", path, "-map", "0:v:0", "-f", "null", "-")


def collect(remote, output):
    with tempfile.TemporaryDirectory(dir=output.parent.parent) as temporary:
        video = Path(temporary) / output.name
        run("adb", "pull", remote, video)
        verify_video(video)
        video.replace(output)
    run("adb", "shell", "rm", "-f", remote)


def capture(log, out, expected, package, activity):
    passed = set()
    current = None
    recorder = None
    pending = []
    with ThreadPoolExecutor(max_workers=1) as copies, (out / "wynik.txt").open("w") as report:
        try:
            run("adb", "shell", "am", "start", "-n", f"{package}/{activity}", "--ez", "brax_osiedle_qa", "true")
            for line in log:
                if "FATAL EXCEPTION" in line or "BX_QA_FAIL" in line:
                    report.write(line)
                    raise RuntimeError(line.strip())
                if "BX_QA_" not in line:
                    continue
                report.write(line)
                report.flush()
                print(line.strip(), flush=True)
                if "BX_QA_START " in line:
                    case = line.split("BX_QA_START ", 1)[1].strip()
                    if case not in expected or case in passed or recorder is not None:
                        raise ValueError(line)
                    current = case
                    recorder, remote = start_recording(case)
                elif "BX_QA_PASS " in line:
                    case = line.split("BX_QA_PASS ", 1)[1].strip()
                    if recorder is None or case != current:
                        raise ValueError(line)
                    process, recorder = recorder, None
                    stop_recording(process)
                    pending.append(copies.submit(collect, remote, out / (case + ".mp4")))
                    passed.add(case)
                    current = None
                elif "BX_QA_GOTOWE" in line:
                    break
            else:
                raise RuntimeError("Log gry zakończył się przed BX_QA_GOTOWE")
            if passed != expected or recorder is not None:
                raise RuntimeError("Brakuje prób: " + str(expected - passed))
            for copy in pending:
                copy.result()
        finally:
            if recorder is not None:
                try:
                    stop_recording(recorder)
                    collect(remote, out / (current + "-blad.mp4"))
                except (ValueError, RuntimeError, OSError, subprocess.SubprocessError) as error:
                    print(f"Przerwano nagranie {current}: {error}", file=sys.stderr)


def main(apk):
    sdk = Path(os.environ["ANDROID_HOME"])
    buildtools = max((sdk / "build-tools").iterdir(), key=lambda p: [int(n) for n in re.findall(r'\d+', p.name)])
    subprocess.run([buildtools / "apksigner", "verify", "--verbose", apk], check=True)
    badging = run(buildtools / "aapt", "dump", "badging", apk)
    package = re.search(r"package: name='([^']+)'", badging)[1]
    activity = re.search(r"launchable-activity: name='([^']+)'", badging)[1]
    abis = run("adb", "shell", "getprop", "ro.product.cpu.abilist")
    if "arm64-v8a" not in abis.split(","):
        raise RuntimeError("Brak obsługi ARM64 w emulatorze: " + abis)
    run("adb", "install", "-r", apk)
    run("adb", "logcat", "-c")
    out = Path("proba-apk")
    out.mkdir(exist_ok=True)
    expected = {"bar"} | {f"BX{i}" for i in range(10001, 10015)}
    with subprocess.Popen(["adb", "logcat", "-v", "brief", "Unity:I", "AndroidRuntime:E", "*:S"],
                          stdout=subprocess.PIPE, text=True) as log:
        try:
            capture(log.stdout, out, expected, package, activity)
        finally:
            log.terminate()


if __name__ == "__main__":
    main(Path(sys.argv[1]))
