import os
import random

inputDir = "c_files/"
outputDir = "m_files/"
path_split = "/"
from_start = 200
from_end = 200
file_count = 500

t_inputDirs = os.listdir(inputDir)
inputDirs = []
for t in t_inputDirs:
    if os.path.isdir(inputDir + t):
        inputDirs.append(t)

for directory in inputDirs:
    print(directory)
    files = list(map(lambda x: inputDir + directory + path_split + x, os.listdir(inputDir + directory)))
    datas = []

    if len(files) < file_count:
        print("less than %d... skip"%(file_count))
        continue

    for i in range(file_count):
        try:
            if i % 100 == 0:
                print(i)
            while True:
                fname = random.choice(files)
                files.remove(fname)
                if os.path.getsize(fname) < from_start + from_end:
                    continue
                with open(fname, "rb") as f:
                    data = f.read()
                datas.append((data[:from_start], data[-from_end:]))
                break
        except IndexError:
            print("IndexError")
            break

    with open(outputDir + directory + ".csv", "w") as f:
        for data in datas:
            csv_data = ",".join(map(str, list(data[0]))) + ","
            csv_data += ",".join(map(str, list(data[1]))) + "\n"
            f.write(csv_data)

# https://github.com/ahupp/python-magic
