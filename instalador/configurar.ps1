<#
.SYNOPSIS
  Configura la instalación del Comedor: archivo de configuración local, firewall y respaldo diario (D-39, D-51).
.DESCRIPTION
  - Crea %ProgramData%\Comedor\appsettings.Local.json solo si NO existe (en una actualización no lo toca).
    Lleva claves aleatorias de 48 caracteres (generador criptográfico), la cadena de conexión a SQLEXPRESS,
    el administrador inicial, el puerto, la dirección del QR y Fuente:Tipo = Excel (D-51).
  - Deja ese archivo legible solo para SYSTEM y Administradores.
  - Crea la regla de firewall "Comedor" (TCP, solo la red local) y la tarea programada "Comedor - Respaldo"
    (diaria, 03:00, como SYSTEM). Ambas se recrean siempre, así que el script se puede repetir.
  Nunca escribe en pantalla ni en bitácoras la contraseña ni las claves.
  Con -ActualizarIp (solo con ese parámetro) cambia únicamente App:UrlPublica en el archivo existente,
  con la IP actual del equipo, y reinicia el servicio "Comedor" (D-53). El resto de la configuración
  (claves, administrador, etc.) queda intacto. Después hay que reimprimir el QR desde Panel > Puntos.
.PARAMETER ActualizarIp
  Rehace la dirección del QR (App:UrlPublica) con la IP actual del equipo, sin tocar nada más.
.PARAMETER Puerto
  Puerto TCP de la aplicación. En una actualización se usa el que ya está en el archivo de configuración.
.PARAMETER AdminUsuario
  Usuario del administrador inicial.
.PARAMETER AdminContrasena
  Contraseña del administrador inicial (mínimo 8 caracteres). Una línea de comandos puede verla otro
  proceso mientras corre; por eso el instalador usa -ArchivoContrasena.
.PARAMETER ArchivoContrasena
  Ruta de un archivo temporal con la contraseña en la primera línea. Se lee y se borra.
#>
[CmdletBinding()]
param(
    [int]$Puerto = 80,
    [string]$AdminUsuario = '',
    [string]$AdminContrasena = '',
    [string]$ArchivoContrasena = '',
    [switch]$ActualizarIp
)

$ErrorActionPreference = 'Stop'

# El archivo temporal con la contraseña se borra siempre, también si el script falla (código de salida 1).
trap {
    if ($ArchivoContrasena) { Remove-Item -LiteralPath $ArchivoContrasena -Force -ErrorAction SilentlyContinue }
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}

$carpetaDatos = Join-Path $env:ProgramData 'Comedor'
$archivoConfig = Join-Path $carpetaDatos 'appsettings.Local.json'
$carpetaRespaldos = Join-Path $carpetaDatos 'Respaldos'
$raizScripts = $PSScriptRoot

function ClaveAleatoria([int]$longitud) {
    $alfabeto = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $resultado = New-Object System.Text.StringBuilder
        $limite = 256 - (256 % $alfabeto.Length)  # descarta valores que sesgarían el reparto
        $byte = New-Object byte[] 1
        while ($resultado.Length -lt $longitud) {
            $rng.GetBytes($byte)
            if ($byte[0] -lt $limite) {
                [void]$resultado.Append($alfabeto[$byte[0] % $alfabeto.Length])
            }
        }
        return $resultado.ToString()
    }
    finally {
        $rng.Dispose()
    }
}

function IpLocal {
    # La interfaz activa con puerta de enlace predeterminada; si hay varias, la primera.
    $conPuerta = @(Get-NetIPConfiguration -ErrorAction SilentlyContinue |
            Where-Object { $_.IPv4DefaultGateway -ne $null -and $_.NetAdapter.Status -eq 'Up' -and $_.IPv4Address })
    if ($conPuerta.Count -gt 0) {
        return [string]$conPuerta[0].IPv4Address[0].IPAddress
    }
    Write-Warning 'No se encontró una interfaz con puerta de enlace; se usa la primera IP de la red que no sea local.'
    $otras = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' })
    if ($otras.Count -gt 0) {
        return [string]$otras[0].IPAddress
    }
    throw 'No hay conexión a la red. Conecta el equipo a la red de la empresa y vuelve a ejecutar configurar.ps1.'
}

function RestringirAAdministradores([string]$ruta, [bool]$esCarpeta = $false) {
    # Quita la herencia y deja solo SYSTEM y Administradores (por SID, sin depender del idioma de Windows).
    $sufijo = if ($esCarpeta) { "(OI)(CI)(F)" } else { "(F)" }
    & icacls.exe $ruta "/inheritance:r" "/grant:r" "*S-1-5-18:$sufijo" "*S-1-5-32-544:$sufijo" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudieron restringir los permisos de $ruta." }
}

New-Item -ItemType Directory -Force -Path $carpetaDatos | Out-Null

# --- 0. Solo con -ActualizarIp: cambia únicamente App:UrlPublica y reinicia el servicio (D-53) ---
if ($ActualizarIp) {
    if (-not (Test-Path -LiteralPath $archivoConfig) -or ((Get-Item -LiteralPath $archivoConfig).Length -eq 0)) {
        throw 'No hay configuración que actualizar. Ejecuta el instalador primero.'
    }
    $existente = Get-Content -LiteralPath $archivoConfig -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($existente.PSObject.Properties.Name -contains 'Comedor' -and $existente.Comedor.Puerto) {
        $Puerto = [int]$existente.Comedor.Puerto
    }
    $ip = IpLocal
    $urlPublica = if ($Puerto -eq 80) { "http://$ip" } else { "http://${ip}:$Puerto" }

    if ($existente.PSObject.Properties.Name -notcontains 'App') {
        $existente | Add-Member -NotePropertyName 'App' -NotePropertyValue ([pscustomobject]@{ UrlPublica = $urlPublica })
    }
    elseif ($existente.App.PSObject.Properties.Name -contains 'UrlPublica') {
        $existente.App.UrlPublica = $urlPublica
    }
    else {
        $existente.App | Add-Member -NotePropertyName 'UrlPublica' -NotePropertyValue $urlPublica
    }
    $json = $existente | ConvertTo-Json -Depth 10

    # Escritura segura: archivo temporal ya restringido a SYSTEM y Administradores, y luego reemplazo del original.
    $temporal = $archivoConfig + '.nuevo'
    try {
        [System.IO.File]::WriteAllText($temporal, '', (New-Object System.Text.UTF8Encoding($false)))
        RestringirAAdministradores $temporal
        [System.IO.File]::WriteAllText($temporal, $json, (New-Object System.Text.UTF8Encoding($false)))
        [System.IO.File]::Replace($temporal, $archivoConfig, $null)
        RestringirAAdministradores $archivoConfig
    }
    catch {
        Remove-Item -LiteralPath $temporal -Force -ErrorAction SilentlyContinue
        throw 'No se pudo actualizar la dirección. La configuración anterior quedó como estaba.'
    }
    $json = $null
    $existente = $null
    Write-Host "Dirección del QR actualizada a $urlPublica."

    $servicio = Get-Service -Name 'Comedor' -ErrorAction SilentlyContinue
    if ($null -eq $servicio) {
        Write-Warning 'El servicio "Comedor" no está instalado; no hay nada que reiniciar.'
    }
    else {
        try {
            Restart-Service -Name 'Comedor' -Force -ErrorAction Stop
            Write-Host 'Servicio "Comedor" reiniciado.'
        }
        catch {
            Write-Warning 'No se pudo reiniciar el servicio "Comedor". Reinícialo desde Servicios de Windows.'
        }
    }
    Write-Host 'Ahora reimprime el QR desde Panel > Puntos.'
    exit 0
}

# --- 1. Archivo de configuración (solo si no existe) ---
if ((Test-Path -LiteralPath $archivoConfig) -and ((Get-Item -LiteralPath $archivoConfig).Length -gt 0)) {
    Write-Host 'La configuración ya existe: no se modifica.'
    try {
        $existente = Get-Content -LiteralPath $archivoConfig -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($existente.PSObject.Properties.Name -contains 'Comedor' -and $existente.Comedor.Puerto) {
            $Puerto = [int]$existente.Comedor.Puerto   # el firewall sigue al puerto realmente configurado
        }
    }
    catch {
        Write-Warning 'No se pudo leer el puerto de la configuración existente; se usa el indicado.'
    }
}
else {
    if ($ArchivoContrasena) {
        try {
            $AdminContrasena = (Get-Content -LiteralPath $ArchivoContrasena -TotalCount 1 -Encoding UTF8)
        }
        finally {
            Remove-Item -LiteralPath $ArchivoContrasena -Force -ErrorAction SilentlyContinue
        }
    }
    if ([string]::IsNullOrWhiteSpace($AdminUsuario)) { throw 'Falta -AdminUsuario.' }
    if ([string]::IsNullOrEmpty($AdminContrasena) -or $AdminContrasena.Length -lt 8) {
        throw 'La contraseña del administrador debe tener al menos 8 caracteres.'
    }
    if ($Puerto -lt 1 -or $Puerto -gt 65535) { throw 'El puerto debe estar entre 1 y 65535.' }

    $ip = IpLocal
    $urlPublica = if ($Puerto -eq 80) { "http://$ip" } else { "http://${ip}:$Puerto" }

    $config = [ordered]@{
        ConnectionStrings = [ordered]@{ Comedor = 'Server=.\SQLEXPRESS;Database=Comedor;Trusted_Connection=True;TrustServerCertificate=True' }
        Jwt               = [ordered]@{ Clave = (ClaveAleatoria 48) }
        Qr                = [ordered]@{ ClaveFirma = (ClaveAleatoria 48) }
        Semilla           = [ordered]@{ Admin = [ordered]@{ Usuario = $AdminUsuario.Trim(); Contrasena = $AdminContrasena } }
        Comedor           = [ordered]@{ Puerto = $Puerto }
        App               = [ordered]@{ UrlPublica = $urlPublica }
        Fuente            = [ordered]@{ Tipo = 'Excel' }
    }
    $json = $config | ConvertTo-Json -Depth 5

    # Se crea vacío, se restringe y recién entonces se escribe el contenido: los secretos nunca están en un archivo abierto a todos.
    # Si algo falla a medias se borra el archivo parcial: uno vacío haría creer a la siguiente ejecución que ya hay configuración.
    try {
        [System.IO.File]::WriteAllText($archivoConfig, '', (New-Object System.Text.UTF8Encoding($false)))
        RestringirAAdministradores $archivoConfig
        [System.IO.File]::WriteAllText($archivoConfig, $json, (New-Object System.Text.UTF8Encoding($false)))
    }
    catch {
        Remove-Item -LiteralPath $archivoConfig -Force -ErrorAction SilentlyContinue
        throw 'No se pudo crear el archivo de configuración; se borró el archivo incompleto. Vuelve a ejecutar el instalador como administrador.'
    }
    $AdminContrasena = $null
    $json = $null
    Write-Host "Configuración creada en $archivoConfig (dirección del QR: $urlPublica)."
}

# --- 2. Carpeta de respaldos: solo SYSTEM, Administradores y el motor de SQL Server ---
New-Item -ItemType Directory -Force -Path $carpetaRespaldos | Out-Null
RestringirAAdministradores $carpetaRespaldos $true
& icacls.exe $carpetaRespaldos '/grant' 'NT SERVICE\MSSQL$SQLEXPRESS:(OI)(CI)(M)' | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Warning 'No se pudo dar permiso al motor de SQL Server en la carpeta de respaldos; el respaldo puede fallar.'
}

# --- 3. Firewall: solo la red local ---
Get-NetFirewallRule -DisplayName 'Comedor' -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName 'Comedor' -Direction Inbound -Action Allow -Protocol TCP `
    -LocalPort $Puerto -RemoteAddress LocalSubnet -Profile Any | Out-Null
Write-Host "Regla de firewall 'Comedor' creada (TCP $Puerto, solo la red local)."

# --- 4. Respaldo diario a las 03:00 como SYSTEM ---
$scriptRespaldo = Join-Path $raizScripts 'respaldar.ps1'
if (-not (Test-Path -LiteralPath $scriptRespaldo)) { throw "No se encontró $scriptRespaldo." }
$accion = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument ('-NoProfile -ExecutionPolicy Bypass -File "{0}"' -f $scriptRespaldo)
$disparador = New-ScheduledTaskTrigger -Daily -At '03:00'
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName 'Comedor - Respaldo' -Action $accion -Trigger $disparador -Principal $principal `
    -Description 'Respaldo diario de la base del Comedor (conserva 30 días)' -Force | Out-Null
Write-Host "Tarea programada 'Comedor - Respaldo' creada (diaria, 03:00)."
exit 0
