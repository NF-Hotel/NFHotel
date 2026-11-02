<#
.SYNOPSIS
  Prepares this machine so NF Hotel starts from Visual Studio with F5: runs scripts/dev-f5.sh through Git Bash.

.DESCRIPTION
  The same as .\scripts\dev-up.ps1 --f5 (ADR-0036, MIL-057). It starts PostgreSQL in Docker and writes the .NET user
  secrets of NFHotel.Api and NFHotel.Web once; then open NFHotel.sln, choose the launch profile "API and web" and press
  F5. The logic lives once, in scripts/dev-f5.sh; dev-up.ps1 finds Git Bash.

.EXAMPLE
  .\scripts\dev-f5.ps1 --check
  .\scripts\dev-f5.ps1
  .\scripts\dev-f5.ps1 --destroy

.NOTES
  Exit code: the exit code of dev-f5.sh; 1 when Git Bash is not found.
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $ScriptArguments
)

& (Join-Path $PSScriptRoot 'dev-up.ps1') '--f5' @ScriptArguments
exit $LASTEXITCODE
