using System;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check ClickLock is enabled
            if (!ClickLockHelper.IsClickLockEnabled())
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
