import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
from datetime import datetime
import matplotlib.pyplot as plt
import sys

#some constants used for fine-tuning
MAXITER = 40
DROPOUT = 0.5
LEARNING_RATE = 0.005
BATCH_SIZE = 256
HIDDEN_1 = 15
HIDDEN_2 = 10
OUTPUT = 3
EPOCHS = 75
OUTMETHOD = 'softmax'
#OUTMETHOD = 'sigmoid'



'''
country = 'fra'
data = 'data2'
result = 'res'
path = 'C:\\Users\\dsoip\\source\\repos\\soccer-neural-network2\\Databases\\'
extension = '.csv'

'''
country = 'spa'
# version = '6f'
# mods = 'new9'
#data = 'data' + version + mods
data = "input"
#result = 'res' + version + mods
result = "res" + data
path = 'C:\\Users\\dsoip\\source\\repos\\soccer-neural-network2\\Databases\\'
extension = '.csv' 
#'''

country = sys.argv[1]

logpath = "Logs\\"
filename = logpath + country + datetime.now().strftime('%Y%m%d%H%M%S') + OUTMETHOD + ".txt"
file = open(filename,"x");

csv = np.genfromtxt(path + country + data + extension, delimiter=";",dtype=None,encoding=None)
test = np.genfromtxt(path + country + result + extension, delimiter=";")
# kinda_test = np.genfromtxt(path + country + "res5new8" + extension, delimiter=";")

file.write('{0} {1}{3}|{2}{3}\n\n'.format(country,data,result,extension))



text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)


train_data = csv[:,:-3]
train_labels = csv[:,-3:]

test_data = test[:,:-6]
test_labels = test[:,-6:-3]
test_bets = test[:,-3:]

# this area is just for fine-tuning the network ...
# number of games in round 1 (number of teams is x2, number of games in season n*(n-1))


season_games = 0
x = 0
while (csv[x][0] == 0) and (csv[x][1] == 0) and (csv[x][2] == 0):
	x += 1
	season_games += 1
teams_number = 2*season_games
season_games = teams_number * (teams_number - 1)

# choosing just some specific features

# [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46]
# feat - (GPG U home) = [0, 1, 2,  5, 6, 7,   20, 21, 22,  25, 26, 27,  30, 31, 32,  35, 36, 37,   40, 41, 42, 43,    44, 45, 46] <- 22 -> 15 -> 10 -> 3
# feat - (D U GPG U dFCS) = [0,  2,  5,  7,  10,  12,  15, 17, 20,  22,  25,  27,  30,  32,  35, 37,  40, 41, 42,   44, 45, 46] <- 19 -> 15 -> 10 -> 3
# feat - (F U GPG U dFCS) = [0, 1, 2,  5, 6, 7,  10, 11, 12,  15, 16, 17,  30, 31, 32,  35, 36, 37,   40, 41, 42,    44, 45, 46] <- 21 -> 15 -> 10 -> 3
features_chosen = [0, 1, 2,  5, 6, 7,   20, 21, 22,  25, 26, 27,  30, 31, 32,  35, 36, 37,   40, 41, 42, 43,    44, 45, 46]
print(features_chosen)
csv = csv[:,features_chosen]
train_data = csv[:,:-3]
train_labels = csv[:,-3:]

half_season = (int)(season_games/2)

test_data = train_data[(-1)*half_season:,:]
test_labels = train_labels[(-1)*half_season:,:]

train_data = train_data[half_season:(-1)*half_season,:]
train_labels = train_labels[half_season:(-1)*half_season,:]

'''
test = kinda_test
test_bets = test[:,-3:]
test = test[:,features_chosen]
test_data = test[:,:-3]
test_labels = test[:,-3:]
'''

print(test_data.shape, test_labels.shape, train_data.shape, train_labels.shape)

# ... until here

INPUT = len(text) - OUTPUT
file.write('Neural network, style: {}->DROPOUT({})->{}->{}->{} ending function is {}\n EPOCHS: {}\nNETWORK TUNING\nRATE = {}; BATCH_SIZE={}\n'.format(
	INPUT,DROPOUT,HIDDEN_1,HIDDEN_2,OUTPUT,OUTMETHOD,EPOCHS,LEARNING_RATE,BATCH_SIZE))


np.set_printoptions(suppress=True)

file.write(str(text))
file.write("\n\n")
#print(test_data[:5,])
#print(test_labels[:5,])
#print(test_bets[:5,])

test_acc_arr = []
test_acc_avg = 0
train_acc_arr = []
train_acc_avg = 0
close_acc = 0
profit_avg = 0
faith_sorted_avg_best = 0


for k in range(MAXITER):
	seed(k)
	set_random_seed(k)
	print("\n\n{}\n".format(k))
	model = tf.keras.Sequential()
	model.add(layers.Dense(INPUT, activation='relu'))
	model.add(layers.Dropout(DROPOUT))
	model.add(layers.Dense(HIDDEN_1, activation='relu'))
	model.add(layers.Dense(HIDDEN_2, activation='relu'))
	model.add(layers.Dense(OUTPUT, activation=OUTMETHOD))

	model.compile(optimizer=tf.train.GradientDescentOptimizer(LEARNING_RATE),
              loss='categorical_crossentropy',
              metrics=['accuracy'])

	hist = model.fit(train_data, train_labels, epochs=EPOCHS, batch_size=BATCH_SIZE)

	results = model.evaluate(test_data, test_labels)
	
	file.write('Loss: {}, Accuracy: {}, Test accuracy: {}\n'.format(results[0],hist.history['acc'][-1],results[1]))
	test_acc_arr.append(results[1])
	test_acc_avg += results[1]
	train_acc_arr.append(hist.history['acc'][-1])
	train_acc_avg += hist.history['acc'][-1]
	raw_prediction = model.predict(test_data)
	print('Loss: {}, Accuracy: {}, Test accuracy: {}\n'.format(results[0],hist.history['acc'][-1],results[1]))
	
	'''
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
		# print ("{} {} {}\n".format(predictions[j],test_labels[j],bool))
		if bool == True :
			pred_acc += 1
			profit_full += test_bets[j][max]
			
	pred_acc /= len(predictions)
	profit_full -= len(predictions)
	
	# print(results)
	# print(pred_acc)
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
			# print(test_bets[n][maximum])
		# print("{} -> {} | {} \n".format(predictions[n],test_labels[n],test_bets[n]))

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
	
	# print(faith_sorted)
	# print()
	faith_sorted = [faith_sorted[n] for n in range(len(faith_sorted)) if faith_sorted[n][4] > 0.19]
	# print(faith_sorted)
	
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
		# print(to_print)
		if(faith_sorted[n][5] == True):
			faith_right += 1
		faith_sorted_acc.append((n + 1, faith_right / (n + 1), prof))
	maximum = np.max(faith_sorted_acc[int(len(faith_sorted)/10) : ])
	where = np.where(faith_sorted_acc == maximum)
	# print("Threshold faith is {} at {} games".format(maximum,where))
	prof_sorted = sorted(faith_sorted_acc,reverse = True, key = lambda f: f[2])
	print(prof)
	print("max profit is {}".format(prof_sorted[0][2]))
	faith_sorted_avg_best += maximum
	
	file.write("\n")
	
	# plt.plot([faith_sorted[n][4] for n in range(len(faith_sorted))],[faith_sorted_acc[n][2] for n in range(len(faith_sorted_acc))], label = "Random seed {}".format(k))

# accuracy to faith rank
# plt.plot(range(1,len(faith_sorted)+1),faith_sorted_acc)		

# accuracy to faith
# plt.plot([faith_sorted[n][4] for n in range(len(faith_sorted))],[faith_sorted_acc[n][1] for n in range(len(faith_sorted_acc))],"r")

# profit to faith rank
# plt.plot(range(1,len(faith_sorted)+1),[faith_sorted_acc[n][2] for n in range(len(faith_sorted_acc))])

# profit to faith

# plt.xlabel("Faith in betting")
# plt.ylabel("Profit")
# plt.legend(loc = "lower right")

# plt.show()	
		'''
plt.plot(range(MAXITER),train_acc_arr,"r",label = "Train acc")
plt.plot(range(MAXITER),test_acc_arr,"b", label = "Test acc")
plt.legend(loc = "lower right")

test_acc_avg /= MAXITER
train_acc_avg /= MAXITER
# close_acc /= MAXITER
# profit_avg /= MAXITER
# faith_sorted_avg_best /= MAXITER


file.write("Average training accuracy is {}\n".format(train_acc_avg))
file.write("Average test accuracy is {}\n\n".format(test_acc_avg))
# file.write("Average close game accuracy is {} with profit of {}\n\n".format(close_acc,profit_avg))

print("Average training accuracy is {}\n".format(train_acc_avg))
print("Average test accuracy is {}\n\n".format(test_acc_avg))
# print("Average close game accuracy is {} with profit of {}\n\n".format(close_acc,profit_avg))
plt.show()


