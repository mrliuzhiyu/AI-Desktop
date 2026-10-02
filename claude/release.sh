#!/bin/zsh
# 维护者用：打包 Mac 通用版 build/ClaudeBootAnimation.zip 和 Windows 版 build/ClaudeBootAnimation-Windows.zip，供上传到 GitHub Releases
# 用法：./release.sh            只打包
#       ./release.sh v1.0.1     打包并用 gh 发布这个版本
set -e
cd "${0:A:h}"
./build.sh
OUT=build/release/ClaudeBootAnimation
rm -rf build/release build/ClaudeBootAnimation.zip; mkdir -p "$OUT"
ditto build/ClaudeBootAnimation.app "$OUT/ClaudeBootAnimation.app"
cp install.sh uninstall.sh ../LICENSE "$OUT/"
cp assets/LICENSE.md "$OUT/ASSETS-LICENSE.md"
ditto -c -k --norsrc --noextattr --keepParent "$OUT" build/ClaudeBootAnimation.zip
echo "压缩包：build/ClaudeBootAnimation.zip（$(du -h build/ClaudeBootAnimation.zip | cut -f1)）"

# Windows 版：源码 + 脚本 + H.264 视频（很多 Windows 播不了 HEVC），用户电脑上用系统自带编译器现场编译
WOUT=build/release/ClaudeBootAnimation-Windows
rm -f build/ClaudeBootAnimation-Windows.zip; mkdir -p "$WOUT/assets"
cp -R windows/src windows/build.ps1 windows/install.ps1 windows/uninstall.ps1 "$WOUT/"
cp windows/assets/LibreBodoni-Italic.ttf windows/assets/LibreBodoni-OFL.txt "$WOUT/assets/"
cp ../LICENSE "$WOUT/"; cp assets/LICENSE.md "$WOUT/ASSETS-LICENSE.md"
ffmpeg -v error -y -i assets/intro.mp4 -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart -c:a aac -b:a 160k "$WOUT/assets/intro.mp4"
ffmpeg -v error -y -i assets/loop.mp4 -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart -an "$WOUT/assets/loop.mp4"
ditto -c -k --norsrc --noextattr --keepParent "$WOUT" build/ClaudeBootAnimation-Windows.zip
echo "压缩包：build/ClaudeBootAnimation-Windows.zip（$(du -h build/ClaudeBootAnimation-Windows.zip | cut -f1)）"

# 两个平台的包必须放在同一个 Release 里：一行安装都指向 releases/latest，缺哪个哪个就 404
if [[ -n "$1" ]]; then
  gh release create "$1" build/ClaudeBootAnimation.zip build/ClaudeBootAnimation-Windows.zip --title "Claude 开机动画 $1" \
    --notes "${NOTES:-macOS 13+（Apple 芯片 / Intel）与 Windows 10/11。一行命令安装见 README。}"
fi
