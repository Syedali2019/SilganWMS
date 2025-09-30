using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class WorkOrder
    {
        public long WorkOrderID { get; set; }
        public string ItemNo { get; set; }
        public string StartDate { get; set; }
        public decimal REQUIRED_QUANTITY { get; set; }
        public decimal PICKED_QUANTITY { get; set; }
        public decimal RECEIVED_QUANTITY { get; set; }
        public int COLOR_CODE { get; set; }
    }

    public class WorkOrderAssignBOM
    {
        public long WorkOrderID { get; set; }
    }

    public class WorkOrderSingle
    {
        public long WorkOrderID { get; set; }
        public string ASSY_RUN { get; set; }
    }

    public class WorkOrderBOMSingle
    {
        public long WorkOrderID { get; set; }
        public long ARINVT_ID { get; set; }
        public decimal QUAN { get; set; }
    }
}
