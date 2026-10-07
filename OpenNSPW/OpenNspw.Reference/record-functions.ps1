# Builds the reference, records the function tests (see function_tests.h), and writes them to OpenNspw.Tests:
# Layout.json as it is, and the recorded calls compressed with gzip, as Functions/<file>/<function>.jsonl.gz.
param(
	[string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# The project uses the Visual Studio 2022 toolset (v143).
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -version '[17.0,18.0)' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild "$PSScriptRoot\OpenNspw.Reference.vcxproj" "-p:Configuration=$Configuration" -p:Platform=Win32 -m -v:minimal -nologo
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$recorded = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
$process = Start-Process "$PSScriptRoot\bin\$Configuration\OpenNspw.Reference.exe" -ArgumentList "--record-functions `"$recorded`"" -Wait -PassThru
if ($process.ExitCode) { throw "Recording failed with exit code $($process.ExitCode)." }

$tests = Resolve-Path "$PSScriptRoot\..\OpenNspw.Tests"
Copy-Item "$recorded\Layout.json" "$tests\Layout.json"
if (Test-Path "$tests\Functions") { Remove-Item "$tests\Functions" -Recurse }
foreach ($file in Get-ChildItem "$recorded\Functions" -Recurse -Filter *.jsonl) {
	$target = "$tests\Functions\$($file.Directory.Name)\$($file.Name).gz"
	New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
	$in = [IO.File]::OpenRead($file.FullName)
	$out = [IO.File]::Create($target)
	$gzip = New-Object IO.Compression.GZipStream($out, [IO.Compression.CompressionMode]::Compress)
	$in.CopyTo($gzip)
	$gzip.Dispose()
	$out.Dispose()
	$in.Dispose()
}

Remove-Item $recorded -Recurse
