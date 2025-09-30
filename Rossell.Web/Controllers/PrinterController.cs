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
    public class PrinterController : Controller
    {
        // GET: Printer
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString() + " :: Selection Criteria";
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            if (Session["Users"] != null)
            {
                using (ServiceBusinessLogic serviceBL = new ServiceBusinessLogic())
                {
                    var result = serviceBL.GetPrinterList();
                    if (result == null || result.Count <= 0)
                    {
                        Printer printer = new Printer();
                        printer.PrinterName = ConfigurationManager.AppSettings["PRINTERNAME"].ToString();
                        result.Add(printer);
                    }
                    Session["PRINTERLIST"] = result;
                    return View(result);
                }                
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpPost]
        public ActionResult Next(string[] printer)
        {
            if (Session["Users"] != null)
            {
                if (printer != null)
                {
                    //using (WebApiDeviceBusinessLogic webApiDeviceBL = new WebApiDeviceBusinessLogic())
                    //{
                    //    WebApiDevice webApiDevice = webApiDeviceBL.GetWebApiDevice(printer[0].ToString());
                    //    if (webApiDevice == null)
                    //    {
                    //        string name = ConfigurationManager.AppSettings["APPLICATION"].ToString();
                    //        string module = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();
                    //        string type = "PRINTER";
                    //        AuthTokenModel authToken = (AuthTokenModel)Session["AUTHTOKEN"];
                    //        string token = authToken.AuthToken;
                    //        token = token = token.Substring((token.IndexOf("==|") + 3), token.Length - token.IndexOf("==|") - 3);
                    //        token = token.Substring(token.Length - 36, 36);
                    //        long deviceID = webApiDeviceBL.SaveWebApiDevice(printer[0].ToString(), printer[0].ToString(), module, type, printer[0].ToString());
                    //        bool isUpdate = webApiDeviceBL.UpdateDeviceID(token, deviceID);
                    //    }
                    //    else
                    //    {
                    //        AuthTokenModel authToken = (AuthTokenModel)Session["AUTHTOKEN"];
                    //        string token = authToken.AuthToken;
                    //        token = token.Substring(token.Length - 36, 36);
                    //        bool isUpdate = webApiDeviceBL.UpdateDeviceID(token, webApiDevice.ID);
                    //    }
                    //}

                    Session["PRINTERNAME"] = printer[0].ToString();
                    return Json(1);
                }
                else
                {
                    return Json(2);
                }
            }
            else
            {
                return Json(-1);
            }
        }

        [HttpPost]
        public ActionResult Search(string searchPrinter)
        {
            if (Session["Users"] != null)
            {
                if (searchPrinter != null)
                {
                    List<Printer> printerList = (List<Printer>)Session["PRINTERLIST"];
                    List<Printer> searchPrinterList = printerList.Where(p => p.PrinterName.ToLower().Contains(searchPrinter.ToLower())).ToList();
                    var result = searchPrinterList;
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(2);
                }
            }
            else
            {
                return Json(-1);
            }
        }
    }
}