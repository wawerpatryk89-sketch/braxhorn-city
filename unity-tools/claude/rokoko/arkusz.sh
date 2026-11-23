#!/usr/bin/env bash
# arkusz.sh <katalog_klatek> <wyjscie.jpg> <kolumn>
set -e
ffmpeg -v error -y -pattern_type glob -i "$1/*.jpg" -vf "tile=${3}x$(( ( $(ls "$1"/*.jpg | wc -l) + $3 - 1 ) / $3 ))" -frames:v 1 -q:v 3 "$2"
