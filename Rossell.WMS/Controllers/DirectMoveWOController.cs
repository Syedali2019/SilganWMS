using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Rossell.BusinessLogic;
using Rossell.BusinessEntity;
using Rossell.Common;
using System.Configuration;

namespace Rossell.WMS.Controllers
{
    public class DirectMoveWOController : Controller
    {
        RossellTextLogger rossellLog = new RossellTextLogger();
        // GET: DirectMoveWO
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Selection Criteria";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();
            ViewBag.UseKeyboardOnScan = ConfigurationManager.AppSettings["USEKEYBOARDONSCAN"].ToString().ToUpper();

            if (Session["Users"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        public ActionResult Validate(string serialNumber)
        {
            Object jsonData = null;
            if (Session["Users"] != null)
            {
                try
                {
                    if (serialNumber != null && !serialNumber.ToString().Equals(""))
                    {
                        BusinessEntity.MasterLabelDetail label;
                        using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                        {
                            label = labelBL.GetMasterLabelDetailData(serialNumber);
                            if (label != null)
                            {
                                jsonData = new
                                {
                                    label = label,
                                    status = 0,
                                    message = ""
                                };
                                return Json(jsonData);
                            }
                            else
                            {
                                jsonData = new
                                {
                                    label = new object(),
                                    status = 1,
                                    message = "The scanned serial number doesn't exists. Please enter valid serial number."
                                };
                                return Json(jsonData);
                            }
                        }
                    }
                    else
                    {
                        jsonData = new
                        {
                            label = new object(),
                            status = 3,
                            message = string.Empty
                        };
                        return Json(jsonData); //3
                    }
                }
                catch (Exception exp)
                {
                    jsonData = new
                    {
                        label = new object(),
                        status = 3,
                        message = string.Empty
                    };
                    return Json(jsonData); //3
                }
            }
            else
            {
                jsonData = new
                {
                    label = new object(),
                    status = -3,
                    message = "Invalid Session"
                };
                return Json(jsonData);
            }
        }

        public ActionResult ValidateWorkOrder(long workOrder, string serialNumber)
        {
            Object jsonData = null;
            if (Session["Users"] != null)
            {
                try
                {
                    if (workOrder != null && Convert.ToInt32(workOrder) >0)
                    {
                        
                        using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                        {
                            WorkOrderSingle workOrderSingle = workOrderBL.GetWorkOrderData(workOrder);

                            if (workOrderSingle != null)
                            {
                                BusinessEntity.MasterLabelDetail label;
                                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                {
                                    label = labelBL.GetMasterLabelDetailData(serialNumber);
                                }

                                WorkOrderBOMSingle workOrderBOMSingle = workOrderBL.GetWorkOrderItemData(workOrder, label.ARINVT_ID);
                                if(workOrderBOMSingle !=null)
                                {
                                    if(label!=null)
                                    {
                                        label.BOM_QTY = workOrderBOMSingle.QUAN;
                                    }                                    
                                }
                                else
                                {
                                    jsonData = new
                                    {
                                        label = new object(),
                                        status = 1,
                                        message = "Item doesn't belong to WorkOrder #" + workOrder.ToString()
                                    };
                                    return Json(jsonData);
                                }

                                MFGCell mFGCell = workOrderBL.GetWorkOrderMFGCellData(workOrder);

                                if (mFGCell != null && mFGCell.STAGING_LOCATION_ID <=0)
                                {
                                    jsonData = new
                                    {
                                        label = new object(),
                                        locationID = 0,
                                        status = 1,
                                        message = "Staging Location In not defined in MFG Cell to WorkOrder #" + workOrder.ToString()
                                    };
                                }
                                else if(mFGCell == null)
                                {

                                    jsonData = new
                                    {
                                        label = new object(),
                                        locationID = 0,
                                        status = 1,
                                        message = "MFG Cell not defined to WorkOrder #" + workOrder.ToString()
                                    };
                                    return Json(jsonData);
                                }

                                jsonData = new
                                {
                                    label = label,
                                    locationID= mFGCell.STAGING_LOCATION_ID,
                                    status = 0,
                                    message = ""
                                };
                                return Json(jsonData);
                            }
                            else
                            {
                                jsonData = new
                                {
                                    location = new object(),
                                    locationID = 0,
                                    status = 1,
                                    message = "The scanned WorkOrder doesn't exists. Please enter valid WorkOrder"
                                };
                                return Json(jsonData);
                            }
                        }
                    }
                    else
                    {
                        jsonData = new
                        {
                            label = new object(),
                            locationID = 0,
                            status = 3,
                            message = string.Empty
                        };
                        return Json(jsonData); //3
                    }
                }
                catch (Exception exp)
                {
                    jsonData = new
                    {
                        label = new object(),
                        locationID = 0,
                        status = 3,
                        message = string.Empty
                    };
                    return Json(jsonData); //3
                }
            }
            else
            {
                jsonData = new
                {
                    label = new object(),
                    locationID = 0,
                    status = -3,
                    message = "Invalid Session"
                };
                return Json(jsonData);
            }
        }

        public ActionResult MoveSerial(string serial, long serialID, decimal scanQty, long locationID, bool isRepack, long fgmultiID, string sourceLocation, string targetLocation, long arinvtID, long workOrderID, string lotNo)
        {
            Object jsonData = null;
            if (Session["Users"] != null)
            {
                string newSerialNumber = string.Empty;
                bool isMoved = false;
                BusinessEntity.FGMULTI newFGMULTI = null;
                try
                {
                    string Logfilename = "RossellWMSLog_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt";
                    rossellLog.FileName = Logfilename;

                    if (serial != null && !serial.ToString().Equals(""))
                    {
                        rossellLog.MessageLog("--------------------------------------------------------------- Rossell WMS Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                        rossellLog.MessageLog("MASTER LABEL ID : " + serialID.ToString());
                        rossellLog.MessageLog("WORKORDER ID : " + workOrderID.ToString());
                        rossellLog.MessageLog("SERIAL : " + serial.ToString());
                        rossellLog.MessageLog("ARINVT_ID : " + arinvtID.ToString());
                        rossellLog.MessageLog("SOURCE LOCATION : " + sourceLocation);
                        rossellLog.MessageLog("SOURCE FGMULTI ID : " + fgmultiID.ToString());
                        rossellLog.MessageLog("TARGET LOCATION : " + locationID.ToString());
                        rossellLog.MessageLog("IS REPACK : " + isRepack.ToString());
                        rossellLog.MessageLog("");

                        try
                        {

                            using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                            {
                                if (isRepack)
                                {
                                    newSerialNumber = services.MasterLabelRepack(serialID, scanQty, serial);
                                    if (!newSerialNumber.Equals(""))
                                    {
                                        BusinessEntity.Location location;
                                        using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                                        {
                                            location = locationBL.GetLocation(targetLocation, arinvtID, lotNo);
                                            if (location != null)
                                            {
                                                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                                {
                                                    labelBL.UpdateMasterLabel(newSerialNumber, scanQty, location.FGMULI_ID);
                                                }
                                            }
                                        }

                                        rossellLog.MessageLog("Successfully called Inventory/Disposition/MasterLabelRepack Web API ");
                                        rossellLog.MessageLog("");
                                        rossellLog.MessageLog("Serial Number After Repack Web API : " + newSerialNumber);
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Inventory/Disposition/MasterLabelRepack Web API");
                                        rossellLog.MessageLog("");
                                        jsonData = new
                                        {
                                            status = 3,
                                            serial = string.Empty,
                                            message = "Could not successfully called Inventory/Disposition/MasterLabelRepack Web API"
                                        };
                                        return Json(jsonData);
                                    }
                                }
                                else
                                {
                                    BusinessEntity.Location location;
                                    using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                                    {
                                        location = locationBL.GetLocation(locationID);
                                        if (location != null)
                                        {
                                            using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                            {
                                                labelBL.UpdateMasterLabel(serial, scanQty);
                                            }
                                        }
                                    }
                                    newSerialNumber = serial;
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            rossellLog.MessageLog("Could not successfully called Inventory/Disposition/MasterLabelRepack Web API \n" + exp.Message.ToString());
                            rossellLog.MessageLog("");
                        }

                        BusinessEntity.MasterLabel masterLabel = null;

                        using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                        {
                            masterLabel = labelBL.GetMasterLabelData(newSerialNumber);
                            rossellLog.MessageLog("Master Label ID : " + masterLabel.MASTER_LABEL_ID);
                            rossellLog.MessageLog("");
                        }

                        try
                        {

                            using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                            {
                                BusinessEntity.Location location;
                                location = locationBL.GetLocation(locationID);

                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    isMoved = services.MoveToLocationFromMasterLabel(masterLabel.SERIAL, location.ID);
                                    if (isMoved)
                                    {
                                        using (TransLogBusinessLogic transLogBL = new TransLogBusinessLogic())
                                        {
                                            TransLog transLog = transLogBL.GetTransLogData(arinvtID, location.FGMULI_ID);
                                            if (transLog != null)
                                            {
                                                //bool updateReason = transLogBL.UpdateTransLogReason(transLog.ID, "Scanner Direct Move");
                                                TransLog transLogMasterLabel = transLogBL.GetTransLogMasterLabelData(transLog.ID);
                                                if (transLogMasterLabel == null)
                                                {
                                                    transLogBL.AddTransLogMasterLabel(transLog.ID, masterLabel.MASTER_LABEL_ID, scanQty);
                                                }
                                            }
                                        }

                                        rossellLog.MessageLog("Successfully called Inventory/TransactionLocation/MoveToLocationFromMasterLabel Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocationFromMasterLabel Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            rossellLog.MessageLog("Could not successfully called Inventory/TransactionLocation/MoveToLocationFromMasterLabel Web API \n" + exp.Message.ToString());
                            rossellLog.MessageLog("");
                        }

                        try
                        {
                            using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                            {
                                User user = null;
                                if (Session["Users"] != null)
                                {
                                    user = (User)Session["Users"];
                                }

                                bool isExecute = false;
                                if (user != null)
                                {
                                    isExecute = workOrderBL.HardAllocationPackage(workOrderID, masterLabel.SERIAL, user.userEmail);
                                }
                                else
                                {
                                    isExecute = workOrderBL.HardAllocationPackage(workOrderID, masterLabel.SERIAL, string.Empty);
                                }

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
                        }
                        catch(Exception exp)
                        {
                            rossellLog.MessageLog("Could not successfully called ARS_UTILS.DO_HARDALLOC Package \n" + exp.Message.ToString());
                            rossellLog.MessageLog("");
                        }


                        rossellLog.MessageLog("--------------------------------------------------------------- Rossell WMS End " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                        rossellLog.MessageLog("");

                        jsonData = new
                        {
                            label = new object(),
                            status = 0,
                            serial = newSerialNumber,
                            message = "Successfully moved the serial #" + serial + " into Location # " + targetLocation
                        };
                        return Json(jsonData);
                    }
                    else
                    {
                        jsonData = new
                        {
                            label = new object(),
                            status = 3,
                            serial = string.Empty,
                            message = "Please enter serial Number"
                        };
                        return Json(jsonData); //3
                    }
                }
                catch (Exception exp)
                {
                    jsonData = new
                    {
                        label = new object(),
                        status = 3,
                        serial = string.Empty,
                        message = exp.ToString()
                    };
                    return Json(jsonData);
                }
            }
            else
            {
                jsonData = new
                {
                    label = new object(),
                    status = -3,
                    serial = string.Empty,
                    message = "Invalid Session"
                };
                return Json(jsonData);
            }
        }

    }
}