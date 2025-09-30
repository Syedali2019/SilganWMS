using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;

namespace Rossell.BusinessLogic
{
    public class LabelBusinessLogic : IDisposable
    {
        public Label GetMasterLabelData(string serialNumber, long arinvtID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelData(serialNumber, arinvtID);
            }
        }

        public Label GetMasterLabelNotFoundReason(string serialNumber, long arinvtID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelNotFoundReason(serialNumber, arinvtID);
            }
        }

        public BusinessEntity.MasterLabel GetMasterLabelData(string serialNumber)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelData(serialNumber);
            }
        }

        public MasterLabelDetail GetMasterLabelDetailData(string serialNumber)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelDetailData(serialNumber);
            }
        }

        public FGMULTI GetFGMULTIData(long id)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetFGMULTIData(id);
            }
        }

        public FGMULTI GetNewFGMULTIData(long workOrderID, long arinvtID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetNewFGMULTIData(workOrderID, arinvtID);
            }
        }

        public Master_Label GetMasterLabelData(long id)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelData(id);
            }
        }

        public long GetMasterLabelBetween(long id, long labelTypeID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetMasterLabelBetween(id, labelTypeID);
            }
        }

        public PCSLabelType GetPCSLabelType()
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.GetPCSLabelType();
            }
        }

        public bool UpdateMasterLabel(long id, long fgMultiID, string locationDescription, string lotNo)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(id, fgMultiID, locationDescription, lotNo);
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(serialNumber, qty);
            }
        }

        public bool UpdateMasterLabelType(string serialNumber, long labelType)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabelType(serialNumber, labelType);
            }

        }

        public bool UpdateMasterLabelQtyWithType(string serialNumber, decimal qty, long labelType)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabelQtyWithType(serialNumber, qty, labelType);
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTIID, long oldSerialID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(serialNumber, qty, fgMULTIID, oldSerialID);
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID, long oldSerialID, string locationDesc)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(serialNumber, qty, fgMULTI_ID, oldSerialID, locationDesc);
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTIID)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(serialNumber, qty, fgMULTIID);
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID, string locationDesc)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateMasterLabel(serialNumber, qty, fgMULTI_ID, locationDesc);
            }
        }

        public bool UpdateFGMULTI_NoShip(long id, string noShip)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateFGMULTI_NoShip(id, noShip);
            }
        }

        public bool AdjustMasterLabelQty(long id, decimal qty)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.AdjustMasterLabelQty(id, qty);
            }
        }
        public bool UpdateSourceFGMULTIOnHand(long id, decimal qty)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateSourceFGMULTIOnHand(id, qty);
            }
        }

        public bool UpdateFGMULTIOnHand(long id, decimal qty)
        {
            using (LabelDataLogic labelDataLogic = new LabelDataLogic())
            {
                return labelDataLogic.UpdateFGMULTIOnHand(id, qty);
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
