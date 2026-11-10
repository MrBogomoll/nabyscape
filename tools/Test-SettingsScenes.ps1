param([string]$Repository=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
$settings=Join-Path $Repository 'src\NabyScape\UI\Settings'
$scene=Get-Content -LiteralPath (Join-Path $settings 'SettingsScene.cs') -Raw
$registry=[regex]::Match($scene,'Sections=\{([^}]+)\}').Groups[1].Value
$registered=@([regex]::Matches($registry,'"([^"]+)"') | ForEach-Object {$_.Groups[1].Value})
$cases=@([regex]::Matches($scene,'case "([^"]+)":') | ForEach-Object {$_.Groups[1].Value})
foreach($title in $registered) { if ($registered -notcontains $title -or $cases -notcontains $title) { throw "Legacy scene has no rendering branch: $title" } }
if (@($registered | Sort-Object -Unique).Count -ne $registered.Count) { throw 'Duplicate Settings scene registration.' }
Write-Output "PASS $($registered.Count) legacy scene registrations match their rendering branches; live settings are covered by runtime diagnostics"
