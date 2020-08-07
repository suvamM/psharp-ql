param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [string]$mode="Bugfinding",
    [int]$numEpochs = 100,
    [int]$timeout = 0
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Running the P# reinforcement-learning benchmarks" -color "yellow"

if ($mode -eq "Test") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Test"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout
    }

    Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
    python3 ./Test/AggregateResults.py
    Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
}

elseif ($mode -eq "Bugfinding") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Bugfinding"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout
    }

    Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
    python3 ./Bugfinding/AggregateResults.py
    Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
}

elseif ($mode -eq "DataNondet") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/DataNondet"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $_ $experiments/$numEpochs $timeout
    }

    Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
    python3 ./DataNondet/AggregateResults.py
    Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
}

elseif ($mode -eq "StateHash") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/StateHash"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout
    }
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
