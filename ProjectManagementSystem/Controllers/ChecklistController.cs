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
using System.Diagnostics;
using Microsoft.AspNet.Identity;
using ProjectManagementSystem.CustomAttributes;

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
            var UserId = User.Identity.GetUserId();

            var rawProjectsAndMilestones = (from m in db.MilestoneTbls
                                            join p in db.MainTables on m.main_id equals p.main_id
                                            join t in db.DetailsTbls on m.milestone_id equals t.milestone_id into tasks
                                            from task in tasks.DefaultIfEmpty()
                                            group task by new { p.main_id, p.project_title, m.milestone_name, m.milestone_position, p.user_id }
                                            into g
                                            select new
                                            {
                                                MainId = g.Key.main_id,
                                                UserId = g.Key.user_id,
                                                ProjectTitle = g.Key.project_title,
                                                MilestoneName = g.Key.milestone_name,
                                                MilestonePosition = g.Key.milestone_position,
                                                Tasks = g.Where(t => t != null).Select(t => new
                                                {
                                                    t.task_start,
                                                    t.task_duration,
                                                    t.isCompleted
                                                }).ToList()
                                            }).ToList();

            var projectsAndMilestones = rawProjectsAndMilestones
                .Where(g => g.UserId == UserId)
                .OrderBy(g => g.MilestonePosition)
                .Select(g => new ProjectMilestoneViewModel
                {
                    MainId = g.MainId,
                    ProjectTitle = g.ProjectTitle,
                    MilestoneName = g.MilestoneName,
                    Tasks = g.Tasks.Select(t => new TaskViewModel
                    {
                        TaskStart = t.task_start,
                        Duration = t.task_duration ?? 0,
                        IsCompleted = t.isCompleted ?? false
                    }).ToList(),
                    EndDate = g.Tasks.Any(t => t.task_start.HasValue && t.task_duration > 0)
                             ? g.Tasks.Where(t => t.task_start.HasValue && t.task_duration > 0)
                                      .Max(t => t.task_start.Value.AddDays((double)t.task_duration))
                             : (DateTime?)null
                }).ToList();

            var completedTasks = projectsAndMilestones.Sum(x => x.Tasks.Count(t => t.IsCompleted));
            var totalTasks = projectsAndMilestones.Sum(x => x.Tasks.Count());
            var pendingTasks = totalTasks - completedTasks;

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
                    DueDate = t.TaskStart.AddDays(t.TaskDuration)
                })
                .Where(d => d.DueDate >= DateTime.Now)
                .OrderBy(d => d.DueDate)
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

            // Fetch project data
            var projects = db.MainTables
                .Where(m => m.main_id == id)
                .Select(m => new ProjectMilestoneViewModel
                {
                    MainId = m.main_id,
                    ProjectTitle = m.project_title,
                    StartDate = m.project_start,
                    EndDate = m.project_end,
                    Duration = m.duration ?? 0,
                    ProjectYear = m.year ?? 0,
                    Division = m.division,
                    Category = m.category,
                    ProjectOwner = m.project_owner
                })
                .FirstOrDefault();

            if (projects == null)
            {
                return HttpNotFound("No milestones found.");
            }

            // Fetch project details
            var projectDetails = db.MainTables
                .Where(p => p.main_id == id)
                .Select(p => new ProjectDetailViewModel
                {
                    Id = p.main_id,
                    ProjectTitle = p.project_title,
                    ProjectStart = p.project_start.ToString(),
                    ProjectEnd = p.project_end.ToString(),
                    ProjectDuration = p.duration ?? 0,
                    ProjectYear = p.year ?? 0,
                    Division = p.division,
                    Category = p.category,
                    ProjectOwner = p.project_owner,
                })
                .ToList();

            // Fetch milestones for dropdown
            var milestones = db.MilestoneTbls
                .Where(m => m.milestone_id == id)
                .Select(m => new SelectListItem
                {
                    Value = m.milestone_id.ToString(),
                    Text = m.milestone_name
                })
                .ToList();

            // Fetch activity logs for the project
            var activityLogs = db.Activity_Log
                .Where(log => log.log_id == id)
                .Select(log => new ActivityLogViewModel
                {
                    LogId = log.log_id,
                    Username = log.username,
                    DatetimePerformed = log.datetime_performed,
                    ActionLevel = log.action_level.ToString(),
                    Action = log.action,
                    Description = log.description,
                    Department = log.department,
                    Division = log.division
                })
                .ToList();

            var viewModel = new ProjectMilestoneViewModel
            {
                MainId = projects.MainId,
                ProjectTitle = projects.ProjectTitle,
                StartDate = projects.StartDate,
                EndDate = projects.EndDate,
                Duration = projects.Duration,
                ProjectYear = projects.ProjectYear,
                Division = projects.Division,
                Category = projects.Category,
                ProjectOwner = projects.ProjectOwner,
                ProjectDetails = projectDetails,
                Milestones = milestones,
                ActivityLogs = activityLogs
            };

            return View(viewModel);
        }



        public JsonResult getGanttData(int id)
        {
            var currentYear = DateTime.Now.Year;
            var tasks = db.DetailsTbls
                .Where(x => x.main_id == id && x.task_start.HasValue)
                .OrderBy(x => x.milestone_id)
                .ToList();

            var data = tasks.Select(x => new
            {
                id = x.details_id,
                start_date = x.task_start.HasValue ? x.task_start.Value.ToString("yyyy/MM/dd") : DateTime.Now.ToString("yyyy/MM/dd"),
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
                return "orange"; // ongoing task
            else
                return "red"; // overdue task
        }

        [CustomAuthorize(Roles = "PMS_Developer")]
        public ActionResult AddProject()
        {
            List<RegistrationTbl> listProjects = db.RegistrationTbls.Where(x => x.is_file_uploaded == false).ToList();
            return View(listProjects);
        }

        [HttpPost]
        public JsonResult AddProjectUpload()
        {
            var message = "";
            var status = false;
            var attachment = System.Web.HttpContext.Current.Request.Files["pmcsv"];
            var UserId = User.Identity.GetUserId();
            int projectId = Int32.Parse(System.Web.HttpContext.Current.Request.Params.GetValues(0)[0]);
            var project = db.RegistrationTbls.Where(x => x.registration_id == projectId).Single();

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "File is empty", status = false }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var reader = new StreamReader(attachment.InputStream))
                using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture)))
                {
                    csv.Context.RegisterClassMap<exportCSVHeader.ProjectMap>();
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
                            var cntr = db.RegistrationTbls.Where(x => x.project_name == getProject.ProjectTitle).Count();

                            if (getProject == null)
                            {
                                throw new Exception("No valid project data found.");
                            }

                            if (getProject.ProjectTitle == project.project_name)
                            {
                                string dateFormat = "MM/dd/yyyy";

                                if (string.IsNullOrWhiteSpace(getProject.projectStart) ||
                                     string.IsNullOrWhiteSpace(getProject.projectEnd) ||
                                     getProject.ProjectDuration <= 0 ||
                                     getProject.ProjectYear <= 0)
                                {
                                    throw new Exception("Start date, end date, duration, and year cannot be" +
                                        " empty.");
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
                                    project_owner = getProject.projectOwner,
                                    user_id = UserId
                                    
                                };

                                db.MainTables.Add(addWeeklyChecklist);
                                db.SaveChanges();

                                var mainIDfordetails = addWeeklyChecklist.main_id;

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

                                        var subtask = false;
                                        int? parentId = null;
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
                                            main_id = mainIDfordetails
                                        };

                                        detailList.Add(addTask);
                                    }
                                }

                                db.DetailsTbls.AddRange(detailList);
                                db.SaveChanges();

                                var upd_rgstr = db.RegistrationTbls.Where(x => x.registration_id == projectId).Single();
                                upd_rgstr.is_file_uploaded = true;
                                db.SaveChanges();

                                Activity_Log logs = new Activity_Log
                                {
                                    username = User.Identity.Name,
                                    datetime_performed = DateTime.Now,
                                    action_level = 5,
                                    action = "Project Upload",
                                    description = getProject.ProjectTitle + " Project Uploaded by: " + getProject.projectOwner + " For Year: " + getProject.ProjectYear,
                                    department = "ITS",
                                    division = "SDD"
                                };

                                db.Activity_Log.Add(logs);
                                db.SaveChanges();

                                transaction.Commit();
                                message = "Project schedule added successfully!";
                                status = true;
                            }
                            else
                            {
                                message = "Invalid excel file. Please try again.";
                                status = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            message = "An error occurred while saving: " + (ex.InnerException?.Message ?? ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message = "An error occurred: " + (ex.InnerException?.Message ?? ex.Message);
                return Json(new { message = message, status = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public ActionResult UpdateStatus(ProjectMilestoneViewModel model)
        {
            if (ModelState.IsValid)
            {
                var milestone = db.MilestoneTbls.Find(int.Parse(model.SelectedMilestone));
                if (milestone != null)
                {
                    var statusUpdate = new WeeklyStatu
                    {
                        milestone_id = milestone.milestone_id,
                        description = model.StatusUpdate,
                        date_updated = DateTime.Now
                    };

                    if (model.FileUpload != null && model.FileUpload.ContentLength > 0)
                    {
                        string path = Path.Combine(Server.MapPath("~/Uploads"), Path.GetFileName(model.FileUpload.FileName));
                        model.FileUpload.SaveAs(path);
                    }

                    db.WeeklyStatus.Add(statusUpdate);
                    db.SaveChanges();
                }

                return RedirectToAction("WeeklyMilestone", new { id = model.MainId });
            }

            return View(model);
        }
    }
}
