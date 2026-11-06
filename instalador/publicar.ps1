<#
.SYNOPSIS
  Genera salida\InstalarComedor.exe: compila el frontend, publica la API autocontenida y empaqueta con Inno Setup 6.
.DESCRIPTION
  Se ejecuta en la PC de desarrollo (necesita Node.js, .NET SDK 8 e Inno Setup 6 con iscc.exe), desde cualquier carpeta.
  La PC de la empresa no necesita .NET (RNF-11).
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$publicacion = Join-Path $raiz 'publicacion'
$salida = Join-Path $raiz 'salida'

function Ejecutar([string]$que, [scriptblock]$bloque) {
    Write-Host "==> $que" -ForegroundColor Cyan
    & $bloque
    if ($LASTEXITCODE -ne 0) { throw "Falló: $que (código $LASTEXITCODE)." }
}

# iscc primero: si falta, no se pierde tiempo compilando.
$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    # Inno Setup 6 o posterior: la carpeta lleva el número de versión ("Inno Setup 6", "Inno Setup 7"...).
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        $candidato = Get-ChildItem -LiteralPath $base -Directory -Filter 'Inno Setup *' -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'ISCC.exe' } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1
        if ($candidato) { $iscc = $candidato; break }
    }
}
if (-not $iscc) {
    throw 'No se encontró iscc.exe (Inno Setup 6 o posterior). Instálalo desde https://jrsoftware.org/isdl.php y vuelve a ejecutar publicar.ps1.'
}

if (Test-Path -LiteralPath $publicacion) { Remove-Item -LiteralPath $publicacion -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publicacion, $salida | Out-Null

Push-Location (Join-Path $raiz 'frontend\comedor-web')
try {
    Ejecutar 'Compilar el frontend' { npx ng build }
}
finally {
    Pop-Location
}
$dist = Join-Path $raiz 'frontend\comedor-web\dist\comedor-web\browser'
if (-not (Test-Path -LiteralPath $dist)) { throw "No existe $dist después de compilar el frontend." }

Ejecutar 'Publicar la API (autocontenida, win-x64)' {
    dotnet publish (Join-Path $raiz 'backend\src\Comedor.Api') -c Release -r win-x64 --self-contained true -o $publicacion
}

$wwwroot = Join-Path $publicacion 'wwwroot'
if (Test-Path -LiteralPath $wwwroot) { Remove-Item -LiteralPath $wwwroot -Recurse -Force }
Copy-Item -LiteralPath $dist -Destination $wwwroot -Recurse

Ejecutar 'Empaquetar con Inno Setup' { & $iscc (Join-Path $PSScriptRoot 'comedor.iss') }

$exe = Join-Path $salida 'InstalarComedor.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "No se generó $exe." }
Write-Host "Listo: $exe" -ForegroundColor Green
