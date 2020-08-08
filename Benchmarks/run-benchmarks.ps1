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
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout
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

elseif ($mode -eq "Perf") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $experiments = "$PSScriptRoot/Perf"
    Get-ChildItem $experiments -Filter *.test.json |
    Foreach-Object {
        Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
        & $dotnet $PSScriptRoot/bin/netcoreapp3.1/EvaluationDriver.dll $experiments/$_ $numEpochs $timeout
    }

    Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
    python3 ./Perf/AggregateResults.py
    Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
}

elseif ($mode -eq "StateCoverage") {
    Write-Comment -prefix ".." -text "Running in mode $mode" -color "yellow"
    $testerPath = "$PSScriptRoot/../bin/netcoreapp3.1/PSharpTester.dll"
    Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler QL" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:rl -abstraction-level:default -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/QL.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler Random" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:random -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/Random.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler Greedy" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:greedy -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/Greedy.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler PCT:3" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:pct:3 -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/PCT3.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler PCT:10" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:pct:10 -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/PCT10.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler PCT:30" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:pct:30 -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/PCT30.csv"
    & Write-Comment -prefix "..." -text "Running experiment Raftv1, Scheduler IDB" -color "yellow"
    & $dotnet $testerPath -test:$PSScriptRoot/bin/netcoreapp3.1/Benchmarks.Protocols_BugsDisabled.dll -method:Test_Raftv1 -i:10000 -max-steps:1000:1000 -sch:idb -stateInfoCSV:"$PSScriptRoot/StateCoverage/Raftv1/IDB.csv"
}

else {
    Write-Comment -prefix ".." -text "Bad mode ($mode) specified" -color "red"
}

Write-Comment -prefix "." -text "Successfully run the P# reinforcement-learning benchmarks" -color "green"
