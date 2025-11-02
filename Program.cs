using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace BitLockerManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Check if running as administrator
            if (!IsRunningAsAdministrator())
            {
                // Restart as administrator
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "",
                        Verb = "runas" // This triggers UAC prompt
                    };
                    
                    Process.Start(processInfo);
                    return; // Exit current instance
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to restart as administrator: {ex.Message}\n\n" +
                                  "Please manually run this application as administrator.", 
                                  "Administrator Required", 
                                  MessageBoxButtons.OK, 
                                  MessageBoxIcon.Error);
                    return;
                }
            }
            
            Application.Run(new MainForm());
        }

        private static bool IsRunningAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}