#!/usr/bin/python3
import requests
import os
import sys
import progressbar
from bs4 import BeautifulSoup
import hashlib
from multiprocessing import Process

keywords = ["news", "backup", "model", "lecture", "example", "source", "data"]

def md5(fname):
    hash_md5 = hashlib.md5()
    with open(fname, "rb") as f:
        for chunk in iter(lambda: f.read(4096), b""):
            hash_md5.update(chunk)
    return hash_md5.hexdigest()

def download_file(url, t):
    local_filename = url.split('/')[-1]
    r = requests.get(url, stream=True)
    if not os.path.exists("sample"):
        os.makedirs("sample")
    if not os.path.exists("sample/{}".format(t)):
        os.makedirs("sample/{}".format(t))
    with open("sample/{}/".format(t) + local_filename, 'wb') as f:
        for chunk in r.iter_content(chunk_size=1024):
            if chunk:
                f.write(chunk)

    try:
        if os.path.getsize("sample/{}/".format(t) + local_filename) < 1000:
            os.remove("sample/{}/".format(t) + local_filename)
            return False
        else:
            try:
                os.rename("sample/{}/".format(t) + local_filename, "sample/{}/".format(t) + md5("sample/{}/".format(t) + local_filename) + "." + t)
            except FileExistsError:
                print("exist err")
                os.remove("sample/{}/".format(t) + local_filename)
                return False
    except:
        os.remove("sample/{}/".format(t) + local_filename)
        return False
    return True

def machine(t, download = 1000, n = 0):
    #bar = progressbar.ProgressBar(max_value=download)
    #bar.update(n)

    print("[Thread %5d] %s start" % (os.getpid(), t))

    for keyword in keywords:
        pageIndex = 1
        while n <= download:
            url = "https://www.google.co.kr/search?q={}%20filetype:{}&start={}".format(keyword, t, pageIndex*10)
            headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36',
                       'Host': 'www.google.co.kr',
                       'Connection': 'keep-alive',
                       'Cache-Control': 'max-age=0',
                       'Reffer': 'https://www.google.co.kr/'}
            pageIndex += 1
            response = requests.get(url, headers=headers)
            print(url)
            print(response)
            html = response.text
            root = BeautifulSoup(html, 'html.parser')

            articles = root.find_all('h3', {'class': 'r'})
            if len(articles) == 0:
                break
            for article in articles:
                try:
                    link = "https://google.co.kr/url?q=" + article.find_all('a')[0]['href']
                    link = BeautifulSoup(requests.get(link, headers=headers).text, 'html.parser').find_all('div', {'class': '_jFe'})[0].find_all('a')[0]['href']
                    print(link)
                    if download_file(link, t):
                        n += 1
                        #bar.update(n)
                except:
                    print("err?")
                    continue

def read_ext_files(filename):
    with open(filename, "r") as f:
        lists = f.read().split()
    return lists

def main() :
    if len(sys.argv) < 2:
        print("[*] python3 Usage {} ext_lists".format(sys.argv[0]))
        sys.exit()
    else:
        lists = read_ext_files(sys.argv[1])
        proclist = []
        for l in lists:
            print("[+] Downloading %s..."%(l))
            machine(l)
            '''
            proc = Process(target=machine, args=(l, ))
            proc.start()
            proclist.append(proc)
        for proc in proclist:
            proc.join()'''

if __name__ == '__main__':
    main()
