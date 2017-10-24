import matplotlib.pyplot as plt
import pickle

f = open("h_data", "rb")
h = pickle.load(f)
f.close()
for i in h:
    plt.plot(i[0], i[1], 'ro' if i[0] > i[1] else 'mo')
plt.show()