import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from numpy.random import seed
from tensorflow import set_random_seed
from datetime import datetime
import matplotlib.pyplot as plt

#some hyperparameters used for fine-tuning
MAXITER = 40
DROPOUT = 0.5
LEARNING_RATE = 0.005
BATCH_SIZE = 256
HIDDEN = 25
OUTPUT = 2
EPOCHS = 100
OUTMETHOD = 'softmax'
#OUTMETHOD = 'sigmoid'

logpath = "Logs\\"
filename = logpath + "RNN" + "ATP" + datetime.now().strftime('%Y%m%d%H%M%S') + OUTMETHOD + ".txt"
file = open(filename,"x");

csv = np.loadtxt('atp.csv', delimiter="; ",dtype=np.dtype(str),encoding=None)
test = np.genfromtxt('atpres.csv', delimiter=";")

file.write('Tennis\n\nPredicting season 2017\n\n')

# training and testing
# selecting features

# season 				form					surface						surfaceform				mutual					rank		surface		score		winner
# [0, 1, 2, 3, 4, 5,   6, 7, 8, 9, 10, 11,   12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,  27, 28, 29,   30, 31,   32, 33, 34,   35, 36,   37, 38]
# N - S: [0, 1, 2, 3, 4, 5,   6, 7, 8, 9, 10, 11,   12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,  27, 28, 29,   30, 31,   35, 36,   37, 38]
# N - S - score   : [0, 1, 2, 3, 4, 5,   6, 7, 8, 9, 10, 11,   12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,  27, 28, 29,   30, 31,    37, 38]
# N - S - F - dSFS: [0, 1, 2, 3, 4, 5,     12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,  27, 28, 29,   30, 31,  35,   37, 38]
# N – S – SM	  : [0, 1, 2, 3, 4, 5,   6, 7, 8, 9, 10, 11,   12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,    30, 31,   35, 36,   37, 38]
# N – S – F – GdpS: [0, 1,  3, 4,      12, 13,  15, 16,    18, 19, 21, 22,    24, 25,   27, 28,    30, 31,   35, 36,   37, 38]
# N – S – FL	  : [0,  2, 3, 5,   6,  8, 9,  11,   12,  14, 15,  17,   18,  20, 21, 23,   24, 25, 26,  27, 28, 29,   30, 31,   35, 36,   37, 38]
# N – S – F – SF  : [0, 1, 2, 3, 4, 5,    12, 13, 14, 15, 16, 17,     24, 25, 26,  27, 28, 29,   30, 31,   35, 36,   37, 38]
features_chosen = [0, 1, 2, 3, 4, 5,     12, 13, 14, 15, 16, 17,   18, 19, 20, 21, 22, 23,   24, 25, 26,  27, 28, 29,   30, 31,  35,   37, 38]
csv = csv[:,features_chosen]
test_bets = test[:,-2:]

test = test[:,features_chosen]
# ...

text = csv[0,:]
csv = csv[1:,:]
csv = csv.astype(np.float)
test_data = test[:,:-2]
test_labels = test[:,-2:]
# test_bets = test[:,-2:]
train_data = csv[:,:-2]
train_labels = csv[:,-2:]

# another hyperparameters, need to know OUTPUT
INPUT = len(text) - OUTPUT
LSTM_NEURONS = 25
LSTM_TIMESTEPS = 31
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
# print(lstm_head_train_data[len(lstm_head_train_data) - 1][:5])
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
	model.add(layers.Dense(HIDDEN, activation='relu'))
	model.add(layers.Dense(OUTPUT, activation=OUTMETHOD))

	# model.compile(optimizer=tf.train.GradientDescentOptimizer(LEARNING_RATE),
	model.compile(optimizer=tf.train.AdamOptimizer(LEARNING_RATE),
              loss='categorical_crossentropy',
              metrics=['accuracy'])

	hist = model.fit(lstm_train_data, train_labels, epochs=EPOCHS, batch_size=BATCH_SIZE)

	results = model.evaluate(lstm_test_data, test_labels)
	
	file.write('Loss: {}, Accuracy: {}, Test accuracy: {}\n'.format(results[0],hist.history['acc'][-1],results[1]))
	if len(train_acc) == 0:
		if hist.history['acc'][-1] < 0.55:
			print("My mistake")
			mistake += 1
			continue
	elif hist.history['acc'][-1] < train_acc[-1] - 0.15:
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
		predictions.append([0,0])
		max = np.argmax(raw_prediction[j])
		for i in range(OUTPUT):
			predictions[j][i] = 0
		predictions[j][max] = 1
		bool = (predictions[j] == test_labels[j]).all()
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
	close_games_nrs = [n for n in range(len(test_bets)) if abs(test_bets[n][0] - test_bets[n][1]) <= 1]

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
		faith = abs(raw_prediction[n][0] - raw_prediction[n][1])
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
	
	faith_sorted = [faith_sorted[n] for n in range(len(faith_sorted)) if faith_sorted[n][4] > 0.3]
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

plt.show()	
		

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