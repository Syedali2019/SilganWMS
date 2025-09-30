using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class MasterLabel
    {
        public long MASTER_LABEL_ID { get; set; }
        public string SERIAL { get; set; }
        public long FGMULTI_ID { get; set; }
        public string LOT_NO { get; set; }
        public decimal QUANTITY { get; set; }
        public string DISPO_DATE { get; set; }
        public string FG_LOTNO { get; set; }
        public string PRINT_DATE { get; set; }
    }


    public class MasterLabelDetail
    {
        public long MASTER_LABEL_ID { get; set; }
        public string SERIAL { get; set; }
        public long ARINVT_ID { get; set; }
        public string ITEM_NO { get; set; }
        public string DESCRIPTION { get; set; }
        public string DESCRIPTION2 { get; set; }
        public string CLASS { get; set; }
        public string UNIT { get; set; }
        public string REV { get; set; }
        public long FGMULTI_ID { get; set; }
        public string LOCATION_DESC { get; set; }
        public decimal TOTAL_QUANTITY { get; set; }
        public string LOT_DESC { get; set; }
        public decimal FGMULTI_ONHAND { get; set; }
        public decimal BOM_QTY { get; set; }
        public string DISPO_DATE { get; set; }
        public string LOT_NO { get; set; }
    }

    public class Master_Label
    {
        public long ID { get; set; }
        public long ARINVT_ID { get; set; }
        public long BOX_ID { get; set; }
        public string BOX_NO { get; set; }
        public string CLASS { get; set; }
        public string DESCRIPTION { get; set; }
        public string DESCRIPTION2 { get; set; }
        public string DISPO_DATE { get; set; }
        public string DISPO_SCAN { get; set; }
        public string SERIAL { get; set; }
        public long FGMULTI_ID { get; set; }
        public string ITEM_NO { get; set; }
        public string LOT_NO { get; set; }
        public decimal QUANTITY { get; set; }        
        public string FG_LOTNO { get; set; }
        public string PRINT_DATE { get; set; }
        public long LM_LABEL_ID { get; set; }
        public string LOC_DESC { get; set; }
        public string LOT_DATE { get; set; }
        public string MFG_NO { get; set; }
        public string ORDER_NO { get; set; }
        public long ORDER_DETAIL_ID { get; set; }
        public string PO_NO { get; set; }
        public string PRESS_NO { get; set; }
        public decimal Print_Qty { get; set; }
        public string CUSER1 { get; set; }
        public string CUSER2 { get; set; }
        public string ORIFINAL_SYSDATE { get; set; }
        public string ORIGINAL_USERNAME { get; set; }
        public string SYS_DATE { get; set; }
        public string USERNAME { get; set; }
    }


    public class FGMULTI
    {
        public long ID { get; set; }
        public string LOT_NO { get; set; }
        public long LOCATION_ID { get; set; }
        public string LOC_DESC { get; set; }
        public string NO_SHIP { get; set; }
    }

    public class PCSLabelType
    {
        public long ID { get; set; }
    }
}
