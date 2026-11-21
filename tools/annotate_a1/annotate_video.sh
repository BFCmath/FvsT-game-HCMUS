#!/bin/sh
set -eu

if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <input.mov|input.mp4> <captions.json> <output.mp4>" >&2
    exit 2
fi

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cache_dir="$(getconf DARWIN_USER_TEMP_DIR)annotate-a1-swift-cache"
mkdir -p "$cache_dir"
build_dir=$(mktemp -d "$cache_dir/build.XXXXXXXX")
trap 'rm -rf "$build_dir"' EXIT
export CLANG_MODULE_CACHE_PATH="$cache_dir"

input=$1
captions=$2
output=$3
set -- -module-cache-path "$cache_dir"

# Some upgraded Command Line Tools ship two definitions of SwiftBridging.
# Mask the obsolete definition for this compilation without changing the SDK.
clt=/Library/Developer/CommandLineTools
if [ "$(xcode-select -p)" = "$clt" ] &&
   [ -f "$clt/usr/include/swift/module.modulemap" ] &&
   [ -f "$clt/usr/include/swift/bridging.modulemap" ]; then
    : > "$cache_dir/empty.modulemap"
    cat > "$build_dir/overlay.yaml" <<EOF
{
  "version": 0,
  "case-sensitive": false,
  "roots": [{
    "type": "file",
    "name": "$clt/usr/include/swift/module.modulemap",
    "external-contents": "$cache_dir/empty.modulemap"
  }]
}
EOF
    mv -f "$build_dir/overlay.yaml" "$cache_dir/overlay.yaml"
    set -- "$@" -vfsoverlay "$cache_dir/overlay.yaml" -Xcc -ivfsoverlay -Xcc "$cache_dir/overlay.yaml"
fi

/usr/bin/swiftc "$@" "$script_dir/AnnotateVideo.swift" -o "$build_dir/annotate-a1"
"$build_dir/annotate-a1" "$input" "$captions" "$output"
