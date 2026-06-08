# cursor-setup.ps1
# Creates .cursor/mcp.json and .cursor/rules/yiuimcp.mdc in the Unity project root.
# Run once per Unity project to enable Cursor MCP + rules for YIUI automation.
#
# Usage (from Unity project root):
#   powershell -ExecutionPolicy Bypass -File "Packages\cn.etetet.yiuimcp\Config\cursor-setup.ps1"
# Or with explicit project root:
#   powershell -ExecutionPolicy Bypass -File "..." -ProjectRoot "D:\MyProject"

param([string]$ProjectRoot = "")

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Auto-detect Unity project root: go 3 levels up (Config -> package -> Packages -> project root)
if ($ProjectRoot -eq "") {
    $ProjectRoot = (Resolve-Path (Join-Path $scriptDir "../../..")).Path
}

if (-not (Test-Path (Join-Path $ProjectRoot "Assets"))) {
    Write-Host "ERROR: Assets/ not found at: $ProjectRoot"
    Write-Host "Run from Unity project root, or pass -ProjectRoot <path>"
    exit 1
}

Write-Host "Unity project root: $ProjectRoot"

# UTO build entry point (forward slashes work on Windows in Node)
$utoPath = (Join-Path $ProjectRoot "Packages/cn.etetet.yiuimcp/UTO/build/index.js").Replace("\", "/")

if (-not (Test-Path ($utoPath.Replace("/", "\")))) {
    Write-Host "WARNING: UTO build not found at: $utoPath"
    Write-Host "Run 'npm install && npm run build' in Packages/cn.etetet.yiuimcp/UTO first."
}

$cursorDir = Join-Path $ProjectRoot ".cursor"
$rulesDir  = Join-Path $cursorDir "rules"
New-Item -ItemType Directory -Force $cursorDir | Out-Null
New-Item -ItemType Directory -Force $rulesDir  | Out-Null

# ---- .cursor/mcp.json ----
$mcpPath = Join-Path $cursorDir "mcp.json"

if (Test-Path $mcpPath) {
    $raw = Get-Content $mcpPath -Raw -Encoding UTF8
    if ($raw -match '"yiuimcp"') {
        Write-Host "SKIP: $mcpPath already contains yiuimcp entry (remove it manually to regenerate)"
    } elseif ($raw -match '"mcpServers"\s*:\s*\{') {
        # Inject into existing mcpServers block
        $entry = '"yiuimcp": { "command": "node", "args": ["' + $utoPath + '"] },'
        $raw = $raw -replace '("mcpServers"\s*:\s*\{)', "`$1`n    $entry"
        [System.IO.File]::WriteAllText($mcpPath, $raw, [System.Text.Encoding]::UTF8)
        Write-Host "Updated: $mcpPath  (yiuimcp injected into existing mcpServers)"
    } else {
        Write-Host "WARNING: $mcpPath exists but has no mcpServers block."
        Write-Host "Add this manually:"
        Write-Host '  "mcpServers": { "yiuimcp": { "command": "node", "args": ["' + $utoPath + '"] } }'
    }
} else {
    $mcpJson = @"
{
  "mcpServers": {
    "yiuimcp": {
      "command": "node",
      "args": ["$utoPath"]
    }
  }
}
"@
    [System.IO.File]::WriteAllText($mcpPath, $mcpJson, [System.Text.Encoding]::UTF8)
    Write-Host "Created: $mcpPath"
}

# ---- .cursor/rules/yiuimcp.mdc ----
$mdcSrc = Join-Path $scriptDir "cursor\yiuimcp.mdc"
$mdcDst = Join-Path $rulesDir "yiuimcp.mdc"

if (-not (Test-Path $mdcSrc)) {
    Write-Host "ERROR: rule template not found: $mdcSrc"
    exit 1
}
Copy-Item $mdcSrc $mdcDst -Force
Write-Host "Copied:  $mdcDst"

Write-Host ""
Write-Host "Cursor MCP setup complete! Next steps:"
Write-Host "  1. Restart Cursor (or Ctrl+Shift+P -> Developer: Reload Window)"
Write-Host "  2. Cursor Settings -> Features -> MCP Servers -> verify yiuimcp is listed"
Write-Host "  3. Open Unity and confirm cn.etetet.yiuimcp package is loaded (port 3212)"
Write-Host "  4. Use Cursor Agent mode -- all YIUI tools will be available automatically"
