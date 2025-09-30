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
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            ViewBag.Title = ConfigurationManager.AppSettings["COMPANYNAME"].ToString() + " :: " + ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();
            ViewBag.CompanyName = ConfigurationManager.AppSettings["COMPANYNAME"].ToString();
            ViewBag.SystemName = ConfigurationManager.AppSettings["SYSTEMNAME"].ToString();

            return View();
        }

        [HttpPost]
        public JsonResult Login(User user)
        {
            using (ServiceBusinessLogic serviceBusinessLogic = new ServiceBusinessLogic())
            {
                LoginModel loginModal = new LoginModel();
                loginModal.UserName = user.userEmail;
                loginModal.Password = user.userPassword;
                AuthTokenModel authToken = serviceBusinessLogic.UserAuthentication(loginModal);
                if (authToken!=null && !authToken.AuthToken.Equals(""))
                {
                    Session.Add("AUTHTOKEN", authToken);
                    Session["Users"] = user;
                    return Json(true);
                }
                else
                {
                    return Json(false);
                }
            }
        }
    }
}