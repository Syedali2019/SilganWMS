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
    public class ItemLocationDataLogic : IDisposable
    {
        private OracleDatabase database;
        private readonly Mapper<ItemLocation> mapper;
        private readonly Mapper<Location> mapperLocation;
        private readonly Mapper<Item> mapperItem;

        public ItemLocationDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<ItemLocation>(MapItemLocation);
            mapperLocation = new Mapper<Location>(MapLocation);
            mapperItem = new Mapper<Item>(MapItem);
        }


        public List<ItemLocation> GetItemLocation(string arinvtIDS, DateTime? expiryDate)
        {
            DateTime dtExpiryDate;
            DateTime.TryParse(expiryDate.Value.ToShortDateString(), out dtExpiryDate);
            //var reader = database.ExecuteReader(CommandType.Text, string.Format(@"
            //                                                                    SELECT 
            //                                                                  FG.LOC_ID, 
            //                                                                        L.LOC_DESC, 
            //                                                                        FG.ARINVT_ID, 
            //                                                                        FG.ID AS FGMULTI_ID, 
            //                                                                        FG.ONHAND
            //                                                                   FROM 
            //                                                                  FGMULTI FG
            //                                                                        INNER JOIN LOCATIONS L ON FG.LOC_ID = L.ID
            //                                                                        LEFT JOIN ARINVT_LOT_DOCS ALD ON ALD.ARINVT_ID = FG.ARINVT_ID AND FG.LOTNO = ALD.LOTNO
            //                                                                   WHERE		                                                                                
            //                                                                            FG.ARINVT_ID IN ({0}) AND 
            //                                                                            ((trunc(ALD.EXPIRY_DATE)) IS NULL OR (trunc(ALD.EXPIRY_DATE) >= to_date('{1}','YYYY/MM/DD')))   AND                                                                                        
            //                                                                            FG.ONHAND > 0 AND 
            //                                                                            (FG.non_conform_id IS NULL OR FG.non_conform_allocatable = 'Y') AND 
            //                                                                                    NVL(L.no_backflush,'N')='N' AND 
            //                                                                                    nvl(L.vmi, 'N') <> 'Y' AND 
            //                                                                                    FG.non_allocate_id IS NULL AND 
            //                                                                                    FG.shipment_dtl_id_in_transit IS NULL AND 
            //                                                                                    nvl(FG.in_transit_origin, 0) = 0  AND 
            //                                                                                    nvl(FG.division_id,0) = nvl(0,0)

            //                                                                   ORDER BY 
            //                                                                 FG.ARINVT_ID , L.LOC_DESC", arinvtIDS, expiryDate.Value.ToString("yyyy/MM/dd")));

            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
	                                                                                f.LOC_ID,
	                                                                                f.LOC_DESC,
	                                                                                f.ARINVT_ID,
                                                                                    f.id AS FGMULTI_ID,
	                                                                                f.ONHAND,	                                                                                
                                                                                    '' AS VENDOR_LOT,
                                                                                    '' AS LOTNO
                                                                                FROM 
                                                                                     v_fgmulti_locations f
                                                                                     JOIN fab_lot_mat_loc fab ON f.id = fab.fgmulti_id(+)
                                                                                     JOIN LOCATIONS l ON l.ID = f.LOC_ID
                                                                                     JOIN arinvt a ON a.id = f.arinvt_id
                                                                                     JOIN arinvt_lot_docs ald on f.arinvt_id = ald.arinvt_id AND ald.lotno = f.lotno
                                                                                WHERE 
                                                                                     (f.non_conform_id IS NULL OR f.non_conform_allocatable = 'Y')
                                                                                     AND f.non_allocate_id IS NULL
                                                                                     AND nvl(f.loc_vmi, 'N') <> 'Y'
                                                                                     AND (SELECT nvl(no_backflush, 'N') FROM locations WHERE id = f.loc_id) = 'N'
                                                                                     AND f.shipment_dtl_id_in_transit IS NULL
                                                                                     AND nvl(f.in_transit_origin, 0) = 0  
                                                                                     AND misc.eplant_filter( f.eplant_id ) = 1
                                                                                     AND (0 = 0
                                                                                          OR 0 = 1 AND nvl(f.division_id,0) = nvl(0,0)
                                                                                          OR 0 = 2 AND hard_alloc.location_in_user_work_zone( misc.getusername, f.loc_id ) = 1)
                                                                                     AND hard_alloc.get_hard_alloc_to_wo( f.id ) is null
                                                                                     AND iqms.arinvt_misc.is_lot_expired(f.arinvt_id, f.lotno) <> 1	                                                                                 
                                                                                     AND f.onhand > 0
	                                                                                 AND a.id IN ({0})
                                                                                     AND (l.CUSER2 !='Y' OR l.CUSER2 IS NULL)
                                                                                     AND a.CLASS NOT IN('PS','PK')
	                                                                                 AND ((trunc(ALD.EXPIRY_DATE)) IS NULL OR (trunc(ALD.EXPIRY_DATE) >= to_date('{1}','YYYY/MM/DD')))
                                                                                     AND (f.LOC_DESC LIKE 'C%' OR f.LOC_DESC LIKE 'K%')
                                                                                ORDER BY 
                                                                                     f.in_date,
                                                                                     f.loc_desc,
	                                                                                 f.onhand desc", arinvtIDS, dtExpiryDate.ToString("yyyy/MM/dd")));

            IEnumerable<ItemLocation> lists = mapper.MapList(reader);
            List<ItemLocation> itemLocationList = lists as List<ItemLocation>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return itemLocationList;
        }

        public List<ItemLocation> GetItemLocation(string arinvtIDS, DateTime? expiryDate, double totalRequiredQty)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
	                                                                                f.LOC_ID,
	                                                                                f.LOC_DESC,
	                                                                                f.ARINVT_ID,
                                                                                    f.id AS FGMULTI_ID,
	                                                                                f.ONHAND,	                                                                                
                                                                                    '' AS VENDOR_LOT,
                                                                                    '' AS LOTNO
                                                                                FROM 
                                                                                     v_fgmulti_locations f
                                                                                     JOIN fab_lot_mat_loc fab ON f.id = fab.fgmulti_id(+)
                                                                                     JOIN LOCATIONS l ON l.ID = f.LOC_ID 
                                                                                     JOIN arinvt a ON a.id = f.arinvt_id
                                                                                     JOIN arinvt_lot_docs ald on f.arinvt_id = ald.arinvt_id AND ald.lotno = f.lotno
                                                                                WHERE 
                                                                                     (f.non_conform_id IS NULL OR f.non_conform_allocatable = 'Y')
                                                                                     AND f.non_allocate_id IS NULL
                                                                                     AND nvl(f.loc_vmi, 'N') <> 'Y'
                                                                                     AND (SELECT nvl(no_backflush, 'N') FROM locations WHERE id = f.loc_id) = 'N'
                                                                                     AND f.shipment_dtl_id_in_transit IS NULL
                                                                                     AND nvl(f.in_transit_origin, 0) = 0  
                                                                                     AND misc.eplant_filter( f.eplant_id ) = 1
                                                                                     AND (0 = 0
                                                                                          OR 0 = 1 AND nvl(f.division_id,0) = nvl(0,0)
                                                                                          OR 0 = 2 AND hard_alloc.location_in_user_work_zone( misc.getusername, f.loc_id ) = 1)
                                                                                     AND hard_alloc.get_hard_alloc_to_wo( f.id ) is null
                                                                                     AND iqms.arinvt_misc.is_lot_expired(f.arinvt_id, f.lotno) <> 1	                                                                                 
                                                                                     AND f.onhand > 0
	                                                                                 AND a.id IN ({0})
                                                                                     AND (l.CUSER2 !='Y' OR l.CUSER2 IS NULL)
	                                                                                 AND ((trunc(ALD.EXPIRY_DATE)) IS NULL OR (trunc(ALD.EXPIRY_DATE) >= to_date('{1}','YYYY/MM/DD')))  
                                                                                     AND f.onhand >= {2}
                                                                                     AND (f.LOC_DESC LIKE 'C%' OR f.LOC_DESC LIKE 'K%')
                                                                                ORDER BY 
                                                                                     f.in_date,
                                                                                     f.loc_desc,
	                                                                                 f.onhand desc", arinvtIDS, expiryDate.Value.ToString("yyyy/MM/dd"), totalRequiredQty));

            IEnumerable<ItemLocation> lists = mapper.MapList(reader);
            List<ItemLocation> itemLocationList = lists as List<ItemLocation>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return itemLocationList;
        }

        public List<ItemLocation> GetItemLocation(long arinvtID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT
                                                                                     f.LOC_ID,                                                                                   
                                                                                     f.LOC_DESC,
                                                                                     f.ONHAND ,
                                                                                     f.ARINVT_ID, 
                                                                                     f.id AS FGMULTI_ID,
                                                                                     f.ONHAND,
                                                                                     ald.cuser1 AS VENDOR_LOT,
                                                                                     f.LOTNO
                                                                                FROM 
                                                                                     v_fgmulti_locations f
                                                                                     JOIN fab_lot_mat_loc fab ON f.id = fab.fgmulti_id(+)
                                                                                     JOIN LOCATIONS l ON l.ID = f.LOC_ID
                                                                                     JOIN arinvt a ON a.id = f.arinvt_id
                                                                                     JOIN arinvt_lot_docs ald on f.arinvt_id = ald.arinvt_id AND ald.lotno = f.lotno
                                                                                WHERE 
                                                                                     (f.non_conform_id IS NULL OR f.non_conform_allocatable = 'Y')
                                                                                     AND f.non_allocate_id IS NULL
                                                                                     AND nvl(f.loc_vmi, 'N') <> 'Y'
                                                                                     AND (SELECT nvl(no_backflush, 'N') FROM locations WHERE id = f.loc_id) = 'N'
                                                                                     AND f.shipment_dtl_id_in_transit IS NULL
                                                                                     AND nvl(f.in_transit_origin, 0) = 0  
                                                                                     AND misc.eplant_filter( f.eplant_id ) = 1
                                                                                     AND (0 = 0
                                                                                          OR 0 = 1 AND nvl(f.division_id,0) = nvl(0,0)
                                                                                          OR 0 = 2 AND hard_alloc.location_in_user_work_zone( misc.getusername, f.loc_id ) = 1)
                                                                                     AND hard_alloc.get_hard_alloc_to_wo( f.id ) is null
                                                                                     AND iqms.arinvt_misc.is_lot_expired(f.arinvt_id, f.lotno) <> 1                                                                                    
                                                                                     AND (l.CUSER2 !='Y' OR l.CUSER2 IS NULL)
                                                                                     AND f.onhand > 0
                                                                                     AND a.id IN ({0})
                                                                                     AND (f.LOC_DESC LIKE 'C%' OR f.LOC_DESC LIKE 'K%')
                                                                                    ORDER BY 
                                                                                        f.LOC_DESC", arinvtID));

            IEnumerable<ItemLocation> lists = mapper.MapList(reader);
            List<ItemLocation> itemLocationList = lists as List<ItemLocation>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return itemLocationList;
        }

        public Location GetLocation(string locationName, long arinvtID, string lotNO)
        {
            Location location = null;
            if (!lotNO.Equals(""))
            {
                var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT L.ID, L.LOC_DESC, L.LOC_DESC AS ScanLocation, FG.ID AS FGMULTI_ID FROM LOCATIONS L LEFT JOIN FGMULTI FG ON L.ID=FG.LOC_ID WHERE UPPER(L.LOC_DESC)='{0}' AND FG.ARINVT_ID={1} AND FG.LOTNO='{2}'", locationName.ToUpper(), arinvtID, lotNO));
                location = mapperLocation.MapSingle(reader);
                if (reader.IsClosed == false)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            else
            {
                var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT L.ID, L.LOC_DESC, L.LOC_DESC AS ScanLocation, FG.ID AS FGMULTI_ID FROM LOCATIONS L LEFT JOIN FGMULTI FG ON L.ID=FG.LOC_ID WHERE UPPER(L.LOC_DESC)='{0}' AND FG.ARINVT_ID={1} AND FG.LOTNO IS NULL", locationName.ToUpper(), arinvtID));
                location = mapperLocation.MapSingle(reader);
                if (reader.IsClosed == false)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return location;
        }

        public Location GetLocation(string locationName)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT L.ID, L.LOC_DESC, L.LOC_DESC AS ScanLocation, FG.ID AS FGMULTI_ID FROM LOCATIONS L LEFT JOIN FGMULTI FG ON L.ID=FG.LOC_ID WHERE UPPER(L.LOC_DESC)='{0}'", locationName.ToUpper()));

            Location location = mapperLocation.MapSingle(reader);

            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return location;
        }

        public Location GetLocation(long id)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT L.ID, L.LOC_DESC, L.LOC_DESC AS ScanLocation, FG.ID AS FGMULTI_ID FROM LOCATIONS L LEFT JOIN FGMULTI FG ON L.ID=FG.LOC_ID WHERE L.ID={0}", id));

            Location location = mapperLocation.MapSingle(reader);

            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return location;
        }

        public Item GetItemData(long arinvtID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT
	                                                                                    ID,
	                                                                                    ITEMNO,
	                                                                                    DESCRIP,
	                                                                                    DESCRIP2,
	                                                                                    UNIT,
	                                                                                    CLASS,
	                                                                                    REV,
                                                                                        STANDARD_ID
                                                                                    FROM 
	                                                                                    ARINVT	                                                                                    
                                                                                    WHERE 
	                                                                                    ID={0}", arinvtID));
            Item item = mapperItem.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return item;
        }

        public long AddLocation(string locationName)
        {
            long locationID = 0;
            try
            {
                System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(string.Format(@"
                            INSERT INTO LOCATIONS 
                            (LOC_DESC, IS_STAGING_AREA)
                            VALUES
                            ('{0}', 'N') returning id into :l_id", locationName.ToUpper()));

                System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                parameter.ParameterName = "l_id";
                parameter.Direction = ParameterDirection.Output;
                parameter.DbType = DbType.Decimal;
                dbCommand.Parameters.Add(parameter);

                var reader = database.ExecuteNonQuery(dbCommand);
                locationID = Convert.ToInt64(dbCommand.Parameters["l_id"].Value.ToString());
                return locationID;
            }
            catch (Exception exp)
            {
                return 0;
            }
        }

        private static ItemLocation MapItemLocation(IDataReader reader)
        {
            var itemLocation = new ItemLocation
            {
                LOC_ID = (long)reader["LOC_ID"],
                LOC_DESCRIPTION = Convert.ToString(reader["LOC_DESC"]),
                ARINVT_ID = (long)reader["ARINVT_ID"],
                FGMULTI_ID = (reader["FGMULTI_ID"] == System.DBNull.Value) ? 0 : Convert.ToInt64(reader["FGMULTI_ID"]),
                ON_HAND = (reader["ONHAND"] == System.DBNull.Value) ? 0 : Convert.ToDouble(reader["ONHAND"]),
                ON_HAND_UPDATED = (reader["ONHAND"] == System.DBNull.Value) ? 0 : Convert.ToDouble(reader["ONHAND"]),
                VENDOR_LOT_NUMBER = (reader["VENDOR_LOT"] == System.DBNull.Value) ? string.Empty : Convert.ToString(reader["VENDOR_LOT"]),
                LOT_NUMBER = (reader["LOTNO"] == System.DBNull.Value) ? string.Empty : Convert.ToString(reader["LOTNO"]),
            };
            return itemLocation;
        }

        private static Location MapLocation(IDataReader reader)
        {
            var location = new Location
            {
                ID = (reader["ID"] == System.DBNull.Value) ? 0 : Convert.ToInt64(reader["ID"]),
                LocationName = (reader["LOC_DESC"] == System.DBNull.Value) ? string.Empty : Convert.ToString(reader["LOC_DESC"]),
                ScanLocation = (reader["ScanLocation"] == System.DBNull.Value) ? string.Empty : Convert.ToString(reader["ScanLocation"]),
                FGMULI_ID = (reader["FGMULTI_ID"] == System.DBNull.Value) ? 0 : Convert.ToInt64(reader["FGMULTI_ID"])

            };
            return location;
        }

        private static Item MapItem(IDataReader reader)
        {
            var item = new Item
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                STANDARD_ID = reader["STANDARD_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["STANDARD_ID"]),
                ITEM_NO = reader["ITEMNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEMNO"]),
                DESCRIPTION = reader["DESCRIP"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP"]),
                DESCRIPTION2 = reader["DESCRIP2"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP2"]),
                UNIT = reader["UNIT"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UNIT"]),
                CLASS = reader["CLASS"] == DBNull.Value ? string.Empty : Convert.ToString(reader["CLASS"]),
                REV = reader["REV"] == DBNull.Value ? string.Empty : Convert.ToString(reader["REV"])
            };
            return item;
        }



        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
