using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;

namespace Rossell.BusinessLogic
{
    public class InventoryGroupBusinessLogic : IDisposable
    {
        public List<InventoryGroup> GetInventoryGroupData()
        {
            using (InventoryGroupDataLogic inventoryGroupDataLogic = new InventoryGroupDataLogic())
            {
                return inventoryGroupDataLogic.GetInventoryGroupData();
            }
        }

        public List<PartNumber> GetPartNumberData()
        {
            using (InventoryGroupDataLogic inventoryGroupDataLogic = new InventoryGroupDataLogic())
            {
                return inventoryGroupDataLogic.GetPartNumberData();
            }
        }

        public List<PartNumber> GetPartNumberData(long groupID)
        {
            using (InventoryGroupDataLogic inventoryGroupDataLogic = new InventoryGroupDataLogic())
            {
                return inventoryGroupDataLogic.GetPartNumberData(groupID);
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
