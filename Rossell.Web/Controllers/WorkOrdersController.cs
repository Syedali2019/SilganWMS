using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Rossell.BusinessLogic;
using Rossell.BusinessEntity;
using Rossell.Common;
using System.Configuration;

namespace Rossell.Web.Controllers
{
    public class WorkOrdersController : Controller
    {
        // GET: WorkOrders
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Work Orders";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();
            bool workOrderLogging = Convert.ToBoolean(ConfigurationManager.AppSettings["WORKORDERLOGGING"].ToString());

            if (Session["Users"] != null)
            {
                User user = (User)Session["Users"];
                Session.Remove("TOTALPICKITEM");
                Session.Remove("WorkOrderRMLOC");
                Session.Remove("STARTLOCATION");
                Session.Remove("WorkOrderRM");
                Session.Remove("WorkOrderRMNoFGMULTI");
                Session.Remove("WorkOrderRMNoENOUGHONHAND");               

                using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                {
                    FilterWorkOrder filterWorkOrder =(FilterWorkOrder)Session["FilterWorkOrder"];
                    int mustStartDateDaysAdd = Convert.ToInt32(ConfigurationManager.AppSettings["MUSTSTARTDATEDAYSADD"].ToString());
                    var result = workOrderBL.GetWorkOrderData(filterWorkOrder.PartNumberID, filterWorkOrder.InventoryGroupID, filterWorkOrder.MustStartDate, filterWorkOrder.ExpiryDate, mustStartDateDaysAdd, "Filter WorkOrder" ,user.userEmail, workOrderLogging);                    
                    return View(result);
                }
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }            
        }


        public ActionResult Next(string[] workOrder, string isCheckNonFGMulti)
        {
            Object jsonData = null;
            
            if (Session["Users"] != null)
            {
                User user = (User)Session["Users"];
                bool workOrderLogging = Convert.ToBoolean(ConfigurationManager.AppSettings["WORKORDERLOGGING"].ToString());
                if (workOrder != null)
                {
                    if (Session["WorkOrderRM"] != null)
                    {
                        Session.Remove("WorkOrderRM");
                    }

                    if (Session["STARTLOCATION"] != null)
                    {
                        Session.Remove("STARTLOCATION");
                    }
                    var result = workOrder;
                    string strWorkOrderID = string.Empty;
                    string strArinvtIDs = string.Empty;

                    foreach (string _workOrderID in workOrder)
                    {
                        if (strWorkOrderID.Length <= 0)
                        {
                            strWorkOrderID = _workOrderID;
                        }
                        else
                        {
                            strWorkOrderID += ", " + _workOrderID;
                        }
                    }

                    List<WorkOrderRawMaterial> workOrderRawMaterialList;
                    List<WorkOrderRawMaterial> workOrderRawMaterialListNOFGMULTI;
                    List<WorkOrderRawMaterial> workOrderRawMaterialListNOENOUGHONHAND;
                    string filename = string.Empty;
                    using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                    {
                        FilterWorkOrder filterWorkOrder = (FilterWorkOrder)Session["FilterWorkOrder"];
                        workOrderRawMaterialList = workOrderBL.GetWorkOrderRawMaterial(strWorkOrderID, out filename, filterWorkOrder.ExpiryDate, out workOrderRawMaterialListNOFGMULTI, out workOrderRawMaterialListNOENOUGHONHAND, "Selected WorkOrder", user.userEmail, workOrderLogging);
                    }
                    if (workOrderRawMaterialList != null && workOrderRawMaterialList.Count > 0)
                    {
                        List<string> locationList = workOrderRawMaterialList.Select(o => o.LOC_DESC).Distinct().OrderBy(x => x).ToList();
                        locationList = locationList.OrderBy(x => x).ToList();
                        //locationList.Sort();
                        string startlocation = locationList[0].ToString();

                        Session["WorkOrderRM"] = workOrderRawMaterialList;
                        Session["WorkOrderRMNoFGMULTI"] = workOrderRawMaterialListNOFGMULTI;
                        Session["WorkOrderRMNoENOUGHONHAND"] = workOrderRawMaterialListNOENOUGHONHAND;
                        Session["STARTLOCATION"] = startlocation;
                        Session["JSONFILENAME"] = filename;
                        if (Convert.ToBoolean(isCheckNonFGMulti))
                        {
                            if (workOrder.ToList().Count > 1 && (workOrderRawMaterialListNOFGMULTI.Count > 0 || workOrderRawMaterialListNOENOUGHONHAND.Count > 0))
                            {
                                string noFGMultiItemWO = string.Empty;

                                if (workOrderRawMaterialListNOFGMULTI != null && workOrderRawMaterialListNOFGMULTI.Count > 0)
                                {
                                    foreach (WorkOrderRawMaterial workOrderRawMaterial in workOrderRawMaterialListNOFGMULTI)
                                    {
                                        if (noFGMultiItemWO.Length <= 0)
                                        {
                                            noFGMultiItemWO = " WO = " + workOrderRawMaterial.WORKORDER_ID.ToString() + " ItemNo = " + workOrderRawMaterial.ITEMNO.ToString();
                                        }
                                        else
                                        {
                                            noFGMultiItemWO += ", WO = " + workOrderRawMaterial.WORKORDER_ID.ToString() + " ItemNo = " + workOrderRawMaterial.ITEMNO.ToString();
                                        }
                                    }
                                }

                                if (workOrderRawMaterialListNOENOUGHONHAND != null && workOrderRawMaterialListNOENOUGHONHAND.Count > 0)
                                {
                                    foreach (WorkOrderRawMaterial workOrderRawMaterial in workOrderRawMaterialListNOENOUGHONHAND)
                                    {
                                        if (noFGMultiItemWO.Length <= 0)
                                        {
                                            noFGMultiItemWO = " WO = " + workOrderRawMaterial.WORKORDER_ID.ToString() + " ItemNo = " + workOrderRawMaterial.ITEMNO.ToString();
                                        }
                                        else
                                        {
                                            noFGMultiItemWO += ", WO = " + workOrderRawMaterial.WORKORDER_ID.ToString() + " ItemNo = " + workOrderRawMaterial.ITEMNO.ToString();
                                        }
                                    }
                                }

                                jsonData = new
                                {
                                    user = new object(),
                                    status = 3,
                                    message = string.Format("These WorkOrder(s): {0} are found in which item(s) have no inventory or less inventory. ", noFGMultiItemWO)
                                };
                                return Json(jsonData, JsonRequestBehavior.AllowGet);
                            }
                        }

                        jsonData = new
                        {
                            user = new object(),
                            status = 1,
                            message = string.Empty
                        };
                        return Json(jsonData, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        jsonData = new
                        {
                            user = new object(),
                            status = 0,
                            message = string.Format("No items found to pick on selected WorkOrders")
                        };
                        return Json(jsonData, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    jsonData = new
                    {
                        user = new object(),
                        status = 2,
                        message = string.Format("Please select atleast one Work Order")
                    };
                    return Json(jsonData, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                jsonData = new
                {
                    user = new object(),
                    status = -1,
                    message = string.Empty
                };
                return Json(jsonData, JsonRequestBehavior.AllowGet);
            }
        }
    }
}