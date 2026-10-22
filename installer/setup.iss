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
PrivilegesRequired=lowest
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\bin\Release\SlideApp.exe";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\SlideApp.pdb";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\*.dll";                   DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\bin\Release\*.config";                DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\SlideApp";           Filename: "{app}\SlideApp.exe"
Name: "{commondesktop}\SlideApp";   Filename: "{app}\SlideApp.exe"

[Run]
Filename: "{app}\SlideApp.exe"; Description: "Launch SlideApp"; Flags: nowait postinstall skipifsilent
