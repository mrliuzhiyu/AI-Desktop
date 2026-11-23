# AI Desktop · 客户端壁纸与 Mac 桌面壁纸

「月下剑光」有两种用法。**Mac 桌面壁纸**在桌面图标后面循环播放，关闭 Claude 后也会继续。**Claude 客户端壁纸**跟随 Claude 窗口，在打开客户端时播放开屏，随后显示半透明背景。两者可以独立安装。

## 选择下载

| 想在哪里看 | macOS 13+（Apple 芯片 / Intel） | Windows 10 / 11 |
|---|---|---|
| Mac 桌面 · 月下剑光 | [下载 Mac 桌面壁纸 App](https://github.com/mrliuzhiyu/AI-Desktop/releases/download/claude-v1.3.0/ClaudeWallpaper-1.3.0-macos.zip) | 仅 macOS |
| Claude 客户端窗口 · 月下剑光 | [下载 Claude 客户端壁纸](https://github.com/mrliuzhiyu/AI-Desktop/releases/download/claude-v1.3.0/ClaudeBootAnimation-1.3.0-macos.zip) | [下载 Claude 客户端壁纸](https://github.com/mrliuzhiyu/AI-Desktop/releases/download/claude-v1.3.0/ClaudeBootAnimation-1.3.0-windows.zip) |
| ChatGPT 客户端窗口 · 星空塔罗 | [下载 GPT 客户端动画](https://github.com/mrliuzhiyu/AI-Desktop/releases/download/gpt-v1.1.1/GPTBootAnimation-1.1.1-macos.zip) | [下载 GPT 客户端动画](https://github.com/mrliuzhiyu/AI-Desktop/releases/download/gpt-v1.1.1/GPTBootAnimation-1.1.1-windows.zip) |

每个 ZIP 的 SHA-256 校验文件随版本提供。[Claude 1.3.0](https://github.com/mrliuzhiyu/AI-Desktop/releases/tag/claude-v1.3.0) · [GPT 1.1.1](https://github.com/mrliuzhiyu/AI-Desktop/releases/tag/gpt-v1.1.1)

### 安装 Mac 桌面壁纸

解压 ZIP，把 **Claude 壁纸.app** 拖到「应用程序」后双击。点菜单栏的月亮图标，可暂停、退出或勾选 **登录时自动播放**。更新时先退出壁纸，再用新版替换「应用程序」中的同名 App。首次打开遇到系统提示时，参阅[安装说明](claude/README.md#mac-独立桌面壁纸)。

### 安装 Claude 或 ChatGPT 客户端壁纸

完整解压对应系统的客户端安装包，按下面的步骤运行安装脚本。

**Windows：** 在解压后的程序目录打开 PowerShell，运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

**macOS：** 在解压后的程序目录打开终端，运行：

```sh
zsh install.sh
```

安装后会配置登录启动并打开设置窗口。macOS 包采用本地临时签名，尚未做开发者公证；不要将它视为已公证的软件。已有旧版请先用旧版自带的卸载功能卸载。

每个安装包附带同名 `.sha256` 校验文件，可在对应版本页下载。需要自行编译时再使用下方源码安装步骤。

## 客户端壁纸效果

<table>
<tr>
<th>Claude · 月下剑光</th>
<th>GPT（ChatGPT）· 星空塔罗</th>
</tr>
<tr>
<td><a href="claude/README.md"><img src="claude/docs/preview.jpg" width="460" alt="Claude 动画预览：月光下的持剑角色"></a></td>
<td><a href="codex/README.md"><img src="codex/docs/preview.jpg" width="460" alt="ChatGPT 动画预览：星空中的塔罗角色"></a></td>
</tr>
<tr>
<td>线稿、剑光与火凤，搭配开屏音效。</td>
<td>展翅、发光牌阵与光带，搭配开屏配音。</td>
</tr>
</table>

- **开屏与背景分别控制**：只播放开屏、只保留背景，或同时开启。
- **不妨碍操作**：循环背景支持点击穿透，继续使用原应用。
- **即时调节**：在菜单栏或托盘里调整背景透明度和开屏音量。
- **随时退出**：跳过开屏、关闭背景，或从菜单卸载。
- **独立使用**：两个动画程序可以同时安装，分别控制。

## 客户端壁纸支持平台

| 平台 | 系统要求 | 控制入口 |
|---|---|---|
| Windows | Windows 10 / 11 | 系统托盘 |
| macOS | macOS 13 或更新，Apple 芯片与 Intel | 菜单栏 |

目标是本机运行的 Claude 或 ChatGPT 桌面应用，不用于浏览器网页。具体识别方式和参数见 [Claude 说明](claude/README.md) / [ChatGPT 说明](codex/README.md)。

## 从源码安装客户端壁纸

源码安装需要 **Python 3.11+、FFmpeg 和 Git**，且相关命令已加入 PATH。Windows 使用系统自带的 .NET Framework 编译器；macOS 还需要 Xcode 命令行工具。

先获取项目：

```sh
git clone https://github.com/mrliuzhiyu/AI-Desktop.git
cd AI-Desktop
```

### Windows

在项目根目录打开 PowerShell，先检查环境与项目：

```powershell
python scripts/check.py
```

选择需要的动画安装。安装脚本会自动构建、准备视频、设置登录启动并打开设置窗口。

**Claude：**

```powershell
powershell -ExecutionPolicy Bypass -File .\claude\windows\install.ps1
```

**GPT（ChatGPT）：**

```powershell
powershell -ExecutionPolicy Bypass -File .\codex\windows\install.ps1
```

### macOS

在项目根目录运行检查，再选择一个安装命令：

```sh
python3 scripts/check.py

# Claude
zsh claude/install.sh

# ChatGPT
zsh codex/install.sh
```

当前构建采用本地临时签名，未做开发者公证。系统安全提示与实际运行验证范围见 [构建说明](docs/BUILD.md) 和 [验收记录](docs/VERIFICATION.md)。

> 已安装旧版的用户，请先使用旧版菜单中的卸载功能，再安装当前版本。

## 客户端设置与使用

安装后打开对应应用，通过菜单栏或系统托盘进入设置。开屏和循环背景可以分别开关，调整立即生效。

<table>
<tr>
<th>Claude 设置</th>
<th>GPT 设置</th>
</tr>
<tr>
<td><img src="claude/docs/settings.png" width="300" alt="Claude 设置：开屏、循环背景、透明度和音量"></td>
<td><img src="codex/docs/settings.png" width="300" alt="ChatGPT 设置：开屏、循环背景、透明度和音量"></td>
</tr>
</table>

截图展示 macOS 设置界面；Windows 通过托盘菜单提供对应控制。

需要修改字幕、播放时间或替换自己的视频时，请阅读 [Claude 参数说明](claude/README.md#设置文件) 或 [ChatGPT 参数说明](codex/README.md#设置文件)。

## 卸载客户端壁纸

使用菜单栏或托盘菜单中的“卸载”。也可运行安装目录内的卸载脚本；macOS 对应源码目录中的 `uninstall.sh`。

卸载会移除动画程序、登录启动项及其设置和日志，不卸载 Claude 或 ChatGPT。

## 开发与验证

只构建、不安装：

```sh
python scripts/build.py all
```

macOS 使用 `python3`。构建产物保存在各组件的 `build/` 目录，安装包输出至 `dist/`。

- [构建与本地验证](docs/BUILD.md)
- [版本与发布](docs/RELEASE.md)
- [验收记录与项目历史说明](docs/VERIFICATION.md)
- [自动检查状态](https://github.com/mrliuzhiyu/AI-Desktop/actions)

## 授权与素材

代码使用 [GPL-3.0](LICENSE)。动画素材另按各自许可提供，不能将代码许可视为素材的商用授权：

- [Claude 动画许可](claude/assets/LICENSE.md) · [音效来源](claude/CREDITS.md)
- [ChatGPT 动画许可](codex/assets/LICENSE.md) · [音效与配音来源](codex/CREDITS.md)

字体的许可文件随源码和安装包保留。项目基于既有源码与素材整理，第三方作品的权利仍归各自权利人。

本项目与 Anthropic、OpenAI 无官方关联。Claude、ChatGPT 和 Codex 名称仅用于说明兼容的应用。
