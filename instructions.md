you can detect which volumes are BitLocker-protected and you can lock/unlock them from a C# program. There are three practical approaches: the BitLocker WMI provider (`Win32_EncryptableVolume`), the built-in command-line tool `manage-bde`, or the PowerShell BitLocker cmdlets (`Get-BitLockerVolume`, `Lock-BitLocker`, `Unlock-BitLocker`). Each approach has slightly different capabilities and permission requirements. ([Microsoft Learn][1])

What you can do (high level)

* **Detect** BitLocker volumes and their state (protected/unprotected, locked/unlocked) using WMI (Win32_EncryptableVolume) or PowerShell `Get-BitLockerVolume`. ([Microsoft Learn][1])
* **Lock** a data/removable BitLocker volume (dismount it and remove its key from memory) via the WMI `Lock` method or `manage-bde -lock` / `Lock-BitLocker`. Note: you cannot programmatically lock the currently running OS volume. ([Microsoft Learn][2])
* **Unlock** a protected volume by supplying a valid protector (password/recovery key/external key) via WMI unlock methods, `manage-bde -unlock`, or `Unlock-BitLocker`. ([Microsoft Learn][3])

Which approach to pick (pros/cons)

1. **WMI / `Win32_EncryptableVolume`**

   * Pros: Full Win32 API surface for BitLocker (methods like `GetProtectionStatus`, `Lock`, and unlock methods). Good for tight integration and status queries. ([Microsoft Learn][1])
   * Cons: Some WMI calls require admin privileges; programming the COM/WMI interfaces needs careful error handling and testing.

2. **PowerShell BitLocker cmdlets** (`Get-BitLockerVolume`, `Lock-BitLocker`, `Unlock-BitLocker`)

   * Pros: Very convenient and high level; returns objects you can consume easily. You can invoke PowerShell from C# using the PowerShell SDK to keep things managed. ([Microsoft Learn][4])
   * Cons: Requires the BitLocker PowerShell module to be available (it normally is on Windows), and some cmdlets may prompt or require elevation depending on the operation.

3. **Command line `manage-bde`**

   * Pros: Simple to invoke from C# (`Process.Start`) and mirrors what admins use (`manage-bde -status`, `-lock`, `-unlock`). ([Microsoft Learn][5])
   * Cons: Parsing text output, less elegant than WMI/PowerShell, and may require elevation.

Permissions & limitations you must plan for

* **Elevation/admin**: Locking and some management functions often require administrative privileges; unlocking with a user password sometimes works for standard users if a password protector was configured and the policy allows it — but don’t assume you can do everything as a normal user. Test behavior in your target environment. ([Microsoft Learn][2])
* **OS volume**: You cannot lock the running OS volume. The WMI `Lock` method and `manage-bde -lock` explicitly exclude the system/boot volume. ([Microsoft Learn][2])
* **Security**: Never store cleartext passwords. If your app accepts passwords to unlock volumes, treat them carefully (secure strings in memory, avoid logs, encrypt at rest). Ask for user consent/UAC when needed.

Implementation options from C#

* **Call PowerShell from C#** (recommended for ease): use `System.Management.Automation` to run `Get-BitLockerVolume`, `Unlock-BitLocker -MountPoint X: -Password (ConvertTo-SecureString ...)`, or `Lock-BitLocker -MountPoint X:` and read result objects. (Docs: PowerShell BitLocker cmdlets). ([Microsoft Learn][4])
* **Use WMI**: query `ROOT\CIMV2\Security\MicrosoftVolumeEncryption` for instances of `Win32_EncryptableVolume` and call methods like `GetProtectionStatus`, `Lock`, `UnlockWithNumericalPassword` / `UnlockWithExternalKey` etc. (Docs: Win32_EncryptableVolume). ([Microsoft Learn][1])
* **Run `manage-bde`**: call `manage-bde -status` to detect BitLocker status, use `manage-bde -unlock X: -password` or `manage-bde -lock X:` to change state. Parse output and handle exit codes. ([Microsoft Learn][5])

Quick example directions (conceptual)

* To **detect** BitLocker volumes: call `Get-BitLockerVolume` (PowerShell) and read the `MountPoint`/`ProtectionStatus` fields; or enumerate `Win32_EncryptableVolume` WMI instances and map to drive letters. ([Microsoft Learn][4])
* To **lock** a drive programmatically: either call `Lock()` on the `Win32_EncryptableVolume` instance for that volume or run `manage-bde -lock X:` / `Lock-BitLocker -MountPoint X:`. ([Microsoft Learn][2])
* To **unlock**: use `Unlock` WMI methods or `manage-bde -unlock X: -password <pwd>` or `Unlock-BitLocker -MountPoint X: -Password (SecureString)`. Ensure you provide the correct protector. ([Microsoft Learn][3])

Practical recommendations

* For a GUI app, **PowerShell via the C# PowerShell SDK** is often the easiest and most maintainable path (you get structured objects rather than parsing text). ([Microsoft Learn][4])
* Always check and *request elevation* when you need admin operations (present UAC). Gracefully handle the case where the app cannot get the required rights.
* Don’t roll your own “drive encryption” — you are only controlling native BitLocker; document clearly that admin can override and that this is not a replacement for enterprise management tools like BitLocker with AD key escrow.
* Test across scenarios: removable drives, fixed non-OS drives, TPM vs password protectors, AD-joined machines, non-AD machines.
