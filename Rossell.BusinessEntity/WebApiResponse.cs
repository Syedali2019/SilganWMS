using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rossell.BusinessEntity
{
    public class WebApiResponse
    {
        public object Status { get; set; }
        public string Message { get; set; }
        public bool DidSucceed { get; set; }
        public object ModelState { get; set; }

    }
}
