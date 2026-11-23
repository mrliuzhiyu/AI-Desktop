#!/bin/zsh
# 生成用户可直接解压的 macOS 桌面壁纸，并用解压副本验收。
set -euo pipefail
cd "${0:A:h}"
zsh build-wallpaper.sh
version=$(python3 ../scripts/check_version.py --component claude)
mkdir -p ../dist
stage=$(mktemp -d ../dist/.wallpaper.XXXXXX)
trap 'rm -rf "$stage"' EXIT
archive="ClaudeWallpaper-$version-macos.zip"
bundle="Claude 壁纸.app"
ditto -c -k --norsrc --noextattr --keepParent "build/$bundle" "$stage/$archive"
ditto -x -k "$stage/$archive" "$stage/unpacked"
app="$stage/unpacked/$bundle"
codesign --verify --deep --strict "$app"
for arch in arm64 x86_64; do
  lipo "$app/Contents/MacOS/ClaudeWallpaper" -verify_arch "$arch"
done
[[ "$(plutil -extract CFBundleShortVersionString raw "$app/Contents/Info.plist")" == "$version" ]]
cmp assets/loop.mp4 "$app/Contents/Resources/loop.mp4"
cmp assets/LICENSE.md "$app/Contents/Resources/LICENSE.md"
cmp ../LICENSE "$app/Contents/Resources/CODE-LICENSE"
mv -f "$stage/$archive" "../dist/$archive"
echo "桌面壁纸下载包：dist/$archive"
