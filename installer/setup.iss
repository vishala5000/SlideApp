[Setup]
AppName=SlideApp
AppVersion=1.0
AppPublisher=SlideApp
DefaultDirName={autopf}\SlideApp
DefaultGroupName=SlideApp
OutputDir=Output
OutputBaseFilename=SlideAppSetup
Compression=lzma2
SolidCompression=yes
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\SlideApp.exe

[Files]
Source: "..\bin\Release\SlideApp.exe";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\bg.png";                  DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\bin\Release\font.ttf";                DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\app.ico";                               DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SlideApp";           Filename: "{app}\SlideApp.exe"; IconFilename: "{app}\SlideApp.exe"
Name: "{userdesktop}\SlideApp";     Filename: "{app}\SlideApp.exe"; IconFilename: "{app}\SlideApp.exe"

[Run]
Filename: "{app}\SlideApp.exe"; Description: "Launch SlideApp"; Flags: nowait postinstall skipifsilent
