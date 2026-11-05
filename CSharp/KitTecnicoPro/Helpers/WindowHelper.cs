using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace KitTecnicoPro.Helpers
{
    public static class WindowHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Windows 11 Mica / Acrylic Backdrop
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public static void ApplyModernBackdrop(System.Windows.Window window)
        {
            try 
            {
                var hwnd = new WindowInteropHelper(window).EnsureHandle();
                
                // Force Dark Mode Title Bar
                int dark = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
                
                // Ativa o Efeito de Vidro "Mica" (ou Acrylic dependendo do Windows 11)
                int backdrop = 2; // 2 = Mica, 3 = Acrylic, 4 = Tabbed
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
            catch 
            {
                // Ignora silenciosamente em Windows mais antigos
            }
        }
    }
}
