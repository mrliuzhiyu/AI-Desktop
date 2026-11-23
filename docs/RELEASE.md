---
status: active
type: guide
verified: 2026-11-23
---
# 版本与发布

`versions.json` 是版本唯一来源；`components.json` 定义名称、标识与标签前缀。
Claude 标签为 `claude-v<版本>`，GPT 为 `gpt-v<版本>`。版本使用三段非负整数，不接受前导零。

## 流程

1. 修改版本文件，同次更新相关说明；运行本地检查和平台构建。
2. 在 main 上提交，确认两平台 CI 通过。
3. 需要发版时再推送对应标签，或对已存在标签手动运行 Release Packages。
4. 工作流验证标签与版本、构建两平台 ZIP、检查结构、输出 SHA-256，创建草稿 Release。
5. 人工检查安装、卸载、素材和许可后再发布草稿。普通 main 推送不创建 Release。

下载脚本没有默认个人仓库地址：Claude 使用 `CBA_ZIP_URL`，GPT 使用 `GBA_ZIP_URL`，值必须为可信 HTTPS ZIP 地址。
Windows 包含已编译 EXE 和完整素材，不需要用户安装 Python 或编译器；macOS 包含通用应用，当前仅 ad-hoc 签名，不包含开发者公证。
校验和是完整性检查，不替代发布者身份认证。发布前核对许可证与素材分发条件。
安装旧版本的用户先使用旧版卸载功能，再安装更换内部标识后的版本。

CI 截图只作为短期附件保存，不创建结果分支。日志、构建产物与本地凭据不提交。

## Claude 桌面壁纸与客户端包

每个 Claude 版本包含三个独立 ZIP。两个 `ClaudeBootAnimation` 包分别用于 Windows、macOS 的 Claude 客户端窗口，`ClaudeWallpaper-<版本>-macos.zip` 用于 Mac 桌面。桌面壁纸与 Claude 组件共用版本和发布标签。

本地运行 `zsh claude/package-wallpaper.sh` 后，脚本会解压并验证通用二进制、App 签名、版本、循环原片与许可。发布工作流为三个 ZIP 分别生成校验文件。发布草稿前核对 README 的直链文件名；发布后从公开下载地址取回 ZIP 检查 SHA-256。桌面壁纸的安装验收包括从「应用程序」启动、菜单栏控制，以及启用后在系统登录项中看到对应 App。
