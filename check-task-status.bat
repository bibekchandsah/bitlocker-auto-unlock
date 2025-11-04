@echo off
title BitLocker Auto Unlock - Task Status Check
color 0B

echo ========================================
echo   BitLocker Auto Unlock - Task Status
echo ========================================
echo.

echo Checking for BitLocker Auto Unlock task...
echo.

REM Check if task exists
schtasks /query /tn "BitLocker Auto Unlock" >nul 2>&1

if %errorlevel% equ 0 (
    echo ========================================
    echo          TASK STATUS: FOUND
    echo ========================================
    echo.
    echo The BitLocker Auto Unlock task exists and is configured!
    echo.
    echo Task Details:
    echo -------------
    schtasks /query /tn "BitLocker Auto Unlock" /fo list > temp_task_status.txt 2>nul
    if exist temp_task_status.txt (
        findstr /i "TaskName State Next Author" temp_task_status.txt
        del temp_task_status.txt
    )
    echo.
    echo ========================================
    echo           STATUS: WORKING
    echo ========================================
    echo.
    echo What this means:
    echo - Auto-startup is configured correctly
    echo - BitLocker Manager will start when you log in
    echo - The task will run 30 seconds after login
    echo - You can see this task in Task Scheduler ^(taskschd.msc^)
    echo.
    echo To test: Log out and log back in to Windows
    echo.
) else (
    echo ========================================
    echo        TASK STATUS: NOT FOUND
    echo ========================================
    echo.
    echo The BitLocker Auto Unlock task is NOT configured.
    echo.
    echo To fix this:
    echo 1. Run create-task-admin.bat as Administrator
    echo 2. Or use the "Create Startup Task" button in the app
    echo 3. Or create the task manually in Task Scheduler
    echo.
    echo ========================================
    echo          STATUS: NOT WORKING
    echo ========================================
    echo.
    echo BitLocker Manager will NOT start automatically at login.
    echo.
)

echo.
echo ========================================
echo Press any key to close...
echo ========================================
pause >nul