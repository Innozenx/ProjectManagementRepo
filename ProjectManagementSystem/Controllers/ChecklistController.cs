using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;
using System.Data;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
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

        public ActionResult Dashboard()
        {
            var currentYear = DateTime.Now.Year;
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var currentWeek = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);

            // fetch raw data from the database
            var rawProjectsAndMilestones = (from m in db.MilestoneTbls
                                            join p in db.MainTables on m.main_id equals p.main_id
                                            join t in db.DetailsTbls on m.milestone_id equals t.milestone_id into tasks
                                            from task in tasks.DefaultIfEmpty()
                                            group task by new { p.project_title, m.milestone_name } into g
                                            select new
                                            {
                                                ProjectTitle = g.Key.project_title,
                                                MilestoneName = g.Key.milestone_name,
                                                Tasks = g.Where(t => t != null).Select(t => new
                                                {
                                                    t.task_start,
                                                    t.task_duration,
                                                    t.isCompleted
                                                }).ToList()
                                            }).ToList();

            var projectsAndMilestones = rawProjectsAndMilestones.Select(g => new ProjectMilestoneViewModel
            {
                ProjectTitle = g.ProjectTitle,
                MilestoneName = g.MilestoneName,
                Tasks = g.Tasks.Select(t => new TaskViewModel
                {
                    TaskStart = t.task_start,
                    Duration = t.task_duration ?? 0,
                    IsCompleted = t.isCompleted ?? false
                }).ToList(),
                EndDate = g.Tasks.Where(t => t.task_start.HasValue && t.task_duration > 0)
                                 .Max(t => t.task_start.Value.AddDays((double)t.task_duration)) // Calculate the end date
            }).ToList();

            var completedTasks = projectsAndMilestones.Sum(x => x.Tasks.Count(t => t.IsCompleted));
            var totalTasks = projectsAndMilestones.Sum(x => x.Tasks.Count());
            var pendingTasks = totalTasks - completedTasks;

            // Fetch upcoming tasks based on pending tasks and its end date
            var upcomingDeliverables = db.DetailsTbls
                .Where(t => t.isCompleted == false && t.task_start.HasValue && t.task_duration.HasValue)
                .Select(t => new
                {
                    Tasks = t.process_title,
                    TaskStart = t.task_start.Value,
                    TaskDuration = t.task_duration.Value
                })
                .ToList()
                .Select(t => new Deliverable
                {
                    Tasks = t.Tasks,
                    DueDate = t.TaskStart.AddDays(t.TaskDuration) // end date calculated here
                })
                .Where(d => d.DueDate >= DateTime.Now) // filter for upcoming
                .OrderBy(d => d.DueDate) // order by due date
                .ToList();

            var viewModel = new DashboardViewModel
            {
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                TotalTasks = totalTasks,
                CurrentWeek = currentWeek,
                ProjectsMilestones = projectsAndMilestones,
                UniqueMilestoneNames = projectsAndMilestones.Select(m => m.MilestoneName).Distinct().ToList(),
                ProjectTitles = projectsAndMilestones.Select(pm => pm.ProjectTitle).Distinct().ToList(),
                UpcomingDeliverables = upcomingDeliverables 
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

            var milestones = db.MainTables
                .Where(m => m.main_id == id)
                .Select(m => new ProjectMilestoneViewModel
                {
                    ProjectTitle = m.project_title,
                    StartDate = m.project_start,
                    EndDate = m.project_end,
                    Duration = m.duration ?? 0,
                    ProjectYear = m.year ?? 0,
                    Division = m.division,
                    Category = m.category,
                    ProjectOwner = m.project_owner,
                    MainId = m.main_id
                })
                .FirstOrDefault();

            if (milestones == null)
            {
                return HttpNotFound("No milestones found for the specified project.");
            }

            var viewModel = new ProjectMilestoneViewModel
            {
                ProjectTitle = milestones.ProjectTitle,
                StartDate = milestones.ProjectStart,
                EndDate = milestones.ProjectEnd,
                Duration = milestones.Duration,
                ProjectYear = milestones.ProjectYear,
                Division = milestones.Division,
                Category = milestones.Category,
                ProjectOwner = milestones.ProjectOwner
                
            };

            return View(viewModel);
        }



        public JsonResult getGanttData(int week, string title, string detailsId)
        {
   
            if (!int.TryParse(detailsId, out int parentId))
            {
                return Json(new { error = "Invalid parent ID format" }, JsonRequestBehavior.AllowGet);
            }

            var currentYear = DateTime.Now.Year;

            // calculate date range for the selected week
            var startDate = DateTime.Now.AddDays(7 * (week - 1));
            var endDate = DateTime.Now.AddDays(7 * week);

            // fetch tasks from DetailsTbl based on main_id (parentId), title, and week
            var tasks = db.DetailsTbls
                .Where(x => x.task_start.HasValue &&
                            x.task_start.Value.Year == currentYear &&
                            x.task_start <= endDate &&
                            x.task_start >= startDate &&
                            x.process_title.Equals(title) &&
                            x.parent == parentId) 
                .OrderBy(x => x.milestone_id)
                .ToList();


            var data = tasks.Select(x => new
            {
                id = x.details_id,
                start_date = x.task_start.HasValue ? x.task_start.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy"),
                duration = x.task_duration ?? 0,
                text = x.process_title,
                parent = x.parent,
                color = GetTaskColor(x),
                unscheduled = x.isUnscheduled,
                completed = x.isCompleted
            }).ToArray();

            var jsonData = new
            {
                tasks = data,
                links = new object[] { } 
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }


        //troy changes
        //public JsonResult getGanttData(int id)
        //{
        //    var ganttData = db.DetailsTbls
        //        .Where(t => t.details_id == id)
        //        .Select(t => new
        //        {
        //            id = t.details_id,
        //            text = t.process_title,
        //            start_date = t.task_start/*.ToString("yyyy-MM-dd HH:mm")*/,
        //            duration = t.task_duration,
        //            parent = t.parent
        //        }).ToList();

        //    return Json(ganttData, JsonRequestBehavior.AllowGet);
        //}
        private string GetTaskColor(DetailsTbl task)
        {
            if (task.task_start == null)
                return "white"; // default if task hasn't started yet

            DateTime startDate = task.task_start.Value;

            if (DateTime.Now < startDate)
                return "white"; // future task
            else if (task.isCompleted.GetValueOrDefault(false))
                return "green"; // Completed task
            else if (DateTime.Now <= startDate.AddDays(task.task_duration ?? 0) && DateTime.Now > startDate)
                return "blue"; // ongoing task
            else
                return "red"; // overdue task
        }



        //public JsonResult getGanttData(int week, string title, string projectId)
        //{

        //    List<ChecklistTable> checklist = new List<ChecklistTable>();
        //    Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

        //    var currentYear = DateTime.Now.Year;

        //    checklist = db.ChecklistTables.Where(x => x.startWeek <= week && x.endWeek >= week && x.ofYear == currentYear && x.title.Equals(title) && x.projectId.Equals(projectId)).OrderBy(x => x.sequenceId).ToList();
        //    checklist.AddRange(db.ChecklistTables.Where(x => x.localId >= 1 && x.localId <= 5).ToList());

        //    var data = checklist.Select(x => new
        //    {

        //        id = x.id,
        //        start_date = x.dateInitial != null
        //            ? x.dateInitial.Value.ToString("yyyy-MM-dd")
        //            : DateTime.Now.ToString("yyyy-MM-dd"),
        //        color = x.dateInitial != null
        //            ? DateTime.Now < x.dateInitial ? "black" : x.status == "completed" ? "green" : DateTime.Now <= DateTime.Parse(x.dateInitial.ToString()).AddDays(x.duration) && DateTime.Now > x.dateInitial ? "orange" : "red"
        //            : "white",
        //        duration = x.duration,
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


        //public JsonResult getGanttData(int week, string title, string projectId)
        //{
        //    List<DetailsTbl> details = new List<DetailsTbl>();
        //    Calendar calendar = CultureInfo.InvariantCulture.Calendar;
        //    var currentYear = DateTime.Now.Year;

        //    details = db.DetailsTbls
        //        .Where(x => x.task_start <= week && x.task_duration >= week && x.task_duration == currentYear && x.process_title.Equals(title) && x.details_id.Equals(projectId))
        //        .OrderBy(x => x.sequenceId)
        //        .ToList();

        //    var data = details.Select(x => new
        //    {
        //        id = x.details_id, // use detailsID
        //        start_date = x.task_start != null
        //            ? x.task_start.Value.ToString("yyyy-MM-dd")
        //            : DateTime.Now.ToString("yyyy-MM-dd"),
        //        duration = x.task_duration
        //        color = x.task_start != null
        //            ? DateTime.Now < x.task_start ? "black" : x.isCompleted ? "green" : DateTime.Now <= x.task_start.Value.AddDays(x.duration) && DateTime.Now > x.task_start ? "orange" : "red"
        //            : "white",
        //        text = x.process_title,
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


        public ActionResult Index()
        {
            return View();
        }


        public ActionResult AddProject()
        { 

            //List<WeeklyChecklistTable> listWeekly = db.WeeklyChecklistTables.ToList();
            //return View(listWeekly);

            List<MainTable> listProjects = db.MainTables.ToList();
            return View(listProjects);
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

                        try
                        {
                            exportList.Add(csv.GetRecord<exportCSV>());
                        }
                        catch (Exception ex)
                        {
                            return Json(new { message = "Error parsing CSV: " + ex.Message, status = false }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var getProject = exportList.GroupBy(x => x.ProjectTitle).Select(x => x.First()).FirstOrDefault();

                            if (getProject == null)
                            {
                                throw new Exception("No valid project data found.");
                            }

                            string dateFormat = "dd/MM/yyyy";

                            if (string.IsNullOrWhiteSpace(getProject.projectStart) ||
                                 string.IsNullOrWhiteSpace(getProject.projectEnd) ||
                                 getProject.ProjectDuration <= 0 ||
                                 getProject.ProjectYear <= 0)
                            {
                                throw new Exception("Start date, end date, duration, and year cannot be null or empty.");
                            }

                            var addWeeklyChecklist = new MainTable
                            {
                                project_title = getProject.ProjectTitle,
                                project_start = DateTime.ParseExact(getProject.projectStart, dateFormat, CultureInfo.InvariantCulture),
                                project_end = DateTime.ParseExact(getProject.projectEnd, dateFormat, CultureInfo.InvariantCulture),
                                duration = getProject.ProjectDuration,
                                year = getProject.ProjectYear,
                                division = getProject.division,
                                category = getProject.category,
                                project_owner = getProject.projectOwner
                            };

                            db.MainTables.Add(addWeeklyChecklist);
                            db.SaveChanges(); 

                            var milestones = exportList
                                .Where(x => x.ProjectTitle == getProject.ProjectTitle)
                                .GroupBy(x => x.MilestoneName)
                                .Select(x => x.First())
                                .ToList();

                            List<MilestoneTbl> milestoneList = new List<MilestoneTbl>();

                            foreach (var content in milestones)
                            {
                                var addMilestone = new MilestoneTbl
                                {
                                    milestone_name = content.MilestoneName,
                                    main_id = addWeeklyChecklist.main_id, 
                                    created_date = DateTime.Now,
                                    milestone_position = content.Sequence
                                };
                                milestoneList.Add(addMilestone);
                            }
                            db.MilestoneTbls.AddRange(milestoneList);
                            db.SaveChanges(); 

                            List<DetailsTbl> detailList = new List<DetailsTbl>();
                            foreach (var milestone in milestoneList)
                            {
                                var groupedTasks = exportList
                                    .Where(x => x.MilestoneName == milestone.milestone_name)
                                    .ToList();

                                foreach (var taskGroup in groupedTasks)
                                {
                                    DateTime taskStartDate = DateTime.ParseExact(taskGroup.TaskStart, dateFormat, CultureInfo.InvariantCulture);
                                    int taskDuration = taskGroup.task_duration;
                                    DateTime taskEndDate = taskStartDate.AddDays(taskDuration);

                                    var subtask = false; // subtask
                                    int? parentId = null; // parent id
                                    var _parentID = taskGroup.Parent;

                                    var isSubtask = exportList.Any(x => x.id == _parentID);
                                    if (isSubtask)
                                    {
                                        subtask = true;
                                        var processTitle = exportList.FirstOrDefault(x => x.id == _parentID)?.ProcessTitle;
                                        parentId = db.DetailsTbls.OrderByDescending(x => x.details_id)
                                            .FirstOrDefault(x => x.process_title == processTitle)?.details_id;
                                    }

                                    var addTask = new DetailsTbl
                                    {
                                        milestone_id = milestone.milestone_id,
                                        process_title = taskGroup.ProcessTitle,
                                        task_start = taskStartDate,
                                        task_duration = taskGroup.task_duration,
                                        source = taskGroup.Source,
                                        target = taskGroup.Target,
                                        parent = parentId,
                                        created_date = DateTime.Now.ToLocalTime(),
                                        IsSubtask = subtask,
                                    };

                                    detailList.Add(addTask); 
                                }
                            }

                            db.DetailsTbls.AddRange(detailList);
                            db.SaveChanges(); 

                            transaction.Commit();
                            message = "Project added successfully!";
                            status = true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            message = "An error occurred while saving: " + (ex.InnerException?.Message ?? ex.Message);
                        }
                    }

                    return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                message = "An error occurred: " + (ex.InnerException?.Message ?? ex.Message);
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


