param(
    [string]$Configuration = "Release",
    [ValidateSet("win-x64", "win-arm64")]
    [string]$RuntimeIdentifier = "win-x64"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "src\CGMCollage\CGMCollage.csproj"
$artifacts = [IO.Path]::GetFullPath((Join-Path $root "artifacts"))
$publish = [IO.Path]::GetFullPath((Join-Path $artifacts "publish\$RuntimeIdentifier"))
if (-not $publish.StartsWith($artifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish directory must remain inside $artifacts"
}
if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
$architecture = if ($RuntimeIdentifier -eq "win-arm64") { "arm64" } else { "x64" }
$architectureExpression = if ($architecture -eq "arm64") { "arm64" } else { "x64compatible and not arm64" }
dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
dotnet build $project -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
dotnet publish $project -c $Configuration -r $RuntimeIdentifier --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$publishedExe = Join-Path $publish "CGM Collage.exe"
$exeBytes = [IO.File]::ReadAllBytes($publishedExe)
$peOffset = [BitConverter]::ToInt32($exeBytes, 0x3c)
$machine = [BitConverter]::ToUInt16($exeBytes, $peOffset + 4)
$expectedMachine = if ($architecture -eq "arm64") { [uint16]0xAA64 } else { [uint16]0x8664 }
if ($machine -ne $expectedMachine) {
    throw "Expected $architecture executable machine 0x$($expectedMachine.ToString('X4')), found 0x$($machine.ToString('X4'))"
}
if ($RuntimeIdentifier -eq "win-x64") {
    $smoke = Join-Path $artifacts "smoke\$RuntimeIdentifier-$PID"
    $smokeProcess = Start-Process -FilePath $publishedExe -ArgumentList "--smoke-test", ('"' + $smoke + '"') -Wait -PassThru
    if ($smokeProcess.ExitCode -ne 0 -or -not (Test-Path (Join-Path $smoke "smoke-passed.txt"))) {
        throw "x64 smoke test failed"
    }
}
$isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe" }
if (-not (Test-Path $isccPath)) { throw "Inno Setup 7 was not found. Install it from https://jrsoftware.org/isdl.php" }
& $isccPath "/DRuntimeIdentifier=$RuntimeIdentifier" "/DAppArchitecture=$architecture" "/DArchitectureExpression=$architectureExpression" (Join-Path $root "installer\CGMCollage.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE" }
Write-Host "Built artifacts\CGM-Collage-Setup-$architecture.exe"
