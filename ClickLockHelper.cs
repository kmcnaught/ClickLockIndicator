using System;
using System.Runtime.InteropServices;

namespace ClickLockIndicator
{
    public static class ClickLockHelper
    {
        private const uint SPI_GETMOUSECLICKLOCK = 0x101E;
        private const uint SPI_GETMOUSECLICKLOCKTIME = 0x2008;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        public static bool IsClickLockEnabled()
        {
            uint enabled = 0;
            SystemParametersInfo(SPI_GETMOUSECLICKLOCK, 0, ref enabled, 0);
            return enabled != 0;
        }

        /// <summary>Returns ClickLock hold time in milliseconds.</summary>
        public static int GetClickLockTimeMs()
        {
            uint ms = 0;
            SystemParametersInfo(SPI_GETMOUSECLICKLOCKTIME, 0, ref ms, 0);
            return (int)ms;
        }
    }
}
