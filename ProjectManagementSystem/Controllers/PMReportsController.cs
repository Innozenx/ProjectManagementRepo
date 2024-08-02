using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class PMReportsController : Controller
    {
        private ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        // GET: PMReports
        public ActionResult Index()
        {
            
            return View(db.Reports.ToList());
        }

        [HttpGet]
        public ActionResult DisplayReport(int year, int quarter, string division)
        {
            List<Report> reportList = new List<Report>();
            List<PMReport> pmReport = new List<PMReport>();
            foreach(var item in pmReport)
            {
            }
            reportList = db.Reports.Where(x => x.year == year).Where(x => x.inQuarter == quarter).Where(x => x.division == division).ToList();
            return View(reportList);
        }

    }
}
