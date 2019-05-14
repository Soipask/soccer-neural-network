import numpy as np
import tensorflow as tf
from tensorflow.keras import layers
import pandas as pd
import matplotlib.pyplot as plt


csv = np.genfromtxt('engdata4dfswofs.csv', delimiter=";",dtype=None,encoding=None)
test = np.genfromtxt('frares.csv', delimiter=";")

text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)

train_data = csv[:,:-3]
train_labels = csv[:,-3:]

test_data = test[:,:-6]
test_labels = test[:,-6:-3]
test_bets = test[:,-3:]

dt = pd.DataFrame(data=csv)
dt.columns = text

print(text)

arr = len(text)
print(arr)

for i in range(arr):
	print(i, text[i])

plt.matshow(dt.corr())
plt.show()

#0,1,2,5,6,7,10,11,12,15,16,17,25,26,32,33

