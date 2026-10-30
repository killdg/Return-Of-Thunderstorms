param([Parameter(Mandatory=$true)][string]$SptRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
function Run([string[]]$Arguments){& $Dotnet @Arguments;if($LASTEXITCODE -ne 0){throw 'Build failed.'}}
Run @('build',(Join-Path $PSScriptRoot 'src/Server/ReturnOfThunderstorms.csproj'),'-c','Release',"-p:SPTBinaries=$(Join-Path $SptRoot 'SPT_Runtime')")
Run @('build',(Join-Path $PSScriptRoot 'src/Client/ReturnOfThunderstorms.Client.csproj'),'-c','Release',"-p:SPTPath=$SptRoot")
$server=Join-Path $PSScriptRoot 'artifacts/INSTALL/SPT_Runtime/user/mods/ReturnOfThunderstorms'
$client=Join-Path $PSScriptRoot 'artifacts/INSTALL/BepInEx/plugins/ReturnOfThunderstorms'
New-Item -ItemType Directory -Path $server,(Join-Path $client 'Audio') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Server/bin/Release/net10.0/ReturnOfThunderstorms.dll') -Destination $server -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Server/config.example.json') -Destination $server -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src/Client/bin/Release/netstandard2.1/ReturnOfThunderstorms.Client.dll') -Destination $client -Force
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets/Audio') -Filter '*.wav' | Copy-Item -Destination (Join-Path $client 'Audio') -Force
$clientProject=[xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/Client/ReturnOfThunderstorms.Client.csproj') -Raw)
$packageVersion=[string]$clientProject.Project.PropertyGroup.Version
$packageRoot=Join-Path $PSScriptRoot ('artifacts/package-'+$packageVersion)
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $packageRoot 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'artifacts/INSTALL/BepInEx') -Destination $packageRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'artifacts/INSTALL/SPT_Runtime') -Destination $packageRoot -Recurse -Force
$rootNames=@(Get-ChildItem -LiteralPath $packageRoot | ForEach-Object {$_.Name} | Sort-Object)
if(($rootNames -join '|') -ne 'BepInEx|README.md|SPT_Runtime'){throw 'Unexpected package root entries.'}
$archive=Join-Path $PSScriptRoot ('artifacts/Return-Of-Thunderstorms-'+$packageVersion+'.zip')
Compress-Archive -Path (Join-Path $packageRoot 'README.md'),(Join-Path $packageRoot 'BepInEx'),(Join-Path $packageRoot 'SPT_Runtime') -DestinationPath $archive -Force
Write-Output ('Build staged and root-layout ZIP created: '+$archive+'; no live installation changed.')
