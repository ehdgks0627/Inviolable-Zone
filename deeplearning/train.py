import random
import os
import copy
import tensorflow as tf

def search(dirname, y_label):
    try:
        filenames = os.listdir(dirname)
        for filename in filenames:
            full_filename = os.path.join(dirname, filename)
            try:
                if os.path.isdir(full_filename):
                    search(full_filename, y_label)
                else:
                    with open(full_filename, "rb") as f:
                        x.append(list(map(int, f.read()[:input_size])))
                        y.append(y_label)
            except:
                continue
    except PermissionError:
        pass

input_dirs = ["data\\origin",
              "data\\infection\\Jigsaw",
              "data\\infection\\TeslaCrypt"]
        #"data\\infection\\CerBer",
        #"data\\infection\\Cryptowall",
        #"data\\infection\\Locky",
        #"data\\infection\\Mamba",
        #"data\\infection\\Matsnu",
        #"data\\infection\\Petrwrap",
        #"data\\infection\\Petya",
        #"data\\infection\\Radamant",
        #"data\\infection\\Rex",
        #"data\\infection\\Satana",
        #"data\\infection\\Vipasana",
        #"data\\infection\\WannaCry"]

# 200 200 0.00001

input_size = 201
net_size = input_size
output_size = len(input_dirs)
learning_rate = 0.00001
epoch_size = 3000
test_rate = 0.7

x_datas = []
y_datas = []
x_test_datas = []
y_test_datas = []

for idx, input_dir in enumerate(input_dirs):
    x = []
    y = []
    search(input_dir, [0] * idx + [1] + [0] * (output_size - idx - 1))
    buck = int(len(x) * test_rate)
    x_datas += copy.deepcopy(x[:buck])
    y_datas += copy.deepcopy(y[:buck])
    x_test_datas += copy.deepcopy(x[buck:])
    y_test_datas += copy.deepcopy(y[buck:])

tf.set_random_seed(1004)

X = tf.placeholder(tf.float32, [None, input_size])
Y = tf.placeholder(tf.float32, [None, output_size])

keep_prob = tf.placeholder(tf.float32)

W1 = tf.get_variable("W1", shape=[input_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b1 = tf.Variable(tf.random_normal([net_size]))
L1 = tf.nn.relu(tf.matmul(X, W1) + b1)
L1 = tf.nn.dropout(L1, keep_prob=keep_prob)

W2 = tf.get_variable("W2", shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b2 = tf.Variable(tf.random_normal([net_size]))
L2 = tf.nn.relu(tf.matmul(L1, W2) + b2)
L2 = tf.nn.dropout(L2, keep_prob=keep_prob)

W3 = tf.get_variable("W3", shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b3 = tf.Variable(tf.random_normal([net_size]))
L3 = tf.nn.relu(tf.matmul(L2, W3) + b3)
L3 = tf.nn.dropout(L3, keep_prob=keep_prob)

W4 = tf.get_variable("W4", shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b4 = tf.Variable(tf.random_normal([net_size]))
L4 = tf.nn.relu(tf.matmul(L3, W4) + b4)
L4 = tf.nn.dropout(L4, keep_prob=keep_prob)

W5 = tf.get_variable("W5", shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b5 = tf.Variable(tf.random_normal([net_size]))
L5 = tf.nn.relu(tf.matmul(L4, W5) + b5)
L5 = tf.nn.dropout(L5, keep_prob=keep_prob)

W6 = tf.get_variable("W6", shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
b6 = tf.Variable(tf.random_normal([net_size]))
L6 = tf.nn.relu(tf.matmul(L5, W6) + b6)
L6 = tf.nn.dropout(L6, keep_prob=keep_prob)

W7 = tf.get_variable("W7", shape=[net_size, output_size], initializer=tf.contrib.layers.xavier_initializer())
b7 = tf.Variable(tf.random_normal([output_size]))
hypothesis = tf.sigmoid(tf.matmul(L6, W7) + b7)

cost = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(logits=hypothesis, labels=Y))
optimizer = tf.train.AdamOptimizer(learning_rate=learning_rate).minimize(cost)

sess = tf.Session()
sess.run(tf.global_variables_initializer())

for epoch in range(epoch_size):
    feed_dict = {X: x_datas, Y: y_datas, keep_prob: 1}
    c, _, h, _y = sess.run([cost, optimizer, hypothesis, Y], feed_dict=feed_dict)
    if epoch % 100 == 0:
        print('Epoch:', '%04d' % (epoch), 'cost =', '{:.9f}'.format(c))

print('Learning Finished!')

correct_prediction = tf.equal(tf.argmax(hypothesis, 1), tf.argmax(Y, 1))
accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))
print('Prediction:', sess.run(correct_prediction, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1}))
print('Accuracy:', sess.run(accuracy, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1}))
