# Walk through all the results (for the default abstraction) and aggregate in csv 

import os
import csv
import json
from collections import OrderedDict
from time import gmtime, strftime

def aggregateResults () :
    bugResults = []
    meanResults = []
    stdDevResults = []
    timeResults = []

    bugResults.append(['Benchmark', 'BasicQL', 'QL', 'Random', 'Greedy', 'PCT-3', 'PCT-10', 'PCT-30', 'IDB'])
    meanResults.append(['Benchmark', 'BasicQL', 'QL', 'Random', 'Greedy', 'PCT-3', 'PCT-10', 'PCT-30', 'IDB'])
    stdDevResults.append(['Benchmark', 'BasicQL', 'QL', 'Random', 'Greedy', 'PCT-3', 'PCT-10', 'PCT-30', 'IDB'])
    timeResults.append(['Benchmark', 'BasicQL', 'QL', 'Random', 'Greedy', 'PCT-3', 'PCT-10', 'PCT-30', 'IDB'])

    # Get a list of all the created directories
    directories = [x[0] for x in os.walk("./Bugfinding/out")]

    for i in range(len(directories)) :
        if str(directories[i]).endswith("default"):
            with open(directories[i] + "/results.json") as f:
                bugResult = []
                meanResult = []
                stdDevResult = []
                timeResult = []
                d = json.load(f, object_pairs_hook=OrderedDict)
                bugResult.append(d[0]['TestName'])
                meanResult.append(d[0]['TestName'])
                stdDevResult.append(d[0]['TestName'])
                timeResult.append(d[0]['TestName'])
                for i in range(len(d)):
                    bugResult.append(d[i]['NumBuggyEpochs'])
                    meanResult.append(d[i]['AvgIterationsToBug'])
                    stdDevResult.append(d[i]['IterStdDev'])
                    timeResult.append(d[i]['AvgExplorationTimeSeconds'])
                bugResults.append(bugResult)
                meanResults.append(meanResult)
                stdDevResults.append(stdDevResult)
                timeResults.append(timeResult)
    
    time = strftime("%Y-%m-%d-%H-%M-%S", gmtime())
    with open("bugResults" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(bugResults)

    with open("meanResults" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(meanResults)
    
    with open("stdDevResults" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(stdDevResults)

    with open("timeResults" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(timeResults)

if __name__ == '__main__':
    aggregateResults()