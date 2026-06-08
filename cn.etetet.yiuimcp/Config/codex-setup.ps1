# codex-setup.ps1
# Creates .codex/config.toml (MCP server entry) and patches AGENTS.md in the Unity project root.
# Run once per Unity project to enable OpenAI Codex MCP + agent context for YIUI automation.
#
# Usage (from Unity project root):
#   powershell -ExecutionPolicy Bypass -File "Packages\cn.etetet.yiuimcp\Config\codex-setup.ps1"
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

# UTO build entry point (forward slashes, Node.js accepts them on Windows)
$utoPath = (Join-Path $ProjectRoot "Packages/cn.etetet.yiuimcp/UTO/build/index.js").Replace("\", "/")

if (-not (Test-Path ($utoPath.Replace("/", "\")))) {
    Write-Host "WARNING: UTO build not found at: $utoPath"
    Write-Host "Run 'npm install && npm run build' in Packages/cn.etetet.yiuimcp/UTO first."
}

$codexDir = Join-Path $ProjectRoot ".codex"
New-Item -ItemType Directory -Force $codexDir | Out-Null

# ---- .codex/config.toml ----
$tomlPath = Join-Path $codexDir "config.toml"

$mcpBlock = @"

[mcp_servers.yiuimcp]
command = "node"
args = ["$utoPath"]
"@

if (Test-Path $tomlPath) {
    $raw = Get-Content $tomlPath -Raw -Encoding UTF8
    if ($raw -match '\[mcp_servers\.yiuimcp\]') {
        Write-Host "SKIP: $tomlPath already contains [mcp_servers.yiuimcp]"
    } else {
        [System.IO.File]::WriteAllText(
            $tomlPath,
            $raw.TrimEnd() + "`n" + $mcpBlock + "`n",
            [System.Text.Encoding]::UTF8
        )
        Write-Host "Updated: $tomlPath  (yiuimcp MCP server appended)"
    }
} else {
    [System.IO.File]::WriteAllText($tomlPath, $mcpBlock.TrimStart() + "`n", [System.Text.Encoding]::UTF8)
    Write-Host "Created: $tomlPath"
}

# ---- AGENTS.md ----
$agentsMdPath = Join-Path $ProjectRoot "AGENTS.md"
$agentsSrc    = Join-Path $scriptDir "codex\AGENTS.yiuimcp.md"
$marker       = "<!-- yiuimcp:start -->"

if (-not (Test-Path $agentsSrc)) {
    Write-Host "ERROR: AGENTS template not found: $agentsSrc"
    exit 1
}

$addition = [System.IO.File]::ReadAllText($agentsSrc, [System.Text.Encoding]::UTF8)

if (Test-Path $agentsMdPath) {
    $raw = [System.IO.File]::ReadAllText($agentsMdPath, [System.Text.Encoding]::UTF8)
    if ($raw -match [regex]::Escape($marker)) {
        Write-Host "SKIP: AGENTS.md already contains yiuimcp section"
    } else {
        [System.IO.File]::WriteAllText(
            $agentsMdPath,
            $raw.TrimEnd() + "`n`n" + $addition,
            [System.Text.Encoding]::UTF8
        )
        Write-Host "Updated: $agentsMdPath  (yiuimcp section appended)"
    }
} else {
    [System.IO.File]::WriteAllText($agentsMdPath, $addition, [System.Text.Encoding]::UTF8)
    Write-Host "Created: $agentsMdPath"
}

Write-Host ""
Write-Host "Codex setup complete! Next steps:"
Write-Host "  1. Open Unity and confirm cn.etetet.yiuimcp is loaded (port 3212)"
Write-Host "  2. Run: codex  (in the Unity project root)"
Write-Host "  3. Codex will auto-connect to the yiuimcp MCP server"
Write-Host "  4. All YIUI tools will be available -- use Agent mode"
