using System;
using System.Drawing;
using System.Windows.Forms;

namespace BitLockerManager
{
    public partial class PasswordInputDialog : Form
    {
        private TextBox passwordTextBox = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;

        public string Password => passwordTextBox.Text;

        public PasswordInputDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Enter BitLocker Password";
            this.Size = new Size(350, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = "Enter the password to unlock the BitLocker drive:",
                Location = new Point(10, 15),
                Size = new Size(320, 20)
            };

            passwordTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(320, 25),
                UseSystemPasswordChar = true
            };

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(175, 75),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(255, 75),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] {
                label, passwordTextBox, okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            // Focus on password textbox when dialog opens
            this.Shown += (s, e) => passwordTextBox.Focus();
        }
    }
}