#!/usr/bin/env bash
cd /home/claude/test_rk
rm -rf dobor; mkdir -p dobor
run() { id=$1; src=$(python3 -I -c "import json;print(json.load(open('kand_$id.json'))['zrodlo'])"); python3 -I narz/rk_dobor.py $id $(ls $src/*.fbx) kand_$id.json dobor/$id > dobor/$id.log 2>&1; }
( run kulturysta_rk; run kobieta_bojowki_rk; run niedzwiedz_rk ) &
( run bokser2_rk; run kobieta_czarna_rk ) &
wait
echo GOTOWE > dobor/koniec.txt
