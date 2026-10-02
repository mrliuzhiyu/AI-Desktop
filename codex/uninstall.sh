#!/bin/zsh
# 彻底卸载：停掉常驻、删登录项、删小程序、删设置和日志。ChatGPT.app 本来就没被改过。
UID_=$(id -u)
for L in local.ai-boot-animations.gpt-boot-animation; do
  launchctl bootout "gui/$UID_/$L" 2>/dev/null
  rm -f "$HOME/Library/LaunchAgents/$L.plist"
done
pkill -f 'GPTBootAnimation.app/Contents/MacOS/GPTBootAnimation' 2>/dev/null
rm -rf "$HOME/Applications/GPTBootAnimation.app"
rm -rf "$HOME/Library/Application Support/GPTBootAnimation"
rm -f  "$HOME/Library/Logs/GPTBootAnimation.log"
defaults delete local.ai-boot-animations.gpt-boot-animation 2>/dev/null
echo "已卸载干净。"
