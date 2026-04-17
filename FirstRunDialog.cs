using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClickLockIndicator
{
    public class FirstRunDialog : Form
    {
        private Bitmap _idleBmp;
        private Bitmap _lockedBmp;
        private CheckBox _chkStartWithWindows;
        private ComboBox _cmbOverlay;
        private CheckBox _chkSound;
        private Button _btnOk;

        private const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string REGISTRY_VALUE = "ClickLockIndicator";

        public FirstRunDialog()
        {
            _idleBmp   = TrayApp.DrawTrayIconBitmap(false, 16);
            _lockedBmp = TrayApp.DrawTrayIconBitmap(true,  16);

            Font = SystemFonts.MessageBoxFont;
            Text = "ClickLock Indicator";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(400, 358);

            BuildLayout();

            _btnOk = new Button
            {
                Text = "OK",
                Size = new Size(80, 26),
            };
            _btnOk.Location = new Point(ClientSize.Width - _btnOk.Width - 12, ClientSize.Height - _btnOk.Height - 12);
            _btnOk.Click += OnOkClick;
            Controls.Add(_btnOk);
            AcceptButton = _btnOk;
        }

        private void BuildLayout()
        {
            int y = 16;

            // App description (mixed formatting — plain label can't do italic inline)
            var rtDesc = new RichTextBox
            {
                Location = new Point(14, y),
                Size = new Size(370, 40),
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Control,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false,
                Font = SystemFonts.MessageBoxFont,
            };
            rtDesc.AppendText("This tool shows a visual overlay when the Windows ");
            rtDesc.SelectionFont = new Font(SystemFonts.MessageBoxFont, FontStyle.Italic);
            rtDesc.AppendText("Lock mouse button on long click");
            rtDesc.SelectionFont = SystemFonts.MessageBoxFont;
            rtDesc.AppendText(" or \u201cClickLock\u201d feature locks your mouse button.");
            Controls.Add(rtDesc);

            y += 38;

            var lnkSettings = new LinkLabel
            {
                Text = "Settings \u2192 Accessibility \u2192 Mouse \u2192 Click lock",
                Location = new Point(16, y),
                AutoSize = true,
            };
            lnkSettings.LinkClicked += (s, e) =>
                System.Diagnostics.Process.Start("ms-settings:easeofaccess-mouse");
            Controls.Add(lnkSettings);

            y += 22;

            // "This app runs in the system tray..."
            Controls.Add(new Label
            {
                Text = "This app runs in the system tray. Right-click the icon to edit these options later.",
                Location = new Point(16, y),
                Size = new Size(368, 32),
                AutoSize = false,
            });

            y += 36;

            // Icon appearance row: label | idle icon + "Idle" | locked icon + "Locked"
            Controls.Add(new Label { Text = "Icon appearance:", Location = new Point(16, y + 2), AutoSize = true });

            int iconX = 120;
            Controls.Add(new PictureBox
            {
                Image = _idleBmp,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(iconX, y + 2),
            });
            Controls.Add(new Label { Text = "Idle", Location = new Point(iconX + 20, y + 2), AutoSize = true });

            iconX += 60;
            Controls.Add(new PictureBox
            {
                Image = _lockedBmp,
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(iconX, y + 2),
            });
            Controls.Add(new Label { Text = "Locked", Location = new Point(iconX + 20, y + 2), AutoSize = true });

            y += 32;

            // ── Options heading ───────────────────────────────────────────
            Controls.Add(new Label
            {
                Text = "OPTIONS",
                Location = new Point(16, y),
                AutoSize = true,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                ForeColor = SystemColors.GrayText,
            });

            y += 18;

            Controls.Add(new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(16, y),
                Size = new Size(368, 2),
            });

            y += 10;

            // Start with Windows
            _chkStartWithWindows = new CheckBox
            {
                Text = "Start with Windows  —  automatically launch at startup",
                Location = new Point(16, y),
                AutoSize = true,
            };
            Controls.Add(_chkStartWithWindows);

            y += 28;

            // Sound
            _chkSound = new CheckBox
            {
                Text = "Sound  —  plays a click when ClickLock engages",
                Location = new Point(16, y),
                AutoSize = true,
            };
            Controls.Add(_chkSound);

            y += 28;

            // Overlay
            Controls.Add(new Label { Text = "Overlay:", Location = new Point(16, y + 3), AutoSize = true });
            _cmbOverlay = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(80, y),
                Width = 100,
            };
            _cmbOverlay.Items.AddRange(new object[] { "Arc", "Ring", "None" });
            _cmbOverlay.SelectedIndex = 0;
            Controls.Add(_cmbOverlay);
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            OverlayStyle overlay;
            switch (_cmbOverlay.SelectedIndex)
            {
                case 1:  overlay = OverlayStyle.Ring; break;
                case 2:  overlay = OverlayStyle.None; break;
                default: overlay = OverlayStyle.Arc;  break;
            }

            var settings = new Settings
            {
                StartWithWindows = _chkStartWithWindows.Checked,
                OverlayStyle     = overlay,
                SoundEnabled     = _chkSound.Checked,
            };
            settings.Save();

            if (_chkStartWithWindows.Checked)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                    key?.SetValue(REGISTRY_VALUE,
                        $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}\"");
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idleBmp?.Dispose();
                _lockedBmp?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
