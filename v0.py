import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
from datetime import datetime
import matplotlib.pyplot as plt

#some constants used for fine-tuning
MAXITER = 30
HIDDEN_1 = 25
HIDDEN_2 = 15
OUTPUT = 3
EPOCHS = 1500
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



logpath = "Logs\\"
filename = logpath + country + datetime.now().strftime('%Y%m%d%H%M%S') + OUTMETHOD + ".txt"
file = open(filename,"x");

csv = np.genfromtxt(path + country + data + extension, delimiter=";",dtype=None,encoding=None)
test = np.genfromtxt(path + country + result + extension, delimiter=";")

file.write('{0} {1}{3}|{2}{3}\n\n'.format(country,data,result,extension))


text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)

INPUT = len(text) - OUTPUT
file.write('Neural network, style: {}->{}->{}->{} ending function is {}\n EPOCHS: {}\n\n'.format(INPUT,HIDDEN_1,HIDDEN_2,OUTPUT,OUTMETHOD,EPOCHS))

train_data = csv[:,:-3]
train_labels = csv[:,-3:]

test_data = test[:,:-6]
test_labels = test[:,-6:-3]
test_bets = test[:,-3:]

np.set_printoptions(suppress=True)

file.write(str(text))
file.write("\n\n")
#print(test_data[:5,])
#print(test_labels[:5,])
#print(test_bets[:5,])

res = []
train_acc = 0
acc = []
test_acc = 0
close_acc = 0
profit_avg = 0
faith_sorted_avg_best = 0


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
	
	file.write('Loss: {}, Accuracy: {}\n'.format(results[0],results[1]))
	res.append(results[1])
	train_acc += results[1]
	acc.append(hist.history['acc'][-1])
	test_acc += hist.history['acc'][-1]
	raw_prediction = model.predict(test_data)
	print(raw_prediction)
	
	pred_acc = 0
	predictions = []
	profit_full = 0
	
	for j in range(len(raw_prediction)):
		predictions.append([0,0,0])
		max = np.argmax(raw_prediction[j])
		for i in range(OUTPUT):
			predictions[j][i] = 0
		predictions[j][max] = 1
		bool = (predictions[j] == test_labels[j]).all()
		print ("{} {} {}\n".format(predictions[j],test_labels[j],bool))
		if bool == True :
			pred_acc += 1
			profit_full += test_bets[j][max]
			
	pred_acc /= len(predictions)
	profit_full -= len(predictions)
	
	print(results)
	print(pred_acc)
	print(profit_full)
	file.write("Expected profit when bet on all games separately: {}\n".format(profit_full))
	
	# close games
	close = 0;
	profit = 0;
	close_games_nrs = [n for n in range(len(test_bets)) if abs(test_bets[n][0] - test_bets[n][2]) <= 1]

	for n in close_games_nrs:
		if(predictions[n] == test_labels[n]).all():
			close += 1;
			maximum = np.argmax(predictions[n])
			profit += test_bets[n][maximum]
			print(test_bets[n][maximum])
		print("{} -> {} | {} \n".format(predictions[n],test_labels[n],test_bets[n]))

	close /= len(close_games_nrs)
	close_acc += close
	
	close_result = "Close game pct: {}, won: {} in {} games, that's profit of {}".format(close,profit,len(close_games_nrs),profit - len(close_games_nrs))
	profit -= len(close_games_nrs)
	profit_avg += profit
	file.write(close_result)
	file.write("\n")
	print(close_result)
		

	# games with the best faith in prediction
	
	faith_tuples = []
	for n in range(len(raw_prediction)):
		max = 0
		max_2 = 0
		for m in range(3):
			if raw_prediction[n][m] > max :
				max_2 = max
				max = raw_prediction[n][m]
			elif raw_prediction[n][m] > max_2 :
				max_2 = raw_prediction[n][m]
		faith = max - max_2
		truth = (predictions[n] == test_labels[n]).all()
		faith_tuples.append((raw_prediction[n],
			predictions[n],
			test_labels[n],
			test_bets[n],
			faith,
			truth))

	faith_sorted = sorted(faith_tuples, reverse = True, key = lambda match: match[4])
	faith_sorted_acc = []
	faith_right = 0
	prof = 0
	for n in range(len(faith_sorted)):
		prof -= 1
		if (faith_sorted[n][1] == faith_sorted[n][2]).all() :
			maximum = np.argmax(faith_sorted[n][1])
			prof += faith_sorted[n][3][maximum]
		to_print = "{} {} {} {} {} {} {}".format(
			faith_sorted[n][0],
			faith_sorted[n][1],
			faith_sorted[n][2],
			faith_sorted[n][3],
			faith_sorted[n][4],
			faith_sorted[n][5],
			prof)
		print(to_print)
		if(faith_sorted[n][5] == True):
			faith_right += 1
		faith_sorted_acc.append((n + 1, faith_right / (n + 1), prof))
	maximum = np.max(faith_sorted_acc[int(len(faith_sorted)/10) : ])
	where = np.where(faith_sorted_acc == maximum)
	print("Threshold faith is {} at {} games".format(maximum,where))
	prof_sorted = sorted(faith_sorted_acc,reverse = True, key = lambda f: f[2])
	print("max profit is {}".format(prof_sorted[0][2]))
	faith_sorted_avg_best += maximum
	
	file.write("\n")

# accuracy to faith rank
# plt.plot(range(1,len(faith_sorted)+1),faith_sorted_acc)		

# accuracy to faith
# plt.plot([faith_sorted[n][4] for n in range(len(faith_sorted))],[faith_sorted_acc[n][1] for n in range(len(faith_sorted_acc))],"r")

# profit to faith rank
# plt.plot(range(1,len(faith_sorted)+1),[faith_sorted_acc[n][2] for n in range(len(faith_sorted_acc))])

# profit to faith
plt.plot([faith_sorted[n][4] for n in range(len(faith_sorted))],[faith_sorted_acc[n][2] for n in range(len(faith_sorted_acc))],"g")

# plt.show()	
		

train_acc /= MAXITER
test_acc /= MAXITER
close_acc /= MAXITER
profit_avg /= MAXITER
faith_sorted_avg_best /= MAXITER


file.write("Average training accuracy is {}\n".format(train_acc))
file.write("Average test accuracy is {}\n\n".format(test_acc))
file.write("Average close game accuracy is {} with profit of {}\n\n".format(close_acc,profit_avg))


