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
            try
            {
                
                var users = cmdb.AspNetUsers
                    .Select(u => new UserModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email
                    })
                    .ToList();

                
                ViewBag.Users = users;

               
                var checklists = db.ChecklistSetups
                    .Select(c => new
                    {
                        ChecklistId = c.cl_sett_id,
                        ChecklistName = c.checklist_name,
                        Division = c.division
                    })
                    .ToList();

              
                var checklistViewModels = checklists
                    .Select(c => new ChecklistSettingsViewModel
                    {
                        ChecklistId = c.ChecklistId,
                        ChecklistName = c.ChecklistName,
                        Division = c.Division,
                        Onboarding = new Onboarding
                        {
                            Users = users 
                }
                    })
                    .ToList();

                
                ViewBag.Checklists = checklistViewModels;

                return View();
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error in ChecklistSettings: {ex.Message}");

                TempData["ErrorMessage"] = "An error occurred while loading checklist settings.";
                return RedirectToAction("Error", "Home");
            }
        }





        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER")]
        [HttpGet]
        public JsonResult GetProjectsByChecklist(int checklistId)
        {
            var checklist = db.ChecklistSetups.FirstOrDefault(c => c.cl_sett_id == checklistId);
            if (checklist == null)
                return Json(new { success = false, message = "Checklist not found." }, JsonRequestBehavior.AllowGet);

            var projects = db.MainTables
                .Where(p => p.division == checklist.division)
                .Select(p => new
                {
                    MainId = p.main_id,
                    ProjectTitle = p.project_title
                })
                .ToList();

            return Json(projects, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER")]
        [HttpGet]
        public JsonResult GetProjectDetails(int projectId)
        {
            var milestones = db.MilestoneTbls
                .Where(m => m.main_id == projectId)
                .Select(milestone => new
                {
                    MilestoneName = milestone.milestone_name,
                    Tasks = db.DetailsTbls
                        .Where(task => task.milestone_id == milestone.milestone_id)
                        .Select(task => new
                        {
                            Id = task.details_id,
                            TaskName = task.process_title,
                            RequiresApproval = task.RequiresApproval ?? false
                        }).ToList()
                }).ToList();

            return Json(new { Milestones = milestones }, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER")]
        [HttpGet]
        public JsonResult GetProjectsForChecklist(int checklistId)
        {
            var projects = db.MainTables
                .Where(project => db.FixedChecklistTbls.Any(fc =>
                    fc.Checklist_ID == checklistId &&
                    fc.Main_ID == project.main_id))
                .Select(project => new
                {
                    MainId = project.main_id,
                    ProjectTitle = project.project_title
                }).ToList();

            return Json(projects, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER")]
        [HttpGet]
        public JsonResult GetProjectTasks(int projectId)
        {
            var projectTasks = db.MilestoneTbls
                .Where(m => m.main_id == projectId)
                .Select(milestone => new
                {
                    MilestoneName = milestone.milestone_name,
                    MilestoneId = milestone.milestone_id,
                    Tasks = db.DetailsTbls
                        .Where(task => task.milestone_id == milestone.milestone_id)
                        .Select(task => new
                        {
                            Id = task.details_id,
                            TaskName = task.process_title,
                            RequiresApproval = task.RequiresApproval ?? false
                        }).ToList()
                }).ToList();

            return Json(new { Milestones = projectTasks }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AssignApprovers(int taskId, List<string> approvers, int milestoneId, int mainId)
        {
            using (var transaction = db.Database.BeginTransaction()) 
            {
                try
                {
                    if (taskId <= 0 || milestoneId <= 0 || mainId <= 0)
                    {
                        return Json(new { success = false, message = "Invalid task, milestone, or project ID." });
                    }

                    var existingApprovers = db.ApproversTbls
                        .Where(a => a.Details_Id == taskId)
                        .ToList();

                    if (approvers != null && approvers.Any())
                    {
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
                    }
                    else
                    {
                        foreach (var approver in existingApprovers)
                        {
                            approver.IsRemoved_ = true;
                            db.Entry(approver).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit(); 
                    return Json(new { success = true, message = "Approvers assigned successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); 
                    return Json(new { success = false, message = "Error assigning approvers: " + ex.Message });
                }
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
        public ActionResult ChecklistSetup()
        {
            var divisions = db.MainTables
                              .Select(m => m.division.Trim())
                              .Distinct()
                              .OrderBy(d => d)
                              .ToList();
            ViewBag.Divisions = divisions;

            var checklists = db.ChecklistSetups
                .Select(c => new ChecklistSettingsViewModel
                {
                    ChecklistId = c.cl_sett_id,
                    ChecklistName = c.checklist_name,
                    Division = c.division,
                    Milestones = db.FixedChecklistTbls
                        .Where(fc => fc.Checklist_ID == c.cl_sett_id)
                        .GroupBy(fc => fc.Milestone_ID) 
                        .Select(fcGroup => fcGroup.FirstOrDefault()) 
                        .Select(fc => new MilestoneViewModel
                        {
                            Id = fc.Milestone_ID ?? 0,
                            MilestoneName = db.MilestoneTbls
                                .Where(m => fc.Milestone_ID.HasValue && m.milestone_id == fc.Milestone_ID.Value)
                                .Select(m => m.milestone_name)
                                .FirstOrDefault() ?? "No Name"
                        }).ToList()
                })
                .ToList();

            ViewBag.Checklists = checklists;

            return View();
        }



        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpGet]
        public JsonResult GetMilestonesByDivision(string division, int? checklistId = null)
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
                .GroupBy(x => x.MilestoneName)
                .Select(group => group.OrderBy(m => m.Position).FirstOrDefault())
                .Select(x => new
                {
                    x.MilestoneId,
                    x.MilestoneName,
                    x.Position,
                    IsSelected = checklistId.HasValue && db.FixedChecklistTbls
                        .Any(cm => cm.Checklist_ID == checklistId && cm.Milestone_ID == x.MilestoneId) 
                })
                .OrderBy(m => m.Position)
                .ToList();

            return Json(milestones, JsonRequestBehavior.AllowGet);
        }

        public void AssignChecklistToDivision(int checklistId)
        {
            var checklist = db.ChecklistSetups.FirstOrDefault(c => c.cl_sett_id == checklistId);
            if (checklist == null) return;

            var projectsInDivision = db.MainTables
                .Where(m => m.division == checklist.division)
                .Select(m => m.main_id)
                .ToList();

            foreach (var projectId in projectsInDivision)
            {
                bool exists = db.FixedChecklistTbls
                    .Any(fc => fc.Checklist_ID == checklistId && fc.Main_ID == projectId);

                if (!exists)
                {
                    db.FixedChecklistTbls.Add(new FixedChecklistTbl
                    {
                        Checklist_ID = checklistId,
                        Main_ID = projectId
                    });
                }
            }

            db.SaveChanges();
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
                    var existingChecklist = db.ChecklistSetups
                        .FirstOrDefault(c => c.division.Equals(division, StringComparison.OrdinalIgnoreCase));

                    if (existingChecklist != null)
                    {
                        return Json(new { success = false, message = "A checklist for this division already exists." });
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

                    var projectsInDivision = db.MainTables
                        .Where(p => p.division == division)
                        .Select(p => p.main_id)
                        .ToList();

                    foreach (var mainId in projectsInDivision)
                    {
                        foreach (var milestoneId in milestoneIds)
                        {
                            db.FixedChecklistTbls.Add(new FixedChecklistTbl
                            {
                                Checklist_ID = newChecklistId,
                                Milestone_ID = milestoneId,
                                Main_ID = mainId,
                                Project_Specific = false
                            });
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = $"{checklistName} saved successfully.", redirectUrl = Url.Action("ChecklistSettings", "Admin") });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new { success = false, message = "An error occurred while saving the checklist. Please try again later." });
            }
        }


        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpPost]
        public JsonResult DeleteChecklist(int checklistId)
        {
            try
            {
                var checklist = db.ChecklistSetups.FirstOrDefault(c => c.cl_sett_id == checklistId);
                if (checklist == null)
                {
                    return Json(new { success = false, message = "Checklist not found." });
                }

                var associatedMilestones = db.FixedChecklistTbls.Where(f => f.Checklist_ID == checklistId).ToList();
                db.FixedChecklistTbls.RemoveRange(associatedMilestones);
                db.ChecklistSetups.Remove(checklist);

                db.SaveChanges();

                return Json(new { success = true, message = "Checklist deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting checklist.", error = ex.Message });
            }
        }

         
        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpGet]
        public JsonResult GetChecklistDetails(int checklistId)
        {
            try
            {
                var checklist = db.ChecklistSetups
                    .Where(c => c.cl_sett_id == checklistId)
                    .Select(c => new
                    {
                        ChecklistId = c.cl_sett_id,
                        ChecklistName = c.checklist_name,
                        Division = c.division,
                        Milestones = db.FixedChecklistTbls
                            .Where(fc => fc.Checklist_ID == c.cl_sett_id)
                            .Select(fc => new
                            {
                                Id = fc.Milestone_ID,
                                Name = db.MilestoneTbls
                                    .Where(m => m.milestone_id == fc.Milestone_ID)
                                    .Select(m => m.milestone_name)
                                    .FirstOrDefault(),
                                IsSelected = true
                            }).ToList()
                    })
                    .FirstOrDefault();

                if (checklist == null)
                {
                    return Json(new { success = false, message = "Checklist not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, checklist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while fetching the checklist.", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN")]
        [HttpPost]
        public JsonResult UpdateChecklist(int checklistId, List<int> milestoneIds)
        {
            try
            {
                var checklist = db.ChecklistSetups.Find(checklistId);
                if (checklist == null)
                {
                    return Json(new { success = false, message = "Checklist not found." });
                }

                var existingMilestones = db.FixedChecklistTbls
                    .Where(fc => fc.Checklist_ID == checklistId)
                    .ToList();

                db.FixedChecklistTbls.RemoveRange(existingMilestones); 

                foreach (var milestoneId in milestoneIds)
                {
                    db.FixedChecklistTbls.Add(new FixedChecklistTbl
                    {
                        Checklist_ID = checklistId,
                        Milestone_ID = milestoneId,
                        Project_Specific = false
                    });
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Checklist updated successfully." });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = "An error occured while updating the checklist.", error = ex.Message });
            }
        }



    }
}