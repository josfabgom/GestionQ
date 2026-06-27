; Script de Inno Setup para GestionQ - Cliente
[Setup]
AppId={{C6269CD4-6E35-4309-8F72-35AC0F1179F9}
AppName=GestionQ Cliente
AppVersion=1.0
AppPublisher=GestionQ
DefaultDirName=C:\GestionQ_Cliente
DefaultGroupName=GestionQ Cliente
DisableProgramGroupPage=yes
OutputDir=out
OutputBaseFilename=GestionQ_Cliente_Setup_1.0
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=out\GestionQ_Instalador\app_cliente\favicon.ico
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "out\GestionQ_Instalador\app_cliente\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "out\GestionQ_Instalador\app_cliente\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{userdesktop}\Iniciar GestionQ"; Filename: "{app}\Iniciar-GestionQ.vbs"; IconFilename: "{app}\favicon.ico"; Comment: "Iniciar Punto de Venta - GestionQ"
