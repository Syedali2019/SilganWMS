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
    public class LabelDataLogic : IDisposable
    {

        private OracleDatabase database;
        private readonly Mapper<Label> mapper;
        private readonly Mapper<MasterLabel> mapperMasterLabel;
        private readonly Mapper<Master_Label> mapperMaster_Label;
        private readonly Mapper<FGMULTI> mapperFGMULTI;
        private readonly Mapper<PCSLabelType> mapperPCSLabelType;
        private readonly Mapper<MasterLabelDetail> mapperMasterLabelDetail;

        public LabelDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<Label>(MapLabel);
            mapperMasterLabel = new Mapper<MasterLabel>(MapMasterLabel);
            mapperMaster_Label = new Mapper<Master_Label>(MapMaster_Label);
            mapperFGMULTI = new Mapper<FGMULTI>(MapFGMULTI);
            mapperMasterLabelDetail = new Mapper<MasterLabelDetail>(MapMasterLabelDetail);
            mapperPCSLabelType = new Mapper<PCSLabelType>(MapPCSLabelType); 
        }

        public Label GetMasterLabelData(string serialNumber, long arinvtID)
        {
            DataSet dataSet = this.database.ExecuteDataSet(CommandType.Text, string.Format(@"SELECT
                                                                                                    ID, 
	                                                                                                SERIAL, 
	                                                                                                ARINVT_ID, 
	                                                                                                FGMULTI_ID,
                                                                                                    QTY
                                                                                                FROM
                                                                                                    MASTER_LABEL
                                                                                                WHERE 
                                                                                                    FGMULTI_ID IN 
                                                                                                    (SELECT 
                                                                                                        f.id
                                                                                                            FROM 
                                                                                                                v_fgmulti_locations f
                                                                                                                JOIN fab_lot_mat_loc fab ON f.id = fab.fgmulti_id(+)
                                                                                                                JOIN arinvt a ON a.id = f.arinvt_id
                                                                                                                JOIN arinvt_lot_docs ald ON f.arinvt_id = ald.arinvt_id AND ald.lotno = f.lotno
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
                                                                                                                AND hard_alloc.get_hard_alloc_to_wo( f.id ) IS NULL
                                                                                                                AND iqms.arinvt_misc.is_lot_expired(f.arinvt_id, f.lotno) <> 1
                                                                                                                AND f.onhand > 0
                                                                                                                AND a.id IN ({0})
                                                                                                    ) AND SERIAL='{1}'", arinvtID, serialNumber));
            Label label=null;
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                label = new Label()
                {
                    ID = (long)dataSet.Tables[0].Rows[0]["ID"],
                    SERIAL_NUMBER = Convert.ToString(dataSet.Tables[0].Rows[0]["SERIAL"]),                    
                    ARINVT_ID = (long)dataSet.Tables[0].Rows[0]["ARINVT_ID"],
                    FGMULTI_ID = dataSet.Tables[0].Rows[0]["FGMULTI_ID"] == System.DBNull.Value ? 0:  (long)dataSet.Tables[0].Rows[0]["FGMULTI_ID"],
                    QUANTITY = dataSet.Tables[0].Rows[0]["QTY"] == System.DBNull.Value ? 0 : Convert.ToDecimal(dataSet.Tables[0].Rows[0]["QTY"])
                };

            dataSet.Dispose();            
            return label;
        }

        public Label GetMasterLabelNotFoundReason(string serialNumber, long arinvtID)
        {
            DataSet dataSet = this.database.ExecuteDataSet(CommandType.Text, string.Format(@"SELECT 
	                                                                                            M.ID,
	                                                                                            M.SERIAL,
	                                                                                            M.ARINVT_ID,
	                                                                                            M.FGMULTI_ID,
                                                                                                M.FG_LOTNO,	
                                                                                                NVL((SELECT hard_alloc.get_hard_alloc_to_wo( F.ID ) FROM DUAL),0) AS WORKODER_HARDALLOC,
	                                                                                            CASE
		                                                                                            WHEN F.non_conform_id IS NOT NULL OR F.non_conform_allocatable = 'N' THEN 'Serial Number {0} belongs to a Non-Conform location'
		                                                                                            WHEN F.non_allocate_id IS NOT NULL THEN 'Serial number {0} belongs to a non-allocatable location'
		                                                                                            WHEN nvl(F.loc_vmi, 'N') = 'Y' THEN ' Serial number {0} belongs to a VMI location'
		                                                                                            WHEN (SELECT nvl(no_backflush, 'N') FROM locations WHERE id = F.loc_id) = 'Y' THEN ' Serial Number {0} belongs to no_backflush'
		                                                                                            WHEN nvl(f.in_transit_origin, 0) = 1 THEN 'Serial number {0} is in transit location'
		                                                                                            WHEN hard_alloc.get_hard_alloc_to_wo( F.id ) IS NOT NULL  THEN 'Serial number {0} is already hard allocated against WO# #WORKORDER'
		                                                                                            WHEN iqms.arinvt_misc.is_lot_expired(F.arinvt_id, F.lotno) = 1 THEN 'Serial number {0} is expired for Lot No# #LOTNO'
		                                                                                            WHEN F.onhand <= 0 THEN 'Quantiy is less than or equal to Zero'
	                                                                                            END REASON
                                                                                            FROM 
	                                                                                            MASTER_LABEL M, 
	                                                                                            v_fgmulti_locations F
                                                                                            WHERE 
	                                                                                            M.FGMULTI_ID=F.ID AND
	                                                                                            M.SERIAL='{0}' ", serialNumber));
            Label label = null;
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                label = new Label()
                {
                    ID = (long)dataSet.Tables[0].Rows[0]["ID"],
                    SERIAL_NUMBER = Convert.ToString(dataSet.Tables[0].Rows[0]["SERIAL"]),
                    ARINVT_ID = (long)dataSet.Tables[0].Rows[0]["ARINVT_ID"],
                    FGMULTI_ID = dataSet.Tables[0].Rows[0]["FGMULTI_ID"] == System.DBNull.Value ? 0 : (long)dataSet.Tables[0].Rows[0]["FGMULTI_ID"],
                    REASON = dataSet.Tables[0].Rows[0]["REASON"] == DBNull.Value ? string.Empty : Convert.ToString(dataSet.Tables[0].Rows[0]["REASON"]),
                    FG_LOTNO = dataSet.Tables[0].Rows[0]["FG_LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(dataSet.Tables[0].Rows[0]["FG_LOTNO"]),
                    WORKORDER = dataSet.Tables[0].Rows[0]["WORKODER_HARDALLOC"] == DBNull.Value ? string.Empty : Convert.ToString(dataSet.Tables[0].Rows[0]["WORKODER_HARDALLOC"])
                };
            dataSet.Dispose();
            return label;
        }

        public MasterLabel GetMasterLabelData(string serialNumber)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ML.ID, ML.SERIAL, ML.FGMULTI_ID, FG.LOTNO, ML.QTY, TO_CHAR(ML.DISPO_DATE, 'MM/DD/YYYY HH:MI:SS') AS DISPO_DATE, ML.FG_LOTNO, TO_CHAR(ML.PRINT_DATE, 'MM/DD/YYYY HH:MI:SS') AS PRINT_DATE  FROM MASTER_LABEL ML INNER JOIN FGMULTI FG ON ML.FGMULTI_ID=FG.ID  WHERE ML.SERIAL='{0}'", serialNumber));
            MasterLabel masterLabel = mapperMasterLabel.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return masterLabel;
        }

        public MasterLabelDetail GetMasterLabelDetailData(string serialNumber)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT
	                                                                                    ML.ID,  
	                                                                                    ML.SERIAL,
	                                                                                    A.ID AS ARINVT_ID,
	                                                                                    A.ITEMNO,
	                                                                                    A.DESCRIP,
	                                                                                    A.DESCRIP2,
	                                                                                    A.UNIT,
	                                                                                    A.CLASS,
	                                                                                    A.REV,
	                                                                                    ML.QTY,
	                                                                                    ML.FGMULTI_ID AS FGMULTI_ID,
                                                                                        FG.ONHAND,        
	                                                                                    ML.LOC_DESC,
                                                                                        FG.LOTNO,
                                                                                        TO_CHAR(ML.DISPO_DATE, 'MM/DD/YYYY HH:MI:SS') AS DISPO_DATE, 
                                                                                        ML.FG_LOTNO 
                                                                                    FROM 
	                                                                                    MASTER_LABEL ML 
	                                                                                    INNER JOIN ARINVT A ON ML.ARINVT_ID=A.ID
	                                                                                    LEFT JOIN FGMULTI FG ON ML.FGMULTI_ID=FG.ID
	                                                                                    LEFT JOIN LOCATIONS L ON FG.LOC_ID=L.ID
                                                                                    WHERE 
	                                                                                    ML.SERIAL='{0}'", serialNumber));
            MasterLabelDetail masterLabel = mapperMasterLabelDetail.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return masterLabel;
        }


        public Master_Label GetMasterLabelData(long id)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID, ARINVT_ID, Boxno, BOX_ID, Class, DESCRIP, DESCRIP2, TO_CHAR(DISPO_DATE, 'MM/DD/YYYY HH:MI:SS') AS DISPO_DATE, DISPO_SCAN, Fgmulti_Id, Fg_Lotno, Itemno,  Lm_Labels_Id, Loc_Desc, TO_CHAR(Lot_Date, 'MM/DD/YYYY HH:MI:SS') AS LOT_DATE, Mfgno, Orderno, Ord_Detail_Id, Pono, Pressno, TO_CHAR(Print_Date, 'MM/DD/YYYY HH:MI:SS') AS PRINT_DATE, Print_Qty, Inv_Cuser1, Inv_Cuser2, TO_CHAR(Orig_Sysdate, 'MM/DD/YYYY HH:MI:SS') AS Orig_Sysdate, Orig_User_Name, Qty, SERIAL, TO_CHAR(Sys_Date, 'MM/DD/YYYY HH:MI:SS') AS Sys_Date, USER_NAME  FROM  MASTER_LABEL WHERE ID={0}", id));
            Master_Label masterLabel = mapperMaster_Label.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return masterLabel;
        }

        public long GetMasterLabelBetween(long id, long labelTypeID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
                                                                                    ID AS MASTER_LABEL_ID 
                                                                                    FROM (
	                                                                                    SELECT 
	                                                                                    ID,
	                                                                                    ROWNUM
	                                                                                    FROM master_label
	                                                                                    WHERE ID <={0} AND LM_LABELS_ID={1}
	                                                                                    ORDER BY ID DESC
                                                                                    ) X WHERE ROWNUM <=2
                                                                                    ORDER BY ID", id, labelTypeID));           
            IEnumerable<Label> lists = mapper.MapList(reader);
            List<Label> labelList = lists as List<Label>;
            Label label = labelList.FirstOrDefault();
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return label.ID;
        }

        public PCSLabelType GetPCSLabelType()
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID FROM(SELECT ID FROM LM_LABELS WHERE LABEL_TYPE='PURCHASED' AND LABEL_MENU_NAME='PCS_KIT_LABEL' ORDER BY ID DESC) WHERE ROWNUM=1"));
            PCSLabelType pCSLabelType = mapperPCSLabelType.MapSingle(reader);           
            
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return pCSLabelType;
        }

        public bool UpdateMasterLabel(long id, long fgMultiID, string locationDescription, string lotNo)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET FG_LOTNO='{0}', LOC_DESC='{1}', FGMULTI_ID={2} WHERE ID={3}", lotNo, locationDescription, fgMultiID, id));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY={0} WHERE SERIAL='{1}'", qty, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }


        public bool UpdateMasterLabelType(string serialNumber, long labelType)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET LM_LABELS_ID={0} WHERE SERIAL='{1}'", labelType, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabelQtyWithType(string serialNumber, decimal qty, long labelType)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET SET QTY={0}, LM_LABELS_ID={1} WHERE SERIAL='{2}'", labelType, qty, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID, long oldSerialID)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY={0}, FGMULTI_ID={1}, REPACKED_MASTER_LABEL_ID={2} WHERE SERIAL='{3}'", qty, fgMULTI_ID, oldSerialID, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID, long oldSerialID, string locationDesc)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY={0}, FGMULTI_ID={1}, REPACKED_MASTER_LABEL_ID={2}, LOC_DESC='{3}' WHERE SERIAL='{4}'", qty, fgMULTI_ID, oldSerialID, locationDesc, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY={0}, FGMULTI_ID={1} WHERE SERIAL='{2}'", qty, fgMULTI_ID, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateMasterLabel(string serialNumber, decimal qty, long fgMULTI_ID, string locationDesc)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY={0}, FGMULTI_ID={1}, LOC_DESC='{2}' WHERE SERIAL='{3}'", qty, fgMULTI_ID, locationDesc, serialNumber));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateFGMULTI_NoShip(long id, string noShip)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE FGMULTI SET NO_SHIP='{1}' WHERE ID={0}", id, noShip));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool AdjustMasterLabelQty(long id, decimal qty)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE MASTER_LABEL SET QTY = QTY + {0} WHERE ID={1}", qty, id));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateSourceFGMULTIOnHand(long id, decimal qty)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE FGMULTI SET ONHAND = (ONHAND - {0}) WHERE ID={1}", qty, id));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        public bool UpdateFGMULTIOnHand(long id, decimal qty)
        {
            try
            {
                var reader = database.ExecuteNonQuery(CommandType.Text, string.Format(@"UPDATE FGMULTI SET ONHAND = {0} WHERE ID={1}", qty, id));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }


        public FGMULTI GetFGMULTIData(long id)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@" SELECT FG.ID, FG.LOTNO, L.LOC_DESC, L.ID  AS LOCATION_ID, NVL(FG.NO_SHIP,'N') AS NO_SHIP FROM FGMULTI FG, LOCATIONS L WHERE FG.LOC_ID=L.ID AND FG.ID={0}", id));
            FGMULTI fgMulti = mapperFGMULTI.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return fgMulti;
        }

        public FGMULTI GetNewFGMULTIData(long workOrderID, long arinvtID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@" SELECT 
	                                                                                WOB_LOC.FGMULTI_ID AS ID ,
	                                                                                FG.LOTNO AS LOTNO,
	                                                                                LOC.ID AS LOCATION_ID,
	                                                                                LOC.LOC_DESC AS LOC_DESC,
	                                                                                NVL(FG.NO_SHIP,'N') AS NO_SHIP
                                                                                FROM 
	                                                                                WORKORDER_BOM WOB 
	                                                                                INNER JOIN WORKORDER_BOM_MAT_LOC WOB_LOC ON WOB_LOC.WORKORDER_BOM_ID=WOB.ID
	                                                                                INNER JOIN FGMULTI FG ON WOB_LOC.FGMULTI_ID = FG.ID
	                                                                                INNER JOIN LOCATIONS LOC ON FG.LOC_ID=LOC.ID
                                                                                WHERE
	                                                                                WOB.WORKORDER_ID={0} AND WOB.ARINVT_ID={1} ", workOrderID, arinvtID));
            FGMULTI fgMulti = mapperFGMULTI.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return fgMulti;
        }
        private static Label MapLabel(IDataReader reader)
        {
            var label = new Label
            {
                ID = (long)reader["MASTER_LABEL_ID"]

            };
            return label;
        }

        private static MasterLabel MapMasterLabel(IDataReader reader)
        {
            var label = new MasterLabel
            {
                MASTER_LABEL_ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                SERIAL= reader["SERIAL"] == DBNull.Value ? string.Empty : Convert.ToString(reader["SERIAL"]),
                FGMULTI_ID = reader["FGMULTI_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["FGMULTI_ID"]),
                LOT_NO = reader["LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOTNO"]),
                QUANTITY = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                DISPO_DATE = reader["DISPO_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DISPO_DATE"]),
                FG_LOTNO = reader["FG_LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["FG_LOTNO"]),
                PRINT_DATE = reader["PRINT_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["PRINT_DATE"]),
            };
            return label;
        }

        private static MasterLabelDetail MapMasterLabelDetail(IDataReader reader)
        {
            var label = new MasterLabelDetail
            {
                MASTER_LABEL_ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                SERIAL = reader["SERIAL"] == DBNull.Value ? string.Empty : Convert.ToString(reader["SERIAL"]),
                ARINVT_ID = reader["ARINVT_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ARINVT_ID"]),
                ITEM_NO = reader["ITEMNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEMNO"]),
                DESCRIPTION = reader["DESCRIP"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP"]),
                DESCRIPTION2 = reader["DESCRIP2"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP2"]),
                UNIT = reader["UNIT"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UNIT"]),
                CLASS = reader["CLASS"] == DBNull.Value ? string.Empty : Convert.ToString(reader["CLASS"]),
                REV = reader["REV"] == DBNull.Value ? string.Empty : Convert.ToString(reader["REV"]),
                FGMULTI_ID = reader["FGMULTI_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["FGMULTI_ID"]),                
                TOTAL_QUANTITY = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                LOCATION_DESC = reader["LOC_DESC"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOC_DESC"]),
                LOT_DESC = reader["LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOTNO"]),
                FGMULTI_ONHAND = reader["ONHAND"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ONHAND"]),
                DISPO_DATE = reader["DISPO_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DISPO_DATE"]),
                LOT_NO = reader["FG_LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["FG_LOTNO"])
            };
            return label;
        }

        private static FGMULTI MapFGMULTI(IDataReader reader)
        {
            var fgMulti = new FGMULTI
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                LOT_NO = reader["LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOTNO"]),
                LOCATION_ID = reader["LOCATION_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["LOCATION_ID"]),
                LOC_DESC = reader["LOC_DESC"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOC_DESC"]),
                NO_SHIP = reader["NO_SHIP"] == DBNull.Value ? "N" : Convert.ToString(reader["NO_SHIP"])
            };
            return fgMulti;
        }

        private static Master_Label MapMaster_Label(IDataReader reader)
        {
            var label = new Master_Label
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                ARINVT_ID = reader["ARINVT_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ARINVT_ID"]),
                BOX_ID = reader["BOX_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["BOX_ID"]),
                SERIAL = reader["SERIAL"] == DBNull.Value ? string.Empty : Convert.ToString(reader["SERIAL"]),
                CLASS = reader["Class"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Class"]),
                DESCRIPTION = reader["DESCRIP"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP"]),
                DESCRIPTION2 = reader["DESCRIP2"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP2"]),
                DISPO_SCAN = reader["DISPO_SCAN"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DISPO_SCAN"]),
                ITEM_NO = reader["ITEMNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEMNO"]),
                FGMULTI_ID = reader["FGMULTI_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["FGMULTI_ID"]),                
                QUANTITY = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                DISPO_DATE = reader["DISPO_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DISPO_DATE"]),
                FG_LOTNO = reader["FG_LOTNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["FG_LOTNO"]),
                PRINT_DATE = reader["PRINT_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["PRINT_DATE"]),
                LM_LABEL_ID = reader["Lm_Labels_Id"] == DBNull.Value ? 0 : Convert.ToInt64(reader["Lm_Labels_Id"]),
                LOC_DESC = reader["Loc_Desc"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Loc_Desc"]),
                LOT_DATE = reader["LOT_DATE"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOT_DATE"]),
                MFG_NO = reader["Mfgno"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Mfgno"]),
                ORDER_NO = reader["Orderno"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Orderno"]),
                ORDER_DETAIL_ID = reader["Ord_Detail_Id"] == DBNull.Value ? 0 : Convert.ToInt64(reader["Ord_Detail_Id"]),
                PO_NO = reader["Pono"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Pono"]),
                PRESS_NO = reader["Pressno"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Pressno"]),
                Print_Qty = reader["Print_Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Print_Qty"]),
                CUSER1 = reader["Inv_Cuser1"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Inv_Cuser1"]),
                CUSER2 = reader["Inv_Cuser2"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Inv_Cuser2"]),
                ORIFINAL_SYSDATE = reader["Orig_Sysdate"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Orig_Sysdate"]),
                ORIGINAL_USERNAME = reader["Orig_User_Name"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Orig_User_Name"]),
                SYS_DATE = reader["Sys_Date"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Sys_Date"]),
                USERNAME = reader["USER_NAME"] == DBNull.Value ? string.Empty : Convert.ToString(reader["USER_NAME"]),
                BOX_NO  = reader["Boxno"] == DBNull.Value ? string.Empty : Convert.ToString(reader["Boxno"]),                
            };
            return label;
        }

        private static PCSLabelType MapPCSLabelType(IDataReader reader)
        {
            var pCSLabelType = new PCSLabelType
            {
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"])
            };
            return pCSLabelType;
        }       


        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

