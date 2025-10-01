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
    public class DirectMoveSerialController : Controller
    {
        RossellTextLogger rossellLog = new RossellTextLogger();
        // GET: DirectMoveSerial
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

        public ActionResult Move()
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
                        BusinessEntity.Location locationFGMUTI;
                        int transCode = Convert.ToInt32(ConfigurationManager.AppSettings["TRANSCODE"].ToString());
                        Item item=null;
                        using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                        {
                            label = labelBL.GetMasterLabelDetailData(serialNumber);
                            
                            if (label != null)
                            {
                                FGMULTI fgmulti = labelBL.GetFGMULTIData(label.FGMULTI_ID);
                                if(fgmulti == null)
                                {
                                    using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                                    {
                                        using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
                                        {
                                            item = locationBL.GetItemData(label.ARINVT_ID);
                                            BusinessEntity.Location location = locationBL.GetLocation(label.LOCATION_DESC);
                                            serviceBL.AddLocation(label.ARINVT_ID, location.ID, label.LOT_DESC);

                                            locationFGMUTI = locationBL.GetLocation(label.LOCATION_DESC, label.ARINVT_ID, label.LOT_DESC);
                                            if (locationFGMUTI != null)
                                            {
                                                labelBL.UpdateMasterLabel(label.SERIAL, label.TOTAL_QUANTITY, locationFGMUTI.FGMULI_ID);
                                                if (item != null)
                                                {
                                                    serviceBL.AddItemToLocation(label.ARINVT_ID, locationFGMUTI.FGMULI_ID, label.TOTAL_QUANTITY, item.STANDARD_ID, transCode, DateTime.Now, label);
                                                }
                                                
                                                label = labelBL.GetMasterLabelDetailData(serialNumber);
                                            }
                                        }
                                    }
                                }

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

        public ActionResult ValidateLocation(string locationName, long arinvtID, string serialNumber)
        {
            Object jsonData = null;
            if (Session["Users"] != null)
            {
                try
                {
                    if (locationName != null && !locationName.ToString().Equals(""))
                    {
                        BusinessEntity.Location location;
                        BusinessEntity.Location locationFGMUTI;
                        using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                        {
                            location = locationBL.GetLocation(locationName);
                            if (location != null)
                            {
                                BusinessEntity.MasterLabelDetail label;
                                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                {
                                    label = labelBL.GetMasterLabelDetailData(serialNumber);
                                }

                                locationFGMUTI = locationBL.GetLocation(locationName, arinvtID, label.LOT_DESC);
                                if (locationFGMUTI == null)
                                {
                                    using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
                                    {
                                        serviceBL.AddLocation(label.ARINVT_ID, location.ID, label.LOT_DESC);
                                    }
                                }
                                locationFGMUTI = locationBL.GetLocation(locationName, arinvtID, label.LOT_DESC);
                                jsonData = new
                                {
                                    location = locationFGMUTI,
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
                                    status = 1,
                                    message = "The scanned location doesn't exists.<p></p>"
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

        public ActionResult AddLocation(string locationName, long arinvtID, string serialNumber)
        {
            Object jsonData = null;
            if (Session["Users"] != null)
            {
                try
                {
                    if (locationName != null && !locationName.ToString().Equals(""))
                    {
                        BusinessEntity.Location location;

                        using (ItemLocationBusinessLogic locationBL = new ItemLocationBusinessLogic())
                        {
                            location = locationBL.GetLocation(locationName);
                            if (location == null)
                            {
                                long locationID = locationBL.AddLocation(locationName);
                                BusinessEntity.MasterLabelDetail label;
                                using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                {
                                    label = labelBL.GetMasterLabelDetailData(serialNumber);
                                }
                                location = locationBL.GetLocation(locationName, arinvtID, label.LOT_DESC);
                                if (location == null)
                                {
                                    using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
                                    {
                                        serviceBL.AddLocation(label.ARINVT_ID, locationID, label.LOT_DESC);
                                    }
                                }

                                location = locationBL.GetLocation(locationName, arinvtID, label.LOT_DESC);

                                jsonData = new
                                {
                                    ID = locationID,
                                    status = 0,
                                    message = "Location " + locationName + " successfully added."
                                };
                                return Json(jsonData);
                            }
                            else
                            {
                                jsonData = new
                                {
                                    ID = 0,
                                    status = 1,
                                    message = "The scanned location : " + locationName + " already exists in the database"
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

        public ActionResult MoveSerial(string serial, long serialID, decimal scanQty, long locationID, bool isRepack, long fgmultiID, string sourceLocation, string targetLocation, long arinvtID, string lotNo)
        {
            Object jsonData = null;
            WebApiResponse webAPIResponse = new WebApiResponse();
            if (Session["Users"] != null)
            {
                string newSerialNumber = string.Empty;
                bool isMoved = false;
                try
                {
                    string Logfilename = "Log_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt";
                    rossellLog.FileName = Logfilename;

                    if (serial != null && !serial.ToString().Equals(""))
                    {
                        rossellLog.MessageLog("--------------------------------------------------------------- SilganDispensing WMS Start " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                        rossellLog.MessageLog("MASTER LABEL ID : " + serialID.ToString());
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
                                                    labelBL.UpdateMasterLabel(newSerialNumber, scanQty, location.FGMULI_ID, serialID, location.LocationName);
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
                                location = locationBL.GetLocation(targetLocation, arinvtID, lotNo);
                                
                                using (ServiceBusinessLogic services = new ServiceBusinessLogic())
                                {
                                    webAPIResponse = services.MoveToLocation(arinvtID, fgmultiID, location.FGMULI_ID, scanQty, masterLabel);
                                    if (webAPIResponse.DidSucceed)
                                    {
                                        location = locationBL.GetLocation(targetLocation, arinvtID, lotNo);
                                        if (location != null)
                                        {
                                            using (LabelBusinessLogic labelBL = new LabelBusinessLogic())
                                            {
                                                labelBL.UpdateMasterLabel(serial, scanQty, location.FGMULI_ID, location.LocationName);
                                            }
                                        }

                                        using (TransLogBusinessLogic transLogBL = new TransLogBusinessLogic())
                                        {
                                            TransLog transLog = transLogBL.GetTransLogData(arinvtID, location.FGMULI_ID);
                                            if(transLog!=null)
                                            {
                                                bool updateReason = transLogBL.UpdateTransLogReason(transLog.ID, "Scanner Direct Move");
                                                TransLog transLogMasterLabel = transLogBL.GetTransLogMasterLabelData(transLog.ID);
                                                if(transLogMasterLabel == null)
                                                {
                                                    transLogBL.AddTransLogMasterLabel(transLog.ID, masterLabel.MASTER_LABEL_ID, scanQty);
                                                }                                                
                                            }
                                        }
                                        jsonData = new
                                        {
                                            label = new object(),
                                            status = 0,
                                            serial = newSerialNumber,
                                            message = "Successfully moved the serial #" + serial + " into Location # " + targetLocation
                                        };
                                        rossellLog.MessageLog("Successfully called Inventory/TransactionLocation/MoveToLocationFromMasterLabel Web API ");
                                        rossellLog.MessageLog("");
                                    }
                                    else
                                    {
                                        jsonData = new
                                        {
                                            label = new object(),
                                            status = 3,
                                            serial = newSerialNumber,
                                            message = "Couldn't successfully moved the serial #" + serial + " into Location # " + targetLocation + " due to following issue \n" + webAPIResponse.Message
                                        };
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

                        rossellLog.MessageLog("--------------------------------------------------------------- SilganDispensing WMS End " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + "-----------------------------------------------------------------");
                        rossellLog.MessageLog("");
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

        public ActionResult RePrint(string serialNumber)
        {
            if (Session["Users"] != null)
            {
                bool isreprint = false;
                using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
                {               
                    isreprint = serviceBL.ReprintSerialLabel(serialNumber, Convert.ToInt64(serialNumber), Session["PRINTERNAME"].ToString());
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
            else
            {
                return Json(-1);
            }
        }

    }
}