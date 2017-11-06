#!/usr/bin/python3
import requests
import os
import sys
import progressbar
from bs4 import BeautifulSoup

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
    except:
        pass
    return local_filename

def machine(count_start, count_end, t):
    bar = progressbar.ProgressBar(max_value=(count_end - count_start))
    n = 0
    bar.update(n)
    for num in range(int(count_start/10), int(count_end/10)):
        url = "https://www.google.co.kr/search?q=example filetype:{}&start={}".format(t, num*10)
        headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36',
                   'Host': 'www.google.co.kr',
                   'Connection': 'keep-alive',
                   'Cache-Control': 'max-age=0',
                   'Reffer': 'https://www.google.co.kr/'}

        response = requests.get(url, headers=headers)
        html = response.text
        root = BeautifulSoup(html, 'html.parser')

        articles = root.find_all('h3', {'class': 'r'})
        if len(articles) == 0:
            return
        for article in articles:
            try:
                link = "https://google.co.kr/url?q=" + article.find_all('a')[0]['href']
                link = BeautifulSoup(requests.get(link, headers=headers).text, 'html.parser').find_all('div', {'class': '_jFe'})[0].find_all('a')[0]['href']
                download_file(link, t)
                n += 1
                bar.update(n)
            except:
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
        for l in lists:
            print("[+] Downloading %s..."%(l))
            machine(0, 1000, l)

if __name__ == '__main__':
    main()
