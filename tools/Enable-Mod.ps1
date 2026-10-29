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
$enabled = @($settings.EnabledModifications | Where-Object { $_ })
$disabled = @($settings.DisabledModifications | Where-Object { $_ })
if ($enabled -contains $Name -and $disabled -notcontains $Name) { Write-Host "$Name is already enabled"; return }
# Add-Member -Force works whether or not the property exists (a plain assignment throws when it's missing).
if ($enabled -notcontains $Name) { $enabled += $Name }
$settings | Add-Member -NotePropertyName EnabledModifications -NotePropertyValue @($enabled) -Force
$settings | Add-Member -NotePropertyName DisabledModifications -NotePropertyValue @($disabled | Where-Object { $_ -ne $Name }) -Force
[IO.File]::WriteAllText($path, ($settings | ConvertTo-Json -Compress -Depth 5))
Write-Host "Enabled $Name in $path"
