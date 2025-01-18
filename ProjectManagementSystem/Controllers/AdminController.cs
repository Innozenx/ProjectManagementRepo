using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ProjectManagementSystem.CustomAttributes;
using System.Data.Entity;
using System.Data.SqlClient;

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

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER, PMS_PROJECT_MANAGER")]
        public ActionResult Register()
        {
            return View();
        }

        public JsonResult Register_Project(string name)
        {
            var message = "";
            var status = false;
            var division = "";
            var department = "";

            CMIdentityDBEntities cmdb = new CMIdentityDBEntities();

            if (string.IsNullOrWhiteSpace(name))
            {
                message = "Project name cannot be empty.";
                return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
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

                ProjectRegister details = new ProjectRegister
                {
                    ProjectName = name,
                    DateRegistered = DateTime.Now,
                    Division = division,
                    RegisteredBy = User.Identity.Name,
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
                    department = department,
                    division = division
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

        //[CustomAuthorize(Roles = "PMS_Developer")]
        //public ActionResult RegisterUserView()
        //{
        //    List<UserType> listTypes = db.UserTypes.ToList();
        //    return View(listTypes);
        //}

        //public JsonResult RegisterUser(string email, string type)
        //{
        //    var message = "";
        //    var status = false;
        //    var division = "";
        //    var department = "";

        //    try
        //    {
        //        var tblJoin = (from netUser in cmdb.AspNetUsers
        //                       join jobDesc in cmdb.Identity_JobDescription on netUser.JobId equals jobDesc.Id
        //                       join idKey in cmdb.Identity_Keywords on jobDesc.DeptId equals idKey.Id
        //                       select new { netUser.UserName, jobDesc.DeptId, jobDesc.DivisionId, idKey.Description }).Where(x => x.UserName == User.Identity.Name).ToList();

        //        foreach (var item in tblJoin)
        //        {
        //            division = cmdb.Identity_Keywords.Where(x => x.Id == item.DivisionId).Select(x => x.Description).Single();
        //            department = cmdb.Identity_Keywords.Where(x => x.Id == item.DeptId).Select(x => x.Description).Single();
        //        }

        //        AdminList admin = new AdminList()
        //        {
        //            user_level = type,
        //            user_email = email,
        //            is_active = true
        //        };

        //        db.AdminLists.Add(admin);

        //        Activity_Log logs = new Activity_Log
        //        {
        //            username = User.Identity.Name,
        //            datetime_performed = DateTime.Now,
        //            action_level = 5,
        //            action = "User Registration",
        //            description = "User: " + email + "-" + type + " Registered by: " + User.Identity.Name,
        //            department = department,
        //            division = division
        //        };

        //        db.Activity_Log.Add(logs);

        //        db.SaveChanges();

        //        message = "User Registration Successful";
        //        status = true;
        //    }
        //    catch (Exception e)
        //    {
        //        message = "User Registration Failed";
        //        status = false;
        //    }



        //    return Json(new { message = message, status = status }, JsonRequestBehavior.AllowGet);
        //}


        public ActionResult RoleConfiguration()
        {

            var viewModel = new RoleViewModel
            {
                ExistingRoles = db.Roles.Select(r => r.RoleName).ToList()
            };

            return View(viewModel);

        }

        [HttpPost]
        public JsonResult AddRole(string roleName)
        {
            var message = "";
            var status = false;

            if (string.IsNullOrWhiteSpace(roleName))
            {
                message = "Role name cannot be empty.";
                return Json(new { message, status }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var existingRole = db.Roles.FirstOrDefault(r => r.RoleName == roleName);
                if (existingRole != null)
                {
                    message = "Role already exists.";
                    return Json(new { message, status }, JsonRequestBehavior.AllowGet);
                }

                var newRole = new Role
                {
                    RoleName = roleName
                };
                db.Roles.Add(newRole);
                db.SaveChanges();

                message = "Role added successfully!";
                status = true;
            }
            catch (Exception ex)
            {
                message = "Failed to add role. Error: " + ex.Message;
                Debug.WriteLine(ex.Message);
            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult EditRole(int id, string newRoleName)
        {
            var message = "";
            var status = false;

            if (string.IsNullOrWhiteSpace(newRoleName))
            {
                message = "Role name cannot be empty.";
                return Json(new { message, status }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var role = db.Roles.FirstOrDefault(r => r.id == id);
                if (role == null)
                {
                    message = "Role not found.";
                    return Json(new { message, status }, JsonRequestBehavior.AllowGet);
                }

                if (db.Roles.Any(r => r.RoleName == newRoleName && r.id != id))
                {
                    message = "Another role with the same name already exists.";
                    return Json(new { message, status }, JsonRequestBehavior.AllowGet);
                }

                role.RoleName = newRoleName;
                db.SaveChanges();

                message = "Role updated successfully!";
                status = true;
            }
            catch (Exception ex)
            {
                message = "Failed to edit role. Error: " + ex.Message;
                Debug.WriteLine(ex.Message);
            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteRole(int id)
        {
            var message = "";
            var status = false;

            try
            {
                var role = db.Roles.FirstOrDefault(r => r.id == id);
                if (role == null)
                {
                    message = "Role not found.";
                    return Json(new { message, status }, JsonRequestBehavior.AllowGet);
                }

                db.Roles.Remove(role);
                db.SaveChanges();

                message = "Role deleted successfully!";
                status = true;
            }
            catch (Exception ex)
            {
                message = "Failed to delete role. Error: " + ex.Message;
                Debug.WriteLine(ex.Message);
            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER")]
        [HttpGet]
        public ActionResult ChecklistSettings()
        {
  
            var onboarding = new Onboarding
            {
                Users = cmdb.AspNetUsers.Select(user => new UserModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                }).ToList()
            };

            var projects = db.MainTables
                .Select(project => new
                {
                    MainId = project.main_id,
                    ProjectName = project.project_title,
                    Milestones = db.MilestoneTbls
                        .Where(m => m.main_id == project.main_id)
                        .Select(m => new
                        {
                            Id = m.milestone_id,
                            MilestoneName = m.milestone_name,
                            Tasks = db.DetailsTbls
                                .Where(d => d.milestone_id == m.milestone_id)
                                .Select(d => new
                                {
                                    Id = d.details_id,
                                    TaskName = d.process_title,
                                    RequiresApproval = d.RequiresApproval ?? false
                                }).ToList()
                        }).ToList()
                }).ToList();

         
            var checklistSettings = projects.Select(p => new ChecklistSettingsViewModel
            {
                MainId = p.MainId,
                ProjectName = p.ProjectName,
                Milestones = p.Milestones.Select(m => new MilestoneViewModel
                {
                    Id = m.Id,
                    MilestoneName = m.MilestoneName,
                    Tasks = m.Tasks.Select(t => new TaskViewModel
                    {
                        Id = t.Id,
                        TaskName = t.TaskName,
                        RequiresApproval = t.RequiresApproval
                    }).ToList()
                }).ToList(),
                Onboarding = onboarding 
            }).ToList();

            return View(checklistSettings);
        }



        [HttpGet]
        public JsonResult GetProjectTasks(int projectId)
        {
            var projectTasks = db.MainTables
                .Where(p => p.main_id == projectId)
                .Select(p => new
                {
                    ProjectName = p.project_title,
                    Milestones = db.MilestoneTbls
                        .Where(m => m.main_id == p.main_id)
                        .Select(m => new
                        {
                            MilestoneName = m.milestone_name,
                            MilestoneId = m.milestone_id,
                            Tasks = db.DetailsTbls
                                .Where(d => d.milestone_id == m.milestone_id)
                                .Select(d => new
                                {
                                    Id = d.details_id,
                                    TaskName = d.process_title,
                                    RequiresApproval = d.RequiresApproval ?? false
                                }).ToList()
                        }).ToList()
                }).ToList();

            return Json(projectTasks, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public JsonResult AssignApprovers(int taskId, List<string> approvers, int milestoneId, int mainId)
        {
            try
            {
                Console.WriteLine("Task ID: " + taskId);
                Console.WriteLine("Approvers: " + string.Join(", ", approvers ?? new List<string>()));
                Console.WriteLine("Milestone ID: " + milestoneId);
                Console.WriteLine("Main ID: " + mainId);

                if (taskId <= 0 || milestoneId <= 0 || mainId <= 0)
                {
                    return Json(new { success = false, message = "Invalid task, milestone, or project ID." });
                }

                if (approvers != null && approvers.Any())
                {
                    var existingApprovers = db.ApproversTbls
                        .Where(a => a.Details_Id == taskId)
                        .ToList();

                    foreach (var approver in existingApprovers)
                    {
                        if (approvers.Contains(approver.User_Id))
                        {
                            approver.IsRemoved_ = false;
                        }
                        else
                        {
                            approver.IsRemoved_ = true;
                        }
                        db.Entry(approver).State = EntityState.Modified;
                    }

                    foreach (var approverId in approvers)
                    {
                        if (!existingApprovers.Any(a => a.User_Id == approverId))
                        {
                            var user = cmdb.AspNetUsers
                                .Where(x => x.Id == approverId)
                                .Select(x => new { x.FirstName, x.LastName })
                                .FirstOrDefault();

                            if (user != null)
                            {
                                var newApprover = new ApproversTbl
                                {
                                    Details_Id = taskId,
                                    User_Id = approverId,
                                    Approver_Name = $"{user.FirstName} {user.LastName}",
                                    Milestone_Id = milestoneId,
                                    Main_Id = mainId, 
                                    ApprovalDate = DateTime.Now,
                                    IsRemoved_ = false
                                };

                                db.ApproversTbls.Add(newApprover);
                            }
                        }
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Approvers assigned successfully." });
                }
                else
                {
                    var existingApprovers = db.ApproversTbls
                        .Where(a => a.Details_Id == taskId)
                        .ToList();

                    foreach (var approver in existingApprovers)
                    {
                        approver.IsRemoved_ = true;
                        db.Entry(approver).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "All approvers removed successfully." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                return Json(new { success = false, message = "Error assigning approvers: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateTaskApproval(int taskId, bool requiresApproval)
        {
            try
            {
                
                var task = db.DetailsTbls.FirstOrDefault(t => t.details_id == taskId);
                if (task == null)
                {
                    return Json(new { success = false, message = "Task not found." });
                }

                task.RequiresApproval = requiresApproval;

                if (!requiresApproval)
                {

                    var approvers = db.ApproversTbls
                        .Where(a => a.Details_Id == taskId && (a.IsRemoved_ == false || a.IsRemoved_ == null))
                        .ToList();
                    approvers.ForEach(a => a.IsRemoved_ = true);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Task approval updated successfully." });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CheckApprovers(int taskId)
        {
            var hasApprovers = db.ApproversTbls
                .Any(a => a.Details_Id == taskId && (a.IsRemoved_ == false || a.IsRemoved_ == null));
            return Json(new { hasApprovers });
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpGet]
        public ActionResult ChecklistSetup()
        {
            var divisions = db.MainTables
                              .Select(m => m.division.Trim())
                              .Distinct()
                              .OrderBy(d => d)
                              .ToList();

            ViewBag.Divisions = divisions;

            return View();
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpGet]
        public JsonResult GetMilestonesByDivision(string division)
        {
            if (string.IsNullOrEmpty(division))
            {
                return Json(new { success = false, message = "Division not found." }, JsonRequestBehavior.AllowGet);
            }

            var milestones = db.MilestoneTbls
                .Join(db.MainTables,
                      milestone => milestone.main_id,
                      main => main.main_id,
                      (milestone, main) => new
                      {
                          Division = main.division,
                          MilestoneId = milestone.milestone_id,
                          MilestoneName = milestone.milestone_name,
                          Position = milestone.milestone_position
                      })
                .Where(x => x.Division == division)
                .GroupBy(x => x.MilestoneName) // grouping of milestones
                .Select(group => group.OrderBy(m => m.Position).FirstOrDefault()) // avoiding of milestone duplicates
                .Select(x => new
                {
                    x.MilestoneId,
                    x.MilestoneName,
                    x.Position
                })
                .OrderBy(m => m.Position) // milestones in order based from csv
                .ToList();

            return Json(milestones, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpPost]
        public JsonResult SaveChecklist(string division, List<int> milestoneIds, string checklistName)
        {
            if (string.IsNullOrEmpty(division) || milestoneIds == null || !milestoneIds.Any())
            {
                return Json(new { success = false, message = "Please select a division and at least one milestone." });
            }

            try
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    
                    var existingChecklist = db.ChecklistSetups.FirstOrDefault(c => c.division == division);
                    if (existingChecklist != null)
                    {
                        return Json(new { success = false, message = "Checklist for this division already exists." });
                    }

                    var newChecklist = new ChecklistSetup
                    {
                        checklist_name = checklistName,
                        division = division,
                        created_by = User.Identity.Name,
                        date_created = DateTime.Now
                    };
                    db.ChecklistSetups.Add(newChecklist);
                    db.SaveChanges();

                    
                    int newChecklistId = newChecklist.cl_sett_id;

                   
                    foreach (var milestoneId in milestoneIds)
                    {
                        
                        var mainId = db.MilestoneTbls
                                       .Where(m => m.milestone_id == milestoneId)
                                       .Select(m => m.main_id)
                                       .FirstOrDefault();

                        if (mainId != null)
                        {
                            var fixedChecklistEntry = new FixedChecklistTbl
                            {
                                Checklist_ID = newChecklistId,
                                Milestone_ID = milestoneId,
                                Main_ID = mainId,
                                //Date_Created = DateTime.Now.ToString("yyyy-MM-dd"),
                                Project_Specific = false // fixed checklist
                            };
                            db.FixedChecklistTbls.Add(fixedChecklistEntry);
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = $"{checklistName} saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving the checklist.", error = ex.Message });
            }
        }
    }
}