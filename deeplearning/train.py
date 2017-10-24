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
                        data = f.read()
                        x.append(list(map(int, data[:int(input_size/2)] + data[int(-input_size/2):])))
                        y.append(y_label)
            except:
                continue
    except PermissionError:
        pass


OK = [1, 0]
INFECTED = [0, 1]

input_dirs = [("data\\origin", OK),
              ("data\\infection\\Jigsaw", INFECTED),
              ("data\\infection\\TeslaCrypt", INFECTED),
              ("data\\infection\\WannaCry", INFECTED),
              ("data\\infection\\CerBer", INFECTED),
              ("data\\infection\\Vipasana", INFECTED), ]
# "data\\infection\\Cryptowall",
# "data\\infection\\Locky",
# "data\\infection\\Mamba",
# "data\\infection\\Matsnu",
# "data\\infection\\Petrwrap",
# "data\\infection\\Petya",
# "data\\infection\\Radamant",
# "data\\infection\\Rex",
# "data\\infection\\Satana",


# 200 -200 200 0.000001

input_size = 400
net_size = 3000
# output_size = len(input_dirs)
output_size = 2
learning_rate = 0.0000005
epoch_size = 500
test_rate = 0.7
layer_count = 7
dropout_rate = 0.7

x_datas = []
y_datas = []
x_test_datas = []
y_test_datas = []

tf.set_random_seed(1004)

for input_size in range(200, 300, 400):
    for MAX_LAYER in range(3, 8):
        tf.reset_default_graph()
        Ls = []
        X = tf.placeholder(tf.float32, [None, input_size])
        Y = tf.placeholder(tf.float32, [None, output_size])

        keep_prob = tf.placeholder(tf.float32)
        for idx, input_dir in enumerate(input_dirs):
            x = []
            y = []
            search(input_dir[0], input_dir[1])
            # search(input_dir, [0] * idx + [1] + [0] * (output_size - idx - 1))
            buck = int(len(x) * test_rate)

            x_datas += copy.deepcopy(x[:buck])
            y_datas += copy.deepcopy(y[:buck])
            x_test_datas += copy.deepcopy(x[buck:])
            y_test_datas += copy.deepcopy(y[buck:])

            #if idx < 2:
            #    x_datas += copy.deepcopy(x)
            #    y_datas += copy.deepcopy(y)
            #else:
            #    x_test_datas += copy.deepcopy(x)
            #    y_test_datas += copy.deepcopy(y)

        for layer_count in range(1, MAX_LAYER):
            if layer_count == 1:
                W = tf.get_variable("W%d"%(layer_count), shape=[input_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
                b = tf.Variable(tf.random_normal([net_size]))
            elif layer_count == MAX_LAYER - 1:
                W = tf.get_variable("W%d"%(layer_count), shape=[net_size, output_size], initializer=tf.contrib.layers.xavier_initializer())
                b = tf.Variable(tf.random_normal([output_size]))
            else:
                W = tf.get_variable("W%d"%(layer_count), shape=[net_size, net_size], initializer=tf.contrib.layers.xavier_initializer())
                b = tf.Variable(tf.random_normal([net_size]))

            if layer_count == 1:
                L = tf.nn.relu(tf.matmul(X, W) + b)
            elif layer_count == MAX_LAYER - 1:
                hypothesis = tf.sigmoid(tf.matmul(Ls[-1], W) + b)
            else:
                L = tf.nn.relu(tf.matmul(Ls[-1], W) + b)
                L = tf.nn.dropout(Ls[-1], keep_prob=keep_prob)
            Ls.append(L)

        cost = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(logits=hypothesis, labels=Y))
        optimizer = tf.train.AdamOptimizer(learning_rate=learning_rate).minimize(cost)

        correct_prediction = tf.equal(tf.argmax(hypothesis, 1), tf.argmax(Y, 1))
        accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))

        cost_summary = tf.summary.scalar('cost', cost)
        accuracy_summary = tf.summary.scalar('accuracy', accuracy)
        merged = tf.summary.merge_all()

        for dropout_rate in [0.1, 0.5, 0.9]:
            sess = tf.Session()
            sess.run(tf.global_variables_initializer())

            writer = tf.summary.FileWriter('./log/layer(%d)-input(%d)-dropout(%s)'%(layer_count, input_size, dropout_rate), sess.graph)

            for epoch in range(epoch_size):
                feed_dict = {X: x_datas, Y: y_datas, keep_prob: dropout_rate}
                c, _, h, _y = sess.run([cost, optimizer, hypothesis, Y], feed_dict=feed_dict)
                if epoch % 10 == 0:
                    print('Epoch:', '%04d' % (epoch), 'cost =', '{:.9f}'.format(c))
                    print('Accuracy:', sess.run(accuracy, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1}))
                    result = sess.run(merged, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1})
                    writer.add_summary(result, epoch)

            print('Learning Finished!')

            print('Prediction:', sess.run(correct_prediction, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1}))
            print('Accuracy:', sess.run(accuracy, feed_dict={X: x_test_datas, Y: y_test_datas, keep_prob: 1}))
