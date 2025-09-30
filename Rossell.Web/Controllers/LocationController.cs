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
    public class LocationController : Controller
    {        
        // GET: Location
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Location";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            if (Session["Users"] != null)
            {
                Session.Remove("TOTALPICKITEM");
                Session.Remove("WorkOrderRMLOC");                
                ViewBag.Location = Session["STARTLOCATION"].ToString();
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }           
        }

        public ActionResult Next(Location location)
        {
            if (Session["Users"] != null)
            {
                if (location.LocationName.ToUpper().ToString().Equals(location.ScanLocation.ToUpper().ToString()))
                {
                    List<WorkOrderRawMaterial> workOrderRawMaterialList;
                    List<WorkOrderRawMaterial> workOrderRawMaterialLOCList;
                    workOrderRawMaterialList = (List<WorkOrderRawMaterial>)Session["WorkOrderRM"];
                    workOrderRawMaterialLOCList = workOrderRawMaterialList.Where(m => m.LOC_DESC == location.ScanLocation.ToUpper().ToString()).ToList().OrderBy(ord => ord.ARINVT_ID_RM).ThenBy(ord => ord.WORKORDER_ID).ToList();
                    if (Session["WorkOrderRMLOC"] != null)
                    {
                        Session.Remove("WorkOrderRMLOC");
                    }
                    Session["WorkOrderRMLOC"] = workOrderRawMaterialLOCList;
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
            else
            {
                return Json(-1);
            }
        }
    }
}