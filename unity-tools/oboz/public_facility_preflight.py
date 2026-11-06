#!/usr/bin/env python3
"""Very fast read-only official Poly Haven metadata check, no texture downloads.

Verifies the exact eight CC0 map URLs needed by the camp bathhouse
before starting a 5-hour Unity build. No scene/APK/material files are created.
"""
from __future__ import annotations
import json
import re
import sys
from facility_public_materials import API,ASSETS,ROLES,fetch_bytes,official_file,source


def main():
    for asset in ASSETS:
        data=json.loads(fetch_bytes(API+asset,limit=4_000_000))
        for role in ROLES:
            entry=official_file(data,role)
            expected=source(asset,role)
            if entry.get("url")!=expected:
                raise ValueError(
                    "Poly Haven original URL changed: "+asset+"/"+role+
                    ": wanted "+expected+", got "+repr(entry.get("url")))
            md5=entry.get("md5")
            if not isinstance(md5,str) or not re.fullmatch("[0-9a-f]{32}",md5):
                raise ValueError("Missing official MD5: "+asset+"/"+role)
            size=entry.get("size",0)
            if not isinstance(size,int) or not 500<=size<=28_000_000:
                raise ValueError("Invalid official 2K file size: "+asset+"/"+role)
            print("OBOZ_POLYHAVEN_PREFLIGHT_MAP=PASS",asset,role,size,md5)
    print("OBOZ_POLYHAVEN_READONLY=PASS; assets=2 maps=8 jpgDownloaded=0 unityStarted=false")

if __name__=="__main__":
    try:main()
    except Exception as ex:
        print("::error::OBOZ_POLYHAVEN_READONLY_FAIL "+str(ex),file=sys.stderr)
        raise SystemExit(2)
