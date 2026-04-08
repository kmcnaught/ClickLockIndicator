using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    public class OverlayWindow : Form
    {
        // Win32 constants for layered/transparent window
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int OVERLAY_SIZE = 48;
        private const int OFFSET_X = -OVERLAY_SIZE / 2;  // centre on cursor hotspot
        private const int OFFSET_Y = -OVERLAY_SIZE / 2;

        // 0.0 = no arc shown, 0.5–1.0 = charging, 1.0 = locked
        private float _progress = 0f;
        private bool _locked = false;
        private OverlayStyle _style = OverlayStyle.Arc;

        private Timer _animTimer;
        private float _animProgress = 0f; // smoothed display value

        public OverlayWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(OVERLAY_SIZE, OVERLAY_SIZE);
            BackColor = Color.Magenta; // key colour for transparency
            TransparencyKey = Color.Magenta;

            _animTimer = new Timer { Interval = 16 }; // ~60fps
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }

        public void SetStyle(OverlayStyle style)
        {
            _style = style;
            Invalidate();
        }

        /// <summary>Call with progress 0.0–1.0 as ClickLock charges (from 50% of hold time to 100%).</summary>
        public void SetCharging(float progress)
        {
            _progress = Math.Max(0f, Math.Min(1f, progress));
            _locked = false;
        }

        public void SetLocked()
        {
            _progress = 1f;
            _locked = true;
        }

        public void SetIdle()
        {
            _progress = 0f;
            _animProgress = 0f;
            _locked = false;
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            // Smooth the display value toward target
            float target = _progress;
            float delta = target - _animProgress;
            _animProgress += delta * 0.25f;

            if (Math.Abs(_animProgress - target) < 0.005f)
                _animProgress = target;

            // Track cursor position
            var cursor = Cursor.Position;
            Left = cursor.X + OFFSET_X;
            Top = cursor.Y + OFFSET_Y;

            // Only show if there's something to show
            bool shouldShow = _style != OverlayStyle.None && _animProgress > 0.01f;
            if (shouldShow && !Visible)
                Show();
            else if (!shouldShow && Visible)
                Hide();

            if (Visible)
                Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Magenta); // transparent key

            if (_style == OverlayStyle.None || _animProgress < 0.01f)
                return;

            float p = _animProgress; // 0..1
            int margin = 4;
            var rect = new RectangleF(margin, margin, OVERLAY_SIZE - margin * 2, OVERLAY_SIZE - margin * 2);

            if (_style == OverlayStyle.Ring)
            {
                DrawRing(g, rect, p);
            }
            else if (_style == OverlayStyle.Arc)
            {
                DrawArc(g, rect, p);
            }
        }

        private void DrawRing(Graphics g, RectangleF rect, float p)
        {
            // Ring fades in as ClickLock charges, glows when locked
            int alpha = (int)(p * 220);
            alpha = Math.Max(0, Math.Min(255, alpha));

            Color ringColor = _locked
                ? Color.FromArgb(alpha, 80, 200, 255)   // blue when locked
                : Color.FromArgb(alpha, 255, 180, 40);  // amber while charging

            float thickness = _locked ? 3.0f : 2.0f;
            using (var pen = new Pen(ringColor, thickness))
                g.DrawEllipse(pen, rect);

            // Inner glow dot when locked
            if (_locked)
            {
                float cx = rect.Left + rect.Width / 2f;
                float cy = rect.Top + rect.Height / 2f;
                float dotR = 3f;
                using (var brush = new SolidBrush(Color.FromArgb(180, 80, 200, 255)))
                    g.FillEllipse(brush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
            }
        }

        private void DrawArc(Graphics g, RectangleF rect, float p)
        {
            // Arc sweeps clockwise from top, proportional to progress
            float sweepAngle = p * 360f;
            Color arcColor = _locked
                ? Color.FromArgb(230, 80, 200, 255)   // blue when locked
                : Color.FromArgb(230, 255, 210, 0);   // amber while charging
            using (var pen = new Pen(arcColor, 5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, rect, -90f, sweepAngle);
            }

            // Dot at arc tip
            if (sweepAngle > 5f)
            {
                double tipAngle = (-90f + sweepAngle) * Math.PI / 180.0;
                float cx = rect.Left + rect.Width / 2f;
                float cy = rect.Top + rect.Height / 2f;
                float r = rect.Width / 2f;
                float tx = cx + (float)(r * Math.Cos(tipAngle));
                float ty = cy + (float)(r * Math.Sin(tipAngle));
                float dotR = 3.5f;
                using (var brush = new SolidBrush(arcColor))
                    g.FillEllipse(brush, tx - dotR, ty - dotR, dotR * 2, dotR * 2);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _animTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
