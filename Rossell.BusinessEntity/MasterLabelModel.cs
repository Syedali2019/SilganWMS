using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class MasterLabelModel
    {
        public int Id { get; set; }
        public int ArinvtId { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string InventoryClass { get; set; }
        public string Rev { get; set; }
        public string FGLotNo { get; set; }
        public string CustNo { get; set; }
        public string Company { get; set; }
        public string SerialNo { get; set; }
        public decimal Qty { get; set; }
        public string ItemNo { get; set; }
        public string LocationName { get; set; }
        public int FGMultiId { get; set; }
        public bool BackflushBySerial { get; set; }
        public DateTime DispoDate { get; set; }
        public string PalletSerial { get; set; }
        public DateTime PrintDate { get; set; }
        public string LocDesc { get; set; }
        public int LocId { get; set; }
        public int DayPartId { get; set; }
        public bool Scanned { get; set; }

    }

    public class FGMULTIModel
    {
        public int fgMultiId { get; set; }
    }
}
