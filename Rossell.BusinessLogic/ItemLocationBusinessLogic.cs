using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;

namespace Rossell.BusinessLogic
{
    public class ItemLocationBusinessLogic : IDisposable
    {
        public List<ItemLocation> GetItemLocation(string arinvtIDS, DateTime? expiryDate)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetItemLocation(arinvtIDS, expiryDate);
            }
        }

        public List<ItemLocation> GetItemLocation(string arinvtIDS, DateTime? expiryDate, double totalRequiredQuantity)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetItemLocation(arinvtIDS, expiryDate, totalRequiredQuantity);
            }
        }

        public List<ItemLocation> GetItemLocation(long arinvtID)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetItemLocation(arinvtID);
            }
        }

        public Location GetLocation(string locationName, long arinvtID, string lotNO)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetLocation(locationName, arinvtID, lotNO);
            }
        }

        public Location GetLocation(string locationName)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetLocation(locationName);
            }
        }

        public Location GetLocation(long id)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetLocation(id);
            }
        }

        public Item GetItemData(long arinvtID)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.GetItemData(arinvtID);
            }
        }

        public long AddLocation(string locationName)
        {
            using (ItemLocationDataLogic itemLocationData = new ItemLocationDataLogic())
            {
                return itemLocationData.AddLocation(locationName);
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
