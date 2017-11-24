import os
import hashlib
import magic

def md5(fname):
    hash_md5 = hashlib.md5()
    with open(fname, "rb") as f:
        for chunk in iter(lambda: f.read(4096), b""):
            hash_md5.update(chunk)
    return hash_md5.hexdigest()

def getMagic(fname):
    return magic.from_file(fname, mime=True) + path_split

inputDir  = "files/"
outputDir = "c_files/"
path_split = "/"

for (path, dirs, files) in os.walk(inputDir):
    count = len(files)
    now = 0
    for file in files:
        now += 1
        try:
            fileMagic = getMagic(path + file).replace(path_split, "_")
            fileMD5 =  md5(path + path_split + file)
            if not os.path.isdir(outputDir + fileMagic):
                os.mkdir(outputDir + fileMagic)
            os.rename(path + file, outputDir + fileMagic + path_split + fileMD5)
            if now % 10 == 0 or True:
                print("(%d/%d) - %s - %s"%(now, count, file, fileMagic))

        except OSError: # if 2, OSError 3, FileExistsError
            print("err")
            os.remove(path + file)
            pass

#https://github.com/ahupp/python-magic