@echo off
REM This batch file must be run as Administrator
title BitLocker Auto Unlock - Task Creation (Admin Required)
@REM color 0A

REM Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: This script must be run as Administrator!
    echo.
    echo Please:
    echo 1. Right-click on this batch file
    echo 2. Select "Run as administrator"
    echo 3. Click "Yes" when prompted
    echo.
    pause
    exit /b 1
)

echo =====================================
echo BitLocker Auto Unlock - Task Creation
echo =====================================
echo Running as Administrator: YES
echo.

REM Get the directory where this batch file is located
set "SCRIPT_DIR=%~dp0"
set "EXE_PATH=%SCRIPT_DIR%BitLockerManager.exe"

echo Script Directory: %SCRIPT_DIR%
echo Executable Path: %EXE_PATH%
echo.

REM Check if executable exists
if not exist "%EXE_PATH%" (
    echo ERROR: BitLockerManager.exe not found!
    echo Expected location: %EXE_PATH%
    echo Make sure this batch file is in the same folder as BitLockerManager.exe
    echo.
    pause
    exit /b 1
)

echo Found BitLockerManager.exe
echo.

REM Remove existing task (ignore errors)
echo Removing existing task if it exists...
schtasks /delete /tn "BitLocker Auto Unlock" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo Existing task removed
) else (
    echo No existing task found
)
echo.

REM Create new task
echo Creating new startup task...
schtasks /create /tn "BitLocker Auto Unlock" /tr "\"%EXE_PATH%\"" /sc onlogon /delay 0000:30 /rl highest /f
@REM schtasks /create /tn "BitLocker Auto Unlock" /tr "\"%EXE_PATH%\"" /sc onlogon /delay 0000:30 /rl highest /ru "%USERNAME%" /f


set TASK_CREATE_ERROR=%errorlevel%

if %TASK_CREATE_ERROR% equ 0 (
    echo Disabling AC power requirement...
    powershell -NoProfile -Command "$s = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries:$true -DontStopIfGoingOnBatteries:$true; Set-ScheduledTask -TaskName 'BitLocker Auto Unlock' -Settings $s"
    if %errorlevel% neq 0 (
        echo Warning: Could not disable power restrictions. Task created but may only run on AC power.
    )
)


if %TASK_CREATE_ERROR% equ 0 (
    echo.
    echo ========================================
    echo SUCCESS: Task created successfully!
    echo ========================================
    echo BitLocker Manager will start automatically when you log in
    echo.
    
    REM Show task details
    echo Task Details:
    echo -------------
    schtasks /query /tn "BitLocker Auto Unlock" /fo list > temp_task_info.txt 2>nul
    if exist temp_task_info.txt (
        findstr /i "TaskName State Next" temp_task_info.txt
        del temp_task_info.txt
    )
    
) else (
    echo.
    echo ========================================
    echo ERROR: Failed to create task
    echo ========================================
    echo Error Code: %errorlevel%
    echo.
    echo Possible causes:
    echo 1. Insufficient permissions ^(even though running as admin^)
    echo 2. Group Policy restrictions
    echo 3. Corporate security policies
    echo 4. Task Scheduler service not running
    echo.
    echo Try these troubleshooting steps:
    echo 1. Check if Task Scheduler service is running: services.msc
    echo 2. Try creating the task manually in Task Scheduler GUI
    echo 3. Contact your system administrator if on a corporate network
    echo 4. Try running this batch file as Administrator
    echo.
    echo Press any key to exit...
    pause >nul
)

echo.
echo BitLocker Auto Unlock - Task Created successfully
echo.
echo Closing in 3 seconds...
timeout /t 3 /nobreak >nul
exit
