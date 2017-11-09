# tensorflow-django

## How to Use(Python3.5 need)
```sh
virtualenv venv
(or virtualenv -p python3.5 venv)
pip install -r requirements.txt
python manage.py runserver
(or python manage.py runserver 0.0.0.0:8000)
```

## API List


| Purpose | url | Method | request | response |
|:-----------:|:------------:|:------:|:------------:|:------------:|
| paid key? | /v1/api-key/is-paid/ | post | {"api-key": ${api-key}} | {"state": "OK", "is-paid": ${boolean}} |
| join | /v1/user/join/ | post | {} | {"state": "OK", "api-key": ${api-key}} |
| check file | /v1/wallnut/is-infected/ | post | {"api-key": "${api-key}", "data": ${filedata(400byte)}} | {"state": "OK", "is-infected": ${boolean}} |
