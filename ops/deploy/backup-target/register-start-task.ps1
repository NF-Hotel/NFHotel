# Registers the scheduled task that starts the stopped services of the backup target (MIL-074).
#
# Run it once on the backup host, as the user who runs Docker Desktop, from the folder of the
# backup target (the copy of ops/deploy/backup-target that holds backup-target.env):
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\register-start-task.ps1
# The task "\NFHotel\Backup target up" runs start-target.js at sign-in and every hour, only
# while that user is signed in, so no password is stored. Running the script again replaces
# the task. To remove it: Unregister-ScheduledTask -TaskPath '\NFHotel\' -TaskName 'Backup target up'
[CmdletBinding()]
param(
  # The folder of the backup target, with start-target.js and backup-target.env.
  [string]$Folder = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

$taskPath = '\NFHotel\'
$taskName = 'Backup target up'
$runTimeLimit = New-TimeSpan -Minutes 10
$repeatEvery = New-TimeSpan -Hours 1

$script = Join-Path $Folder 'start-target.js'
foreach ($required in @($script, (Join-Path $Folder 'backup-target.env'))) {
  if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
    throw "missing $required"
  }
}

$user = "$env:USERDOMAIN\$env:USERNAME"
$action = New-ScheduledTaskAction -Execute (Join-Path $env:WINDIR 'System32\wscript.exe') `
  -Argument "//B //Nologo `"$script`"" -WorkingDirectory $Folder
$atSignIn = New-ScheduledTaskTrigger -AtLogOn -User $user
$hourly = New-ScheduledTaskTrigger -Once -At (Get-Date).Date -RepetitionInterval $repeatEvery
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries `
  -DontStopIfGoingOnBatteries -ExecutionTimeLimit $runTimeLimit -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskPath $taskPath -TaskName $taskName -Action $action `
  -Trigger $atSignIn, $hourly -Principal $principal -Settings $settings -Force `
  -Description ('MIL-074: at sign-in and every hour, docker compose up -d in the folder of ' +
    'the backup target, so a stopped service of the target of the copy off the VM starts ' +
    'again. Changes nothing while both services run.') |
  Select-Object TaskPath, TaskName, State
