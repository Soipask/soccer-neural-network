import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
from datetime import datetime
import matplotlib.pyplot as plt
import sys
#some hyperparameters used for fine-tuning
MAXITER = 40
DROPOUT = 0.5
LEARNING_RATE = 0.005
BATCH_SIZE = 16
HIDDEN = 10
OUTPUT = 3
EPOCHS = 70
OUTMETHOD = 'softmax'
#OUTMETHOD = 'sigmoid'

#'''
data = "input"
result = "res" + data
path = 'C:\\Users\\dsoip\\source\\repos\\soccer-neural-network2\\Databases\\'
extension = '.csv' 
#'''
country = sys.argv[1]
logpath = "Logs\\"
filename = logpath + "RNN" + country + datetime.now().strftime('%Y%m%d%H%M%S') + OUTMETHOD + ".txt"
file = open(filename,"x");

csv = np.genfromtxt(path + country + data + extension, delimiter=";",dtype=None,encoding=None)
test = np.genfromtxt(path + country + result + extension, delimiter=";")
kinda_test = np.genfromtxt(path + country + "res5new8" + extension, delimiter=";")
file.write('{0} {1}{3}|{2}{3}\n\n'.format(country,data,result,extension))

# training and testing
# number of games in round 1 (number of teams is x2, number of games in season n*(n-1))


season_games = 0
x = 0
while (csv[x][0] == 0) and (csv[x][1] == 0) and (csv[x][2] == 0):
	x += 1
	season_games += 1
teams_number = 2*season_games
season_games = teams_number * (teams_number - 1)

# selecting features

# [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46]
# feat - (GPG U home) = [0, 1, 2,  5, 6, 7,   20, 21, 22,  25, 26, 27,  30, 31, 32,  35, 36, 37,   40, 41, 42, 43,    44, 45, 46] <- 22 -> 15 -> 10 -> 3
# feat - (D U GPG U dFCS) = [0,  2,  5,  7,  10,  12,  15, 17, 20,  22,  25,  27,  30,  32,  35, 37,  40, 41, 42,   44, 45, 46] <- 19 -> 15 -> 10 -> 3
# feat - (F U GPG U dFCS) = [0, 1, 2,  5, 6, 7,  10, 11, 12,  15, 16, 17,  30, 31, 32,  35, 36, 37,   40, 41, 42,    44, 45, 46] <- 21 -> 15 -> 10 -> 3
features_chosen = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46]
print(features_chosen)
csv = csv[:,features_chosen]
train_data = csv[:,:-3]
train_labels = csv[:,-3:]

half_season = (int)(season_games/2)

test_data = train_data[(-1)*half_season:,:]
test_labels = train_labels[(-1)*half_season:,:]

train_data = train_data[half_season:(-1)*half_season,:]
train_labels = train_labels[half_season:(-1)*half_season,:]

#'''
test = kinda_test
test_bets = test[:,-3:]
test = test[:,features_chosen]
test_data = test[:,:-3]
test_labels = test[:,-3:]
#'''

print(test_data.shape, test_labels.shape, train_data.shape, train_labels.shape)

# ... until here

text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)
test_data = test[:,:-3]
test_labels = test[:,-3:]
# test_bets = test[:,-2:]
train_data = csv[:,:-3]
train_labels = csv[:,-3:]

# another hyperparameters, need to know OUTPUT
INPUT = len(text) - OUTPUT
LSTM_NEURONS = 25
LSTM_TIMESTEPS = 40
LSTM_FEATURES = INPUT
file.write('LSTM Neural network, style: {}->LSTM( Neurons:{} Timesteps: {} Features:{})->{} ending function is {}\n EPOCHS: {}\n\n'.format(INPUT,LSTM_NEURONS, LSTM_TIMESTEPS, LSTM_FEATURES, OUTPUT,OUTMETHOD,EPOCHS))
#...


print(text)
print(test_data.shape, test_labels.shape, train_data.shape, train_labels.shape)

np.set_printoptions(suppress=True)

file.write(str(text))
file.write("\n\n")

lstm_train_data = []
lstm_train_labels = []
lstm_test_data = []
lstm_test_labels = []

lstm_head_train_data = []
lstm_head_train_labels = []
lstm_head_test_data = []
lstm_head_test_labels = []


# getting training and testing data

for i in range(LSTM_TIMESTEPS, len(train_data) + 1):
	lstm_train_data.append(train_data[i - LSTM_TIMESTEPS : i,:])
for i in range(LSTM_TIMESTEPS, len(test_data) + 1):
	lstm_test_data.append(test_data[i - LSTM_TIMESTEPS : i,:])
for i in range(1,LSTM_TIMESTEPS):
	false = []
	f_t = []
	for j in range(LSTM_TIMESTEPS - i):
		data = []
		h_test = []
		for k in range(len(train_data[0])):
			data.append(0)
		false.append(np.asarray(data))
		if (i > LSTM_TIMESTEPS - 2):
			print((-1)*(LSTM_TIMESTEPS - i) + j)
		f_t.append(np.asarray(train_data[(-1)*(LSTM_TIMESTEPS - i) + j,:]))
	
	for j in range(i):
		false.append(train_data[j,:])
		f_t.append(test_data[j,:])

	lstm_head_train_data.append(np.asarray(false))
	lstm_head_test_data.append(np.asarray(f_t))

lstm_train_data = np.array(lstm_train_data)
lstm_head_train_data = np.array(lstm_head_train_data)
lstm_train_labels = np.array(lstm_train_labels)
lstm_test_data = np.array(lstm_test_data)
lstm_head_test_data = np.array(lstm_head_test_data)
lstm_test_labels = np.array(lstm_test_labels)


print(lstm_head_train_data.shape, lstm_train_data.shape)
print(lstm_head_train_data[len(lstm_head_train_data) - 1][:5])
print("W")
print(lstm_train_data[0][:5])
print("T")
print(lstm_train_data[len(lstm_train_data) - 1][:5])
print("F")
print(lstm_head_test_data[0][:5])

lstm_train_data = np.concatenate((lstm_head_train_data, lstm_train_data), axis=0)
lstm_test_data = np.concatenate((lstm_head_test_data, lstm_test_data), axis=0)

test_acc = []
test_acc_avg = 0
train_acc = []
train_acc_avg = 0
close_acc = 0
profit_avg = 0
faith_sorted_avg_best = 0

mistake = 0

for k in range(MAXITER):
	seed(k)
	set_random_seed(k)
	print("\n\n{}\n".format(k))
	model = tf.keras.Sequential()
	model.add(layers.LSTM(LSTM_NEURONS, input_shape = (LSTM_TIMESTEPS,LSTM_FEATURES)))
	model.add(layers.Dropout(DROPOUT))
	# model.add(layers.Dense(HIDDEN, activation='relu'))
	model.add(layers.Dense(OUTPUT, activation=OUTMETHOD))
#----------------------------------------------------------------------------------------------
	# model.compile(optimizer=tf.train.GradientDescentOptimizer(LEARNING_RATE),
	model.compile(optimizer=tf.train.AdamOptimizer(LEARNING_RATE),
              loss='categorical_crossentropy',
              metrics=['accuracy'])

	hist = model.fit(lstm_train_data, train_labels, epochs=EPOCHS, batch_size=BATCH_SIZE)

	results = model.evaluate(lstm_test_data, test_labels)
	
	file.write('Loss: {}, Accuracy: {}, Test accuracy: {}\n'.format(results[0],hist.history['acc'][-1],results[1]))

	if hist.history['acc'][-1] < 0.45:
			print("My mistake")
			mistake += 1
			continue
		
	test_acc.append(results[1])
	test_acc_avg += results[1]
	train_acc.append(hist.history['acc'][-1])
	train_acc_avg += hist.history['acc'][-1]
	raw_prediction = model.predict(lstm_test_data)
	print('Loss: {}, Accuracy: {}, Test accuracy: {}\n'.format(results[0],hist.history['acc'][-1],results[1]))
	
	
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
	
	faith_sorted = [faith_sorted[n] for n in range(len(faith_sorted)) if faith_sorted[n][4] > 0.19]
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
	if (len(faith_sorted) > 0):
		maximum = np.max(faith_sorted_acc[int(len(faith_sorted)/10) : ])
		where = np.where(faith_sorted_acc == maximum)
		prof_sorted = sorted(faith_sorted_acc,reverse = True, key = lambda f: f[2])
		print(prof)
		print("max profit is {}".format(prof_sorted[0][2]))
		faith_sorted_avg_best += maximum
	else:
		print(raw_prediction)
	
	file.write("\n")
	

# accuracy to faith rank
# plt.plot(range(1,len(faith_sorted)+1),faith_sorted_acc)		

# accuracy to faith
# plt.plot([faith_sorted[n][4] for n in range(len(faith_sorted))],[faith_sorted_acc[n][1] for n in range(len(faith_sorted_acc))],"r")

# profit to faith rank
# plt.plot(range(1,len(faith_sorted)+1),[faith_sorted_acc[n][2] for n in range(len(faith_sorted_acc))])

# profit to faith

plt.plot(range(MAXITER - mistake),test_acc,"r", label = "Test accuracy")
plt.plot(range(MAXITER - mistake),train_acc,"b", label = "Train accuracy")

plt.xlabel("Random seed")
plt.ylabel("Accuracy")
plt.legend(loc = "lower right")


		

test_acc_avg /= MAXITER - mistake
train_acc_avg /= MAXITER - mistake
close_acc /= MAXITER - mistake
profit_avg /= MAXITER - mistake
faith_sorted_avg_best /= MAXITER - mistake


file.write("Average training accuracy is {}\n".format(train_acc_avg))
file.write("Average test accuracy is {}\n\n".format(test_acc_avg))
file.write("Average close game accuracy is {} with profit of {}\n\n".format(close_acc,profit_avg))

print("Average training accuracy is {}\n".format(train_acc_avg))
print("Average test accuracy is {}\n\n".format(test_acc_avg))
print("Average close game accuracy is {} with profit of {}\n\n".format(close_acc,profit_avg))
plt.show()