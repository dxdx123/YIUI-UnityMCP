# Generic codegen-menu flow: ExecuteMenu(<MenuPath>) -> TriggerCompile -> GetCompileResult
# For ET codegen menus that generate .cs/config then need a recompile, e.g.
#   -MenuPath "ET/Proto/Proto2CS"   (see proto2cs-flow.ps1)
#   -MenuPath "ET/Excel/ExcelExporter" (see excel-export-flow.ps1)
# Drives the UTO /batch layer so the domain reload after codegen is recovered automatically.
# NOTE: keep this script pure ASCII (PowerShell 5.1 reads no-BOM .ps1 as system ANSI/GBK).
param(
    [Parameter(Mandatory = $true)]
    [string]$MenuPath,
    [bool]$Force = $False,
    [bool]$NoWait = $True
)

$ErrorActionPreference = "Stop"
$UTO_PATH = Join-Path $PSScriptRoot "..\UTO"

# Resolve Unity MCP port from .port (fallback 3212); UTO HTTP port = Unity port + 1
$UNITY_MCP_PORT = 3212
$portFile = Join-Path $UTO_PATH ".port"
if (Test-Path $portFile) {
    $UNITY_MCP_PORT = [int](Get-Content $portFile -Raw).Trim()
}
$UTO_HTTP_PORT = $UNITY_MCP_PORT + 1

Write-Host "========================================"
Write-Host "ET codegen flow: $MenuPath (Force: $Force)"
Write-Host "Unity MCP port: $UNITY_MCP_PORT / UTO HTTP port: $UTO_HTTP_PORT"
Write-Host "========================================"

# Clean stale UTO process on the HTTP port
try {
    $conn = Get-NetTCPConnection -LocalPort $UTO_HTTP_PORT -ErrorAction SilentlyContinue
    if ($conn) {
        Write-Host "Cleaning stale UTO process..." -ForegroundColor Yellow
        Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
} catch {}

# Start UTO HTTP server (reads .port itself)
Write-Host "Starting UTO HTTP server..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "node"
$psi.Arguments = "build/index.js --http"
$psi.WorkingDirectory = $UTO_PATH
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$utoProcess = [System.Diagnostics.Process]::Start($psi)

# Wait for UTO ready
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:$UTO_HTTP_PORT/health" -TimeoutSec 2
        if ($health -and $health.status -eq "ok") {
            $ready = $true
            Write-Host "UTO ready" -ForegroundColor Green
            break
        }
    } catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $ready) {
    Write-Host "UTO start timeout" -ForegroundColor Red
    if (-not $NoWait) { Write-Host "Press any key..."; [Console]::ReadKey($true) | Out-Null; Stop-Process -Id $PID }
    exit 1
}

# Batch: stop play (compile needs edit mode), run codegen menu, compile, read result
$tools = @(
    @{ name = "StopPlayMode";     arguments = @{} },
    @{ name = "ExecuteMenu";      arguments = @{ menuPath = $MenuPath } },
    @{ name = "TriggerCompile";   arguments = @{ Force = $Force } },
    @{ name = "GetCompileResult"; arguments = @{} }
)
$body = @{ tools = $tools } | ConvertTo-Json -Depth 10 -Compress

Write-Host ""
Write-Host "Steps: 1) StopPlayMode  2) ExecuteMenu $MenuPath  3) TriggerCompile  4) GetCompileResult"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:$UTO_HTTP_PORT/batch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 600
} catch {
    Write-Host "Call failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($utoProcess -and -not $utoProcess.HasExited) { try { $utoProcess.Kill(); $utoProcess.WaitForExit(3000) } catch {} }
    if (-not $NoWait) { Write-Host "Press any key..."; [Console]::ReadKey($true) | Out-Null; Stop-Process -Id $PID }
    exit 1
}

Write-Host "========================================"
if ($response.success) {
    Write-Host "Flow OK!  total: $($response.totalDurationSeconds)s" -ForegroundColor Green
    foreach ($result in $response.results) {
        $d = ""
        if ($null -ne $result.duration -and "$($result.duration)" -ne "") { $d = " ($($result.duration) ms)" }
        Write-Host "[OK] $($result.tool)$d" -ForegroundColor Green
        if ($result.result) { Write-Host "  $($result.result)" -ForegroundColor Gray }
    }
} else {
    Write-Host "Flow FAILED!  total: $($response.totalDurationSeconds)s" -ForegroundColor Red
    Write-Host "Failed at step: $($response.failedAt + 1)"
    Write-Host "Error: $($response.error)"
    foreach ($result in $response.results) {
        if ($result.success) {
            Write-Host "[OK] $($result.tool)" -ForegroundColor Green
        } else {
            Write-Host "[X] $($result.tool)" -ForegroundColor Red
            if ($result.error) { Write-Host "  Error: $($result.error)" -ForegroundColor Red }
        }
    }
}
Write-Host "========================================"

if ($utoProcess -and -not $utoProcess.HasExited) { try { $utoProcess.Kill(); $utoProcess.WaitForExit(3000) } catch {} }
if (-not $NoWait) { Write-Host "Press any key..."; [Console]::ReadKey($true) | Out-Null; Stop-Process -Id $PID }
if ($response.success) { exit 0 } else { exit 1 }
