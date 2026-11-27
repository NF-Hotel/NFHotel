# Registers the scheduled task that starts the stopped services of the backup target (MIL-074)
# and restarts the service backup-target when it runs without its port (MIL-091).
#
# Run it once on the backup host, as the user who runs Docker Desktop, from the folder of the
# backup target (the copy of ops/deploy/backup-target that holds backup-target.env):
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\register-start-task.ps1
# The task "\NFHotel\Backup target up" runs start-target.js 5 minutes after sign-in, when
# Tailscale and Docker Desktop are up, and every hour, only while that user is signed in, so no
# password is stored. Running the script again replaces the task. To remove it:
#   Unregister-ScheduledTask -TaskPath '\NFHotel\' -TaskName 'Backup target up'
[CmdletBinding()]
param(
  # The folder of the backup target, with start-target.js and backup-target.env; the folder of
  # this script when it is not given.
  [string]$Folder
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 leaves $PSScriptRoot empty in the default value of a parameter of an
# advanced script that runs with -File, so the default is set here, where it is known.
if (-not $Folder) {
  $Folder = $PSScriptRoot
}

$taskPath = '\NFHotel\'
$taskName = 'Backup target up'
$runTimeLimit = New-TimeSpan -Minutes 10
$repeatEvery = New-TimeSpan -Hours 1
# The delay of the run at sign-in, as an ISO 8601 duration: a service that Docker Desktop starts
# before the host has its tailnet address runs without its port, so the first check comes later.
$signInDelay = 'PT5M'

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
$atSignIn.Delay = $signInDelay
$hourly = New-ScheduledTaskTrigger -Once -At (Get-Date).Date -RepetitionInterval $repeatEvery
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries `
  -DontStopIfGoingOnBatteries -ExecutionTimeLimit $runTimeLimit -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskPath $taskPath -TaskName $taskName -Action $action `
  -Trigger $atSignIn, $hourly -Principal $principal -Settings $settings -Force `
  -Description ('MIL-074 and MIL-091: 5 minutes after sign-in and every hour, docker compose ' +
    'up -d in the folder of the backup target, so a stopped service of the target of the copy ' +
    'off the VM starts again, and one restart of the service backup-target when nothing ' +
    'listens on its port while the host has its tailnet address. Changes nothing while the ' +
    'port answers.') |
  Select-Object TaskPath, TaskName, State
