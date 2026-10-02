#!/bin/zsh
# 彻底卸载：停掉常驻、删登录项、删小程序、删设置和日志。Claude.app 本来就没被改过。
UID_=$(id -u)
for L in local.ai-boot-animations.claude-boot-animation; do
  launchctl bootout "gui/$UID_/$L" 2>/dev/null
  rm -f "$HOME/Library/LaunchAgents/$L.plist"
done
pkill -f 'ClaudeBootAnimation.app/Contents/MacOS/ClaudeBootAnimation' 2>/dev/null
rm -rf "$HOME/Applications/ClaudeBootAnimation.app"
rm -rf "$HOME/Library/Application Support/ClaudeBootAnimation"
rm -f  "$HOME/Library/Logs/ClaudeBootAnimation.log"
defaults delete local.ai-boot-animations.claude-boot-animation 2>/dev/null
echo "已卸载干净。"
