using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;
using Newtonsoft;
using Newtonsoft.Json;
using System.Configuration;
namespace Rossell.BusinessLogic
{
    public class TransLogBusinessLogic : IDisposable
    {

        public TransLog GetTransLogData(long arinvtID, long fgmultiID)
        {
            using (TransLogDataLogic transLogDL = new TransLogDataLogic())
            {
                return transLogDL.GetTransLogData(arinvtID, fgmultiID);
            }
        }

        public TransLog GetTransLogMasterLabelData(long transLogID)
        {
            using (TransLogDataLogic transLogDL = new TransLogDataLogic())
            {
                return transLogDL.GetTransLogMasterLabelData(transLogID);
            }
        }

        public long AddTransLogMasterLabel(long transLogID, long masterLabelID, decimal qty)
        {
            using (TransLogDataLogic transLogDL = new TransLogDataLogic())
            {
                return transLogDL.AddTransLogMasterLabel(transLogID, masterLabelID, qty);
            }
        }

        public bool UpdateTransLogReason(long id, string reason)
        {
            using (TransLogDataLogic transLogDL = new TransLogDataLogic())
            {
                return transLogDL.UpdateTransLogReason(id, reason);
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
