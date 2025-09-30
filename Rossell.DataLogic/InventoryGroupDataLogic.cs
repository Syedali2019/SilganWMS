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
    public class InventoryGroupDataLogic : IDisposable
    {
        private OracleDatabase database;
        private readonly Mapper<InventoryGroup> mapper;
        private readonly Mapper<PartNumber> mapperPartNumber;
        public InventoryGroupDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<InventoryGroup>(MapInventoryGroup);
            mapperPartNumber = new Mapper<PartNumber>(MapPartNumber);
        }

        public List<InventoryGroup> GetInventoryGroupData()
        {

            var reader = database.ExecuteReader(CommandType.Text, @"SELECT ID, CODE FROM ARINVT_GROUP ORDER BY CODE");
            IEnumerable<InventoryGroup> lists = mapper.MapList(reader);
            List<InventoryGroup> inventoryGroupList = lists as List<InventoryGroup>;
            InventoryGroup invGroup = new InventoryGroup();
            invGroup.CODE = "Select Inventory Group";
            invGroup.ID = 0;            
            inventoryGroupList.Insert(0, invGroup);

            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }

            return inventoryGroupList;
        }

        public List<PartNumber> GetPartNumberData()
        {
            var reader = database.ExecuteReader(CommandType.Text,
                                                        @"SELECT DISTINCT ID, ITEMNO FROM ARINVT
                                                            WHERE ID IN (SELECT ARINVT_ID FROM PARTNO
                                                            WHERE STANDARD_ID IN (SELECT DISTINCT(STANDARD_ID) FROM WORKORDER
                                                            WHERE ID IN (SELECT DISTINCT(WORKORDER_ID) FROM SNDOP_DISPATCH))) ORDER BY ITEMNO");
            IEnumerable<PartNumber> lists = mapperPartNumber.MapList(reader);
            List<PartNumber> partNumberList = lists as List<PartNumber>;
            PartNumber partNumber = new PartNumber();
            partNumber.ID = 0;
            partNumber.ItemNo = "Select Part Number";
            partNumberList.Insert(0, partNumber);

            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }

            return partNumberList;
        }

        public List<PartNumber> GetPartNumberData(long groupID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(
                                                        @"SELECT DISTINCT ID, ITEMNO FROM ARINVT
                                                            WHERE ARINVT_GROUP_ID={0} AND ID IN (SELECT ARINVT_ID FROM PARTNO
                                                            WHERE STANDARD_ID IN (SELECT DISTINCT(STANDARD_ID) FROM WORKORDER
                                                            WHERE ID IN (SELECT DISTINCT(WORKORDER_ID) FROM SNDOP_DISPATCH))) ORDER BY ITEMNO", groupID));
            IEnumerable<PartNumber> lists = mapperPartNumber.MapList(reader);
            List<PartNumber> partNumberList = lists as List<PartNumber>;
            PartNumber partNumber = new PartNumber();
            partNumber.ID = 0;
            partNumber.ItemNo = "Select Part Number";
            partNumberList.Insert(0, partNumber);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return partNumberList;
        }

        private static InventoryGroup MapInventoryGroup(IDataReader reader)
        {
            var inventoryGroup = new InventoryGroup
            {
                ID = (long)reader["ID"],
                CODE=reader["CODE"].ToString()
            };
            return inventoryGroup;
        }

        private static PartNumber MapPartNumber(IDataReader reader)
        {
            var partNumber = new PartNumber
            {
                ID = (long)reader["ID"],
                ItemNo = reader["ITEMNO"].ToString()
            };
            return partNumber;
        }


        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
