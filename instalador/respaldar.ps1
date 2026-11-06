<#
.SYNOPSIS
  Respaldo de la base "Comedor" (RNF-07): .bak en %ProgramData%\Comedor\Respaldos y borrado de los de más de 30 días.
.DESCRIPTION
  Usa System.Data.SqlClient (incluido en Windows PowerShell 5.1 / .NET Framework), así que no necesita
  sqlcmd ni módulos adicionales; basta con SQL Server Express instalado. El respaldo lo escribe el
  servicio de SQL Server, que debe poder escribir en la carpeta de respaldos (configurar.ps1 le concede
  el permiso).
  Código de salida: 0 correcto, 1 error (queda registrado en respaldos.log).
#>
[CmdletBinding()]
param(
    [string]$Servidor = '.\SQLEXPRESS',
    [string]$Base = 'Comedor',
    [int]$DiasConservar = 30
)

$ErrorActionPreference = 'Stop'

$carpetaDatos = Join-Path $env:ProgramData 'Comedor'
$carpeta = Join-Path $carpetaDatos 'Respaldos'
$bitacora = Join-Path $carpetaDatos 'respaldos.log'

function Registrar([string]$mensaje) {
    $linea = '{0:yyyy-MM-dd HH:mm:ss} {1}' -f (Get-Date), $mensaje
    try {
        Add-Content -LiteralPath $bitacora -Value $linea -Encoding UTF8
    }
    catch { }
    Write-Host $linea
}

try {
    New-Item -ItemType Directory -Force -Path $carpeta | Out-Null

    $archivo = Join-Path $carpeta ('Comedor-{0:yyyyMMdd-HHmm}.bak' -f (Get-Date))
    $cadena = "Server=$Servidor;Database=master;Integrated Security=True;TrustServerCertificate=True"
    $conexion = New-Object System.Data.SqlClient.SqlConnection $cadena
    try {
        $conexion.Open()
        $comando = $conexion.CreateCommand()
        $comando.CommandTimeout = 0
        # El nombre de la base va con QUOTENAME y la ruta como parámetro: nada se concatena sin escapar.
        $comando.CommandText = "DECLARE @sql nvarchar(max) = N'BACKUP DATABASE ' + QUOTENAME(@base) + N' TO DISK = @ruta WITH INIT, CHECKSUM'; EXEC sp_executesql @sql, N'@ruta nvarchar(260)', @ruta = @ruta;"
        [void]$comando.Parameters.AddWithValue('@base', $Base)
        [void]$comando.Parameters.AddWithValue('@ruta', $archivo)
        [void]$comando.ExecuteNonQuery()
    }
    finally {
        $conexion.Dispose()
    }

    if (-not (Test-Path -LiteralPath $archivo)) {
        throw "SQL Server no dejó el archivo $archivo."
    }
    Registrar "Respaldo creado: $archivo"

    $limite = (Get-Date).AddDays(-$DiasConservar)
    $viejos = Get-ChildItem -LiteralPath $carpeta -Filter 'Comedor-*.bak' -File | Where-Object { $_.LastWriteTime -lt $limite }
    foreach ($viejo in $viejos) {
        Remove-Item -LiteralPath $viejo.FullName -Force
        Registrar "Respaldo antiguo borrado: $($viejo.Name)"
    }
    exit 0
}
catch {
    Registrar ('ERROR en el respaldo: ' + $_.Exception.Message)
    exit 1
}
