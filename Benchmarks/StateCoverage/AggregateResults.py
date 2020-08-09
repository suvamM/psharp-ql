import os
import glob
import pandas as pd
import sys

def aggregateResults(expName):
    os.chdir("./StateCoverage/" + str(expName))
    extension = 'csv'
    all_filenames = [i for i in glob.glob('*.{}'.format(extension))]

    combined_csv = pd.merge(left=pd.read_csv(all_filenames[0]), right=pd.read_csv(all_filenames[1]), on='Step')
    
    for i in range(2,len(all_filenames)):
        combined_csv = pd.merge(left=combined_csv, right=pd.read_csv(all_filenames[i]), on='Step')

    os.chdir('../')
    combined_csv.to_csv(str(expName) + ".csv", index=False, encoding='utf-8-sig')

if __name__ == '__main__':
    aggregateResults(str(sys.argv[1]))