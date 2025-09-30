using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class ItemLocation
    {
        public long LOC_ID { get; set; }
        public string LOC_DESCRIPTION { get; set; }
        public long ARINVT_ID { get; set; }
        public long FGMULTI_ID { get; set; }
        public double ON_HAND { get; set; }
        public double ON_HAND_UPDATED { get; set; }
        public string VENDOR_LOT_NUMBER { get; set; }
        public string LOT_NUMBER { get; set; }
    }

    public class Location
    {
        public long ID { get; set; }
        public string LocationName { get; set; }
        public string ScanLocation { get; set; }
        public long FGMULI_ID { get; set; }
    }

    public class Item
    {
        public long ID { get; set; }
        public long STANDARD_ID { get; set; }
        public string ITEM_NO { get; set; }
        public string DESCRIPTION { get; set; }
        public string DESCRIPTION2 { get; set; }
        public string CLASS { get; set; }
        public string UNIT { get; set; }
        public string REV { get; set; }        
    }

}
