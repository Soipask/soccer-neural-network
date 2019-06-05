import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
from datetime import datetime
import matplotlib.pyplot as plt

#some constants used for fine-tuning
MAXITER = 20
HIDDEN_1 = 25
HIDDEN_2 = 15
OUTPUT = 2
OUTMETHOD = 'softmax'
#OUTMETHOD = 'sigmoid'

logpath = "Logs\\"
filename = logpath + "ATP" + datetime.now().strftime('%Y%m%d%H%M%S') + OUTMETHOD + ".txt"
file = open(filename,"x");

csv = np.loadtxt('C:\\Users\\dsoip\\source\\repos\\soccer-neural-network2\\ATPDataMaker\\ATPDataMaker\\bin\\Debug\\atp.csv', delimiter="; ",dtype=np.dtype(str),encoding=None)
test = np.genfromtxt('C:\\Users\\dsoip\\source\\repos\\soccer-neural-network2\\ATPDataMaker\\ATPDataMaker\\bin\\Debug\\atpres.csv', delimiter=";")

file.write('Tennis\n\nPredicting season 2018\n\n')

text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)

INPUT = len(text) - OUTPUT
# file.write('Neural network, style: {}->{}->{}->{} ending function is {}\n EPOCHS: {}\n\n'.format(INPUT,HIDDEN_1,HIDDEN_2,OUTPUT,OUTMETHOD,EPOCHS))

train_data = csv[:,:-2]
train_labels = csv[:,-2:]

test_data = test[:,:-2]
test_labels = test[:,-2:]

np.set_printoptions(suppress=True)

file.write(str(text))
file.write("\n\n")

test_acc = []
test_acc_avg = 0
train_acc = []
train_acc_avg = 0
close_acc = 0
profit_avg = 0
faith_sorted_avg_best = 0

all_epochs = [10,20,40,50,80,100,150,200,300,400,500,750]
train_acc_by_epochs = []
test_acc_by_epochs = []

for EPOCHS in all_epochs:
	for k in range(MAXITER):
		seed(k)
		set_random_seed(k)
		print("\n\n{}\n".format(k))
		model = tf.keras.Sequential()
		model.add(layers.Dense(INPUT, activation='relu'))
		model.add(layers.Dense(HIDDEN_1, activation='relu'))
		model.add(layers.Dense(HIDDEN_2, activation='relu'))
		model.add(layers.Dense(OUTPUT, activation=OUTMETHOD))

		model.compile(optimizer=tf.train.GradientDescentOptimizer(0.001),
				  loss='categorical_crossentropy',
				  metrics=['accuracy'])

		hist = model.fit(train_data, train_labels, epochs=EPOCHS)

		results = model.evaluate(test_data, test_labels)
		
		print(results)
		
		file.write('Loss: {}, Accuracy: {}\n'.format(results[0],results[1]))
		test_acc.append(results[1])
		test_acc_avg += results[1]
		train_acc.append(hist.history['acc'][-1])
		train_acc_avg += hist.history['acc'][-1]
		raw_prediction = model.predict(test_data)
	
	test_acc_avg /= MAXITER
	train_acc_avg /= MAXITER
	train_acc_by_epochs.append(train_acc_avg)
	test_acc_by_epochs.append(test_acc_avg)

plt.plot(all_epochs,test_acc_by_epochs,"r",label = "Test Accuracy")
plt.plot(all_epochs,train_acc_by_epochs,"b", label ="Train Accuracy")
plt.xlabel("Epochs")
plt.ylabel("Accuracy (average per {} evaluations)".format(MAXITER))
plt.legend(loc = "lower right")
plt.show()
