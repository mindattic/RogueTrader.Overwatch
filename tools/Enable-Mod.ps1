# Adds a mod to Owlcat's EnabledModifications list so the game loads it on the next start.
param(
  [Parameter(Mandatory)] [string]$GameDataDir,
  [Parameter(Mandatory)] [string]$Name
)
$ErrorActionPreference = 'Stop'
$path = Join-Path $GameDataDir 'OwlcatModificationManagerSettings.json'
if (Test-Path $path) {
  $settings = Get-Content $path -Raw | ConvertFrom-Json
} else {
  $settings = [pscustomobject]@{ SourceDirectories = @(); EnabledModifications = @(); ActiveModifications = @(); DisabledModifications = @() }
}
$enabled = @($settings.EnabledModifications)
if ($enabled -contains $Name) { Write-Host "$Name is already enabled"; return }
$settings.EnabledModifications = $enabled + $Name
if ($settings.PSObject.Properties.Name -contains 'DisabledModifications') {
  $settings.DisabledModifications = @($settings.DisabledModifications | Where-Object { $_ -ne $Name })
}
[IO.File]::WriteAllText($path, ($settings | ConvertTo-Json -Compress -Depth 5))
Write-Host "Enabled $Name in $path"
