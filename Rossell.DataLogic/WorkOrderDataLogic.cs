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
    public class WorkOrderDataLogic : IDisposable
    {
        private OracleDatabase database;
        private readonly Mapper<WorkOrder> mapper;
        private readonly Mapper<WorkOrderRawMaterial> mapperRM;
        private readonly Mapper<MFGCell> mapperMFG;
        private readonly Mapper<WorkOrderAssignBOM> mapperWorkOrderAssignBOM;
        private readonly Mapper<WorkOrderSingle> mapperWOSingle;
        private readonly Mapper<WorkOrderBOMSingle> mapperWOBOMSingle;
        public WorkOrderDataLogic()
        {
            database = new OracleDatabase(ConfigurationManager.AppSettings["OracleDB"]);
            mapper = new Mapper<WorkOrder>(MapWorkOrder);
            mapperRM = new Mapper<WorkOrderRawMaterial>(MapWorkOrderRM);
            mapperMFG = new Mapper<MFGCell>(MapWorkOrderMFG);
            mapperWorkOrderAssignBOM = new Mapper<WorkOrderAssignBOM>(MapWorkOrderAssignBOM);
            mapperWOSingle = new Mapper<WorkOrderSingle>(MapWorkOrderSingle);
            mapperWOBOMSingle = new Mapper<WorkOrderBOMSingle>(MapWorkOrderBOMSingle);
        }

        public List<WorkOrder> GetWorkOrderData(long arinvtID, long arinvtGroupID, DateTime? mustStartDate, DateTime? expiryDate, int mustStartDaysAdd)
        {
            string arinvtWhereClause = string.Empty;
            string InventoryGroupWhereClause = string.Empty;
            string mustStartDateWhereClause = string.Empty;
            string expiryWhereClause = string.Empty;            

            if (arinvtID > 0)
            {
                arinvtWhereClause = " AND AR.ID="+ arinvtID.ToString() + "";
            }

            if (arinvtGroupID > 0)
            {
                InventoryGroupWhereClause = " AND AR.ARINVT_GROUP_ID=" + arinvtGroupID.ToString() + "";
            }

            if (mustStartDate!=null && mustStartDate.Value != null )
            {
                //DateTime mustStartDateUpdated = Convert.ToDateTime(mustStartDate.Value.ToString("dd-MM-yyyy"));
                DateTime mustStartDateUpdated;
                DateTime.TryParse(mustStartDate.Value.ToShortDateString(), out mustStartDateUpdated);
                mustStartDateWhereClause = " AND trunc(WO.START_TIME) <= to_date('" + mustStartDateUpdated.ToString("yyyy/MM/dd") + "','YYYY/MM/DD') ";
            }

            if (expiryDate != null && expiryDate.Value != null)
            {
                //expiryWhereClause = " AND trunc(ALD.EXPIRY_DATE) <= to_date('" + expiryDate.Value.ToString("yyyy/MM/dd") + "','YYYY/MM/DD') ";
            }

            var reader = database.ExecuteReader(CommandType.Text, @"SELECT WorkOrderID, ITEMNO, START_TIME, REQ_QTY, HARD_ALLOC, KIT_ACK_QTY,
                                                                        CASE
                                                                            WHEN KIT_ACK_QTY=0 AND HARD_ALLOC=0 THEN 0
	                                                                        WHEN KIT_ACK_QTY=0 AND (HARD_ALLOC < REQ_QTY) THEN 1
	                                                                        WHEN KIT_ACK_QTY != 0 AND (HARD_ALLOC < REQ_QTY) THEN -1
                                                                        END COLOR_CODE,
                                                                        CASE
                                                                            WHEN KIT_ACK_QTY=0 AND HARD_ALLOC=0 THEN 2
                                                                            WHEN KIT_ACK_QTY=0 AND (HARD_ALLOC < REQ_QTY) THEN 0
                                                                            WHEN KIT_ACK_QTY != 0 AND (HARD_ALLOC < REQ_QTY) THEN -1
                                                                        END COLOR_CODE_2,   
                                                                        CASE
                                                                            WHEN KIT_ACK_QTY=0 AND HARD_ALLOC=0 THEN NVL(START_TIME,TO_DATE('1900/01/01', 'YYYY/MM/DD'))
                                                                            WHEN KIT_ACK_QTY=0 AND (HARD_ALLOC < REQ_QTY) THEN NVL(START_TIME,TO_DATE('1900/01/01', 'YYYY/MM/DD'))	                                                                        
                                                                            WHEN KIT_ACK_QTY != 0 AND (HARD_ALLOC < REQ_QTY) THEN NVL(START_TIME,TO_DATE('1900/01/01', 'YYYY/MM/DD'))
                                                                        END START_TIME_DISP
                                                                    FROM (SELECT 
	                                                                    DISTINCT WO.ID AS WorkOrderID,
                                                                        AR.ITEMNO,
                                                                        WO.START_TIME,
                                                                        NVL((SELECT SUM(WO1.QUAN) FROM WORKORDER_BOM WO1, ARINVT A WHERE WO1.ARINVT_ID=A.ID AND WO1.WORKORDER_ID=WO.ID AND WO1.PARENT_ID IS NOT NULL AND A.CLASS NOT IN('PS','PK')),0) AS REQ_QTY,
                                                                        NVL((SELECT SUM(f.onhand)     
                                                                                FROM
                                                                                    workorder_bom_mat_loc w, 
                                                                                    v_fgmulti_locations f,
                                                                                    arinvt a
                                                                                WHERE w.workorder_bom_id IN (SELECT ID FROM WORKORDER_BOM WHERE WORKORDER_ID=WO.ID)
                                                                                      AND w.fgmulti_id = f.id(+) AND f.arinvt_id=a.id and a.class NOT IN('PS','PK')),0) AS HARD_ALLOC,
                                                                        NVL((SELECT SUM(RECEIVED_QTY) FROM ARISOFT_KITACK WHERE WORKORDER_ID=WO.ID) ,0) AS KIT_ACK_QTY
                                                                    FROM 
                                                                        WORKORDER_BOM WOB INNER JOIN WORKORDER WO ON WOB.WORKORDER_ID=WO.ID
	                                                                    --SNDOP_DISPATCH SND INNER JOIN WORKORDER WO ON SND.WORKORDER_ID=WO.ID
                                                                        INNER JOIN PARTNO PN  ON WO.STANDARD_ID=PN.STANDARD_ID 
                                                                        INNER JOIN ARINVT AR  ON PN.ARINVT_ID=AR.ID
                                                                        --LEFT JOIN WORKORDER_BOM WOB  ON WO.ID=WOB.WORKORDER_ID
                                                                        INNER JOIN ARINVT_LOT_DOCS ALD ON WOB.ARINVT_ID=ALD.ARINVT_ID
                                                                        INNER JOIN FGMULTI FG ON FG.LOTNO = ALD.LOTNO AND FG.ARINVT_ID = ALD.ARINVT_ID
                                                                    WHERE
	                                                                    1=1 "
                                                                        + arinvtWhereClause 
                                                                        + InventoryGroupWhereClause 
                                                                        + mustStartDateWhereClause
                                                                        + expiryWhereClause
                                                                        + ") X WHERE ((X.REQ_QTY > 0) AND (X.REQ_QTY > X.HARD_ALLOC))  ORDER BY COLOR_CODE_2, START_TIME_DISP, COLOR_CODE --OR (X.REQ_QTY=0) ORDER BY START_TIME");
            IEnumerable<WorkOrder> lists = mapper.MapList(reader);
            List<WorkOrder> workOrderList = lists as List<WorkOrder>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return workOrderList;            
        }

        public List<WorkOrderRawMaterial> GetWorkOrderRawMaterial(string workOrderIDs)
        {

            /*var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ROWNUM AS SEQ_ID, 'N/A' AS UOM, 0 AS LOC_ID, '' AS LOC_DESC,0 AS IS_PICKED, 0 AS IS_ERROR, '' AS ERROR_DESCRIPTION, 0 AS  MASTER_LABEL_ID,X.* FROM (SELECT DISTINCT 
		                                                                            workorder.id AS WORKORDER_ID,
                                                                                   standard.id as STANDARD_ID,
	                                                                               arinvt.id as ARINVT_ID_FG,
	                                                                               arinvt.itemno as ITEM_NO,
	                                                                               SUM(PTORDER_REL.REL_QUAN_DISP) AS REL_QUANTITY,
	                                                                               opmat.SNDOP_ID, 
	                                                                               opmat.ARINVT_ID AS ARINVT_ID_RM, 
	                                                                               opmat.PTSPER, 
	                                                                               opmat.UNIT,
	                                                                               ar.ITEMNO,
	                                                                               ar.DESCRIP,
	                                                                               NVL(ar.DESCRIP2,'N/A') AS DESCRIP2,
	                                                                               (SUM(PTORDER_REL.REL_QUAN_DISP) * opmat.PTSPER) AS REQ_QUANTITY
	   
                                                                              FROM 
		                                                                            workorder, partno, arinvt, standard, PTORDER, PTORDER_REL, sndop_dispatch, sndop, opmat, arinvt ar
                                                                             WHERE 
		                                                                            workorder.id IN ({0})
                                                                               and partno.standard_id = workorder.standard_id
                                                                               and partno.arinvt_id = arinvt.id       
                                                                               and workorder.standard_id = standard.id
                                                                               and WORKORDER.ID=PTORDER.WORKORDER_ID 
                                                                               AND PTORDER_REL.PTORDER_ID=PTORDER.ID
                                                                               AND WORKORDER.ID=sndop_dispatch.WORKORDER_ID
                                                                               AND sndop_dispatch.sndop_id=sndop.ID 
                                                                               AND sndop.ID=opmat.SNDOP_ID 
                                                                               AND opmat.ARINVT_ID=ar.ID 
                                                                            GROUP BY 	
		                                                                            ROWNUM,
		                                                                            workorder.id,
                                                                                   standard.id,
	                                                                               arinvt.id,
                                                                                   arinvt.itemno,
	                                                                               opmat.SNDOP_ID, 
	                                                                               opmat.ARINVT_ID, 
	                                                                               opmat.PTSPER, 
	                                                                               opmat.UNIT,
	                                                                               ar.ITEMNO,
	                                                                               ar.DESCRIP,
	                                                                               ar.DESCRIP2
                                                                            ORDER BY 7,1) X ORDER BY SEQ_ID", workOrderIDs));*/

            //var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT
            //                                                                        ROWNUM AS SEQ_ID,
            //                                                                        ARINVT.UNIT AS UOM,
            //                                                                        -1 AS LOC_ID,
            //                                                                        '' AS LOC_DESC,
            //                                                                        0 AS IS_PICKED,
            //                                                                        0 AS IS_ERROR,
            //                                                                        0 AS IS_SKIPPED,
            //                                                                        '' AS ERROR_DESCRIPTION,
            //                                                                        0 AS  MASTER_LABEL_ID,
            //                                                                        -1 AS FGMULTI_ID,
            //                                                                        0 AS  NEW_MASTER_LABEL_ID,
            //                                                                        WOB.ID AS WORKORDER_BOM_ID,
            //                                                                        WOB.WORKORDER_ID,
            //                                                                        WOB.PARENT_STANDARD_ID AS STANDARD_ID,
            //                                                                        WOB.PARENT_ARINVT_ID AS ARINVT_ID_FG,
            //                                                                        arinvt.itemno as ITEM_NO,
            //                                                                        WOB.QUAN REL_QUANTITY,
            //                                                                        CASE
            //                                                                   WHEN WOB.QUAN > 0 THEN WOB.QUAN - (NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+)),0))
            //                                                                   WHEN WOB.QUAN <= 0 THEN 0
            //                                                                  END  REQ_QUANTITY,
            //                                                                        NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+)),0) AS HARD_ALLOCATION,
            //                                                                        WOB.SNDOP_ID,
            //                                                                        WOB.ARINVT_ID AS ARINVT_ID_RM,
            //                                                                        OP.PTSPER,
            //                                                                        OP.UNIT,
            //                                                                        ar.ITEMNO,
            //                                                                        ar.DESCRIP,
            //                                                                        NVL(ar.DESCRIP2, 'N/A') AS DESCRIP2,
            //                                                                        (SELECT     
            //                                                                            mc.staging_locations_id
            //                                                                         FROM workorder wo
            //                                                                            JOIN standard s ON wo.standard_id = s.ID
            //                                                                            JOIN mfgcell mc ON s.mfgcell_id = mc.id
            //                                                                         WHERE 
            //                                                                            wo.id = WOB.WORKORDER_ID) AS STAGING_LOCATIONS_ID

            //                                                                      FROM
            //                                                                        WORKORDER_BOM WOB,
            //                                                                        ARINVT,
            //                                                                        ARINVT AR,
            //                                                                        OPMAT OP

            //                                                                      WHERE
            //                                                                        WOB.PARENT_ARINVT_ID = ARINVT.ID AND
            //                                                                        WOB.ARINVT_ID = AR.ID AND
            //                                                                        WOB.SNDOP_ID = OP.SNDOP_ID AND
            //                                                                        OP.ARINVT_ID = AR.ID AND
            //                                                                        WOB.PARENT_ID > 0 AND
            //                                                                        WOB.WORKORDER_ID IN({0})

            //                                                                      ORDER BY
            //                                                                        WOB.ARINVT_ID, WOB.WORKORDER_ID  ", workOrderIDs));

            //var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
            //                                                                            ROWNUM AS SEQ_ID,
            //                                                                            -1 AS LOC_ID,
            //                                                                            '' AS LOC_DESC,
            //                                                                            0 AS IS_PICKED,
            //                                                                            0 AS IS_ERROR,
            //                                                                            0 AS IS_SKIPPED,
            //                                                                            '' AS ERROR_DESCRIPTION,
            //                                                                            0 AS  MASTER_LABEL_ID,
            //                                                                            -1 AS FGMULTI_ID,
            //                                                                            0 AS  NEW_MASTER_LABEL_ID,
            //                                                                            0 AS WORKORDER_BOM_ID,
            //                                                                            0 AS SNDOP_ID,
            //                                                                            A.* FROM 
            //                                                                            (
            //                                                                            SELECT 
            //                                                                            UOM,
            //                                                                            WORKORDER_ID,
            //                                                                            STANDARD_ID,
            //                                                                            ARINVT_ID_FG,
            //                                                                            ITEM_NO,
            //                                                                            SUM(REL_QUANTITY) AS REL_QUANTITY,
            //                                                                            SUM(REQ_QUANTITY) AS REQ_QUANTITY,
            //                                                                            SUM(HARD_ALLOCATION) AS HARD_ALLOCATION,
            //                                                                            ARINVT_ID_RM,
            //                                                                            SUM(PTSPER) AS PTSPER,
            //                                                                            UNIT,
            //                                                                            ITEMNO,
            //                                                                            DESCRIP,
            //                                                                            DESCRIP2,
            //                                                                            STAGING_LOCATIONS_ID

            //                                                                            FROM 
            //                                                                            (
            //                                                                             SELECT
            //                                                                                 ARINVT.UNIT AS UOM,    
            //                                                                                 WOB.ID AS WORKORDER_BOM_ID,
            //                                                                                 WOB.WORKORDER_ID,
            //                                                                                 WOB.PARENT_STANDARD_ID AS STANDARD_ID,
            //                                                                                 WOB.PARENT_ARINVT_ID AS ARINVT_ID_FG,
            //                                                                                 arinvt.itemno as ITEM_NO,
            //                                                                                 WOB.QUAN REL_QUANTITY,
            //                                                                                 CASE
            //                                                                                     WHEN WOB.QUAN > 0 THEN WOB.QUAN - (NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+)),0))
            //                                                                                     WHEN WOB.QUAN <= 0 THEN 0
            //                                                                                 END  REQ_QUANTITY,
            //                                                                                 NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+)),0) AS HARD_ALLOCATION,
            //                                                                                 WOB.SNDOP_ID,
            //                                                                                 WOB.ARINVT_ID AS ARINVT_ID_RM,
            //                                                                                 OP.PTSPER,
            //                                                                                 OP.UNIT,
            //                                                                                 ar.ITEMNO,
            //                                                                                 ar.DESCRIP,
            //                                                                                 NVL(ar.DESCRIP2, 'N/A') AS DESCRIP2,
            //                                                                                 (SELECT     
            //                                                                                     mc.staging_locations_id
            //                                                                                     FROM workorder wo
            //                                                                                     JOIN standard s ON wo.standard_id = s.ID
            //                                                                                     JOIN mfgcell mc ON s.mfgcell_id = mc.id
            //                                                                                     WHERE 
            //                                                                                     wo.id = WOB.WORKORDER_ID) AS STAGING_LOCATIONS_ID

            //                                                                                 FROM
            //                                                                                 WORKORDER_BOM WOB,
            //                                                                                 ARINVT,
            //                                                                                 ARINVT AR,
            //                                                                                 OPMAT OP

            //                                                                                 WHERE
            //                                                                                 WOB.PARENT_ARINVT_ID = ARINVT.ID AND
            //                                                                                 WOB.ARINVT_ID = AR.ID AND
            //                                                                                 WOB.SNDOP_ID = OP.SNDOP_ID AND
            //                                                                                 OP.ARINVT_ID = AR.ID AND
            //                                                                                 WOB.PARENT_ID > 0 AND
            //                                                                                 WOB.WORKORDER_ID IN({0})

            //                                                                                 ORDER BY
            //                                                                                 WOB.ARINVT_ID, WOB.WORKORDER_ID
            //                                                                             ) X

            //                                                                            GROUP BY 
            //                                                                             UOM,
            //                                                                             WORKORDER_ID,
            //                                                                             STANDARD_ID,
            //                                                                             ARINVT_ID_FG,
            //                                                                             ITEM_NO,
            //                                                                             ARINVT_ID_RM,
            //                                                                             UNIT,
            //                                                                             ITEMNO,
            //                                                                             DESCRIP,
            //                                                                             DESCRIP2,
            //                                                                             STAGING_LOCATIONS_ID

            //                                                                                 ORDER BY
            //                                                                                 ARINVT_ID_RM, WORKORDER_ID	
            //                                                                            ) A
            //                                                                            ORDER BY 
            //                                                                             ARINVT_ID_RM,
            //                                                                             WORKORDER_ID  ", workOrderIDs));

            //var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
            //                                                                            ROWNUM AS SEQ_ID,
            //                                                                            -1 AS LOC_ID,
            //                                                                            '' AS LOC_DESC,
            //                                                                            0 AS IS_PICKED,
            //                                                                            0 AS IS_ERROR,
            //                                                                            0 AS IS_SKIPPED,
            //                                                                            '' AS ERROR_DESCRIPTION,
            //                                                                            0 AS  MASTER_LABEL_ID,
            //                                                                            -1 AS FGMULTI_ID,
            //                                                                            0 AS  NEW_MASTER_LABEL_ID,
            //                                                                            0 AS WORKORDER_BOM_ID,
            //                                                                            0 AS SNDOP_ID,
            //                                                                            A.* FROM 
            //                                                                            (
            //                                                                            SELECT 
            //                                                                            UOM,
            //                                                                            WORKORDER_ID,
            //                                                                            STANDARD_ID,
            //                                                                            ARINVT_ID_FG,
            //                                                                            ITEM_NO,
            //                                                                            SUM(REL_QUANTITY) AS REL_QUANTITY,
            //                                                                            SUM(REQ_QUANTITY) AS REQ_QUANTITY,
            //                                                                            SUM(HARD_ALLOCATION) AS HARD_ALLOCATION,
            //                                                                            ARINVT_ID_RM,
            //                                                                            SUM(PTSPER) AS PTSPER,
            //                                                                            UNIT,
            //                                                                            ITEMNO,
            //                                                                            DESCRIP,
            //                                                                            DESCRIP2,
            //                                                                            STAGING_LOCATIONS_ID

            //                                                                            FROM 
            //                                                                            (
            //                                                                                SELECT
            //                                                                                    A.UNIT AS UOM,    
            //                                                                                    WOB.ID AS WORKORDER_BOM_ID,
            //                                                                                    WOB.WORKORDER_ID,
            //                                                                                    WOB.PARENT_STANDARD_ID AS STANDARD_ID,
            //                                                                                    WOB.PARENT_ARINVT_ID AS ARINVT_ID_FG,
            //                                                                                    A.itemno as ITEM_NO,
            //                                                                                    WOB.QUAN REL_QUANTITY,
            //                                                                                    CASE
            //                                                                                        WHEN WOB.QUAN > 0 THEN WOB.QUAN - (NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f, arinvt a WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+) AND f.arinvt_id=a.id and a.class NOT IN ('PS','PK')),0))
            //                                                                                        WHEN WOB.QUAN <= 0 THEN 0
            //                                                                                    END  REQ_QUANTITY,
            //                                                                                    NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f, arinvt a WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+) AND f.arinvt_id=a.id and a.class NOT IN('PS','PK')),0) AS HARD_ALLOCATION,
            //                                                                                    WOB.SNDOP_ID,
            //                                                                                    WOB.ARINVT_ID AS ARINVT_ID_RM,
            //                                                                                    CASE NVL(WOB.MANUAL,'N')
            //	                                                                                    WHEN 'Y' THEN WOB.PTSPER_INTERNAL 
            //	                                                                                    WHEN 'N' THEN OP.PTSPER
            //                                                                                    END AS PTSPER, 
            //                                                                           AR.UNIT,
            //                                                                                    ar.ITEMNO,
            //                                                                                    ar.DESCRIP,
            //                                                                                    NVL(ar.DESCRIP2, 'N/A') AS DESCRIP2,
            //                                                                                    (SELECT     
            //                                                                                        mc.staging_locations_id
            //                                                                                        FROM workorder wo
            //                                                                                        JOIN standard s ON wo.standard_id = s.ID
            //                                                                                        JOIN mfgcell mc ON s.mfgcell_id = mc.id
            //                                                                                        WHERE 
            //                                                                                        wo.id = WOB.WORKORDER_ID) AS STAGING_LOCATIONS_ID

            //                                                                                    FROM
            //	                                                                                    WORKORDER_BOM WOB INNER JOIN ARINVT A ON WOB.PARENT_ARINVT_ID = A.ID
            //	                                                                                    INNER JOIN ARINVT AR ON WOB.ARINVT_ID = AR.ID
            //	                                                                                    LEFT JOIN OPMAT OP ON WOB.SNDOP_ID = OP.SNDOP_ID AND OP.ARINVT_ID = AR.ID

            //                                                                                    WHERE
            //	                                                                                    WOB.PARENT_ID > 0 AND
            //                                                                                        AR.CLASS NOT IN('PS','PK') AND
            //	                                                                                    WOB.WORKORDER_ID IN({0})

            //                                                                                    ORDER BY
            //	                                                                                    WOB.ARINVT_ID, WOB.WORKORDER_ID    
            //                                                                                ) X

            //                                                                            GROUP BY 
            //                                                                                UOM,
            //                                                                                WORKORDER_ID,
            //                                                                                STANDARD_ID,
            //                                                                                ARINVT_ID_FG,
            //                                                                                ITEM_NO,
            //                                                                                ARINVT_ID_RM,
            //                                                                                UNIT,
            //                                                                                ITEMNO,
            //                                                                                DESCRIP,
            //                                                                                DESCRIP2,
            //                                                                                STAGING_LOCATIONS_ID

            //                                                                                    ORDER BY
            //                                                                                    ARINVT_ID_RM, WORKORDER_ID	
            //                                                                            ) A
            //                                                                            ORDER BY 
            //                                                                                ARINVT_ID_RM,
            //                                                                                WORKORDER_ID   ", workOrderIDs));

            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT 
                                                                                        ROWNUM AS SEQ_ID,
                                                                                        -1 AS LOC_ID,
                                                                                        '' AS LOC_DESC,
                                                                                        0 AS IS_PICKED,
                                                                                        0 AS IS_KITTED,
                                                                                        0 AS IS_ERROR,
                                                                                        0 AS IS_SKIPPED,
                                                                                        '' AS ERROR_DESCRIPTION,
                                                                                        0 AS  MASTER_LABEL_ID,
                                                                                        -1 AS FGMULTI_ID,
                                                                                        0 AS  NEW_MASTER_LABEL_ID,
                                                                                        0 AS WORKORDER_BOM_ID,
                                                                                        0 AS SNDOP_ID,
                                                                                        A.UOM, 
                                                                                        A.WORKORDER_ID,
                                                                                        A.STANDARD_ID,
                                                                                        A.ARINVT_ID_FG,
                                                                                        A.ITEM_NO,
                                                                                        CASE NVL(MANUAL,'N')
                                                                                        	WHEN 'Y' THEN A.REL_QUANTITY
                                                                                            WHEN 'N' THEN (A.REL_QUANTITY  * WO_QTY)
                                                                                        END AS REL_QUANTITY,
                                                                                        ROUND(((A.REQ_QUANTITY) * (A.REL_QUANTITY / A.PTSPER)),2) AS REQ_QUANTITY,
                                                                                        ROUND(((A.HARD_ALLOCATION) * (A.REL_QUANTITY / A.PTSPER)),2) AS HARD_ALLOCATION,
                                                                                        A.ARINVT_ID_RM,
                                                                                        A.PTSPER,
                                                                                        A.UNIT,
                                                                                        A.ITEMNO,
                                                                                        A.DESCRIP,
                                                                                        A.DESCRIP2,
                                                                                        A.STAGING_LOCATIONS_ID,
                                                                                        TO_CHAR((A.REL_QUANTITY / A.PTSPER),'9999.99') AS OPMAT_FACTOR
                                                                                        FROM
                                                                                        (
                                                                                        SELECT 
                                                                                        UOM,
                                                                                        WORKORDER_ID,
                                                                                        STANDARD_ID,
                                                                                        ARINVT_ID_FG,
                                                                                        ITEM_NO,
                                                                                        SUM(REL_QUANTITY) AS REL_QUANTITY,
                                                                                        SUM(REQ_QUANTITY) AS REQ_QUANTITY,
                                                                                        SUM(HARD_ALLOCATION) AS HARD_ALLOCATION,
                                                                                        ARINVT_ID_RM,
                                                                                        SUM(PTSPER) AS PTSPER,
                                                                                        UNIT,
                                                                                        ITEMNO,
                                                                                        DESCRIP,
                                                                                        DESCRIP2,
                                                                                        STAGING_LOCATIONS_ID,
                                                                                        WO_QTY,
                                                                                        MANUAL
                                                                                        FROM 
                                                                                        (
                                                                                            SELECT
                                                                                                AR.UNIT AS UOM,    
                                                                                                WOB.ID AS WORKORDER_BOM_ID,
                                                                                                WOB.WORKORDER_ID,
                                                                                                WOB.PARENT_STANDARD_ID AS STANDARD_ID,
                                                                                                WOB.PARENT_ARINVT_ID AS ARINVT_ID_FG,
                                                                                                A.itemno as ITEM_NO,
                                                                                                CASE
                                                                                                    WHEN NVL(OP.PTSPER_DISP,0) > 0 THEN OP.PTSPER_DISP
                                                                                                    WHEN NVL(OP.PTSPER_DISP,0) <= 0 THEN WOB.QUAN
                                                                                                END  REL_QUANTITY,
                                                                                                CASE
                                                                                                    WHEN WOB.QUAN > 0 THEN WOB.QUAN - (NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f, arinvt a WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+) AND f.arinvt_id=a.id and a.class NOT IN ('PS','PK')),0))
                                                                                                    WHEN WOB.QUAN <= 0 THEN 0
                                                                                                END  REQ_QUANTITY,
                                                                                                NVL((SELECT SUM(f.onhand) FROM workorder_bom_mat_loc w, v_fgmulti_locations f, arinvt a WHERE w.workorder_bom_id = WOB.ID AND w.fgmulti_id = f.id(+) AND f.arinvt_id=a.id and a.class NOT IN('PS','PK')),0) AS HARD_ALLOCATION,
                                                                                                WOB.SNDOP_ID,
                                                                                                WOB.ARINVT_ID AS ARINVT_ID_RM,
                                                                                                CASE NVL(WOB.MANUAL,'N')
                                                                                                    WHEN 'Y' THEN WOB.QUAN
                                                                                                    WHEN 'N' THEN OP.PTSPER
                                                                                                END AS PTSPER, 
                                                                                               CASE NVL(WOB.MANUAL,'N')
                                                                                                    WHEN 'Y' THEN AR.UNIT
                                                                                                    WHEN 'N' THEN OP.UNIT
                                                                                                END AS UNIT,
                                                                                                ar.ITEMNO,
                                                                                                ar.DESCRIP,
                                                                                                NVL(ar.DESCRIP2, 'N/A') AS DESCRIP2,
                                                                                                (SELECT     
                                                                                                    mc.staging_locations_id
                                                                                                    FROM workorder wo
                                                                                                    JOIN standard s ON wo.standard_id = s.ID
                                                                                                    JOIN mfgcell mc ON s.mfgcell_id = mc.id
                                                                                                    WHERE 
                                                                                                    wo.id = WOB.WORKORDER_ID) AS STAGING_LOCATIONS_ID,
                                                                                                    NVL(WO.CYCLES_REQ,1) AS WO_QTY,
                                                                                                    NVL(WOB.MANUAL,'N') AS MANUAL
                                                                                                FROM
            	                                                                                    WORKORDER_BOM WOB INNER JOIN ARINVT A ON WOB.PARENT_ARINVT_ID = A.ID
                                                                                                    INNER JOIN WORKORDER WO ON WO.ID= WOB.WORKORDER_ID 
            	                                                                                    INNER JOIN ARINVT AR ON WOB.ARINVT_ID = AR.ID
            	                                                                                    LEFT JOIN OPMAT OP ON WOB.SNDOP_ID = OP.SNDOP_ID AND WOB.OPMAT_ID=OP.ID AND OP.ARINVT_ID = AR.ID

                                                                                                WHERE
            	                                                                                    WOB.PARENT_ID > 0 AND
                                                                                                    AR.CLASS NOT IN('PS','PK') AND
            	                                                                                    WOB.WORKORDER_ID IN({0})

                                                                                                ORDER BY
            	                                                                                    WOB.ARINVT_ID, WOB.WORKORDER_ID    
                                                                                            ) X

                                                                                        GROUP BY 
                                                                                            UOM,
                                                                                            WORKORDER_ID,
                                                                                            STANDARD_ID,
                                                                                            ARINVT_ID_FG,
                                                                                            ITEM_NO,
                                                                                            ARINVT_ID_RM,
                                                                                            UNIT,
                                                                                            ITEMNO,
                                                                                            DESCRIP,
                                                                                            DESCRIP2,
                                                                                            STAGING_LOCATIONS_ID,
                                                                                            WO_QTY,
                                                                                            MANUAL

                                                                                                ORDER BY
                                                                                                ARINVT_ID_RM, WORKORDER_ID	
                                                                                        ) A
                                                                                        ORDER BY 
                                                                                            ARINVT_ID_RM,
                                                                                            WORKORDER_ID    ", workOrderIDs));

            IEnumerable<WorkOrderRawMaterial> lists = mapperRM.MapList(reader);
            List<WorkOrderRawMaterial> workOrderRawMaterialList = lists as List<WorkOrderRawMaterial>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return workOrderRawMaterialList;
        }

        public MFGCell GetWorkOrderMFGCellData(long workOrderID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT     
                                                                                     mc.id,
                                                                                     mc.mfgcell,
                                                                                     mc.mfgtype,
                                                                                     mc.staging_locations_id
                                                                                FROM workorder wo
                                                                                JOIN standard s ON wo.standard_id = s.ID
                                                                                JOIN mfgcell mc ON s.mfgcell_id = mc.id
                                                                                WHERE 
                                                                                 wo.id = {0}", workOrderID));
            MFGCell mfgCell = mapperMFG.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return mfgCell;
        }

        public bool HardAllocationPackage(long workOrderID, long fgMultiID)
        {
            try
            {
                System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(@"begin hard_alloc.check_associate_fgmulti_to_wo(:workOrderId, :existingFGMultiId); end;");
                System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                System.Data.Common.DbParameter parameter1 = dbCommand.CreateParameter();
                System.Data.Common.DbParameter parameter2 = dbCommand.CreateParameter();

                //parameter.ParameterName = "ret";
                //parameter.Direction = ParameterDirection.Output;
                //parameter.DbType = DbType.String;
                //parameter.Size = 6000;
                //dbCommand.Parameters.Add(parameter);

                parameter1.ParameterName = "workorderid";
                parameter1.Direction = ParameterDirection.Input;
                parameter1.DbType = DbType.Int64;
                parameter1.Value = workOrderID;
                dbCommand.Parameters.Add(parameter1);

                parameter2.ParameterName = "existingFGMultiId";
                parameter2.Direction = ParameterDirection.Input;
                parameter2.DbType = DbType.Int64;
                parameter2.Value = fgMultiID;
                dbCommand.Parameters.Add(parameter2);

                var reader = database.ExecuteNonQuery(dbCommand);
                //string retvalue = dbCommand.Parameters["ret"].Value.ToString();
                //database.ExecuteNonQuery(CommandType.Text, string.Format("begin hard_alloc.check_associate_fgmulti_to_wo({0},{1}); end;", serialNumber, workOrderID));
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }


        public List<WorkOrderAssignBOM> GetWorkOrderDataAssignBOM(long arinvtID, long arinvtGroupID, DateTime? mustStartDate, int mustStartDaysAdd)
        {

            string arinvtWhereClause = string.Empty;
            string InventoryGroupWhereClause = string.Empty;
            string mustStartDateWhereClause = string.Empty;            

            if (arinvtID > 0)
            {
                arinvtWhereClause = " AND AR.ID=" + arinvtID.ToString() + "";
            }

            if (arinvtGroupID > 0)
            {
                InventoryGroupWhereClause = " AND AR.ARINVT_GROUP_ID=" + arinvtGroupID.ToString() + "";
            }

            if (mustStartDate != null && mustStartDate.Value != null)
            {
                DateTime mustStartDateUpdated = Convert.ToDateTime(mustStartDate.Value.ToString("dd-MM-yyyy")).AddDays(mustStartDaysAdd);
                mustStartDateWhereClause = " AND trunc(WO.START_TIME) <= to_date('" + mustStartDateUpdated.ToString("yyyy/MM/dd") + "','YYYY/MM/DD') ";
            }

            var reader = database.ExecuteReader(CommandType.Text, @"SELECT 
                                                                        WO.ID AS WorkOrderID
                                                                    FROM  
                                                                        WORKORDER WO LEFT OUTER JOIN WORKORDER_BOM  WOB ON WO.ID=WOB.WORKORDER_ID
                                                                        INNER JOIN PARTNO PN ON WO.STANDARD_ID=PN.STANDARD_ID 
                                                                        INNER JOIN ARINVT AR ON PN.ARINVT_ID=AR.ID
                                                                    WHERE 1=1 "
                                                                        + arinvtWhereClause
                                                                        + InventoryGroupWhereClause
                                                                        + mustStartDateWhereClause
                                                                        + " AND WO.ASSY_RUN='Y' AND WO.FIRM='Y' AND WO.ORIGIN IN('MANUAL','PLANNED') AND WOB.ID IS NULL");
            IEnumerable<WorkOrderAssignBOM> lists = mapperWorkOrderAssignBOM.MapList(reader);
            List<WorkOrderAssignBOM> workOrderList = lists as List<WorkOrderAssignBOM>;
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return workOrderList;
        }
        public void GetWorkOrderBOM(long workOrderID, string userName, string processName, bool isLogging)
        {
            database.ExecuteScalar(CommandType.Text, string.Format("begin wo_bom.populate_workorder_bom({0}); end;", workOrderID));

            if (isLogging)
            {
                long id = 0;
                try
                {
                    System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(string.Format(@"
                            INSERT INTO ARISOFT_LOG_WORKORDER
                             (ID, WORKORDER_ID, APPLICATION, PROCESS_NAME, USERNAME, CREATED_DATE)
                            VALUES
                            (S_ARISOFT_LOG_WORKORDER.NEXTVAL, {0}, '{1}', '{2}', '{3}',TO_DATE('{4}', 'YYYY-MM-DD HH:mi:ss')) returning id into :l_id", workOrderID, "KITTING", processName, userName, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")));

                    System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                    parameter.ParameterName = "l_id";
                    parameter.Direction = ParameterDirection.Output;
                    parameter.DbType = DbType.Decimal;
                    dbCommand.Parameters.Add(parameter);

                    var reader = database.ExecuteNonQuery(dbCommand);
                    id = Convert.ToInt64(dbCommand.Parameters["l_id"].Value.ToString());
                }
                catch (Exception exp)
                {

                }
            }
        }

        public bool GetWorkOrderBOMData(long workOrderID)
        {
            DataSet dataSet = this.database.ExecuteDataSet(CommandType.Text, string.Format(@"SELECT ID FROM WORKORDER_BOM WHERE WORKORDER_ID={0}", workOrderID));
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                dataSet.Dispose();
                return true;
            }
            else
            {
                dataSet.Dispose();
                return false;
            }
        }

        public WorkOrderSingle GetWorkOrderData(long workOrderID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID, ASSY_RUN FROM WORKORDER WHERE ID={0}", workOrderID));
            WorkOrderSingle workOrderSingle = mapperWOSingle.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return workOrderSingle;
        }

        public WorkOrderBOMSingle GetWorkOrderItemData(long workOrderID, long arinvtID)
        {
            var reader = database.ExecuteReader(CommandType.Text, string.Format(@"SELECT ID, ARINVT_ID, QUAN FROM WORKORDER_BOM WHERE WORKORDER_ID={0} AND ARINVT_ID={1}", workOrderID, arinvtID));
            WorkOrderBOMSingle workOrderBOMSingle = mapperWOBOMSingle.MapSingle(reader);
            if (reader.IsClosed == false)
            {
                reader.Close();
                reader.Dispose();
            }
            return workOrderBOMSingle;
        }


        public bool HardAllocationPackage(long workOrderID, string serialNumber, string userID)
        {
            try
            {
                System.Data.Common.DbCommand dbCommand = database.GetSqlStringCommand(@"begin :ret := ARS_UTILS.do_hardalloc(:serialno, :workorderid, :userid); end;");
                System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                System.Data.Common.DbParameter parameter1 = dbCommand.CreateParameter();
                System.Data.Common.DbParameter parameter2 = dbCommand.CreateParameter();
                System.Data.Common.DbParameter parameter3 = dbCommand.CreateParameter();

                parameter.ParameterName = "ret";
                parameter.Direction = ParameterDirection.Output;
                parameter.DbType = DbType.String;
                parameter.Size = 6000;
                dbCommand.Parameters.Add(parameter);

                parameter1.ParameterName = "serialno";
                parameter1.Direction = ParameterDirection.Input;
                parameter1.DbType = DbType.String;
                parameter1.Value = serialNumber;
                dbCommand.Parameters.Add(parameter1);

                parameter2.ParameterName = "workorderid";
                parameter2.Direction = ParameterDirection.Input;
                parameter2.DbType = DbType.Int64;
                parameter2.Value = workOrderID;
                dbCommand.Parameters.Add(parameter2);

                parameter3.ParameterName = "userid";
                parameter3.Direction = ParameterDirection.Input;
                parameter3.DbType = DbType.String;
                parameter3.Value = userID;
                dbCommand.Parameters.Add(parameter3);

                var reader = database.ExecuteNonQuery(dbCommand);
                string retvalue = dbCommand.Parameters["ret"].Value.ToString();                
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }

        private static WorkOrderRawMaterial MapWorkOrderRM(IDataReader reader)
        {
            var workOrderRM = new WorkOrderRawMaterial
            {
                SEQ_ID = reader["SEQ_ID"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SEQ_ID"]),                
                UOM = reader["UOM"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UOM"]),                
                LOC_ID = reader["LOC_ID"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LOC_ID"]),                
                LOC_DESC = reader["LOC_DESC"] == DBNull.Value ? string.Empty : Convert.ToString(reader["LOC_DESC"]),
                WORKORDER_BOM_ID = reader["WORKORDER_BOM_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["WORKORDER_BOM_ID"]),
                WORKORDER_ID = reader["WORKORDER_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["WORKORDER_ID"]),                
                STANDARD_ID = reader["STANDARD_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["STANDARD_ID"]),                
                ARINVT_ID_FG = reader["ARINVT_ID_FG"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ARINVT_ID_FG"]),                
                ITEM_NO = reader["ITEM_NO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEM_NO"]),                
                REL_QUANTITY= reader["REL_QUANTITY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["REL_QUANTITY"]),                
                SNDOP_ID= reader["SNDOP_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["SNDOP_ID"]),                
                ARINVT_ID_RM= reader["ARINVT_ID_RM"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ARINVT_ID_RM"]),                
                PTSPER= reader["PTSPER"] == DBNull.Value ? 0 : Convert.ToDouble(reader["PTSPER"]),                
                UNIT = reader["UNIT"] == DBNull.Value ? string.Empty : Convert.ToString(reader["UNIT"]),                               
                ITEMNO = reader["ITEMNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEMNO"]),                           
                DESCRIPTION = reader["DESCRIP"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP"]),                             
                DESCRIPTION2 = reader["DESCRIP2"] == DBNull.Value ? string.Empty : Convert.ToString(reader["DESCRIP2"]),                
                REQ_QUANTITY = reader["REQ_QUANTITY"] == DBNull.Value ? 0 : Convert.ToDouble(reader["REQ_QUANTITY"]),
                HARD_ALLOCATION = reader["HARD_ALLOCATION"] == DBNull.Value ? 0 : Convert.ToDouble(reader["HARD_ALLOCATION"]),
                IS_PICKED = reader["IS_ERROR"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IS_ERROR"]),                
                IS_ERROR = reader["IS_PICKED"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IS_PICKED"]),
                IS_SKIPPED = reader["IS_SKIPPED"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IS_SKIPPED"]),
                IS_KITTED = reader["IS_KITTED"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IS_KITTED"]),
                ERROR_DESCRIPTION = reader["ERROR_DESCRIPTION"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ERROR_DESCRIPTION"]),                
                MASTER_LABEL_ID = reader["MASTER_LABEL_ID"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MASTER_LABEL_ID"]),                
                FGMULTI_ID = reader["FGMULTI_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["FGMULTI_ID"]),
                STAGING_LOCATIONS_ID = reader["STAGING_LOCATIONS_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["STAGING_LOCATIONS_ID"]),
                NEW_MASTER_LABEL_ID = reader["NEW_MASTER_LABEL_ID"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["NEW_MASTER_LABEL_ID"]),
                OPMAT_FACTOR = reader["OPMAT_FACTOR"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OPMAT_FACTOR"]),
            };
            return workOrderRM;
        }

        private static MFGCell MapWorkOrderMFG(IDataReader reader)
        {
            var mfgCell = new MFGCell
            {   
                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),               
                MFG_CELL = reader["mfgcell"] == DBNull.Value ? string.Empty : Convert.ToString(reader["mfgcell"]),
                MFG_TYPE = reader["mfgtype"] == DBNull.Value ? string.Empty : Convert.ToString(reader["mfgtype"]),                
                STAGING_LOCATION_ID = reader["staging_locations_id"] == DBNull.Value ? 0 : Convert.ToInt64(reader["staging_locations_id"]),                
            };
            return mfgCell;
        }


        private static WorkOrder MapWorkOrder(IDataReader reader)
        {
            var workOrder = new WorkOrder
            {
                WorkOrderID = reader["WorkOrderID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["WorkOrderID"]),                
                ItemNo = reader["ITEMNO"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ITEMNO"]),
                StartDate = reader["START_TIME"] == DBNull.Value ? string.Empty : Convert.ToDateTime(reader["START_TIME"]).ToString("dd-MM-yyyy"),
                REQUIRED_QUANTITY = reader["REQ_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["REQ_QTY"]),
                PICKED_QUANTITY = reader["HARD_ALLOC"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["HARD_ALLOC"]),
                RECEIVED_QUANTITY = reader["KIT_ACK_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["KIT_ACK_QTY"]),
                COLOR_CODE = reader["COLOR_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["COLOR_CODE"]),
            };
            return workOrder;
        }

        private static WorkOrderSingle MapWorkOrderSingle(IDataReader reader)
        {
            var workOrder = new WorkOrderSingle
            {
                WorkOrderID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                ASSY_RUN = reader["ASSY_RUN"] == DBNull.Value ? "N" : Convert.ToString(reader["ASSY_RUN"])
            };
            return workOrder;
        }

        private static WorkOrderBOMSingle MapWorkOrderBOMSingle(IDataReader reader)
        {
            var workOrderBOM = new WorkOrderBOMSingle
            {
                WorkOrderID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID"]),
                ARINVT_ID = reader["ARINVT_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ARINVT_ID"]),
                QUAN = reader["QUAN"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QUAN"]),
            };
            return workOrderBOM;
        }




        private static WorkOrderAssignBOM MapWorkOrderAssignBOM(IDataReader reader)
        {
            var workOrder = new WorkOrderAssignBOM
            {
                WorkOrderID = reader["WorkOrderID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["WorkOrderID"]),
            };
            return workOrder;
        }

        

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
