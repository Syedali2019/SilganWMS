using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Rossell.Common
{
    public class RossellLog
    {
        private LogWriter logWriter;

        public RossellLog()
        {
            InitLogging();
        }

        private void InitLogging()
        {
            logWriter = new LogWriterFactory().Create();
            Logger.SetLogWriter(logWriter, false);
        }

        public LogWriter LogWriter
        {
            get
            {
                return logWriter;
            }
        }

        public void WriteTraceLog(string message)
        {
            logWriter.Write(message);
        }
    }
}
