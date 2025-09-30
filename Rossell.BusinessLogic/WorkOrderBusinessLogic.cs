using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rossell.BusinessEntity;
using Rossell.DataLogic;
using Rossell.Common;
using Newtonsoft;
using Newtonsoft.Json;
using System.Configuration;

namespace Rossell.BusinessLogic
{
    public class WorkOrderBusinessLogic : IDisposable
    {
        public List<WorkOrder> GetWorkOrderData(long arinvtID, long arinvtGroupID, DateTime? mustStartDate, DateTime? expiryDate, int mustStartDateDaysAdd, string processName, string userName, bool isLogging)
        {
            using (WorkOrderDataLogic workOrderData = new WorkOrderDataLogic())
            {
                List<WorkOrderAssignBOM> workOrderOrderAssignBOMList = workOrderData.GetWorkOrderDataAssignBOM(arinvtID, arinvtGroupID, mustStartDate, mustStartDateDaysAdd);
                foreach (WorkOrderAssignBOM workOrderAssignBOM in workOrderOrderAssignBOMList)
                {
                    GetWorkOrderBOM(workOrderAssignBOM.WorkOrderID, processName, userName, isLogging);
                }
                return workOrderData.GetWorkOrderData(arinvtID, arinvtGroupID, mustStartDate, expiryDate, mustStartDateDaysAdd);
            }
        }

        public List<WorkOrderRawMaterial> GetWorkOrderRawMaterial(string workOrderIDs, out string FileName, DateTime? expiryDate, out List<WorkOrderRawMaterial> workOrderRawMaterialListNOFGMULTI, out List<WorkOrderRawMaterial> workOrderRawMaterialListNOENOUGHQUANTITY, string processName, string userName, bool isLogging)
        {
            string jsonFilePath = ConfigurationManager.AppSettings["JSONFILEPATH"].ToString();
            string filename = DateTime.Now.ToString("yyyyMMddhhmmss") + ".json";
            FileName = filename;

            List<WorkOrderRawMaterial> workOrderRawMaterialList;
            workOrderRawMaterialListNOFGMULTI =new List<WorkOrderRawMaterial>();
            workOrderRawMaterialListNOENOUGHQUANTITY = new List<WorkOrderRawMaterial>();
            List<ItemLocation> itemLocationList=null;
            string[] aWorkOrders = workOrderIDs.Split(',');
            foreach (string strworkOrder in aWorkOrders)
            {
                GetWorkOrderBOM(Convert.ToInt64(strworkOrder), processName ,userName, isLogging);
            }
            using (WorkOrderDataLogic workOrderData = new WorkOrderDataLogic())
            {
                workOrderRawMaterialList = workOrderData.GetWorkOrderRawMaterial(workOrderIDs);
            }
            string strArinvtIDs = string.Empty;

            foreach (WorkOrderRawMaterial workOrderRM in workOrderRawMaterialList)
            {
                if (workOrderRM.REQ_QUANTITY <= 0)
                {
                    workOrderRM.IS_PICKED = 1;
                }
                else
                {
                    if (!strArinvtIDs.Contains(workOrderRM.ARINVT_ID_RM.ToString()))
                    {
                        if (strArinvtIDs.Length <= 0)
                        {
                            strArinvtIDs = workOrderRM.ARINVT_ID_RM.ToString();
                        }
                        else
                        {
                            strArinvtIDs += ", " + workOrderRM.ARINVT_ID_RM.ToString();
                        }
                    }
                }
            }

            using (ItemLocationBusinessLogic itemLocationBL = new ItemLocationBusinessLogic())
            {
                if (!strArinvtIDs.Equals(""))
                {
                    itemLocationList = itemLocationBL.GetItemLocation(strArinvtIDs, expiryDate);
                }
            }

            if(itemLocationList == null) 
            {
                return null;
            }

            int totalRecords = workOrderRawMaterialList.Count;
            double factor = 1;
            List<WorkOrderRawMaterial> workOrderRMAdded = new List<WorkOrderRawMaterial>();
            WorkOrderRawMaterial addedworkOrderRawMaterial = null;

            if (workOrderRawMaterialList != null && workOrderRawMaterialList.Count > 0)
            {
                foreach (WorkOrderRawMaterial workOrderRM in workOrderRawMaterialList)
                {
                    List<ItemLocation> itemlocations = itemLocationList.Where(o => o.ARINVT_ID == workOrderRM.ARINVT_ID_RM).OrderBy(ord => ord.ARINVT_ID).ThenBy(ord => ord.LOC_DESCRIPTION).ToList();

                    // CASE 1 IF ARINVT HAS ONLY 1 LOCATION
                    if (itemlocations.Count == 1)
                    {
                        double totalReqQuantity = 0;
                        ItemLocation itemLocation = itemLocationList.FirstOrDefault(o => o.ARINVT_ID == workOrderRM.ARINVT_ID_RM);
                        totalReqQuantity = workOrderRawMaterialList.Where(itm => itm.ARINVT_ID_RM == workOrderRM.ARINVT_ID_RM).Sum(item => item.REQ_QUANTITY);
                        if ((itemLocation.ON_HAND * factor) >= Convert.ToDouble(totalReqQuantity))
                        {
                            workOrderRM.LOC_ID = itemLocation.LOC_ID;
                            workOrderRM.LOC_DESC = itemLocation.LOC_DESCRIPTION;
                            workOrderRM.FGMULTI_ID = itemLocation.FGMULTI_ID;
                            workOrderRM.ENOUGH_ONHAND = true;
                        }
                        else
                        {
                            workOrderRM.LOC_ID = itemLocation.LOC_ID;
                            workOrderRM.LOC_DESC = itemLocation.LOC_DESCRIPTION;
                            workOrderRM.FGMULTI_ID = itemLocation.FGMULTI_ID;
                            workOrderRM.ENOUGH_ONHAND = false;
                        }

                       
                    }
                    else
                    {
                        double totalReqQuantity = 0;
                        bool isfound = false;
                        factor = Convert.ToDouble(workOrderRM.OPMAT_FACTOR);

                        totalReqQuantity = workOrderRawMaterialList.Where(itm => itm.ARINVT_ID_RM == workOrderRM.ARINVT_ID_RM).Sum(item => item.REQ_QUANTITY);
                        // CASE 2 IF ARINVT HAVE MULTIPLE FGMULTI ID BUT ONE LOCATION HAVE ENOUGH QUANTITY
                        foreach (ItemLocation itemloc in itemlocations)
                        {
                            if ((itemloc.ON_HAND * factor) >= Convert.ToDouble(totalReqQuantity))
                            {
                                if (isfound == false)
                                {
                                    workOrderRM.LOC_ID = itemloc.LOC_ID;
                                    workOrderRM.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                    workOrderRM.FGMULTI_ID = itemloc.FGMULTI_ID;
                                    workOrderRM.ENOUGH_ONHAND = true;
                                    isfound = true;
                                    //itemloc.ON_HAND_UPDATED = (itemloc.ON_HAND_UPDATED - totalReqQuantity);
                                    break;
                                }
                            }
                        }
                        totalReqQuantity = 0;
                    }
                    factor = 1;
                }

                List<WorkOrderRawMaterial> nonSingularLOC = workOrderRawMaterialList.Where(wo => wo.LOC_ID == -1).ToList();
                // CASE 3 IF ARINVT HAVE MULTIPLE FGMULTI ID BUT NO LOCATION HAVE ENOUGH QUANTITY
                foreach (WorkOrderRawMaterial workOrderRM in nonSingularLOC)
                {
                    List<ItemLocation> itemlocations = itemLocationList.Where(o => o.ARINVT_ID == workOrderRM.ARINVT_ID_RM).OrderBy(ord => ord.ARINVT_ID).ThenBy(ord => ord.LOC_DESCRIPTION).ToList();
                    double totalReqQuantity = workOrderRM.REQ_QUANTITY;
                    factor = Convert.ToDouble(workOrderRM.OPMAT_FACTOR);

                    foreach (ItemLocation itemloc in itemlocations)
                    {
                        double totalQuantity = (itemloc.ON_HAND_UPDATED * factor);
                        if (totalReqQuantity > 0 && totalQuantity > 0)
                        {
                            if (totalReqQuantity >= totalQuantity)
                            {
                                addedworkOrderRawMaterial = new WorkOrderRawMaterial();
                                addedworkOrderRawMaterial.SEQ_ID = ++totalRecords;
                                addedworkOrderRawMaterial.UOM = workOrderRM.UOM;
                                addedworkOrderRawMaterial.LOC_ID = itemloc.LOC_ID;
                                addedworkOrderRawMaterial.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                addedworkOrderRawMaterial.WORKORDER_ID = workOrderRM.WORKORDER_ID;
                                addedworkOrderRawMaterial.STANDARD_ID = workOrderRM.STANDARD_ID;
                                addedworkOrderRawMaterial.ARINVT_ID_FG = workOrderRM.ARINVT_ID_FG;
                                addedworkOrderRawMaterial.ITEM_NO = workOrderRM.ITEM_NO;
                                addedworkOrderRawMaterial.REL_QUANTITY = workOrderRM.REL_QUANTITY;
                                addedworkOrderRawMaterial.SNDOP_ID = workOrderRM.SNDOP_ID;
                                addedworkOrderRawMaterial.ARINVT_ID_RM = workOrderRM.ARINVT_ID_RM;
                                addedworkOrderRawMaterial.PTSPER = workOrderRM.PTSPER;
                                addedworkOrderRawMaterial.UNIT = workOrderRM.UNIT;
                                addedworkOrderRawMaterial.ITEMNO = workOrderRM.ITEMNO;
                                addedworkOrderRawMaterial.DESCRIPTION = workOrderRM.DESCRIPTION;
                                addedworkOrderRawMaterial.DESCRIPTION2 = workOrderRM.DESCRIPTION2;
                                addedworkOrderRawMaterial.REQ_QUANTITY = totalQuantity;
                                addedworkOrderRawMaterial.IS_PICKED = workOrderRM.IS_PICKED;
                                addedworkOrderRawMaterial.IS_ERROR = workOrderRM.IS_ERROR;
                                addedworkOrderRawMaterial.ERROR_DESCRIPTION = workOrderRM.ERROR_DESCRIPTION;
                                addedworkOrderRawMaterial.MASTER_LABEL_ID = workOrderRM.MASTER_LABEL_ID;
                                addedworkOrderRawMaterial.FGMULTI_ID = itemloc.FGMULTI_ID;
                                addedworkOrderRawMaterial.ENOUGH_ONHAND = true;
                                workOrderRMAdded.Add(addedworkOrderRawMaterial);
                                workOrderRM.LOC_ID = -1;
                                workOrderRM.FGMULTI_ID = -1;
                                totalReqQuantity = (totalReqQuantity - totalQuantity);
                                itemloc.ON_HAND_UPDATED = (itemloc.ON_HAND_UPDATED - totalQuantity);
                            }
                            else
                            {
                                addedworkOrderRawMaterial = new WorkOrderRawMaterial();
                                addedworkOrderRawMaterial.SEQ_ID = totalRecords++;
                                addedworkOrderRawMaterial.UOM = workOrderRM.UOM;
                                addedworkOrderRawMaterial.LOC_ID = itemloc.LOC_ID;
                                addedworkOrderRawMaterial.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                addedworkOrderRawMaterial.WORKORDER_ID = workOrderRM.WORKORDER_ID;
                                addedworkOrderRawMaterial.STANDARD_ID = workOrderRM.STANDARD_ID;
                                addedworkOrderRawMaterial.ARINVT_ID_FG = workOrderRM.ARINVT_ID_FG;
                                addedworkOrderRawMaterial.ITEM_NO = workOrderRM.ITEM_NO;
                                addedworkOrderRawMaterial.REL_QUANTITY = workOrderRM.REL_QUANTITY;
                                addedworkOrderRawMaterial.SNDOP_ID = workOrderRM.SNDOP_ID;
                                addedworkOrderRawMaterial.ARINVT_ID_RM = workOrderRM.ARINVT_ID_RM;
                                addedworkOrderRawMaterial.PTSPER = workOrderRM.PTSPER;
                                addedworkOrderRawMaterial.UNIT = workOrderRM.UNIT;
                                addedworkOrderRawMaterial.ITEMNO = workOrderRM.ITEMNO;
                                addedworkOrderRawMaterial.DESCRIPTION = workOrderRM.DESCRIPTION;
                                addedworkOrderRawMaterial.DESCRIPTION2 = workOrderRM.DESCRIPTION2;
                                addedworkOrderRawMaterial.REQ_QUANTITY = totalReqQuantity;
                                addedworkOrderRawMaterial.IS_PICKED = workOrderRM.IS_PICKED;
                                addedworkOrderRawMaterial.IS_ERROR = workOrderRM.IS_ERROR;
                                addedworkOrderRawMaterial.ERROR_DESCRIPTION = workOrderRM.ERROR_DESCRIPTION;
                                addedworkOrderRawMaterial.MASTER_LABEL_ID = workOrderRM.MASTER_LABEL_ID;
                                addedworkOrderRawMaterial.FGMULTI_ID = itemloc.FGMULTI_ID;
                                addedworkOrderRawMaterial.ENOUGH_ONHAND = false;
                                workOrderRMAdded.Add(addedworkOrderRawMaterial);
                                itemloc.ON_HAND_UPDATED = (itemloc.ON_HAND_UPDATED - totalReqQuantity);
                                totalReqQuantity = 0;
                            }
                        }
                    }
                }


                /*
                 if (isfound == false)
                        {
                            foreach (ItemLocation itemloc in itemlocations)
                            {
                                double totalQuantity = itemloc.ON_HAND;
                                if (totalReqQuantity > 0)
                                {
                                    if (totalReqQuantity >= totalQuantity)
                                    {
                                        addedworkOrderRawMaterial = new WorkOrderRawMaterial();
                                        addedworkOrderRawMaterial.SEQ_ID = totalRecords++;
                                        addedworkOrderRawMaterial.UOM = workOrderRM.UOM;
                                        addedworkOrderRawMaterial.LOC_ID = itemloc.LOC_ID;
                                        addedworkOrderRawMaterial.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                        addedworkOrderRawMaterial.WORKORDER_ID = workOrderRM.WORKORDER_ID;
                                        addedworkOrderRawMaterial.STANDARD_ID = workOrderRM.STANDARD_ID;
                                        addedworkOrderRawMaterial.ARINVT_ID_FG = workOrderRM.ARINVT_ID_FG;
                                        addedworkOrderRawMaterial.ITEM_NO = workOrderRM.ITEM_NO;
                                        addedworkOrderRawMaterial.REL_QUANTITY = totalQuantity;
                                        addedworkOrderRawMaterial.SNDOP_ID = workOrderRM.SNDOP_ID;
                                        addedworkOrderRawMaterial.ARINVT_ID_RM = workOrderRM.ARINVT_ID_RM;
                                        addedworkOrderRawMaterial.PTSPER = workOrderRM.PTSPER;
                                        addedworkOrderRawMaterial.UNIT = workOrderRM.UNIT;
                                        addedworkOrderRawMaterial.ITEMNO = workOrderRM.ITEMNO;
                                        addedworkOrderRawMaterial.DESCRIPTION = workOrderRM.DESCRIPTION;
                                        addedworkOrderRawMaterial.DESCRIPTION2 = workOrderRM.DESCRIPTION2;
                                        addedworkOrderRawMaterial.REQ_QUANTITY = totalQuantity;
                                        addedworkOrderRawMaterial.IS_PICKED = workOrderRM.IS_PICKED;
                                        addedworkOrderRawMaterial.IS_ERROR = workOrderRM.IS_ERROR;
                                        addedworkOrderRawMaterial.ERROR_DESCRIPTION = workOrderRM.ERROR_DESCRIPTION;
                                        addedworkOrderRawMaterial.MASTER_LABEL_ID = workOrderRM.MASTER_LABEL_ID;
                                        addedworkOrderRawMaterial.FGMULTI_ID = itemloc.FGMULTI_ID;
                                        workOrderRMAdded.Add(addedworkOrderRawMaterial);
                                        workOrderRM.LOC_ID = -1;
                                        workOrderRM.FGMULTI_ID = -1;
                                        totalReqQuantity = (totalReqQuantity - totalQuantity);
                                    }
                                    else
                                    {
                                        addedworkOrderRawMaterial = new WorkOrderRawMaterial();
                                        addedworkOrderRawMaterial.SEQ_ID = totalRecords++;
                                        addedworkOrderRawMaterial.UOM = workOrderRM.UOM;
                                        addedworkOrderRawMaterial.LOC_ID = itemloc.LOC_ID;
                                        addedworkOrderRawMaterial.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                        addedworkOrderRawMaterial.WORKORDER_ID = workOrderRM.WORKORDER_ID;
                                        addedworkOrderRawMaterial.STANDARD_ID = workOrderRM.STANDARD_ID;
                                        addedworkOrderRawMaterial.ARINVT_ID_FG = workOrderRM.ARINVT_ID_FG;
                                        addedworkOrderRawMaterial.ITEM_NO = workOrderRM.ITEM_NO;
                                        addedworkOrderRawMaterial.REL_QUANTITY = totalReqQuantity;
                                        addedworkOrderRawMaterial.SNDOP_ID = workOrderRM.SNDOP_ID;
                                        addedworkOrderRawMaterial.ARINVT_ID_RM = workOrderRM.ARINVT_ID_RM;
                                        addedworkOrderRawMaterial.PTSPER = workOrderRM.PTSPER;
                                        addedworkOrderRawMaterial.UNIT = workOrderRM.UNIT;
                                        addedworkOrderRawMaterial.ITEMNO = workOrderRM.ITEMNO;
                                        addedworkOrderRawMaterial.DESCRIPTION = workOrderRM.DESCRIPTION;
                                        addedworkOrderRawMaterial.DESCRIPTION2 = workOrderRM.DESCRIPTION2;
                                        addedworkOrderRawMaterial.REQ_QUANTITY = totalReqQuantity;
                                        addedworkOrderRawMaterial.IS_PICKED = workOrderRM.IS_PICKED;
                                        addedworkOrderRawMaterial.IS_ERROR = workOrderRM.IS_ERROR;
                                        addedworkOrderRawMaterial.ERROR_DESCRIPTION = workOrderRM.ERROR_DESCRIPTION;
                                        addedworkOrderRawMaterial.MASTER_LABEL_ID = workOrderRM.MASTER_LABEL_ID;
                                        addedworkOrderRawMaterial.FGMULTI_ID = itemloc.FGMULTI_ID;
                                        workOrderRMAdded.Add(addedworkOrderRawMaterial);
                                        totalReqQuantity = 0;
                                    }                                
                                }                            
                            }
                        }
                        else
                        {
                            totalReqQuantity = 0;
                        }
                 */

                workOrderRawMaterialList.AddRange(workOrderRMAdded);
                workOrderRawMaterialListNOFGMULTI = workOrderRawMaterialList.Where(r => r.FGMULTI_ID == -1 && r.LOC_ID == -1 && r.IS_PICKED == 0).ToList();
                workOrderRawMaterialListNOENOUGHQUANTITY = workOrderRawMaterialList.Where(r => r.ENOUGH_ONHAND == false && r.IS_PICKED == 0).ToList();
                workOrderRawMaterialList.RemoveAll(r => r.FGMULTI_ID == -1 && r.LOC_ID == -1);
                workOrderRawMaterialList = workOrderRawMaterialList.OrderBy(ord => ord.ARINVT_ID_RM).ThenBy(ord => ord.WORKORDER_ID).ToList();
                string workOrderRawMaterialJSON = JsonConvert.SerializeObject(workOrderRawMaterialList);
                try
                {
                    string jsonFormatted = Newtonsoft.Json.Linq.JValue.Parse(workOrderRawMaterialJSON).ToString(Formatting.Indented);
                    System.IO.File.WriteAllText(jsonFilePath + filename, jsonFormatted);
                }
                catch (Exception exp)
                {

                }
            }
            return workOrderRawMaterialList;
        }

        public MFGCell GetWorkOrderMFGCellData(long workOrderID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.GetWorkOrderMFGCellData(workOrderID);
            }
        }

        public bool HardAllocationPackage(long workOrderID, long fgMultiID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.HardAllocationPackage(workOrderID, fgMultiID);
            }
        }

        public bool HardAllocationPackage(long workOrderID, string serialNumber, string userID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.HardAllocationPackage(workOrderID, serialNumber, userID);
            }
        }

        public bool GetWorkOrderBOMData(long workOrderID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.GetWorkOrderBOMData(workOrderID);
            }
        }

        public void GetWorkOrderBOM(long workOrderID, string processName, string userName, bool isLogging)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                bool isWorkOrderBOMExists = workOrderDataLogic.GetWorkOrderBOMData(workOrderID);
                if (isWorkOrderBOMExists == false)
                {
                    workOrderDataLogic.GetWorkOrderBOM(workOrderID, userName, processName, isLogging);
                }                
            }
        }

        public WorkOrderSingle GetWorkOrderData(long workOrderID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.GetWorkOrderData(workOrderID);
            }
        }

        public WorkOrderBOMSingle GetWorkOrderItemData(long workOrderID, long arinvtID)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.GetWorkOrderItemData(workOrderID, arinvtID);
            }
        }

        public List<WorkOrderAssignBOM> GetWorkOrderDataAssignBOM(long arinvtID, long arinvtGroupID, DateTime? mustStartDate, int mustStartDaysAdd)
        {
            using (WorkOrderDataLogic workOrderDataLogic = new WorkOrderDataLogic())
            {
                return workOrderDataLogic.GetWorkOrderDataAssignBOM(arinvtID, arinvtGroupID, mustStartDate, mustStartDaysAdd);
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
