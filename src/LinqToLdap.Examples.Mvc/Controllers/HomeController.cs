﻿using System.Web.Mvc;

namespace LinqToLdap.Examples.Mvc.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult About()
        {
            return View();
        }

        public ActionResult ServerInfo()
        {
            return View();
        }

        public ActionResult Users()
        {
            return View();
        }
    }
}
