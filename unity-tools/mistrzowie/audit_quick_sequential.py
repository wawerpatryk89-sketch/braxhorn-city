"""Sequential short offline audit of Champions code, assets and scene contracts.

One independent process per test module, preserving complete diagnostics.
No Unity editor, no Android APK, no downloads, no release mutation.
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
TEST_ROOT = ROOT / "unity-tools" / "mistrzowie"
REPORT = ROOT / "mistrzowie-evidence" / "quick-sequential-audit.json"
OUTPUT = ROOT / "mistrzowie-evidence" / "quick-sequential-audit.txt"

def main() -> int:
    files = sorted(TEST_ROOT.glob("test_*.py"))
    results = []
    started = time.monotonic()
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("w", encoding="utf-8") as log:
        for index, path in enumerate(files, 1):
            label = path.name
            command = [sys.executable, "-m", "unittest", "discover",
                       "-s", str(TEST_ROOT), "-p", label, "-v"]
            begin = time.monotonic()
            try:
                run = subprocess.run(command, cwd=ROOT, capture_output=True,
                                     text=True, timeout=45, check=False,
                                     env={**os.environ, "PYTHONUNBUFFERED": "1"})
                status = "PASS" if run.returncode == 0 else "FAIL"
                output = run.stdout + run.stderr
                exit_code = run.returncode
            except subprocess.TimeoutExpired as error:
                status = "TIMEOUT"
                exit_code = 124
                out = error.stdout or b""
                err = error.stderr or b""
                if isinstance(out, bytes):
                    out = out.decode("utf-8", "replace")
                if isinstance(err, bytes):
                    err = err.decode("utf-8", "replace")
                output = out + err + "\nTest exceeded 45-second short-test limit."
            seconds = round(time.monotonic() - begin, 2)
            entry = {"module": label, "status": status, "seconds": seconds,
                     "exitCode": exit_code}
            if status != "PASS":
                entry["errorTail"] = output[-3500:]
            results.append(entry)
            log.write(f"===== {index}/{len(files)} {label} {status} {seconds}s =====\n")
            log.write(output[-10000:] + "\n")
            log.flush()
            print(f"BRAX_MISTRZOWIE_SHORT_TEST {index}/{len(files)} "
                  f"{label} {status} {seconds}s", flush=True)
            if status != "PASS":
                print("BRAX_MISTRZOWIE_TEST_FAILURE " + label
                      + "\n" + output[-3000:], flush=True)
        report = {
            "sourceCommit": os.environ.get("GITHUB_SHA", "local"),
            "status": "PASS" if all(r["status"] == "PASS" for r in results)
                      else "ISSUES_FOUND",
            "tested": len(results),
            "passed": sum(r["status"] == "PASS" for r in results),
            "failed": sum(r["status"] == "FAIL" for r in results),
            "timedOut": sum(r["status"] == "TIMEOUT" for r in results),
            "seconds": round(time.monotonic() - started, 2),
            "notes": ("Source-only tests; does not prove Unity scene render, "
                      "3D animation, navigability or APK execution."),
            "tests": results,
        }
        REPORT.write_text(json.dumps(report, indent=2, ensure_ascii=False)
                          + "\n", encoding="utf-8")
        print("BRAX_MISTRZOWIE_SEQUENTIAL_REPORT "
              + json.dumps({k: report[k] for k in
                  ("status", "tested", "passed", "failed", "timedOut", "seconds")}),
              flush=True)
    return 0 if report["status"] == "PASS" else 1

if __name__ == "__main__":
    sys.exit(main())
