using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

namespace G4EUkrChatSupportFork.Sys
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint thread);

        public static CultureInfo GetCurrentKeyboardLayout(uint aWindowThreadProcessId)
        {
            try
            {
                return new CultureInfo(GetKeyboardLayout(aWindowThreadProcessId).ToInt32() & 0xFFFF);
            }
            catch
            {
                return new CultureInfo(1033);
            }
        }
    }
}
