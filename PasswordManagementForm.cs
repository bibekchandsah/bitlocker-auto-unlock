using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BitLockerManager
{
    public partial class PasswordManagementForm : Form
    {
        private readonly SecurePasswordStorage passwordStorage;
        private ListBox driveListBox = null!;
        private Button removeButton = null!;
        private Button closeButton = null!;
        private Label infoLabel = null!;

        public PasswordManagementForm(SecurePasswordStorage storage, List<string> savedDrives)
        {
            passwordStorage = storage;
            InitializeComponent();
            LoadSavedDrives(savedDrives);
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Saved Passwords";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            infoLabel = new Label
            {
                Text = "Saved BitLocker passwords (secured with Windows Hello):",
                Location = new Point(10, 10),
                Size = new Size(360, 20)
            };

            driveListBox = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(360, 150),
                SelectionMode = SelectionMode.One
            };
            driveListBox.SelectedIndexChanged += DriveListBox_SelectedIndexChanged;

            removeButton = new Button
            {
                Text = "🗑️ Remove Selected",
                Location = new Point(10, 200),
                Size = new Size(120, 30),
                Enabled = false
            };
            removeButton.Click += RemoveButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(295, 200),
                Size = new Size(75, 30)
            };
            closeButton.Click += (s, e) => this.Close();

            var warningLabel = new Label
            {
                Text = "⚠️ Removing a password will require Windows Hello authentication",
                Location = new Point(10, 240),
                Size = new Size(360, 20),
                ForeColor = Color.DarkOrange,
                Font = new Font(this.Font, FontStyle.Italic)
            };

            this.Controls.AddRange(new Control[] {
                infoLabel, driveListBox, removeButton, closeButton, warningLabel
            });
        }

        private void LoadSavedDrives(List<string> savedDrives)
        {
            driveListBox.Items.Clear();
            foreach (var drive in savedDrives.OrderBy(d => d))
            {
                driveListBox.Items.Add($"Drive {drive} - BitLocker Password");
            }

            if (driveListBox.Items.Count == 0)
            {
                driveListBox.Items.Add("No saved passwords found");
                removeButton.Enabled = false;
            }
        }

        private void DriveListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            removeButton.Enabled = driveListBox.SelectedIndex >= 0 && 
                                 !driveListBox.SelectedItem?.ToString()?.Contains("No saved passwords") == true;
        }

        private async void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (driveListBox.SelectedIndex < 0) return;

            var selectedItem = driveListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem)) return;

            // Extract drive letter from the display text
            var driveLetter = selectedItem.Split(' ')[1]; // "Drive D:" -> "D:"

            var result = MessageBox.Show(
                $"Are you sure you want to remove the saved password for {driveLetter}?\n\n" +
                "This action requires Windows Hello authentication and cannot be undone.",
                "Confirm Password Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                removeButton.Enabled = false;
                removeButton.Text = "Authenticating...";

                var success = await passwordStorage.RemovePasswordAsync(driveLetter, this);

                if (success)
                {
                    driveListBox.Items.RemoveAt(driveListBox.SelectedIndex);
                    
                    if (driveListBox.Items.Count == 0)
                    {
                        driveListBox.Items.Add("No saved passwords found");
                    }

                    MessageBox.Show($"Password for {driveLetter} has been removed successfully.",
                                  "Password Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to remove password. Authentication may have been cancelled.",
                                  "Removal Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing password:\n{ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                removeButton.Text = "🗑️ Remove Selected";
                DriveListBox_SelectedIndexChanged(null, EventArgs.Empty); // Refresh button state
            }
        }
    }
}