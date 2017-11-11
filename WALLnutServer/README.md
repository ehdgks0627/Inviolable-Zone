# cntk-django-rest-api-server

## Usage
1. Donwoad Anaconda3-4.1.1 version
2. Install Conda
3. Enter [Anaconda Archive](https://repo.continuum.io/archive/)
4. Download CNTK2.2 [Windows](https://docs.microsoft.com/en-us/cognitive-toolkit/setup-windows-python?tabs=cntkpy22) or [Linux](https://docs.microsoft.com/en-us/cognitive-toolkit/setup-linux-python?tabs=cntkpy22) AND install with pip<br>
```pip install https://~~~```
5. Enter WALLnutServer Folder
6. ```conda create -n venv python=3.5```
7. ```activate venv```
8. ```pip install -r requirements.txt```
9. Modify the "keras.json" file under %USERPROFILE%/.keras on Windows, or $HOME/.keras on Linux
```
{
    "epsilon": 1e-07, 
    "image_data_format": "channels_last", 
    "backend": "cntk", 
    "floatx": "float32" 
}
```
10. ```python manage.py runserver```
http://localhost/v1/classification/test/

## API List

| Purpose | url | Method | request | response |
|:-----------:|:------------:|:------:|:------------:|:------------:|
| paid key? | /v1/api-key/is-paid/ | post | {"api-key": ${api-key}} | {"state": "OK", "is-paid": ${boolean}} |
| join | /v1/user/join/ | post | {} | {"state": "OK", "api-key": ${api-key}} |
| check file | /v1/wallnut/is-infected/ | post | {"api-key": "${api-key}", "data": ${filedata(400byte)}} | {"state": "OK", "is-infected": ${boolean}} |
