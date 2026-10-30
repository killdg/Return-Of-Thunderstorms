param(
    [Parameter(Mandatory = $true)]
    [string]$SptRoot,
    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'

function Invoke-DotnetBuild
{
    param([string[]]$Arguments)

    & $Dotnet @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Build failed.'
    }
}

$serverProject = Join-Path $PSScriptRoot 'src/Server/ReturnOfThunderstorms.csproj'
$clientProject = Join-Path $PSScriptRoot 'src/Client/ReturnOfThunderstorms.Client.csproj'

Invoke-DotnetBuild -Arguments @(
    'build', $serverProject, '-c', 'Release',
    "-p:SPTBinaries=$(Join-Path $SptRoot 'SPT_Runtime')"
)
Invoke-DotnetBuild -Arguments @(
    'build', $clientProject, '-c', 'Release',
    "-p:SPTPath=$SptRoot"
)

# Stage only this mod's own binaries, example configuration and local audio.
# Nothing in this script installs files into the running SPT game directory.
[xml]$projectXml = Get-Content -LiteralPath $clientProject -Raw
$version = [string]$projectXml.Project.PropertyGroup.Version
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$buildId = [Guid]::NewGuid().ToString('N')
$packageRoot = Join-Path $artifacts ("package-$version-$buildId")
$serverFolder = Join-Path $packageRoot 'SPT_Runtime/user/mods/ReturnOfThunderstorms'
$clientFolder = Join-Path $packageRoot 'BepInEx/plugins/ReturnOfThunderstorms'
$audioFolder = Join-Path $clientFolder 'Audio'

New-Item -ItemType Directory -Path $serverFolder, $audioFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Server/bin/Release/net10.0/ReturnOfThunderstorms.dll') `
    -Destination $serverFolder
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Server/config.example.json') `
    -Destination $serverFolder
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Client/bin/Release/netstandard2.1/ReturnOfThunderstorms.Client.dll') `
    -Destination $clientFolder

foreach ($audioFile in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets/Audio') -Filter '*.wav' -File)
{
    Copy-Item -LiteralPath $audioFile.FullName -Destination $audioFolder
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $packageRoot

# SPT mod-page layout: no INSTALL wrapper or extra top-level files.
$rootNames = @(Get-ChildItem -LiteralPath $packageRoot | ForEach-Object { $_.Name } | Sort-Object)
if (($rootNames -join '|') -ne 'BepInEx|README.md|SPT_Runtime')
{
    throw 'Unexpected package root entries.'
}

$archive = Join-Path $artifacts ("Return-Of-Thunderstorms-$version.zip")
$archiveEntries = @(
    (Join-Path $packageRoot 'README.md'),
    (Join-Path $packageRoot 'BepInEx'),
    (Join-Path $packageRoot 'SPT_Runtime')
)
Compress-Archive -Path $archiveEntries -DestinationPath $archive -Force
Write-Output "Release ZIP created: $archive. No live installation changed."
