using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class ChecklistController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        public ActionResult Checklist()
        {
            List<ChecklistTable> checklist = new List<ChecklistTable>();

            checklist = db.ChecklistTables.ToList();

            return View(checklist);
        }
    }

    
}
