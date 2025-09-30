using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using EntLibContrib.Data.Oracle.ManagedDataAccess;
using Rossell.BusinessEntity;
using Rossell.Common;

namespace Rossell.DataLogic
{
    public class WebApiDeviceDataLogic : IDisposable
    {
        private OracleDatabase database;
        private readonly Mapper<WebApiDevice> mapper;

        public WebApiDeviceDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<WebApiDevice>(MapWebApiDevice);
        }

        public WebApiDevice GetWebApiDevice(string defaultPrinter)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID, MAC, NAME, MODULES, TYPE, DEFAULT_PRINTER, LABEL_PRINTER FROM S_WEBAPI_DEVICE WHERE DEFAULT_PRINTER = '{0}'", defaultPrinter));
            WebApiDevice webApiDevice = mapper.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return webApiDevice;
        }

        public long SaveWebApiDevice(string mac, string name, string modules, string type, string printer)
        {
            long deviceID = 0;
            try
            {

                System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(string.Format(@"INSERT INTO S_WEBAPI_DEVICE 
                                                                                        (ID, MAC, NAME, MODULES, TYPE, DEFAULT_PRINTER, LABEL_PRINTER, DATE_CREATED, TZ_OFFSET, EPLANT_ID) 
                                                                                        VALUES 
                                                                                        (S_S_WEBAPI_DEVICE.Nextval, '{0}', '{1}','{2}','{3}','{4}','{5}',TO_DATE('{6}', 'YYYY-MM-DD HH:mi:ss'),0,0) returning id into :l_id", mac, name, modules, type, printer, printer, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")));
                System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                parameter.ParameterName = "l_id";
                parameter.Direction = ParameterDirection.Output;
                parameter.DbType = DbType.Decimal;
                dbCommand.Parameters.Add(parameter);

                var reader = database.ExecuteNonQuery(dbCommand);
                deviceID = Convert.ToInt64(dbCommand.Parameters["l_id"].Value.ToString());

                
                return deviceID;
            }
            catch (Exception exp)
            {
                return deviceID;
            }
        }


        public bool UpdateDeviceID(string token, long webApiDeviceID)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE S_USER_TOKENS SET S_WEBAPI_DEVICE_ID = '{0}' WHERE TOKEN = '{1}'", webApiDeviceID, token));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }
        private static WebApiDevice MapWebApiDevice(IDataReader reader)
        {
            var webApiDevice = new WebApiDevice
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                MAC = reader["MAC"] == DBNull.Value ? string.Empty : Convert.ToString(reader["MAC"]),
                NAME = reader["NAME"] == DBNull.Value ? string.Empty : Convert.ToString(reader["NAME"]),
                MODULE = reader["MODULES"] == DBNull.Value ? string.Empty : Convert.ToString(reader["MODULES"]),
                TYPE  = reader["TYPE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["TYPE"]),
                DEFAULT_PRINTER = reader["DEFAULT_PRINTER"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DEFAULT_PRINTER"]),
                LABEL_PRINTER = reader["LABEL_PRINTER"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LABEL_PRINTER"]),                
            };
            return webApiDevice;
        }


        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
