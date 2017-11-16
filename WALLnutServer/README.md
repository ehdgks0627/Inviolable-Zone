# cntk-django-rest-api-server

## Usage
1. Enter [Anaconda Archive](https://repo.continuum.io/archive/)
2. Donwoad Anaconda3-4.1.1 version
3. Install Conda
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

### err
1. ImportError: libjasper.so.1: cannot open shared object file: No such file or directory<br>
```apt install graphicsmagick```
## API List

| Purpose | url | Method | request | response |
|:-----------:|:------------:|:------:|:------------:|:------------:|
| paid key? | /v1/api-key/is-paid/ | post | {"api-key": ${api-key}} | {"state": "OK", "is-paid": ${boolean}} |
| join | /v1/user/join/ | post | {} | {"state": "OK", "api-key": ${api-key}} |
| check file | /v1/wallnut/check-file/ | post | {"api-key": "${api-key}", "data": [[${id}, ${filetype}, ${filedata(400byte)}], [], []...]} | {"state": "OK", "is-infected": ${boolean}, "aes128_key": ${aes128_key} |
