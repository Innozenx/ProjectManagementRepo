using ProjectManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ProjectManagementSystem.CustomAttributes;

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

        [CustomAuthorize(Roles = "PMS_Developer")]
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
        [CustomAuthorize(Roles = "PMS_Developer")]
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

        [HttpPost]
        public JsonResult UpdateTaskApproval(int taskId, bool requiresApproval)
        {
            var message = "";
            var status = false;

            try
            {
                var task = db.DetailsTbls.FirstOrDefault(t => t.details_id == taskId);
                if (task != null)
                {
                    task.RequiresApproval = requiresApproval;
                    db.SaveChanges();

                    message = "Requires approval.";
                    status = true;
                }
                else
                {
                    message = "Task not found.";
                }
            }
            catch (Exception ex)
            {
                message = "Error updating task approval status: " + ex.Message;
                Debug.WriteLine(ex.Message); 
            }

            return Json(new { success = status, message = message }, JsonRequestBehavior.AllowGet);
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


        //[HttpPost]
        //public JsonResult AssigApprovers(int taskId, List<int> approvers)
        //{
        //    try
        //    {
        //        Console.WriteLine("Task ID: " + taskId);
        //        Console.WriteLine("Approvers: ")
        //    }
        //    return View();
        //}


        [HttpPost]
        public JsonResult AssignApprovers(int taskId, List<int> approvers)
        {
            try
            {
                Console.WriteLine("Task ID: " + taskId);
                Console.WriteLine("Approvers: " + string.Join(", ", approvers));

                if (approvers != null && approvers.Count > 0)
                {
                    var existingApprovers = db.ApproversTbls.Where(a => a.Details_Id == taskId).ToList();
                    db.ApproversTbls.RemoveRange(existingApprovers);

                    foreach (var approverId in approvers)
                    {
                        var newApprover = new ApproversTbl
                        {
                            Details_Id = taskId,
                            User_Id = approverId,
                            ApprovalDate = DateTime.Now
                        };
                        db.ApproversTbls.Add(newApprover);
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Approvers assigned successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "No approvers selected." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error assigning approvers: " + ex.Message });
            }
        }

    }
}