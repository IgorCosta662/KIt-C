using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace KitTecnicoPro.Views
{
    public partial class QuickToolsWindow : Window
    {
        public QuickToolsWindow() {
            InitializeComponent();
            this.SourceInitialized += (s, e) => 
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch {}
            };

        
        }private void OnLaunch(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button)?.Tag?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(tag)) return;

            try
            {
                if      (tag == "__chkdsk")   RunCmd("chkdsk C: /f /r");
                else if (tag == "__ipconfig")  RunCmd("ipconfig /all");
                else if (tag == "__ping")      RunCmd("ping 8.8.8.8 -t");
                else if (tag == "__netstat")   RunCmd("netstat -an");
                else if (tag == "__flushdns")  RunCmdAdmin("ipconfig /flushdns");
                else if (tag == "__sfc")       RunCmdAdmin("sfc /scannow");
                else if (tag == "__dism")      RunCmdAdmin("DISM /Online /Cleanup-Image /RestoreHealth");
                else if (tag == "__startup")   Process.Start(new ProcessStartInfo("taskmgr") { UseShellExecute = true });
                else if (tag.StartsWith("explorer "))
                {
                    var parts = tag.Split(' ', 2);
                    Process.Start(new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "")
                        { UseShellExecute = true });
                }
                else if (tag.Contains(".exe "))
                {
                    var p = tag.Split(' ', 2);
                    Process.Start(new ProcessStartInfo(p[0], p.Length > 1 ? p[1] : "")
                        { UseShellExecute = true });
                }
                else
                {
                    Process.Start(new ProcessStartInfo(tag) { UseShellExecute = true });
                }

                TxtStatus.Text = $"✅ Aberto: {tag.Replace("__", "")}";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Erro: {ex.Message}";
            }
        }

        private static void RunCmd(string command)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/k {command}")
            {
                UseShellExecute = true
            });
        }

        private static void RunCmdAdmin(string command)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/k {command}")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
    }
}


