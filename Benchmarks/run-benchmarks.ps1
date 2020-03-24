param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Removing existing results, if any" -color "yellow"
Remove-Item ".\out" -Recurse -ErrorAction Ignore

Write-Comment -prefix "." -text "Running the P# reinforcement-learning benchmarks" -color "yellow"

$experiments = "$PSScriptRoot\Experiments"
Get-ChildItem $experiments -Filter *.test.json |
Foreach-Object {
    # $content = Get-Content $_.FullName
    Write-Comment -prefix "..." -text "Running experiment $_" -color "yellow"
    & $PSScriptRoot\bin\net46\EvaluationDriver.exe $experiments\$_
}

Write-Comment -prefix "." -text "Successfully run the P# reinforcement-learning benchmarks" -color "green"

Write-Comment -prefix "." -text "Aggregating results, and dumping to csv" -color "yellow"
python .\AggregateResults.py
Write-Comment -prefix "." -text "Result aggregation completed. All experiments done." -color "green"
