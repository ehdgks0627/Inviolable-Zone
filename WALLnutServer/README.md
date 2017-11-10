# cntk-django-rest-api-server

CNTK and django virtualenv setting [here](https://docs.microsoft.com/en-us/cognitive-toolkit/Using-CNTK-with-Keras)
http://localhost/v1/classification/test/

## API List


| Purpose | url | Method | request | response |
|:-----------:|:------------:|:------:|:------------:|:------------:|
| paid key? | /v1/api-key/is-paid/ | post | {"api-key": ${api-key}} | {"state": "OK", "is-paid": ${boolean}} |
| join | /v1/user/join/ | post | {} | {"state": "OK", "api-key": ${api-key}} |
| check file | /v1/wallnut/is-infected/ | post | {"api-key": "${api-key}", "data": ${filedata(400byte)}} | {"state": "OK", "is-infected": ${boolean}} |
