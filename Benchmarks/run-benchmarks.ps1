param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Running the P# reinforcement-learning benchmarks" -color "yellow"

$experiments = "$PSScriptRoot/Experiments"
Get-ChildItem $experiments -Filter *.test.json |
Foreach-Object {
    # $content = Get-Content $_.FullName
    Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
    & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $_
}

Write-Comment -prefix "." -text "Successfully run the P# reinforcement-learning benchmarks" -color "green"
