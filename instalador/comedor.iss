; Instalador del Comedor de personal (Inno Setup 6.3 o posterior). RF-35, RNF-11, D-39, D-51.
; Se compila con publicar.ps1 (iscc instalador\comedor.iss) después de dejar la API y el frontend en ..\publicacion.
; Orden de la instalación: (1) SQL Server 2022 Express si falta, (2) configurar.ps1, (3) servicio "Comedor", (4) accesos directos.

#define NombreApp "Comedor de personal"
#define VersionApp "2.0.0"
; Instalador web (bootstrapper) oficial de SQL Server 2022 Express. NO VERIFICADO desde este repositorio:
; confirmar el enlace y los argumentos silenciosos en una PC limpia antes de entregar (ver docs/aceptacion.md, "Instalación v2").
#define UrlSqlExpress "https://go.microsoft.com/fwlink/p/?linkid=2216019"

[Setup]
AppId={{48EE07C9-483A-494D-B6DD-D572A1F6431D}
AppName={#NombreApp}
AppVersion={#VersionApp}
AppPublisher=Comedor de personal
DefaultDirName={commonpf64}\Comedor
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=..\salida
OutputBaseFilename=InstalarComedor
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
PrivilegesRequired=admin
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=no
UninstallDisplayName={#NombreApp}
SetupLogging=yes

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; La aplicación autocontenida (API + wwwroot): no necesita .NET en la PC de la empresa.
Source: "..\publicacion\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Scripts de configuración, respaldo y verificación.
Source: "configurar.ps1"; DestDir: "{app}\instalador"; Flags: ignoreversion
Source: "respaldar.ps1"; DestDir: "{app}\instalador"; Flags: ignoreversion
Source: "verificar.ps1"; DestDir: "{app}\instalador"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\Pantalla del chef"; Filename: "{commonpf32}\Microsoft\Edge\Application\msedge.exe"; Parameters: "--kiosk {code:UrlLocal}/inicio --edge-kiosk-type=fullscreen"; Comment: "Pantalla de servicio del chef"
Name: "{autodesktop}\Panel del comedor"; Filename: "{commonpf32}\Microsoft\Edge\Application\msedge.exe"; Parameters: "{code:UrlLocal}/panel"; Comment: "Panel del administrador"

[Code]
const
  ServicioNombre = 'Comedor';

var
  PaginaAdmin: TInputQueryWizardPage;
  PaginaDescarga: TDownloadWizardPage;
  PuertoRevisado: Boolean;
  PuertoElegido: Integer;
  InstalacionConConfig: Boolean;

function ArchivoConfig: String;
begin
  Result := ExpandConstant('{commonappdata}\Comedor\appsettings.Local.json');
end;

function PowerShellExe: String;
begin
  Result := ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
end;

function SqlExpressInstalado: Boolean;
begin
  { Con la instalación en modo de 64 bits, HKLM es la vista de 64 bits, donde SQL Server registra sus instancias. }
  Result := RegValueExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', 'SQLEXPRESS');
end;

function PuertoOcupado(Puerto: Integer): Boolean;
var
  Codigo: Integer;
begin
  Result := False;
  if Exec(PowerShellExe,
    '-NoProfile -ExecutionPolicy Bypass -Command "if (Get-NetTCPConnection -State Listen -LocalPort ' + IntToStr(Puerto) +
    ' -ErrorAction SilentlyContinue) { exit 1 } else { exit 0 }"',
    '', SW_HIDE, ewWaitUntilTerminated, Codigo) then
    Result := (Codigo = 1);
end;

{ Lee "Puerto" de la configuración existente (actualización o reinstalación). 80 si no se puede leer. }
function PuertoDeConfigExistente: Integer;
var
  Texto: AnsiString;
  Contenido: String;
  Posicion, Valor: Integer;
begin
  Result := 80;
  if not LoadStringFromFile(ArchivoConfig, Texto) then
    Exit;
  Contenido := String(Texto);
  Posicion := Pos('"Puerto"', Contenido);
  if Posicion = 0 then
    Exit;
  Posicion := Posicion + Length('"Puerto"');
  while (Posicion <= Length(Contenido)) and ((Contenido[Posicion] = ':') or (Contenido[Posicion] = ' ')) do
    Posicion := Posicion + 1;
  Valor := 0;
  while (Posicion <= Length(Contenido)) and (Contenido[Posicion] >= '0') and (Contenido[Posicion] <= '9') do
  begin
    Valor := Valor * 10 + (Ord(Contenido[Posicion]) - Ord('0'));
    Posicion := Posicion + 1;
  end;
  if (Valor >= 1) and (Valor <= 65535) then
    Result := Valor;
end;

{ Dirección local para los accesos directos; sin ":puerto" cuando es el 80 (D-43). }
function UrlLocal(Param: String): String;
begin
  if PuertoElegido = 80 then
    Result := 'http://localhost'
  else
    Result := 'http://localhost:' + IntToStr(PuertoElegido);
end;

function UsuarioValido(const Usuario: String): Boolean;
var
  I: Integer;
  C: Char;
begin
  Result := Length(Usuario) > 0;
  for I := 1 to Length(Usuario) do
  begin
    C := Usuario[I];
    if not (((C >= 'a') and (C <= 'z')) or ((C >= 'A') and (C <= 'Z')) or ((C >= '0') and (C <= '9')) or
      (C = '.') or (C = '_') or (C = '-') or (C = '@')) then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

procedure InitializeWizard;
begin
  InstalacionConConfig := FileExists(ArchivoConfig);
  PuertoElegido := 80;
  if InstalacionConConfig then
    PuertoElegido := PuertoDeConfigExistente;

  PaginaAdmin := CreateInputQueryPage(wpWelcome, 'Administrador y puerto',
    'Datos del administrador inicial y puerto del sistema',
    'El administrador entra al panel del comedor. La contraseña debe tener al menos 8 caracteres.');
  PaginaAdmin.Add('Usuario del administrador (letras, números, punto, guion):', False);
  PaginaAdmin.Add('Contraseña (mínimo 8 caracteres):', True);
  PaginaAdmin.Add('Repite la contraseña:', True);
  PaginaAdmin.Add('Puerto (80 recomendado):', False);
  PaginaAdmin.Values[3] := '80';

  PaginaDescarga := CreateDownloadPage(SetupMessage(msgWizardPreparing), 'Descargando SQL Server 2022 Express…', nil);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  { En una actualización la configuración ya existe y no se toca: no se piden otra vez los datos. }
  Result := (PageID = PaginaAdmin.ID) and InstalacionConConfig;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = PaginaAdmin.ID) and (not PuertoRevisado) then
  begin
    PuertoRevisado := True;
    if PuertoOcupado(80) then
    begin
      PaginaAdmin.Values[3] := '8080';
      MsgBox('El puerto 80 ya está en uso en este equipo. Se propone el puerto 8080; los celulares usarán una dirección con ":8080".',
        mbInformation, MB_OK);
    end;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Puerto: Integer;
  Proximo: Boolean;
begin
  Result := True;

  if CurPageID = PaginaAdmin.ID then
  begin
    Result := False;
    if not UsuarioValido(Trim(PaginaAdmin.Values[0])) then
      MsgBox('Escribe el usuario del administrador usando solo letras, números, punto, guion, guion bajo o arroba.', mbError, MB_OK)
    else if Length(PaginaAdmin.Values[1]) < 8 then
      MsgBox('La contraseña debe tener al menos 8 caracteres.', mbError, MB_OK)
    else if PaginaAdmin.Values[1] <> PaginaAdmin.Values[2] then
      MsgBox('Las dos contraseñas no coinciden.', mbError, MB_OK)
    else
    begin
      Puerto := StrToIntDef(Trim(PaginaAdmin.Values[3]), 0);
      if (Puerto < 1) or (Puerto > 65535) then
        MsgBox('El puerto debe ser un número entre 1 y 65535.', mbError, MB_OK)
      else if PuertoOcupado(Puerto) then
      begin
        Proximo := (Puerto <> 8080) and (not PuertoOcupado(8080));
        if Proximo then
        begin
          PaginaAdmin.Values[3] := '8080';
          MsgBox('El puerto ' + IntToStr(Puerto) + ' ya está en uso en este equipo. Se propone el puerto 8080; revisa y pulsa Siguiente.',
            mbInformation, MB_OK);
        end
        else
          MsgBox('El puerto ' + IntToStr(Puerto) + ' ya está en uso en este equipo. Escribe otro puerto.', mbError, MB_OK);
      end
      else
      begin
        PuertoElegido := Puerto;
        Result := True;
      end;
    end;
  end
  else if (CurPageID = wpReady) and (not SqlExpressInstalado) then
  begin
    { Descarga de SQL Server 2022 Express antes de copiar archivos. Si falla, no se instala nada. }
    PaginaDescarga.Clear;
    PaginaDescarga.Add('{#UrlSqlExpress}', 'SqlExpressSetup.exe', '');
    PaginaDescarga.Show;
    try
      try
        PaginaDescarga.Download;
      except
        if PaginaDescarga.AbortedByUser then
          Log('Descarga cancelada por el usuario.')
        else
          SuppressibleMsgBox('No se pudo descargar SQL Server Express: ' + AddPeriod(GetExceptionMessage) +
            #13#10 + 'Revisa la conexión a internet y vuelve a ejecutar el instalador.', mbCriticalError, MB_OK, IDOK);
        Result := False;
      end;
    finally
      PaginaDescarga.Hide;
    end;
  end;
end;

{ Antes de copiar archivos, detiene el servicio si es una actualización (el .exe en uso no se puede reemplazar). }
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Codigo: Integer;
begin
  Result := '';
  Exec(ExpandConstant('{sys}\net.exe'), 'stop ' + ServicioNombre, '', SW_HIDE, ewWaitUntilTerminated, Codigo);
end;

function InstalarSqlExpress: Boolean;
var
  Codigo: Integer;
  Instalador: String;
begin
  Result := True;
  if SqlExpressInstalado then
    Exit;
  Instalador := ExpandConstant('{tmp}\SqlExpressSetup.exe');
  WizardForm.StatusLabel.Caption := 'Instalando SQL Server 2022 Express (puede tardar varios minutos)…';
  { El instalador de SQL Server debe estar firmado por Microsoft antes de ejecutarlo como administrador. }
  if not Exec(PowerShellExe,
    '-NoProfile -ExecutionPolicy Bypass -Command "$f = Get-AuthenticodeSignature -LiteralPath ''' + Instalador +
    '''; if ($f.Status -eq ''Valid'' -and $f.SignerCertificate.Subject -like ''*Microsoft Corporation*'') { exit 0 } else { exit 1 }"',
    '', SW_HIDE, ewWaitUntilTerminated, Codigo) or (Codigo <> 0) then
  begin
    MsgBox('El archivo descargado de SQL Server Express no tiene una firma válida de Microsoft. No se instalará.', mbCriticalError, MB_OK);
    Result := False;
    Exit;
  end;
  if (not Exec(Instalador,
    '/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSYSADMINACCOUNTS="NT AUTHORITY\SYSTEM" "BUILTIN\Administrators" /IACCEPTSQLSERVERLICENSETERMS',
    '', SW_SHOW, ewWaitUntilTerminated, Codigo)) or ((Codigo <> 0) and (Codigo <> 3010)) or (not SqlExpressInstalado) then
  begin
    MsgBox('No se pudo instalar SQL Server Express (código ' + IntToStr(Codigo) + '). Revisa el registro de la instalación de SQL Server en C:\Program Files\Microsoft SQL Server\160\Setup Bootstrap\Log.',
      mbCriticalError, MB_OK);
    Result := False;
  end;
end;

function EjecutarConfigurar: Boolean;
var
  Codigo: Integer;
  ArchivoPwd, Parametros: String;
  Lineas: TArrayOfString;
begin
  Parametros := '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\instalador\configurar.ps1') + '" -Puerto ' + IntToStr(PuertoElegido);
  if not InstalacionConConfig then
  begin
    { La contraseña viaja en un archivo temporal que configurar.ps1 lee y borra: no queda en una línea de comandos. }
    ArchivoPwd := ExpandConstant('{tmp}\admin.pwd');
    SetArrayLength(Lineas, 1);
    Lineas[0] := PaginaAdmin.Values[1];
    SaveStringsToUTF8File(ArchivoPwd, Lineas, False);
    Parametros := Parametros + ' -AdminUsuario "' + Trim(PaginaAdmin.Values[0]) + '" -ArchivoContrasena "' + ArchivoPwd + '"';
  end;
  WizardForm.StatusLabel.Caption := 'Configurando el sistema…';
  Result := Exec(PowerShellExe, Parametros, '', SW_HIDE, ewWaitUntilTerminated, Codigo) and (Codigo = 0);
  if ArchivoPwd <> '' then
    DeleteFile(ArchivoPwd);
  if not Result then
    MsgBox('No se pudo configurar el sistema (código ' + IntToStr(Codigo) + ').', mbCriticalError, MB_OK);
end;

function CrearYArrancarServicio: Boolean;
var
  Codigo: Integer;
  Exe, Sc: String;
begin
  Exe := ExpandConstant('{app}\Comedor.Api.exe');
  Sc := ExpandConstant('{sys}\sc.exe');
  WizardForm.StatusLabel.Caption := 'Creando el servicio de Windows…';
  { Si ya existe (actualización), solo se actualiza la ruta. }
  if Exec(Sc, 'query ' + ServicioNombre, '', SW_HIDE, ewWaitUntilTerminated, Codigo) and (Codigo = 0) then
    Exec(Sc, 'config ' + ServicioNombre + ' binPath= "\"' + Exe + '\"" start= auto obj= LocalSystem', '', SW_HIDE, ewWaitUntilTerminated, Codigo)
  else
    Exec(Sc, 'create ' + ServicioNombre + ' binPath= "\"' + Exe + '\"" start= auto obj= LocalSystem DisplayName= "Comedor de personal"',
      '', SW_HIDE, ewWaitUntilTerminated, Codigo);
  Result := (Codigo = 0);
  if Result then
  begin
    Exec(Sc, 'description ' + ServicioNombre + ' "Control de acceso al comedor de personal"', '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    { Reinicio ante fallas: 5 s, 5 s y 30 s; el contador se reinicia tras un día sin fallas. }
    Exec(Sc, 'failure ' + ServicioNombre + ' reset= 86400 actions= restart/5000/restart/5000/restart/30000', '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    Result := Exec(ExpandConstant('{sys}\net.exe'), 'start ' + ServicioNombre, '', SW_HIDE, ewWaitUntilTerminated, Codigo) and (Codigo = 0);
  end;
  if not Result then
    MsgBox('No se pudo crear o arrancar el servicio "Comedor". Revisa los registros de Windows (Visor de eventos) o ejecuta instalador\verificar.ps1 en la carpeta de instalación.',
      mbCriticalError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if InstalarSqlExpress then
      if EjecutarConfigurar then
        CrearYArrancarServicio;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Codigo: Integer;
begin
  { Quita servicio, regla de firewall y tarea de respaldo. Conserva la base de datos y %ProgramData%\Comedor. }
  if CurUninstallStep = usUninstall then
  begin
    Exec(ExpandConstant('{sys}\net.exe'), 'stop ' + ServicioNombre, '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    Exec(ExpandConstant('{sys}\sc.exe'), 'delete ' + ServicioNombre, '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    Exec(ExpandConstant('{sys}\netsh.exe'), 'advfirewall firewall delete rule name="Comedor"', '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    Exec(ExpandConstant('{sys}\schtasks.exe'), '/Delete /TN "Comedor - Respaldo" /F', '', SW_HIDE, ewWaitUntilTerminated, Codigo);
  end;
end;
