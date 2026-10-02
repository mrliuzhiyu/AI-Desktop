#!/bin/zsh
# 下载并安装指定的发布包；先设置 CBA_ZIP_URL 为安装包的 HTTPS 地址。
set -e
URL="${CBA_ZIP_URL:-}"
[[ "$URL" == https://* ]] || { echo "请先设置 CBA_ZIP_URL 为可信安装包的 HTTPS 地址"; exit 1; }
[[ "$(uname)" == Darwin ]] || { echo "只支持 macOS"; exit 1; }
MAJOR=$(sw_vers -productVersion | cut -d. -f1)
(( MAJOR >= 13 )) || { echo "需要 macOS 13 或更新，你的是 $(sw_vers -productVersion)"; exit 1; }
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
echo "⬇️  正在下载 Claude 开机动画（约 50MB）…"
curl -fL --progress-bar -o "$TMP/pkg.zip" "$URL"
ditto -x -k "$TMP/pkg.zip" "$TMP/pkg"
zsh "$TMP/pkg/ClaudeBootAnimation/install.sh"
