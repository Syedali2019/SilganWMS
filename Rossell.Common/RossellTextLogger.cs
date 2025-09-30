using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace Rossell.Common
{
    public class RossellTextLogger
    {
        public string FileName { get; set; }

        public void ProcessStartLog()
        {
            try
            {
                string filePath = ConfigurationManager.AppSettings["LOGFILEPATH"].ToString() + FileName;
                StreamWriter writer = new StreamWriter(filePath, true);
                writer.WriteLine("");
                writer.WriteLine("=============================================================== Rossell Kitting Material Process Start : " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + " ================================================================");
                writer.Flush();
                writer.Close();
            }
            catch (Exception)
            {
            }
        }

        public void ProcessEndLog()
        {
            try
            {
                string filePath = ConfigurationManager.AppSettings["LOGFILEPATH"].ToString() + FileName;
                StreamWriter writer = new StreamWriter(filePath, true);
                writer.WriteLine("");
                writer.WriteLine("=============================================================== Rossell Kitting Material Process End : " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + " ================================================================");
                writer.Flush();
                writer.Close();
            }
            catch (Exception)
            {
            }
        }
        public void MessageLog(string sErrorMessage)
        {
            try
            {
               
                string filePath = ConfigurationManager.AppSettings["LOGFILEPATH"].ToString() + FileName;
                StreamWriter writer = new StreamWriter(filePath, true);
                writer.WriteLine(sErrorMessage);
                writer.Flush();
                writer.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
