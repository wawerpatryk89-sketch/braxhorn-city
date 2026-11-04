#!/usr/bin/env bash
# Isolated Champions import cache. Never restore generated scenes or signing keys.
set -euo pipefail
mode=${1:?restore or save}
project=${2:?Unity project path}
archive=${3:?encrypted archive path}
project=$(realpath "$project")
archive=$(realpath -m "$archive")
: "${KLUCZ:?Repository encryption key required}"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
case "$mode" in
  save)
    test -d "$project/Library"
    mkdir -p "$(dirname "$archive")"
    (cd "$project"; find Assets -type f -name '*.meta' ! -path 'Assets/PlayableGenerated/*' -print0; printf 'Library\0') > "$tmp/files"
    tar -C "$project" --exclude=Library/Bee --exclude=Library/Il2cppBuildCache -cf - --null -T "$tmp/files" |
      zstd -T0 -3 | openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:KLUCZ -out "$tmp/cache.enc"
    mv "$tmp/cache.enc" "$archive"
    ;;
  restore)
    if [[ ! -f "$archive" ]]; then echo 'Champions Library cache miss; clean import.'; exit 0; fi
    # Decode completely before touching the project: corrupt cache cannot erase GUIDs.
    openssl enc -d -aes-256-cbc -pbkdf2 -pass env:KLUCZ -in "$archive" | zstd -d -T0 | tar -xf - -C "$tmp"
    test -d "$tmp/Library"
    if [[ -d "$tmp/Assets" ]]; then cp -an "$tmp/Assets/." "$project/Assets/"; fi
    rm -rf "$project/Library"
    mv "$tmp/Library" "$project/Library"
    echo 'Champions Library restored; existing source metadata retained.'
    ;;
  *) echo 'Use restore or save' >&2; exit 2 ;;
esac
