#!/usr/bin/env bash
# Durable Champions import memory (owner 12.11.2026 21:20: "przecież ja już od dawna ustawiałem ... dlaczego to nie
# funkcjonuje cały czas?").
# Why the Actions cache never worked: the Library archive is 6.2 GB, the PBR source maps 5.6 GB, and GitHub keeps at most
# 10 GB of Actions cache for the whole repository (all four regions). Every later save evicted the Library, so since
# build 38 every build imported everything from zero (~1 h 40 min). The archive now lives as assets of one release
# (tag mistrzowie-library-cache), outside that limit:
#   - the archive is encrypted by library_cache.sh (AES-256, the repository's key) and split into parts < 2 GiB;
#   - parts carry the run id; the manifest (champions-library.sha256) is uploaded LAST and names the parts and the
#     profile (Unity version + package manifest), so a half-finished upload is never restored;
#   - restore verifies every part's SHA-256 and the profile before library_cache.sh touches the project; any
#     mismatch = clean import, as before (never a failed build);
#   - old parts are deleted only after the new manifest is in place.
# usage: library_store.sh save|restore <unity project> <unity version>      (needs GH_TOKEN, GITHUB_REPOSITORY, KLUCZ)
set -euo pipefail
mode=${1:?save or restore}
project=$(realpath "${2:?Unity project path}")
unity=${3:?Unity version}
: "${GH_TOKEN:?}" "${GITHUB_REPOSITORY:?}" "${KLUCZ:?}"
tag=mistrzowie-library-cache
here=$(cd "$(dirname "$0")" && pwd)
work=${RUNNER_TEMP:-/tmp}/champions-library-store
profile="$unity-$(sha256sum "$project/Packages/manifest.json" | cut -c1-16)"
rm -rf "$work"; mkdir -p "$work"
trap 'rm -rf "$work"' EXIT

case "$mode" in
  save)
    bash "$here/library_cache.sh" save "$project" "$work/archive.enc"
    run="${GITHUB_RUN_ID:-local}-${GITHUB_RUN_ATTEMPT:-1}"
    (cd "$work" && split -b "${LIBRARY_PART_SIZE:-1900m}" -d -a 2 archive.enc "champions-library-$run.part" && rm -f archive.enc)
    (cd "$work" && { echo "# profile $profile"; sha256sum champions-library-"$run".part*; } > champions-library.sha256)
    if ! gh release view "$tag" --repo "$GITHUB_REPOSITORY" >/dev/null 2>&1; then
      gh release create "$tag" --repo "$GITHUB_REPOSITORY" --prerelease --title "Miasto Mistrzów — pamięć importu Unity (zaszyfrowana)" \
        --notes "Zaszyfrowane archiwum Library Unity dla buildów Miasta Mistrzów. Nie jest grą ani zasobem do pobrania." >/dev/null
    fi
    old=$(gh release view "$tag" --repo "$GITHUB_REPOSITORY" --json assets -q '.assets[].name' | grep '^champions-library-.*\.part' || true)
    gh release upload "$tag" --repo "$GITHUB_REPOSITORY" --clobber "$work"/champions-library-"$run".part*
    gh release upload "$tag" --repo "$GITHUB_REPOSITORY" --clobber "$work/champions-library.sha256"
    for a in $old; do case "$a" in champions-library-"$run".part*) ;; *) gh release delete-asset "$tag" "$a" --repo "$GITHUB_REPOSITORY" -y || true ;; esac; done
    echo "BRAX_LIBRARY_STORE saved profile=$profile parts=$(ls "$work"/champions-library-"$run".part* | wc -l) bytes=$(cat "$work"/champions-library-"$run".part* | wc -c)"
    ;;
  restore)
    if ! gh release download "$tag" --repo "$GITHUB_REPOSITORY" --pattern champions-library.sha256 --dir "$work" 2>/dev/null; then
      echo "BRAX_LIBRARY_STORE miss: no stored Library yet; clean import"; exit 0
    fi
    stored=$(sed -n 's/^# profile //p' "$work/champions-library.sha256")
    if [[ "$stored" != "$profile" ]]; then echo "BRAX_LIBRARY_STORE miss: profile $stored != $profile; clean import"; exit 0; fi
    parts=$(grep -v '^#' "$work/champions-library.sha256" | awk '{print $2}')
    for p in $parts; do gh release download "$tag" --repo "$GITHUB_REPOSITORY" --pattern "$p" --dir "$work"; done
    if ! (cd "$work" && grep -v '^#' champions-library.sha256 | sha256sum -c --quiet); then
      echo "BRAX_LIBRARY_STORE miss: a part failed its SHA-256; clean import"; exit 0
    fi
    (cd "$work" && cat $parts > archive.enc && rm -f $parts)
    bash "$here/library_cache.sh" restore "$project" "$work/archive.enc"
    echo "BRAX_LIBRARY_STORE restored profile=$profile"
    ;;
  *) echo 'Use save or restore' >&2; exit 2 ;;
esac
