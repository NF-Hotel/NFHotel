<#
.SYNOPSIS
  Starts the development deployment from PowerShell: finds Git Bash and runs scripts/dev-up.sh with the same arguments.

.DESCRIPTION
  scripts/dev-up.sh is a bash script (ADR-0003: Proxmox VE in a Docker container, a VM in it, the services in the VM).
  On Windows it needs Git Bash, not the bash of WSL. This launcher looks for Git for Windows in the usual places and
  starts the script from the repository root, so it can be run from any folder and from a double click on a shortcut.
  The deployment comes from a checkout of NF-Hotel/DevOps (ADR-0012): ../DevOps beside this repository, the folder
  given with -DevOpsDir, or the one in the environment variable NFHOTEL_DEVOPS_DIR.

.PARAMETER DevOpsDir
  The NF-Hotel/DevOps checkout to deploy with. Sets NFHOTEL_DEVOPS_DIR for dev-up.sh.

.EXAMPLE
  .\scripts\dev-up.ps1 --check
  .\scripts\dev-up.ps1
  .\scripts\dev-up.ps1 --seed --open
  .\scripts\dev-up.ps1 web
  .\scripts\dev-up.ps1 -DevOpsDir D:\src\DevOps --check

.NOTES
  Exit code: the exit code of dev-up.sh; 1 when Git Bash is not found.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $DevOpsDir,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ScriptArguments
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Find-GitBash {
    $candidates = @(
        (Join-Path $env:ProgramFiles 'Git\bin\bash.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Git\bin\bash.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Git\bin\bash.exe')
    )
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($git) {
        # ...\Git\cmd\git.exe or ...\Git\mingw64\bin\git.exe: the bash is under bin of the Git folder.
        $gitFolder = Split-Path -Parent (Split-Path -Parent $git.Source)
        $candidates += (Join-Path $gitFolder 'bin\bash.exe')
        $candidates += (Join-Path (Split-Path -Parent $gitFolder) 'bin\bash.exe')
    }
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }
    return $null
}

$bash = Find-GitBash
if (-not $bash) {
    Write-Error 'Git Bash was not found. Install Git for Windows (https://git-scm.com/download/win) and run this again.'
    exit 1
}

if ($DevOpsDir) {
    # Forward slashes, so that bash reads the path as it is.
    $env:NFHOTEL_DEVOPS_DIR = (Resolve-Path -LiteralPath $DevOpsDir).Path -replace '\\', '/'
}

Push-Location $repositoryRoot
try {
    & $bash 'scripts/dev-up.sh' @ScriptArguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
