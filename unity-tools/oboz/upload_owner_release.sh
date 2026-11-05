#!/usr/bin/env bash
# Optional CLI equivalent of manual Safari upload. Draft-only. No public git binaries.
set -euo pipefail
DIR="${1:?Provide local folder containing five ZIPs and bull GLB}"
REPO="wawerpatryk89-sketch/braxhorn-city"
TAG="oboz-owner-remodel-20261105"
BRANCH="codex/oboz-przebudowa-pbr-20261105"
NAMES=(
  BRAXHORN_MODELE_CZESC_01_Z_05.zip
  BRAXHORN_MODELE_CZESC_02_Z_05.zip
  BRAXHORN_MODELE_CZESC_03_Z_05.zip
  BRAXHORN_MODELE_CZESC_04_Z_05.zip
  BRAXHORN_MODELE_CZESC_05_Z_05.zip
  BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb
)
SIZES=(84984137 84997165 84995158 84985353 5311312 2232460)
HASHES=(
  1207ac04b4bd2422a9b5e9870332887b42d110f3628bdf960b5f96174fa4e281
  01a33b3b93a01604e89bcf0ff0ba3d831d89d32f8ddb52e51051920342d24b8b
  399abced06451b1dc19a723a0578d7db24d6afb97d992dacd551f22cfcb42321
  f7a07cd18ef6ad800878d8b2964851ee0262d74ad5098de0b4bf2ca538b14f08
  59c048af6a84ca4cd5668498f369fbfc746cb2d23c12636e73ced40372bec1c6
  425788876b8fe2dfaba90157d74c6631d231184f96659759cdafade794a5352f
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
