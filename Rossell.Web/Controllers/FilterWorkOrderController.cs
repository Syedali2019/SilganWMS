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
    public class FilterWorkOrderController : Controller
    {
        // GET: FilterWorkOrder
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Selection Criteria";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            if (Session["Users"] != null)
            {
                using (InventoryGroupBusinessLogic inventoryBL = new InventoryGroupBusinessLogic())
                {
                    ViewBag.InventoryGroup = inventoryBL.GetInventoryGroupData();
                    ViewBag.PartNumber = inventoryBL.GetPartNumberData();
                    ViewBag.ExpiryDate = DateTime.Now.ToString("dd-MM-yyyy");
                    return View();
                }
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }                
        }

        [HttpPost]
        public ActionResult Next(FilterWorkOrder filterWorkOrder)
        {
            if (Session["Users"] != null)
            {
                using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                {
                    if (Session["FilterWorkOrder"] != null)
                    {
                        Session.Remove("FilterWorkOrder");
                    }
                    if (filterWorkOrder.ExpiryDate == null)
                    {
                        filterWorkOrder.ExpiryDate = DateTime.Now;
                    }
                    var result = filterWorkOrder;
                    Session["FilterWorkOrder"] = filterWorkOrder;
                    return Json(true, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        public ActionResult FilterInventoryGroup(Int32 inventoryGroupID)
        {
            if (Session["Users"] != null)
            {
                using (WorkOrderBusinessLogic workOrderBL = new WorkOrderBusinessLogic())
                {
                    List<PartNumber> partNumberList = null;
                    using (InventoryGroupBusinessLogic inventoryBL = new InventoryGroupBusinessLogic())
                    {
                        if (inventoryGroupID > 0)
                        {
                            partNumberList = inventoryBL.GetPartNumberData(inventoryGroupID);
                        }
                        else
                        {
                            partNumberList = inventoryBL.GetPartNumberData();
                        }
                        var result = partNumberList;
                        return Json(result);                        
                    }
                }
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }
    }
}