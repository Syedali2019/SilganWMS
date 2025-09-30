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
    public class TransLogDataLogic : IDisposable
    {
        private OracleDatabase database;
        private readonly Mapper<TransLog> mapper;

        public TransLogDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<TransLog>(MapTransLog);
        }

        public TransLog GetTransLogData(long arinvtID, long fgmultiID)
        {
            

            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID FROM (SELECT ID FROM TRANSLOG WHERE arinvt_id = {0} AND TRANS_IN_OUT='IN' AND FGMULTI_ID={1} ORDER BY ID DESC) WHERE ROWNUM=1", arinvtID, fgmultiID));
            TransLog transLog = mapper.MapSingle(reader);            
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return transLog;
        }

        public TransLog GetTransLogMasterLabelData(long transLogID)
        {


            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT translog_id AS ID FROM translog_master_label WHERE translog_id = {0}", transLogID));
            TransLog transLog = mapper.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return transLog;
        }

        public long AddTransLogMasterLabel(long transLogID, long masterLabelID, decimal qty)
        {
            long transLogMasterID = 0;
            try
            {
                System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(string.Format(@"
                            INSERT INTO translog_master_label
                             (TRANSLOG_ID, MASTER_LABEL_ID, QTY,STANDARD_ID)
                            VALUES
                            ({0}, {1}, {2}, 0) returning id into :l_id", transLogID, masterLabelID, qty));

                System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                parameter.ParameterName = "l_id";
                parameter.Direction = ParameterDirection.Output;
                parameter.DbType = DbType.Decimal;
                dbCommand.Parameters.Add(parameter);

                var reader = database.ExecuteNonQuery(dbCommand);
                transLogMasterID = Convert.ToInt64(dbCommand.Parameters["l_id"].Value.ToString());
                return transLogMasterID;
            }
            catch (Exception exp)
            {
                return 0;
            }
        }

        public bool UpdateTransLogReason(long id, string reason)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE TRANSLOG SET TRANS_REASON='{1}' WHERE ID={0}", id, reason));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        private static TransLog MapTransLog(IDataReader reader)
        {
            var transLog = new TransLog
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"])
                
            };
            return transLog;
        }

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
