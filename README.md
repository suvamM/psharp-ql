# Learning-based controlled concurrency testing in P#
This repository is a fork of the [P#](https://github.com/p-org/PSharp)
programming framework that implements a learning-based exploration strategy
using Q-learning.

## Benchmarks for evaluating the Q-Learning strategy

You can find benchmarks for evaluating our Q-Learning strategy in this [directory](Benchmarks), as well as scripts for easily running these benchmarks.

## Prerequisites
Install [Visual Studio 2019](https://www.visualstudio.com/downloads/) and if necessary a version of [.NET Core](https://dotnet.microsoft.com/download/dotnet-core) that matches the version specified in the [global.json](../global.json) file. See [version matching rules](https://docs.microsoft.com/en-us/dotnet/core/tools/global-json). Also install all the SDK versions of the .NET Framework that P# currently supports (4.5 and 4.6) from [here](https://www.microsoft.com/net/download/archives).

## Building
First build the P# runtime and testing tool. From this root directory, run in `powershell`:
```
.\Scripts\build.ps1
```
Next, build the benchmarks and evaluation driver, by running in `powershell`:
```
.\Benchmarks\build-benchmarks.ps1
```

## How to run
To execute an experiment, simply run the `EvaluationDriver` using the corresponding test configuration file, as in the following example:
```
.\bin\net46\EvaluationDriver.exe .\Benchmarks\Protocols\FailureDetector.test.json
```
