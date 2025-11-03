# BitLocker Drive Manager - Distribution Guide

## 📦 **Files to Share with Users**

### **Required Files for Distribution:**
1. **Entire `bin\Release\net8.0-windows10.0.17763.0` folder** - Contains all application files and dependencies
2. **README.md** - User documentation and instructions

### **How to Create Distribution Package:**

1. **Build Release Version:**
   ```cmd
   dotnet build -c Release
   ```

2. **Create Distribution Folder:**
   ```
   BitLockerManager-v1.0/
   ├── BitLockerManager.exe           (Main executable)
   ├── BitLockerManager.dll           (Application library)
   ├── BitLockerManager.deps.json     (Dependencies)
   ├── BitLockerManager.runtimeconfig.json (Runtime config)
   ├── icon.png                       (Application icon)
   ├── icon.ico                       (Windows icon)
   ├── All DLL files                  (PowerShell and Windows APIs)
   ├── runtimes/ folder               (Platform-specific libraries)
   ├── Language folders (cs/, de/, es/, etc.) (Localization)
   └── README.md                      (User guide)
   ```

3. **Package for Distribution:**
   - Copy entire `bin\Release\net8.0-windows10.0.17763.0` folder
   - Copy `README.md` to the folder
   - Rename folder to `BitLockerManager-v1.0`
   - Create ZIP file: `BitLockerManager-v1.0.zip`

## 📋 **User Requirements:**

### **System Requirements:**
- Windows 10 version 1809 (build 17763) or later
- Windows 11 (any version)
- **.NET 8.0 Runtime** (users need to install if not present)
- Administrator privileges (application will request UAC)
- BitLocker feature enabled on Windows

### **Installation Requirements:**
- **.NET 8.0 Desktop Runtime** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Users should install "Desktop Runtime" (not SDK)
- Approximately 50MB download

## 📧 **Distribution Methods:**

### **Option 1: Direct File Sharing (Recommended)**
- Create ZIP file with all required files (~15-20MB)
- Share via email, cloud storage, or USB drive
- Users extract and run BitLockerManager.exe

### **Option 2: GitHub Release**
- Create a release on GitHub
- Upload the zipped package as a release asset
- Include installation instructions in release notes

### **Option 3: Installer Package**
- Use Inno Setup or similar to create installer
- Can include .NET runtime check and download
- Professional distribution method

## 📝 **User Installation Instructions:**

### **Step 1: Install .NET Runtime (if needed)**
1. Download .NET 8.0 Desktop Runtime from Microsoft
2. Run the installer (requires admin rights)
3. Restart computer if prompted

### **Step 2: Install BitLocker Manager**
1. Extract the ZIP file to desired location (e.g., `C:\Program Files\BitLockerManager`)
2. Right-click `BitLockerManager.exe` → "Run as administrator"
3. Allow UAC prompt when requested
4. Application will start and create system tray icon

## ⚠️ **Important Notes for Users:**

1. **Administrator Rights:** Application requires admin privileges and will show UAC prompt
2. **Windows Defender:** May flag as unknown application - users need to allow it
3. **Keep Files Together:** Don't separate the files - keep entire folder intact
4. **First Run:** Application will create system tray icon and may show Windows Hello setup prompts
5. **.NET Dependency:** Users need .NET 8.0 Desktop Runtime installed

## 🔒 **Security Considerations:**

- Application stores encrypted passwords using Windows Data Protection API (DPAPI)
- Passwords are tied to the user account and cannot be accessed by other users
- Windows Hello authentication required for accessing saved passwords
- All BitLocker operations use official Microsoft PowerShell cmdlets
- Application requires administrator privileges for BitLocker management

## 📊 **File Sizes:**
- **ZIP Package**: ~15-20MB (much smaller than self-contained)
- **Extracted**: ~40-50MB (includes all dependencies)
- **Runtime Requirement**: .NET 8.0 Desktop Runtime (~50MB one-time install)