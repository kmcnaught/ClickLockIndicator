using System;
using System.Threading;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "ClickLockIndicator_SingleInstance", out createdNew))
            {
                if (!createdNew)
                    return; // another instance is already running

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // At Windows startup, user profile settings load asynchronously and
                // SPI_GETMOUSECLICKLOCK can return 0 for several seconds even when ClickLock
                // is enabled. Retry for up to ~10 s before showing an error.
                bool clickLockEnabled = false;
                for (int i = 0; i < 20; i++)
                {
                    if (ClickLockHelper.IsClickLockEnabled()) { clickLockEnabled = true; break; }
                    System.Threading.Thread.Sleep(500);
                }

                if (!clickLockEnabled)
                {
                    MessageBox.Show(
                        "ClickLock is not enabled in Windows Mouse settings.\n\nEnable it via: Control Panel → Mouse → Buttons tab → Turn on ClickLock.",
                        "ClickLock Indicator",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.Run(new TrayApp());
            }
        }
    }
}
