; Script de Inno Setup para GestionQ - Punto de Venta en Red

[Setup]
AppId={{9F7B910A-3755-4927-B67B-60C1E55E1C38}
AppName=GestionQ
AppVersion=1.0
AppPublisher=GestionQ
AppPublisherURL=http://localhost:5144
AppSupportURL=http://localhost:5144
AppUpdatesURL=http://localhost:5144
DefaultDirName=C:\GestionQ
DefaultGroupName=GestionQ
DisableProgramGroupPage=yes
OutputDir=out
OutputBaseFilename=GestionQ_Setup_1.0
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
CreateUninstallRegKey=yes
UpdateUninstallLogAppName=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; Archivos de la aplicación recopilados en dotnet publish
Source: "out\GestionQ_Instalador\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Instalador de SQL Server Express local (se conserva para configuración manual)
Source: "out\SQL2022-SSEI-Expr.exe"; DestDir: "{app}"; Flags: ignoreversion
; Script de PowerShell para configuracion de DB e idiomas (se conserva en C:\GestionQ)
Source: "out\GestionQ_Instalador\configurar_db.ps1"; DestDir: "{app}"; Flags: ignoreversion
; Script de lote por si el usuario desea correr la configuracion manualmente
Source: "out\GestionQ_Instalador\configurar_sistema.bat"; DestDir: "{app}"; Flags: ignoreversion
; Script SQL de la estructura de base de datos para instalacion manual
Source: "out\GestionQ_Instalador\GestionQ_Schema.sql"; DestDir: "{app}"; Flags: ignoreversion
; Script SQL de los datos basicos para la base de datos
Source: "out\GestionQ_Instalador\GestionQ_Datos_Basicos.sql"; DestDir: "{app}"; Flags: ignoreversion
; Script de lote para registrar servicio y abrir puertos de forma manual
Source: "out\GestionQ_Instalador\registrar_servicio.bat"; DestDir: "{app}"; Flags: ignoreversion




[Icons]
; Acceso directo en el Escritorio
Name: "{userdesktop}\Punto de Venta - GestionQ"; Filename: "http://localhost:5144"; IconFilename: "{app}\wwwroot\favicon.ico"; Comment: "Iniciar Punto de Venta - GestionQ"

[Run]
; 1. Ejecutar script de PowerShell que realiza todo el proceso de instalación y configuración de base de datos, servicio y firewall.
; Toda la salida (logs) se guarda internamente en C:\GestionQ\install.log
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\configurar_db.ps1"" ""{app}"" ""{app}\SQL2022-SSEI-Expr.exe"""; StatusMsg: "Configurando base de datos e instalando servicios (esto abrirá una ventana de PowerShell)..."

[UninstallRun]
; Acciones ejecutadas antes de eliminar los archivos durante la desinstalación
; 1. Detener el Servicio
Filename: "{sys}\net.exe"; Parameters: "stop GestionQ_Web_Service"; Flags: runhidden; RunOnceId: "StopService"
; 2. Eliminar el Servicio
Filename: "{sys}\sc.exe"; Parameters: "delete GestionQ_Web_Service"; Flags: runhidden; RunOnceId: "DeleteService"
; 3. Remover las reglas del Firewall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""GestionQ Web App (TCP 5144)"""; Flags: runhidden; RunOnceId: "DeleteFirewallRule1"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""SQL Server (TCP 1433)"""; Flags: runhidden; RunOnceId: "DeleteFirewallRule2"
