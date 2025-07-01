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
using System.Data.Entity;

namespace ProjectManagementSystem.Controllers
{
    public class ChecklistController : BaseController
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

        [Authorize(Roles = "PMS_Management, PMS_ODCP_ADMIN, PMS_DIVISION_HEAD")]
        public ActionResult DashboardManagement()
        {
            var userName = User.Identity.Name;
            var userDetails = cmdb.AspNetUsers.FirstOrDefault(x => x.UserName == userName);
            var userDivision = (from u in cmdb.AspNetUsers
                                join j in cmdb.Identity_JobDescription on u.JobId equals j.Id
                                join i in cmdb.Identity_Keywords on j.DivisionId equals i.Id
                                where u.UserName == userName && i.Type == "Divisions"
                                select i.Description).FirstOrDefault();

            db.Database.CommandTimeout = 120;

            var mainProjects = db.MainTables
                .Where(p => !string.IsNullOrEmpty(p.division) && (p.IsArchived == false || p.IsArchived == null))
                .ToList();

            var allMilestonesRaw = (from m in db.MilestoneTbls
                                    join p in db.MainTables on m.main_id equals p.main_id
                                    where p.IsArchived == false || p.IsArchived == null
                                    join t in db.DetailsTbls on m.milestone_id equals t.milestone_id into tasks
                                    from task in tasks.DefaultIfEmpty()
                                    group new { m, task } by new
                                    {
                                        p.main_id,
                                        p.project_title,
                                        p.division,
                                        m.milestone_name,
                                        m.milestone_position,
                                        m.milestone_id
                                    }
                                    into g
                                    select new
                                    {
                                        g.Key.main_id,
                                        g.Key.project_title,
                                        g.Key.division,
                                        g.Key.milestone_name,
                                        g.Key.milestone_position,
                                        g.Key.milestone_id,
                                        Tasks = g.Select(x => new TempTask
                                        {
                                            task_start = x.task.task_start,
                                            task_duration = x.task.task_duration,
                                            isCompleted = x.task.isCompleted,
                                            CurrentCompletionDate = x.m.current_completion_date,
                                            CompletionDate = x.m.completion_date
                                        }).ToList()
                                    }).ToList();

            var milestoneDict = allMilestonesRaw
                .GroupBy(m => m.main_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.milestone_position).ToList()
                );

            foreach (var project in milestoneDict)
            {
                DateTime? previousMaxDate = null;

                foreach (var milestone in project.Value)
                {
                    var maxOriginalDate = milestone.Tasks.Max(t => t.CompletionDate ?? t.CurrentCompletionDate ?? DateTime.MinValue);
                    var maxCurrentDate = milestone.Tasks.Max(t => t.CurrentCompletionDate ?? t.CompletionDate ?? DateTime.MinValue);

                    if (previousMaxDate.HasValue && maxCurrentDate < previousMaxDate.Value)
                    {
                        var delayDays = (previousMaxDate.Value - maxCurrentDate).Days;

                        for (int i = 0; i < milestone.Tasks.Count; i++)
                        {
                            var task = milestone.Tasks[i];
                            milestone.Tasks[i] = new TempTask
                            {
                                task_start = task.task_start?.AddDays(delayDays),
                                task_duration = task.task_duration,
                                isCompleted = task.isCompleted,
                                CurrentCompletionDate = task.CurrentCompletionDate?.AddDays(delayDays),
                                CompletionDate = task.CompletionDate?.AddDays(delayDays)
                            };
                        }

                        maxCurrentDate = maxCurrentDate.AddDays(delayDays);
                    }

                    previousMaxDate = maxCurrentDate > previousMaxDate ? maxCurrentDate : previousMaxDate;
                }
            }

            var allMilestoneViewModels = allMilestonesRaw.Select(g =>
            {
                var dueDate = g.Tasks.Max(t => t.CurrentCompletionDate ?? t.CompletionDate ?? DateTime.MinValue);
                var today = DateTime.Today;

                var checklistApprovalsQuery = db.ChecklistSubmissions
                    .Where(cs => cs.main_id == g.main_id && cs.milestone_id == g.milestone_id);

                bool allApproved = checklistApprovalsQuery.Any() && checklistApprovalsQuery.All(cs => cs.is_approved == true);

                string status = "Pending";
                if (allApproved) status = "Completed";
                else if (dueDate != DateTime.MinValue)
                {
                    if (dueDate < today) status = "Delayed";
                    else if (dueDate == today) status = "Due Today";
                    else status = "Active";
                }

                return new ProjectMilestoneViewModel
                {
                    MainId = g.main_id,
                    ProjectTitle = g.project_title,
                    MilestoneName = g.milestone_name,
                    EndDate = dueDate != DateTime.MinValue ? (DateTime?)dueDate : null,
                    CurrentCompletionDate = dueDate != DateTime.MinValue ? (DateTime?)dueDate : null,
                    MilestoneStatus = status,
                    MilestonePosition = g.milestone_position ?? 0,
                    Tasks = g.Tasks.Select(t => new TaskViewModel
                    {
                        TaskStart = t.task_start,
                        Duration = t.task_duration ?? 0,
                        IsCompleted = t.isCompleted ?? false
                    }).ToList()
                };
            }).ToList();

            var projectMilestoneGroups = allMilestoneViewModels
                .GroupBy(p => p.MainId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int totalProjects = projectMilestoneGroups.Count();
            int completedProjects = projectMilestoneGroups.Count(x => x.Value.All(m => m.MilestoneStatus == "Completed"));
            int pendingProjects = totalProjects - completedProjects;

            var groupedByDivision = allMilestoneViewModels
                .GroupBy(pm => mainProjects.FirstOrDefault(p => p.main_id == pm.MainId)?.division?.Trim().ToUpper() ?? $"NoDivision_{pm.MainId}")
                .ToDictionary(g => g.Key, g => g.ToList());

            var viewModel = new DashboardManagementViewModel
            {
                ProjectsByDivision = groupedByDivision.ToDictionary(
                    g => g.Key,
                    g => g.Value
                        .GroupBy(m => m.MainId)
                        .Select(group => new MainTableViewModel
                        {
                            MainId = group.Key,
                            ProjectTitle = group.First().ProjectTitle,
                            Milestones = group.Select(m => new MilestoneViewModel
                            {
                                MilestoneName = m.MilestoneName,
                                CurrentTaskEnd = m.CurrentCompletionDate
                            }).ToList()
                        }).ToList()
                ),

                ProjectsMilestones = allMilestoneViewModels,

                UniqueMilestoneNames = allMilestoneViewModels
                    .GroupBy(m => m.MilestoneName)
                    .OrderBy(g => allMilestoneViewModels
                        .Where(x => x.MilestoneName == g.Key)
                        .Min(x => x.MilestonePosition))
                    .Select(g => g.Key)
                    .ToList(),

                userRole = userDetails?.JobLevel ?? 0,
                userDivision = userDivision,
                Divisions = groupedByDivision.Keys.ToList(),
                CompletedTasks = completedProjects,
                PendingTasks = pendingProjects,
                TotalTasks = totalProjects,
                CurrentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),

            IndividualProjects = allMilestoneViewModels
            .GroupBy(m => m.MainId)
            .Select(group => new MainTableViewModel
            {
                MainId = group.Key,
                ProjectTitle = group.First().ProjectTitle,
                Milestones = group.Select(m => new MilestoneViewModel
                {
                    MilestoneName = m.MilestoneName,
                    CurrentTaskEnd = m.CurrentCompletionDate
                }).ToList()
            }).ToList(),
             ShowBothDashboards = (User.IsInRole("PMS_ODCP_ADMIN") && userDetails?.JobLevel == 4035),

            };

            return View(viewModel);
        }

        [Authorize(Roles = "PMS_PROJECT_MANAGER, PMS_ODCP_ADMIN, PMS_Management, PMS_PROJECT_OWNER, PMS_DIVISION_HEAD, PMS_USER")]
        public ActionResult Dashboard()
        {
            var userEmail = User.Identity.GetUserName(); ;

            var userId = User.Identity.GetUserId();

            if (User.IsInRole("PMS_Management") || User.IsInRole("PMS_DIVISION_HEAD"))
            {
                return RedirectToAction("DashboardManagement");
            }

            if (!User.Identity.IsAuthenticated)
            {
                return View("~/Views/Account/Login.cshtml");
            }

            //var userEmail = User.Identity.GetUserName();
            var userInfo = cmdb.AspNetUsers.FirstOrDefault(u => u.Email == userEmail);

            ViewBag.IsApprover = db.ApproversTbls.Any(a => a.User_Id == userId && a.IsRemoved_ == false)
            || db.OptionalMilestoneApprovers.Any(a => a.approver_email.ToLower().Trim() == userEmail.ToLower().Trim())
            || db.PreSetMilestoneApprovers.Any(a => a.approver_email.ToLower().Trim() == userEmail.ToLower().Trim());

            ViewBag.FirstName = userInfo?.FirstName ?? "User";

            // get the projects where the user is a member, excluding archived/completed
            var projectList = (from p in db.ProjectMembersTbls
                               join m in db.MainTables on new { id = p.project_id } equals new { id = (int?)m.main_id }
                               where p.email == userEmail
                                     && m.isCompleted != true
                                     && (m.IsArchived == false || m.IsArchived == null)
                               select m.main_id).ToList();

            var currentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);

            db.Database.CommandTimeout = 120;

            // get project titles for summary 
            var projects = db.MainTables
                .Where(p => projectList.Contains(p.main_id))
                .Select(p => new ProjectChecklistGroupViewModel
                {
                    MainId = p.main_id,
                    ProjectName = p.project_title
                }).ToList();

            // fetch raw milestone data
            var rawProjectsAndMilestones = (from m in db.MilestoneTbls
                                            join p in db.MainTables on m.main_id equals p.main_id
                                            where p.IsArchived == false || p.IsArchived == null
                                            join t in db.DetailsTbls on m.milestone_id equals t.milestone_id into tasks
                                            from task in tasks.DefaultIfEmpty()
                                            group new { m, task } by new { p.main_id, p.project_title, m.milestone_name, m.milestone_position, m.milestone_id }
                                            into g
                                            select new
                                            {
                                                MainId = g.Key.main_id,
                                                ProjectTitle = g.Key.project_title,
                                                MilestoneName = g.Key.milestone_name,
                                                MilestonePosition = g.Key.milestone_position,
                                                MilestoneId = g.Key.milestone_id,
                                                Tasks = g.Select(x => new TempTask
                                                {
                                                    task_start = x.task.task_start,
                                                    task_duration = x.task.task_duration,
                                                    isCompleted = x.task.isCompleted,
                                                    CurrentCompletionDate = x.m.current_completion_date,
                                                    CompletionDate = x.m.completion_date
                                                }).ToList()
                                            }).ToList();

            // delay logic per milestone
            var milestoneDict = rawProjectsAndMilestones
                .Where(m => projectList.Contains(m.MainId))
                .GroupBy(m => m.MainId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.MilestonePosition).ToList()
                );

            foreach (var project in milestoneDict)
            {
                DateTime? previousMaxDate = null;

                foreach (var milestone in project.Value)
                {
                    var maxOriginalDate = milestone.Tasks
                        .Max(t => t.CompletionDate ?? t.CurrentCompletionDate ?? DateTime.MinValue);

                    var maxCurrentDate = milestone.Tasks
                        .Max(t => t.CurrentCompletionDate ?? t.CompletionDate ?? DateTime.MinValue);

                    if (previousMaxDate.HasValue && maxCurrentDate < previousMaxDate.Value)
                    {
                        var delayDays = (previousMaxDate.Value - maxCurrentDate).Days;

                        for (int i = 0; i < milestone.Tasks.Count; i++)
                        {
                            var task = milestone.Tasks[i];

                            milestone.Tasks[i] = new TempTask
                            {
                                task_start = task.task_start?.AddDays(delayDays),
                                task_duration = task.task_duration,
                                isCompleted = task.isCompleted,
                                CurrentCompletionDate = task.CurrentCompletionDate?.AddDays(delayDays),
                                CompletionDate = task.CompletionDate?.AddDays(delayDays)
                            };
                        }

                        maxCurrentDate = maxCurrentDate.AddDays(delayDays);
                    }

                    previousMaxDate = maxCurrentDate > previousMaxDate ? maxCurrentDate : previousMaxDate;
                }
            }

            // milestones with status
            var projectsAndMilestones = rawProjectsAndMilestones
                .Where(g => projectList.Contains(g.MainId))
                .OrderBy(g => g.MilestonePosition)
                .Select(g =>
                {
                    var dueDate = g.Tasks
                        .Max(t => t.CurrentCompletionDate ?? t.CompletionDate ?? DateTime.MinValue);

                    bool hasDueDate = dueDate != DateTime.MinValue;

                    var checklistApprovalsQuery = db.ChecklistSubmissions
                        .Where(cs => cs.main_id == g.MainId && cs.milestone_id == g.MilestoneId);

                    bool allApproved = checklistApprovalsQuery.Any() && checklistApprovalsQuery.All(cs => cs.is_approved == true);

                    string status;
                    var today = DateTime.Today;

                    if (allApproved)
                    {
                        status = "Completed";
                    }
                    else if (hasDueDate)
                    {
                        if (dueDate.Date > today)
                        {
                            status = "Active";
                        }
                        else if (dueDate.Date == today)
                        {
                            status = "Due Today";
                        }
                        else
                        {
                            status = "Delayed";
                        }
                    }
                    else
                    {
                        status = "Pending";
                    }

                    return new ProjectMilestoneViewModel
                    {
                        MainId = g.MainId,
                        ProjectTitle = g.ProjectTitle,
                        MilestoneName = g.MilestoneName,
                        CurrentCompletionDate = hasDueDate ? (DateTime?)dueDate : null,
                        Tasks = g.Tasks.Select(t => new TaskViewModel
                        {
                            TaskStart = t.task_start,
                            Duration = t.task_duration ?? 0,
                            IsCompleted = t.isCompleted ?? false
                        }).ToList(),
                        EndDate = hasDueDate ? (DateTime?)dueDate : null,
                        MilestoneStatus = status
                    };
                }).ToList();

            //var userEmail = User.Identity.Name.ToLower().Trim();

            // approval task count (preset + optional)
            var checklistMain = db.PreSetMilestoneApprovers
                .Where(x => x.added_by == userEmail)
                .Select(x => new { IsApproved = x.approved })
                
                .ToList();

            var checklistOptional = db.OptionalMilestoneApprovers
                .Where(x => x.added_by == userEmail)
                .Select(x => new { IsApproved = x.approved })
                .ToList();

            var allChecklistItems = checklistMain.Concat(checklistOptional).ToList();
            var totalTasks = allChecklistItems.Count;
            var completedTasks = allChecklistItems.Count(x => x.IsApproved == true);
            var pendingTasks = totalTasks - completedTasks;

            // upcoming deliverables
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
                Projects = projects,
                UpcomingDeliverables = upcomingDeliverables
            };

            return View(viewModel);
        }


        private List<ProjectMilestoneViewModel> GetProjectsAndMilestones(List<int> projectIds)
        {
            var rawProjectsAndMilestones = (from m in db.MilestoneTbls
                                            join p in db.MainTables on m.main_id equals p.main_id
                                            where projectIds.Contains(p.main_id)
                                                  && (p.IsArchived == false || p.IsArchived == null)
                                            join t in db.DetailsTbls on m.milestone_id equals t.milestone_id into tasks
                                            from task in tasks.DefaultIfEmpty()
                                            group new { m, task } by new { p.main_id, p.project_title, m.milestone_name, m.milestone_position, m.milestone_id }
                                            into g
                                            select new
                                            {
                                                MainId = g.Key.main_id,
                                                ProjectTitle = g.Key.project_title,
                                                MilestoneName = g.Key.milestone_name,
                                                MilestonePosition = g.Key.milestone_position,
                                                MilestoneId = g.Key.milestone_id,
                                                Tasks = g.Select(x => new TempTask
                                                {
                                                    task_start = x.task.task_start,
                                                    task_duration = x.task.task_duration,
                                                    isCompleted = x.task.isCompleted,
                                                    CurrentCompletionDate = x.m.current_completion_date,
                                                    CompletionDate = x.m.completion_date
                                                }).ToList()
                                            }).ToList();

            return rawProjectsAndMilestones
                .OrderBy(m => m.MilestonePosition)
                .Select(g =>
                {
                    var dueDate = g.Tasks
                        .Max(t => t.CurrentCompletionDate ?? t.CompletionDate ?? DateTime.MinValue);

                    var today = DateTime.Today;
                    bool allApproved = db.ChecklistSubmissions
                        .Where(cs => cs.main_id == g.MainId && cs.milestone_id == g.MilestoneId)
                        .All(cs => cs.is_approved == true);

                    string status = "Pending";
                    if (allApproved)
                        status = "Completed";
                    else if (dueDate > today)
                        status = "Active";
                    else if (dueDate == today)
                        status = "Due Today";
                    else if (dueDate != DateTime.MinValue)
                        status = "Delayed";

                    return new ProjectMilestoneViewModel
                    {
                        MainId = g.MainId,
                        ProjectTitle = g.ProjectTitle,
                        MilestoneName = g.MilestoneName,
                        CurrentCompletionDate = dueDate != DateTime.MinValue ? (DateTime?)dueDate : null,
                        EndDate = dueDate != DateTime.MinValue ? (DateTime?)dueDate : null,
                        MilestoneStatus = status,
                        Tasks = g.Tasks.Select(t => new TaskViewModel
                        {
                            TaskStart = t.task_start,
                            Duration = t.task_duration ?? 0,
                            IsCompleted = t.isCompleted ?? false
                        }).ToList()
                    };
                }).ToList();
        }



        public class TempTask
        {
            public DateTime? task_start { get; set; }
            public int? task_duration { get; set; }
            public bool? isCompleted { get; set; }
            public DateTime? CurrentCompletionDate { get; set; }
            public DateTime? CompletionDate { get; set; }
        }

        public ActionResult ArchiveProject(int id)
        {
            var project = db.MainTables.FirstOrDefault(p => p.main_id == id);

            if (project == null)
                return HttpNotFound();

            // temporarily removed condition for testing
            // if (project.isCompleted != true)
            // {
            //     TempData["ArchiveError"] = "This project is not yet completed.";
            //     return RedirectToAction("Dashboard");
            // }

            project.IsArchived = true;
            db.SaveChanges();

            TempData["ArchiveSuccess"] = "Project archived successfully.";
            return RedirectToAction("Dashboard");
        }

        public ActionResult ArchivedProjects()
        {
            var archivedProjects = db.MainTables
                .Where(p => p.IsArchived == true)
                .ToList();

            return View(archivedProjects); 
        }
        [HttpPost]
        public ActionResult RestoreProject(int id)
        {
            var project = db.MainTables.FirstOrDefault(p => p.main_id == id);
            if (project != null)
            {
                project.IsArchived = false;
                db.SaveChanges();
                TempData["Message"] = "Project restored successfully.";
            }

            return RedirectToAction("ArchivedProjects");
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
        
        public ActionResult weeklyMilestone(int id, string title, string projectId, string tab = "overview", bool fromApproval = false)
        {

            ViewBag.FromApproval = TempData["FromApproval"] != null && (bool)TempData["FromApproval"];
            ViewBag.CurrentUserEmail = TempData["CurrentUserEmail"] ?? User.Identity.Name;
            ViewBag.CurrentUserEmail = User.Identity.Name;

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var userEmail = User.Identity.GetUserName();
                var userDetails = cmdb.AspNetUsers.Where(x => x.UserName == userEmail).SingleOrDefault();

                var projectList = (from p in db.ProjectMembersTbls
                                   join m in db.MainTables on new { id = p.project_id } equals new { id = (int?)m.main_id }
                                   where p.email == userEmail && m.isCompleted != true
                                   select m.main_id).ToList();

                var userProject = db.MainTables.FirstOrDefault(m => m.main_id == id);

                // OLD
                //bool isUserAssigned = projectList.Contains(id);
                //bool isArchived = userProject?.IsArchived == true;
                //bool isPrivilegedUser = userDetails.JobLevel == 4035 || userDetails.JobLevel == 4036;

                //if (!isUserAssigned && !isArchived && !isPrivilegedUser)
                //{
                //    return RedirectToAction("AccessDenied", "Error");
                //}

                //if (userProject == null && !isPrivilegedUser)
                //{
                //    return RedirectToAction("AccessDenied", "Error");
                //}

                //bool isProjectManager = db.ProjectMembersTbls
                //    .Any(pm => pm.project_id == id && pm.email == userEmail && pm.role == 1004);

                // NEW
                bool isUserAssigned = projectList.Contains(id);
                bool isArchived = userProject?.IsArchived == true;

                bool isODCPAdmin = User.IsInRole("PMS_ODCP_ADMIN");
                bool isManagement = User.IsInRole("PMS_Management");
                bool isDivisionOrDeptHead = userDetails.JobLevel == 4035 || userDetails.JobLevel == 4036;

                // check if user is an approver for this project
                bool isApprover = db.PreSetMilestoneApprovers
                                    .Any(a => a.main_id == id && a.approver_email.ToLower() == userEmail.ToLower() && (a.is_removed == null || a.is_removed == false))
                                 || db.OptionalMilestoneApprovers
                                    .Any(a => a.main_id == id && a.approver_email.ToLower() == userEmail.ToLower() && (a.is_removed == null || a.is_removed == false));

                bool isPrivilegedUser = isODCPAdmin || isManagement || isDivisionOrDeptHead;

                if (!isUserAssigned && !isArchived && !isPrivilegedUser && !isApprover)
                {
                    return RedirectToAction("AccessDenied", "Error");
                }

                bool isProjectManager = db.ProjectMembersTbls
                    .Any(pm => pm.project_id == id && pm.email == userEmail && pm.role == 1004);
                bool isReadOnlyChecklistView = !isProjectManager && !isODCPAdmin; // newly added

                UserModel userInfo = new UserModel()
                {
                    Email = userDetails.Email,
                    JobLevel = userDetails.JobLevel,
                    FirstName = userDetails.FirstName,
                    LastName = userDetails.LastName
                };

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
                        ProjectOwner = m.project_owner,
                        IsArchived = m.IsArchived ?? false
                    })
                    .FirstOrDefault();

                if (projects == null)
                {
                    return HttpNotFound("No milestones found.");
                }
      
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

                var milestones = db.MilestoneTbls
                    .Where(m => m.main_id == id)
                    .Select(m => new SelectListItem
                    {
                        Value = m.milestone_id.ToString(),
                        Text = m.milestone_name
                    })
                    .ToList();

                var tasks = db.DetailsTbls
                    .Where(t => t.main_id == id)
                    .Select(t => new SelectListItem
                    {
                        Value = t.details_id.ToString(),
                        Text = t.process_title
                    })
                    .ToList();

                var statusLogs = db.WeeklyStatus
                     .Where(log => log.main_id == id)
                     .Select(log => new StatusLogsViewModel
                     {
                         StatusId = log.status_id,
                         MilestoneId = log.details_id,
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
                        Email = pm.email
                    })
                    .ToList();

                var projChecklist = db.ApproversTbls
                    .Where(a => a.Main_Id == id)
                    .Select(a => new ApproverViewModel
                    {
                        DetailsId = (int)a.Details_Id,
                        MilestoneId = (int)a.Milestone_Id,
                        TaskName = db.DetailsTbls.Where(d => d.details_id == a.Details_Id).Select(d => d.process_title).FirstOrDefault(),
                        ApproverName = a.Approver_Name,
                        IsRemoved = a.IsRemoved_ ?? false
                    })
                    .ToList();

                var milestoneIds = db.MilestoneTbls
                    .Where(m => m.main_id == id)
                    .Select(m => m.milestone_id)
                    .ToList();

                var submissions = db.ChecklistSubmissions
                    .Where(cs => cs.main_id == id)
                    .ToList();

                bool allApproved = submissions.Count > 0 && submissions.All(cs => cs.is_approved == true);

                DateTime? projectEndDate = db.MainTables
                    .Where(p => p.main_id == id)
                    .Select(p => p.project_end)
                    .FirstOrDefault();

                DateTime today = DateTime.Today;

                string projectStatus;
                if (allApproved)
                {
                    projectStatus = "Completed";
                }
                else if (projectEndDate.HasValue && today <= projectEndDate.Value.Date)
                {
                    projectStatus = "Active";
                }
                else
                {
                    projectStatus = "Delayed";
                }

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
                    ProjectMembers = projectMembers,
                    Checklist = projChecklist,
                    userDetails = userInfo,
                    TaskTitle = tasks,
                    IsProjectManager = isProjectManager,
                    ProjectStatus = projectStatus,
                    IsArchived = projects.IsArchived,
                    IsReadOnlyChecklistView = isReadOnlyChecklistView,
                    milestone = id,
                    ReadOnlyApprover = userDetails.FirstName + " " + userDetails.LastName

                };
                //viewModel.IsReadOnlyChecklistView = !isProjectManager;
                bool isDivisionHeadAdmin = isODCPAdmin && userDetails.JobLevel == 4035;
                viewModel.IsReadOnlyChecklistView = !isProjectManager || isDivisionHeadAdmin;

                ViewBag.SelectedTab = tab;



                return View(viewModel);
            }
            else
            {
                return View("~/Views/Account/Login.cshtml");
            }
        }


        // new old 
        //public ActionResult weeklyMilestone(int id, string title, string projectId)
        //{
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        var userId = User.Identity.GetUserId();
        //        var userEmail = User.Identity.GetUserName();
        //        var userDetails = cmdb.AspNetUsers.Where(x => x.UserName == userEmail).SingleOrDefault();

        //        var projectList = (from p in db.ProjectMembersTbls
        //                           join m in db.MainTables on new { id = p.project_id } equals new { id = (int?)m.main_id }
        //                           where p.email == userEmail && m.isCompleted != true
        //                           select m.main_id).ToList();

        //        var userProject = db.MainTables
        //            .Where(m => m.main_id == id && projectList.Contains(m.main_id))
        //            .FirstOrDefault();

        //        if (userProject == null && userDetails.JobLevel != 4035 && userDetails.JobLevel != 4036)
        //        {
        //            return RedirectToAction("AccessDenied", "Error");
        //        }

        //        // if the user is a project manager (1004) for this project
        //        bool isProjectManager = db.ProjectMembersTbls
        //            .Any(pm => pm.project_id == id && pm.email == userEmail && pm.role == 1004);

        //        UserModel userInfo = new UserModel()
        //        {
        //            Email = userDetails.Email,
        //            JobLevel = userDetails.JobLevel,
        //            FirstName = userDetails.FirstName,
        //            LastName = userDetails.LastName
        //        };

        //        TempData["entry"] = id;
        //        TempData["title"] = title;
        //        TempData["project"] = projectId;

        //        var projects = db.MainTables
        //            .Where(m => m.main_id == id)
        //            .Select(m => new ProjectMilestoneViewModel
        //            {
        //                MainId = m.main_id,
        //                ProjectTitle = m.project_title,
        //                StartDate = m.project_start,
        //                EndDate = m.project_end,
        //                Duration = m.duration ?? 0,
        //                ProjectYear = m.year ?? 0,
        //                Division = m.division,
        //                Category = m.category,
        //                ProjectOwner = m.project_owner
        //            })
        //            .FirstOrDefault();

        //        if (projects == null)
        //        {
        //            return HttpNotFound("No milestones found.");
        //        }

        //        var projectDetails = db.MainTables
        //            .Where(p => p.main_id == id)
        //            .Select(p => new ProjectDetailViewModel
        //            {
        //                Id = p.main_id,
        //                ProjectTitle = p.project_title,
        //                ProjectStart = p.project_start.ToString(),
        //                ProjectEnd = p.project_end.ToString(),
        //                ProjectDuration = p.duration ?? 0,
        //                ProjectYear = p.year ?? 0,
        //                Division = p.division,
        //                Category = p.category,
        //                ProjectOwner = p.project_owner,
        //            })
        //            .ToList();

        //        var milestones = db.MilestoneTbls
        //            .Where(m => m.main_id == id)
        //            .Select(m => new SelectListItem
        //            {
        //                Value = m.milestone_id.ToString(),
        //                Text = m.milestone_name
        //            })
        //            .ToList();

        //        var tasks = db.DetailsTbls
        //            .Where(t => t.main_id == id)
        //            .Select(t => new SelectListItem
        //            {
        //                Value = t.details_id.ToString(),
        //                Text = t.process_title
        //            })
        //            .ToList();

        //        var statusLogs = db.WeeklyStatus
        //             .Where(log => log.main_id == id)
        //             .Select(log => new StatusLogsViewModel
        //             {
        //                 StatusId = log.status_id,
        //                // MilestoneId = log.milestone_id,
        //                 ProjectOwner = log.project_owner,
        //                 Description = log.description,
        //                 DateUpdated = log.date_updated.ToString(),
        //                 Username = log.user_id,
        //                 MilestoneName = log.milestone_name,
        //                 Attachment = db.AttachmentTables
        //                    .Where(a => a.status_id == log.status_id)
        //                    .Select(a => a.path_file)
        //                    .FirstOrDefault()
        //             })
        //             .OrderByDescending(log => log.DateUpdated)
        //             .ToList();


        //        var projectMembers = db.ProjectMembersTbls
        //            .Where(pm => pm.project_id == id)
        //            .AsEnumerable()
        //            .Select(pm => new ProjectMemberViewModel
        //            {
        //                Name = pm.name,
        //                Role = pm.role.Value,
        //                Initials = !string.IsNullOrEmpty(pm.name) && pm.name.Split(' ').Length > 2
        //                    ? pm.name.Split(' ')[0].Substring(0, 1) + pm.name.Split(' ')[1].Substring(0, 1)
        //                    : "N/A",
        //                Email = pm.email
        //            })
        //            .ToList();


        //        var projChecklist = db.ApproversTbls
        //            .Where(a => a.Main_Id == id)
        //            .Select(a => new ApproverViewModel
        //            {
        //                DetailsId = (int)a.Details_Id,
        //                MilestoneId = (int)a.Milestone_Id,
        //                TaskName = db.DetailsTbls.Where(d => d.details_id == a.Details_Id).Select(d => d.process_title).FirstOrDefault(),
        //                ApproverName = a.Approver_Name,
        //                IsRemoved = a.IsRemoved_ ?? false
        //            })
        //            .ToList();


        //        // fetch all milestone id for the project
        //        var milestoneIds = db.MilestoneTbls
        //            .Where(m => m.main_id == id)
        //            .Select(m => m.milestone_id)
        //            .ToList();

        //        // fetch all checklist submissions for the project
        //        var submissions = db.ChecklistSubmissions
        //            .Where(cs => cs.main_id == id)
        //            .ToList();

        //        // determine if all submissions are approved
        //        bool allApproved = submissions.Count > 0 && submissions.All(cs => cs.is_approved == true);


        //        DateTime? projectEndDate = db.MainTables
        //            .Where(p => p.main_id == id)
        //            .Select(p => p.project_end)
        //            .FirstOrDefault();

        //        DateTime today = DateTime.Today;


        //        string projectStatus;
        //        if (allApproved)
        //        {
        //            projectStatus = "Completed";
        //        }
        //        else if (projectEndDate.HasValue && today <= projectEndDate.Value.Date)
        //        {
        //            projectStatus = "Active";
        //        }
        //        else
        //        {
        //            projectStatus = "Delayed";
        //        }


        //        var viewModel = new ProjectMilestoneViewModel
        //        {
        //            MainId = projects.MainId,
        //            ProjectTitle = projects.ProjectTitle,
        //            StartDate = projects.StartDate,
        //            EndDate = projects.EndDate,
        //            Duration = projects.Duration,
        //            ProjectYear = projects.ProjectYear,
        //            Division = projects.Division,
        //            Category = projects.Category,
        //            ProjectOwner = projects.ProjectOwner,
        //            ProjectDetails = projectDetails,
        //            Milestones = milestones,
        //            StatusLogs = statusLogs,
        //            ProjectMembers = projectMembers,
        //            Checklist = projChecklist,
        //            userDetails = userInfo,
        //            TaskTitle = tasks,
        //            IsProjectManager = isProjectManager,
        //            ProjectStatus = projectStatus
        //        };

        //        return View(viewModel);

        //    }
        //    else
        //    {
        //        return View("~/Views/Account/Login.cshtml");
        //    }
        //}

        public JsonResult getGanttData(int id)
        {
            var currentYear = DateTime.Now.Year;
            var tasks = db.DetailsTbls
                .Where(x => x.main_id == id && x.task_start.HasValue)
                .OrderBy(x => x.milestone_id)
                .ToList();

            var milestones = db.MilestoneTbls.Where(x => x.main_id == id).OrderBy(x => x.milestone_id).ToList();

            var milestoneData = milestones.Select(x => new
            {
                id = x.excel_id,
                start_date = "",
                duration = 0,
                text = x.milestone_name,
                parent = x.parent,
                color = "",
                unscheduled = x.unscheduled,
                completed = x.IsCompleted,
                key_person = ""
            }).ToArray();

            var data = tasks.Select(x => new
            {
                id = x.excel_id,
                //start_date = x.task_start.HasValue ? x.task_start.Value.ToString("yyyy/MM/dd") : DateTime.Now.ToString("yyyy/MM/dd"),
                start_date = x.current_task_start.HasValue ? x.current_task_start.Value.ToString("yyyy/MM/dd") 
                                                           : x.task_start.HasValue 
                                                                ? x.task_start.Value.ToString("yyyy/MM/dd") 
                                                                : DateTime.Now.ToString("yyyy/MM/dd"),
                //duration = x.task_duration + (x.task_delay.Value * 7) ?? 0,
                duration = x.task_duration ?? 0,
                text = x.process_title,
                parent = x.IsSubtask == false ? x.milestone_id : x.parent,
                color = GetTaskColor(x),
                unscheduled = x.isUnscheduled,
                completed = x.isCompleted,
                key_person = x.key_person,
            }).ToArray();

            var milestoneLink = milestones.Select(x => new
            {
                id = x.excel_id,
                source = x.source,
                target = x.target,
                type = "0"
            });

            var linkData = tasks.Select(x => new
            {
                id = x.excel_id,
                source = x.source,
                target = x.target,
                type = "0"
            }).ToArray();

            var jsonData = new
            {
                tasks = milestoneData.Concat(data).ToArray(),
                links = milestoneLink.Concat(linkData).ToArray()
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

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER, PMS_PROJECT_MANAGER")]
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
            // fetch users
            var users = cmdb.AspNetUsers.Select(u => new UserModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            }).ToList();

            // fetch roles
            var roles = db.Roles.Select(r => new RoleModel
            {
                Id = r.id,
                RoleName = r.RoleName
            }).ToList();

            // fetch projects
            var projects = db.MainTables.Select(p => new ProjectModel
            {
                Id = p.main_id,
                Title = p.project_title
            }).ToList();


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
            var ownerName = "";

            var userDetails = (from u in cmdb.AspNetUsers
                               join j in cmdb.Identity_JobDescription on new { jId = u.JobId } equals new { jId = j.Id }
                               join k in cmdb.Identity_Keywords.Where(x => x.Type == "Departments") on new { department = j.DeptId } equals new { department = k.Id }
                               join kd in cmdb.Identity_Keywords.Where(x => x.Type == "Divisions") on new { division = j.DivisionId } equals new { division = kd.Id }
                               select new
                               {
                                   email = u.Email,
                                   name = u.FirstName + " " + u.MiddleName + " " + u.LastName,
                                   emp_id = u.CMId,
                                   designation = j.PositionDescription,
                                   job_level = u.JobLevel,
                                   department = k.Description,
                                   division = kd.Description
                               }).Where(x => x.email == User.Identity.Name).OrderByDescending(x => x.emp_id).FirstOrDefault();

            if (attachment == null || attachment.ContentLength <= 0)
            {
                return Json(new { message = "File is empty", status = false }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var tblJoin = (from netUser in cmdb.AspNetUsers
                               join jobDesc in cmdb.Identity_JobDescription on netUser.JobId equals jobDesc.Id
                               join idKey in cmdb.Identity_Keywords on jobDesc.DeptId equals idKey.Id
                               select new { netUser.UserName, jobDesc.DeptId, jobDesc.DivisionId, idKey.Description })
                              .Where(x => x.UserName == User.Identity.Name).ToList();

                foreach (var item in tblJoin)
                {
                    division = cmdb.Identity_Keywords.Where(x => x.Id == item.DivisionId).Select(x => x.Description).Single();
                    department = cmdb.Identity_Keywords.Where(x => x.Id == item.DeptId).Select(x => x.Description).Single();
                }

                using (var reader = new StreamReader(attachment.InputStream))
                using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture)
                {
                    MissingFieldFound = null,
                    BadDataFound = null,
                    HeaderValidated = null,
                }))
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
                            return Json(new { message = $"Error parsing CSV: Row {csv.Context.Parser.Row}: {ex.Message}", status = false }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    ownerName = exportList.FirstOrDefault()?.Owner?.Trim();

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
                                    throw new Exception("Start date, end date, duration, and year cannot be empty.");
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
                                    project_owner = ownerName,
                                    user_id = UserId
                                };

                                db.MainTables.Add(addWeeklyChecklist);
                                db.SaveChanges();
                                var temp_mainID =  addWeeklyChecklist.main_id;

                                var approver_list = db.PreSetMilestones.Where(x => x.division_string == userDetails.division).ToList();
                                var j = 0;
                                foreach (var approver in approver_list)
                                {
                                    var individual_approver = approver.Approvers.Split(',')[0];
                                    var cmdb_approver = cmdb.AspNetUsers.Where(x => x.Id == individual_approver).FirstOrDefault();

                                    PreSetMilestoneApprover presets = new PreSetMilestoneApprover()
                                    {
                                        approver_name = cmdb_approver.FirstName + " " + cmdb_approver.MiddleName + " " + cmdb_approver.LastName,
                                        approver_email = cmdb_approver.Email,
                                        main_id = temp_mainID,
                                        milestone_id = approver.MilestoneID,
                                        date_added = DateTime.Now,
                                        added_by = User.Identity.Name,
                                        division = userDetails.division,
                                        employee_id = Int32.Parse(userDetails.emp_id)

                                    };

                                    db.PreSetMilestoneApprovers.Add(presets);
                                    j++;
                                }

                                db.SaveChanges();

                                var mainIDfordetails = addWeeklyChecklist.main_id;

                                //var milestones = exportList
                                //    .Where(x => x.ProjectTitle == getProject.ProjectTitle)
                                //    .GroupBy(x => x.MilestoneName)
                                //    .Select(x => x.First())
                                //    .ToList();
                                var milestones = exportList
                                    .Where(x => x.ProjectTitle == getProject.ProjectTitle)
                                    .GroupBy(x => x.Process)
                                    .Select(x => x.First())
                                    .ToList();

                                List<MilestoneTbl> milestoneList = new List<MilestoneTbl>();

                                foreach (var content in milestones)
                                {
                                    var addMilestone = new MilestoneTbl
                                    {
                                        milestone_name = content.Process,
                                        main_id = addWeeklyChecklist.main_id,
                                        created_date = DateTime.Now,
                                        milestone_position = content.Sequence,
                                        IsCompleted = false,
                                        unscheduled = false,
                                        root_id = db.MilestoneRoots.Where(x => x.milestone_name == content.Process).Select(x => x.id).FirstOrDefault()
                                    };
                                    milestoneList.Add(addMilestone);
                                }
                                var newlyAdded = db.MilestoneTbls.AddRange(milestoneList);
                                db.SaveChanges();

                                newlyAdded.ToList();

                                List<DetailsTbl> detailList = new List<DetailsTbl>();
                                foreach (var milestone in milestoneList)
                                {
                                    //var groupedTasks = exportList
                                    //    .Where(x => x.MilestoneName == milestone.milestone_name)
                                    //    .ToList();
                                    var groupedTasks = exportList
                                        .Where(x => x.Process == milestone.milestone_name)
                                        .OrderBy(x => x.Sequence)
                                        .ToList();

                                    foreach (var taskGroup in groupedTasks)
                                    {

                                        DateTime taskStartDate = DateTime.ParseExact(taskGroup.TaskStart.ToString(), dateFormat, CultureInfo.InvariantCulture);
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
                                            task_end = taskStartDate.AddDays(taskDuration),
                                            task_duration = taskGroup.task_duration,
                                            source = taskGroup.Source,
                                            target = taskGroup.Target,
                                            parent = taskGroup.Parent,
                                            created_date = DateTime.Now.ToLocalTime(),
                                            IsSubtask = subtask,
                                            main_id = mainIDfordetails,
                                            excel_id = taskGroup.id,
                                            task_delay = 0
                                        };

                                        detailList.Add(addTask);
                                    }

                                    //checklist table insert
                                    ChecklistTable checklist_tbl = new ChecklistTable
                                    {
                                        main_id = mainIDfordetails,
                                        milestone_id = milestone.root_id
                                    };

                                    db.ChecklistTables.Add(checklist_tbl);
                                    db.SaveChanges();
                                }

                                db.DetailsTbls.AddRange(detailList);
                                db.SaveChanges();

                                var completionColumn = db.MilestoneTbls.Where(x => x.main_id == mainIDfordetails).ToList();
                                var latestTask = db.DetailsTbls
                                    .Where(x => x.main_id == mainIDfordetails)
                                    .GroupBy(x => x.milestone_id)
                                    .Select(x => x.OrderByDescending(y => y.task_end).FirstOrDefault())
                                    .ToList();

                                if (completionColumn.Count() == latestTask.Count())
                                {
                                    for (var i = 0; i < completionColumn.Count(); i++)
                                    {
                                        completionColumn[i].completion_date = latestTask[i].task_end;
                                        db.SaveChanges();
                                    }

                                    var upd_rgstr = db.RegistrationTbls.Where(x => x.registration_id == projectId).Single();
                                    upd_rgstr.is_file_uploaded = true;
                                    db.SaveChanges();

                                    

                                    Activity_Log logs = new Activity_Log
                                    {
                                        username = User.Identity.Name,
                                        datetime_performed = DateTime.Now,
                                        action_level = 5,
                                        action = "Project Upload",
                                        description = getProject.ProjectTitle + " Project Uploaded by: " + ownerName + " For Year: " + getProject.ProjectYear,
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
                                    transaction.Rollback();
                                    message = "Completion date column error.";
                                }
                            }
                            else
                            {
                                message = "Invalid. Project title and Project name must be the same. Please try again.";
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

            return Json(new { message = message, status = status, owner = ownerName }, JsonRequestBehavior.AllowGet);
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


        //[HttpPost]
        //public ActionResult UpdateStatus(ProjectMilestoneViewModel model)
        //{
        //    var err = ModelState.Values.SelectMany(v => v.Errors);

        //    if (ModelState.IsValid)
        //    {
        //        var milestone = db.MilestoneTbls.Find(int.Parse(model.SelectedMilestone));
        //        if (milestone != null)
        //        {

        //            int projectId = (int)milestone.main_id;
        //            string userId = User.Identity.GetUserId();

        //            var statusUpdate = new WeeklyStatu
        //            {
        //                milestone_id = milestone.milestone_id,
        //                description = model.StatusUpdate,
        //                date_updated = DateTime.Now,
        //                main_id = projectId,
        //                user_id = User.Identity.Name,
        //                milestone_name = milestone.milestone_name


        //            };
        //            db.WeeklyStatus.Add(statusUpdate);
        //            db.SaveChanges();

        //             Save file if uploaded
        //            if (model.FileUpload != null && model.FileUpload.ContentLength > 0)
        //            {
        //                string uploadPath = Server.MapPath("~/Uploads");
        //                if (!Directory.Exists(uploadPath))
        //                {
        //                    Directory.CreateDirectory(uploadPath);
        //                }

        //                string filePath = Path.Combine(uploadPath, Path.GetFileName(model.FileUpload.FileName));
        //                model.FileUpload.SaveAs(filePath);

        //                var attachment = new AttachmentTable
        //                {
        //                    status_id = statusUpdate.status_id,
        //                    path_file = filePath
        //                };

        //                db.AttachmentTables.Add(attachment);
        //                db.SaveChanges();
        //            }

        //            var activityLog = new Activity_Log
        //            {
        //                log_id = projectId,
        //                username = User.Identity.Name,
        //                datetime_performed = DateTime.Now,
        //                action_level = 1,
        //                action = "Status Update",
        //                description = $"Updated status for milestone: {milestone.milestone_name}.",
        //                department = "ITS",
        //                division = model.Division

        //            };

        //            db.Activity_Log.Add(activityLog);
        //            db.SaveChanges();

        //            TempData["StatusUpdated"] = true;

        //            return RedirectToAction("weeklyMilestone", new { id = projectId, title = TempData["title"], projectId = TempData["project"] });
        //        }

        //        ModelState.AddModelError("", "Milestone not found.");
        //        return View(model);
        //    }
        //    return View(model);
        //}

        //------------------------Activity Log Viewing----------------------------

        [HttpPost]
        public ActionResult UpdateStatus(ProjectMilestoneViewModel model, HttpPostedFileBase attachment)
        {
            if (ModelState.IsValid)
            {
                var taskId = Int32.Parse(model.SelectedMilestone);
                var task = db.DetailsTbls.Find(taskId);
                if (task != null)
                {

                    int projectId = (int)task.main_id;
                    string userId = User.Identity.GetUserId();

                    if (model.isDelayed == true)
                    {
                        var deetsDB = db.DetailsTbls.Where(x => x.details_id == taskId).ToList().SingleOrDefault();
                        var mainId = deetsDB.main_id;
                        var excelId = deetsDB.excel_id;
                        deetsDB.task_delay = model.delay;

                        var deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault() != null ? db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault() : deetsDB;

                        if (deetsDB.parent != null)
                        {
                            //var deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault();
                            while (deetsParent != null)
                            {
                                deetsParent.task_delay = model.delay;

                                if (deetsParent.parent != null)
                                {
                                    deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsParent.parent && x.main_id == mainId).ToList().SingleOrDefault();
                                    db.SaveChanges();
                                }

                                else
                                {
                                    break;
                                }

                            }
                        }

                        //var targetList = db.DetailsTbls.Where(x => x.source == excelId && x.main_id == mainId).ToList();
                        //var tempList = db.DetailsTbls.Where(x => x.source == excelId && x.main_id == mainId).ToList();
                        //var ctr = targetList.Count;

                        //tempList.Clear();
                        //tempList.TrimExcess();

                        //while(ctr > 0)
                        //{
                        //    foreach (var item in targetList)
                        //    {
                        //        item.task_delay = model.delay;

                        //        if (db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).SingleOrDefault() != null)
                        //        {
                        //            tempList.Add(db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).SingleOrDefault());
                        //        }

                        //        db.SaveChanges();
                        //    }

                        //    ctr = tempList.Count;
                        //    targetList = tempList.ToList();

                        //    tempList.Clear();
                        //    tempList.TrimExcess();
                        //}

                        /* ------------------------------------------------------------------------- */
                        var childTasks = db.DetailsTbls.Where(x => x.source == deetsParent.excel_id && x.main_id == mainId).ToList();
                        var tempChild = db.DetailsTbls.Where(x => x.parent == null && x.main_id == mainId).ToList();
                        var ctr = childTasks.Count;

                        tempChild.Clear();
                        tempChild.TrimExcess();

                        while (ctr > 0)
                        {
                            foreach (var item in childTasks)
                            {
                                item.task_delay = deetsParent.task_delay;

                                if (db.DetailsTbls.Where(x => x.source == item.excel_id).Count() > 0)
                                {
                                    tempChild.AddRange(db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).ToList());
                                }

                                db.SaveChanges();
                            }

                            ctr = tempChild.Count;
                            childTasks = tempChild.ToList();

                            tempChild.Clear();
                            tempChild.TrimExcess();
                        }
                        /* ------------------------------------------------------------------------- */

                        var milestoneParent = db.MilestoneTbls.Where(x => x.milestone_id == deetsParent.milestone_id).Single();

                        milestoneParent.current_completion_date = db.DetailsTbls.Where(x => x.milestone_id == milestoneParent.milestone_id).OrderByDescending(x => x.current_task_end).Select(x => x.current_task_end).First();

                        var milestoneChild = db.MilestoneTbls.Where(x => x.main_id == milestoneParent.main_id && x.milestone_id != milestoneParent.milestone_id && x.source == milestoneParent.milestone_id).Single();

                        while (milestoneChild != null)
                        {

                            //milestoneChild.current_completion_date = milestoneChild.completion_date.Value.AddDays(model.delay);
                            milestoneChild.current_completion_date = milestoneChild.completion_date.Value.AddDays(model.delay * 7);

                            foreach (var milestoneTasks in db.DetailsTbls.Where(x => x.milestone_id == milestoneChild.milestone_id))
                            {
                                milestoneTasks.task_delay = model.delay;
                            }

                            milestoneChild = db.MilestoneTbls.Where(x => x.main_id == milestoneChild.main_id && x.milestone_id != milestoneChild.milestone_id && x.source == milestoneChild.milestone_id).SingleOrDefault();

                        }
                    }

                    else
                    {
                        var deetsDB = db.DetailsTbls.Where(x => x.details_id == taskId).ToList().SingleOrDefault();
                        var mainId = deetsDB.main_id;
                        var excelId = deetsDB.excel_id;
                        deetsDB.task_delay = model.delay;

                        var deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault() != null ? db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault() : deetsDB;

                        if (deetsDB.parent != null)
                        {
                            //var deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsDB.parent && x.main_id == mainId).ToList().SingleOrDefault();
                            while (deetsParent != null)
                            {
                                deetsParent.task_delay = model.delay;

                                if (deetsParent.parent != null)
                                {
                                    deetsParent = db.DetailsTbls.Where(x => x.excel_id == deetsParent.parent && x.main_id == mainId).ToList().SingleOrDefault();
                                    db.SaveChanges();
                                }

                                else
                                {
                                    break;
                                }

                            }
                        }

                        //var targetList = db.DetailsTbls.Where(x => x.source == excelId && x.main_id == mainId).ToList();
                        //var tempList = db.DetailsTbls.Where(x => x.source == excelId && x.main_id == mainId).ToList();
                        //var ctr = targetList.Count;

                        //tempList.Clear();
                        //tempList.TrimExcess();

                        //while(ctr > 0)
                        //{
                        //    foreach (var item in targetList)
                        //    {
                        //        item.task_delay = model.delay;

                        //        if (db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).SingleOrDefault() != null)
                        //        {
                        //            tempList.Add(db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).SingleOrDefault());
                        //        }

                        //        db.SaveChanges();
                        //    }

                        //    ctr = tempList.Count;
                        //    targetList = tempList.ToList();

                        //    tempList.Clear();
                        //    tempList.TrimExcess();
                        //}

                        /* ------------------------------------------------------------------------- */
                        var childTasks = db.DetailsTbls.Where(x => x.source == deetsParent.excel_id && x.main_id == mainId).ToList();
                        var tempChild = db.DetailsTbls.Where(x => x.parent == null && x.main_id == mainId).ToList();
                        var ctr = childTasks.Count;

                        tempChild.Clear();
                        tempChild.TrimExcess();

                        while (ctr > 0)
                        {
                            foreach (var item in childTasks)
                            {
                                item.task_delay = deetsParent.task_delay;

                                if (db.DetailsTbls.Where(x => x.source == item.excel_id).Count() > 0)
                                {
                                    tempChild.AddRange(db.DetailsTbls.Where(x => x.source == item.excel_id && x.main_id == mainId).ToList());
                                }

                                db.SaveChanges();
                            }

                            ctr = tempChild.Count;
                            childTasks = tempChild.ToList();

                            tempChild.Clear();
                            tempChild.TrimExcess();
                        }
                        /* ------------------------------------------------------------------------- */

                        var milestoneParent = db.MilestoneTbls.Where(x => x.milestone_id == deetsParent.milestone_id).Single();

                        milestoneParent.current_completion_date = db.DetailsTbls.Where(x => x.milestone_id == milestoneParent.milestone_id).OrderByDescending(x => x.current_task_end).Select(x => x.current_task_end).First();

                        var milestoneChild = db.MilestoneTbls.Where(x => x.main_id == milestoneParent.main_id && x.milestone_id != milestoneParent.milestone_id && x.source == milestoneParent.milestone_id).Single();

                        while (milestoneChild != null)
                        {

                            milestoneChild.current_completion_date = milestoneChild.completion_date.Value.AddDays(model.delay);

                            foreach (var milestoneTasks in db.DetailsTbls.Where(x => x.milestone_id == milestoneChild.milestone_id))
                            {
                                milestoneTasks.task_delay = model.delay;
                            }

                            milestoneChild = db.MilestoneTbls.Where(x => x.main_id == milestoneChild.main_id && x.milestone_id != milestoneChild.milestone_id && x.source == milestoneChild.milestone_id).SingleOrDefault();

                        }
                    }

                    var statusUpdate = new WeeklyStatu
                    {
                        details_id = taskId,
                        description = model.StatusUpdate,
                        date_updated = DateTime.Now,
                        main_id = projectId,
                        user_id = User.Identity.Name,
                        milestone_name = task.process_title

                    };
                    db.WeeklyStatus.Add(statusUpdate);
                    db.SaveChanges();

                    // Save file if uploaded
                    if (attachment != null && attachment.ContentLength > 0)
                    {
                        string uploadPath = Server.MapPath("~/Uploads");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        string filePath = Path.Combine(uploadPath, Path.GetFileName(model.FileUpload.FileName));
                        model.FileUpload.SaveAs(filePath);

                        var attachment_tbl = new AttachmentTable
                        {
                            status_id = statusUpdate.status_id,
                            path_file = filePath.Replace("c:\\users\\jyparraguirre\\source\\repos\\ProjectManagementSystem\\ProjectManagementSystem", ""),
                            details_id = taskId
                        };

                        db.AttachmentTables.Add(attachment_tbl);
                        db.SaveChanges();
                    }

                    var activityLog = new Activity_Log
                    {
                        log_id = projectId,
                        username = User.Identity.Name,
                        datetime_performed = DateTime.Now,
                        action_level = 1,
                        action = "Status Update",
                        description = $"Updated status for milestone: {task.process_title}.",
                        department = "ITS",
                        division = "TEST"

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


       //  ------------------------Activity Log Viewing----------------------------//
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
            var intProject = Int32.Parse(project);
            var title = db.MainTables.Where(x => x.main_id == intProject).Select(x => x.project_title).SingleOrDefault();
            var userDivision = (from u in cmdb.AspNetUsers
                                join j in cmdb.Identity_JobDescription on new { jobid = u.JobId, uname = u.UserName } equals new { jobid = j.Id, uname = User.Identity.Name }
                                join i in cmdb.Identity_Keywords on j.DivisionId equals i.Id
                                where i.Type == "Divisions"
                                select i.Description).FirstOrDefault();

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
                                <div style='font-family: Poppins, Arial, sans-serif; background-color: #f3f4f7; padding: 40px 20px;'>
                                    <div style='max-width: 600px; margin: auto; background-color: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 20px rgba(0,0,0,0.05);'>

                                        <!-- Header Section -->
                                        <div style='background-color: #66339A; color: #fff; padding: 30px 40px; text-align: center;'>
                                            <h1 style='margin: 0; font-size: 24px;'>You're In!</h1>
                                            <p style='margin: 10px 0 0; font-size: 16px;'>Welcome to the <strong>" + userProject + @"</strong> Project</p>
                                        </div>

                                        <!-- Body Section -->
                                        <div style='padding: 30px 40px;'>
                                            <h2 style='color: #333; font-size: 18px; margin-top: 0;'>Hello, " + userName + @"</h2>
                                            <p style='color: #555; font-size: 14px; margin-bottom: 20px;'>
                                                You've officially been added to <strong>" + userProject + @"</strong> as a <strong>" + userRole + @"</strong>. We're excited to have you join the team.
                                            </p>

                                            <div style='background-color: #f7f2fc; padding: 20px; border-left: 5px solid #66339A; border-radius: 8px; margin-bottom: 25px;'>
                                                <p style='margin: 0; color: #444; font-size: 14px;'>
                                                    Tip: You can now view your tasks, track milestones, and start collaborating with your project team.
                                                </p>
                                            </div>

                                            <div style='text-align: center; margin-bottom: 30px;'>
                                                <a href='http://localhost:60297/Checklist/weeklyMilestone?id=" + project + @"&title=" + title + @"'
                                                   style='padding: 14px 30px; background-color: #66339A; color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 6px; font-size: 15px; display: inline-block;'>
                                                   Open Project Dashboard
                                                </a>
                                            </div>

                                            <p style='font-size: 13px; color: #888; text-align: center;'>
                                                Need help getting started? Reach out to the ITS team loc. 132
                                            </p>
                                        </div>

                                        <!-- Footer Section -->
                                        <div style='background-color: #f1f1f5; text-align: center; padding: 15px; font-size: 12px; color: #888;'>
                                            This is an automated message from the Project Management System.<br/>
                                            For assistance, contact ITS at <strong>LOCAL: 132</strong>.
                                        </div>
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
                                Division = userDivision,
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
                            //transaction.Commit();
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
                transaction.Commit();
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
        public JsonResult GetMilestonesWithApprovalTasks(int mainId)
        {
            try
            {
                //var milestones = db.MilestoneTbls
                //    .Where(m => m.main_id == mainId)
                //    .Select(m => new
                //    {
                //        MilestoneId = m.milestone_id,
                //        MilestoneName = m.milestone_name,
                //        Tasks = db.DetailsTbls
                //            .Where(t => t.milestone_id == m.milestone_id && t.RequiresApproval == true)
                //            .Select(t => new
                //            {
                //                TaskId = t.details_id,
                //                TaskName = t.process_title,
                //                RequiresApproval = t.RequiresApproval,
                //                IsApproved = t.IsApproved,
                //                Approver = t.key_person
                //            }).ToList()
                //    }).ToList();

                //List<dynamic> milestoneList = new List<dynamic>();

                var milestones = db.MilestoneTbls
                    .Where(m => m.main_id == mainId)
                    .Select(m => new
                    {
                        MilestoneId = m.root_id,
                        MilestoneName = m.milestone_name,
                        isCompleted = m.IsCompleted
                    }).ToList();

                //foreach(var row in milestones)
                //{
                //    var getpreset = db.PreSetMilestoneApprovers
                //         .Where(x => x.main_id == mainId && x.milestone_id == row.MilestoneId)
                //         .ToList();

                //    var getoptional = db.OptionalMilestoneApprovers
                //        .Where(x => x.main_id == mainId && x.milestone_id == row.MilestoneId)
                //        .ToList();

                //    bool milestoneCompleted = false;

                //    // Only check lists if they contain items
                //    bool allPresetApproved = getpreset.Count == 0 || getpreset.All(x => x.approved == true);
                //    bool allOptionalApproved = getoptional.Count == 0 || getoptional.All(x => x.approved == true);

                //    // If both are either empty or fully approved, milestone is completed
                //    if (allPresetApproved && allOptionalApproved)
                //    {
                //        milestoneCompleted = true;
                //    }

                //    // Add to milestoneList
                //    milestoneList.Add(new
                //    {
                //        row.MilestoneId,
                //        row.MilestoneName,
                //        milestoneCompleted
                //    });
                //}

                return Json(new { success = true, data = milestones }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetTasksForMilestone(int milestoneId, int mainId, string division)
        {
            try
            {
                var ctr = 0;

                List<TaskContainerModel> container = new List<TaskContainerModel>();

                var tasks = db.OptionalMilestones.Where(x => x.milestone_id == milestoneId && x.main_id == mainId && x.division == division && x.is_removed != true).ToList();

                var submissions = db.ChecklistSubmissions.Where(x => x.main_id == mainId && x.milestone_id == milestoneId).ToList();

                var checklistGrp_id = db.ChecklistTables.Where(x => x.main_id == mainId && x.milestone_id == milestoneId).Select(x => x.checklist_id).FirstOrDefault();

                foreach (var item in tasks)
                {

                    if (submissions.Any(x => x.main_id == item.main_id && x.task_id == item.id && x.milestone_id == milestoneId))
                    {

                        var submissionContainer = submissions.Where(x => x.main_id == item.main_id && x.task_id == item.id && x.milestone_id == milestoneId).OrderByDescending(x => x.submission_date).Select(x => new
                        {
                            taskname = x.task_name,
                            approved = x.is_approved,
                            approver_status = db.OptionalMilestoneApprovers.Where(y => y.task_id == x.task_id).Select(y => y.approved).ToList(),
                            approvers = db.OptionalMilestoneApprovers.Where(y => y.task_id == x.task_id && y.main_id == item.main_id && x.milestone_id == milestoneId && y.is_removed != true).Select(y => y.approver_name).ToList(),
                            task_id = x.task_id,
                            milestone_id = x.milestone_id,
                            project_id = x.main_id,
                            attachment = x.filepath,
                            reason = x.disapproval_reason,
                            approval_enabled = x.approval_enabled
                        }).First();

                        TaskContainerModel temporary = new TaskContainerModel()
                        {
                            taskname = submissionContainer.taskname,
                            approved = submissionContainer.approved,
                            approvers = submissionContainer.approvers,
                            task_id = submissionContainer.task_id,
                            milestone_id = submissionContainer.milestone_id,
                            project_id = submissionContainer.project_id,
                            attachment = submissionContainer.attachment,
                            reason = submissionContainer.reason,
                            approver_status = submissionContainer.approver_status,
                            optFlag = true,
                            approval_enabled = submissionContainer.approval_enabled,
                            submissionFlag = true
                        };

                        container.Add(temporary);
                    }
                    else
                    {
                        var optionalTasks = db.OptionalMilestones
                            .Where(x => x.milestone_id == item.milestone_id && x.id == item.id && x.is_removed != true && x.main_id == mainId)
                            .Select(x => new
                            {
                                taskname = x.task,
                                approved = x.approved,
                                approvers = db.OptionalMilestoneApprovers.Where(y => y.task_id == item.id && y.main_id == item.main_id && x.milestone_id == milestoneId  && y.is_removed != true).Select(y => y.approver_name).ToList(),
                                approver_status = db.OptionalMilestoneApprovers.Where(y => y.task_id == item.id).Select(y => y.approved).ToList(),
                                task_id = x.id,
                                milestone_id = x.milestone_id,
                                project_id = x.main_id
                            }).Single();

                        TaskContainerModel temporary = new TaskContainerModel()
                        {
                            taskname = optionalTasks.taskname,
                            approved = optionalTasks.approved,
                            approvers = optionalTasks.approvers,
                            task_id = optionalTasks.task_id,
                            milestone_id = optionalTasks.milestone_id,
                            project_id = optionalTasks.project_id,
                            attachment = null,
                            reason = null,
                            approver_status = optionalTasks.approver_status,
                            optFlag = true,
                            submissionFlag = false
                        };

                        container.Add(temporary);

                    }
                }

                var presetTasks = db.PreSetMilestones.Where(x => x.MilestoneID == milestoneId && x.division_string == division).ToList();

                foreach (var preset in presetTasks)
                {
                    if (submissions.Any(x => x.task_id == preset.ID))
                    {

                        var submissionContainer = submissions.Where(x => x.task_id == preset.ID).OrderByDescending(x => x.submission_date).Select(x => new
                        {
                            taskname = x.task_name,
                            approved = x.is_approved,
                            approver_status = db.PreSetMilestoneApprovers.Where(y => y.main_id == x.main_id && y.milestone_id == x.milestone_id && y.task_id == preset.ID).Select(y => y.approved).ToList(),
                            approvers = db.PreSetMilestoneApprovers.Where(y => y.milestone_id == milestoneId && y.main_id == mainId && y.is_removed != true && y.task_id == preset.ID).Select(y => y.approver_name).ToList(),
                            task_id = x.task_id,
                            milestone_id = x.milestone_id,
                            project_id = x.main_id,
                            attachment = x.filepath,
                            reason = x.disapproval_reason,
                            approval_enabled = x.approval_enabled
                        }).First();

                        TaskContainerModel temporary = new TaskContainerModel()
                        {
                            taskname = submissionContainer.taskname,
                            approved = submissionContainer.approved,
                            approvers = submissionContainer.approvers,
                            task_id = submissionContainer.task_id,
                            milestone_id = submissionContainer.milestone_id,
                            project_id = submissionContainer.project_id,
                            attachment = submissionContainer.attachment,
                            reason = submissionContainer.reason,
                            approver_status = submissionContainer.approver_status,
                            optFlag = false,
                            approval_enabled = submissionContainer.approval_enabled,
                            submissionFlag = true
                        };

                        container.Add(temporary);
                    }

                    else
                    {
                        TaskContainerModel preset_container = new TaskContainerModel()
                        {
                            taskname = preset.Requirements,
                            approvers = db.PreSetMilestoneApprovers.Where(x => x.milestone_id == milestoneId && x.main_id == mainId && x.task_id == preset.ID && x.is_removed != true).Select(x => x.approver_name).ToList(),
                            task_id = preset.ID,
                            milestone_id = preset.MilestoneID,
                            approver_status = db.ChecklistSubmissions.Where(x => x.task_id == milestoneId).Select(x => x.is_approved).ToList(),
                            optFlag = false,
                            submissionFlag = false
                        };

                        container.Add(preset_container);
                    }
                    
                }

                var checklist_for_approval = false;

                foreach(var item in container)
                {
                    if (item.approval_enabled != true)
                    {
                        checklist_for_approval = false;
                    }

                    else
                    {
                        checklist_for_approval = true;
                    }
                }


                return Json(new { success = true, data = container, id = checklistGrp_id, flag = checklist_for_approval }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetProjectOwner(int projectId)
        {
            // fetch project and the registered_by
            var project = db.RegistrationTbls
                .Where(x => x.registration_id == projectId)
                .Select(x => new { x.registered_by }) // sa registered_by column, email laman neto
                .SingleOrDefault();

            if (project == null)
            {
                return Json(new { success = false, message = "Project not found." }, JsonRequestBehavior.AllowGet);
            }

            var registeredByEmail = project.registered_by?.Trim();

            var owner = cmdb.AspNetUsers
                .Where(u => u.Email.Trim().ToLower() == registeredByEmail.ToLower())
                .Select(u => new { u.FirstName, u.LastName, u.Email })
                .FirstOrDefault();

            if (owner == null)
            {
                Debug.WriteLine($"No owner found with Email: {registeredByEmail}");
                return Json(new { success = false, message = "Owner not found." }, JsonRequestBehavior.AllowGet);
            }

            var ownerFullName = $"{owner.FirstName} {owner.LastName}";
            return Json(new { success = true, owner = new { FullName = ownerFullName, owner.Email } }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTaskUpdate(int selection)
        {
            var task = db.DetailsTbls.Where(x => x.details_id == selection).SingleOrDefault();
            var success = false;
            var message = "";
            var delay_week = 0;

            try
            {
                if (task.task_delay != 0)
                {
                    delay_week = task.task_delay.Value;
                }

                success = true;
            }

            catch (Exception e)
            {
                message = e.Message;
            }


            return Json(new { success = success, data = delay_week, message = message }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddOptional(ChecklistForm formData)
        {
            var message = "";

            try
            {
                var userDetails = (from u in cmdb.AspNetUsers.Where(x => x.Email == User.Identity.Name)
                                   join j in cmdb.Identity_JobDescription on new { jId = u.JobId } equals new { jId = j.Id }
                                   join k in cmdb.Identity_Keywords on new { division = j.DivisionId } equals new { division = k.Id }
                                   select new
                                   {
                                       email = u.Email,
                                       name = u.FirstName + " " + u.MiddleName + " " + u.LastName,
                                       emp_id = u.CMId,
                                       division = k.Description,
                                       designation = j.PositionDescription
                                   }).FirstOrDefault();

                List<OptionalMilestone> optional_list = new List<OptionalMilestone>();

                foreach (var item in formData.item)
                {
                    OptionalMilestone optional = new OptionalMilestone()
                    {
                        main_id = formData.project_id,
                        milestone_id = formData.milestone_id,
                        division = userDetails.division,
                        task = item.title,
                        description = item.description,
                        date_created = DateTime.Now,
                        created_by = userDetails.name

                    };

                    optional_list.Add(optional);
                }
                db.OptionalMilestones.AddRange(optional_list);
                db.SaveChanges();
                message = "success";
            }

            catch (Exception e)
            {
                message = e.Message;
            }

            return Json(new { message = message }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserList()
        {
            List<UserModel> userlist = new List<UserModel>();
            var fetch_users = cmdb.AspNetUsers.Select(x => new { x.CMId, x.FirstName, x.LastName, x.Email, x.JobLevel }).ToList();

            foreach (var item in fetch_users)
            {
                UserModel user = new UserModel()
                {
                    Id = item.CMId,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Email = item.Email,
                    JobLevel = item.JobLevel

                };
                userlist.Add(user);
            }

            return Json(new { message = "success", data = userlist }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AddApproverOpt(int id, int proj_id, List<int> approvers, int milestone_id)
        {
            var message = ""; 
            try
            {
                List<OptionalMilestoneApprover> approver_list = new List<OptionalMilestoneApprover>();

                var userDetails = (from u in cmdb.AspNetUsers
                                   join j in cmdb.Identity_JobDescription on new { jId = u.JobId } equals new { jId = j.Id }
                                   join k in cmdb.Identity_Keywords on new { division = j.DivisionId } equals new { division = k.Id }
                                   select new
                                   {
                                       email = u.Email,
                                       name = u.FirstName + " " + u.LastName,
                                       emp_id = u.CMId,
                                       division = k.Description,
                                       designation = j.PositionDescription
                                   }).ToList();

                foreach(var item in approvers)
                {
                    var current_approver = userDetails.Where(x => Int32.Parse(x.emp_id) == item).FirstOrDefault();
                    OptionalMilestoneApprover approver = new OptionalMilestoneApprover() {
                        approver_name = current_approver.name,
                        approver_email = current_approver.email,
                        main_id = proj_id,
                        milestone_id = milestone_id,
                        task_id = id,
                        date_added = DateTime.Now,
                        added_by = User.Identity.Name,
                        division = userDetails.Where(x => x.email == User.Identity.Name).Select(x => x.division).FirstOrDefault(),
                        employee_id = Int32.Parse(userDetails.Where(x => x.emp_id == current_approver.emp_id).Select(x => x.emp_id).FirstOrDefault())
                    };

                    approver_list.Add(approver);
                }
                db.OptionalMilestoneApprovers.AddRange(approver_list);
                db.SaveChanges();

                return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
            }

            catch(Exception e)
            {
                message = e.Message;

                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetApprover(int main_id, int milestone_id)
        {
            var message = "";
            try
            {
                var approvers = db.OptionalMilestoneApprovers.Where(x => x.main_id == main_id && x.task_id == milestone_id && x.is_removed != true).ToList();
                List<OptionalTaskApprover> approver_containerlist = new List<OptionalTaskApprover>();

                foreach(var item in approvers)
                {
                    OptionalTaskApprover approver_container = new OptionalTaskApprover()
                    {
                        approver_name = item.approver_name,
                        email = item.approver_email,
                        main_id = item.main_id.Value,
                        milestone_id = item.milestone_id.Value,
                        date_added = item.date_added.Value,
                        added_by = item.added_by,
                        employee_id = item.employee_id.Value
                    };

                    approver_containerlist.Add(approver_container);
                }

                return Json(new { message = "success", data = approver_containerlist }, JsonRequestBehavior.AllowGet);
            }

            catch(Exception e)
            {
                message = e.Message;

                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult RemoveOptionalApprover(int id, int project_id, int milestone_id)
        {
            var message = "";

            try
            {
                //var dbSubmission = db.ChecklistSubmissions.Where(x => x.main_id == project_id && x.milestone_id == milestone_id && x.is_removed != true).FirstOrDefault();
                //dbSubmission.is_removed = true;

                var dbItem = db.OptionalMilestoneApprovers.Where(x => x.main_id == project_id && x.task_id == milestone_id && x.employee_id == id && x.is_removed != true).FirstOrDefault();
                dbItem.is_removed = true;
                dbItem.removed_by = User.Identity.Name;
                dbItem.date_removed = DateTime.Now;

                db.SaveChanges();
                return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception e)
            {
                message = e.Message;

                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult RemoveOptionalTask(int id)
        {
            var message = "";

            try
            {
                var optionalSubmission = db.ChecklistSubmissions.Where(x => x.task_id == id && x.type == "optional").FirstOrDefault();
                if(optionalSubmission != null)
                {
                    optionalSubmission.is_removed = true;
                }

                var dbOptional = db.OptionalMilestones.Where(x => x.id == id).SingleOrDefault();
                dbOptional.removed_by = User.Identity.Name;
                dbOptional.is_removed = true;
                dbOptional.date_removed = DateTime.Now;

                var dbOptionalApprovers = db.OptionalMilestoneApprovers.Where(x => x.task_id == id).ToList();
                foreach(var approver in dbOptionalApprovers)
                {
                    approver.is_removed = true;
                    approver.date_removed = DateTime.Now;
                    approver.removed_by = User.Identity.Name;
                }

                db.SaveChanges();

                return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
            }

            catch(Exception e)
            {
                message = e.Message;
            }

            return Json(new { message = message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GanttChart(int id)
        {
            ViewBag.ProjectId = id;
            return View();
        }

        public JsonResult NotifyApprovers(int cID, int pID, int mID)
        {
            try
            {
                var dbChecklist = (from c in db.ChecklistTables.Where(x => x.checklist_id == cID)
                                   join s in db.ChecklistSubmissions.Where(x => x.is_removed != true && x.is_approved != true) 
                                   on new { mainID = c.main_id, milestoneID = c.milestone_id} equals new { mainID = s.main_id, milestoneID = s.milestone_id}
                                   select new { submissions = s, checklist = c }).ToList();
                //error here
                var updateChecklist = dbChecklist.FirstOrDefault().checklist;
                updateChecklist.for_approval = true;

                foreach (var checklist in dbChecklist)
                {
                    checklist.submissions.approval_enabled = true;
                }

                db.SaveChanges();

                var optionalApprovers = (from s in db.ChecklistSubmissions.Where(x => x.is_removed != true && x.approval_enabled == true && x.type == "optional" && x.main_id == pID && x.milestone_id == mID)
                                         join o in db.OptionalMilestoneApprovers.Where(x => x.is_removed != true && x.main_id == pID && x.milestone_id == mID) on new { taskID = s.task_id, } equals new { taskID = o.task_id }
                                         select o).ToList();

                var presetApprovers = (from s in db.ChecklistSubmissions.Where(x => x.is_removed != true && x.approval_enabled == true && x.type == "preset" && x.main_id == pID && x.milestone_id == mID)
                                       join o in db.PreSetMilestoneApprovers.Where(x => x.is_removed != true && x.main_id == pID && x.milestone_id == mID) on new { taskID = s.task_id, } equals new { taskID = o.task_id }
                                       select o).ToList();

                //optional approver email notification
                foreach (var approver in optionalApprovers)
                {
                    var projectTitle = db.MainTables.Where(x => x.main_id == pID).Select(x => x.project_title).SingleOrDefault();
                    var milestoneTitle = db.MilestoneRoots.Where(x => x.id == mID).Select(x => x.milestone_name).SingleOrDefault();

                    var systemEmail = "e-notify@enchantedkingdom.ph";
                    var systemName = "PM SYSTEM";
                    var email = new MimeMessage();

                    email.From.Add(new MailboxAddress(systemName, systemEmail));
                    email.To.Add(new MailboxAddress(approver.approver_name, approver.approver_email));

                    email.Subject = "PM System Pending Approval";
                    email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                    {
                        Text = @"
                            <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                <div style='text-align: center; margin-bottom: 20px;'>
                                </div>
                                <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                    <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + approver.approver_name + @"!</p>
                                    <p style='font-size: 14px; color: #666; margin-top: 10px;'>You have a pending approval for 
                                    <br/>Project: <b>" + projectTitle + "</b>" +
                                    "<br/>Milestone: <b>" + milestoneTitle + "</b>" +
                                    "<br/><br/> as an <b>Optional Milestone Approver</b>" + @" .</p>
                                    <p style='font-size: 14px; color: #555;'>
                                        Please see the link below to view the request for your approval:
                                    </p>
                                    <div style='text-align: center; margin: 30px 0;'>
                                        <a href='http://localhost:60297/Admin/PendingApprovals'
                                           style='display: inline-block; padding: 14px 40px; background-color: #66339A; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px; font-size: 16px;'>
                                           Get Started
                                        </a>
                                    </div>
                                    <p style='font-size: 14px; color: #555; text-align: center;'>
                                        Need help or have questions? Don’t hesitate to reach out. We’re here to support you every step of the way!
                                    </p>
                                </div>
                                <div style='margin-top: 20px; padding: 20px; text-align: center; background-color: #f4f4f9; border-radius: 5px; font-size: 12px; color: #999;'>
                                    <i>*This is an automated email from the Project Management System. Please do not reply. For assistance, contact your supervisor or ITS at <b>LOCAL: 132</b>.</i>
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
                }

                //preset approver email notification
                foreach (var approver in presetApprovers)
                {
                    var projectTitle = db.MainTables.Where(x => x.main_id == pID).Select(x => x.project_title).SingleOrDefault();
                    var milestoneTitle = db.MilestoneRoots.Where(x => x.id == mID).Select(x => x.milestone_name).SingleOrDefault();

                    var systemEmail = "e-notify@enchantedkingdom.ph";
                    var systemName = "PM SYSTEM";
                    var email = new MimeMessage();

                    email.From.Add(new MailboxAddress(systemName, systemEmail));
                    email.To.Add(new MailboxAddress(approver.approver_name, approver.approver_email));

                    email.Subject = "PM System Pending Approval";
                    email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                    {
                        Text = @"
                            <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                <div style='text-align: center; margin-bottom: 20px;'>
                              
                                    <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                </div>
                                <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                    <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + approver.approver_name + @"!</p>
                                    <p style='font-size: 14px; color: #666; margin-top: 10px;'>You have a pending approval for
                                    <br/>Project: <b>" + projectTitle + "</b>" +
                                    "<br/>Milestone: <b>" + milestoneTitle + "" +
                                    "<br/><br/></b> as a <b>Preset Milestone Approver</b>" + @" .</p>
                                    <p style='font-size: 14px; color: #555;'>
                                        Please see the link below to view the request for your approval:
                                    </p>
                                    <div style='text-align: center; margin: 30px 0;'>
                                        <a href='http://localhost:60297/Admin/PendingApprovals'
                                           style='display: inline-block; padding: 14px 40px; background-color: #66339A; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px; font-size: 16px;'>
                                           Get Started
                                        </a>
                                    </div>
                                    <p style='font-size: 14px; color: #555; text-align: center;'>
                                        Need help or have questions? Don’t hesitate to reach out. We’re here to support you every step of the way!
                                    </p>
                                </div>
                                <div style='margin-top: 20px; padding: 20px; text-align: center; background-color: #f4f4f9; border-radius: 5px; font-size: 12px; color: #999;'>
                                    <i>*This is an automated email from the Project Management System. Please do not reply. For assistance, contact your supervisor or ITS at <b>LOCAL: 132</b>.</i>
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
                }

                return Json(new { message = "success", JsonRequestBehavior.AllowGet });
            }

            catch(Exception e)
            {
                return Json(new { message = e.Message, JsonRequestBehavior.AllowGet });
            }
        }

        [HttpPost]
        public JsonResult BulkApproved(List<BulkApprove> tasks)
        {
            if (tasks == null)
            {
                return Json(new { success = false, JsonRequestBehavior.AllowGet });
            }

            foreach (var task in tasks)
            {
                var getChecklistSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == task.TaskId && x.milestone_id == task.MilestoneId);
                if(getChecklistSubmission.type == "optional")
                {
                    var approverToUpdate = db.OptionalMilestoneApprovers.FirstOrDefault(x =>
                        x.task_id == task.TaskId &&
                        //x.milestone_id == task.MilestoneId &&
                        x.approver_name == task.Approver
                    );

                    if (approverToUpdate != null)
                    {
                        // Perform update, e.g.:
                        approverToUpdate.approved = true;
                        approverToUpdate.date_approved = DateTime.Now;
                        db.Entry(approverToUpdate).State = EntityState.Modified;

                        //update CheckListSubmission
                        var updateChecklistSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == task.TaskId);
                        updateChecklistSubmission.approved_by = task.Approver;
                        updateChecklistSubmission.approval_date = DateTime.Now;
                        db.Entry(updateChecklistSubmission).State = EntityState.Modified;


                    }
                }
                else if(getChecklistSubmission.type == "preset")
                {
                    // Update the PreSetMilestoneApprovers table
                    var presetToUpdate = db.PreSetMilestoneApprovers.FirstOrDefault(x =>
                        x.task_id == task.TaskId &&
                       //x.milestone_id == task.MilestoneId &&
                        x.approver_name == task.Approver
                    );

                    if (presetToUpdate != null)
                    {
                        // Perform update
                        presetToUpdate.approved = true;
                        presetToUpdate.date_approved = DateTime.Now;
                        db.Entry(presetToUpdate).State = EntityState.Modified;

                        //update CheckListSubmission
                        var updateChecklistSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == task.TaskId);
                        updateChecklistSubmission.approved_by = task.Approver;
                        updateChecklistSubmission.approval_date = DateTime.Now;
                        db.Entry(updateChecklistSubmission).State = EntityState.Modified;
                    }
                }
            }

            // Save changes once after the loop
            db.SaveChanges();

            return Json(new { success = true, JsonRequestBehavior.AllowGet });
        }

        //[HttpPost]
        //public JsonResult SubmitForApproval(int taskId)
        //{
        //    try
        //    {
        //        string userEmail = User.Identity.Name.ToLower().Trim();

        //        var existing = db.ChecklistSubmissions
        //            .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

        //        if (existing != null)
        //        {
        //            return Json(new { success = false, message = "Task already submitted." });
        //        }

        //        var taskDetails = db.DetailsTbls.FirstOrDefault(d => d.details_id == taskId);
        //        if (taskDetails == null)
        //        {
        //            return Json(new { success = false, message = "Task not found." });
        //        }

        //        var submission = new ChecklistSubmission
        //        {
        //            task_id = taskId,
        //            main_id = taskDetails.main_id,
        //            is_approved = null,
        //            is_removed = false,
        //            submitted_by = userEmail,
        //            submission_date = DateTime.Now
        //        };

        //        db.ChecklistSubmissions.Add(submission);
        //        db.SaveChanges();

        //        return Json(new { success = true, message = "Task submitted for approval.", id = submission.submission_id });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error: " + ex.Message });
        //    }
        //}




        //public JsonResult GetStatusUpdates()
        //{
        //    var message = "";
        //    var data = db.WeeklyStatus.ToList();

        //    return Json(new { data = data, message = message }, JsonRequestBehavior.AllowGet);
        //}

        //public JsonResult GetStatusUpdates()
        //{
        //    var message = "";
        //    var data = db.WeeklyStatus.ToList();

        //    return Json(new { data = data, message = message }, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult ApproverView(int taskId)
        //{
        //    try
        //    {
        //        // Fetch task details
        //        var task = db.DetailsTbls.FirstOrDefault(t => t.details_id == taskId);
        //        if (task == null)
        //        {
        //            return HttpNotFound("Task not found.");
        //        }

        //        // Fetch approvers for the task
        //        var approvers = db.ApproversTbls
        //            .Where(a => a.Details_Id == taskId)
        //            .Select(a => new ApproverViewModel
        //            {
        //                UserId = a.User_Id,
        //                ApproverName = a.Approver_Name,
        //                IsRemoved = a.IsRemoved_ ?? false
        //            }).ToList();

        //        // Pass data to the view model
        //        var model = new TaskApproverViewModel
        //        {
        //            TaskId = taskId,
        //            //TaskName = task.details_id,
        //            Approvers = approvers
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex}");
        //        return View("Error", new HandleErrorInfo(ex, "Admin", "ApproverView"));
        //    }
        //}

        //[HttpGet]
        //public ActionResult ProjectChecklist()
        //{
        //    var groupedMilestones = db.MainTables
        //        .Select(project => new ProjectChecklistGroupViewModel
        //        {
        //            MainId = project.main_id,
        //            ProjectName = project.project_title,
        //            Milestones = db.MilestoneTbls
        //                .Where(m => m.main_id == project.main_id)
        //                .OrderBy(m => m.milestone_position)
        //                .Select(milestone => new MilestoneViewModel
        //                {
        //                    Id = milestone.milestone_id,
        //                    MilestoneName = milestone.milestone_name,
        //                    MilestonePosition = milestone.milestone_position ?? 0, 
        //            IsCompleted = db.DetailsTbls
        //                        .Where(t => t.milestone_id == milestone.milestone_id)
        //                        .All(t => t.IsApproved.HasValue && t.IsApproved.Value),
        //                    StatusUpdate = db.DetailsTbls
        //                        .Where(t => t.milestone_id == milestone.milestone_id)
        //                        .All(t => t.IsApproved.HasValue && t.IsApproved.Value)
        //                            ? "Completed" // approved tasks
        //                            : "In Progress", // not yet done
        //                    Tasks = db.DetailsTbls
        //                        .Where(task => task.milestone_id == milestone.milestone_id)
        //                        .Select(task => new TaskViewModel
        //                        {
        //                            Id = task.details_id,
        //                            TaskName = task.process_title,
        //                            IsApproved = task.IsApproved.HasValue && task.IsApproved.Value,
        //                            Attachments = db.AttachmentTables
        //                                .Where(a => a.details_id == task.details_id)
        //                                .Select(a => a.path_file)
        //                                .ToList(),
        //                            Approvers = db.ApproversTbls
        //                                .Where(a => a.Details_Id == task.details_id)
        //                                .Select(a => new ApproverViewModel
        //                                {
        //                                    ApproverName = a.Approver_Name,
        //                                    Status = a.Status ?? false
        //                                })
        //                                .ToList()
        //                        })
        //                        .ToList()
        //                })
        //                .ToList()
        //        })
        //        .ToList();

        //    return View(groupedMilestones);
        //}

        //[HttpGet]
        //public ActionResult ProjectChecklist(int? projectId)
        //{
        //    // fetch all projects
        //    var groupedMilestones = db.MainTables
        //        .Select(project => new ProjectChecklistGroupViewModel
        //        {
        //            MainId = project.main_id,
        //            ProjectName = project.project_title,
        //            Milestones = db.MilestoneTbls
        //                .Where(m => m.main_id == project.main_id)
        //                .OrderBy(m => m.milestone_position)
        //                .Select(milestone => new MilestoneViewModel
        //                {
        //                    Id = milestone.milestone_id,
        //                    MilestoneName = milestone.milestone_name,
        //                    MilestonePosition = milestone.milestone_position ?? 0,

        //                    IsCompleted = db.DetailsTbls
        //                        .Where(t => t.milestone_id == milestone.milestone_id)
        //                        .All(t => t.IsApproved.HasValue && t.IsApproved.Value),

        //                    StatusUpdate = db.DetailsTbls
        //                        .Where(t => t.milestone_id == milestone.milestone_id)
        //                        .All(t => t.IsApproved.HasValue && t.IsApproved.Value)
        //                            ? "Completed" : "In Progress",

        //                    Tasks = db.DetailsTbls
        //                        .Where(task => task.milestone_id == milestone.milestone_id)
        //                        .Select(task => new TaskViewModel
        //                        {
        //                            Id = task.details_id,
        //                            TaskName = task.process_title,
        //                            IsApproved = task.IsApproved.HasValue && task.IsApproved.Value,

        //                            Attachments = db.AttachmentTables
        //                                .Where(a => a.details_id == task.details_id)
        //                                .Select(a => a.path_file)
        //                                .ToList(),

        //                            Approvers = db.ApproversTbls
        //                                .Where(a => a.Details_Id == task.details_id)
        //                                .Select(a => new ApproverViewModel
        //                                {
        //                                    ApproverName = a.Approver_Name,
        //                                    Status = a.Status ?? false
        //                                })
        //                                .ToList()
        //                        })
        //                        .ToList()
        //                })
        //                .ToList()
        //        })
        //        .ToList();

        //    // filter by project id 
        //    if (projectId.HasValue)
        //    {
        //        groupedMilestones = groupedMilestones
        //            .Where(g => g.MainId == projectId.Value)
        //            .ToList();
        //    }

        //    ViewBag.ProjectList = groupedMilestones.Select(p => new { p.MainId, p.ProjectName }).ToList();

        //    return View(groupedMilestones);
    }
}