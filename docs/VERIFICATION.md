---
status: active
type: progress
verified: 2026-11-22
---
# 基建验收记录

## 本地实际执行

- 两组件 Windows 完整构建通过；输出媒体为 H.264/yuv420p、开屏 AAC、循环静音。
- 两组件程序集版本与 versions.json 一致。
- 两组件各两张字幕图片实际渲染成功。
- 两套 Windows ZIP 的文件结构、许可证、媒体和 CRC 检查通过，已生成 SHA-256。
- 10 项组件、版本、安装包负向场景测试通过。
- PowerShell 语法、文档状态头与索引、Git diff 格式检查通过。
- GitHub 工作流通过 actionlint 1.7.12 静态检查。
- 当前文件及可达历史的账号、私人邮箱、个人目录、常见凭据格式扫描通过。

## 边界

正则扫描不是无泄漏证明。媒体元数据与现有文档截图已检查，动画已抽帧检查；不宣称逐帧、音频语义或隐写审计。
保留字体、音效的公开许可署名及品牌兼容性标识，这些不属于当前维护者的私人资料。
Windows 编译仍有两个未赋值字段警告（每个组件各两处）；不是编译失败。
远端 Windows 与 macOS 两组件矩阵、字幕渲染、双架构校验、签名检查及两平台 ZIP 校验均已通过。GUI 安装／卸载、开机自启仍未实机验证。macOS 使用 ad-hoc 签名，尚未公证。

## 历史说明

本仓库从既有源码和素材基线重建。按维护者要求，34 个基建提交及其后续文档、修复提交重新安排到 2026 年 10 月的 12 个晚间日期，每日若干次；它们是整理后的 Git 日期，不代表实际开发或公开日期。本轮核实日期为上方 verified。
提交作者使用维护者指定的 joy 与 GitHub noreply 邮箱。第三方代码和素材不因重建提交历史而转移著作权。

## 远端证据

以下记录对应实现提交 `e6ffae9`，全部成功：

- [Windows 两组件构建、字幕与安装包](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44919963405)
- [macOS 两组件通用构建、签名与安装包](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44919963401)
- [完整历史隐私规则扫描](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44919963454)
- [文档检查](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44919963417)

远端为 Public，仅 main 分支。2026-11-22（北京时间）已发布两个版本，各含 Windows、macOS ZIP 与对应 SHA-256 文件：

- [Claude 1.2.1](https://github.com/mrliuzhiyu/AI-Desktop/releases/tag/claude-v1.2.1) · [发布构建](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44922020408)
- [GPT 1.1.1](https://github.com/mrliuzhiyu/AI-Desktop/releases/tag/gpt-v1.1.1) · [发布构建](https://github.com/mrliuzhiyu/AI-Desktop/actions/runs/44922020969)

发布前已从草稿 Release 下载四个安装包，逐一复核 SHA-256、ZIP 结构、媒体编码和程序版本。上述安装／卸载与代码签名边界仍适用。文档 verified 日期按北京时间校验，避免 UTC runner 在跨午夜时误报未来日期。
