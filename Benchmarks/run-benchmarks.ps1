param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [string]$mode="Bugfinding",
    [int]$numEpochs = 100,
    [int]$timeout = 0,
    [string]$defaultCSVPath = ""
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Running the P# reinforcement-learning benchmarks" -color "yellow"

if ($mode -eq "Test") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Test"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout $defaultCSVPath
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
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout $defaultCSVPath
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
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout $defaultCSVPath
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
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout $defaultCSVPath
    }
}

elseif ($mode -eq "Perf") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Perf"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ 1 $timeout $defaultCSVPath
    }

    Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
    python3 ./Perf/AggregateResults.py
    Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
}

elseif ($mode -eq "StateCoverage") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $testerPath = "$PSScriptRoot/../bin/netcoreapp3.1/PSharpTester.dll"
    $raftCsvPath = "$PSScriptRoot/StateCoverage/Raftv1"
    $fdCsvPath = "$PSScriptRoot/StateCoverage/FailureDetector"
    $ssCSvPath = "$PSScriptRoot/StateCoverage/SafeStack"
    Write-Comment -prefix "..." -text "Running experiment Raftv1" -color "yellow"
    & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll ./StateCoverage/Raftv1.default.test.json 1 $timeout $raftCsvPath
    & python3 ./StateCoverage/AggregateResults.py Raftv1
    & Write-Comment -prefix "..." -text "Running experiment FailureDetector" -color "yellow"
    & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll ./StateCoverage/FailureDetector.default.test.json 1 $timeout $fdCsvPath
    & python3 ./StateCoverage/AggregateResults.py FailureDetector
    & Write-Comment -prefix "..." -text "Running experiment SafeStack" -color "yellow"
    & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll ./StateCoverage/SafeStack.default.test.json 1 $timeout $ssCsvPath
    & python3 ./StateCoverage/AggregateResults.py SafeStack
}

else {
    Write-Comment -prefix ".." -text "Bad mode ($mode) specified" -color "red"
}

Write-Comment -prefix "." -text "Successfully run the P# reinforcement-learning benchmarks" -color "green"
