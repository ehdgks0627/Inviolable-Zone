using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public class BaseEventArgs : EventArgs
    {
        public object Source { get; set; }
        public BaseEventArgs(object source)
            : base()
        {
            this.Source = source;
        }
    }
}
