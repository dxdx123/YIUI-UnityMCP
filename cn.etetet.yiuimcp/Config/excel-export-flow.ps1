# Excel export flow: run ET/Excel/ExcelExporter codegen, then compile, then report result.
# Thin wrapper over menu-codegen-flow.ps1. Pure ASCII (PowerShell 5.1 / no-BOM).
param(
    [bool]$Force = $False,
    [bool]$NoWait = $True
)
& (Join-Path $PSScriptRoot 'menu-codegen-flow.ps1') -MenuPath 'ET/Excel/ExcelExporter' -Force $Force -NoWait $NoWait
exit $LASTEXITCODE
