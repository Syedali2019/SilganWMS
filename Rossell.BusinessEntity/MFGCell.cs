using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class MFGCell
    {
        public long ID { get; set; }
        public string MFG_CELL { get; set; }
        public string MFG_TYPE { get; set; }
        public long STAGING_LOCATION_ID { get; set; }
    }
}
