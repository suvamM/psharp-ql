param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [string]$mode="bugfinding",
    [string]$benchmarks="all",
    [int]$bx = 100
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Running the P# reinforcement-learning benchmarks" -color "yellow"

if ($mode -eq "bugfinding") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Bugfinding"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $_
    }
}

elseif ($mode -eq "datanotdet") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
}

elseif ($mode -eq "statehash") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
}

elseif ($mode -eq "perf") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
}

elseif ($mode -eq "state-coverage") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
}

else {
    Write-Comment -prefix ".." -text "Bad mode ($mode) specified" -color "red"
}

Write-Comment -prefix "." -text "Successfully run the P# reinforcement-learning benchmarks" -color "green"
