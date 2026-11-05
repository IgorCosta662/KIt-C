using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace TechKitPro
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsRunningAsAdministrator())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath ?? Application.ExecutablePath,
                        Verb = "runas",
                        UseShellExecute = true,
                    });
                }
                catch (Exception)
                {
                    // User cancelled UAC prompt
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }

        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
