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
    public class WMSOptionListController : Controller
    {
        // GET: WMSOptionList
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Selection Criteria";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            if (Session["Users"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpPost]
        public ActionResult Next(string[] wmsOption)
        {
            if (Session["Users"] != null)
            {
                if (wmsOption != null)
                {
                    string option = wmsOption[0].ToString();
                    if(option.Equals("1"))
                    {
                        return Json(1);
                    }
                    else
                    {
                        return Json(2);
                    }                    
                }
                else
                {
                    return Json(3);
                }
            }
            else
            {
                return Json(-1);
            }
        }
    }
}