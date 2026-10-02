# GPT 开机动画 · GPT Boot Animation

> 属于 [ai-boot-animations](../README.md) 合集，是 [Claude 开机动画](../claude/) 的姊妹版。

给 ChatGPT 桌面端（Mac，Codex 也是同一个应用）加一段开机动画：每次打开 ChatGPT，先在窗口上播放一段淡紫龙娘的塔罗牌开屏，进入循环画面后慢慢透出真正的 ChatGPT 界面，留下一层半透明的动态背景。

![预览](docs/preview.jpg)

- 🎬 **开屏动画**：发光线稿 → 化为实体 → 展翅、七张塔罗牌亮起 → 牌化作光带环绕 → 光汇入手心成牌 → 甩牌挡嘴，带音效、英文配音和花体字幕
- 🌙 **循环背景**：开屏结束后半透明循环播放，点击直接穿透到 ChatGPT，不影响使用
- ⏭ **跳过**：开屏时右上角有「跳过」，点了会淡出接到循环背景
- 🌙 **菜单栏随时调**：开屏开关、循环背景开关、背景不透明度、开屏音量，拖滑块实时生效（[详细说明](#菜单栏--能干什么)）
- 🧼 **不改 ChatGPT 本体**：它是一个独立的小程序，盖在 ChatGPT 窗口上播放；ChatGPT 自动更新、签名、权限都不受影响，卸载后什么都不留
- 🤝 **可以和 Claude 开机动画同时装**：两个各管各的，菜单栏图标也不一样（Claude 是 ✨，这个是 🌙）

> 非官方同人项目，与 OpenAI 无关。ChatGPT、Codex 是 OpenAI 的商标。

## 安装

### macOS

需要 **macOS 13 或更新**，Apple 芯片和 Intel 都行。

从源码目录安装，需要 Xcode 命令行工具（没有的话先运行 `xcode-select --install`）。在项目根目录执行：

```bash
cd codex
./install.sh
```

装好后会弹出设置窗口。下次打开 ChatGPT 就会播放，ChatGPT 开着时菜单栏右边会出现 🌙 图标（月亮和星星）；想马上看效果，先打开 ChatGPT，再点 🌙 →「播放一次开屏动画」。

不需要辅助功能、屏幕录制等任何系统授权，也不会修改 ChatGPT 本身。

### Windows 10 / 11

从源码安装需要先用 FFmpeg 将动画转为 Windows 可播放的 H.264。在项目根目录执行：

```powershell
ffmpeg -i codex/assets/intro.mp4 -c:v libx264 -crf 23 -pix_fmt yuv420p -c:a copy codex/windows/assets/intro.mp4
ffmpeg -i codex/assets/loop.mp4 -c:v libx264 -crf 23 -pix_fmt yuv420p -an codex/windows/assets/loop.mp4
powershell -ExecutionPolicy Bypass -File codex/windows/install.ps1
```

也可设置环境变量 `GBA_ZIP_URL` 为可信发布包的 HTTPS 地址，再运行 `windows/web-install.ps1`；macOS 的 `web-install.sh` 使用同名变量。项目不再内置个人仓库下载地址。

如果已安装旧版，请先使用旧版自带的卸载功能，再安装此版本；应用内部标识已更换。

## 菜单栏 🌙 能干什么

ChatGPT 开着的时候，菜单栏右边会出现一个月亮 🌙，点开就能调（ChatGPT 没开时图标自动隐藏；想改设置，再打开一次「GPT 开机动画」App 就会弹出设置窗口）。所有调节**拖动时立刻生效**，正在播放的动画马上跟着变，不用重启 ChatGPT。

<p align="center">
  <img src="docs/menu.png" width="300" alt="菜单栏 🌙 菜单">
  &nbsp;&nbsp;
  <img src="docs/settings.png" width="330" alt="设置窗口">
</p>

**打开 ChatGPT 时**放什么，两个开关自由组合：

| 播放开屏动画 | 保留循环背景 | 效果 |
|:---:|:---:|---|
| ✅ | ✅ | 默认：先放开屏，再透出界面，留下半透明动态背景，直到你手动关掉 |
| ✅ | ❌ | 只放开屏，透出界面时整个淡出消失 |
| ❌ | ✅ | 不放开屏，ChatGPT 一打开就直接有动态背景 |
| ❌ | ❌ | 原装启动，什么都不放 |

**两个滑块：**

- **背景不透明度**（0–80%）：循环背景有多浓。调低更不挡字，调高更有氛围。默认 25%
- **开屏音量**：开屏音效和配音的音量。电脑静音时不出声；循环背景本身是静音的

**现在，在 ChatGPT 窗口上：** 不用重启 ChatGPT，直接在当前窗口上「播放一次开屏动画」「开启循环背景」或「关掉动画 / 背景」。循环背景就是从这里关的。

**其他：**

- **跳过**：开屏时右上角有「跳过」，点了会把画面淡出、接到循环背景；进入循环背景后跳过键自动消失
- **设置窗口…**：跟菜单一样的内容，单独一个窗口。找不到 🌙 时，在聚焦搜索（⌘ 空格）里搜 `GPTBootAnimation` 打开也会弹出它
- **打开设置文件…**：更细的参数，比如字幕文字、字幕时间、字幕大小，见下表
- **卸载…**：确认后一键删干净

### 设置文件

`~/Library/Application Support/GPTBootAnimation/config.json`，改完保存，下次播放生效：

| 键 | 默认 | 含义 |
|---|---|---|
| `playIntro` / `loopBackground` | true / true | 同菜单里的两个开关 |
| `residualAlpha` | 0.25 | 循环背景不透明度 |
| `introVolume` | 1.0 | 开屏音量（循环背景是静音的） |
| `fadeStartSeconds` | 11.4 | 开屏里进入循环画面、开始透出界面的时间 |
| `fadeDurationSeconds` | 1.2 | 透出界面的渐变时长 |
| `showSubtitles` | true | 是否显示字幕 |
| `subtitles` | 两句 | 字幕文字和起止秒数 |
| `subtitleFadeSeconds` | 0.5 | 字幕渐显渐隐时长 |
| `subtitleWidthRatio` | 0.6 | 字幕大小（第一句约占窗口宽度的比例） |

## 换成自己的动画

```bash
./build.sh 你的开屏.mp4 你的循环.mp4          # 只打包
./install.sh                                  # 打包并安装（用 assets/ 里的默认动画）
```

想装自己的动画：把两个文件替换成 `assets/intro.mp4`、`assets/loop.mp4` 再运行 `./install.sh`。要求：

- 开屏的最后一帧能接上循环的第一帧，循环首尾相接
- 推荐 H.264 或 HEVC（`hvc1` 标签）；音轨放在开屏文件里，循环文件不用带声音（带了也会被静音）
- 改了开屏长度，记得在设置文件里调 `fadeStartSeconds` 和字幕时间

## 卸载

菜单栏 🌙 →「卸载…」，或者：

```bash
./uninstall.sh
```

会删除小程序、登录自启、设置和日志。ChatGPT 本身从来没被改过，卸载后就是原装的样子。

## 原理

- 小程序登录后在后台常驻（`~/Library/LaunchAgents`），监听 ChatGPT 启动（应用标识 `com.openai.codex`）
- 用 `CGWindowListCopyWindowInfo` 只读取 ChatGPT 主窗口的位置和大小（不读标题和内容，所以不需要授权），在它正上方盖一个无边框窗口播放动画，并跟着窗口移动、缩放，不会盖到别的 App
- 开屏和循环在内存里拼成一条约 8 小时的时间线一次播放，接缝不卡；放到头自动跳回循环开头
- 只在 ChatGPT 启动、真要播放时才在后台拼这条时间线（先拼一条短的应急），播完、跳过或 ChatGPT 关掉就释放；平时后台只占几十 MB
- 进入循环后窗口变半透明并开启点击穿透，ChatGPT 照常使用

日志：`~/Library/Logs/GPTBootAnimation.log`

开发用参数：`--preview`（在当前 ChatGPT 窗口上预览）、`--loop-only`、`--test-skip 秒`、`--repeats N`、`--render-subtitle 背景.png 输出.png 第几句`、`--show-menu`（自动弹出菜单，方便截图）、`--settings`。

## 授权

- 代码：[GPL-3.0](../LICENSE)：可以自由使用、学习和修改；发布改过的版本时必须同样以 GPL-3.0 开源，并保留原作者署名
- 动画素材：[CC BY-NC-ND 4.0](assets/LICENSE.md)，可以安装、分享，不能商用、不能改后再发布。角色是网上流传的 GPT 拟人形象，画面由即梦（Seedance 模型）生成，分镜与动作设计由作者完成
- 音效：Freesound CC0 素材；配音用 ElevenLabs 生成。见 [CREDITS.md](CREDITS.md)

---

**English (short):** An unofficial boot animation for the ChatGPT desktop app (bundle id `com.openai.codex`, which is also Codex) on macOS. A tiny menu-bar helper overlays ChatGPT's window with an intro animation, then fades into a translucent, click-through looping background. It never modifies ChatGPT.app and can be installed alongside the Claude boot animation. Install with `./install.sh` (macOS 13+); uninstall from the 🌙 menu or with `./uninstall.sh`. Code is GPL-3.0; animation assets are CC BY-NC-ND 4.0.
