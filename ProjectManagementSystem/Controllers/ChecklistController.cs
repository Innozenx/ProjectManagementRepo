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

namespace ProjectManagementSystem.Controllers
{
    public class ChecklistController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        //public ActionResult Checklist()
        //{
        //    List<ChecklistTable> checklist = new List<ChecklistTable>();
        //    Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

        //    var currentWeek = Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
        //    var currentYear = DateTime.Now.Year;
        //    checklist = db.ChecklistTables.ToList();

        //    return View(checklist);
        //}

        //[System.Web.Http.HttpPost]
        //public JsonResult checkUncheck(int workDay, int workWeek, bool check, int checklist, int workYear)
        //{

        //    var dbQuery = db.ChecklistTables.Where(x => x.ofYear == workYear).Where(x => x.inWeek == workWeek).Where(x => x.checkListId == checklist).SingleOrDefault();

        //    if(check == true)
        //    {
        //        switch (workDay)
        //        {
        //            case 0:
        //                dbQuery.isChecked0 = true;
        //                break;

        //            case 1:
        //                dbQuery.isChecked1 = true;
        //                break;

        //            case 2:
        //                dbQuery.isChecked2 = true;
        //                break;

        //            case 3:
        //                dbQuery.isChecked3 = true;
        //                break;

        //            case 4:
        //                dbQuery.isChecked4 = true;
        //                break;

        //            case 5:
        //                dbQuery.isChecked5 = true;
        //                break;

        //            case 6:
        //                dbQuery.isChecked6 = true;
        //                break;

        //        }
        //    }

        //    else
        //    {
        //        switch (workDay)
        //        {
        //            case 0:
        //                dbQuery.isChecked0 = false;
        //                break;

        //            case 1:
        //                dbQuery.isChecked1 = false;
        //                break;

        //            case 2:
        //                dbQuery.isChecked2 = false;
        //                break;

        //            case 3:
        //                dbQuery.isChecked3 = false;
        //                break;

        //            case 4:
        //                dbQuery.isChecked4 = false;
        //                break;

        //            case 5:
        //                dbQuery.isChecked5 = false;
        //                break;

        //            case 6:
        //                dbQuery.isChecked6 = false;
        //                break;

        //        }
        //    }

        //    var res = "";
        //    try
        //    {
        //        db.SaveChanges();
        //        res = "success";
        //    }

        //    catch{
        //        res = "failed";
        //    }


        //    return Json(new { res = res }, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult WeeklyChecklist()
        {
            List<WeeklyChecklistTable> checklist = new List<WeeklyChecklistTable>();
            Calendar Calendar = CultureInfo.InvariantCulture.Calendar;


            var currentYear = DateTime.Now.Year;
            checklist = db.WeeklyChecklistTables.Where(x => x.weeklyInYear == currentYear).ToList();

            return View(checklist);
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

            //if (db.ChecklistTables.Where(x => x.startWeek <= week && x.endWeek >= week && x.ofYear == currentYear && x.title.Equals(title)).Any())
            //{
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


            //}
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


        [HttpPost] //not yet done hehe! :D
        public JsonResult AddProjectUpload()
        {
            var message = "";
            var status = false;

            var attachment = System.Web.HttpContext.Current.Request.Files["pmcsv"];

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(null);
            }

            //var csvReader = new StreamReader(attachment.InputStream);
            //var exportCsvList = new List<exportCSV>();
            //string inputDataRead;
            //var values = new List<String>();

            //while((inputDataRead = csvReader.ReadLine()) != null)
            //{
            //    values.Add(inputDataRead.Trim().Replace(" ", "").Replace(",", " "));
            //}

            //values.Remove(values[0]);
            //values.Remove(values[values.Count - 1]);

            //foreach(var value in values)
            //{
            //    var valueContent = value.Split(' ');
            //    var valueArr = value[0].ToString();
            //}

            using (var reader = new StreamReader(attachment.InputStream))
            //using (var reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "PMExport.CSV")))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                csv.Context.RegisterClassMap<ProjectMap>();
                List<exportCSV> export = new List<exportCSV>();
                var isHeader = true;
                int i = 0;

                while (csv.Read())
                {
                    if (isHeader)
                    {
                        csv.ReadHeader();
                        isHeader = false;
                        continue;
                    }

                    else
                    {
                        export.Add(csv.GetRecord<exportCSV>());
                    }
                }

                foreach (var content in export)
                {
                    //Console.WriteLine($"{content.projectTitle}");
                    try
                    {
                        if(i == 0)
                        {
                            var addWeeklyChecklist = new WeeklyChecklistTable
                            {
                                weeklyTitle = content.processTitle,
                                weeklyDuration = content.projectDuration.ToString(),
                                weeklyStart = content.projectStart,
                                weeklyTarget = content.projectStart,
                                weeklyInYear = content.projectYear,
                                subMain = null,
                                subSub = null,
                                division = content.division,
                                category = content.category,
                                inWeek = null,
                                isCancelled = false,
                                isDelayed = false,
                                WeeklyMonth = null,
                                WeeklyDay = null,
                                isCompleted = false,

                            };

                            db.WeeklyChecklistTables.Add(addWeeklyChecklist);
                            db.SaveChanges();
                            i++;
                        }

                        var add = new ChecklistTable
                        {
                            text = content.processTitle,
                            duration = content.duration,
                            start_date = content.projectStart,
                            parent = null,
                            projectReference = null,
                            source = null,
                            target = null,
                            type = "test",
                            ofYear = content.projectYear,
                            startWeek = 1,
                            endWeek = 2,
                            title = content.processTitle,
                            projectType = "n/a",
                            status = "active",
                            color = "black",
                            details = "n/a",
                            dateInitial = content.start,
                            dateFinished = null,
                            project_name = "n/a",
                            project_owner = "tester",
                            
                        };
                        db.ChecklistTables.Add(add);
                        db.SaveChanges();

                        message = "Added!";
                        status = true;

                    }
                    catch (Exception e)
                    {
                        message = e.Message;
                    }

                }
            }

            
            return Json(new { message = message, status = status },
                JsonRequestBehavior.AllowGet);
        }
    }
}



