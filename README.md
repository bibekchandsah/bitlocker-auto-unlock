# BitLocker Drive Manager

A C# Windows Forms application to check, lock, and unlock BitLocker-encrypted drives.

**Developer:** Bibek Chand Sah

## Features

- **Detect BitLocker Drives**: Shows all drives and their BitLocker protection status
- **View Drive Details**: Display encryption percentage, lock status, and key protectors
- **Lock Drives**: Lock BitLocker-protected drives (except OS drive) with force dismount
- **Unlock Drives**: Unlock drives using password authentication
- **Process Usage Check**: Check which processes might be using a drive before locking
- **Secure Password Storage**: Save BitLocker passwords encrypted with Windows Data Protection API (DPAPI)
- **Windows Hello Authentication**: Uses native Windows Hello biometric authentication (fingerprint, face recognition, PIN) for accessing saved passwords
- **Password Management**: Save, use, and manage stored passwords with secure authentication
- **Improved Error Handling**: Better error messages and troubleshooting guidance
- **Real-time Status**: Refresh drive status and see current encryption state
- **Auto-Elevation**: Automatically requests administrator privileges on startup
- **System Tray Integration**: Minimize to system tray with context menu for quick drive operations
- **Auto Unlock**: Automatically prompts to unlock the last unlocked drive when application opens

## Requirements

- Windows 10/11 with BitLocker support
- .NET 6.0 or later
- Administrator privileges (recommended for full functionality)
- PowerShell BitLocker module (usually pre-installed on Windows)

## How to Run

1. **Build the application:**
   ```cmd
   dotnet build
   ```

2. **Run the application:**
   ```cmd
   dotnet run
   ```

   **Note:** The application will automatically request administrator privileges when launched. If not running as administrator, it will restart itself with elevated privileges through UAC prompt.

## Usage

### Viewing Drive Status
- The main grid shows all drives with their BitLocker status
- Columns include: Drive letter, Type, Protection Status, Lock Status, Encryption %, and Key Protectors
- Click "Refresh" to update the drive information

### Locking a Drive
1. Select a BitLocker-protected, unlocked drive (not the OS drive)
2. Click "Lock Drive"
3. Confirm the action in the dialog
4. The drive will be dismounted and locked

### Unlocking a Drive
1. Select a locked BitLocker drive
2. **Option A - Manual Password:**
   - Enter the password in the password field
   - Click "Unlock Drive"
3. **Option B - Saved Password (for locked drives only):**
   - Click "🔐 Use Saved" to use a previously saved password
   - Authenticate with Windows Hello (fingerprint, face recognition, or PIN)
4. The drive will be unlocked and mounted if the password is correct

### Password Management
1. **Save Password:**
   - Enter a password for a BitLocker drive
   - Click "💾 Save Password"
   - Authenticate to securely store the password
2. **Use Saved Password:**
   - Select a **locked** drive with a saved password
   - Click "🔐 Use Saved" (only enabled for locked drives)
   - Authenticate with Windows Hello (fingerprint, face, or PIN) to retrieve and use the stored password
3. **Manage Passwords:**
   - Click "⚙️ Manage Passwords" to view and remove saved passwords
   - Remove passwords you no longer need

### System Tray Operations
1. **Minimize to Tray:**
   - Close the main window to minimize to system tray
   - Double-click tray icon to restore window
2. **Tray Context Menu:**
   - **Show/Hide**: Toggle main window visibility
   - **Drive Operations**: Right-click for per-drive lock/unlock options
   - **Use Saved Password**: Quick unlock with saved passwords
   - **Exit**: Close application completely

### Auto Unlock Configuration
1. **Enable Auto Unlock:**
   - Check "🔓 Auto unlock last drive when application opens" in main window
   - Application remembers the last drive you unlock
   - Every time you open the application, it checks if that drive is locked
   - If locked, automatically prompts to unlock the drive
2. **How it works:**
   - **With Saved Password**: Uses Windows Hello authentication and unlocks automatically
   - **Without Saved Password**: Shows password input dialog
   - **Smart Detection**: Only prompts if the drive is actually locked
   - **Convenient Access**: Works every time you open the application
   - **Background Operation**: Can work from system tray without showing main window

## Security Notes

- **Password Storage**: Passwords are encrypted using Windows Data Protection API (DPAPI) and stored locally
- **Memory Security**: Passwords are handled as SecureString objects in memory
- **Windows Hello Authentication**: Native Windows Hello biometric authentication (fingerprint, face recognition, PIN) required before saving or retrieving stored passwords
- **Administrator Privileges**: The application checks for administrator privileges on startup
- **OS Drive Protection**: OS drives cannot be locked programmatically for safety
- **Recovery Keys**: Always ensure you have recovery keys before locking drives
- **Local Storage**: Saved passwords are tied to the current Windows user account

## Technical Implementation

This application uses:
- **PowerShell BitLocker cmdlets** via the PowerShell SDK for BitLocker operations
- **Windows Forms** for the GUI interface
- **Async/await** patterns for non-blocking operations
- **Automatic UAC elevation** through application manifest and programmatic restart
- **PowerShell execution policy bypass** to ensure BitLocker module loads correctly
- **Proper error handling** and user feedback

The implementation follows the recommendations from the instructions.md file, using PowerShell cmdlets as the most maintainable approach for BitLocker management.

## Fixes Applied

- **Auto-elevation**: Application automatically requests administrator privileges on startup
- **PowerShell execution policy**: Bypasses execution policy restrictions for BitLocker module loading
- **Error handling**: Improved error messages and graceful handling of permission issues