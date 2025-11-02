using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;

namespace BitLockerManager
{
    public partial class MainForm : Form
    {
        private DataGridView driveGrid = null!;
        private Button refreshButton = null!;
        private Button lockButton = null!;
        private Button unlockButton = null!;
        private Button checkProcessesButton = null!;
        private TextBox passwordTextBox = null!;
        private Label statusLabel = null!;
        private Button savePasswordButton = null!;
        private Button useSavedPasswordButton = null!;
        private Button managePasswordsButton = null!;
        private CheckBox autoUnlockCheckBox = null!;
        private Label lastUnlockedDriveLabel = null!;
        private List<BitLockerDrive> drives = new List<BitLockerDrive>();
        private SecurePasswordStorage passwordStorage = new SecurePasswordStorage();
        private NotifyIcon notifyIcon = null!;
        private ContextMenuStrip trayContextMenu = null!;
        private const string LastUnlockedDriveKey = "LastUnlockedDrive";
        private const string AutoUnlockEnabledKey = "AutoUnlockEnabled";

        public MainForm()
        {
            InitializeComponent();
            LoadBitLockerStatus();
            
            // Initialize checkbox state after form is fully loaded
            this.Load += MainForm_Load;
            
            // Start auto unlock check on every application start
            _ = Task.Run(async () =>
            {
                // Wait for initialization to complete
                await Task.Delay(1000);
                await CheckAndAutoUnlockAsync();
            });
        }

        private void InitializeComponent()
        {
            this.Text = "BitLocker Drive Manager";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Set window and taskbar icon
            try
            {
                this.Icon = IconHelper.CreateIconFromPng("icon.png");
            }
            catch
            {
                // Use fallback icon if PNG loading fails
                this.Icon = IconHelper.CreateFallbackIcon();
            }

            // Initialize system tray
            InitializeSystemTray();

            // Status label
            statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 25,
                BackColor = SystemColors.Control,
                Padding = new Padding(5)
            };

            // Main panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Drive grid
            driveGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            // Configure columns
            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MountPoint",
                HeaderText = "Drive",
                DataPropertyName = "MountPoint",
                Width = 80
            });

            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VolumeType",
                HeaderText = "Type",
                DataPropertyName = "VolumeType",
                Width = 100
            });

            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProtectionStatus",
                HeaderText = "Protection Status",
                DataPropertyName = "ProtectionStatus",
                Width = 150
            });

            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LockStatus",
                HeaderText = "Lock Status",
                DataPropertyName = "LockStatus",
                Width = 120
            });

            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EncryptionPercentage",
                HeaderText = "Encryption %",
                DataPropertyName = "EncryptionPercentage",
                Width = 100
            });

            driveGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "KeyProtector",
                HeaderText = "Key Protectors",
                DataPropertyName = "KeyProtector",
                Width = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // Control panel
            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 170,
                Padding = new Padding(5)
            };

            refreshButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 30),
                Location = new Point(5, 10)
            };
            refreshButton.Click += RefreshButton_Click;

            lockButton = new Button
            {
                Text = "Lock Drive",
                Size = new Size(80, 30),
                Location = new Point(95, 10),
                Enabled = false
            };
            lockButton.Click += LockButton_Click;

            unlockButton = new Button
            {
                Text = "Unlock Drive",
                Size = new Size(90, 30),
                Location = new Point(185, 10),
                Enabled = false
            };
            unlockButton.Click += UnlockButton_Click;

            checkProcessesButton = new Button
            {
                Text = "Check Usage",
                Size = new Size(90, 30),
                Location = new Point(285, 10),
                Enabled = false
            };
            checkProcessesButton.Click += CheckProcessesButton_Click;

            var passwordLabel = new Label
            {
                Text = "Password:",
                Location = new Point(385, 15),
                Size = new Size(60, 20)
            };

            passwordTextBox = new TextBox
            {
                Location = new Point(450, 12),
                Size = new Size(150, 25),
                UseSystemPasswordChar = true
            };
            passwordTextBox.TextChanged += PasswordTextBox_TextChanged;

            // Password management buttons (second row)
            savePasswordButton = new Button
            {
                Text = "💾 Save Password",
                Size = new Size(120, 30),
                Location = new Point(5, 50),
                Enabled = false
            };
            savePasswordButton.Click += SavePasswordButton_Click;

            useSavedPasswordButton = new Button
            {
                Text = "🔐 Use Saved",
                Size = new Size(100, 30),
                Location = new Point(135, 50),
                Enabled = false
            };
            useSavedPasswordButton.Click += UseSavedPasswordButton_Click;

            managePasswordsButton = new Button
            {
                Text = "⚙️ Manage Passwords",
                Size = new Size(140, 30),
                Location = new Point(245, 50)
            };
            managePasswordsButton.Click += ManagePasswordsButton_Click;

            // Auto unlock checkbox (third row)
            autoUnlockCheckBox = new CheckBox
            {
                Text = "🔓 Auto unlock last drive when application opens",
                Size = new Size(400, 25),
                Location = new Point(5, 90),
                Checked = false // Initialize as false, will be set properly after form loads
            };
            autoUnlockCheckBox.CheckedChanged += AutoUnlockCheckBox_CheckedChanged;

            // Last unlocked drive label
            lastUnlockedDriveLabel = new Label
            {
                Text = "Last unlocked: None",
                Size = new Size(200, 20),
                Location = new Point(5, 115),
                ForeColor = Color.Gray,
                Font = new Font(this.Font.FontFamily, 8.5f, FontStyle.Italic)
            };

            // Temporary debug button (remove after testing)
            var debugButton = new Button
            {
                Text = "Debug Registry",
                Size = new Size(100, 25),
                Location = new Point(410, 90)
            };
            debugButton.Click += (s, e) => {
                var autoUnlockEnabled = GetAutoUnlockEnabled();
                var lastUnlockedDrive = GetLastUnlockedDrive();
                var lastDriveDisplay = string.IsNullOrEmpty(lastUnlockedDrive) ? "None" : lastUnlockedDrive;
                
                MessageBox.Show(
                    $"Auto Unlock Enabled: {autoUnlockEnabled}\n" +
                    $"Checkbox State: {autoUnlockCheckBox.Checked}\n" +
                    $"Last Unlocked Drive: {lastDriveDisplay}\n\n" +
                    $"Registry Path: HKEY_CURRENT_USER\\SOFTWARE\\BitLockerManager",
                    "Debug Info - Auto Unlock Settings");
            };

            controlPanel.Controls.AddRange(new Control[] {
                refreshButton, lockButton, unlockButton, checkProcessesButton, passwordLabel, passwordTextBox,
                savePasswordButton, useSavedPasswordButton, managePasswordsButton, autoUnlockCheckBox, 
                lastUnlockedDriveLabel, debugButton
            });

            mainPanel.Controls.Add(driveGrid);
            mainPanel.Controls.Add(controlPanel);

            this.Controls.Add(mainPanel);
            this.Controls.Add(statusLabel);

            driveGrid.SelectionChanged += DriveGrid_SelectionChanged;
        }

        private void InitializeSystemTray()
        {
            // Create system tray icon
            notifyIcon = new NotifyIcon();
            
            try
            {
                // Use the same icon as the main form but sized for system tray
                var mainIcon = IconHelper.CreateIconFromPng("icon.png");
                notifyIcon.Icon = new Icon(mainIcon, 16, 16);
            }
            catch
            {
                // Use fallback icon for system tray
                notifyIcon.Icon = new Icon(IconHelper.CreateFallbackIcon(), 16, 16);
            }
            
            notifyIcon.Text = "BitLocker Drive Manager";
            notifyIcon.Visible = true;
            
            // Create context menu
            trayContextMenu = new ContextMenuStrip();
            
            // Show/Hide menu item
            var showHideItem = new ToolStripMenuItem("Show");
            showHideItem.Click += ShowHideItem_Click;
            trayContextMenu.Items.Add(showHideItem);
            
            trayContextMenu.Items.Add(new ToolStripSeparator());
            
            // Drive list will be populated dynamically
            trayContextMenu.Opening += TrayContextMenu_Opening;
            
            trayContextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit menu item
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;
            trayContextMenu.Items.Add(exitItem);
            
            notifyIcon.ContextMenuStrip = trayContextMenu;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            // Handle form closing to minimize to tray instead
            this.FormClosing += MainForm_FormClosing;
            this.WindowState = FormWindowState.Normal;
        }

        private async void LoadBitLockerStatus()
        {
            statusLabel.Text = "Loading BitLocker status...";
            refreshButton.Enabled = false;

            try
            {
                drives = await GetBitLockerVolumesAsync();
                driveGrid.DataSource = drives;
                statusLabel.Text = $"Found {drives.Count} drives. {drives.Count(d => d.IsProtected)} protected with BitLocker.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load BitLocker status:\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                refreshButton.Enabled = true;
            }
        }      
        private async Task<List<BitLockerDrive>> GetBitLockerVolumesAsync()
        {
            return await Task.Run(() =>
            {
                var driveList = new List<BitLockerDrive>();

                using (var ps = PowerShell.Create())
                {
                    try
                    {
                        // Set execution policy for this session to allow BitLocker module
                        ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force");
                        ps.Invoke();
                        ps.Commands.Clear();

                        // Import BitLocker module explicitly
                        ps.AddScript("Import-Module BitLocker -Force");
                        ps.Invoke();
                        ps.Commands.Clear();

                        if (ps.HadErrors)
                        {
                            var errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                            throw new Exception($"Failed to import BitLocker module: {errors}");
                        }

                        // Get BitLocker volumes
                        ps.AddScript("Get-BitLockerVolume | Select-Object MountPoint, VolumeType, ProtectionStatus, LockStatus, EncryptionPercentage, KeyProtector");
                        var results = ps.Invoke();

                        if (ps.HadErrors)
                        {
                            var errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                            throw new Exception($"PowerShell errors: {errors}");
                        }

                        foreach (var result in results)
                        {
                            var drive = new BitLockerDrive
                            {
                                MountPoint = result.Properties["MountPoint"]?.Value?.ToString() ?? "Unknown",
                                VolumeType = result.Properties["VolumeType"]?.Value?.ToString() ?? "Unknown",
                                ProtectionStatus = result.Properties["ProtectionStatus"]?.Value?.ToString() ?? "Unknown",
                                LockStatus = result.Properties["LockStatus"]?.Value?.ToString() ?? "Unknown",
                                EncryptionPercentage = result.Properties["EncryptionPercentage"]?.Value?.ToString() ?? "0",
                                KeyProtector = GetKeyProtectorString(result.Properties["KeyProtector"]?.Value)
                            };

                            driveList.Add(drive);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to execute PowerShell command: {ex.Message}");
                    }
                }

                return driveList;
            });
        }

        private string GetKeyProtectorString(object? keyProtectorObj)
        {
            if (keyProtectorObj == null) return "None";

            if (keyProtectorObj is System.Collections.IEnumerable enumerable && !(keyProtectorObj is string))
            {
                var protectors = new List<string>();
                foreach (var item in enumerable)
                {
                    if (item != null)
                        protectors.Add(item.ToString() ?? "Unknown");
                }
                return string.Join(", ", protectors);
            }

            return keyProtectorObj.ToString() ?? "Unknown";
        }

        private async Task<List<string>> GetProcessesUsingDriveAsync(string mountPoint)
        {
            return await Task.Run(() =>
            {
                var processes = new List<string>();
                
                using (var ps = PowerShell.Create())
                {
                    try
                    {
                        // Get processes that have handles to files on the drive
                        ps.AddScript($@"
                            $driveLetter = '{mountPoint}'.TrimEnd(':')
                            Get-Process | Where-Object {{ 
                                $_.Modules -and ($_.Modules | Where-Object {{ $_.FileName -like ""$driveLetter`:*"" }}) 
                            }} | Select-Object -ExpandProperty ProcessName -Unique
                        ");
                        
                        var results = ps.Invoke();
                        
                        foreach (var result in results)
                        {
                            if (result != null)
                                processes.Add(result.ToString() ?? "Unknown");
                        }
                    }
                    catch
                    {
                        // If we can't get the processes, just return empty list
                    }
                }
                
                return processes;
            });
        }

        private async void DriveGrid_SelectionChanged(object? sender, EventArgs e)
        {
            await RefreshButtonStatesAsync();
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            LoadBitLockerStatus();
        }

        private async void LockButton_Click(object? sender, EventArgs e)
        {
            if (driveGrid.SelectedRows.Count == 0) return;

            var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;
            
            var result = MessageBox.Show($"Are you sure you want to lock drive {selectedDrive.MountPoint}?\n\n" +
                "⚠️ IMPORTANT:\n" +
                "• This will dismount the drive and close all open files\n" +
                "• You'll need a password or recovery key to unlock it\n" +
                "• Make sure no programs are currently using this drive\n" +
                "• Save any open work on this drive before proceeding\n\n" +
                "Continue with locking?",
                "Confirm Lock Drive", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
            if (result != DialogResult.Yes)
                return;

            statusLabel.Text = $"Locking drive {selectedDrive.MountPoint}...";
            lockButton.Enabled = false;
            refreshButton.Enabled = false;

            try
            {
                await LockDriveAsync(selectedDrive.MountPoint);
                statusLabel.Text = $"Drive {selectedDrive.MountPoint} locked successfully.";
                MessageBox.Show($"Drive {selectedDrive.MountPoint} has been locked successfully.\n\n" +
                              "The drive is now dismounted and encrypted. Use the unlock function with your password to access it again.",
                              "Lock Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBitLockerStatus(); // Refresh the list
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Failed to lock drive: {ex.Message}";
                
                string errorMessage = $"Failed to lock drive {selectedDrive.MountPoint}:\n\n{ex.Message}\n\n";
                
                if (ex.Message.Contains("Access is denied") || ex.Message.Contains("in use"))
                {
                    errorMessage += "💡 Troubleshooting tips:\n" +
                                  "• Close all programs that might be accessing the drive\n" +
                                  "• Close File Explorer windows showing the drive\n" +
                                  "• End any processes using files on the drive\n" +
                                  "• Try again after a few seconds";
                }
                
                MessageBox.Show(errorMessage, "Lock Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lockButton.Enabled = true;
                refreshButton.Enabled = true;
            }
        }

        private async void UnlockButton_Click(object? sender, EventArgs e)
        {
            if (driveGrid.SelectedRows.Count == 0) return;

            var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;
            
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show("Please enter a password to unlock the drive.", "Password Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                passwordTextBox.Focus();
                return;
            }

            statusLabel.Text = $"Unlocking drive {selectedDrive.MountPoint}...";
            unlockButton.Enabled = false;

            try
            {
                await UnlockDriveWithTrackingAsync(selectedDrive.MountPoint, passwordTextBox.Text);
                statusLabel.Text = $"Drive {selectedDrive.MountPoint} unlocked successfully.";
                passwordTextBox.Clear();
                LoadBitLockerStatus(); // Refresh the list
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Failed to unlock drive: {ex.Message}";
                MessageBox.Show($"Failed to unlock drive {selectedDrive.MountPoint}:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                unlockButton.Enabled = true;
            }
        }

        private async Task LockDriveAsync(string mountPoint)
        {
            await Task.Run(() =>
            {
                using (var ps = PowerShell.Create())
                {
                    try
                    {
                        // Set execution policy and import module
                        ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force");
                        ps.Invoke();
                        ps.Commands.Clear();

                        ps.AddScript("Import-Module BitLocker -Force");
                        ps.Invoke();
                        ps.Commands.Clear();

                        // First try to lock with force dismount
                        ps.AddScript($"Lock-BitLocker -MountPoint '{mountPoint}' -ForceDismount");
                        var results = ps.Invoke();

                        if (ps.HadErrors)
                        {
                            var errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                            
                            // If force dismount fails, try without it
                            if (errors.Contains("ForceDismount") || errors.Contains("parameter"))
                            {
                                ps.Commands.Clear();
                                ps.Streams.Error.Clear();
                                
                                ps.AddScript($"Lock-BitLocker -MountPoint '{mountPoint}'");
                                results = ps.Invoke();
                                
                                if (ps.HadErrors)
                                {
                                    var newErrors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                                    
                                    if (newErrors.Contains("Access is denied") || newErrors.Contains("0x80070005"))
                                    {
                                        throw new Exception($"Access denied. The drive may be in use by another process. Try closing all programs that might be accessing drive {mountPoint} and try again.");
                                    }
                                    else
                                    {
                                        throw new Exception($"Failed to lock drive: {newErrors}");
                                    }
                                }
                            }
                            else if (errors.Contains("Access is denied") || errors.Contains("0x80070005"))
                            {
                                throw new Exception($"Access denied. The drive may be in use by another process. Try closing all programs that might be accessing drive {mountPoint} and try again.");
                            }
                            else
                            {
                                throw new Exception($"PowerShell errors: {errors}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.StartsWith("Failed to lock drive"))
                            throw;
                        else
                            throw new Exception($"Failed to lock drive {mountPoint}: {ex.Message}");
                    }
                }
            });
        }

        private async Task UnlockDriveAsync(string mountPoint, string password)
        {
            await Task.Run(() =>
            {
                using (var ps = PowerShell.Create())
                {
                    // Set execution policy and import module
                    ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force");
                    ps.Invoke();
                    ps.Commands.Clear();

                    ps.AddScript("Import-Module BitLocker -Force");
                    ps.Invoke();
                    ps.Commands.Clear();

                    // Convert password to SecureString
                    var securePassword = new SecureString();
                    foreach (char c in password)
                        securePassword.AppendChar(c);
                    securePassword.MakeReadOnly();

                    ps.AddCommand("Unlock-BitLocker")
                      .AddParameter("MountPoint", mountPoint)
                      .AddParameter("Password", securePassword);

                    var results = ps.Invoke();

                    if (ps.HadErrors)
                    {
                        var errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                        throw new Exception($"PowerShell errors: {errors}");
                    }
                }
            });
        }

        private async void CheckProcessesButton_Click(object? sender, EventArgs e)
        {
            if (driveGrid.SelectedRows.Count == 0) return;

            var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;
            
            statusLabel.Text = $"Checking processes using drive {selectedDrive.MountPoint}...";
            
            try
            {
                var processes = await GetProcessesUsingDriveAsync(selectedDrive.MountPoint);
                
                if (processes.Count == 0)
                {
                    MessageBox.Show($"No processes found using drive {selectedDrive.MountPoint}.\n\n" +
                                  "The drive should be safe to lock.",
                                  "Drive Usage Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var processNames = string.Join("\n• ", processes);
                    MessageBox.Show($"The following processes may be using drive {selectedDrive.MountPoint}:\n\n" +
                                  $"• {processNames}\n\n" +
                                  "Consider closing these applications before locking the drive.",
                                  "Drive Usage Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                statusLabel.Text = $"Drive usage check completed for {selectedDrive.MountPoint}.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Failed to check drive usage.";
                MessageBox.Show($"Failed to check drive usage:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void PasswordTextBox_TextChanged(object? sender, EventArgs e)
        {
            await RefreshButtonStatesAsync();
        }

        private async Task RefreshButtonStatesAsync()
        {
            if (driveGrid.SelectedRows.Count > 0)
            {
                var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;
                
                // Enable lock button only for unlocked, protected drives (and not OS drive)
                lockButton.Enabled = selectedDrive.IsProtected && 
                                   selectedDrive.LockStatus == "Unlocked" && 
                                   !selectedDrive.MountPoint.StartsWith("C:");

                // Enable unlock button only for locked drives
                unlockButton.Enabled = selectedDrive.LockStatus == "Locked";
                
                // Enable check processes button for unlocked drives that are not the OS drive
                // (locked drives are dismounted, so no processes can be using them)
                checkProcessesButton.Enabled = !selectedDrive.MountPoint.StartsWith("C:") && 
                                             selectedDrive.LockStatus == "Unlocked";

                // Enable save password button for protected drives with password entered
                savePasswordButton.Enabled = selectedDrive.IsProtected && 
                                            !string.IsNullOrWhiteSpace(passwordTextBox.Text);

                // Enable use saved password button for LOCKED drives with saved passwords
                bool isLocked = selectedDrive.LockStatus == "Locked";
                bool hasSavedPassword = await passwordStorage.HasSavedPasswordAsync(selectedDrive.MountPoint);
                
                useSavedPasswordButton.Enabled = isLocked && hasSavedPassword;
                
                // Debug info
                statusLabel.Text = $"Drive {selectedDrive.MountPoint}: Status={selectedDrive.LockStatus}, HasSaved={hasSavedPassword}, UseSavedEnabled={useSavedPasswordButton.Enabled}";
            }
            else
            {
                lockButton.Enabled = false;
                unlockButton.Enabled = false;
                checkProcessesButton.Enabled = false;
                savePasswordButton.Enabled = false;
                useSavedPasswordButton.Enabled = false;
            }
            
            // Update the last unlocked drive label
            UpdateLastUnlockedDriveLabel();
        }

        private async void SavePasswordButton_Click(object? sender, EventArgs e)
        {
            if (driveGrid.SelectedRows.Count == 0 || string.IsNullOrWhiteSpace(passwordTextBox.Text))
                return;

            var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;

            try
            {
                // Check if Windows Hello is available
                if (!await passwordStorage.IsWindowsHelloAvailableAsync())
                {
                    var result = MessageBox.Show("Windows Hello is not available or not configured on this device.\n\n" +
                                  "For best security, please set up Windows Hello (fingerprint, face recognition, or PIN) in Windows Settings.\n\n" +
                                  "Continue with basic authentication?",
                                  "Windows Hello Recommended", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    
                    if (result != DialogResult.Yes)
                        return;
                }

                statusLabel.Text = "Authenticating with Windows Hello...";
                
                var success = await passwordStorage.SavePasswordAsync(selectedDrive.MountPoint, passwordTextBox.Text, this);
                
                if (success)
                {
                    statusLabel.Text = $"Password saved for drive {selectedDrive.MountPoint}";
                    MessageBox.Show($"Password for drive {selectedDrive.MountPoint} has been saved securely.\n\n" +
                                  "You can now use 'Use Saved' button to unlock this drive with Windows Hello authentication.",
                                  "Password Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Update button states
                    await RefreshButtonStatesAsync();
                }
                else
                {
                    statusLabel.Text = "Failed to save password";
                    MessageBox.Show("Failed to save password. Authentication may have been cancelled or failed.",
                                  "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Error saving password";
                MessageBox.Show($"Error saving password:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void UseSavedPasswordButton_Click(object? sender, EventArgs e)
        {
            if (driveGrid.SelectedRows.Count == 0) return;

            var selectedDrive = (BitLockerDrive)driveGrid.SelectedRows[0].DataBoundItem;

            try
            {
                statusLabel.Text = "Authenticating with Windows Hello...";
                
                var savedPassword = await passwordStorage.GetPasswordAsync(selectedDrive.MountPoint, this);
                
                if (savedPassword != null)
                {
                    statusLabel.Text = $"Unlocking drive {selectedDrive.MountPoint} with saved password...";
                    
                    try
                    {
                        await UnlockDriveWithTrackingAsync(selectedDrive.MountPoint, savedPassword);
                        statusLabel.Text = $"Drive {selectedDrive.MountPoint} unlocked successfully with saved password.";
                        MessageBox.Show($"Drive {selectedDrive.MountPoint} unlocked successfully using saved password!",
                                      "Unlock Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadBitLockerStatus(); // Refresh the list
                    }
                    catch (Exception unlockEx)
                    {
                        statusLabel.Text = "Failed to unlock drive with saved password";
                        MessageBox.Show($"Failed to unlock drive {selectedDrive.MountPoint} with saved password:\n{unlockEx.Message}\n\n" +
                                      "The saved password may be incorrect or the drive configuration may have changed.",
                                      "Unlock Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    statusLabel.Text = "Authentication cancelled or failed";
                    MessageBox.Show("Authentication was cancelled or failed. Could not retrieve saved password.",
                                  "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Error retrieving saved password";
                MessageBox.Show($"Error retrieving saved password:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ManagePasswordsButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var savedDrives = await passwordStorage.GetSavedDrivesAsync();
                
                if (savedDrives.Count == 0)
                {
                    MessageBox.Show("No saved passwords found.\n\nUse the 'Save Password' button to securely store BitLocker passwords.",
                                  "No Saved Passwords", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var manageForm = new PasswordManagementForm(passwordStorage, savedDrives);
                manageForm.ShowDialog();
                
                // Refresh button states after management
                await RefreshButtonStatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error managing passwords:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region System Tray Event Handlers

        private void ShowHideItem_Click(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Hide();
                ((ToolStripMenuItem)sender!).Text = "Show";
            }
            else
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();
                ((ToolStripMenuItem)sender!).Text = "Hide";
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();
            }
            
            // Update the show/hide menu item text
            var showHideItem = (ToolStripMenuItem)trayContextMenu.Items[0];
            showHideItem.Text = this.Visible ? "Hide" : "Show";
        }

        private async void TrayContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Update the show/hide menu item text
            var showHideItem = (ToolStripMenuItem)trayContextMenu.Items[0];
            showHideItem.Text = this.Visible ? "Hide" : "Show";
            
            // Clear existing drive items (keep Show/Hide, separators, and Exit)
            for (int i = trayContextMenu.Items.Count - 1; i >= 2; i--)
            {
                if (trayContextMenu.Items[i].Text != "Exit")
                {
                    trayContextMenu.Items.RemoveAt(i);
                }
            }
            
            // Refresh drive list
            try
            {
                drives = await GetBitLockerVolumesAsync();
                
                // Add drive menu items
                foreach (var drive in drives.Where(d => d.IsProtected))
                {
                    var driveMenu = new ToolStripMenuItem($"Drive {drive.MountPoint}");
                    
                    // Lock/Unlock menu item
                    if (drive.LockStatus == "Locked")
                    {
                        var unlockItem = new ToolStripMenuItem("🔓 Unlock Drive");
                        unlockItem.Click += async (s, args) => await TrayUnlockDrive_Click(drive);
                        driveMenu.DropDownItems.Add(unlockItem);
                        
                        // Use saved password item (only for locked drives)
                        if (await passwordStorage.HasSavedPasswordAsync(drive.MountPoint))
                        {
                            var useSavedItem = new ToolStripMenuItem("🔐 Use Saved Password");
                            useSavedItem.Click += async (s, args) => await TrayUseSavedPassword_Click(drive);
                            driveMenu.DropDownItems.Add(useSavedItem);
                        }
                    }
                    else if (drive.LockStatus == "Unlocked" && !drive.MountPoint.StartsWith("C:"))
                    {
                        var lockItem = new ToolStripMenuItem("🔒 Lock Drive");
                        lockItem.Click += async (s, args) => await TrayLockDrive_Click(drive);
                        driveMenu.DropDownItems.Add(lockItem);
                    }
                    
                    trayContextMenu.Items.Insert(trayContextMenu.Items.Count - 2, driveMenu);
                }
            }
            catch (Exception ex)
            {
                var errorItem = new ToolStripMenuItem($"Error: {ex.Message}");
                errorItem.Enabled = false;
                trayContextMenu.Items.Insert(trayContextMenu.Items.Count - 2, errorItem);
            }
        }

        private async Task TrayLockDrive_Click(BitLockerDrive drive)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to lock drive {drive.MountPoint}?\n\n" +
                    "This will dismount the drive and you'll need a password or recovery key to unlock it.",
                    "Confirm Lock Drive",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    await LockDriveAsync(drive.MountPoint);
                    notifyIcon.ShowBalloonTip(3000, "BitLocker Manager", 
                        $"Drive {drive.MountPoint} locked successfully.", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to lock drive {drive.MountPoint}:\n{ex.Message}", 
                    "Lock Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task TrayUnlockDrive_Click(BitLockerDrive drive)
        {
            try
            {
                // Show password input dialog
                var passwordDialog = new PasswordInputDialog();
                if (passwordDialog.ShowDialog() == DialogResult.OK)
                {
                    await UnlockDriveWithTrackingAsync(drive.MountPoint, passwordDialog.Password);
                    notifyIcon.ShowBalloonTip(3000, "BitLocker Manager", 
                        $"Drive {drive.MountPoint} unlocked successfully.", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unlock drive {drive.MountPoint}:\n{ex.Message}", 
                    "Unlock Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task TrayUseSavedPassword_Click(BitLockerDrive drive)
        {
            try
            {
                var savedPassword = await passwordStorage.GetPasswordAsync(drive.MountPoint, this);
                if (savedPassword != null)
                {
                    await UnlockDriveWithTrackingAsync(drive.MountPoint, savedPassword);
                    notifyIcon.ShowBalloonTip(3000, "BitLocker Manager", 
                        $"Drive {drive.MountPoint} unlocked successfully using saved password.", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unlock drive {drive.MountPoint} with saved password:\n{ex.Message}", 
                    "Unlock Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ExitItem_Click(object? sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                
                // Update the show/hide menu item text
                var showHideItem = (ToolStripMenuItem)trayContextMenu.Items[0];
                showHideItem.Text = "Show";
                
                // Show balloon tip on first minimize
                notifyIcon.ShowBalloonTip(2000, "BitLocker Manager", 
                    "Application minimized to system tray. Double-click to restore.", ToolTipIcon.Info);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                notifyIcon?.Dispose();
                trayContextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Auto Unlock Functionality

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Properly initialize the checkbox state after the form is fully loaded
            // Temporarily disable the event handler to avoid triggering it during initialization
            autoUnlockCheckBox.CheckedChanged -= AutoUnlockCheckBox_CheckedChanged;
            
            var enabled = GetAutoUnlockEnabled();
            autoUnlockCheckBox.Checked = enabled;
            
            System.Diagnostics.Debug.WriteLine($"MainForm_Load: Setting checkbox to {enabled}");
            
            // Update the last unlocked drive label
            UpdateLastUnlockedDriveLabel();
            
            // Re-enable the event handler
            autoUnlockCheckBox.CheckedChanged += AutoUnlockCheckBox_CheckedChanged;
        }

        private void UpdateLastUnlockedDriveLabel()
        {
            var lastDrive = GetLastUnlockedDrive();
            if (string.IsNullOrEmpty(lastDrive))
            {
                lastUnlockedDriveLabel.Text = "Last unlocked: None";
                lastUnlockedDriveLabel.ForeColor = Color.Gray;
            }
            else
            {
                lastUnlockedDriveLabel.Text = $"Last unlocked: Drive {lastDrive}";
                lastUnlockedDriveLabel.ForeColor = Color.DarkBlue;
            }
        }

        private void AutoUnlockCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"AutoUnlockCheckBox_CheckedChanged: Checkbox is now {autoUnlockCheckBox.Checked}");
            
            SetAutoUnlockEnabled(autoUnlockCheckBox.Checked);
            
            if (autoUnlockCheckBox.Checked)
            {
                // Show information about the feature
                MessageBox.Show(
                    "Auto unlock feature enabled!\n\n" +
                    "• The application will remember the last drive you unlock\n" +
                    "• Every time you open the application, it will check if that drive is locked\n" +
                    "• If locked, it will automatically prompt to unlock the drive\n" +
                    "• Uses saved passwords when available, otherwise prompts for password",
                    "Auto Unlock Enabled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private bool GetAutoUnlockEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BitLockerManager", false))
                {
                    if (key == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GetAutoUnlockEnabled: Registry key does not exist");
                        return false;
                    }
                    
                    var value = key.GetValue(AutoUnlockEnabledKey);
                    if (value == null)
                    {
                        System.Diagnostics.Debug.WriteLine("GetAutoUnlockEnabled: Registry value does not exist");
                        return false;
                    }
                    
                    // Handle both DWord (int) and string values for backward compatibility
                    bool result = false;
                    if (value is int intValue)
                    {
                        result = intValue != 0;
                        System.Diagnostics.Debug.WriteLine($"GetAutoUnlockEnabled: Registry DWord value = {intValue}, Result = {result}");
                    }
                    else
                    {
                        var stringValue = value.ToString();
                        result = string.Equals(stringValue, "True", StringComparison.OrdinalIgnoreCase) ||
                                stringValue == "1";
                        System.Diagnostics.Debug.WriteLine($"GetAutoUnlockEnabled: Registry string value = '{stringValue}', Result = {result}");
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAutoUnlockEnabled error: {ex.Message}");
                return false;
            }
        }

        private void SetAutoUnlockEnabled(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BitLockerManager"))
                {
                    if (key == null)
                    {
                        throw new Exception("Failed to create registry key");
                    }
                    
                    // Use boolean value directly instead of string
                    key.SetValue(AutoUnlockEnabledKey, enabled, RegistryValueKind.DWord);
                    
                    // Debug: Confirm what we're writing
                    System.Diagnostics.Debug.WriteLine($"SetAutoUnlockEnabled: Writing '{enabled}' to registry as DWord");
                    
                    // Verify the write by reading it back
                    var verification = key.GetValue(AutoUnlockEnabledKey);
                    System.Diagnostics.Debug.WriteLine($"SetAutoUnlockEnabled: Verification read = '{verification}' (type: {verification?.GetType()})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetAutoUnlockEnabled error: {ex.Message}");
                MessageBox.Show($"Failed to save auto unlock setting: {ex.Message}",
                    "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string? GetLastUnlockedDrive()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BitLockerManager", false))
                {
                    return key?.GetValue(LastUnlockedDriveKey)?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private void SetLastUnlockedDrive(string driveLetter)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BitLockerManager"))
                {
                    key.SetValue(LastUnlockedDriveKey, driveLetter);
                }
            }
            catch
            {
                // Silently fail - not critical
            }
        }

        private async Task CheckAndAutoUnlockAsync()
        {
            try
            {
                // Only proceed if auto unlock is enabled
                if (!GetAutoUnlockEnabled())
                    return;

                var lastUnlockedDrive = GetLastUnlockedDrive();
                if (string.IsNullOrEmpty(lastUnlockedDrive))
                    return;

                // Wait a bit for the system to settle after boot
                await Task.Delay(3000);

                // Get current drive status
                drives = await GetBitLockerVolumesAsync();
                var targetDrive = drives.FirstOrDefault(d => d.MountPoint == lastUnlockedDrive);

                if (targetDrive == null || !targetDrive.IsProtected)
                    return;

                // Only proceed if the drive is currently locked
                if (targetDrive.LockStatus != "Locked")
                    return;

                // Show notification about auto unlock attempt
                var message = $"Your last unlocked drive {targetDrive.MountPoint} is currently locked. Attempting to unlock...";
                notifyIcon.ShowBalloonTip(5000, "BitLocker Manager", message, ToolTipIcon.Info);

                // Try to unlock with saved password first
                if (await passwordStorage.HasSavedPasswordAsync(targetDrive.MountPoint))
                {
                    await TrayUseSavedPassword_Click(targetDrive);
                }
                else
                {
                    // Show password input dialog
                    await TrayUnlockDrive_Click(targetDrive);
                }
            }
            catch (Exception ex)
            {
                // Show error in balloon tip
                notifyIcon.ShowBalloonTip(5000, "BitLocker Manager",
                    $"Auto unlock failed: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private async Task UnlockDriveWithTrackingAsync(string mountPoint, string password)
        {
            await UnlockDriveAsync(mountPoint, password);
            
            // Track this as the last unlocked drive
            SetLastUnlockedDrive(mountPoint);
            
            // Update the UI label if we're on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(UpdateLastUnlockedDriveLabel);
            }
            else
            {
                UpdateLastUnlockedDriveLabel();
            }
        }

        #endregion
    }

    public class BitLockerDrive
    {
        public string MountPoint { get; set; } = string.Empty;
        public string VolumeType { get; set; } = string.Empty;
        public string ProtectionStatus { get; set; } = string.Empty;
        public string LockStatus { get; set; } = string.Empty;
        public string EncryptionPercentage { get; set; } = string.Empty;
        public string KeyProtector { get; set; } = string.Empty;

        public bool IsProtected => ProtectionStatus == "On" || (ProtectionStatus == "Unknown" && !string.IsNullOrEmpty(KeyProtector) && KeyProtector != "None");
    }
}