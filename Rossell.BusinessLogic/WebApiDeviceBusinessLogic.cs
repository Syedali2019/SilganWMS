using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;

namespace Rossell.BusinessLogic
{
    public class WebApiDeviceBusinessLogic : IDisposable
    {
        public WebApiDevice GetWebApiDevice(string defaultPrinter)
        {
            using (WebApiDeviceDataLogic webApiDeviceDA = new WebApiDeviceDataLogic())
            {
                return webApiDeviceDA.GetWebApiDevice(defaultPrinter);
            }
        }

        public long SaveWebApiDevice(string mac, string name, string modules, string type, string printer)
        {
            using (WebApiDeviceDataLogic webApiDeviceDA = new WebApiDeviceDataLogic())
            {
                return webApiDeviceDA.SaveWebApiDevice(mac, name, modules, type, printer);
            }
        }

        public bool UpdateDeviceID(string token, long webApiDeviceID)
        {
            using (WebApiDeviceDataLogic webApiDeviceDA = new WebApiDeviceDataLogic())
            {
                return webApiDeviceDA.UpdateDeviceID(token, webApiDeviceID);
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
