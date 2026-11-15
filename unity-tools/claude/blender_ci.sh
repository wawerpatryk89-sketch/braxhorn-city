#!/usr/bin/env bash
# Pobierz dokładnie tę samą wersję z lustra Blendera, jeśli pierwsze źródło odmówi dostępu.
set -euo pipefail
archive=blender-4.2.3-linux-x64.tar.xz
sha=3a64efd1982465395abab4259b4091d5c8c56054c7267e9633e4f702a71ea3f4
dest=$(mktemp -d "${RUNNER_TEMP:-/tmp}/brax-blender.XXXXXX")
trap 'rm -rf "$dest"' EXIT
for base in \
    https://mirror.blender.org/release \
    https://download.blender.org/release \
    https://mirror.clarkson.edu/blender/release \
    https://mirrors.ocf.berkeley.edu/blender/release \
    https://ftp.nluug.nl/graphics/blender/release; do
    url="$base/Blender4.2/$archive"
    if curl --fail --location --silent --show-error --connect-timeout 12 --max-time 300 \
            --output "$dest/$archive" "$url" &&
        (cd "$dest" && printf '%s  %s\n' "$sha" "$archive" | sha256sum --check --strict); then
        tar -xJf "$dest/$archive" -C "$dest"
        "$dest/${archive%.tar.xz}/blender" --version
        final="${RUNNER_TEMP:-/tmp}/${archive%.tar.xz}"
        mv -T "$dest/${archive%.tar.xz}" "$final"
        echo "BRAXHORN_BLENDER url=$url sha256=$sha"
        exit 0
    fi
    echo "::warning::Nieudane pobranie lub błędna suma z $base"
done
echo '::error::Brak dostępnej kopii Blendera 4.2.3 z zatwierdzoną sumą SHA-256.'
exit 1
