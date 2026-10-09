#!/bin/zsh
set -euo pipefail
[[ "$(uname)" == Darwin ]] || { echo 'macOS is required'; exit 1; }
for tool in python3 swiftc xcrun lipo codesign ditto ffmpeg ffprobe; do
  command -v "$tool" >/dev/null || { echo "Missing tool: $tool"; exit 1; }
done
python3 -c 'import sys; assert sys.version_info >= (3, 11), "Python 3.11+ required"'
xcrun --show-sdk-path >/dev/null
echo 'macOS build prerequisites OK'
