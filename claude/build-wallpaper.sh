#!/bin/zsh
# 仅构建本地可双击的 macOS 壁纸 App。
set -euo pipefail
cd "${0:A:h}"
version=$(python3 ../scripts/check_version.py --component claude)
mkdir -p build
stage=$(mktemp -d build/.wallpaper.XXXXXX)
trap 'rm -rf "$stage"' EXIT
app="$stage/Claude 壁纸.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
for arch in arm64 x86_64; do
  xcrun swiftc -O -target "$arch-apple-macos13" Wallpaper/main.swift -o "$stage/$arch"
done
xcrun lipo -create "$stage/arm64" "$stage/x86_64" -output "$app/Contents/MacOS/ClaudeWallpaper"
cp assets/loop.mp4 assets/LICENSE.md "$app/Contents/Resources/"
cp ../LICENSE "$app/Contents/Resources/CODE-LICENSE"
cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleIdentifier</key><string>local.ai-desktop.claude-wallpaper</string>
  <key>CFBundleName</key><string>Claude 壁纸</string>
  <key>CFBundleExecutable</key><string>ClaudeWallpaper</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>LSMinimumSystemVersion</key><string>13.0</string>
  <key>LSUIElement</key><true/>
</dict></plist>
PLIST
plutil -lint "$app/Contents/Info.plist"
codesign --force --sign - "$app"
codesign --verify --strict --deep "$app"
# ditto 可以更新原有副本；构建和签名完成后才触及交付目录。
ditto "$app" "build/Claude 壁纸.app"
codesign --verify --strict --deep "build/Claude 壁纸.app"
echo "双击打开：build/Claude 壁纸.app"
