param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "src\CGMCollage\CGMCollage.csproj"
$publish = Join-Path $root "artifacts\publish"
dotnet restore $project
dotnet build $project -c $Configuration --no-restore
dotnet publish $project -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publish
$isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe" }
if (-not (Test-Path $isccPath)) { throw "Inno Setup 7 was not found. Install it from https://jrsoftware.org/isdl.php" }
& $isccPath (Join-Path $root "installer\CGMCollage.iss")
Write-Host "Built artifacts\CGM-Collage-Setup.exe"
