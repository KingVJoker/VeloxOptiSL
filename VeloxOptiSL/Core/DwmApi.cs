#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VeloxOptiSL.Core
{
    public static class DwmApi
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public enum BackdropType
        {
            Auto = 0,
            MainWindow = 1,     // Mica base
            Acrylic = 2,        // Acrylic background
            Tabbed = 3          // Mica Alt
        }

        public static void EnableFluentBackdrop(Window window, BackdropType backdropType = BackdropType.MainWindow)
        {
            var hwndHelper = new WindowInteropHelper(window);
            IntPtr hwnd = hwndHelper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // Enable Dark Mode Title bar for consistency
            int useDarkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

            // Set Rounded Corners (Preference 2 = Round, 3 = SmallRound)
            int cornerPreference = 2;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            // Set System Backdrop (Mica / Acrylic)
            int backdropValue = (int)backdropType;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropValue, sizeof(int));

            // Ensure window background is transparent so the backdrop shows through
            window.Background = System.Windows.Media.Brushes.Transparent;
        }
    }
}