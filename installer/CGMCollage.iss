#define AppName "CGM Collage"
#ifndef RuntimeIdentifier
#define RuntimeIdentifier "win-x64"
#endif
#ifndef AppArchitecture
#define AppArchitecture "x64"
#endif
#ifndef ArchitectureExpression
#define ArchitectureExpression "x64compatible and not arm64"
#endif

#define AppVersion "3.1.0"
#define AppPublisher "CGM"
#define AppExeName "CGM Collage.exe"

[Setup]
AppId={{9EC61CA1-8591-4E47-A6B9-6C7B4CF02C34}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\CGM Collage
DefaultGroupName=CGM Collage
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed={#ArchitectureExpression}
ArchitecturesInstallIn64BitMode={#ArchitectureExpression}
OutputDir=..\artifacts
OutputBaseFilename=CGM-Collage-Setup-{#AppArchitecture}
SetupIconFile=..\src\CGMCollage\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=CGM Collage Windows Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\{#RuntimeIdentifier}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\CGM Collage"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\CGM Collage"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Classes\.cgmcollage"; ValueType: string; ValueData: "CGMCollage.Project"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\CGMCollage.Project"; ValueType: string; ValueData: "CGM Collage Project"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\CGMCollage.Project\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExeName},0"
Root: HKA; Subkey: "Software\Classes\CGMCollage.Project\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch CGM Collage"; Flags: nowait postinstall skipifsilent
