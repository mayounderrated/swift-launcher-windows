#define AppVersion "2.0.0"
[Setup]
AppId={{C84566B8-823F-4C9E-AC18-ED68BC5609F7}
AppName=Swift
AppVersion={#AppVersion}
AppVerName=Swift {#AppVersion}
AppPublisher=Swift Launcher
DefaultDirName={localappdata}\Programs\Swift
DefaultGroupName=Swift
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
WizardStyle=modern
SetupIconFile=..\assets\Swift.ico
UninstallDisplayIcon={app}\Swift.exe
UninstallDisplayName=Swift Launcher
AppMutex=Local\SwiftLauncher.1
CloseApplications=yes
RestartApplications=no
Compression=lzma2
SolidCompression=yes
OutputDir=..\dist
OutputBaseFilename=Swift-Setup-2.0.0
VersionInfoVersion=2.0.0.0
VersionInfoDescription=Swift Launcher Setup

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start Swift when I sign in"; Flags: unchecked

[Files]
Source: "..\dist\Swift.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\GettingStarted.txt"; DestDir: "{app}"; DestName: "Readme.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\Swift"; Filename: "{app}\Swift.exe"; WorkingDir: "{app}"; AppUserModelID: "Swift.Launcher"
Name: "{autodesktop}\Swift"; Filename: "{app}\Swift.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\Swift"; Filename: "{app}\Swift.exe"; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\Swift.exe"; Description: "Open Swift"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Swift.exe"; Parameters: "--quit"; Flags: runhidden skipifdoesntexist; RunOnceId: "StopSwift"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := IsDotNetInstalled(net472, 0);
  if not Result then
    MsgBox('Swift needs Microsoft .NET Framework 4.7.2 or newer. Install it through Windows Update, then run Setup again.', mbError, MB_OK);
end;

