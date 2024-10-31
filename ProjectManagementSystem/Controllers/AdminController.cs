using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ProjectManagementSystem.CustomAttributes;

namespace ProjectManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();
        CMIdentityDBEntities cmdb = new CMIdentityDBEntities();

        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        [CustomAuthorize(Roles = "PMS_Developer")]
        public ActionResult Register()
        {
            return View();
        }

        public JsonResult Register_Project(string name)
        {
            var message = "";
            var status = false;

            if (string.IsNullOrWhiteSpace(name))
            {
                message = "Project name cannot be empty.";
                return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                ProjectRegister details = new ProjectRegister
                {
                    ProjectName = name,
                    DateRegistered = DateTime.Now,
                    Division = "ITS",
                    RegisteredBy = "ME",
                    IsCompleted = false,
                    Unregistered = false, 
                    IsFileUploaded = false,
                    Year = DateTime.Now.Year
                };

                var insDeets = new RegistrationTbl
                {
                    project_name = details.ProjectName,
                    date_registered = details.DateRegistered,
                    division = details.Division,
                    registered_by = details.RegisteredBy,
                    is_completed = details.IsCompleted,
                    unregistered = details.Unregistered,
                    is_file_uploaded = details.IsFileUploaded,
                    year = details.Year
                };

                db.RegistrationTbls.Add(insDeets);

                Activity_Log logs = new Activity_Log
                {
                    username = User.Identity.Name,
                    datetime_performed = DateTime.Now,
                    action_level = 5,
                    action = "Project Registration",
                    description = details.ProjectName + " Project Registered by: " + details.RegisteredBy + " For Year: " + details.Year,
                    department = "ITS",
                    division = "SDD"
                };

                db.Activity_Log.Add(logs);

                db.SaveChanges();

                message = "Project name has been successfully registered!";
                status = true;
            }
            catch (Exception e)
            {
                message = "Project name already exists.";
                status = false;
                Debug.WriteLine(e.Message);
            }

            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }

        [CustomAuthorize(Roles = "PMS_Developer")]
        public ActionResult RegisterUserView()
        {
            List<UserType> listTypes = db.UserTypes.ToList();
            return View(listTypes);
        }

        public JsonResult RegisterUser(string email, string type)
        {
            var message = "";
            var status = false;

            try
            {
                AdminList admin = new AdminList()
                {
                    user_level = type,
                    user_email = email,
                    is_active = true
                };

                db.AdminLists.Add(admin);

                Activity_Log logs = new Activity_Log
                {
                    username = User.Identity.Name,
                    datetime_performed = DateTime.Now,
                    action_level = 5,
                    action = "User Registration",
                    description = "User: " + email + "-" + type + " Registered by: " + User.Identity.Name,
                    department = "ITS",
                    division = "SDD"
                };

                db.Activity_Log.Add(logs);

                db.SaveChanges();

                message = "User Registration Successful";
                status = true;
            }
            catch (Exception e)
            {
                message = "User Registration Failed";
                status = false;
            }



            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }

    }
}