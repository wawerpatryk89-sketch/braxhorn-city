#!/usr/bin/env bash
set -euo pipefail
DIR="${1:?Podaj katalog zawierajacy dwa pliki wlasciciela}"
REPO=wawerpatryk89-sketch/braxhorn-city
TAG=oboz-owner-remodel-20261105
BRANCH=codex/oboz-przebudowa-pbr-20261105
ZIP="$DIR/BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip"
BULL="$DIR/BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb"
test "$(stat -c %s "$ZIP")" = 345273037
test "$(stat -c %s "$BULL")" = 2232460
echo "b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006  $ZIP" | sha256sum -c --status
echo "425788876b8fe2dfaba90157d74c6631d231184f96659759cdafade794a5352f  $BULL" | sha256sum -c --status
if ! gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
 gh release create "$TAG" --repo "$REPO" --draft --target "$BRANCH" --title "Oboz — prywatne oryginaly 2026-11-05" --notes "DRAFT ONLY. No public release."
fi
[ "$(gh release view "$TAG" --repo "$REPO" --json isDraft --jq .isDraft)" = true ] || exit 2
gh release upload "$TAG" "$ZIP" "$BULL" --repo "$REPO"
python3 unity-tools/oboz/owner_release_assets.py preflight --repo "$REPO"
