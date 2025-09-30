using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class FilterWorkOrder
    {
        public int InventoryGroupID { get; set; }
        public int PartNumberID { get; set; }
        
        public Nullable<System.DateTime> MustStartDate { get; set; }
        public Nullable<System.DateTime> ExpiryDate { get; set; }
    }
}
