import matplotlib as mpl
import matplotlib.pyplot as plt
import numpy as np

import pandas as pd
from pandas import ExcelWriter
from pandas import ExcelFile

from collections import OrderedDict

plt.style.use('classic')
# plt.style.use('seaborn-deep')

fonts = {
  "text.usetex": True,
  "font.family": "serif",
  "axes.labelsize": 16,
  "font.size": 14,
  "legend.fontsize": 12,
  "xtick.labelsize": 12,
  "ytick.labelsize": 12,
}

mpl.rcParams.update(fonts)

df = pd.read_excel('CalculatorActionsRandom.xlsx', sheet_name='Sheet1')
#plt.plot(x1, y1, label = "line 1") 
fig, ax = plt.subplots(ncols=1, figsize=(6, 3))
x = df['Iterations'].tolist()

# Raft-v1 data
y0_add = df['Add'].tolist()
y0_sub = df['Sub'].tolist()
y0_mul = df['Mul'].tolist()
y0_div = df['Div'].tolist()
y0_res = df['Res'].tolist()


ax.grid(True, axis='y')
ax.set_aspect('auto', adjustable='box')
# ax.set_title('Calculator')
ax.set_ylabel('\#Actions')
ax.set_xlabel('\#Program runs')
#ax.set_ylim(0, 400000)
#ax.set_xticks(x, minor=False)
#ax.set_xticklabels(x_labels)
ax.margins(0.05)

linestyles = OrderedDict(
    [('solid',               (0, ())),
     ('densely dotted',      (0, (1, 1))),
     ('dashed',              (0, (5, 5))),
     ('densely dashed',      (0, (5, 1))),

     ('loosely dashdotted',  (0, (3, 10, 1, 10))),
     ('dashdotted',          (0, (3, 5, 1, 5))),
     ('densely dashdotted',  (0, (3, 1, 1, 1))),

     ('loosely dashdotdotted', (0, (3, 10, 1, 10, 1, 10))),
     ('dashdotdotted',         (0, (3, 5, 1, 5, 1, 5))),
     ('densely dashdotdotted', (0, (3, 1, 1, 1, 1, 1)))])

ax.plot(x, y0_add, linestyle=linestyles['solid'], linewidth=2)#, marker='d', markersize=5)
ax.plot(x, y0_sub, linestyle=linestyles['densely dashdotted'], linewidth=2)#, marker='s', markersize=5)
ax.plot(x, y0_mul, linestyle=linestyles['dashed'], linewidth=2)#, marker='s', markersize=5)
ax.plot(x, y0_div, linestyle=linestyles['loosely dashdotted'], linewidth=2)#, marker='s', markersize=5)
ax.plot(x, y0_res, linestyle=linestyles['densely dashdotdotted'], linewidth=2)#, marker='s', markersize=5)


strategies = ["Add", "Sub", "Mul", "Div", "Res"]
# fig.legend(strategy_plots, labels=strategies, loc=5, title="Strategies")
ax.legend(labels=strategies, loc=2)

plt.subplots_adjust(wspace=0.2)
plt.savefig("CalculatorActionsRandom.pdf", bbox_inches='tight')