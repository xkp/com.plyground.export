param(
  [Parameter(Mandatory = $true)][string]$UnityEditor,
  [Parameter(Mandatory = $true)][string]$PlasticNewtonsoftDll
)
$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
$smokeRoot = Join-Path ([IO.Path]::GetTempPath()) ('plyground-export-schema-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$smokeRoot/Assets/Editor", "$smokeRoot/Packages" | Out-Null
Get-ChildItem -LiteralPath "$packageRoot/Editor" -Filter '*.cs' | Copy-Item -Destination "$smokeRoot/Assets/Editor"
Copy-Item -LiteralPath "$PSScriptRoot/CompactFeatureSmoke.cs", $PlasticNewtonsoftDll -Destination "$smokeRoot/Assets/Editor"
Set-Content -LiteralPath "$smokeRoot/Packages/manifest.json" -Value '{"dependencies":{}}'
$smokeProcess = Start-Process -FilePath $UnityEditor -ArgumentList "-batchmode -nographics -projectPath `"$smokeRoot`" -executeMethod CompactFeatureSmoke.Run -logFile `"$smokeRoot/smoke.log`"" -WindowStyle Hidden -PassThru
$smokeProcess.WaitForExit()
Write-Output "Smoke project and log: $smokeRoot"
if ($smokeProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath "$smokeRoot/smoke.log" -Pattern 'COMPACT_FEATURE_SMOKE_PASSED' -Quiet)) {
  throw "Compact feature schema tests failed. See $smokeRoot/smoke.log"
}
Write-Output 'Compact feature schema tests passed.'
