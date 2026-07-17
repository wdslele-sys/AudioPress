param(
  [string]$Destination = "",
  [string]$FfmpegUrl = "",
  [switch]$Force
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([string]::IsNullOrWhiteSpace($Destination)) {
  $Destination = Join-Path $root "tools/ffmpeg"
}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

if ([string]::IsNullOrWhiteSpace($FfmpegUrl)) {
  $FfmpegUrl = $env:AUDIOPRESS_FFMPEG_URL
}

$existingFfmpeg = Join-Path $Destination "ffmpeg.exe"
$existingFfprobe = Join-Path $Destination "ffprobe.exe"
if (!$Force -and [string]::IsNullOrWhiteSpace($FfmpegUrl) -and (Test-Path $existingFfmpeg) -and (Test-Path $existingFfprobe)) {
  Write-Host "Using existing FFmpeg tools in $Destination"
  if (!(Test-Path (Join-Path $Destination "FFMPEG_VERSION.txt"))) {
    & $existingFfmpeg -version | Set-Content -Path (Join-Path $Destination "FFMPEG_VERSION.txt") -Encoding UTF8
  }
  return
}

if ([string]::IsNullOrWhiteSpace($FfmpegUrl)) {
  $FfmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip"
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("AudioPress-ffmpeg-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $temp | Out-Null

try {
  $zip = Join-Path $temp "ffmpeg.zip"
  Write-Host "Downloading FFmpeg from $FfmpegUrl"
  Invoke-WebRequest -Uri $FfmpegUrl -OutFile $zip

  Expand-Archive -Path $zip -DestinationPath $temp -Force

  $ffmpeg = Get-ChildItem -Path $temp -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
  $ffprobe = Get-ChildItem -Path $temp -Recurse -Filter "ffprobe.exe" | Select-Object -First 1

  if ($null -eq $ffmpeg -or $null -eq $ffprobe) {
    throw "The downloaded FFmpeg archive did not contain ffmpeg.exe and ffprobe.exe."
  }

  Copy-Item $ffmpeg.FullName (Join-Path $Destination "ffmpeg.exe") -Force
  Copy-Item $ffprobe.FullName (Join-Path $Destination "ffprobe.exe") -Force

  $license = Get-ChildItem -Path $temp -Recurse -Filter "LICENSE*" | Select-Object -First 1
  if ($null -ne $license) {
    Copy-Item $license.FullName (Join-Path $Destination "LICENSE.txt") -Force
  }

  $readme = Get-ChildItem -Path $temp -Recurse -Filter "README*" | Select-Object -First 1
  if ($null -ne $readme) {
    Copy-Item $readme.FullName (Join-Path $Destination "README.txt") -Force
  }

  $versionPath = Join-Path $Destination "FFMPEG_VERSION.txt"
  & (Join-Path $Destination "ffmpeg.exe") -version | Set-Content -Path $versionPath -Encoding UTF8
  Write-Host "FFmpeg prepared in $Destination"
}
finally {
  Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
}
