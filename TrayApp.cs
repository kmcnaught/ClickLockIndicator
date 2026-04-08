using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClickLockIndicator
{
    public class TrayApp : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _menu;
        private OverlayWindow _overlay;
        private MouseHook _hook;
        private ClickLockStateMachine _stateMachine;
        private Settings _settings;

        private ToolStripMenuItem _menuStartWithWindows;
        private ToolStripMenuItem _menuSound;
        private ToolStripMenuItem _menuOverlayNone;
        private ToolStripMenuItem _menuOverlayRing;
        private ToolStripMenuItem _menuOverlayArc;

        private bool _isLocked = false;

        private const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string REGISTRY_VALUE = "ClickLockIndicator";

        public TrayApp()
        {
            _settings = Settings.Load();

            _overlay = new OverlayWindow();
            _overlay.SetStyle(_settings.OverlayStyle);

            _hook = new MouseHook();
            _hook.Install();

            _stateMachine = new ClickLockStateMachine(_hook, _overlay, _settings);
            _stateMachine.LockedStateChanged += OnLockedStateChanged;

            BuildMenu();
            BuildTrayIcon();
            UpdateTrayIcon();
        }

        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();

            // Start with Windows
            _menuStartWithWindows = new ToolStripMenuItem("Start with Windows");
            _menuStartWithWindows.Checked = IsRegisteredStartup();
            _menuStartWithWindows.Click += (s, e) => ToggleStartWithWindows();

            // Sound
            _menuSound = new ToolStripMenuItem("Sound");
            _menuSound.Checked = _settings.SoundEnabled;
            _menuSound.Click += (s, e) => ToggleSound();

            // Overlay submenu
            var overlayMenu = new ToolStripMenuItem("Overlay");
            _menuOverlayNone = new ToolStripMenuItem("None");
            _menuOverlayRing = new ToolStripMenuItem("Ring");
            _menuOverlayArc  = new ToolStripMenuItem("Arc (charging)");

            _menuOverlayNone.Click += (s, e) => SetOverlay(OverlayStyle.None);
            _menuOverlayRing.Click += (s, e) => SetOverlay(OverlayStyle.Ring);
            _menuOverlayArc.Click  += (s, e) => SetOverlay(OverlayStyle.Arc);

            overlayMenu.DropDownItems.Add(_menuOverlayNone);
            overlayMenu.DropDownItems.Add(_menuOverlayRing);
            overlayMenu.DropDownItems.Add(_menuOverlayArc);

            UpdateOverlayMenuChecks();

            _menu.Items.Add(_menuStartWithWindows);
            _menu.Items.Add(_menuSound);
            _menu.Items.Add(overlayMenu);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Quit", null, (s, e) => ExitApp());
        }

        private void BuildTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                ContextMenuStrip = _menu,
                Visible = true,
                Text = "ClickLock Indicator"
            };
        }

        private void OnLockedStateChanged(object sender, bool isLocked)
        {
            _isLocked = isLocked;
            UpdateTrayIcon();
        }

        private void UpdateTrayIcon()
        {
            _trayIcon.Icon?.Dispose();
            _trayIcon.Icon = DrawTrayIcon(_isLocked);
            _trayIcon.Text = _isLocked ? "ClickLock Indicator — LOCKED" : "ClickLock Indicator — Idle";
        }

        /// <summary>Draws a 16x16 tray icon: grey circle (idle) or filled blue circle (locked).</summary>
        private Icon DrawTrayIcon(bool locked)
        {
            int size = 16;
            using (var bmp = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                if (locked)
                {
                    // Filled blue circle with white centre dot
                    using (var brush = new SolidBrush(Color.FromArgb(80, 200, 255)))
                        g.FillEllipse(brush, 1, 1, 13, 13);
                    using (var pen = new Pen(Color.FromArgb(40, 140, 200), 1.5f))
                        g.DrawEllipse(pen, 1, 1, 13, 13);
                    using (var brush = new SolidBrush(Color.White))
                        g.FillEllipse(brush, 5, 5, 5, 5);
                }
                else
                {
                    // Outline circle, grey
                    using (var pen = new Pen(Color.FromArgb(160, 160, 160), 1.5f))
                        g.DrawEllipse(pen, 2, 2, 11, 11);
                    using (var brush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                        g.FillEllipse(brush, 5, 5, 5, 5);
                }

                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        // ── Settings actions ──────────────────────────────────────────────

        private void ToggleSound()
        {
            _settings.SoundEnabled = !_settings.SoundEnabled;
            _menuSound.Checked = _settings.SoundEnabled;
            _settings.Save();
        }

        private void SetOverlay(OverlayStyle style)
        {
            _settings.OverlayStyle = style;
            _overlay.SetStyle(style);
            UpdateOverlayMenuChecks();
            _settings.Save();
        }

        private void UpdateOverlayMenuChecks()
        {
            _menuOverlayNone.Checked = _settings.OverlayStyle == OverlayStyle.None;
            _menuOverlayRing.Checked = _settings.OverlayStyle == OverlayStyle.Ring;
            _menuOverlayArc.Checked  = _settings.OverlayStyle == OverlayStyle.Arc;
        }

        private void ToggleStartWithWindows()
        {
            if (IsRegisteredStartup())
            {
                UnregisterStartup();
                _menuStartWithWindows.Checked = false;
                _settings.StartWithWindows = false;
            }
            else
            {
                RegisterStartup();
                _menuStartWithWindows.Checked = true;
                _settings.StartWithWindows = true;
            }
            _settings.Save();
        }

        // ── Registry startup ──────────────────────────────────────────────

        private bool IsRegisteredStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false))
                return key?.GetValue(REGISTRY_VALUE) != null;
        }

        private void RegisterStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                key?.SetValue(REGISTRY_VALUE,
                    $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}\"");
        }

        private void UnregisterStartup()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                key?.DeleteValue(REGISTRY_VALUE, throwOnMissingValue: false);
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private void ExitApp()
        {
            _stateMachine?.Dispose();
            _hook?.Uninstall();
            _overlay?.Dispose();
            _trayIcon.Visible = false;
            _trayIcon?.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ExitApp();
            base.Dispose(disposing);
        }
    }
}
