using System;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    /// <summary>
    /// Watches mouse hook events and manages ClickLock state transitions.
    /// Drives the overlay with charging progress and locked/idle signals.
    /// </summary>
    public class ClickLockStateMachine : IDisposable
    {
        private readonly MouseHook _hook;
        private readonly OverlayWindow _overlay;
        private readonly Settings _settings;

        private Timer _holdTimer;       // fires at intervals to update progress
        private DateTime _holdStart;
        private bool _isLocked = false;
        private bool _isHolding = false;

        private System.Media.SoundPlayer _lockPlayer;
        private System.Media.SoundPlayer _unlockPlayer;

        // Charging starts at 50% of the ClickLock hold time
        private const float CHARGE_START_FRACTION = 0.5f;

        public event EventHandler<bool> LockedStateChanged; // bool = isLocked

        public ClickLockStateMachine(MouseHook hook, OverlayWindow overlay, Settings settings)
        {
            _hook = hook;
            _overlay = overlay;
            _settings = settings;

            _holdTimer = new Timer { Interval = 16 }; // ~60fps polling
            _holdTimer.Tick += OnHoldTick;

            _hook.LeftButtonDown += OnLeftDown;
            _hook.LeftButtonUp += OnLeftUp;
            _hook.ReleaseTrigger += OnReleaseTrigger;

            PreloadSounds();
        }

        private void OnLeftDown(object sender, EventArgs e)
        {
            if (_isLocked)
            {
                // A left click when already locked releases the lock
                Unlock();
                return;
            }

            _holdStart = DateTime.UtcNow;
            _isHolding = true;
            _holdTimer.Start();
        }

        private void OnLeftUp(object sender, EventArgs e)
        {
            if (!_isHolding) return;

            int clickLockMs = ClickLockHelper.GetClickLockTimeMs();
            double heldMs = (DateTime.UtcNow - _holdStart).TotalMilliseconds;

            if (heldMs >= clickLockMs)
            {
                // Held long enough — ClickLock has engaged
                Lock();
            }
            else
            {
                // Normal click, cancel
                CancelHold();
            }
        }

        private void OnReleaseTrigger(object sender, EventArgs e)
        {
            if (_isLocked)
                Unlock();
        }

        private void OnHoldTick(object sender, EventArgs e)
        {
            if (!_isHolding) return;

            int clickLockMs = ClickLockHelper.GetClickLockTimeMs();
            double heldMs = (DateTime.UtcNow - _holdStart).TotalMilliseconds;

            if (heldMs >= clickLockMs)
            {
                // We've passed the threshold; Windows will engage lock on button up
                // Show arc as complete but not yet "locked" (button still held)
                _overlay.SetCharging(1.0f);
                return;
            }

            double chargeStartMs = clickLockMs * CHARGE_START_FRACTION;
            if (heldMs < chargeStartMs)
            {
                // Too early to show anything
                _overlay.SetIdle();
                return;
            }

            // Map chargeStartMs..clickLockMs → 0..1
            float progress = (float)((heldMs - chargeStartMs) / (clickLockMs - chargeStartMs));
            _overlay.SetCharging(progress);
        }

        private void Lock()
        {
            _isHolding = false;
            _isLocked = true;
            _holdTimer.Stop();
            _overlay.SetLocked();

            if (_settings.SoundEnabled)
                PlaySound(true);

            LockedStateChanged?.Invoke(this, true);
        }

        private void Unlock()
        {
            _isHolding = false;
            _isLocked = false;
            _holdTimer.Stop();
            _overlay.SetIdle();

            if (_settings.SoundEnabled)
                PlaySound(false);

            LockedStateChanged?.Invoke(this, false);
        }

        private void CancelHold()
        {
            _isHolding = false;
            _holdTimer.Stop();
            _overlay.SetIdle();
        }

        private void PreloadSounds()
        {
            try
            {
                string wav = @"C:\Windows\Media\Windows Navigation Start.wav";
                _lockPlayer = new System.Media.SoundPlayer(wav);
                _lockPlayer.Load();
                _unlockPlayer = new System.Media.SoundPlayer(wav);
                _unlockPlayer.Load();
            }
            catch { /* best effort */ }
        }

        private void PlaySound(bool locking)
        {
            try { (locking ? _lockPlayer : _unlockPlayer)?.Play(); }
            catch { /* best effort */ }
        }

        public void Dispose()
        {
            _holdTimer?.Dispose();
            _lockPlayer?.Dispose();
            _unlockPlayer?.Dispose();
            _hook.LeftButtonDown -= OnLeftDown;
            _hook.LeftButtonUp -= OnLeftUp;
            _hook.ReleaseTrigger -= OnReleaseTrigger;
        }
    }
}
