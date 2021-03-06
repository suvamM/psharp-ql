# Walk through all the results (for the default abstraction) and aggregate in csv 

import os
import csv
import json
from collections import OrderedDict
from time import gmtime, strftime

def aggregateResults () :
    results = []
    results.append(['Benchmark', 'QL', 'QL-NDN'])

    # Get a list of all the created directories
    directories = [x[0] for x in os.walk("./DataNondet/out")]

    for i in range(len(directories)) :
        if str(directories[i]).endswith("default"):
            with open(directories[i] + "/results.json") as f:
                bugResult = []
                d = json.load(f, object_pairs_hook=OrderedDict)
                bugResult.append(d[0]['TestName'])
                for i in range(len(d)):
                    bugResult.append(d[i]['NumBuggyEpochs'])

                results.append(bugResult)
    
    time = strftime("%Y-%m-%d-%H-%M-%S", gmtime())
    with open("datanondet" + time + ".csv", "a", newline='') as my_csv:
        csvWriter = csv.writer(my_csv, delimiter=',')
        csvWriter.writerows(results)

if __name__ == '__main__':
    aggregateResults()