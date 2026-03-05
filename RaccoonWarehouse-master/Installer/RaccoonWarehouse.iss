#define MyAppName "Raccoon Warehouse"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Raccoon Warehouse"
#define MyAppExeName "RaccoonWarehouse.exe"
#define MyAppSourceDir "C:\Users\obadaqafisheh\source\repos\ROCCOPOS\RaccoonWarehouse-master\bin\x64\Release\net8.0-windows\app.publish"
#define MyAppIcon "C:\Users\obadaqafisheh\source\repos\ROCCOPOS\RaccoonWarehouse-master\Logo newfinal.ico"
#define MyOutputDir "C:\Users\obadaqafisheh\source\repos\ROCCOPOS\RaccoonWarehouse-master\Installer\BuildOutput"

[Setup]
AppId={{8CC8C68A-6D46-4F0A-ABCB-1D0C0C8C6D11}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#MyOutputDir}
OutputBaseFilename=RaccoonWarehouse-Setup
SetupIconFile={#MyAppIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{#MyAppIcon}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{#MyAppIcon}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
