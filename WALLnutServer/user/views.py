from django.shortcuts import render
from django.http import HttpResponse, JsonResponse
from django.views.decorators.csrf import csrf_exempt
from api_key.urls import *
from .models import *
from user.models import *
import json
import random


def isValidAccessToken(access_token):
    return User.objects.filter(access_token=access_token)

@csrf_exempt
def Join(request):
    request_data = json.loads(request.body.decode())
    token = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

    def CreateUser(name, api_key_instance):
        new_user = User.objects.create(name=name, access_token=GenerateToken(), aes128_key=GenerateToken(length=128), api_key=api_key_instance)
        api_key_instance.used = True
        api_key_instance.save(update_fields=['used'])
        return new_user

    def CreateAPIkey():
        while True:
            try:
                serial = "-".join("".join([random.choice(token) for _ in range(5)]) for _ in range(5))
                api_key_instance = APIkey.objects.create(serial=serial, paid=False, used=False)
                break
            except:
                continue
        return api_key_instance

    def GenerateToken(length=32):
        return "".join([random.choice(token) for _ in range(length)])

    api_key = request_data.get("api_key", "")
    name = request_data.get("name", "default name")
    if api_key:
        api_key_instance = isExistAPIkey(api_key)
        if api_key_instance:
            api_key_instance = api_key_instance[0]
            if api_key_instance.used:
                return JsonResponse({"err_msg": "already used api-key"})
            new_user = CreateUser(name, api_key_instance)
            if api_key_instance.paid:
                return JsonResponse({"access_token": new_user.access_token})
            else:
                return JsonResponse({"access_token": new_user.access_token})
        else:
            return JsonResponse({"err_msg": "not a valid api-key"})
    else:
        new_user = CreateUser(name, CreateAPIkey())
        return JsonResponse({"access_token": new_user.access_token})
