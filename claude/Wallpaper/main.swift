import AppKit
import AVFoundation
import ServiceManagement

// 每个屏幕拥有自己的播放器和图层，以便独立调整尺寸、接入和移除。
final class WallpaperScreen {
    let window: NSWindow
    let player = AVQueuePlayer()
    private let looper: AVPlayerLooper

    init(screen: NSScreen, video: URL) {
        player.isMuted = true
        looper = AVPlayerLooper(player: player, templateItem: AVPlayerItem(url: video))
        window = NSWindow(contentRect: screen.frame, styleMask: .borderless,
                          backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.desktopWindow)) + 1)
        window.collectionBehavior = [.canJoinAllSpaces, .stationary, .ignoresCycle]
        window.ignoresMouseEvents = true
        window.hasShadow = false
        window.backgroundColor = .black
        window.canHide = false
        window.animationBehavior = .none

        let view = NSView(frame: NSRect(origin: .zero, size: screen.frame.size))
        let layer = AVPlayerLayer(player: player)
        layer.videoGravity = .resizeAspectFill
        view.layer = layer
        view.wantsLayer = true
        window.contentView = view
        window.orderFrontRegardless()
    }

    func close() {
        player.pause()
        looper.disableLooping()
        window.close()
    }
}

final class WallpaperApp: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private var screens: [CGDirectDisplayID: WallpaperScreen] = [:]
    private var video: URL!
    private var statusItem: NSStatusItem!
    private let playbackItem = NSMenuItem()
    private let loginItem = NSMenuItem()
    private var pausedByUser = false
    private var displaysAsleep = false
    private var sessionInactive = false
    private var screenLocked = false
    private var shouldPlay: Bool { !pausedByUser && !displaysAsleep && !sessionInactive && !screenLocked }

    func applicationDidFinishLaunching(_ notification: Notification) {
        guard let url = Bundle.main.url(forResource: "loop", withExtension: "mp4") else {
            let alert = NSAlert()
            alert.messageText = "找不到壁纸视频，请重新构建 Claude 壁纸。"
            alert.runModal()
            NSApp.terminate(nil)
            return
        }
        video = url
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        statusItem.button?.image = NSImage(systemSymbolName: "moon.stars.fill",
                                           accessibilityDescription: "Claude 壁纸")
        statusItem.button?.toolTip = "Claude 壁纸 · 月下剑光"
        let menu = NSMenu()
        let title = menu.addItem(withTitle: "Claude 壁纸 · 月下剑光", action: nil, keyEquivalent: "")
        title.isEnabled = false
        menu.addItem(.separator())
        playbackItem.target = self
        playbackItem.action = #selector(togglePlayback)
        menu.addItem(playbackItem)
        loginItem.target = self
        loginItem.action = #selector(toggleLogin)
        menu.addItem(loginItem)
        menu.delegate = self
        refreshLoginItem()
        menu.addItem(.separator())
        let quit = menu.addItem(withTitle: "退出壁纸", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        quit.target = NSApp
        statusItem.menu = menu

        NotificationCenter.default.addObserver(self, selector: #selector(updateScreens),
            name: NSApplication.didChangeScreenParametersNotification, object: nil)
        let workspace = NSWorkspace.shared.notificationCenter
        for event in [NSWorkspace.screensDidSleepNotification, NSWorkspace.screensDidWakeNotification,
                      NSWorkspace.sessionDidResignActiveNotification, NSWorkspace.sessionDidBecomeActiveNotification] {
            workspace.addObserver(self, selector: #selector(sessionChanged(_:)), name: event, object: nil)
        }
        for event in ["com.apple.screenIsLocked", "com.apple.screenIsUnlocked"] {
            DistributedNotificationCenter.default().addObserver(self, selector: #selector(sessionChanged(_:)),
                                                                name: .init(event), object: nil)
        }
        updateScreens()
    }

    @objc private func updateScreens() {
        var present = Set<CGDirectDisplayID>()
        for screen in NSScreen.screens {
            guard let id = screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? CGDirectDisplayID else { continue }
            present.insert(id)
            if let existing = screens[id] {
                existing.window.setFrame(screen.frame, display: true)
            } else {
                screens[id] = WallpaperScreen(screen: screen, video: video)
            }
        }
        for id in Set(screens.keys).subtracting(present) {
            screens.removeValue(forKey: id)?.close()
        }
        updatePlayback()
    }

    @objc private func sessionChanged(_ notification: Notification) {
        switch notification.name {
        case NSWorkspace.screensDidSleepNotification: displaysAsleep = true
        case NSWorkspace.screensDidWakeNotification: displaysAsleep = false
        case NSWorkspace.sessionDidResignActiveNotification: sessionInactive = true
        case NSWorkspace.sessionDidBecomeActiveNotification: sessionInactive = false
        case .init("com.apple.screenIsLocked"): screenLocked = true
        case .init("com.apple.screenIsUnlocked"): screenLocked = false
        default: return
        }
        updatePlayback()
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        refreshLoginItem()
    }

    private func refreshLoginItem() {
        let status = SMAppService.mainApp.status
        loginItem.state = status == .enabled ? .on : .off
        loginItem.title = status == .requiresApproval ? "前往系统设置允许登录启动…" : "登录时自动播放"
    }

    @objc private func toggleLogin() {
        let service = SMAppService.mainApp
        do {
            switch service.status {
            case .enabled:
                try service.unregister()
            case .requiresApproval:
                SMAppService.openSystemSettingsLoginItems()
            default:
                try service.register()
                if service.status == .requiresApproval {
                    SMAppService.openSystemSettingsLoginItems()
                }
            }
        } catch {
            let alert = NSAlert(error: error)
            alert.messageText = "无法更改登录启动设置"
            alert.informativeText = "请将 Claude 壁纸放入「应用程序」并从那里打开，然后重试。\n" + error.localizedDescription
            alert.runModal()
        }
        refreshLoginItem()
    }

    @objc private func togglePlayback() {
        pausedByUser.toggle()
        updatePlayback()
    }

    private func updatePlayback() {
        playbackItem.title = pausedByUser ? "继续播放" : "暂停播放"
        for screen in screens.values {
            if shouldPlay { screen.player.play() } else { screen.player.pause() }
        }
    }
}

let app = NSApplication.shared
let delegate = WallpaperApp()
app.delegate = delegate
app.setActivationPolicy(.accessory)
app.run()
