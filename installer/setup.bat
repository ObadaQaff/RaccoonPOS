@echo off
setlocal EnableExtensions

set "APP_NAME=ROCCOPOS"
set "INSTALL_DIR=%LocalAppData%\Programs\ROCCOPOS"
set "ARCHIVE=%~dp0ROCCOPOS-Payload.zip"

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%ARCHIVE%' -DestinationPath '%INSTALL_DIR%' -Force"
if errorlevel 1 exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell = New-Object -ComObject WScript.Shell; $desktop = [IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'ROCCOPOS.lnk'); $shortcut = $shell.CreateShortcut($desktop); $shortcut.TargetPath = [IO.Path]::Combine('%INSTALL_DIR%', 'RaccoonWarehouse.exe'); $shortcut.WorkingDirectory = '%INSTALL_DIR%'; $shortcut.IconLocation = $shortcut.TargetPath; $shortcut.Save()"
if errorlevel 1 exit /b 1

start "" "%INSTALL_DIR%\RaccoonWarehouse.exe"

endlocal
