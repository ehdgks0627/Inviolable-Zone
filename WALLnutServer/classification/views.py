from django.shortcuts import render
from django.http import HttpResponse, JsonResponse
from keras.models import Sequential, load_model
from keras.layers.core import Dense, Activation
from keras.optimizers import SGD
from django.views.decorators.csrf import csrf_exempt
from user.models import *
from user.views import *
import numpy as np
import json

CSHARP_TO_PY = {"a": "application_msword_",
                "b": "application_pdf_",
                "c": "application_vnd.ms-powerpoint_",
                "d": "application_vnd.openxmlformats-officedocument.presentationml.presentation_",
                "e": "application_vnd.openxmlformats-officedocument.spreadsheetml.sheet_",
                "f": "application_vnd.openxmlformats-officedocument.wordprocessingml.document_",
                "g": "application_x-hwp_",
                "h": "application_zip_",
                "i": "image_gif_",
                "j": "image_jpeg_",
                "k": "image_png_"}
MODELS = {}

for TYPE in CSHARP_TO_PY.values():
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

    access_token = request_data.get("access_token", "")
    features = request_data.get("features", [])
    request_id = request_data.get("request_id", "")
    result = []

    if not access_token or not isValidAccessToken(access_token):
        return JsonResponse({"err_msg": "not a valid access_token"})

    isInfected = False

    for feature in features:
        ftype = feature[0]
        fdata = np.asarray([feature[1]*10])
        if ftype in CSHARP_TO_PY.keys():
            result = MODELS[CSHARP_TO_PY[ftype]].predict_classes(fdata)
            if result[0] == 1:
                isInfected = True
        else:
            continue
    aes128_key = User.objects.filter(access_token=access_token)[0].aes128_key
    if isInfected:
        return JsonResponse({"aes128_key": aes128_key, "isInfected": isInfected, "request_id": request_id})
    else:
        return JsonResponse({"aes128_key": aes128_key, "isInfected": isInfected, "request_id": request_id})
