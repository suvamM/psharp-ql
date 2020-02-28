# Walk through all the results (for the default abstraction) and aggregate in csv 

import os
import csv
import json
from collections import OrderedDict
from time import gmtime, strftime

def aggregateResults () :
    results = []
    results.append(['Benchmark', 'QL', 'QL-NDN', 'Random', 'Greedy', 'PCT-3', 'PCT-10', 'PCT-30', 'IDB'])

    # Get a list of all the created directories
    directories = [x[0] for x in os.walk("./out")]

    for i in range(len(directories)) :
        if "default" in directories[i]:
            with open(directories[i] + "\\results.json") as f:
                bugResult = []
                bugResult.append(getBenchmarkName(directories[i]))
                d = json.load(f, object_pairs_hook=OrderedDict)

                for i in range(len(d)):
                    bugResult.append(d[i]['BugFraction'])

                results.append(bugResult)
    
    time = strftime("%Y-%m-%d-%H-%M-%S", gmtime())
    with open(directories[0] + "\\results" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(results)

def getBenchmarkName (s):
    splitnames = s.split("\\")
    name = splitnames[1]
    return name

if __name__ == '__main__':
    print(".. Aggregating results")
    aggregateResults()

