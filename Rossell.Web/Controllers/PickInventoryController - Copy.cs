using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Rossell.BusinessLogic;
using Rossell.BusinessEntity;
using Rossell.Common;
using Newtonsoft;
using Newtonsoft.Json;
using System.Configuration;

namespace Rossell.Web.Controllers
{
    public class PickInventoryController : Controller
    {
        RossellTextLogger rossellLog = new RossellTextLogger(); 
        // GET: PickInventory
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Pick Inventory";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            if (Session["Users"] != null)
            {
                if (Session["WorkOrderRMLOC"] != null)
                {                   
                    string location = Session["STARTLOCATION"].ToString();                    
                    List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];
                    WorkOrderRawMaterial workOrderRM = null;

                    if (Session["ISSKIPPED"] != null && Session["ISSKIPPED"].ToString().Equals("true"))
                    {
                        workOrderRM = workOrderRMList.LastOrDefault(m => m.IS_SKIPPED == 1);
                        if (workOrderRM != null)
                        {
                            workOrderRM.IS_SKIPPED = 0;
                            workOrderRM.IS_PICKED = 0;
                        }
                        Session.Remove("ISSKIPPED");
                    }
                    else
                    {
                        workOrderRM = workOrderRMList.FirstOrDefault(m => m.IS_PICKED == 0);
                    }
                    
                    if (workOrderRM != null)
                    {
                        ViewBag.PickInventoryItem = workOrderRM;
                        using (ItemLocationBusinessLogic itemLocationBL = new ItemLocationBusinessLogic())
                        {
                            ViewBag.ItemLocation = itemLocationBL.GetItemLocation(workOrderRM.ARINVT_ID_RM);
                        }

                        ViewBag.SeqNo = 0;
                        ViewBag.Location = Session["STARTLOCATION"].ToString();                        
                    }
                }
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }            
        }

        public JsonResult Next(PickInventory pickInventory)
        {
            Object jsonData = null;
            string Logfilename = Session["JSONFILENAME"].ToString();
            Logfilename = Logfilename.Replace(".json", ".txt");
            rossellLog.FileName = Logfilename;
            int iCount = 0;
            string newSerialNumber = string.Empty;

            if (Session["COUNT"] == null)
            {
                Session["COUNT"] = 1;
                iCount = 1;
            }
            else
            {
                iCount = Convert.ToInt32(Session["COUNT"]) + 1;
            }
            
            if (pickInventory != null)
            {
                if (pickInventory.SerialNumber != null)
                {
                    if (!pickInventory.SerialNumber.Equals("") && pickInventory.PickQuantity > 0)
                    {
                        if (Session["WorkOrderRMLOC"] != null)
                        {
                            string location = Session["STARTLOCATION"].ToString();
                            List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];
                            WorkOrderRawMaterial workOrderRM = workOrderRMList.SingleOrDefault(o => o.WORKORDER_ID == pickInventory.WorkOrderID && o.ARINVT_ID_RM == pickInventory.ARINVT_ID && o.LOC_DESC== location);
                            rossellLog.MessageLog("--------------------------------------------------------------- Rossell Kitting Material Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                            rossellLog.MessageLog("RECORD_NO : "+ iCount.ToString() +"/" + workOrderRMList.Count.ToString());
                            rossellLog.MessageLog("WORKORDER_BOM_ID : " + workOrderRM.WORKORDER_BOM_ID.ToString());
                            rossellLog.MessageLog("WORKORDER_ID : " + workOrderRM.WORKORDER_ID.ToString());
                            rossellLog.MessageLog("STANDARD_ID : " + workOrderRM.STANDARD_ID.ToString());
                            rossellLog.MessageLog("ARINVT_ID_FG : " + workOrderRM.ARINVT_ID_FG.ToString());
                            rossellLog.MessageLog("ITEM_NO : " + workOrderRM.ITEM_NO.ToString());
                            rossellLog.MessageLog("SNDOP_ID : " + workOrderRM.SNDOP_ID.ToString());
                            rossellLog.MessageLog("ARINVT_ID_RM : " + workOrderRM.ARINVT_ID_RM.ToString());
                            rossellLog.MessageLog("PTSPER : " + workOrderRM.PTSPER.ToString());
                            rossellLog.MessageLog("ITEMNO : " + workOrderRM.ITEMNO.ToString());
                            rossellLog.MessageLog("DESCRIPTION : " + workOrderRM.DESCRIPTION.ToString());
                            rossellLog.MessageLog("DESCRIPTION2 : " + workOrderRM.DESCRIPTION2.ToString());
                            rossellLog.MessageLog("REQ_QUANTITY : " + workOrderRM.REQ_QUANTITY.ToString());
                            rossellLog.MessageLog("HARD_ALLOCATION : " + workOrderRM.HARD_ALLOCATION.ToString());
                            rossellLog.MessageLog("FGMULTI_ID : " + workOrderRM.FGMULTI_ID.ToString());
                            rossellLog.MessageLog("LOC_ID : " + workOrderRM.LOC_ID.ToString());
                            rossellLog.MessageLog("LOC_DESC : " + workOrderRM.LOC_DESC.ToString());
                            rossellLog.MessageLog("");
                            
                            try
                            {
                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    newSerialNumber = services.MasterLabelRepack(workOrderRM.MASTER_LABEL_ID, pickInventory.PickQuantity, pickInventory.SerialNumber);
                                    if (!newSerialNumber.Equals(""))
                                    {
                                        rossellLog.MessageLog("Successfully called Inventory/Disposition/MasterLabelRepack Web API ");
                                        rossellLog.MessageLog("");
                                        rossellLog.MessageLog("Serial Number After Repack Web API : " + newSerialNumber);
                                        rossellLog.MessageLog("");

                                        using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                        {
                                            bool isMasterLabelUpdate = labelBL.UpdateMasterLabel(newSerialNumber, pickInventory.PickQuantity);
                                            decimal adjustQty = Math.Ceiling(pickInventory.PickQuantity) - pickInventory.PickQuantity;
                                            bool isAdjustMasterLabelUpdate = labelBL.AdjustMasterLabelQty(Convert.ToInt64(workOrderRM.MASTER_LABEL_ID), adjustQty);
                                            if (isMasterLabelUpdate)
                                            {
                                                rossellLog.MessageLog("Successfully Updated the Master Label Quantity manually : " + newSerialNumber);
                                                rossellLog.MessageLog("");
                                            }

                                            if (isAdjustMasterLabelUpdate)
                                            {
                                                rossellLog.MessageLog("Successfully adjusted the Master Label Quantity manually : " + workOrderRM.MASTER_LABEL_ID.ToString());
                                                rossellLog.MessageLog("");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Inventory/Disposition/MasterLabelRepack Web API");
                                        rossellLog.MessageLog("");
                                        jsonData = new
                                        {
                                            status = 3,
                                            serial = string.Empty,
                                        };
                                        return Json(jsonData);
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                rossellLog.MessageLog("Could not successfully called Inventory/Disposition/MasterLabelRepack Web API \n" + exp.Message.ToString());
                                rossellLog.MessageLog("");
                            }

                            BusinessEntity.MasterLabel masterLabel = null;
                            BusinessEntity.Master_Label master_Label = null;
                            BusinessEntity.FGMULTI fgMulti = null;
                            bool isMoved = false;
                            bool isUpdated = false;
                            bool isReprint = false;
                            long targetFGMultiID = 0;

                            using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                            {
                                masterLabel = labelBL.GetMasterLabelData(newSerialNumber);
                                master_Label = labelBL.GetMasterLabelData(masterLabel.MASTER_LABEL_ID);                               
                                rossellLog.MessageLog("Master Label ID After Repack Web API : " + masterLabel.MASTER_LABEL_ID);
                                rossellLog.MessageLog("");
                            }

                            try
                            {

                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    targetFGMultiID = services.AddInventoryLocation(workOrderRM.ARINVT_ID_RM, workOrderRM.STAGING_LOCATIONS_ID, masterLabel.LOT_NO, workOrderRM.STANDARD_ID);
                                    rossellLog.MessageLog("Successfully called Manufacturing/Inventory/AddInventoryLocation Web API ");
                                    rossellLog.MessageLog("");
                                    rossellLog.MessageLog("FGMuliID After Add Inventory Location Web API : " + targetFGMultiID.ToString());
                                    rossellLog.MessageLog("");
                                }

                                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                {
                                    if (targetFGMultiID > 0)
                                    {
                                        fgMulti = labelBL.GetFGMULTIData(targetFGMultiID);
                                        rossellLog.MessageLog("Get the FGMULTI Record After Manufacturing/Inventory/AddInventoryLocation Web API : " + fgMulti.ID);
                                        rossellLog.MessageLog("");
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                rossellLog.MessageLog("Could not successfully called Manufacturing/Inventory/AddInventoryLocation Web API \n" + exp.Message.ToString());
                                rossellLog.MessageLog("");
                            }


                            try
                            {

                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    isMoved = services.MoveToLocation(workOrderRM.ARINVT_ID_RM, workOrderRM.FGMULTI_ID, targetFGMultiID, pickInventory.PickQuantity, masterLabel);
                                    if (isMoved)
                                    {
                                        rossellLog.MessageLog("Successfully called Inventory/TransactionLocation/MoveToLocation Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocation Web API ");
                                        rossellLog.MessageLog("");
                                    }

                                }
                            }
                            catch (Exception exp)
                            {
                                rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocation Web API \n" + exp.Message.ToString());
                                rossellLog.MessageLog("");
                            }

                            try
                            {

                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    isUpdated = services.UpdateMasterLabel(master_Label, fgMulti, workOrderRM.LOC_DESC.ToString());
                                    if (isUpdated)
                                    {
                                        rossellLog.MessageLog("Successfully called Labels/PrintLabel/UpdateMasterLabel Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Labels/PrintLabel/UpdateMasterLabel Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                }

                                bool isUpdatedMasterLabelManually = Convert.ToBoolean(ConfigurationManager.AppSettings["MASTERLABELUPDATE"]);
                                if (isUpdatedMasterLabelManually)
                                {
                                    using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                    {
                                        bool isMasterLabelUpdate = labelBL.UpdateMasterLabel(master_Label.ID, master_Label.FGMULTI_ID, workOrderRM.LOC_DESC.ToString(), master_Label.LOT_NO);
                                        if (isMasterLabelUpdate)
                                        {
                                            rossellLog.MessageLog("Successfully Updated the Master Label manually : " + master_Label.ID);
                                            rossellLog.MessageLog("");
                                        }
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                rossellLog.MessageLog("Could not successfully called Labels/PrintLabel/UpdateMasterLabel Web API \n" + exp.Message.ToString());
                                rossellLog.MessageLog("");
                            }

                            try
                            {

                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    isUpdated = services.DeleteInventoryLocation(workOrderRM.FGMULTI_ID);
                                    if (isUpdated)
                                    {
                                        rossellLog.MessageLog("Successfully called Manufacturing/Inventory/DeleteInventoryLocation Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Manufacturing/Inventory/DeleteInventoryLocation Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                }
                            }
                            catch (Exception exp)
                            {
                                rossellLog.MessageLog("Could not successfully called Manufacturing/Inventory/DeleteInventoryLocation Web API \n" + exp.Message.ToString());
                                rossellLog.MessageLog("");
                            }

                            #region "Old Code for Picking"
                            //try
                            //{

                            //    using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                            //    {
                            //        targetFGMultiID = services.AddInventoryLocation(workOrderRM.ARINVT_ID_RM, workOrderRM.STAGING_LOCATIONS_ID, masterLabel.LOT_NO, workOrderRM.STANDARD_ID);
                            //        rossellLog.MessageLog("Successfully called Manufacturing/Inventory/AddInventoryLocation Web API ");
                            //        rossellLog.MessageLog("");
                            //        rossellLog.MessageLog("FGMuliID After Add Inventory Location Web API : " + targetFGMultiID.ToString());
                            //        rossellLog.MessageLog("");
                            //    }

                            //    using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                            //    {
                            //        if (targetFGMultiID > 0)
                            //        {
                            //            fgMulti = labelBL.GetFGMULTIData(targetFGMultiID);
                            //            rossellLog.MessageLog("Get the FGMULTI Record After Manufacturing/Inventory/AddInventoryLocation Web API : " + fgMulti.ID);
                            //            rossellLog.MessageLog("");
                            //        }
                            //    }

                            //}
                            //catch (Exception exp)
                            //{
                            //    rossellLog.MessageLog("Could not successfully called Manufacturing/Inventory/AddInventoryLocation Web API \n" + exp.Message.ToString());
                            //    rossellLog.MessageLog("");
                            //}

                            //try
                            //{

                            //    using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                            //    {
                            //        isMoved = services.MoveToLocation(workOrderRM.ARINVT_ID_RM, workOrderRM.FGMULTI_ID, targetFGMultiID, pickInventory.PickQuantity, masterLabel);
                            //        if (isMoved)
                            //        {
                            //            rossellLog.MessageLog("Successfully called Inventory/TransactionLocation/MoveToLocation Web API ");
                            //            rossellLog.MessageLog("");
                            //        }
                            //        else
                            //        {
                            //            rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocation Web API ");
                            //            rossellLog.MessageLog("");
                            //        }

                            //    }
                            //}
                            //catch (Exception exp)
                            //{
                            //    rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocation Web API \n"+ exp.Message.ToString());
                            //    rossellLog.MessageLog("");
                            //}

                            //try
                            //{

                            //    using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                            //    {
                            //        isMoved = services.MoveToLocationWithSerial(workOrderRM.ARINVT_ID_RM, workOrderRM.FGMULTI_ID, workOrderRM.STAGING_LOCATIONS_ID, pickInventory.PickQuantity, masterLabel);
                            //        if (isMoved)
                            //        {
                            //            rossellLog.MessageLog("Successfully called Inventory/ScanId/MoveToLocation Web API ");
                            //            rossellLog.MessageLog("");
                            //        }
                            //        else
                            //        {
                            //            rossellLog.MessageLog("Could not successfully called Inventory/ScanId/MoveToLocation Web API ");
                            //            rossellLog.MessageLog("");
                            //        }

                            //    }
                            //}
                            //catch (Exception exp)
                            //{
                            //    rossellLog.MessageLog("Could not successfully called Inventory/ScanId/MoveToLocation Web API \n" + exp.Message.ToString());
                            //    rossellLog.MessageLog("");
                            //}

                            //using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                            //{
                            //    if (targetFGMultiID > 0)
                            //    {
                            //        fgMulti = labelBL.GetFGMULTIData(workOrderRM.FGMULTI_ID);
                            //        rossellLog.MessageLog("Get the FGMULTI Record After Inventory/ScanId/MoveToLocation Web API : " + fgMulti.ID);
                            //        rossellLog.MessageLog("");
                            //    }
                            //}

                            //try
                            //{

                            //    using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                            //    {                                   
                            //        isUpdated = services.UpdateMasterLabel(master_Label, fgMulti, workOrderRM.LOC_DESC.ToString());
                            //        if (isUpdated)
                            //        {
                            //            rossellLog.MessageLog("Successfully called Labels/PrintLabel/UpdateMasterLabel Web API ");
                            //            rossellLog.MessageLog("");
                            //        }
                            //        else
                            //        {
                            //            rossellLog.MessageLog("Could not successfully called Labels/PrintLabel/UpdateMasterLabel Web API ");
                            //            rossellLog.MessageLog("");
                            //        }
                            //    }

                            //    bool isUpdatedMasterLabelManually = Convert.ToBoolean(ConfigurationManager.AppSettings["MASTERLABELUPDATE"]);
                            //    if (isUpdatedMasterLabelManually)
                            //    {
                            //        using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                            //        {
                            //            bool isMasterLabelUpdate = labelBL.UpdateMasterLabel(master_Label.ID, master_Label.FGMULTI_ID, workOrderRM.LOC_DESC.ToString(), master_Label.LOT_NO);
                            //            if (isMasterLabelUpdate)
                            //            {
                            //                rossellLog.MessageLog("Successfully Updated the Master Label manually : " + master_Label.ID);
                            //                rossellLog.MessageLog("");
                            //            }
                            //        }
                            //    }
                            //}
                            //catch (Exception exp)
                            //{
                            //    rossellLog.MessageLog("Could not successfully called Labels/PrintLabel/UpdateMasterLabel Web API \n" + exp.Message.ToString());
                            //    rossellLog.MessageLog("");
                            //}
                            #endregion

                            using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                            {
                               bool isExecute= workOrderBL.HardAllocationPackage(workOrderRM.WORKORDER_ID, masterLabel.SERIAL);
                                if (isExecute)
                                {
                                    rossellLog.MessageLog("Successfully called ARS_UTILS.DO_HARDALLOC Package");
                                    rossellLog.MessageLog("");
                                }
                                else
                                {
                                    rossellLog.MessageLog("Could not successfully called ARS_UTILS.DO_HARDALLOC Package");
                                    rossellLog.MessageLog("");
                                }
                            }                            

                            try
                            {
                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                { 
                                    isReprint= services.ReprintSerialLabel(masterLabel.SERIAL, masterLabel.MASTER_LABEL_ID, Session["PRINTERNAME"].ToString());

                                    if (isReprint)
                                    {
                                        rossellLog.MessageLog("Successfully called Labels/PrintLabel/ReprintSerial Web API");
                                        rossellLog.MessageLog("");                                         
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Labels/PrintLabel/ReprintSerial Web API");
                                        rossellLog.MessageLog("");
                                    }

                                    using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                    {
                                        bool isMasterLabelUpdate = labelBL.UpdateMasterLabel(newSerialNumber, pickInventory.PickQuantity);
                                        decimal adjustQty = Math.Ceiling(pickInventory.PickQuantity) - pickInventory.PickQuantity;
                                        bool isAdjustMasterLabelUpdate = labelBL.AdjustMasterLabelQty(Convert.ToInt64(workOrderRM.MASTER_LABEL_ID), adjustQty);

                                        if (isMasterLabelUpdate)
                                        {
                                            rossellLog.MessageLog("Successfully Updated the Master Label Quantity manually After Reprint Serial Label Web API: " + newSerialNumber);
                                            rossellLog.MessageLog("");
                                        }

                                        if (isAdjustMasterLabelUpdate)
                                        {
                                            rossellLog.MessageLog("Successfully adjusted the Master Label Quantity manually After Reprint Serial Label Web API: " + workOrderRM.MASTER_LABEL_ID.ToString());
                                            rossellLog.MessageLog("");
                                        }
                                    }
                                }
                            }
                            catch (Exception exp)
                            {

                            }

                            if (workOrderRM != null)
                            {
                                workOrderRM.HARD_ALLOCATION = workOrderRM.HARD_ALLOCATION + Convert.ToDouble(pickInventory.PickQuantity);                                
                                workOrderRM.REQ_QUANTITY = workOrderRM.REQ_QUANTITY - Convert.ToDouble(pickInventory.PickQuantity);
                                workOrderRM.NEW_MASTER_LABEL_ID = masterLabel.MASTER_LABEL_ID;

                                workOrderRM.IS_PICKED = 1;

                                if (workOrderRM.REQ_QUANTITY > 0)
                                {
                                    workOrderRM.IS_PICKED = 0;

                                }
                                else
                                {
                                    workOrderRM.IS_PICKED = 1;
                                }

                                if (Convert.ToDouble(pickInventory.PickQuantity) < workOrderRM.REL_QUANTITY)
                                {
                                    workOrderRM.IS_SKIPPED = 1;                                    
                                }
                                else
                                {
                                    workOrderRM.IS_SKIPPED = 0;
                                }

                                rossellLog.MessageLog(string.Format("Successfully picked ARINVT_ID {0} and WORKORDER_ID {1}", workOrderRM.ARINVT_ID_RM, workOrderRM.WORKORDER_ID));
                                rossellLog.MessageLog("");
                                string json = JsonConvert.SerializeObject(workOrderRM);                                
                                rossellLog.MessageLog("JSON DATA : " + json.ToString());
                                rossellLog.MessageLog("");

                                List<WorkOrderRawMaterial> workOrderRMNextList = workOrderRMList.Where(o => o.ARINVT_ID_RM == pickInventory.ARINVT_ID && o.LOC_DESC == location && o.IS_PICKED == 0 && o.IS_SKIPPED == 0 && o.WORKORDER_ID != pickInventory.WorkOrderID).ToList();
                                if (workOrderRMNextList != null && workOrderRMNextList.Count > 0)
                                {
                                    CheckItemLocationAfterPicked(ref workOrderRMNextList, location, pickInventory.WorkOrderID, pickInventory.ARINVT_ID);
                                }
                            }

                            if (Session["TOTALPICKITEM"] == null)
                            {
                                Session["TOTALPICKITEM"] = workOrderRMList.Count.ToString();

                                List<WorkOrderRawMaterial> workOrderPCount = workOrderRMList.Where(o => o.IS_PICKED == 1).ToList();
                                int totalPickItem = Convert.ToInt32(Session["TOTALPICKITEM"].ToString());
                                if (totalPickItem == workOrderPCount.Count)
                                {
                                    List<WorkOrderRawMaterial> workOrderList = (List<WorkOrderRawMaterial>)Session["WorkOrderRM"];
                                    List<string> locationList = workOrderList.Where(o => o.IS_PICKED == 0).Select(o => o.LOC_DESC).Distinct().OrderBy(x => x).ToList();                                    
                                    locationList = locationList.OrderBy(x => x).ToList();

                                    if (locationList.Count > 0)
                                    {
                                        Session["STARTLOCATION"] = locationList[0].ToString();
                                    }
                                    else
                                    {
                                        jsonData = new
                                        {
                                            status = 1,
                                            serial = string.Empty,
                                        };
                                        return Json(jsonData);
                                    }
                                    jsonData = new
                                    {
                                        status = 0,
                                        serial = string.Empty,
                                    };
                                    return Json(jsonData);
                                }
                            }
                            else
                            {
                                List<WorkOrderRawMaterial> workOrderPCount = workOrderRMList.Where(o => o.IS_PICKED == 1).ToList();
                                int totalPickItem = Convert.ToInt32(Session["TOTALPICKITEM"].ToString());
                                if (totalPickItem == workOrderPCount.Count)
                                {
                                    List<WorkOrderRawMaterial> workOrderList = (List<WorkOrderRawMaterial>)Session["WorkOrderRM"];
                                    List<string> locationList = workOrderList.Where(o => o.IS_PICKED == 0).Select(o => o.LOC_DESC).Distinct().OrderBy(x => x).ToList();
                                    locationList = locationList.OrderBy(x => x).ToList();
                                    if (locationList.Count > 0)
                                    {
                                        Session["STARTLOCATION"] = locationList[0].ToString();
                                    }
                                    else
                                    {
                                        jsonData = new
                                        {
                                            status = 1,
                                            serial = string.Empty,
                                        };
                                        return Json(jsonData);
                                    }
                                    jsonData = new
                                    {
                                        status = 0,
                                        serial = string.Empty,
                                    };
                                    return Json(jsonData);
                                }
                            }
                            rossellLog.MessageLog("--------------------------------------------------------------- Rossell Kitting Material Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                            rossellLog.MessageLog("");
                        }
                        Session["ISPICKED"] = true;
                        jsonData = new
                        {                            
                            status = -1,
                            serial = newSerialNumber,
                        };
                        return Json(jsonData);
                    }
                    else
                    {
                        jsonData = new
                        {
                            status = -2,
                            serial = newSerialNumber,
                        };
                        return Json(jsonData);
                    }
                }
                else
                {
                    jsonData = new
                    {
                        status = -2,
                        serial = newSerialNumber,
                    };
                    return Json(jsonData);
                }
            }
            else
            {
                jsonData = new
                {
                    status = -2,
                    serial = newSerialNumber,
                };
                return Json(jsonData);
            }
        }

        private void CheckItemLocationAfterPicked(ref List<WorkOrderRawMaterial> workOrderRMLOC, string location, long workOrderID, long arinvtID)
        {            
            List<ItemLocation> itemLocationList = null;
            double totalQuantity = 0;
            totalQuantity = workOrderRMLOC.Where(itm => itm.ARINVT_ID_RM == arinvtID).Sum(item => item.REQ_QUANTITY);

            FilterWorkOrder filterWorkOrder = (FilterWorkOrder)Session["FilterWorkOrder"];
            using (ItemLocationBusinessLogic itemLocationBL = new ItemLocationBusinessLogic())
            {
                itemLocationList = itemLocationBL.GetItemLocation(arinvtID.ToString(), filterWorkOrder.ExpiryDate, totalQuantity);
            }

            List<WorkOrderRawMaterial> workOrderRawMaterialList = (List<WorkOrderRawMaterial>)Session["WorkOrderRM"];
            workOrderRawMaterialList = workOrderRawMaterialList.Where(o => o.ARINVT_ID_RM == arinvtID && o.LOC_DESC == location && o.IS_PICKED == 0 && o.IS_SKIPPED == 0 && o.WORKORDER_ID != workOrderID).ToList();

            if (workOrderRawMaterialList != null && workOrderRawMaterialList.Count > 0)
            {
                foreach (WorkOrderRawMaterial workOrderRM in workOrderRawMaterialList)
                {
                    workOrderRM.LOC_ID = -1;
                    workOrderRM.LOC_DESC = string.Empty;
                    workOrderRM.FGMULTI_ID = 0;
                }

                foreach (WorkOrderRawMaterial workOrderRM in workOrderRawMaterialList)
                {
                    List<ItemLocation> itemlocations = itemLocationList.Where(o => o.ARINVT_ID == workOrderRM.ARINVT_ID_RM).OrderBy(ord => ord.ARINVT_ID).ThenBy(ord => ord.LOC_DESCRIPTION).ToList();

                    // CASE 1 IF ARINVT HAS ONLY 1 LOCATION
                    if (itemlocations.Count == 1)
                    {
                        ItemLocation itemLocation = itemLocationList.FirstOrDefault(o => o.ARINVT_ID == workOrderRM.ARINVT_ID_RM);
                        workOrderRM.LOC_ID = itemLocation.LOC_ID;
                        workOrderRM.LOC_DESC = itemLocation.LOC_DESCRIPTION;
                        workOrderRM.FGMULTI_ID = itemLocation.FGMULTI_ID;
                    }
                    else
                    {
                        double totalReqQuantity = 0;
                        bool isfound = false;

                        totalReqQuantity = workOrderRawMaterialList.Where(itm => itm.ARINVT_ID_RM == workOrderRM.ARINVT_ID_RM).Sum(item => item.REQ_QUANTITY);
                        // CASE 2 IF ARINVT HAVE MULTIPLE FGMULTI ID BUT ONE LOCATION HAVE ENOUGH QUANTITY
                        foreach (ItemLocation itemloc in itemlocations)
                        {
                            if (itemloc.ON_HAND >= Convert.ToDouble(totalReqQuantity))
                            {
                                if (isfound == false)
                                {
                                    workOrderRM.LOC_ID = itemloc.LOC_ID;
                                    workOrderRM.LOC_DESC = itemloc.LOC_DESCRIPTION;
                                    workOrderRM.FGMULTI_ID = itemloc.FGMULTI_ID;
                                    isfound = true;
                                    break;
                                }
                            }
                        }
                        totalReqQuantity = 0;
                    }
                }

                //workOrderRMLOC.Clear();

                foreach (WorkOrderRawMaterial workOrderRM in workOrderRawMaterialList)
                {
                    WorkOrderRawMaterial _workOrderRMLOC = workOrderRMLOC.SingleOrDefault(rmloc => rmloc.ARINVT_ID_RM == workOrderRM.ARINVT_ID_RM && rmloc.WORKORDER_ID == workOrderRM.WORKORDER_ID);
                    _workOrderRMLOC.LOC_ID = workOrderRM.LOC_ID;
                    _workOrderRMLOC.LOC_DESC = workOrderRM.LOC_DESC;
                    _workOrderRMLOC.FGMULTI_ID = workOrderRM.FGMULTI_ID;                    
                }
            }
        }


        public JsonResult NextItem(PickInventory pickInventory)
        {
            Object jsonData = null;
            string Logfilename = Session["JSONFILENAME"].ToString();
            Logfilename = Logfilename.Replace(".json", ".txt");
            rossellLog.FileName = Logfilename;
            int iCount = 0;

            if (Session["COUNT"] == null)
            {
                Session["COUNT"] = 1;
                iCount = 1;
            }
            else
            {
                iCount = Convert.ToInt32(Session["COUNT"]) + 1;
            }

            if (Session["WorkOrderRMLOC"] != null)
            {
                string location = Session["STARTLOCATION"].ToString();
                List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];
                WorkOrderRawMaterial workOrderRM = workOrderRMList.SingleOrDefault(o => o.WORKORDER_ID == pickInventory.WorkOrderID && o.ARINVT_ID_RM == pickInventory.ARINVT_ID && o.LOC_DESC == location);
                rossellLog.MessageLog("--------------------------------------------------------------- Rossell Kitting Material Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                rossellLog.MessageLog("RECORD_NO : " + iCount.ToString() + "/" + workOrderRMList.Count.ToString());
                rossellLog.MessageLog("WORKORDER_BOM_ID : " + workOrderRM.WORKORDER_BOM_ID.ToString());
                rossellLog.MessageLog("WORKORDER_ID : " + workOrderRM.WORKORDER_ID.ToString());
                rossellLog.MessageLog("STANDARD_ID : " + workOrderRM.STANDARD_ID.ToString());
                rossellLog.MessageLog("ARINVT_ID_FG : " + workOrderRM.ARINVT_ID_FG.ToString());
                rossellLog.MessageLog("ITEM_NO : " + workOrderRM.ITEM_NO.ToString());
                rossellLog.MessageLog("SNDOP_ID : " + workOrderRM.SNDOP_ID.ToString());
                rossellLog.MessageLog("ARINVT_ID_RM : " + workOrderRM.ARINVT_ID_RM.ToString());
                rossellLog.MessageLog("PTSPER : " + workOrderRM.PTSPER.ToString());
                rossellLog.MessageLog("ITEMNO : " + workOrderRM.ITEMNO.ToString());
                rossellLog.MessageLog("DESCRIPTION : " + workOrderRM.DESCRIPTION.ToString());
                rossellLog.MessageLog("DESCRIPTION2 : " + workOrderRM.DESCRIPTION2.ToString());
                rossellLog.MessageLog("REQ_QUANTITY : " + workOrderRM.REQ_QUANTITY.ToString());
                rossellLog.MessageLog("HARD_ALLOCATION : " + workOrderRM.HARD_ALLOCATION.ToString());
                rossellLog.MessageLog("FGMULTI_ID : " + workOrderRM.FGMULTI_ID.ToString());
                rossellLog.MessageLog("LOC_ID : " + workOrderRM.LOC_ID.ToString());
                rossellLog.MessageLog("LOC_DESC : " + workOrderRM.LOC_DESC.ToString());
                rossellLog.MessageLog("");

                if (workOrderRM != null)
                {
                    workOrderRM.IS_PICKED = 1;

                    bool isPicked = false;
                    if (Session["ISPICKED"] != null)
                    {
                        isPicked = Convert.ToBoolean(Session["ISPICKED"]);
                    }

                    if (isPicked)
                    {
                        if (Convert.ToDouble(pickInventory.PickQuantity) < workOrderRM.REL_QUANTITY)
                        {
                            workOrderRM.IS_SKIPPED = 1;
                            rossellLog.MessageLog(string.Format("Skipped the ARINVT_ID {0} and WORKORDER_ID {1}", workOrderRM.ARINVT_ID_RM, workOrderRM.WORKORDER_ID));
                            rossellLog.MessageLog("");
                        }
                        else
                        {
                            workOrderRM.IS_SKIPPED = 0;
                        }
                    }
                    else
                    {
                        workOrderRM.IS_SKIPPED = 1;
                        rossellLog.MessageLog(string.Format("Skipped the ARINVT_ID {0} and WORKORDER_ID {1}", workOrderRM.ARINVT_ID_RM, workOrderRM.WORKORDER_ID));
                        rossellLog.MessageLog("");
                    }
                    
                    string json = JsonConvert.SerializeObject(workOrderRM);
                    rossellLog.MessageLog("JSON DATA : " + json.ToString());
                    rossellLog.MessageLog("");
                }

                if (Session["TOTALPICKITEM"] == null)
                {
                    Session["TOTALPICKITEM"] = workOrderRMList.Count.ToString();
                }
                
                List<WorkOrderRawMaterial> workOrderPCount = workOrderRMList.Where(o => o.IS_PICKED == 1).ToList();
                int totalPickItem = Convert.ToInt32(Session["TOTALPICKITEM"].ToString());
                if (totalPickItem == workOrderPCount.Count)
                {
                    List<WorkOrderRawMaterial> workOrderList = (List<WorkOrderRawMaterial>)Session["WorkOrderRM"];
                    List<string> locationList = workOrderList.Where(o => o.IS_PICKED == 0).Select(o => o.LOC_DESC).Distinct().OrderBy(o=>o).ToList();                    
                    locationList = locationList.OrderBy(x => x).ToList();
                    List<WorkOrderRawMaterial> workOrderSCount = workOrderRMList.Where(o => o.IS_SKIPPED == 1).ToList();
                    //workOrderRM.IS_SKIPPED = 0;
                    //workOrderRM.IS_PICKED = 0;
                    if (locationList!=null && locationList.Count > 0)
                    {
                        Session["STARTLOCATION"] = locationList[0].ToString();
                    }
                    else
                    {
                        jsonData = new
                        {
                            user = new object(),
                            status = 1,
                            message = string.Format("Location: {0} have {1} total items, Picked Items: {2}, Skipped Items: {3}. Click Ok button to continue.", location, workOrderRMList.Count.ToString(), (workOrderRMList.Count - workOrderSCount.Count), workOrderSCount.Count)
                        };
                        return Json(jsonData);
                    }

                    jsonData = new
                    {
                        user = new object(),
                        status = 0,
                        message = string.Format("Location: {0} have {1} total items, Picked Items: {2}, Skipped Items: {3}. Go to the next location.", location, workOrderRMList.Count.ToString(), (workOrderRMList.Count - workOrderSCount.Count), workOrderSCount.Count)
                    };
                    return Json(jsonData);
                }

                rossellLog.MessageLog("--------------------------------------------------------------- Rossell Kitting Material Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                rossellLog.MessageLog("");
            }
            jsonData = new
            {
                user = new object(),
                status = -1,
                message = string.Format("")
            };
            return Json(jsonData);
        }

        public JsonResult Validate(string serialNumber, string workOrderID, string arinvtID)
        {
            Object jsonData = null;
            if (!serialNumber.ToString().Equals(""))
            {
                string location = Session["STARTLOCATION"].ToString();
                List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];
                WorkOrderRawMaterial workOrderRM = workOrderRMList.SingleOrDefault(o => o.WORKORDER_ID == Convert.ToInt64(workOrderID) && o.ARINVT_ID_RM == Convert.ToInt64(arinvtID) && o.LOC_DESC == location);

                Label label;
                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                {                    
                    label = labelBL.GetMasterLabelData(serialNumber, workOrderRM.ARINVT_ID_RM);

                    if (label != null)
                    {
                        if (workOrderRM != null)
                        {
                            workOrderRM.MASTER_LABEL_ID = label.ID;
                        }

                        if (label.FGMULTI_ID <= 0)
                        {
                            workOrderRM.IS_ERROR = 0;
                            workOrderRM.ERROR_DESCRIPTION = "The scanned serial number doesn't belong to <ITEM_NO>";
                            //rossellLog.WriteTraceLog("The scanned serial number doesn't belong to <ITEM_NO>");
                            return Json(2);
                        }

                        if (Convert.ToDouble(label.QUANTITY) < workOrderRM.REQ_QUANTITY)
                        {
                            workOrderRM.IS_ERROR = 0;
                            workOrderRM.ERROR_DESCRIPTION = "The scanned serial number doesn't have enough quantity to pick";
                            jsonData = new
                            {
                                user = new object(),
                                status = 1,
                                message = "The scanned serial number doesn't have enough quantity to pick"
                            };
                            return Json(jsonData);
                        }
                    }
                    else
                    {
                        label = labelBL.GetMasterLabelNotFoundReason(serialNumber, workOrderRM.ARINVT_ID_RM);                        
                        if (label != null)
                        {
                            if (!label.REASON.Equals(""))
                            {
                                label.REASON = label.REASON.Replace("#WORKORDER", label.WORKORDER);
                                label.REASON = label.REASON.Replace("#LOTNO", label.FG_LOTNO);
                            }
                            else
                            {
                                label.REASON = "Serial No does not belong to Item : " + workOrderRM.ITEMNO;
                            }

                            jsonData = new
                            {
                                user = new object(),
                                status = 1,
                                message = label.REASON
                            };
                        }
                        else
                        {
                            jsonData = new
                            {
                                user = new object(),
                                status = 1,
                                message = "Serial Number " + serialNumber + " not found."
                            };
                        }                        

                        return Json(jsonData); //1
                    }
                    jsonData = new
                    {
                        user = new object(),
                        status = 0,
                        message = string.Empty
                    };
                    return Json(jsonData); //0
                }
            }
            else
            {
                jsonData = new
                {
                    user = new object(),
                    status = 3,
                    message = string.Empty
                };
                return Json(jsonData); //3
            }
        }

        public JsonResult RePrint(string serialNumber, string workOrderID, string arinvtID)
        {
            bool isreprint = false;
            using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
            {
                string location = Session["STARTLOCATION"].ToString();
                List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];
                WorkOrderRawMaterial workOrderRM = workOrderRMList.SingleOrDefault(o => o.WORKORDER_ID == Convert.ToInt64(workOrderID) && o.ARINVT_ID_RM == Convert.ToInt64(arinvtID) && o.LOC_DESC == location);
                //decimal iserialFrom = 0;

                //using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                //{
                //    Master_Label master_label = labelBL.GetMasterLabelData(Convert.ToInt64(workOrderRM.NEW_MASTER_LABEL_ID));
                //    iserialFrom = labelBL.GetMasterLabelBetween(master_label.ID, master_label.LM_LABEL_ID);
                //}

                //decimal iserialTo = workOrderRM.NEW_MASTER_LABEL_ID;
                //string serialFrom = iserialFrom.ToString().PadLeft(9, '0').ToString();
                //string serialTo = iserialTo.ToString().PadLeft(9, '0').ToString();
                //isreprint = serviceBL.ReprintMasterLabel(Convert.ToInt64(workOrderRM.NEW_MASTER_LABEL_ID), Session["PRINTERNAME"].ToString());
                //isreprint = serviceBL.ReprintBetweenMasterLabel(serialFrom, serialTo);              
                isreprint = serviceBL.ReprintSerialLabel(serialNumber, Convert.ToInt64(workOrderRM.NEW_MASTER_LABEL_ID), Session["PRINTERNAME"].ToString());
            }
            if (isreprint)
            {
                return Json(1);
            }
            else
            {
                return Json(0);
            }                
        }

        public JsonResult Back(string serialNumber, string workOrderID, string arinvtID)
        {
            //string location = Session["STARTLOCATION"].ToString();
            List<WorkOrderRawMaterial> workOrderRMList = (List<WorkOrderRawMaterial>)Session["WorkOrderRMLOC"];

            List<string> locationList = workOrderRMList.Where(o => o.IS_SKIPPED == 1).Select(o => o.LOC_DESC).Distinct().OrderByDescending(o => o).ToList();
            if (locationList != null && locationList.Count > 0)
            {
                string location = locationList[0].ToString();
                WorkOrderRawMaterial workOrderRM = workOrderRMList.LastOrDefault(o => o.IS_SKIPPED == 1 && o.LOC_DESC == location);
                Session["STARTLOCATION"] = locationList[0].ToString();
                if (workOrderRM != null)
                {
                    Session["ISSKIPPED"] = "true";
                    return Json(1);
                }
                else
                {
                    return Json(0);
                }
            }
            else
            {
                return Json(0);
            }

        }

    }
}