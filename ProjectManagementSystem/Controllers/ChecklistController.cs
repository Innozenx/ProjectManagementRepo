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
using CsvHelper.TypeConversion;

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

        //public ActionResult Dashboard()
        //{
        //    var currentYear = DateTime.Now.Year;
        //    var calendar = CultureInfo.InvariantCulture.Calendar;
        //    var currentWeek = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);

        //    var checklistTables = db.WeeklyChecklistTables
        //        .Where(x => x.weeklyInYear == currentYear && x.isCancelled == false)
        //        .ToList();

        //    // fetch projects and their associated milestones
        //    var projectsAndMilestones = (from m in db.MilestoneTbls
        //                                 join p in db.MainTables on m.main_id equals p.main_id
        //                                 select new ProjectMilestoneViewModel
        //                                 {
        //                                     ProjectTitle = p.project_title,
        //                                     MilestoneName = m.milestone_name,
        //                                     // You can also calculate EndDate here if needed
        //                                 }).ToList();

        //    var tasks = db.DetailsTbls
        //        .Select(t => new
        //        {
        //            t.process_title,
        //            t.task_start,
        //            t.duration,

        //    // calculate enddate by adding duration to task_start
        //    EndDate = t.task_start != null ? t.task_start.Value.AddDays(t.duration ?? 0) : (DateTime?)null
        //        }).ToList();

        //    var viewModel = new DashboardViewModel
        //    {
        //        CompletedTasks = db.DetailsTbls.Count(x => x.isCompleted == true),
        //        PendingTasks = db.DetailsTbls.Count(x => x.isCompleted == false),
        //        TotalTasks = db.DetailsTbls.Count(),
        //        CurrentWeek = currentWeek,
        //        ProjectsMilestones = projectsAndMilestones,
        //        Tasks = tasks
        //    };

        //    return View(viewModel);
        //}


            // datetime i-parse then convert to datetime sa db

        public ActionResult Dashboard()
        {
            var currentYear = DateTime.Now.Year;
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var currentWeek = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);


            var checklistTables = db.WeeklyChecklistTables
                .Where(x => x.weeklyInYear == currentYear && x.isCancelled == false)
                .ToList();

            var completedTasks = checklistTables.Count(x => x.isCompleted == true);
            var pendingTasks = checklistTables.Count(x => x.isCompleted == false);
            var totalTasks = checklistTables.Count();

            // fetch checklist data 
            var weeklyChecklists = checklistTables.Select(x => new WeeklyChecklist
            {
                weeklyID = x.weeklyID,
                weeklyTitle = x.weeklyTitle,
            }).ToList();

            string FormatEndDate(DateTime? endDate)
            {
                if (endDate.HasValue)
                {
                    var year = endDate.Value.Year % 100; // last two digits of the year
                    var week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(endDate.Value, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
                    return $"WW{year:D2}{week:D2}";
                }
                return "N/A";
            }

            // fetch projects and their associated milestones
            var projectsAndMilestones = (from m in db.MilestoneTbls
                                         join p in db.MainTables on m.milestone_id equals p.main_id
                                         select new ProjectMilestoneViewModel
                                         {
                                             ProjectTitle = p.project_title,
                                             MilestoneName = m.milestone_name,
                                            // EndDate = m.end_date
                                         }).ToList();


            projectsAndMilestones.ForEach(pm =>
            {
                pm.EndDateFormat = FormatEndDate(pm.EndDate);
            });

            var uniqueMilestoneNames = projectsAndMilestones
                .Select(m => m.MilestoneName)
                .ToList();

            var viewModel = new DashboardViewModel
            {
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                TotalTasks = totalTasks,
                CurrentWeek = currentWeek,
                ProjectsMilestones = projectsAndMilestones,
                UniqueMilestoneNames = uniqueMilestoneNames
            };

            return View(viewModel);
        }

        private List<string> GetUniqueMilestoneNames()
        {
            return db.MilestoneTbls.Select(m => m.milestone_name).Distinct().ToList();
        }


        public ActionResult weeklyMilestone(int id, string title, string projectId)
        {
            TempData["entry"] = id;
            TempData["title"] = title;
            TempData["project"] = projectId;

            return View();
        }

        //public JsonResult getGanttData(int week, string title, string projectId)
        //{
        //    List<DetailsTbl> details = new List<DetailsTbl>();
        //    Calendar Calendar = CultureInfo.InvariantCulture.Calendar;
        //    var currentYear = DateTime.Now.Year;

        //    details = db.DetailsTbls.Where(x => x.task_start <= week && x.duration >= week && x.year == currentYear && x.process_title.Equals(title) && x.details_id.Equals(projectId)).OrderBy(x => x.sequenceId).ToList();
        //    details.AddRange(db.DetailsTbl.Where(x => x.localId >= 1 && x.localId <= 5).ToList());

        //    var data = details.Select(x => new
        //    {
        //        id = x.id,
        //        start_date = x.dateInitial != null
        //            ? x.dateInitial.Value.ToString("yyyy-MM-dd")
        //            : DateTime.Now.ToString("yyyy-MM-dd"),
        //        duration = x.duration,
        //        end_date = x.dateInitial != null
        //            ? x.dateInitial.Value.AddDays(x.duration).ToString("yyyy-MM-dd")
        //            : DateTime.Now.AddDays(x.duration).ToString("yyyy-MM-dd"),
        //        color = x.dateInitial != null
        //            ? DateTime.Now < x.dateInitial ? "black" : x.status == "completed" ? "green" : DateTime.Now <= x.dateInitial.Value.AddDays(x.duration) && DateTime.Now > x.dateInitial ? "orange" : "red"
        //            : "white",
        //        text = x.text,
        //        parent = x.parent,
        //        target = x.target,
        //        source = x.source,
        //        type = x.type,
        //        unscheduled = x.isUnscheduled
        //    }).ToArray();

        //    var jsonData = new
        //    {
        //        tasks = data,
        //        links = data
        //    };

        //    return new JsonResult { Data = jsonData, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        //}



        public JsonResult getGanttData(int week, string title, string projectId)
        {

            List<ChecklistTable> checklist = new List<ChecklistTable>();
            Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

            var currentYear = DateTime.Now.Year;

            checklist = db.ChecklistTables.Where(x => x.startWeek <= week && x.endWeek >= week && x.ofYear == currentYear && x.title.Equals(title) && x.projectId.Equals(projectId)).OrderBy(x => x.sequenceId).ToList();
            checklist.AddRange(db.ChecklistTables.Where(x => x.localId >= 1 && x.localId <= 5).ToList());

            var data = checklist.Select(x => new
            {

                id = x.id,
                start_date = x.dateInitial != null
                    ? x.dateInitial.Value.ToString("yyyy-MM-dd")
                    : DateTime.Now.ToString("yyyy-MM-dd"),
                color = x.dateInitial != null
                    ? DateTime.Now < x.dateInitial ? "black" : x.status == "completed" ? "green" : DateTime.Now <= DateTime.Parse(x.dateInitial.ToString()).AddDays(x.duration) && DateTime.Now > x.dateInitial ? "orange" : "red"
                    : "white",
                duration = x.duration,
                text = x.text,
                parent = x.parent,
                target = x.target,
                source = x.source,
                type = x.type,
                unscheduled = x.isUnscheduled
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

            List<WeeklyChecklistTable> listWeekly = db.WeeklyChecklistTables.ToList();
            return View(listWeekly);
        }

        public class NullableInt32Converter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                if (int.TryParse(text, out int result))
                    return result;

                return null;
            }
        }

        [HttpPost]
        public JsonResult AddProjectUpload()
        {
            var message = "";
            var status = false;


            var attachment = System.Web.HttpContext.Current.Request.Files["pmcsv"];
            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "File is empty", status = false }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var reader = new StreamReader(attachment.InputStream))
                using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture)))
                {
                    csv.Context.RegisterClassMap<ProjectMap>();
                    var exportList = new List<exportCSV>();
                    var isHeader = true;


                    while (csv.Read())
                    {
                        if (isHeader)
                        {
                            csv.ReadHeader();
                            isHeader = false;
                            continue;
                        }

                        exportList.Add(csv.GetRecord<exportCSV>());
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            #region Main Table
                            var getProject = exportList.GroupBy(x => x.ProjectTitle).Select(x => x.First()).FirstOrDefault();

                            if (getProject == null)
                            {
                                throw new Exception("No valid project data found.");
                            }

                            var addWeeklyChecklist = new MainTable
                            {
                                project_title = getProject.ProjectTitle,
                                project_start = Convert.ToDateTime(getProject.projectStart),
                                project_end = Convert.ToDateTime(getProject.projectEnd),
                                duration = getProject.Duration,
                                year = getProject.year,
                                division = getProject.division,
                                category = getProject.category,
                                project_owner = getProject.projectOwner
                            };
                            var _mainTableId = db.MainTables.Add(addWeeklyChecklist);
                            db.SaveChanges();
                            #endregion

                            #region Milestones
                            var milestones = exportList
                                .Where(x => x.ProjectTitle == getProject.ProjectTitle)
                                .GroupBy(x => x.MilestoneName)
                                .Select(x => x.First())
                                .ToList();

                            List<MilestoneTbl> milestoneList = new List<MilestoneTbl>();

                            // iterate milestones and create record
                            foreach (var content in milestones)
                            {
                               
                                var addMilestone = new MilestoneTbl
                                {
                                    milestone_name = content.MilestoneName,
                                    main_id = _mainTableId.main_id,
                                    created_date = DateTime.Now,
                                    milestone_position = content.Sequence
                                };
                                milestoneList.Add(addMilestone);
                            }
                            db.MilestoneTbls.AddRange(milestoneList);
                            db.SaveChanges();
                            #endregion

                            #region Details and Subtasks 
                            List<DetailsTbl> detailList = new List<DetailsTbl>();
                            foreach (var milestone in milestoneList) 
                            {
                                var groupedTasks = exportList
                                    .Where(x => x.MilestoneName == milestone.milestone_name)
                                    .ToList();


                                foreach (var taskGroup in groupedTasks)
                                {

                                    // calculate task end_date based on task_start and duration
                                    DateTime taskStartDate = Convert.ToDateTime(taskGroup.TaskStart);
                                    int taskDuration = taskGroup.task_duration;
                                    DateTime taskEnddate = taskStartDate.AddDays(taskDuration);

                                    var subtask = false; // subtask
                                    int? parentId = null; // parent id
                                    var _parentID = taskGroup.Parent; // get parent id

                                    // check if subtask
                                    var isSubtask = exportList.Any(x => x.id == _parentID);

                                    // if subtask, get process title and parent id 
                                    if (isSubtask)
                                    {
                                        subtask = true;
                                        var processTitle = exportList.FirstOrDefault(x => x.id == _parentID).ProcessTitle;
                                        parentId = db.DetailsTbls.OrderByDescending(x => x.details_id).FirstOrDefault(x => x.process_title == processTitle).details_id;
                                    }

                                    // create new task and set details
                                    var addTask = new DetailsTbl
                                    {
                                        milestone_id = milestone.milestone_id,
                                        process_title = taskGroup.ProcessTitle,
                                        task_start = Convert.ToDateTime(taskGroup.TaskStart), 
                                        //task_end = taskGroup.TaskEnd,
                                        task_duration = taskGroup.task_duration,
                                        source = taskGroup.Source,
                                        target = taskGroup.Target,
                                        parent = parentId,
                                        created_date = DateTime.Now.ToLocalTime(), 
                                        IsSubtask = subtask,

                                    };

                                    db.DetailsTbls.Add(addTask);
                                    db.SaveChanges();

                                }
                            }
                            #endregion

                            transaction.Commit();
                           
                            message = "Project added successfully!";
                            status = true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            message = "An error occured while saving: " + ex.Message;
                        }

                    }

                    return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                message = "An error occurred: " + ex.Message;
                return Json(new { message = message, status = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult SaveStatus()
        {
            var message = "";
            var status = false;

            var attachment = System.Web.HttpContext.Current.Request.Files["pmcsv"];
          

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "No file uploaded or file is empty", status = false });
            }

            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Cancellation(string cancelOpt)
        {
            var message = "";
            var status = false;

            try
            {
                var toCancel = db.WeeklyChecklistTables.Where(x => x.weeklyTitle == cancelOpt).First();

                toCancel.isCancelled = true;

                db.SaveChanges();

                message = "success";
                status = true;
            }
            catch (Exception)
            {
                message = "failed";
            }

            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Checklist()
        {
            return View();
        }



        //public JsonResult WeeklyStatusUpdate()
        //{
        //    return Json();
        //}
        

    }
}


