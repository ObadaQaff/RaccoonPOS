@echo off
setlocal
set "ZIP=RaccoonWarehouse-Customer-Package.zip"
set "TARGET=%TEMP%\RaccoonWarehouseCustomerPackage"

if exist "%TARGET%" rmdir /s /q "%TARGET%"
mkdir "%TARGET%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -Path '%~dp0%ZIP%' -DestinationPath '%TARGET%' -Force"
if errorlevel 1 (
  echo Failed to extract package.
  pause
  exit /b 1
)

call "%TARGET%\setup.cmd"
