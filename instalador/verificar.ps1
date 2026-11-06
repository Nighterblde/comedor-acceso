<#
.SYNOPSIS
  Prueba de humo de la instalación del Comedor (RF-35). Muestra cada comprobación en verde o rojo.
.DESCRIPTION
  Comprueba: servicio "Comedor" en ejecución, GET /api/salud con 200, regla de firewall "Comedor",
  tarea programada "Comedor - Respaldo" y que el host de App:UrlPublica sea una IP actual del equipo.
  Funciona también en un equipo sin nada instalado (todo en rojo). Termina con código 0 si todo está
  en verde y con 1 si algo está en rojo. Conviene ejecutarlo como administrador (el archivo de
  configuración solo lo leen SYSTEM y los administradores).
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Continue'

$carpetaDatos = Join-Path $env:ProgramData 'Comedor'
$archivoConfig = Join-Path $carpetaDatos 'appsettings.Local.json'
$script:fallas = 0

function Mostrar([bool]$ok, [string]$texto, [string]$detalle = '') {
    if ($ok) {
        Write-Host ('[ OK ] ' + $texto) -ForegroundColor Green
    }
    else {
        Write-Host ('[FALLA] ' + $texto) -ForegroundColor Red
        $script:fallas++
    }
    if ($detalle) { Write-Host ('        ' + $detalle) }
}

function Propiedad($objeto, [string]$nombre) {
    if ($null -ne $objeto -and $objeto.PSObject.Properties.Name -contains $nombre) { return $objeto.$nombre }
    return $null
}

# Configuración de la instalación (puerto y dirección del QR). Nunca se imprimen claves ni contraseñas.
$puerto = 80
$urlPublica = $null
$configLegible = $false
if (Test-Path -LiteralPath $archivoConfig) {
    try {
        $config = Get-Content -LiteralPath $archivoConfig -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json
        $configLegible = $true
        $p = Propiedad (Propiedad $config 'Comedor') 'Puerto'
        if ($p) { $puerto = [int]$p }
        $urlPublica = [string](Propiedad (Propiedad $config 'App') 'UrlPublica')
    }
    catch {
        $configLegible = $false
    }
}

# 1. Servicio
$servicio = Get-Service -Name 'Comedor' -ErrorAction SilentlyContinue
if ($null -eq $servicio) {
    Mostrar $false 'Servicio "Comedor" en ejecución' 'No está instalado.'
}
else {
    Mostrar ($servicio.Status -eq 'Running') 'Servicio "Comedor" en ejecución' ('Estado: ' + $servicio.Status)
}

# 2. Salud de la API
$urlSalud = "http://localhost:$puerto/api/salud"
$codigo = $null
try {
    $respuesta = Invoke-WebRequest -Uri $urlSalud -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    $codigo = [int]$respuesta.StatusCode
}
catch {
    if ($_.Exception.Response) { $codigo = [int]$_.Exception.Response.StatusCode }
}
if ($codigo -eq 200) {
    Mostrar $true "El sistema responde ($urlSalud)"
}
else {
    $motivo = if ($null -eq $codigo) { 'No respondió.' } else { "Respondió $codigo." }
    Mostrar $false "El sistema responde ($urlSalud)" $motivo
}

# 3. Firewall
$regla = $null
try { $regla = Get-NetFirewallRule -DisplayName 'Comedor' -ErrorAction Stop } catch { $regla = $null }
Mostrar ($null -ne $regla) 'Regla de firewall "Comedor"'

# 4. Respaldo programado
$tarea = $null
try { $tarea = Get-ScheduledTask -TaskName 'Comedor - Respaldo' -ErrorAction Stop } catch { $tarea = $null }
Mostrar ($null -ne $tarea) 'Tarea programada "Comedor - Respaldo"'

# 5. La dirección del QR coincide con una IP actual del equipo
$ipsActuales = @()
try {
    $ipsActuales = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction Stop |
            Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
            ForEach-Object { $_.IPAddress })
}
catch {
    $ipsActuales = @()
}

if (-not $configLegible -or [string]::IsNullOrWhiteSpace($urlPublica)) {
    Mostrar $false 'La dirección del QR (App:UrlPublica) es una IP actual del equipo' 'No se pudo leer la configuración. Si ya está instalado, ejecuta este archivo como administrador.'
}
else {
    $hostQr = $null
    try { $hostQr = ([Uri]$urlPublica).Host } catch { $hostQr = $null }
    if ($hostQr -and ($ipsActuales -contains $hostQr)) {
        Mostrar $true "La dirección del QR ($hostQr) es una IP actual del equipo"
    }
    else {
        Mostrar $false 'La dirección del QR (App:UrlPublica) es una IP actual del equipo'
        Write-Host '        La IP cambió: reimprime el QR desde Panel > Puntos' -ForegroundColor Red
        if ($ipsActuales.Count -gt 0) {
            Write-Host ('        IP nueva: ' + ($ipsActuales -join ', ')) -ForegroundColor Yellow
        }
        else {
            Write-Host '        No se encontró ninguna IP en la red.' -ForegroundColor Yellow
        }
    }
}

Write-Host ''
if ($script:fallas -eq 0) {
    Write-Host 'Todo en verde.' -ForegroundColor Green
    exit 0
}
Write-Host ("Hay $($script:fallas) comprobación(es) en rojo.") -ForegroundColor Red
exit 1
