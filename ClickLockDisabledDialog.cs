using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    public class ClickLockDisabledDialog : Form
    {
        public ClickLockDisabledDialog()
        {
            Font = SystemFonts.MessageBoxFont;
            Text = "ClickLock Indicator";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 130);

            int y = 16;

            Controls.Add(new Label
            {
                Text = "ClickLock is not enabled. This app requires it to work.",
                Location = new Point(16, y),
                Size = new Size(348, 18),
                AutoSize = false,
            });

            y += 26;

            Controls.Add(new Label
            {
                Text = "Enable it at:",
                Location = new Point(16, y),
                AutoSize = true,
            });

            y += 20;

            var lnk = new LinkLabel
            {
                Text = "Settings \u2192 Accessibility \u2192 Mouse \u2192 Click lock",
                Location = new Point(16, y),
                AutoSize = true,
            };
            lnk.LinkClicked += (s, e) =>
                System.Diagnostics.Process.Start("ms-settings:easeofaccess-mouse");
            Controls.Add(lnk);

            y += 22;

            Controls.Add(new Label
            {
                Text = "Or search for \u201cLock mouse button on long click\u201d in Settings.",
                Location = new Point(16, y),
                Size = new Size(348, 18),
                AutoSize = false,
                ForeColor = SystemColors.GrayText,
            });

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(80, 26),
                DialogResult = DialogResult.OK,
            };
            btnClose.Location = new Point(ClientSize.Width - btnClose.Width - 12, ClientSize.Height - btnClose.Height - 12);
            Controls.Add(btnClose);
            AcceptButton = btnClose;
        }
    }
}
