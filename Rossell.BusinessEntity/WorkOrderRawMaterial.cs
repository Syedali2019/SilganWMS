using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class WorkOrderRawMaterial
    {
        public decimal SEQ_ID { get; set; }
        public string UOM { get; set; }
        public decimal LOC_ID { get; set; }
        public string LOC_DESC { get; set; }
        public long WORKORDER_BOM_ID { get; set; }
        public long WORKORDER_ID { get; set; }
        public long STANDARD_ID { get; set; }
        public long ARINVT_ID_FG { get; set; }
        public string ITEM_NO { get; set; }
        public double REL_QUANTITY { get; set; }
        public long SNDOP_ID { get; set; }
        public long ARINVT_ID_RM { get; set; }
        public double PTSPER { get; set; }
        public string UNIT { get; set; }
        public string ITEMNO { get; set; }
        public string DESCRIPTION { get; set; }
        public string DESCRIPTION2 { get; set; }
        public double REQ_QUANTITY { get; set; }
        public double HARD_ALLOCATION { get; set; }
        public decimal IS_PICKED { get; set; }
        public decimal IS_ERROR { get; set; }
        public decimal IS_KITTED { get; set; }
        public bool ENOUGH_ONHAND { get; set; }
        public decimal IS_SKIPPED { get; set; }
        public string ERROR_DESCRIPTION { get; set; }
        public decimal MASTER_LABEL_ID { get; set; }
        public decimal NEW_MASTER_LABEL_ID { get; set; }
        public long FGMULTI_ID { get; set; }
        public long STAGING_LOCATIONS_ID { get; set; }
        public decimal OPMAT_FACTOR { get; set; }
    }

    public class PickInventory
    {        
        public string SerialNumber { get; set; }
        public decimal PickQuantity { get; set; }
        public long WorkOrderID { get; set; }
        public long ARINVT_ID { get; set; }
        public decimal Factor { get; set; }
    }
}
