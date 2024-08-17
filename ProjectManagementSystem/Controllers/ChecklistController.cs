using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Threading.Tasks;

namespace ProjectManagementSystem.Controllers
{
    public class ChecklistController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        public ActionResult WeeklyChecklist()
        {
            List<WeeklyChecklistTable> checklist = new List<WeeklyChecklistTable>();
            Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

            var currentYear = DateTime.Now.Year;
            checklist = db.WeeklyChecklistTables.Where(x => x.weeklyInYear == currentYear).ToList();

            return View(checklist);
        }

        public ActionResult Dashboard()
        {
            var currentYear = DateTime.Now.Year;
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var currentWeek = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);

            var checklist = db.WeeklyChecklistTables.Where(x => x.weeklyInYear == currentYear).ToList();

            var completedTasks = checklist.Count(x => x.isCompleted == true);
            var pendingTasks = checklist.Count(x => x.isCompleted == false);
            var totalTasks = checklist.Count();

            var model = new DashboardViewModel
            {
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                TotalTasks = totalTasks,
                CurrentWeek = currentWeek,
                WeeklyChecklists = checklist
            };

            return View(model);
        }




        public ActionResult weeklyMilestone(int id, string title)
        {
            var entry = id;
            var entryTitle = title;
            TempData["entry"] = entry;
            TempData["title"] = title;

            return View();
        }

        public JsonResult getGanttData(int week, string title)
        {

            List<ChecklistTable> checklist = new List<ChecklistTable>();
            Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

            var currentYear = DateTime.Now.Year;

            checklist = db.ChecklistTables.Where(x => x.startWeek <= week && x.endWeek >= week && x.ofYear == currentYear && x.title.Equals(title)).ToList();

            var data = checklist.Select(x => new
            {
                id = x.id,
                start_date = x.dateInitial.Value.ToString("yyyy-MM-dd"),
                color = DateTime.Now < x.dateInitial ? "black" : x.status == "completed" ? "green" : DateTime.Now <= DateTime.Parse(x.dateInitial.ToString()).AddDays(x.duration) && DateTime.Now > x.dateInitial ? "orange" : "red",
                duration = x.duration,
                text = x.text,
                parent = x.parent,
                target = x.target,
                source = x.source,
                type = x.type
            }).ToArray();

            var jsonData = new
            {
                tasks = data,

                links = data
            };

            return new JsonResult { Data = jsonData, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult AddProject()
        {
            return View();
        }


        [HttpPost]
        public JsonResult AddProjectUpload()
        {
            Calendar cal = new CultureInfo("en-US").Calendar;

            var message = "";
            var status = false;
            int weeklyCtr = 0;

            var attachment = System.Web.HttpContext.Current.Request.Files["pmcsv"];

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "No file uploaded or file is empty", status = false });
            }

            try
            {
                using (var reader = new StreamReader(attachment.InputStream))
                using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture)))
                
                {
                    csv.Context.RegisterClassMap<ProjectMap>();
                    var export = new List<exportCSV>();
                    var isHeader = true;

                    while (csv.Read())
                    {
                        if (isHeader)
                        {
                            csv.ReadHeader();
                            isHeader = false;
                            continue;
                        }

                        export.Add(csv.GetRecord<exportCSV>());
                    }

                    foreach (var content in export)
                    {
                        if (content == null) continue;

                        if(weeklyCtr < 1)
                        {
                            var addWeeklyChecklist = new WeeklyChecklistTable
                            {
                                weeklyTitle = content.projectTitle,
                                weeklyDuration = content.projectDuration.ToString(),
                                weeklyStart = content.projectStart,
                                weeklyTarget = content.projectEnd,
                                weeklyInYear = content.projectYear,
                                subMain = null,
                                subSub = null,
                                division = content.division,
                                category = content.category,
                                inWeek = cal.GetWeekOfYear(content.projectStart, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
                                isCancelled = false,
                                isDelayed = false,
                                WeeklyMonth = null,
                                WeeklyDay = null,
                                isCompleted = false,
                                weeklyOwner = content.owner,
                                weeklyID = content.id
                            };

                            db.WeeklyChecklistTables.Add(addWeeklyChecklist);
                            db.SaveChanges();
                            weeklyCtr = 1;
                        }

                        var add = new ChecklistTable
                        {
                            text = content.processTitle,
                            duration = content.duration,
                            start_date = content.projectStart,
                            end_date = content.projectEnd,
                            parent = content.parent,
                            projectReference = null,
                            source = null,
                            target = null,
                            type = "test",
                            ofYear = content.projectYear,
                            startWeek = cal.GetWeekOfYear(content.projectStart, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
                            endWeek = cal.GetWeekOfYear(content.projectEnd, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
                            title = content.projectTitle,
                            projectType = "n/a",
                            status = "active",
                            color = "black",
                            details = "n/a",
                            dateInitial = content.start,
                            dateFinished = null,
                            project_name = content.projectTitle,
                            project_owner = content.owner,
                            id = content.id

                        };

                        db.ChecklistTables.Add(add);
                        db.SaveChanges();

                        message = "Added!";
                        status = true;
                    }
                }
            }
            catch (Exception e)
            {
                message = "An error occurred: " + e.Message;
            }

            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult WeeklyStatus()
        {
            return View();
        }

       
        //public JsonResult WeeklyStatusUpdate()
        //{
        //    return Json();
        //}
    }
}



