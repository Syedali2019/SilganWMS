using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class Printer
    {
        public string PrinterName { get; set; }
    }

    public class WebApiDevice
    {
        public long ID { get; set; }
        public string MAC { get; set; }
        public string NAME { get; set; }
        public string MODULE { get; set; }
        public string TYPE { get; set; }
        public string DEFAULT_PRINTER { get; set; }
        public string LABEL_PRINTER { get; set; }        
    }
}
