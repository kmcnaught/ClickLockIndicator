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

                if (!ClickLockHelper.IsClickLockEnabled())
                {
                    using (var dlg = new ClickLockDisabledDialog())
                        dlg.ShowDialog();
                }

                if (!System.IO.File.Exists(Settings.SettingsPath))
                {
                    using (var dlg = new FirstRunDialog())
                        dlg.ShowDialog();
                }

                Application.Run(new TrayApp());
            }
        }
    }
}
