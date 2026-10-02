// Claude 开机动画 · Windows 版 —— 外置浮层小程序
// 检测 Claude 桌面端启动 → 盖在它的主窗口上播放开屏动画 → 进入循环帧后半透明、点击穿透，作为循环背景一直播放。
// 不修改 Claude 的任何文件。用系统自带的 .NET Framework 4.x 编译器编译（C# 5 语法），不需要装任何 SDK。
//
// Copyright (C) 2026 original author (name redacted)
// 本程序以 GNU GPL v3 发布，见仓库根目录 LICENSE。
// This program is free software under the GNU General Public License v3.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WF = System.Windows.Forms;
using SD = System.Drawing;

namespace ClaudeBootAnimation
{
    // ───────────────────────── 配置 ─────────────────────────

    public class Subtitle
    {
        public string Text; public double Start; public double End;
        public Subtitle(string t, double s, double e) { Text = t; Start = s; End = e; }
    }

    public class Config
    {
        public bool PlayIntro = true;            // Claude 启动时播放开屏动画
        public bool LoopBackground = true;       // 开屏后保留半透明循环背景
        public double FadeStartSeconds = 10.767; // 成片里进入循环帧的时间点
        public double FadeDurationSeconds = 1.2; // 从不透明淡到半透明用多久
        public double ResidualAlpha = 0.25;      // 循环背景不透明度
        public double IntroVolume = 1.0;         // 开屏音量（再乘系统音量）
        public bool ShowSubtitles = true;
        public double SubtitleFadeSeconds = 0.5;
        public double SubtitleWidthRatio = 0.6;  // 第一句字幕约占窗口宽度的比例
        public List<Subtitle> Subtitles = new List<Subtitle> {
            new Subtitle("Under the moonlight... the blade awakens.", 4.087, 7.189),
            new Subtitle("I am Claude. Ask me anything.", 8.281, 11.3),
        };

        public static string Dir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClaudeBootAnimation"); } }
        public static string FilePath { get { return Path.Combine(Dir, "config.json"); } }

        static double D(Dictionary<string, object> d, string k, double def)
        {
            object v; if (d.TryGetValue(k, out v) && v != null) { try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); } catch { } }
            return def;
        }
        static bool B(Dictionary<string, object> d, string k, bool def)
        {
            object v; if (d.TryGetValue(k, out v) && v is bool) return (bool)v;
            return def;
        }

        public static Config Load()
        {
            var c = new Config();
            try
            {
                if (File.Exists(FilePath))
                {
                    var d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath, Encoding.UTF8)) as Dictionary<string, object>;
                    if (d != null)
                    {
                        c.PlayIntro = B(d, "playIntro", c.PlayIntro);
                        c.LoopBackground = B(d, "loopBackground", c.LoopBackground);
                        c.FadeStartSeconds = D(d, "fadeStartSeconds", c.FadeStartSeconds);
                        c.FadeDurationSeconds = D(d, "fadeDurationSeconds", c.FadeDurationSeconds);
                        c.ResidualAlpha = D(d, "residualAlpha", c.ResidualAlpha);
                        c.IntroVolume = D(d, "introVolume", c.IntroVolume);
                        c.ShowSubtitles = B(d, "showSubtitles", c.ShowSubtitles);
                        c.SubtitleFadeSeconds = D(d, "subtitleFadeSeconds", c.SubtitleFadeSeconds);
                        c.SubtitleWidthRatio = D(d, "subtitleWidthRatio", c.SubtitleWidthRatio);
                        object subs;
                        if (d.TryGetValue("subtitles", out subs) && subs is object[])
                        {
                            var list = new List<Subtitle>();
                            foreach (var o in (object[])subs)
                            {
                                var s = o as Dictionary<string, object>;
                                if (s == null || !s.ContainsKey("text")) continue;
                                list.Add(new Subtitle(Convert.ToString(s["text"]), D(s, "start", 0), D(s, "end", 0)));
                            }
                            c.Subtitles = list;
                        }
                    }
                }
            }
            catch (Exception e) { Log.W("设置文件读不了，用默认值：" + e.Message); }
            c.Save(); // 补齐新字段
            return c;
        }

        static string N(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
        static string L(bool v) { return v ? "true" : "false"; }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var js = new JavaScriptSerializer();
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append("  \"fadeDurationSeconds\" : " + N(FadeDurationSeconds) + ",\n");
                sb.Append("  \"fadeStartSeconds\" : " + N(FadeStartSeconds) + ",\n");
                sb.Append("  \"introVolume\" : " + N(IntroVolume) + ",\n");
                sb.Append("  \"loopBackground\" : " + L(LoopBackground) + ",\n");
                sb.Append("  \"playIntro\" : " + L(PlayIntro) + ",\n");
                sb.Append("  \"residualAlpha\" : " + N(ResidualAlpha) + ",\n");
                sb.Append("  \"showSubtitles\" : " + L(ShowSubtitles) + ",\n");
                sb.Append("  \"subtitleFadeSeconds\" : " + N(SubtitleFadeSeconds) + ",\n");
                sb.Append("  \"subtitleWidthRatio\" : " + N(SubtitleWidthRatio) + ",\n");
                sb.Append("  \"subtitles\" : [\n");
                for (int i = 0; i < Subtitles.Count; i++)
                {
                    var s = Subtitles[i];
                    sb.Append("    {\n      \"end\" : " + N(s.End) + ",\n      \"start\" : " + N(s.Start) + ",\n      \"text\" : " + js.Serialize(s.Text) + "\n    }");
                    sb.Append(i < Subtitles.Count - 1 ? ",\n" : "\n");
                }
                sb.Append("  ]\n}\n");
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { Log.W("设置文件写不了：" + e.Message); }
        }
    }

    // ───────────────────────── 日志 ─────────────────────────

    public static class Log
    {
        public static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudeBootAnimation", "log.txt");
        static readonly object Lock = new object();
        public static void W(string msg)
        {
            try
            {
                lock (Lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    var fi = new FileInfo(FilePath);
                    if (fi.Exists && fi.Length > 2 * 1024 * 1024) fi.Delete();
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + msg + "\r\n", Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    // ───────────────────────── Win32 ─────────────────────────

    public static class Native
    {
        public delegate bool EnumProc(IntPtr h, IntPtr l);
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")] static extern int GetWindowLong32(IntPtr h, int i);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")] static extern IntPtr GetWindowLongPtr64(IntPtr h, int i);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] static extern int SetWindowLong32(IntPtr h, int i, int v);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] static extern IntPtr SetWindowLongPtr64(IntPtr h, int i, IntPtr v);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int v, int size);

        public const int GWL_EXSTYLE = -20, GWLP_HWNDPARENT = -8;
        public const long WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x8000000, WS_EX_APPWINDOW = 0x40000;
        public const uint SWP_NOSIZE = 1, SWP_NOMOVE = 2, SWP_NOZORDER = 4, SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40, SWP_NOOWNERZORDER = 0x200;
        public const uint LWA_ALPHA = 2, GW_OWNER = 4;

        public static long GetLong(IntPtr h, int i) { return IntPtr.Size == 8 ? GetWindowLongPtr64(h, i).ToInt64() : GetWindowLong32(h, i); }
        public static void SetLong(IntPtr h, int i, long v)
        {
            if (IntPtr.Size == 8) SetWindowLongPtr64(h, i, new IntPtr(v)); else SetWindowLong32(h, i, (int)v);
        }

        /// 窗口在屏幕上实际占的矩形（去掉 Windows 10/11 看不见的阴影边框），物理像素
        public static bool Bounds(IntPtr h, out RECT r)
        {
            if (DwmGetWindowAttribute(h, 9 /*DWMWA_EXTENDED_FRAME_BOUNDS*/, out r, Marshal.SizeOf(typeof(RECT))) == 0 && r.Right > r.Left) return true;
            return GetWindowRect(h, out r);
        }
        public static bool Cloaked(IntPtr h)
        {
            int v; return DwmGetWindowAttribute(h, 14 /*DWMWA_CLOAKED*/, out v, 4) == 0 && v != 0;
        }
        public static string ClassOf(IntPtr h) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }
        public static string TitleOf(IntPtr h) { var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
    }

    // ───────────────────────── 找 Claude 的主窗口 ─────────────────────────

    public struct Target { public IntPtr Hwnd; public uint Pid; public bool Valid { get { return Hwnd != IntPtr.Zero; } } }

    public static class Finder
    {
        /// 测试用：按窗口标题找（比如记事本），不找 Claude
        public static string TestTitle;
        static readonly Dictionary<uint, string> NameCache = new Dictionary<uint, string>();

        static string ProcName(uint pid)
        {
            string n;
            if (NameCache.TryGetValue(pid, out n)) return n;
            try { n = Process.GetProcessById((int)pid).ProcessName; } catch { n = ""; }
            if (NameCache.Count > 500) NameCache.Clear();
            NameCache[pid] = n;
            return n;
        }

        /// 找 Claude 最大的那个可见主窗口。Claude 是 Electron 程序，会有好几个进程和隐藏窗口，只挑看得见、够大的顶层窗口
        public static Target Find()
        {
            Target best = new Target(); long bestArea = 0;
            uint self = (uint)Process.GetCurrentProcess().Id;
            Native.EnumWindows(delegate (IntPtr h, IntPtr l)
            {
                if (!Native.IsWindowVisible(h) || Native.IsIconic(h) || Native.Cloaked(h)) return true;
                if (Native.GetWindow(h, Native.GW_OWNER) != IntPtr.Zero) return true;
                uint pid; Native.GetWindowThreadProcessId(h, out pid);
                if (pid == self) return true;
                if (TestTitle != null)
                {
                    if (Native.TitleOf(h).IndexOf(TestTitle, StringComparison.OrdinalIgnoreCase) < 0) return true;
                }
                else
                {
                    if (Native.ClassOf(h) != "Chrome_WidgetWin_1") return true;
                    if (!string.Equals(ProcName(pid), "claude", StringComparison.OrdinalIgnoreCase)) return true;
                    if (Native.GetWindowTextLength(h) == 0) return true;
                }
                Native.RECT r; if (!Native.Bounds(h, out r)) return true;
                int w = r.Right - r.Left, ht = r.Bottom - r.Top;
                if (w < 400 || ht < 300) return true;
                long area = (long)w * ht;
                if (area > bestArea) { bestArea = area; best.Hwnd = h; best.Pid = pid; }
                return true;
            }, IntPtr.Zero);
            return best;
        }

        public static bool ProcessAlive(uint pid)
        {
            try { return !Process.GetProcessById((int)pid).HasExited; } catch { return false; }
        }
    }

    // ───────────────────────── 字幕 ─────────────────────────

    /// 英文花体字幕：Libre Bodoni 斜体，白字 + 黑色描边 + 柔和阴影
    public class SubtitleElement : FrameworkElement
    {
        public static FontFamily Font = new FontFamily("Georgia");
        string text;
        public double WidthRatio = 0.6;
        public string RefText = "Under the moonlight... the blade awakens.";

        public SubtitleElement() { IsHitTestVisible = false; Opacity = 0; }
        public void SetText(string t) { text = t; InvalidateVisual(); }

        FormattedText Make(string s, double size)
        {
#pragma warning disable 618
            return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Font, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal), size, Brushes.White);
#pragma warning restore 618
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (string.IsNullOrEmpty(text) || ActualWidth < 10) return;
            double W = ActualWidth, H = ActualHeight;
            // 字号：让第一句约占窗口宽度的 WidthRatio；窗口特别扁时按高度限一下
            double refW = Make(RefText, 100).WidthIncludingTrailingWhitespace;
            double size = refW > 0 ? W * WidthRatio * 100 / refW : 24;
            size = Math.Max(18, Math.Min(size, H * 0.09));
            var ft = Make(text, size);
            double x = (W - ft.Width) / 2, y = H * 0.85 - ft.Height / 2; // 字的中线在离底边 15% 处
            var geo = ft.BuildGeometry(new Point(x, y));
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(230, 0, 0, 0)), size * 0.1) { LineJoin = PenLineJoin.Round };
            dc.DrawGeometry(null, pen, geo);
            dc.DrawGeometry(Brushes.White, null, geo);
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = size * 0.25, ShadowDepth = 0, Opacity = 0.75 };
        }
    }

    // ───────────────────────── 一次播放 ─────────────────────────

    public class Show
    {
        enum Phase { Intro, Revealed, Ending, Done }
        Phase phase = Phase.Intro;

        public readonly Target Target;
        Config config;
        readonly bool withIntro;
        readonly Action onDone;

        Window overlay, top;
        IntPtr overlayH, topH;
        Grid grid, root;
        int styleFixes;
        MediaElement intro, loopA, loopB, activeLoop;
        // 换播放器时：新的那个真正出帧之前，旧的一直盖在上面（不然机器忙的时候会闪黑）
        MediaElement swapFrom, swapTo; DateTime swapT0;
        SubtitleElement sub;
        Border skipBtn;
        DispatcherTimer tick;
        int currentSub = -1;
        bool introGone, hidden;
        Native.RECT lastRect;

        // 整窗透明度的渐变
        double alpha, alphaFrom, alphaTo; DateTime alphaT0; double alphaDur = -1; Action alphaThen;
        bool clickThrough;
        // 开屏音量渐变
        double volFrom, volTo; DateTime volT0; double volDur = -1;

        static string Asset(string name) { return Path.Combine(App.AssetsDir, name); }

        public Show(Target t, Config c, bool withIntro, Action onDone)
        {
            Target = t; config = c; this.withIntro = withIntro; this.onDone = onDone;

            grid = new Grid { Background = withIntro ? Brushes.Black : Brushes.Transparent, ClipToBounds = true };
            loopA = MakePlayer("loop.mp4", true);
            loopB = MakePlayer("loop.mp4", true);
            Panel.SetZIndex(loopA, 1); Panel.SetZIndex(loopB, 0);
            grid.Children.Add(loopA); grid.Children.Add(loopB);
            loopA.MediaEnded += delegate { SwapLoop(loopA, loopB); };
            loopB.MediaEnded += delegate { SwapLoop(loopB, loopA); };
            activeLoop = loopA;

            if (withIntro)
            {
                intro = MakePlayer("intro.mp4", false);
                intro.Volume = config.IntroVolume;
                Panel.SetZIndex(intro, 2);
                grid.Children.Add(intro);
                intro.MediaEnded += delegate { IntroEnded(); };
                intro.MediaFailed += delegate (object s, ExceptionRoutedEventArgs e) { Log.W("开屏视频播不了：" + e.ErrorException.Message); };
            }
            else introGone = true;

            // 一个窗口三层：最下面视频（整体调透明度做半透明背景），上面字幕和「跳过」（不跟着变淡）
            root = new Grid { Background = Brushes.Transparent };
            root.Children.Add(grid);
            overlay = new Window
            {
                WindowStyle = WindowStyle.None, AllowsTransparency = true, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
                ShowActivated = false, Background = Brushes.Transparent, Content = root,
                Left = -32000, Top = -32000, Width = 10, Height = 10, Title = "Claude Boot Animation"
            };
            clickThrough = !withIntro;
            alpha = withIntro ? 1 : 0;
            grid.Opacity = alpha;
            overlay.SourceInitialized += delegate
            {
                overlayH = new WindowInteropHelper(overlay).Handle;
                EnsureOverlayStyle(true);
                // 设成 Claude 窗口的「附属窗口」：系统自动让它一直在 Claude 上面，Claude 被别的窗口挡住时它也一起被挡住
                Native.SetLong(overlayH, Native.GWLP_HWNDPARENT, Target.Hwnd.ToInt64());
            };

            if (withIntro)
            {
                var topGrid = root;
                sub = new SubtitleElement { WidthRatio = config.SubtitleWidthRatio };
                if (config.Subtitles.Count > 0) sub.RefText = config.Subtitles[0].Text;
                topGrid.Children.Add(sub);
                skipBtn = new Border
                {
                    Width = 72, Height = 28, CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(Color.FromArgb(115, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 44, 18, 0), Cursor = Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = "跳过 ›", Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.Medium,
                        FontFamily = new FontFamily("Microsoft YaHei UI"),
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                    }
                };
                skipBtn.MouseLeftButtonUp += delegate { SkipIntro(); };
                topGrid.Children.Add(skipBtn);
            }

            overlay.Show();
            EnsureOverlayStyle(true); // WPF 显示窗口时会重写扩展样式，显示后再补一次
            if (top != null) top.Show();
            Follow(true);
            Log.W("浮层样式 0x" + Native.GetLong(overlayH, Native.GWL_EXSTYLE).ToString("X"));

            if (withIntro)
            {
                intro.Play();
                Log.W("开始播放开屏动画，窗口 " + Target.Hwnd + " " + RectStr(lastRect));
            }
            else
            {
                loopA.Play();
                phase = Phase.Revealed;
                RampAlpha(config.ResidualAlpha, config.FadeDurationSeconds, null);
                Log.W("直接开启循环背景，窗口 " + Target.Hwnd);
            }

            tick = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };
            tick.Tick += delegate { Tick(); };
            tick.Start();
        }

        static string RectStr(Native.RECT r) { return "(" + r.Left + "," + r.Top + " " + (r.Right - r.Left) + "x" + (r.Bottom - r.Top) + ")"; }

        MediaElement MakePlayer(string file, bool muted)
        {
            var m = new MediaElement
            {
                LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Manual,
                Stretch = Stretch.UniformToFill, IsMuted = muted, ScrubbingEnabled = muted,
                Source = new Uri(Asset(file), UriKind.Absolute)
            };
            if (muted)
            {
                // 预先停在第一帧，轮到它时立刻接上，不会黑一下
                bool primed = false;
                m.MediaOpened += delegate
                {
                    if (primed) return; primed = true;
                    if (m != activeLoop || phase == Phase.Intro) { m.Pause(); m.Position = TimeSpan.Zero; }
                };
                m.Play();
            }
            return m;
        }

        /// 两个播放器轮流放循环段：一个放完，另一个已经停在第一帧，马上接上
        void SwapLoop(MediaElement done, MediaElement next)
        {
            if (phase == Phase.Done) return;
            next.Play();
            activeLoop = next;
            BeginSwap(done, next); // done 停在最后一帧盖着，等 next 动起来再换上去
        }

        void BeginSwap(MediaElement from, MediaElement to)
        {
            if (swapFrom != null && swapFrom != from) FinishSwap();
            swapFrom = from; swapTo = to; swapT0 = DateTime.Now;
        }

        // 每帧检查：新播放器的进度走起来了（画面已经出来），才把旧的撤掉
        void CheckSwap()
        {
            if (swapFrom == null) return;
            bool moving = swapTo.Position.TotalSeconds > 0.06;
            if (moving || (DateTime.Now - swapT0).TotalSeconds > 1.5) FinishSwap();
        }

        void FinishSwap()
        {
            var from = swapFrom; var to = swapTo;
            swapFrom = null; swapTo = null;
            if (from == intro)
            {
                intro.Visibility = Visibility.Collapsed;
                intro.Stop(); intro.Close();
                return;
            }
            Panel.SetZIndex(to, 1); Panel.SetZIndex(from, 0);
            from.Pause(); from.Position = TimeSpan.Zero; // 退回第一帧备用（ScrubbingEnabled 让它暂停时也解出这一帧）
        }

        void IntroEnded()
        {
            if (introGone) return;
            introGone = true;
            Log.W("开屏放完，接循环段");
            loopA.Play();
            activeLoop = loopA;
            BeginSwap(intro, loopA); // 开屏停在最后一帧盖着，循环段动起来再撤
            if (phase == Phase.Intro) Reveal(false);
            CloseTop();
        }

        void CloseTop()
        {
            if (skipBtn != null) skipBtn.Visibility = Visibility.Collapsed;
        }

        // ── 每 15ms：跟随窗口、字幕、渐变 ──
        void Tick()
        {
            if (phase == Phase.Done) return;
            Follow(false);
            if (phase == Phase.Done) return;
            EnsureOverlayStyle(false);
            CheckSwap();

            if (!introGone && intro != null)
            {
                double t = intro.Position.TotalSeconds;
                if (phase == Phase.Intro && t >= config.FadeStartSeconds) Reveal(false);
                UpdateSubtitle(t);
            }
            if (alphaDur >= 0)
            {
                double k = alphaDur <= 0 ? 1 : Math.Min(1, (DateTime.Now - alphaT0).TotalSeconds / alphaDur);
                k = k * k * (3 - 2 * k);
                SetAlpha(alphaFrom + (alphaTo - alphaFrom) * k);
                if (k >= 1) { alphaDur = -1; var then = alphaThen; alphaThen = null; if (then != null) then(); }
            }
            if (volDur >= 0 && intro != null)
            {
                double k = volDur <= 0 ? 1 : Math.Min(1, (DateTime.Now - volT0).TotalSeconds / volDur);
                intro.Volume = volFrom + (volTo - volFrom) * k;
                if (k >= 1) volDur = -1;
            }
        }

        /// 跟着 Claude 窗口移动、缩放；Claude 最小化或藏到托盘时一起藏起来；Claude 关了就收工
        void Follow(bool force)
        {
            if (!Native.IsWindow(Target.Hwnd))
            {
                Log.W("Claude 窗口没了，收工");
                Teardown(); return;
            }
            bool vis = Native.IsWindowVisible(Target.Hwnd) && !Native.IsIconic(Target.Hwnd) && !Native.Cloaked(Target.Hwnd);
            if (!vis)
            {
                if (!hidden) { hidden = true; ShowAll(false); }
                return;
            }
            if (hidden) { hidden = false; ShowAll(true); force = true; }
            Native.RECT r;
            if (!Native.Bounds(Target.Hwnd, out r)) return;
            if (!force && r.Left == lastRect.Left && r.Top == lastRect.Top && r.Right == lastRect.Right && r.Bottom == lastRect.Bottom) return;
            lastRect = r;
            uint f = Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER;
            if (overlayH != IntPtr.Zero) Native.SetWindowPos(overlayH, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top, f);
            if (topH != IntPtr.Zero) Native.SetWindowPos(topH, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top, f);
        }

        void ShowAll(bool show)
        {
            int cmd = show ? 4 /*SW_SHOWNOACTIVATE*/ : 0 /*SW_HIDE*/;
            if (overlayH != IntPtr.Zero) Native.ShowWindow(overlayH, cmd);
            if (topH != IntPtr.Zero) Native.ShowWindow(topH, cmd);
        }

        /// 分层（整窗半透明）+ 不出现在任务栏 + 不抢焦点；透出后再加点击穿透。被冲掉就补回来
        void EnsureOverlayStyle(bool force)
        {
            if (overlayH == IntPtr.Zero) return;
            long ex = Native.GetLong(overlayH, Native.GWL_EXSTYLE);
            long want = (ex | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE) & ~Native.WS_EX_APPWINDOW;
            if (clickThrough) want |= Native.WS_EX_TRANSPARENT;
            if (want != ex && styleFixes < 20)
            {
                if (!force) { styleFixes++; Log.W("浮层样式被改过，已补回 0x" + ex.ToString("X") + " → 0x" + want.ToString("X")); }
                Native.SetLong(overlayH, Native.GWL_EXSTYLE, want);
            }
        }

        void SetAlpha(double a)
        {
            alpha = Math.Max(0, Math.Min(1, a));
            grid.Opacity = alpha;
        }

        void RampAlpha(double to, double seconds, Action then)
        {
            alphaFrom = alpha; alphaTo = to; alphaT0 = DateTime.Now; alphaDur = Math.Max(0, seconds); alphaThen = then;
        }

        void RampVolume(double to, double seconds)
        {
            if (intro == null) return;
            volFrom = intro.Volume; volTo = to; volT0 = DateTime.Now; volDur = seconds;
        }

        void SetClickThrough(IntPtr h)
        {
            if (h == IntPtr.Zero) return;
            Native.SetLong(h, Native.GWL_EXSTYLE, Native.GetLong(h, Native.GWL_EXSTYLE) | Native.WS_EX_TRANSPARENT);
        }

        // 字幕：按播放时间算透明度，入场、出场各渐变 SubtitleFadeSeconds
        void UpdateSubtitle(double t)
        {
            if (sub == null || !config.ShowSubtitles) return;
            double fade = Math.Max(0.01, config.SubtitleFadeSeconds);
            int idx = -1;
            for (int i = 0; i < config.Subtitles.Count; i++)
                if (t >= config.Subtitles[i].Start && t <= config.Subtitles[i].End) { idx = i; break; }
            if (idx < 0)
            {
                if (currentSub >= 0 && sub.Opacity > 0) Log.W("字幕 " + (currentSub + 1) + " 消失，视频时间 " + t.ToString("0.000"));
                sub.Opacity = 0; currentSub = idx >= 0 ? idx : currentSub;
                if (currentSub == config.Subtitles.Count - 1 && phase != Phase.Intro) CloseTop(); // 最后一句放完，字幕层收掉
                return;
            }
            var s = config.Subtitles[idx];
            if (idx != currentSub) { currentSub = idx; sub.SetText(s.Text); Log.W("字幕 " + (idx + 1) + " 出现，视频时间 " + t.ToString("0.000")); }
            sub.Opacity = Math.Max(0, Math.Min(1, Math.Min((t - s.Start) / fade, (s.End - t) / fade)));
        }

        void HideSubtitles()
        {
            if (sub == null) return;
            sub.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(0.4)));
            sub = null;
        }

        /// 进入循环帧：透出 Claude 界面，点击穿透
        void Reveal(bool quick)
        {
            if (phase != Phase.Intro) return;
            phase = Phase.Revealed;
            clickThrough = true; EnsureOverlayStyle(true);
            SetClickThrough(topH);
            grid.Background = Brushes.Transparent;
            if (skipBtn != null) skipBtn.Visibility = Visibility.Collapsed; // 进入循环背景后不再显示「跳过」
            if (!config.LoopBackground)
            {
                Log.W("进入循环帧，循环背景已关，淡出");
                Finish(false); return;
            }
            Log.W("进入循环帧，透出界面");
            RampAlpha(config.ResidualAlpha, quick ? 0.9 : config.FadeDurationSeconds, null);
        }

        /// 点「跳过」：开着循环背景就淡出开屏、接到循环段，否则整个关掉
        public void SkipIntro()
        {
            if (phase != Phase.Intro) return;
            Log.W("点了跳过开屏");
            if (!config.LoopBackground) { Finish(true); return; }
            HideSubtitles();
            loopA.Play(); activeLoop = loopA;
            introGone = true;
            var fade = new DoubleAnimation(0, TimeSpan.FromSeconds(0.9)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
            var iv = intro;
            fade.Completed += delegate { iv.Visibility = Visibility.Collapsed; iv.Stop(); iv.Close(); };
            intro.BeginAnimation(UIElement.OpacityProperty, fade);
            RampVolume(0, 0.9); // 开屏音效随画面一起淡出
            Reveal(true);
            var closeTop = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            closeTop.Tick += delegate { closeTop.Stop(); CloseTop(); };
            closeTop.Start();
        }

        /// 菜单、设置窗口里拖滑块时实时生效
        public void Apply(Config c)
        {
            config = c;
            if (phase == Phase.Ending || phase == Phase.Done) return;
            if (phase == Phase.Revealed && alphaDur < 0) SetAlpha(c.ResidualAlpha);
            if (intro != null && !introGone) { volDur = -1; intro.Volume = c.IntroVolume; }
        }

        public void Finish(bool fast)
        {
            if (phase == Phase.Ending || phase == Phase.Done) return;
            phase = Phase.Ending;
            clickThrough = true; EnsureOverlayStyle(true);
            double d = fast ? 0.4 : 1.2;
            Log.W(fast ? "关闭（跳过）" : "淡出");
            if (skipBtn != null) skipBtn.Visibility = Visibility.Collapsed;
            HideSubtitles();
            RampVolume(0, d);
            RampAlpha(0, d, Teardown);
        }

        public void Teardown()
        {
            if (phase == Phase.Done) return;
            phase = Phase.Done;
            if (tick != null) tick.Stop();
            foreach (var m in new[] { intro, loopA, loopB })
                if (m != null) { try { m.Stop(); m.Close(); } catch { } }
            CloseTop();
            try { overlay.Close(); } catch { }
            Log.W("播放结束");
            if (onDone != null) onDone();
        }

        public bool Active { get { return phase != Phase.Done; } }

        /// 测试用：把 Claude 窗口那块屏幕截下来
        public void Snapshot(string path)
        {
            Native.RECT r = lastRect;
            int w = r.Right - r.Left, h = r.Bottom - r.Top;
            if (w <= 0 || h <= 0) return;
            using (var bmp = new SD.Bitmap(w, h))
            {
                using (var g = SD.Graphics.FromImage(bmp))
                {
                    IntPtr dst = g.GetHdc(), src = Native.GetDC(IntPtr.Zero);
                    Native.BitBlt(dst, 0, 0, w, h, src, r.Left, r.Top, 0x00CC0020 | 0x40000000); // SRCCOPY | CAPTUREBLT：分层窗口也拍进去
                    Native.ReleaseDC(IntPtr.Zero, src); g.ReleaseHdc(dst);
                }
                bmp.Save(path, SD.Imaging.ImageFormat.Png);
            }
            // 点击穿透检查：窗口中心这一点，鼠标点下去会落到哪个窗口
            IntPtr hit = Native.WindowFromPoint(new Native.POINT { X = r.Left + w / 2, Y = r.Top + h / 2 });
            IntPtr root = Native.GetAncestor(hit, 2 /*GA_ROOT*/);
            string who = root == overlayH ? "浮层" : root == Target.Hwnd ? "目标窗口" : root == topH ? "字幕层" : ("其他 " + Native.ClassOf(root));
            Log.W("截图 " + path + "  播放阶段=" + phase + " 透明度=" + alpha.ToString("0.00") + " 样式=0x" + Native.GetLong(overlayH, Native.GWL_EXSTYLE).ToString("X") + " 中心点击落到=" + who);
        }
    }

    // ───────────────────────── 设置窗口 ─────────────────────────

    public class SettingsWindow : Window
    {
        readonly App app;
        CheckBox introBox, loopBox;
        Slider alphaSlider, volSlider;
        TextBlock alphaVal, volVal;
        bool loading;

        public SettingsWindow(App app)
        {
            this.app = app;
            Title = "Claude 开机动画 · 设置";
            Width = 420; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Microsoft YaHei UI"); FontSize = 13;
            var sp = new StackPanel { Margin = new Thickness(22, 18, 22, 20) };

            sp.Children.Add(Header("打开 Claude 时"));
            introBox = new CheckBox { Content = "播放开屏动画", Margin = new Thickness(0, 6, 0, 4) };
            loopBox = new CheckBox { Content = "保留循环背景（半透明，点击穿透）", Margin = new Thickness(0, 4, 0, 4) };
            introBox.Click += delegate { Changed(); };
            loopBox.Click += delegate { Changed(); };
            sp.Children.Add(introBox); sp.Children.Add(loopBox);
            sp.Children.Add(new TextBlock { Text = "两个都不勾 = 原装 Claude", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 10) });

            alphaSlider = MakeSlider(sp, "背景不透明度", 0.8, out alphaVal);
            volSlider = MakeSlider(sp, "开屏音量（再乘系统音量，系统静音就没声）", 1, out volVal);

            sp.Children.Add(Header("现在，在 Claude 窗口上"));
            var row = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            row.Children.Add(Btn("播放一次开屏动画", delegate { app.PreviewIntro(); }));
            row.Children.Add(Btn("开启循环背景", delegate { app.PreviewLoop(); }));
            row.Children.Add(Btn("关掉", delegate { app.StopShow(); }));
            sp.Children.Add(row);
            var row2 = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
            row2.Children.Add(Btn("打开设置文件…", delegate { app.OpenConfig(); }));
            row2.Children.Add(Btn("卸载…", delegate { app.Uninstall(); }));
            sp.Children.Add(row2);
            Content = sp;
            Refresh();
        }

        static TextBlock Header(string t) { return new TextBlock { Text = t, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 6, 0, 0) }; }
        static Button Btn(string t, RoutedEventHandler h)
        {
            var b = new Button { Content = t, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 8, 6) };
            b.Click += h; return b;
        }
        Slider MakeSlider(StackPanel sp, string title, double max, out TextBlock val)
        {
            var g = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            var v = new TextBlock { Width = 44, TextAlignment = TextAlignment.Right };
            DockPanel.SetDock(v, Dock.Right);
            g.Children.Add(v);
            g.Children.Add(new TextBlock { Text = title });
            sp.Children.Add(g);
            var s = new Slider { Minimum = 0, Maximum = max, Margin = new Thickness(0, 4, 0, 8), IsMoveToPointEnabled = true };
            s.ValueChanged += delegate { v.Text = Math.Round(s.Value * 100) + "%"; if (!loading) Changed(); };
            sp.Children.Add(s);
            val = v;
            return s;
        }

        public void Refresh()
        {
            loading = true;
            var c = app.Config;
            introBox.IsChecked = c.PlayIntro; loopBox.IsChecked = c.LoopBackground;
            alphaSlider.Value = c.ResidualAlpha; volSlider.Value = c.IntroVolume;
            alphaVal.Text = Math.Round(c.ResidualAlpha * 100) + "%"; volVal.Text = Math.Round(c.IntroVolume * 100) + "%";
            loading = false;
        }

        void Changed()
        {
            if (loading) return;
            app.Update(delegate (Config c)
            {
                c.PlayIntro = introBox.IsChecked == true; c.LoopBackground = loopBox.IsChecked == true;
                c.ResidualAlpha = alphaSlider.Value; c.IntroVolume = volSlider.Value;
            }, this);
        }
    }

    // ───────────────────────── 主程序 ─────────────────────────

    public class App : Application
    {
        public static string AssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        public Config Config;
        Show show;
        WF.NotifyIcon tray;
        WF.ToolStripMenuItem introItem, loopItem, stopItem;
        WF.ToolStripLabel alphaLabel, volLabel;
        WF.TrackBar alphaBar, volBar;
        SettingsWindow settings;
        readonly HashSet<uint> seenPids = new HashSet<uint>();
        DispatcherTimer watch;
        readonly string[] args;
        bool testMode;
        bool menuLoading;

        public App(string[] args) { this.args = args; ShutdownMode = ShutdownMode.OnExplicitShutdown; }

        string Arg(string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        bool Has(string name) { return Array.IndexOf(args, name) >= 0; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Config = Config.Load();
            LoadFont();
            Finder.TestTitle = Arg("--target-title");
            testMode = Finder.TestTitle != null;
            Log.W("启动 " + string.Join(" ", args) + "  程序目录 " + AppDomain.CurrentDomain.BaseDirectory);

            if (Arg("--render-subtitle") != null) { RenderSubtitleTest(); return; }

            SetupTray();
            if (Has("--settings")) OpenSettings();

            // 开机时 Claude 已经开着：不补播
            var t0 = Finder.Find();
            if (t0.Valid && !testMode) seenPids.Add(t0.Pid);

            watch = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            watch.Tick += delegate { Watch(); };
            watch.Start();

            if (testMode) RunTest();
            else
            {
                var promote = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                promote.Tick += delegate { promote.Stop(); PromoteTrayIcon(); };
                promote.Start();
            }
        }

        void LoadFont()
        {
            try
            {
                if (File.Exists(Path.Combine(AssetsDir, "LibreBodoni-Italic.ttf")))
                    SubtitleElement.Font = new FontFamily(new Uri(AssetsDir.TrimEnd('\\') + "\\"), "./#Libre Bodoni");
            }
            catch (Exception ex) { Log.W("字体载入失败，用 Georgia：" + ex.Message); }
        }

        // 每 0.4 秒看一眼：Claude 有没有新开（新的进程号 = 新启动）
        void Watch()
        {
            var t = Finder.Find();
            if (!t.Valid || seenPids.Contains(t.Pid)) return;
            seenPids.Add(t.Pid);
            if (testMode) return;
            Config = Config.Load();
            Log.W("检测到 Claude 启动 pid=" + t.Pid + "，开屏=" + Config.PlayIntro + " 循环背景=" + Config.LoopBackground);
            if (!Config.PlayIntro && !Config.LoopBackground) return;
            Start(t, Config.PlayIntro);
        }

        void Start(Target t, bool withIntro)
        {
            if (show != null) show.Teardown();
            Show s = null;
            s = new Show(t, Config, withIntro, delegate { if (show == s) show = null; RefreshMenu(); });
            show = s;
            RefreshMenu();
        }

        bool StartOnCurrent(bool withIntro)
        {
            var t = Finder.Find();
            if (!t.Valid)
            {
                if (tray != null) tray.ShowBalloonTip(3000, "Claude 开机动画", "没找到 Claude 窗口，先把 Claude 打开吧。", WF.ToolTipIcon.Info);
                return false;
            }
            seenPids.Add(t.Pid);
            Start(t, withIntro);
            return true;
        }

        public void PreviewIntro() { StartOnCurrent(true); }
        public void PreviewLoop() { StartOnCurrent(false); }
        public void StopShow() { if (show != null) show.Finish(true); }
        public void OpenConfig() { Config.Save(); try { Process.Start("notepad.exe", "\"" + Config.FilePath + "\""); } catch { } }

        public void Update(Action<Config> change, object from)
        {
            change(Config);
            Config.Save();
            if (show != null) show.Apply(Config);
            if (from != settings && settings != null) settings.Refresh();
            if (from != tray) RefreshMenu();
        }

        public void OpenSettings()
        {
            Config = Config.Load();
            if (settings == null)
            {
                settings = new SettingsWindow(this);
                settings.Closed += delegate { settings = null; };
            }
            settings.Refresh();
            settings.Show();
            if (settings.WindowState == WindowState.Minimized) settings.WindowState = WindowState.Normal;
            settings.Activate();
        }

        public void Uninstall()
        {
            var r = MessageBox.Show("会删除这个小程序、开机自启、设置和日志。Claude 本身没有被改过，卸载后就是原装的样子。",
                "卸载 Claude 开机动画？", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (r != MessageBoxResult.OK) return;
            if (show != null) show.Teardown();
            string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uninstall.ps1");
            if (File.Exists(script))
            {
                Process.Start(new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\" -Quiet")
                { UseShellExecute = false, CreateNoWindow = true });
            }
            Quit();
        }

        void Quit()
        {
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            Shutdown();
        }

        // ── 托盘图标和菜单 ──
        static SD.Icon MakeIcon()
        {
            var bmp = new SD.Bitmap(32, 32);
            using (var g = SD.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias;
                Action<float, float, float> star = delegate (float cx, float cy, float r)
                {
                    float k = r * 0.28f;
                    var pts = new[] {
                        new SD.PointF(cx, cy - r), new SD.PointF(cx + k, cy - k), new SD.PointF(cx + r, cy), new SD.PointF(cx + k, cy + k),
                        new SD.PointF(cx, cy + r), new SD.PointF(cx - k, cy + k), new SD.PointF(cx - r, cy), new SD.PointF(cx - k, cy - k) };
                    using (var b = new SD.SolidBrush(SD.Color.FromArgb(255, 255, 196, 64))) g.FillPolygon(b, pts);
                };
                star(13, 18, 12); star(25, 7, 6); star(26, 25, 4);
            }
            return SD.Icon.FromHandle(bmp.GetHicon());
        }

        void SetupTray()
        {
            tray = new WF.NotifyIcon { Icon = MakeIcon(), Text = "Claude 开机动画", Visible = true };
            var m = new WF.ContextMenuStrip();
            m.Items.Add(new WF.ToolStripLabel("打开 Claude 时") { Enabled = false });
            introItem = new WF.ToolStripMenuItem("播放开屏动画");
            introItem.Click += delegate { Update(delegate (Config c) { c.PlayIntro = !c.PlayIntro; }, null); };
            loopItem = new WF.ToolStripMenuItem("保留循环背景");
            loopItem.Click += delegate { Update(delegate (Config c) { c.LoopBackground = !c.LoopBackground; }, null); };
            m.Items.Add(introItem); m.Items.Add(loopItem);
            m.Items.Add(new WF.ToolStripSeparator());

            alphaLabel = new WF.ToolStripLabel("背景不透明度");
            alphaBar = MakeBar(80);
            alphaBar.ValueChanged += delegate
            {
                alphaLabel.Text = "背景不透明度  " + alphaBar.Value + "%";
                if (!menuLoading) Update(delegate (Config c) { c.ResidualAlpha = alphaBar.Value / 100.0; }, tray);
            };
            volLabel = new WF.ToolStripLabel("开屏音量");
            volBar = MakeBar(100);
            volBar.ValueChanged += delegate
            {
                volLabel.Text = "开屏音量  " + volBar.Value + "%";
                if (!menuLoading) Update(delegate (Config c) { c.IntroVolume = volBar.Value / 100.0; }, tray);
            };
            m.Items.Add(alphaLabel); m.Items.Add(new WF.ToolStripControlHost(alphaBar));
            m.Items.Add(volLabel); m.Items.Add(new WF.ToolStripControlHost(volBar));
            m.Items.Add(new WF.ToolStripSeparator());

            m.Items.Add(new WF.ToolStripLabel("现在，在 Claude 窗口上") { Enabled = false });
            m.Items.Add("播放一次开屏动画", null, delegate { PreviewIntro(); });
            m.Items.Add("开启循环背景", null, delegate { PreviewLoop(); });
            stopItem = new WF.ToolStripMenuItem("关掉动画 / 背景");
            stopItem.Click += delegate { StopShow(); };
            m.Items.Add(stopItem);
            m.Items.Add(new WF.ToolStripSeparator());
            m.Items.Add("设置窗口…", null, delegate { OpenSettings(); });
            m.Items.Add("打开设置文件…", null, delegate { OpenConfig(); });
            m.Items.Add("卸载 Claude 开机动画…", null, delegate { Uninstall(); });
            m.Opening += delegate { Config = Config.Load(); RefreshMenu(); };
            tray.ContextMenuStrip = m;
            tray.MouseClick += delegate (object s, WF.MouseEventArgs ev) { if (ev.Button == WF.MouseButtons.Left) OpenSettings(); };
            RefreshMenu();
        }

        static WF.TrackBar MakeBar(int max)
        {
            return new WF.TrackBar { Minimum = 0, Maximum = max, TickStyle = WF.TickStyle.None, Width = 200, AutoSize = false, Height = 28, SmallChange = 1, LargeChange = 5 };
        }

        void RefreshMenu()
        {
            if (tray == null) return;
            menuLoading = true;
            introItem.Checked = Config.PlayIntro;
            loopItem.Checked = Config.LoopBackground;
            stopItem.Enabled = show != null;
            alphaBar.Value = Math.Max(0, Math.Min(80, (int)Math.Round(Config.ResidualAlpha * 100)));
            volBar.Value = Math.Max(0, Math.Min(100, (int)Math.Round(Config.IntroVolume * 100)));
            alphaLabel.Text = "背景不透明度  " + alphaBar.Value + "%";
            volLabel.Text = "开屏音量  " + volBar.Value + "%";
            menuLoading = false;
        }

        /// Windows 11 默认把新托盘图标收进「^」里。第一次运行时把自己的图标设为常显（用户以后手动藏起来就不再动它）
        void PromoteTrayIcon()
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                using (var root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
                {
                    if (root == null) return;
                    foreach (var name in root.GetSubKeyNames())
                    {
                        using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings\" + name, true))
                        {
                            if (k == null) continue;
                            var path = k.GetValue("ExecutablePath") as string;
                            if (path == null || !string.Equals(path, exe, StringComparison.OrdinalIgnoreCase)) continue;
                            if (k.GetValue("IsPromoted") == null) { k.SetValue("IsPromoted", 1, RegistryValueKind.DWord); Log.W("托盘图标设为常显"); }
                        }
                    }
                }
            }
            catch (Exception ex) { Log.W("托盘图标常显设置失败：" + ex.Message); }
        }

        // ── 测试 ──
        void RunTest()
        {
            string mode = Arg("--play") ?? "intro";
            double exitAfter = double.Parse(Arg("--exit-after") ?? "30", CultureInfo.InvariantCulture);
            string shotDir = Arg("--shot-dir");
            string shots = Arg("--shot-at");
            string skip = Arg("--test-skip");
            var startAt = DateTime.Now;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            bool started = false, skipped = false;
            var pending = new List<double>();
            if (shots != null) foreach (var s in shots.Split(',')) pending.Add(double.Parse(s, CultureInfo.InvariantCulture));
            DateTime showStart = DateTime.Now;
            timer.Tick += delegate
            {
                if (!started)
                {
                    if (StartOnCurrent(mode != "loop")) { started = true; showStart = DateTime.Now; }
                    else if ((DateTime.Now - startAt).TotalSeconds > 15) { Log.W("测试：找不到目标窗口"); Quit(); }
                    return;
                }
                double el = (DateTime.Now - showStart).TotalSeconds;
                if (skip != null && !skipped && el >= double.Parse(skip, CultureInfo.InvariantCulture) && show != null) { skipped = true; show.SkipIntro(); }
                for (int i = pending.Count - 1; i >= 0; i--)
                    if (el >= pending[i] && show != null && shotDir != null)
                    {
                        double at = pending[i]; pending.RemoveAt(i);
                        try
                        {
                            Directory.CreateDirectory(shotDir);
                            show.Snapshot(Path.Combine(shotDir, "shot_" + at.ToString("00.0", CultureInfo.InvariantCulture) + ".png"));
                        }
                        catch (Exception ex) { Log.W("截图失败：" + ex.Message); }
                    }
                if (el >= exitAfter) { timer.Stop(); if (show != null) show.Teardown(); Log.W("测试结束"); Quit(); }
            };
            timer.Start();
        }

        /// 离屏渲染字幕到图片（不需要显示器），测字体、大小、描边
        void RenderSubtitleTest()
        {
            try
            {
                string bg = Arg("--render-subtitle");
                string outPath = args[Array.IndexOf(args, "--render-subtitle") + 2];
                int idx = args.Length > Array.IndexOf(args, "--render-subtitle") + 3 ? int.Parse(args[Array.IndexOf(args, "--render-subtitle") + 3]) : 1;
                var img = new BitmapImage(new Uri(Path.GetFullPath(bg)));
                int W = img.PixelWidth, H = img.PixelHeight;
                var g = new Grid { Width = W, Height = H };
                g.Children.Add(new Image { Source = img, Stretch = Stretch.Fill });
                var s = new SubtitleElement { WidthRatio = Config.SubtitleWidthRatio, Opacity = 1 };
                s.RefText = Config.Subtitles[0].Text;
                s.SetText(Config.Subtitles[Math.Max(0, Math.Min(Config.Subtitles.Count - 1, idx - 1))].Text);
                g.Children.Add(s);
                g.Measure(new Size(W, H)); g.Arrange(new Rect(0, 0, W, H)); g.UpdateLayout();
                var rtb = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(g);
                var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
                using (var f = File.Create(outPath)) enc.Save(f);
                Log.W("字幕渲染到 " + outPath + "  字体=" + SubtitleElement.Font.Source);
            }
            catch (Exception ex) { Log.W("字幕渲染失败：" + ex); }
            Shutdown();
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Native.SetProcessDPIAware();
            bool test = Array.IndexOf(args, "--target-title") >= 0 || Array.IndexOf(args, "--render-subtitle") >= 0;
            Mutex mutex = null;
            EventWaitHandle showEvent = null;
            if (!test)
            {
                bool created;
                mutex = new Mutex(true, @"Local\local.ai-boot-animations.claude-boot-animation", out created);
                showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\local.ai-boot-animations.claude-boot-animation.show");
                if (!created)
                {
                    // 已经在运行：再打开一次 = 弹出设置窗口
                    showEvent.Set();
                    return 0;
                }
            }
            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e) { Log.W("崩溃：" + e.ExceptionObject); };
            var app = new App(args);
            app.DispatcherUnhandledException += delegate (object s, DispatcherUnhandledExceptionEventArgs e) { Log.W("出错：" + e.Exception); e.Handled = true; };
            if (showEvent != null)
            {
                var ev = showEvent;
                var th = new Thread(delegate ()
                {
                    while (true) { ev.WaitOne(); app.Dispatcher.BeginInvoke(new Action(delegate { app.OpenSettings(); })); }
                }) { IsBackground = true };
                th.Start();
            }
            int code = app.Run();
            GC.KeepAlive(mutex);
            return code;
        }
    }
}
