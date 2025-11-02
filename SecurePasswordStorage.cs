using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.Security.Credentials.UI;

namespace BitLockerManager
{
    public class SecurePasswordStorage
    {
        private const string StorageFileName = "BitLockerPasswords.dat";
        private readonly string _storageFilePath;

        // P/Invoke for bringing window to front
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // Windows Hello / Credential UI P/Invoke declarations
        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        private static extern uint CredUIPromptForWindowsCredentials(
            ref CREDUI_INFO credInfo,
            uint authError,
            ref uint authPackage,
            IntPtr InAuthBuffer,
            uint InAuthBufferSize,
            out IntPtr refOutAuthBuffer,
            out uint refOutAuthBufferSize,
            ref bool fSave,
            uint flags);

        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        private static extern bool CredUnPackAuthenticationBuffer(
            uint dwFlags,
            IntPtr pAuthBuffer,
            uint cbAuthBuffer,
            StringBuilder pszUserName,
            ref int pcchMaxUserName,
            StringBuilder pszDomainName,
            ref int pcchMaxDomainName,
            StringBuilder pszPassword,
            ref int pcchMaxPassword);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        private const uint CREDUIWIN_GENERIC = 0x1;
        private const uint CREDUIWIN_CHECKBOX = 0x2;
        private const uint CREDUIWIN_AUTHPACKAGE_ONLY = 0x10;
        private const uint CREDUIWIN_IN_CRED_ONLY = 0x20;
        private const uint CREDUIWIN_ENUMERATE_ADMINS = 0x100;
        private const uint CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200;
        private const uint CREDUIWIN_SECURE_PROMPT = 0x1000;
        private const uint CREDUIWIN_PACK_32_WOW = 0x10000000;

        public SecurePasswordStorage()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "BitLockerManager");
            Directory.CreateDirectory(appFolder);
            _storageFilePath = Path.Combine(appFolder, StorageFileName);
        }

        private async Task<bool> TryBringWindowsHelloToFrontOnceAsync()
        {
            return await Task.Run(() =>
            {
                // Try to find Windows Hello dialog by various methods
                var windowsHelloTitles = new[]
                {
                    "Windows Security",
                    "Microsoft Windows",
                    "Windows Hello",
                    "Making sure it's you"
                };

                foreach (var title in windowsHelloTitles)
                {
                    var hwnd = FindWindow(null, title);
                    if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd))
                    {
                        // Properly activate and focus the Windows Hello dialog
                        ActivateAndFocusWindow(hwnd);
                        return true; // Found and brought to front
                    }
                }

                // If we can't find by title, enumerate all windows and look for Windows Hello
                bool found = false;
                EnumWindows((hWnd, lParam) =>
                {
                    var className = new StringBuilder(256);
                    var windowText = new StringBuilder(256);
                    
                    GetClassName(hWnd, className, className.Capacity);
                    GetWindowText(hWnd, windowText, windowText.Capacity);

                    var classNameStr = className.ToString();
                    var windowTextStr = windowText.ToString();

                    // Look for Windows Hello related class names or window text
                    if ((classNameStr.Contains("Credential") || 
                        classNameStr.Contains("Windows.UI") ||
                        windowTextStr.Contains("Windows Security") ||
                        windowTextStr.Contains("Making sure")) && IsWindowVisible(hWnd))
                    {
                        // Properly activate and focus the Windows Hello dialog
                        ActivateAndFocusWindow(hWnd);
                        found = true;
                        return false; // Stop enumeration
                    }

                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return found;
            });
        }

        private void ActivateAndFocusWindow(IntPtr hwnd)
        {
            try
            {
                // Get the current foreground window and thread
                var foregroundWindow = GetForegroundWindow();
                var currentThreadId = GetCurrentThreadId();
                var targetThreadId = GetWindowThreadProcessId(hwnd, out _);

                // Attach to the target window's thread to properly set focus
                if (targetThreadId != currentThreadId)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, true);
                }

                // Multiple approaches to ensure the window gets focus
                // 1. Make it topmost temporarily
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
                // 2. Show and restore the window
                ShowWindow(hwnd, SW_RESTORE);
                ShowWindow(hwnd, SW_SHOW);
                
                // 3. Bring to top and set foreground
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
                
                // 4. Set as active window and focus
                SetActiveWindow(hwnd);
                SetFocus(hwnd);
                
                // 5. Small delay to let the system process
                Thread.Sleep(100);
                
                // 6. Remove topmost to prevent interference
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                
                // 7. Final focus attempt
                SetForegroundWindow(hwnd);
                SetFocus(hwnd);

                // Detach from the target thread
                if (targetThreadId != currentThreadId)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }
            }
            catch
            {
                // Fallback: simple approach if the complex one fails
                try
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                    BringWindowToTop(hwnd);
                }
                catch { }
            }
        }

        public async Task<bool> IsWindowsHelloAvailableAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability == UserConsentVerifierAvailability.Available;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AuthenticateUserAsync(string reason = "Access saved BitLocker passwords", Form? parentForm = null)
        {
            try
            {
                // Bring the parent form to foreground initially
                bool wasTopMost = false;
                if (parentForm != null)
                {
                    parentForm.Invoke(() =>
                    {
                        wasTopMost = parentForm.TopMost;
                        
                        // Force the form to be visible and active
                        if (parentForm.WindowState == FormWindowState.Minimized)
                        {
                            parentForm.WindowState = FormWindowState.Normal;
                        }
                        
                        parentForm.Focus();
                        parentForm.BringToFront();
                        parentForm.Activate();
                    });
                    
                    // Give the form time to become active
                    await Task.Delay(100);
                }

                // First check if Windows Hello is available
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                
                if (availability == UserConsentVerifierAvailability.Available)
                {
                    // Start a background task to bring Windows Hello to front ONCE when it appears
                    var cancellationTokenSource = new CancellationTokenSource();
                    var dialogFound = false;
                    var bringToFrontTask = Task.Run(async () =>
                    {
                        // Try to find the dialog for up to 10 seconds, but only bring it to front once
                        for (int i = 0; i < 50 && !cancellationTokenSource.Token.IsCancellationRequested && !dialogFound; i++)
                        {
                            if (await TryBringWindowsHelloToFrontOnceAsync())
                            {
                                dialogFound = true;
                                break;
                            }
                            await Task.Delay(200, cancellationTokenSource.Token);
                        }
                    }, cancellationTokenSource.Token);
                    
                    try
                    {
                        // Use Windows Hello authentication
                        var result = await UserConsentVerifier.RequestVerificationAsync(reason);
                        return result == UserConsentVerificationResult.Verified;
                    }
                    finally
                    {
                        // Stop the bring-to-front task
                        cancellationTokenSource.Cancel();
                        try { await bringToFrontTask; } catch (OperationCanceledException) { }
                    }
                }
                else
                {
                    // Windows Hello not available, show informative message and fallback
                    string availabilityMessage = availability switch
                    {
                        UserConsentVerifierAvailability.DeviceNotPresent => "No biometric device found.",
                        UserConsentVerifierAvailability.NotConfiguredForUser => "Windows Hello is not set up for this user.",
                        UserConsentVerifierAvailability.DisabledByPolicy => "Windows Hello is disabled by policy.",
                        UserConsentVerifierAvailability.DeviceBusy => "Biometric device is busy.",
                        _ => "Windows Hello is not available."
                    };

                    try
                    {
                        var fallbackResult = MessageBox.Show(
                            $"{availabilityMessage}\n\n{reason}\n\nContinue with basic authentication?",
                            "Windows Hello Not Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        return fallbackResult == DialogResult.Yes;
                    }
                    finally
                    {
                        // No cleanup needed for TopMost since we didn't change it
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // Fallback to simple dialog if Windows Hello fails
                    var result = MessageBox.Show(
                        $"Windows Hello authentication failed: {ex.Message}\n\n{reason}\n\nContinue with basic authentication?",
                        "Authentication Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    return result == DialogResult.Yes;
                }
                finally
                {
                    // No cleanup needed for TopMost since we didn't change it
                }
            }
        }

        public async Task<bool> SavePasswordAsync(string driveLetter, string password, Form? parentForm = null)
        {
            try
            {
                // Authenticate user before saving
                if (!await AuthenticateUserAsync($"Save password for drive {driveLetter}", parentForm))
                {
                    return false;
                }

                var passwords = await LoadPasswordsAsync();
                
                // Encrypt the password using DPAPI
                var encryptedPassword = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(password),
                    Encoding.UTF8.GetBytes(Environment.UserName),
                    DataProtectionScope.CurrentUser
                );

                passwords[driveLetter] = Convert.ToBase64String(encryptedPassword);
                
                await SavePasswordsAsync(passwords);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetPasswordAsync(string driveLetter, Form? parentForm = null)
        {
            try
            {
                // Authenticate user before retrieving password
                if (!await AuthenticateUserAsync($"Access saved password for drive {driveLetter}", parentForm))
                {
                    return null;
                }

                var passwords = await LoadPasswordsAsync();
                
                if (!passwords.ContainsKey(driveLetter))
                {
                    return null;
                }

                // Decrypt the password using DPAPI
                var encryptedBytes = Convert.FromBase64String(passwords[driveLetter]);
                var decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    Encoding.UTF8.GetBytes(Environment.UserName),
                    DataProtectionScope.CurrentUser
                );

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> HasSavedPasswordAsync(string driveLetter)
        {
            try
            {
                var passwords = await LoadPasswordsAsync();
                return passwords.ContainsKey(driveLetter);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePasswordAsync(string driveLetter, Form? parentForm = null)
        {
            try
            {
                // Authenticate user before removing password
                if (!await AuthenticateUserAsync($"Remove saved password for drive {driveLetter}", parentForm))
                {
                    return false;
                }

                var passwords = await LoadPasswordsAsync();
                
                if (passwords.Remove(driveLetter))
                {
                    await SavePasswordsAsync(passwords);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetSavedDrivesAsync()
        {
            try
            {
                var passwords = await LoadPasswordsAsync();
                return new List<string>(passwords.Keys);
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<Dictionary<string, string>> LoadPasswordsAsync()
        {
            try
            {
                if (!File.Exists(_storageFilePath))
                {
                    return new Dictionary<string, string>();
                }

                var json = await File.ReadAllTextAsync(_storageFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private async Task SavePasswordsAsync(Dictionary<string, string> passwords)
        {
            var json = JsonSerializer.Serialize(passwords, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_storageFilePath, json);
        }
    }
}