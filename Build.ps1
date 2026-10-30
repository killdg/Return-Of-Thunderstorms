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
Write-Output 'Build staged under artifacts/INSTALL; no live installation changed.'
