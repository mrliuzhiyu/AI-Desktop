#!/bin/zsh
# 维护者用：打包 Mac 通用版 build/GPTBootAnimation.zip 和 Windows 版 build/GPTBootAnimation-Windows.zip，供上传到 GitHub Releases
# 用法：./release.sh            只打包
#       ./release.sh gpt-v1.0.0 打包并用 gh 发布这个版本
set -e
cd "${0:A:h}"
./build.sh
OUT=build/release/GPTBootAnimation
rm -rf build/release build/GPTBootAnimation.zip; mkdir -p "$OUT"
ditto build/GPTBootAnimation.app "$OUT/GPTBootAnimation.app"
cp install.sh uninstall.sh ../LICENSE "$OUT/"
cp assets/LICENSE.md "$OUT/ASSETS-LICENSE.md"
ditto -c -k --norsrc --noextattr --keepParent "$OUT" build/GPTBootAnimation.zip
echo "压缩包：build/GPTBootAnimation.zip（$(du -h build/GPTBootAnimation.zip | cut -f1)）"

# Windows 版：源码 + 脚本 + H.264 视频（很多 Windows 播不了 HEVC），用户电脑上用系统自带编译器现场编译
WOUT=build/release/GPTBootAnimation-Windows
rm -f build/GPTBootAnimation-Windows.zip; mkdir -p "$WOUT/assets"
cp -R windows/src windows/build.ps1 windows/install.ps1 windows/uninstall.ps1 "$WOUT/"
cp windows/assets/LibreBodoni-Italic.ttf windows/assets/LibreBodoni-OFL.txt "$WOUT/assets/"
cp ../LICENSE "$WOUT/"; cp assets/LICENSE.md "$WOUT/ASSETS-LICENSE.md"
ffmpeg -v error -y -i assets/intro.mp4 -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart -c:a aac -b:a 160k "$WOUT/assets/intro.mp4"
ffmpeg -v error -y -i assets/loop.mp4 -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart -an "$WOUT/assets/loop.mp4"
ditto -c -k --norsrc --noextattr --keepParent "$WOUT" build/GPTBootAnimation-Windows.zip
echo "压缩包：build/GPTBootAnimation-Windows.zip（$(du -h build/GPTBootAnimation-Windows.zip | cut -f1)）"

# 和 Claude 版共用一个仓库，所以：版本号用 gpt- 开头，并且标成「不是最新」，
# 免得抢走 Claude 版一行安装要用的 releases/latest。发完记得把 web-install.sh 和 windows/web-install.ps1 里的 TAG 改成新版本号。
if [[ -n "$1" ]]; then
  [[ "$1" == gpt-* ]] || { echo "版本号要以 gpt- 开头，例如 gpt-v1.0.0"; exit 1; }
  gh release create "$1" build/GPTBootAnimation.zip build/GPTBootAnimation-Windows.zip --latest=false --title "GPT 开机动画 $1" \
    --notes "${NOTES:-ChatGPT 桌面端（含 Codex）开机动画，macOS 13+（Apple 芯片 / Intel）与 Windows 10/11。一行命令安装见 codex/README.md。}"
fi
