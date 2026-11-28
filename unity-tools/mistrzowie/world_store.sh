#!/usr/bin/env bash
# MIASTO MISTRZÓW - zapisany świat między dwoma przebiegami buildu (jak checkpoint miasta w Mieście R1).
# Owner 25.11.2026 17:22 "Go" on: "Build jak u R1: limit czasu: jak Unity nie zdąży, kończy czysto po zbudowaniu świata,
# zapis świata, także po przerwaniu, drugi przebieg robi tylko APK".
#   save:    after the Unity step, when Unity wrote mistrzowie-swiat-gotowy.txt (BraxChampionsEtapy: the world is complete),
#            all new or changed files (mtime or ctime), the entire generated Champions world regardless of timestamps,
#            a verified PBR inventory and the list of deleted files (excluding built bundles), as one archive
#            encrypted with the repository's key (AES-256) in parts < 2 GiB of the release mistrzowie-world-cache; the
#            manifest is uploaded LAST, so a half-finished upload is never restored; older worlds go only after it;
#   restore: before Unity, only the world of the same key (source commit + Unity + pipeline + packages): every part's
#            SHA-256 checked first, then extracted over the checkout (times kept), the deleted files removed and
#            mistrzowie-swiat-przywrocony.txt written - BraxChampionsBuild then builds only the APK. A miss or any mismatch
#            = a normal full run (never a failed build).
# usage: world_store.sh save|restore <unity project> <key>     (GH_TOKEN, GITHUB_REPOSITORY, KLUCZ; save: WORLD_START, WORLD_BEFORE)
set -euo pipefail
mode=${1:?save or restore}
project=$(realpath "${2:?Unity project path}")
key=${3:?world key}
: "${GH_TOKEN:?}" "${GITHUB_REPOSITORY:?}" "${KLUCZ:?}"
[[ "$key" =~ ^[0-9a-f]{16,64}$ ]] || { echo "BRAX_WORLD_STORE bad key: $key" >&2; exit 2; }
tag=mistrzowie-world-cache
name="mistrzowie-world-$key"
work=${RUNNER_TEMP:-/tmp}/champions-world-store
export LC_ALL=C
rm -rf "$work"; mkdir -p "$work"
trap 'rm -rf "$work"' EXIT

# #63: File.Copy can keep an older source mtime; never trust the timestamp for generated assets.
# Validate the full PBR file/meta set before packing and again in staging before overlay.
pbr_inventory() (
  cd "$1" || exit 1
  dir=Assets/PlayableGenerated/Mistrzowie/Textures
  [[ -d "$dir" ]] || { echo "BRAX_PBR_INTEGRITY missing $dir" >&2; exit 1; }
  count=0
  while IFS= read -r -d '' f; do
    case "$f" in
      *.meta)
        [[ -e "${f%.meta}" ]] || { echo "BRAX_PBR_INTEGRITY orphan $f" >&2; exit 1; }
        ;;
      *)
        [[ -s "$f" && -s "$f.meta" ]] || { echo "BRAX_PBR_INTEGRITY missing file/meta $f" >&2; exit 1; }
        grep -Eq '^guid: [0-9a-f]{32}[[:space:]]*$' "$f.meta" || { echo "BRAX_PBR_INTEGRITY invalid GUID $f.meta" >&2; exit 1; }
        count=$((count + 1))
        ;;
    esac
  done < <(find "$dir" -type f -print0)
  [[ "$count" -gt 0 ]] || { echo "BRAX_PBR_INTEGRITY empty $dir" >&2; exit 1; }
  find "$dir" -type f -print0 | sort -z | xargs -0 -r sha256sum
)

case "$mode" in
  save)
    start=${WORLD_START:?marker touched right before Unity}
    before=${WORLD_BEFORE:?files under Assets and ProjectSettings before Unity}
    if [[ ! -f "$project/mistrzowie-swiat-gotowy.txt" ]]; then echo "BRAX_WORLD_STORE nothing to save: the world was not completed"; exit 0; fi
    cd "$project"
    find Assets ProjectSettings -type f | sort > "$work/after.txt"
    comm -23 "$before" "$work/after.txt" > mistrzowie-swiat-usuniete.txt
    pbr_inventory "$project" > mistrzowie-swiat-pbr.sha256
    { find Assets ProjectSettings -type f ! -path 'Assets/StreamingAssets/*' \
        \( -newer "$start" -o -cnewer "$start" -o -path 'Assets/PlayableGenerated/Mistrzowie/*' -o -path 'Assets/PlayableGenerated/Mistrzowie.meta' \) -print0
      # New paths must be included even if their mtime predates the marker.
      comm -13 "$before" "$work/after.txt" | awk '!/^Assets\/StreamingAssets\//' | tr '\n' '\0'
      printf 'mistrzowie-swiat-gotowy.txt\0mistrzowie-swiat-usuniete.txt\0mistrzowie-swiat-pbr.sha256\0'; } | sort -zu > "$work/files"
    files=$(tr -cd '\0' < "$work/files" | wc -c)
    tar --null -T "$work/files" -cf - | zstd -T0 -3 | openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:KLUCZ -out "$work/archive.enc"
    run="${GITHUB_RUN_ID:-local}-${GITHUB_RUN_ATTEMPT:-1}"
    (cd "$work" && split -b "${WORLD_PART_SIZE:-1900m}" -d -a 2 archive.enc "$name-$run.part" && rm -f archive.enc)
    (cd "$work" && { echo "# key $key"; echo "# files $files"; sha256sum "$name-$run".part*; } > "$name.sha256")
    if ! gh release view "$tag" --repo "$GITHUB_REPOSITORY" >/dev/null 2>&1; then
      gh release create "$tag" --repo "$GITHUB_REPOSITORY" --prerelease --title "Miasto Mistrzów — zapisany świat buildu (zaszyfrowany)" \
        --notes "Zaszyfrowany świat Miasta Mistrzów między etapem 1 i 2 buildu. Nie jest grą ani zasobem do pobrania." >/dev/null
    fi
    old=$(gh release view "$tag" --repo "$GITHUB_REPOSITORY" --json assets -q '.assets[].name' | grep '^mistrzowie-world-' || true)
    gh release upload "$tag" --repo "$GITHUB_REPOSITORY" --clobber "$work/$name-$run".part*
    gh release upload "$tag" --repo "$GITHUB_REPOSITORY" --clobber "$work/$name.sha256"
    for a in $old; do case "$a" in "$name-$run".part*|"$name.sha256") ;; *) gh release delete-asset "$tag" "$a" --repo "$GITHUB_REPOSITORY" -y || true ;; esac; done
    echo "BRAX_WORLD_STORE saved key=$key files=$files deleted=$(wc -l < mistrzowie-swiat-usuniete.txt) parts=$(ls "$work/$name-$run".part* | wc -l) bytes=$(cat "$work/$name-$run".part* | wc -c)"
    ;;
  restore)
    if ! gh release download "$tag" --repo "$GITHUB_REPOSITORY" --pattern "$name.sha256" --dir "$work" 2>/dev/null; then
      echo "BRAX_WORLD_STORE miss: no saved world for key $key; full run"; exit 0
    fi
    stored=$(sed -n 's/^# key //p' "$work/$name.sha256")
    if [[ "$stored" != "$key" ]]; then echo "BRAX_WORLD_STORE miss: key $stored != $key; full run"; exit 0; fi
    parts=$(grep -v '^#' "$work/$name.sha256" | awk '{print $2}')
    [[ -n "$parts" ]] || { echo "BRAX_WORLD_STORE miss: empty manifest; full run"; exit 0; }
    for p in $parts; do gh release download "$tag" --repo "$GITHUB_REPOSITORY" --pattern "$p" --dir "$work"; done
    if ! (cd "$work" && grep -v '^#' "$name.sha256" | sha256sum -c --quiet); then
      echo "BRAX_WORLD_STORE miss: a part failed its SHA-256; full run"; exit 0
    fi
    # decoded completely into a temporary folder first: a damaged archive never leaves half a world in the project
    mkdir -p "$work/x"
    if ! (cd "$work" && cat $parts | openssl enc -d -aes-256-cbc -pbkdf2 -pass env:KLUCZ | zstd -d -T0 | tar -xf - -C "$work/x"); then
      echo "BRAX_WORLD_STORE miss: the archive did not decode; full run"; exit 0
    fi
    (cd "$work" && rm -f $parts)
    test -f "$work/x/mistrzowie-swiat-gotowy.txt" || { echo "BRAX_WORLD_STORE miss: no world marker in the archive; full run"; exit 0; }
    if ! test -s "$work/x/mistrzowie-swiat-pbr.sha256" ||
       ! pbr_inventory "$work/x" > "$work/restored-pbr.sha256" ||
       ! cmp -s "$work/x/mistrzowie-swiat-pbr.sha256" "$work/restored-pbr.sha256"; then
      echo "BRAX_WORLD_STORE miss: missing or inconsistent PBR inventory; full run without restoring"; exit 0
    fi
    echo "BRAX_PBR_INTEGRITY checkpoint verified"
    restored=$(cd "$work/x" && find . -type f | wc -l)
    cp -a "$work/x/." "$project/"
    removed=0
    while IFS= read -r f; do
      [[ -n "$f" && "$f" != *..* ]] || continue
      case "$f" in Assets/*|ProjectSettings/*) ;; *) continue ;; esac
      if [[ -e "$project/$f" ]]; then rm -f "$project/$f"; removed=$((removed + 1)); fi
    done < "$project/mistrzowie-swiat-usuniete.txt"
    cp "$project/mistrzowie-swiat-gotowy.txt" "$project/mistrzowie-swiat-przywrocony.txt"
    echo "BRAX_WORLD_STORE restored key=$key files=$restored removed=$removed commit=$(cat "$project/mistrzowie-swiat-gotowy.txt")"
    ;;
  *) echo 'Use save or restore' >&2; exit 2 ;;
esac
