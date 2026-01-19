@echo off
cd /d "%~dp0godot"
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)
"%~dp0tools\godot\Godot_v4.5.1-stable_mono_win64_console.exe" --path .
