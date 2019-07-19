param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Building the P# reinforcement-learning benchmarks" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"

$solution = $PSScriptRoot + "\Benchmarks.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build the P# reinforcement-learning benchmarks"

Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built the P# reinforcement-learning benchmarks" -color "green"
