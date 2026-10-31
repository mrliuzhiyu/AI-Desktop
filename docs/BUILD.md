---
status: active
type: guide
verified: 2026-11-21
---
# 构建与本地验证

## 环境

- Python 3.11 或更新；FFmpeg 与 ffprobe 在 PATH 中。
- Windows 10/11：系统 .NET Framework 4.x 编译器与 WPF。
- macOS 13+：Xcode 命令行工具、zsh、lipo、codesign；输出 arm64/x86_64 通用程序。

## 根目录命令

```sh
python scripts/check.py
python scripts/build.py all
python scripts/package.py claude --platform windows --skip-build
python scripts/package.py codex --platform windows --skip-build
python scripts/checksums.py dist
```

macOS 使用 `python3`，打包参数为 `--platform macos`。不带 `--skip-build` 会先构建再打包。
构建输出在组件的 `windows/build/` 或 `build/`，ZIP 与校验和在根目录 `dist/`，均不入库。
Windows 构建自动转码到输出目录，原始媒体不被覆盖。源码安装前先运行检查，再使用各组件安装脚本。

Windows 字幕渲染冒烟检查：`powershell -ExecutionPolicy Bypass -File scripts/smoke-windows.ps1 -Component claude`，GPT 使用 `codex`。
该命令只渲染图片，不安装程序、不设置登录项。输出在被忽略的 `artifacts/`。

## 验证边界

本地检查明确打印平台 SKIP。Windows 通过不代表 macOS 通过，编译通过也不代表 GUI 安装、声音、透明度和登录自启已验证。
安装卸载应在独立测试用户或虚拟机验收；不自动修改日常使用环境。
本地构建不要求联网拉取语言依赖。CI 仅在安装工具链时联网。
