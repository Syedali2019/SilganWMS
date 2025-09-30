using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class Label
    {
        public long ID { get; set; }
        public string SERIAL_NUMBER { get; set; }
        public long ARINVT_ID { get; set; }
        public long FGMULTI_ID { get; set; }
        public string REASON { get; set; }
        public string FG_LOTNO { get; set; }
        public string WORKORDER { get; set; }
        public decimal QUANTITY { get; set; }
    }
}
