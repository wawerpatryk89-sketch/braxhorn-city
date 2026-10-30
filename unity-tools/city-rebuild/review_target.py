#!/usr/bin/env python3
"""Resolve only an explicitly selected immutable Unity build for the QA job."""
import json
import os
from pathlib import Path
import re

target = json.loads(Path(__file__).with_name("review-target.json").read_text())
run_id, commit = target.get("runId"), target.get("commit")
if not isinstance(run_id, int) or run_id <= 0 or not isinstance(commit, str) or not re.fullmatch("[0-9a-f]{40}", commit):
    raise ValueError("Set an exact completed Unity build run and SHA in review-target.json")
outputs = {"run": str(run_id), "commit": commit, "artifact": "city-rebuild-player-" + commit}
if os.environ.get("GITHUB_OUTPUT"):
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        for key, value in outputs.items():
            output.write(key + "=" + value + "\n")
print(json.dumps(outputs))
