from django.shortcuts import render
from .models import *


def isExistAPIkey(api_key):
    return APIkey.objects.filter(serial=api_key)
