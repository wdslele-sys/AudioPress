param(
  [string]$Version = "0.1.0",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $root "artifacts"
$portableRoot = Join-Path $artifacts "portable"
$appDir = Join-Path $portableRoot "AudioPress"
$zipPath = Join-Path $artifacts "AudioPress-v$Version-win-x64-portable.zip"
$shaPath = Join-Path $artifacts "SHA256SUMS.txt"
$assemblyVersion = "0.1.0"
if ($Version -match "^\d+(\.\d+){0,3}") {
  $assemblyVersion = $Matches[0]
}

Remove-Item $portableRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $appDir | Out-Null
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

& (Join-Path $PSScriptRoot "prepare-ffmpeg.ps1")

dotnet publish (Join-Path $root "src/AudioPress/AudioPress.csproj") `
  --configuration $Configuration `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:Version=$Version `
  -p:AssemblyVersion=$assemblyVersion `
  -p:FileVersion=$assemblyVersion `
  -o $appDir

$packageFfmpegDir = Join-Path $appDir "tools/ffmpeg"
New-Item -ItemType Directory -Force -Path $packageFfmpegDir | Out-Null
Copy-Item (Join-Path $root "tools/ffmpeg/*") $packageFfmpegDir -Recurse -Force

Copy-Item (Join-Path $root "packaging/README.txt") (Join-Path $appDir "README.txt") -Force
Copy-Item (Join-Path $root "LICENSE") (Join-Path $appDir "LICENSE") -Force
Copy-Item (Join-Path $root "THIRD_PARTY_NOTICES.md") (Join-Path $appDir "THIRD_PARTY_NOTICES.md") -Force

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path $appDir -DestinationPath $zipPath -Force

$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
"$($hash.Hash.ToLowerInvariant())  $(Split-Path $zipPath -Leaf)" | Set-Content -Path $shaPath -Encoding UTF8

Write-Host "Created $zipPath"
Write-Host "Created $shaPath"
