using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProjectManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        public JsonResult Register_Project(string name)
        {
            var message = "";
            var status = false;


            try
            {
                projectRegister details = new projectRegister
                {
                    project_name = name,
                    date_registered = DateTime.Now,
                    division = "ITS",
                    registered_by = "ME",
                    is_completed = false,
                    unregistered = false,

                    year = DateTime.Now.Year
                };

                var insDeets = new RegistrationTbl
                {
                    project_name = details.project_name,
                    date_registered = details.date_registered,
                    division = details.division,
                    registered_by = details.registered_by,
                    is_completed = details.is_completed,
                    unregistered = details.unregistered,
                    year = details.year
                };

                db.RegistrationTbls.Add(insDeets);
                db.SaveChanges();

                message = "Registration Successful";
                status = true;
            }

            catch (Exception e){
                message = "Registration Failed";
                status = false;
                Debug.WriteLine(e.Message);
            }
            

            
            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }
    }
}