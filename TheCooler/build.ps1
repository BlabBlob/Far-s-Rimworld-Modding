$ErrorActionPreference = "Stop"

$modName = "TheCooler"
$csprojPath = Join-Path $PSScriptRoot "Source\TheCooler\TheCooler.csproj"
$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"

Write-Host "Compiling $modName using latest C# version..." -ForegroundColor Cyan

foreach ($gv in @("1.6", "1.7")) {
    dotnet build $csprojPath -c Release --no-incremental -p:LangVersion=preview -p:GameVer=$gv -p:BaseIntermediateOutputPath=obj/$gv/
    if ($LASTEXITCODE -ne 0) { break }
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to compile mod: $modName"
    exit 1
}

Write-Host "Successfully compiled $modName!" -ForegroundColor Green

if (-not ((Test-Path "$GamePath\RimWorld.exe") -or (Test-Path "$GamePath\RimWorldWin64.exe"))) {
    Write-Warning "RimWorld game directory not found at '$GamePath'. Skipping deployment."
    exit 0
}

$deployDir = "$GamePath\Mods\$modName"
Write-Host "Deploying mod to: $deployDir" -ForegroundColor Green

if (Test-Path $deployDir) {
    Remove-Item -Recurse -Force $deployDir
}
New-Item -ItemType Directory -Force -Path $deployDir > $null
New-Item -ItemType Directory -Force -Path "$deployDir\About" > $null
New-Item -ItemType Directory -Force -Path "$deployDir\Defs" > $null

New-Item -ItemType Directory -Force -Path "$deployDir\Textures" > $null

Copy-Item -Path (Join-Path $PSScriptRoot "About\*") -Destination "$deployDir\About" -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Defs\*") -Destination "$deployDir\Defs" -Recurse -Force
foreach ($ver in @("1.4", "1.5", "1.6", "1.7")) {
    $srcVer = if ($ver -eq "1.7") { "1.7" } else { "1.6" }
    $src = Join-Path $PSScriptRoot "$srcVer\Assemblies"
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force -Path "$deployDir\$ver\Assemblies" > $null
        Copy-Item -Path (Join-Path $src "*") -Destination "$deployDir\$ver\Assemblies" -Recurse -Force
    }
}
Copy-Item -Path (Join-Path $PSScriptRoot "Textures\*") -Destination "$deployDir\Textures" -Recurse -Force

Write-Host "$modName deployed successfully!" -ForegroundColor Green
