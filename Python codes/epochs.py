import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
import matplotlib.pyplot as plt

#some constants used for fine-tuning
MAXITER = 15
HIDDEN_1 = 25
HIDDEN_2 = 15
OUTPUT = 3
OUTMETHOD = 'softmax'
#OUTMETHOD = 'sigmoid'

'''
country = 'fra'
data = 'data2'
result = 'res'
path = ''
extension = '.csv'

'''
country = 'eng'
data = 'data4dfswofs'
result = 'res4dfswofs'
path = ''
extension = '.csv' 
#'''




csv = np.genfromtxt(path + country + data + extension, delimiter=";",dtype=None,encoding=None)
test = np.genfromtxt(path + country + result + extension, delimiter=";")


text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)

INPUT = len(text) - OUTPUT

train_data = csv[:,:-3]
train_labels = csv[:,-3:]

test_data = test[:,:-6]
test_labels = test[:,-6:-3]
test_bets = test[:,-3:]

#

all_epochs = [10,20,30,40,50,60,70,80,90,100,120,140,160,180,200,250,300,350,400,450,500,600,700,800,900,1000,1200,1400,1600,1800,2000]
train_acc_by_epochs = []
test_acc_by_epochs = []

for EPOCHS in all_epochs:

	res = []
	train_acc = 0

	acc = []
	test_acc = 0
	for n in range(MAXITER):
		seed(n)
		set_random_seed(n)
		print("\n\n{}\n".format(n))
		model = tf.keras.Sequential()
		model.add(layers.Dense(INPUT, activation='relu'))
		model.add(layers.Dense(HIDDEN_1, activation='relu'))
		model.add(layers.Dense(HIDDEN_2, activation='relu'))
		model.add(layers.Dense(OUTPUT, activation=OUTMETHOD))

		model.compile(optimizer=tf.train.GradientDescentOptimizer(0.001),
				  loss='categorical_crossentropy',
				  metrics=['accuracy'])

		hist = model.fit(train_data, train_labels, epochs=EPOCHS, batch_size=256)

		results = model.evaluate(test_data, test_labels)
		
		res.append(results[1])
		train_acc += results[1]
		acc.append(hist.history['acc'][-1])
		test_acc += hist.history['acc'][-1]
		predictions = model.predict(test_data)
		
		pred_acc = 0
		
		for j in range(len(predictions)):
			max = np.argmax(predictions[j])
			for i in range(OUTPUT):
				predictions[j][i] = 0
			predictions[j][max] = 1
			
			if (predictions[j] == test_labels[j]).all():
				pred_acc += 1
		pred_acc /= len(predictions)
		

	train_acc /= len(res)
	test_acc /= len(acc)
	
	train_acc_by_epochs.append(train_acc)
	test_acc_by_epochs.append(test_acc)

plt.plot(all_epochs,test_acc_by_epochs,"r",all_epochs,train_acc_by_epochs,"b")
plt.show()



