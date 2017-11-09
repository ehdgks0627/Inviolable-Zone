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
    return magic.from_file(fname, mime=True) + "\\"

inputDir  = "C:\\Users\\sprout\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cache"
outputDir = "C:\\Users\\sprout\\Desktop\\WALLnut\\deeplearning\\filesByLibmagic\\"

for (path, dir, files) in os.walk(inputDir):
    for file in files:
        print(path + "\\" + file)
        try:
            fileMagic = getMagic(path + "\\" + file)
            fileMD5 =  md5(path + "\\" + file)
            if not os.path.isdir(outputDir + fileMagic):
                os.mkdir(outputDir + fileMagic)
            os.rename(path + "\\" + file, outputDir + fileMagic + fileMD5)
        except FileExistsError:
            os.remove(path + "\\" + file)
        except PermissionError:
            print("PermissionError")
            pass