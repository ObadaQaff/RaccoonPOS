@echo off
setlocal
title Raccoon Warehouse Setup
echo Installing Raccoon Warehouse...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
if errorlevel 1 (
  echo.
  echo Installation failed.
  pause
  exit /b 1
)
echo.
echo Installation completed successfully.
pause
