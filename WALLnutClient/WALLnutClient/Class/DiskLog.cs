using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public class DiskLog
    {
        public enum TYPE
        {
            ERROR,
            CREATE,
            RENAME,
            CHANGE,
            DELETE
        }

        public Boolean isFolder { get; set; }
        public String fileName { get; set; }
        public String oldFileName { get; set; }
        public long time { get; set; }

        private TYPE _type;
        public TYPE type
        {
            get { return _type; }
            set
            {
                if (Enum.IsDefined(typeof(TYPE), value) && !value.Equals(TYPE.ERROR))
                {
                    _type = value;
                }
                else
                {
                    _type = TYPE.ERROR;
                }
            }
        }
        
        public DiskLog(TYPE type, Boolean isFolder, string filename,  string oldfilename = "")
        {
            this.type = type;
            this.isFolder = isFolder;
            this.fileName = filename;
            this.oldFileName = oldfilename;
            this.time = DateTime.Now.ToFileTimeUtc();
        }

        public override string ToString()
        {
            string result = "[" + new DateTime(time).ToShortTimeString() + "] " + ((isFolder) ? "Folder " : "File ");
            switch (type)
            {
                case TYPE.ERROR:
                    result = "ERR LOG";
                    break;
                case TYPE.CREATE:
                    result += "Created - " + fileName;
                    break;
                case TYPE.RENAME:
                    result += "Renamed - | " + oldFileName + " | --> | " + fileName + " |";
                    break;
                case TYPE.CHANGE:
                    result += "Changed - " + fileName;
                    break;
                case TYPE.DELETE:
                    result += "Deleted - " + fileName;
                    break;
            }
            return result;
        }
    }
}
