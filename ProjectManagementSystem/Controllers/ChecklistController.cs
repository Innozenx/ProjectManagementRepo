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
using System.Security.Claims;
using System.Web;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net;

namespace ProjectManagementSystem.Controllers
{
    public class ChecklistController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();
        CMIdentityDBEntities cmdb = new CMIdentityDBEntities();

        public ActionResult WeeklyChecklist()
        {
            List<WeeklyChecklistTable> checklist = new List<WeeklyChecklistTable>();
            Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

            var currentYear = DateTime.Now.Year;
            checklist = db.WeeklyChecklistTables.Where(x => x.weeklyInYear == currentYear).ToList();

            return View(checklist);
        }

        public ActionResult DashboardManagement()
        {
            var allProjectsWithMilestones = db.MainTables
                .GroupBy(p => p.division)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => new MainTableViewModel
                    {
                        MainId = p.main_id,
                        ProjectTitle = p.project_title,
                        ProjectStart = p.project_start.ToString(),
                        ProjectEnd = p.project_end.ToString(),
                        Duration = p.duration.ToString(),
                        Year = p.year.ToString(),
                        Division = p.division,
                        Category = p.category,
                        ProjectOwner = p.project_owner,
                        Milestones = db.MilestoneTbls
                            .Where(m => m.main_id == p.main_id)
                            .Select(m => new MilestoneViewModel
                            {
                                MilestoneName = m.milestone_name,
                                // EndDate = m.end_date
                            }).OrderBy(m => m.MilestoneName)
                            .ToList()
                    }).ToList()
                );

            var viewModel = new DashboardManagementViewModel
            {
                ProjectsByDivision = allProjectsWithMilestones,
                UniqueMilestoneNames = allProjectsWithMilestones
                    .SelectMany(d => d.Value.SelectMany(p => p.Milestones.Select(m => m.MilestoneName)))
                    .Distinct()
                    .ToList()
            };

            return View(viewModel);
        }


        public ActionResult Dashboard()
        {
            var userId = User.Identity.GetUserId();

            if (User.IsInRole("PMS_Management"))
            {
                return RedirectToAction("DashboardManagement");
            }


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

        //public ActionResult weeklyMilestone(int id, string title, string projectId)
        //{

        //    TempData["entry"] = id;
        //    TempData["title"] = title;
        //    TempData["project"] = projectId;

        //    // Fetch project data
        //    var projects = db.MainTables
        //        .Where(m => m.main_id == id)
        //        .Select(m => new ProjectMilestoneViewModel
        //        {
        //            MainId = m.main_id,
        //            ProjectTitle = m.project_title,
        //            StartDate = m.project_start,
        //            EndDate = m.project_end,
        //            Duration = m.duration ?? 0,
        //            ProjectYear = m.year ?? 0,
        //            Division = m.division,
        //            Category = m.category,
        //            ProjectOwner = m.project_owner
        //        })
        //        .FirstOrDefault();

        //    if (projects == null)
        //    {
        //        return HttpNotFound("No milestones found.");
        //    }

        //    // Fetch project details
        //    var projectDetails = db.MainTables
        //        .Where(p => p.main_id == id)
        //        .Select(p => new ProjectDetailViewModel
        //        {
        //            Id = p.main_id,
        //            ProjectTitle = p.project_title,
        //            ProjectStart = p.project_start.ToString(),
        //            ProjectEnd = p.project_end.ToString(),
        //            ProjectDuration = p.duration ?? 0,
        //            ProjectYear = p.year ?? 0,
        //            Division = p.division,
        //            Category = p.category,
        //            ProjectOwner = p.project_owner,
        //        })
        //        .ToList();

        //    // Fetch milestones for dropdown
        //    var milestones = db.MilestoneTbls
        //        .Where(m => m.main_id == id)
        //        .Select(m => new SelectListItem
        //        {
        //            Value = m.milestone_id.ToString(),
        //            Text = m.milestone_name
        //        })
        //        .ToList();

        //    // fetch status logs 
        //    var statusLogs = db.WeeklyStatus
        //        .Where(log => log.milestone_id == id) 
        //        .Select(log => new StatusLogsViewModel
        //        {
        //            StatusId = log.status_id,
        //            MilestoneId = log.milestone_id,
        //            ProjectOwner = log.project_owner,
        //            Description = log.description,
        //            DateUpdated = log.date_updated.ToString(),
        //            Attachment = log.attachment
        //        })
        //        .OrderByDescending(log => log.DateUpdated)
        //        .ToList();

        //    var viewModel = new ProjectMilestoneViewModel
        //    {
        //        MainId = projects.MainId,
        //        ProjectTitle = projects.ProjectTitle,
        //        StartDate = projects.StartDate,
        //        EndDate = projects.EndDate,
        //        Duration = projects.Duration,
        //        ProjectYear = projects.ProjectYear,
        //        Division = projects.Division,
        //        Category = projects.Category,
        //        ProjectOwner = projects.ProjectOwner,
        //        ProjectDetails = projectDetails,
        //        Milestones = milestones,
        //        StatusLogs = statusLogs
        //    };

        //    return View(viewModel);
        //}

        public ActionResult weeklyMilestone(int id, string title, string projectId)
        {

            var userId = User.Identity.GetUserId();

            // checker if the project belongs to the user
            var userProject = db.MainTables
                .Where(m => m.main_id == id && m.user_id == userId)
                .FirstOrDefault();

            // if the project doesn't belong to the user, return unauthorized or an error page
            if (userProject == null)
            {
                return RedirectToAction("AccessDenied", "Error");
            }

            TempData["entry"] = id;
            TempData["title"] = title;
            TempData["project"] = projectId;

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

            // fetch project details
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

            // fetch milestone for dropdown
            var milestones = db.MilestoneTbls
                .Where(m => m.main_id == id)
                .Select(m => new SelectListItem
                {
                    Value = m.milestone_id.ToString(),
                    Text = m.milestone_name
                })
                .ToList();

            // fetch status
            var statusLogs = db.WeeklyStatus
                 .Where(log => log.main_id == id)
                 .Select(log => new StatusLogsViewModel
                 {
                     StatusId = log.status_id,
                     MilestoneId = log.milestone_id,
                     ProjectOwner = log.project_owner,
                     Description = log.description,
                     DateUpdated = log.date_updated.ToString(),
                     Username = log.user_id,
                     MilestoneName = log.milestone_name,
                     Attachment = db.AttachmentTables
                        .Where(a => a.status_id == log.status_id)
                        .Select(a => a.path_file)
                        .FirstOrDefault()

                 })
                 .OrderByDescending(log => log.DateUpdated)
                 .ToList();

            // fetch project members
            var projectMembers = db.ProjectMembersTbls
                .Where(pm => pm.project_id == id)
                .AsEnumerable()
                .Select(pm => new ProjectMemberViewModel
                {
                    Name = pm.name,
                    Role = pm.role.Value,
                    Initials = !string.IsNullOrEmpty(pm.name) && pm.name.Split(' ').Length > 2
                        ? pm.name.Split(' ')[0].Substring(0, 1) + pm.name.Split(' ')[1].Substring(0, 1)
                        : "N/A",
                    //Initials = pm.name,
                    Email = pm.email
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
                StatusLogs = statusLogs,
                ProjectMembers = projectMembers
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
                completed = x.isCompleted,
                key_person = x.key_person
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
            var onboardingDetails = new Onboarding()
            {
                registered_project = db.RegistrationTbls.Where(x => x.is_file_uploaded == false && x.unregistered == false && x.is_completed == false).ToList(),
                users = cmdb.AspNetUsers.ToList()

            };

            return View(onboardingDetails);
        }

        public ActionResult InviteTeammates()
        {
            // Fetch users
            var users = cmdb.AspNetUsers.Select(u => new UserModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            }).ToList();

            // Fetch roles
            var roles = db.Roles.Select(r => new RoleModel
            {
                Id = r.id,
                RoleName = r.RoleName
            }).ToList();

            // Fetch projects
            var projects = db.MainTables.Select(p => new ProjectModel
            {
                Id = p.main_id,
                Title = p.project_title
            }).ToList();

            // Create the model
            var model = new Onboarding
            {
                Users = users,
                Roles = roles,
                Projects = projects,

            };

            return View(model);
        }

        [HttpPost]
        public JsonResult AddProjectUpload()
        {
            var message = "";
            var status = false;
            var attachment = System.Web.HttpContext.Current.Request.Files["csvFile"];
            var UserId = User.Identity.GetUserId();
            int projectId = Int32.Parse(System.Web.HttpContext.Current.Request.Params.GetValues(0)[0]);
            var project = db.RegistrationTbls.Where(x => x.registration_id == projectId).Single();
            var division = "";
            var department = "";

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "File is empty", status = false }, JsonRequestBehavior.AllowGet);

            }

            try
            {
                var tblJoin = (from netUser in cmdb.AspNetUsers
                               join jobDesc in cmdb.Identity_JobDescription on netUser.JobId equals jobDesc.Id
                               join idKey in cmdb.Identity_Keywords on jobDesc.DeptId equals idKey.Id
                               select new { netUser.UserName, jobDesc.DeptId, jobDesc.DivisionId, idKey.Description }).Where(x => x.UserName == User.Identity.Name).ToList();

                foreach (var item in tblJoin)
                {
                    division = cmdb.Identity_Keywords.Where(x => x.Id == item.DivisionId).Select(x => x.Description).Single();
                    department = cmdb.Identity_Keywords.Where(x => x.Id == item.DeptId).Select(x => x.Description).Single();
                }

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
                                    project_start = DateTime.ParseExact(getProject.projectStart.ToString(), dateFormat, CultureInfo.InvariantCulture),
                                    project_end = DateTime.ParseExact(getProject.projectEnd.ToString(), dateFormat, CultureInfo.InvariantCulture),
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
                                        DateTime taskStartDate = DateTime.ParseExact(DateTime.Parse(taskGroup.TaskStart).ToString("MM/dd/yyyy"), dateFormat, CultureInfo.InvariantCulture);
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
                                    department = department,
                                    division = division
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


        [HttpPost]
        public ActionResult UpdateStatus(ProjectMilestoneViewModel model)
        {
            if (ModelState.IsValid)
            {
                var milestone = db.MilestoneTbls.Find(int.Parse(model.SelectedMilestone));
                if (milestone != null)
                {

                    int projectId = (int)milestone.main_id;
                    string userId = User.Identity.GetUserId();

                    var statusUpdate = new WeeklyStatu
                    {
                        milestone_id = milestone.milestone_id,
                        description = model.StatusUpdate,
                        date_updated = DateTime.Now,
                        main_id = projectId,
                        user_id = User.Identity.Name,
                        milestone_name = milestone.milestone_name


                    };
                    db.WeeklyStatus.Add(statusUpdate);
                    db.SaveChanges();

                    // Save file if uploaded
                    if (model.FileUpload != null && model.FileUpload.ContentLength > 0)
                    {
                        string uploadPath = Server.MapPath("~/Uploads");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        string filePath = Path.Combine(uploadPath, Path.GetFileName(model.FileUpload.FileName));
                        model.FileUpload.SaveAs(filePath);

                        var attachment = new AttachmentTable
                        {
                            status_id = statusUpdate.status_id,
                            path_file = filePath
                        };

                        db.AttachmentTables.Add(attachment);
                        db.SaveChanges();
                    }

                    var activityLog = new Activity_Log
                    {
                        log_id = projectId,
                        username = User.Identity.Name,
                        datetime_performed = DateTime.Now,
                        action_level = 1,
                        action = "Status Update",
                        description = $"Updated status for milestone: {milestone.milestone_name}.",
                        department = "ITS",
                        division = model.Division

                    };

                    db.Activity_Log.Add(activityLog);
                    db.SaveChanges();

                    TempData["StatusUpdated"] = true;

                    return RedirectToAction("weeklyMilestone", new { id = projectId, title = TempData["title"], projectId = TempData["project"] });
                }

                ModelState.AddModelError("", "Milestone not found.");
                return View(model);
            }
            return View(model);
        }

        //------------------------Activity Log Viewing----------------------------
        public ActionResult ActivityView()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ActivityList(JqueryDatatableParam param)
        {
            var searchValue = Request.Form.GetValues("search[value]").First();
            var division = "";
            var department = "";
            var message = "";
            var userClaims = User.Identity as System.Security.Claims.ClaimsIdentity;
            var role = userClaims.Claims.Where(x => x.Type == ClaimTypes.Role).ToList();
            List<string> userRole = new List<string>();
            List<Activity_Log> logList = new List<Activity_Log>();

            var tblJoin = (from netUser in cmdb.AspNetUsers
                           join jobDesc in cmdb.Identity_JobDescription on netUser.JobId equals jobDesc.Id
                           join idKey in cmdb.Identity_Keywords on jobDesc.DeptId equals idKey.Id
                           select new { netUser.UserName, jobDesc.DeptId, jobDesc.DivisionId, idKey.Description }).Where(x => x.UserName == User.Identity.Name).ToList();

            foreach (var item in tblJoin)
            {
                division = cmdb.Identity_Keywords.Where(x => x.Id == item.DivisionId).Select(x => x.Description).Single();
                department = cmdb.Identity_Keywords.Where(x => x.Id == item.DeptId).Select(x => x.Description).Single();
            }

            foreach (var item in role)
            {
                if (item.Value.ToString().Contains("PMS"))
                {
                    userRole.Add(item.Value);
                }
            };

            var query = db.Activity_Log.OrderByDescending(x => x.datetime_performed).ToList();

            var data = query.Select(x => new
            {
                log_id = x.log_id,
                user = x.username,
                date = DateTime.Parse(x.datetime_performed.ToString()).ToString(),
                action = x.action,
                description = x.description,
                department = x.department,
                division = x.division

            });

            if (!string.IsNullOrEmpty(searchValue))
            {
                try
                {
                    if (data.Where(x => x.user.ToLower().Contains(searchValue.ToLower()) || x.department.ToLower().Contains(searchValue.ToLower()) || x.division.ToLower().Contains(searchValue.ToLower())).ToList().Count != 0)
                    {
                        data = data.Where(x => x.user.ToLower().Contains(searchValue.ToLower()) || x.department.ToLower().Contains(searchValue.ToLower()) || x.division.ToLower().Contains(searchValue.ToLower())).ToList();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("----------------------------------------------------ERROR LOG START-----------------------------------------------------------");
                    Debug.WriteLine(e);
                    Debug.WriteLine("----------------------------------------------------ERROR LOG END!!-----------------------------------------------------------");
                }

            }

            var displayResult = data.Skip(param.start).Take(param.length).ToList();
            var totalRecords = data.Count();
            return Json(new { param.draw, iTotalRecords = totalRecords, iTotalDisplayRecords = totalRecords, data = displayResult }, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult ProjectChecklist()
        //{
        //    return View();
        //}

        public JsonResult EmailInvitees(string[] users, int[] roles, string project)
        {
            var systemEmail = "e-notify@enchantedkingdom.ph";
            var systemName = "PM SYSTEM";
            var message = "";

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    for (var i = 0; i < users.Length; i++)
                    {
                        var userName = users[i].Substring(0, users[i].IndexOf("("));
                        var userEmail = users[i].Substring(users[i].IndexOf("("));
                        userEmail = userEmail.Replace(")", "");
                        userEmail = userEmail.Replace("(", "");

                        var userProjectId = Int32.Parse(project);

                        if (db.ProjectMembersTbls.Where(x => x.email == userEmail && x.project_id == userProjectId).Count() < 1)
                        {
                            var userRoleId = roles[i];
                            var userRole = db.Roles.Where(x => x.id == userRoleId).Select(x => x.RoleName).Single();


                            var userProject = db.MainTables.Where(x => x.main_id == userProjectId).Select(x => x.project_title).Single();

                            var email = new MimeMessage();

                            email.From.Add(new MailboxAddress(systemName, systemEmail));
                            email.To.Add(new MailboxAddress(userName, userEmail));

                            email.Subject = userName + "invited you to a project: " + userProject;
                            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                            {
                                Text = @"
                    <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <img src='~/AdminLTE-3.2.0/dist/img/ekk.jpg' alt='Eldar Logo' style='max-width: 80px; border-radius: 50%; margin-bottom: 20px;'>
                            <h1 style='font-size: 24px; color: #66339A; margin: 0;'>You have been added to the project </h1> <span style='font-size: 16px; font-weight: bold; color: #66339A;'><i>" + userProject + @"</i></span>
                            <p style='font-size: 14px; color: #666; margin-top: 10px;'>Get started on your new assignment with just one click.</p>
                        </div>
                        <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);'>
                            <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Magical Day, " + userName + @"!</p>
               
                            <p style='font-size: 14px; color: #555;'>
                                <b>Your Role:</b> <span style='color: #333;'>" + userRole + @"</span><br>
                                <b>Assigned Task(s):</b> <span style='color: #333;'> <insert tasks here> </span>
                            </p>
                            <p style='font-size: 14px; color: #555; margin-top: 20px;'>
                               Please click the button to get started:
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='http://localhost:60297/Checklist/Dashboard" + userEmail + @"'
                                   style='display: inline-block; padding: 12px 30px; background-color: #66339A; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px; font-size: 14px;'>
                                   View Project
                                </a>
                            </div>
                            <p style='font-size: 14px; color: #555; text-align: center;'>
                                Need assistance? Please don’t hesitate to reach out.
                            </p>
                        </div>
                        <div style='margin-top: 20px; padding: 20px; text-align: center; background-color: #f4f4f9; border-radius: 5px; font-size: 12px; color: #999;'>
                            <i>*This is an automated email from the Project Management System. Please do not reply. For any concerns, kindly contact your immediate supervisor or reach out to ITS at <b>LOCAL: 132</b>.</i>
                        </div>
                    </div>"
                            };

                            using (var smtp = new SmtpClient())
                            {
                                smtp.Connect("mail.enchantedkingdom.ph", 587, false);

                                // Note: only needed if the SMTP server requires authentication
                                smtp.Authenticate("e-notify@enchantedkingdom.ph", "ENCHANTED2024");

                                smtp.Send(email);
                                smtp.Disconnect(true);
                            }

                            ProjectMemberViewModel member = new ProjectMemberViewModel()
                            {
                                Name = userName,
                                Email = userEmail,
                                Project_ID = Int32.Parse(project),
                                Role = roles[i],
                                Division = "N/A",
                                Department = "N/A"
                            };

                            ProjectMembersTbl dbMember = new ProjectMembersTbl
                            {
                                name = member.Name,
                                email = member.Email,
                                project_id = member.Project_ID,
                                role = member.Role,
                                division = member.Division,
                                department = member.Department,
                                acknowledged = false
                            };

                            db.ProjectMembersTbls.Add(dbMember);
                            db.SaveChanges();

                            message = "Success";
                            transaction.Commit();
                        }

                        else
                        {
                            message = "User/s are already invited, please check your list of invitees";
                            transaction.Rollback();
                            break;
                        }

                    }
                }

                catch (Exception e)
                {
                    message = "An error occured. Please refresh your browser and re-check your entries. If error persists, please contact ITS Local: 132";
                    transaction.Rollback();
                }

            }

            return Json(new { message = message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AcknowledgeInvite(string email)
        {
            try
            {
                var acknowledged = db.ProjectMembersTbls.Where(x => x.email == email).First();
                acknowledged.acknowledged = true;

                db.SaveChanges();
            }
            catch (Exception e)
            {
                var message = e.Message;
            }


            return View();
        }
        [HttpGet]
        public ActionResult ProjectChecklist()
        {
            var groupedMilestones = db.MainTables
                .Select(project => new ProjectChecklistGroupViewModel
                {
                    MainId = project.main_id,
                    ProjectName = project.project_title,
                    Milestones = db.MilestoneTbls
                        .Where(m => m.main_id == project.main_id)
                        .OrderBy(m => m.milestone_position)
                        .Select(milestone => new MilestoneViewModel
                        {
                            Id = milestone.milestone_id,
                            MilestoneName = milestone.milestone_name,
                            MilestonePosition = milestone.milestone_position ?? 0, 
                    IsCompleted = db.DetailsTbls
                                .Where(t => t.milestone_id == milestone.milestone_id)
                                .All(t => t.IsApproved.HasValue && t.IsApproved.Value),
                            StatusUpdate = db.DetailsTbls
                                .Where(t => t.milestone_id == milestone.milestone_id)
                                .All(t => t.IsApproved.HasValue && t.IsApproved.Value)
                                    ? "Completed"
                                    : "In Progress",
                            Tasks = db.DetailsTbls
                                .Where(task => task.milestone_id == milestone.milestone_id)
                                .Select(task => new TaskViewModel
                                {
                                    Id = task.details_id,
                                    TaskName = task.process_title,
                                    IsApproved = task.IsApproved.HasValue && task.IsApproved.Value,
                                    Attachments = db.AttachmentTables
                                        .Where(a => a.details_id == task.details_id)
                                        .Select(a => a.path_file)
                                        .ToList(),
                                    Approvers = db.ApproversTbls
                                        .Where(a => a.Details_Id == task.details_id)
                                        .Select(a => new ApproverViewModel
                                        {
                                            ApproverName = a.Approver_Name,
                                            Status = a.Status ?? false
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();

            return View(groupedMilestones);
        }


    }
}