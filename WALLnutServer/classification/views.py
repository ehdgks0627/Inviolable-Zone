from django.shortcuts import render
from django.http import HttpResponse
from keras.models import Sequential
from keras.layers.core import Dense, Activation
from keras.optimizers import SGD
import numpy as np


def XORExample(request):
    val1 = float(request.GET.get("val1", "0"))
    val2 = float(request.GET.get("val2", "1"))
    test_X = np.array([[val1, val2]], "float32")
    X = np.array([[0, 0], [0, 1], [1, 0], [1, 1]], "float32")
    y = np.array([[0], [1], [1], [0]], "float32")

    model = Sequential()
    model.add(Dense(2, input_dim=2))
    model.add(Activation('sigmoid'))
    model.add(Dense(1))
    model.add(Activation('sigmoid'))
    sgd = SGD(lr=0.1, decay=1e-6, momentum=0.9, nesterov=True)
    model.compile(loss='mean_squared_error', optimizer=sgd, class_mode="binary")
    model.fit(X, y, nb_epoch=1000, batch_size=1)
    return HttpResponse(str(val1) + " ^ " + str(val2) + " = " + ("1" if model.predict_proba(test_X)[0][0] > 0.5 else "0"))