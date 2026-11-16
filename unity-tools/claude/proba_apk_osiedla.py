"""Install the actual signed build, capture the Unity player and check the measured drives."""
import os
from pathlib import Path
import re
import subprocess
import sys

def run(*args):
    return subprocess.check_output(args, text=True).strip()

apk, sdk = Path(sys.argv[1]), Path(os.environ["ANDROID_HOME"])
buildtools = sorted((sdk/"build-tools").iterdir(),key=lambda p: [int(n) for n in re.findall(r'\d+',p.name)])[-1]
subprocess.run([buildtools/"apksigner","verify","--verbose",apk],check=True)
badging = run(buildtools/"aapt","dump","badging",apk)
package = re.search(r"package: name='([^']+)'",badging)[1]
activity = re.search(r"launchable-activity: name='([^']+)'",badging)[1]
abis = run("adb","shell","getprop","ro.product.cpu.abilist")
if "arm64-v8a" not in abis: raise RuntimeError("Brak obsługi ARM64 w emulatorze: "+abis)
run("adb","install","-r",apk)
run("adb","logcat","-c")
out = Path("proba-apk");out.mkdir(exist_ok=True)
expected = {"bar"} | {f"BX{i}" for i in range(10001,10015)}
passed = set()
video = None
def finish():
    global video
    if video is None: return
    subprocess.run(["adb","shell","pkill","-INT","screenrecord"],check=False)
    video.wait(timeout=20)
    run("adb","pull","/sdcard/proba-osiedla.mp4",str(out/(current+".mp4")))
    run("ffmpeg","-v","error","-xerror","-i",str(out/(current+".mp4")),"-f","null","-")
    video=None
with subprocess.Popen(["adb","logcat","-v","brief","Unity:I","AndroidRuntime:E","*:S"],stdout=subprocess.PIPE,text=True) as log:
    try:
        run("adb","shell","am","start","-n",package+"/"+activity,"--ez","brax_osiedle_qa","true")
        with (out/"wynik.txt").open("w") as report:
            for line in log.stdout:
                if "FATAL EXCEPTION" in line or "BX_QA_FAIL" in line: raise RuntimeError(line.strip())
                if "BX_QA_" not in line: continue
                report.write(line);report.flush();print(line.strip(),flush=True)
                if "BX_QA_START " in line:
                    current=line.split("BX_QA_START ",1)[1].strip()
                    if current not in expected or current in passed or video: raise ValueError(line)
                    video=subprocess.Popen(["adb","shell","screenrecord","--bit-rate","3500000","/sdcard/proba-osiedla.mp4"])
                elif "BX_QA_PASS " in line:
                    if line.split("BX_QA_PASS ",1)[1].strip()!=current: raise ValueError(line)
                    finish();passed.add(current)
                elif "BX_QA_GOTOWE" in line: break
        if passed!=expected: raise RuntimeError("Brakuje prób: "+str(expected-passed))
    finally:
        log.terminate()
        finish()
