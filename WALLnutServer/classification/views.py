from django.shortcuts import render
from django.http import HttpResponse, JsonResponse
from keras.models import Sequential, load_model
from keras.layers.core import Dense, Activation
from keras.optimizers import SGD
from django.views.decorators.csrf import csrf_exempt
from user.models import *
import numpy as np
import json

CSHARP_TO_PY = {"application_msword_": "",
                "application_pdf_": "",
                "application_vnd.ms-powerpoint_": "",
                "application_vnd.openxmlformats-officedocument.presentationml.presentation_": "",
                "application_vnd.openxmlformats-officedocument.spreadsheetml.sheet_": "",
                "application_vnd.openxmlformats-officedocument.wordprocessingml.document_": "",
                "application_x-hwp_": "",
                "application_zip_": "",
                "image_gif_": "",
                "image_jpeg_": "",
                "image_png_": ""}
MODELS = {}

for TYPE in CSHARP_TO_PY.keys():
    print("Loading Model - %s"%(TYPE))
    MODELS[TYPE] = load_model("models/%s.h5" % (TYPE))
    print("[+] Loading Model Done!")


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
    return HttpResponse(
        str(val1) + " ^ " + str(val2) + " = " + ("1" if model.predict_proba(test_X)[0][0] > 0.5 else "0"))


@csrf_exempt
def checkFile(request):
    request_data = json.loads(request.body.decode())

    api_key = request_data.get("api_key", "")
    filedatas = request_data.get("data", [])
    result = []

    if not api_key:
        return JsonResponse({"err_msg": "not a valid api-key"})

    isInfected = False

    for data in filedatas:
        fileid = data[0]
        filetype = data[1]
        filedata = data[2]
        if filetype in CSHARP_TO_PY.keys():
            pass
            # result = MODELS[CSHARP_TO_PY[filetype]].predict(filedata)
            # model_to_predict
        else:
            continue
    aes128_key = User.objects.filter(api_key=api_key)[0].aes128_key
    return JsonResponse({"aes128_key": aes128_key, "check": isInfected})
