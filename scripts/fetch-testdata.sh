#!/usr/bin/env bash
# Downloads third-party GDTF test data into GDTFSharp.Tests/TestData/External (git-ignored).
# These files are not committed because they come under their own licenses:
#  - gdtf.xsd from https://github.com/mvrdevelopment/tools (no license stated)
#  - sample fixtures from https://github.com/mvrdevelopment/libMVRgdtf (MVR SDK License)
# Tests that need them are skipped when the files are missing.
set -euo pipefail

target="$(cd "$(dirname "$0")/.." && pwd)/GDTFSharp.Tests/TestData/External"
mkdir -p "$target/fixtures"

fetch() {
  echo "Fetching $2"
  curl -fsSL -o "$2" "$1"
}

fetch "https://raw.githubusercontent.com/mvrdevelopment/tools/main/gdtf.xsd" "$target/gdtf.xsd"

libmvr="https://raw.githubusercontent.com/mvrdevelopment/libMVRgdtf/master/unittest/files"
fetch "$libmvr/allWorking.gdtf" "$target/fixtures/allWorking.gdtf"
fetch "$libmvr/virtual_channel.gdtf" "$target/fixtures/virtual_channel.gdtf"
fetch "$libmvr/Robe%20lighting%20s.r.o.@BMFL%20Spot@cannot%20assign%200%20to%20frost%20function.gdtf" \
      "$target/fixtures/Robe lighting s.r.o.@BMFL Spot.gdtf"

echo "Done: $target"
