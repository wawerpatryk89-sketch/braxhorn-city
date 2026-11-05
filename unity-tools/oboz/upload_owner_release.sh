#!/usr/bin/env bash
# Optional CLI equivalent of iPhone Safari upload. Upload only six 14-16 MB ZIPs.
# Their first ZIP embeds unchanged owner bull GLB. Other source ZIPs 02-05 already uploaded.
set -euo pipefail
DIR="${1:?Provide local folder containing five ZIPs and bull GLB}"
REPO="wawerpatryk89-sketch/braxhorn-city"
TAG="oboz-owner-remodel-20261105"
BRANCH="codex/oboz-przebudowa-pbr-20261105"
NAMES=(
  BRAXHORN_OBOZ_MALE_01_01_Z_06.zip
  BRAXHORN_OBOZ_MALE_01_02_Z_06.zip
  BRAXHORN_OBOZ_MALE_01_03_Z_06.zip
  BRAXHORN_OBOZ_MALE_01_04_Z_06.zip
  BRAXHORN_OBOZ_MALE_01_05_Z_06.zip
  BRAXHORN_OBOZ_MALE_01_06_Z_06.zip
)
SIZES=(16396829 14164201 14164201 14164201 14164201 14164200)
HASHES=(
  e0f113fe622c906e0e5103a3b58156b184b1d48e66c0b52fefae0516c8761903
  fab2bc89964edf6ba0f38d8b6c838bf5356c49cf226c4c0cbc308f1778833310
  8fd1f0b5e42b6193d7e3cb7da8c78042abc6e5cd563b05addb8d4cbddf02bc4a
  144cd694f01c0d5a72f31556636c52740ec8e0684e5567ba253941c093955180
  4c33f3303bcc739b761bac44885bb810ae07221d996a503973fc31c7e4c5648b
  66bbe60a9a7a8a7676e306272fe7f113608d9028f688de3afd77bccff46bb125
)
for i in "${!NAMES[@]}"; do
  file="$DIR/${NAMES[$i]}"
  test -f "$file" || { echo "Missing $file" >&2; exit 2; }
  test "$(stat -c %s "$file")" = "${SIZES[$i]}" ||
    { echo "Wrong size: $file" >&2; exit 2; }
  printf '%s  %s\n' "${HASHES[$i]}" "$file" | sha256sum --check --status ||
    { echo "Wrong SHA256: $file" >&2; exit 2; }
done
if ! gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
  gh release create "$TAG" --repo "$REPO" --draft --target "$BRANCH" \
    --title "Oboz BRAXHORN — zrodla 2026-11-05" \
    --notes "Original owner models. DRAFT ONLY. Do not publish."
fi
test "$(gh release view "$TAG" --repo "$REPO" --json isDraft --jq .isDraft)" = true ||
  { echo "Release is not a draft — ABORT" >&2; exit 2; }
for i in "${!NAMES[@]}"; do
  gh release upload "$TAG" "$DIR/${NAMES[$i]}" --repo "$REPO"
done
python3 unity-tools/oboz/owner_release_assets.py preflight --repo "$REPO"
echo "OWNER_ASSETS_UPLOADED_AND_VERIFIED draft=yes assets=6 mode=five_parts"
