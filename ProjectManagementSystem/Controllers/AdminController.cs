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
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNet.Identity;
using MimeKit;
using MailKit.Net.Smtp;

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

        //[Authorize(Roles = "PMS_PROJECT_OWNER, PMS_ADMIN")]
        public JsonResult Register_Project(string name)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

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

                db.SaveChanges();

                details_container.Add(name);
                log.ActivityLog(User.Identity.Name, 3, "Project Registration", name, details_container);

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
            var dbRoles = db.Roles.ToList();
            var viewModel = new RoleViewModel
            {
                ExistingRoles = dbRoles.Select(x => x.RoleName).ToList(),
                RoleID = db.Roles.Select(x => x.id).ToList()
            };

            return View(viewModel);

        }

        [HttpPost]
        public JsonResult AddRole(string roleName)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            var message = "";
            var status = false;

            if (string.IsNullOrWhiteSpace(roleName))
            {
                message = "Role name cannot be empty.";
                return Json(new { message, status }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                //var existingRole = db.Roles.FirstOrDefault(r => r.RoleName == roleName);
                var existingRole = db.Roles.FirstOrDefault(r => r.RoleName.ToLower().Trim() == roleName.ToLower().Trim());

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

                details_container.Add(roleName);
                log.ActivityLog(User.Identity.Name, 5, "Add Role", "N/A", details_container);
            }
            catch (Exception ex)
            {
                //message = "Failed to add role. Error: " + ex.Message;
                //Debug.WriteLine(ex.Message);

                Debug.WriteLine($"Error adding role: {ex.Message}");
                message = "An error occurred while adding the role.";

            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult EditRole(int id, string newRoleName)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

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

                details_container.Add(newRoleName);
                log.ActivityLog(User.Identity.Name, 5, "Role Edit", "N/A", details_container);
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
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

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

                details_container.Add(role.RoleName);
                log.ActivityLog(User.Identity.Name, 5, "Delete Role", "N/A", details_container);
            }
            catch (Exception ex)
            {
                message = "Failed to delete role. Error: " + ex.Message;
                Debug.WriteLine(ex.Message);
            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Milestones()
        {
            var divisions = db.Divisions
                .Select(d => new SelectListItem
                {
                    Value = d.DivisionID.ToString(),
                    Text = d.DivisionName
                })
                .ToList();

            ViewBag.Divisions = divisions;

            var firstDivision = db.Divisions.FirstOrDefault();

            if (firstDivision != null)
            {
                int divisionId = firstDivision.DivisionID;

                var finalizedChecklist = db.ChecklistReferences
                    .FirstOrDefault(c => c.DivisionID == divisionId && c.IsFinalized == true);

                bool isFinalized = finalizedChecklist != null;
                ViewBag.IsFinalized = isFinalized;
                ViewBag.ReferenceNumber = isFinalized ? finalizedChecklist.ReferenceNumber : "";

                if (isFinalized)
                {
                    var milestones = db.PreSetMilestones
                        .Where(m => m.DivisionID == divisionId)
                        .ToList();

                    ViewBag.MilestoneCount = milestones.Count;

                    ViewBag.ChecklistItemCount = milestones
                        .Where(m => !string.IsNullOrEmpty(m.Requirements))
                        .SelectMany(m => m.Requirements.Split(';'))
                        .Count();
                }
                else
                {
                    ViewBag.MilestoneCount = 0;
                    ViewBag.ChecklistItemCount = 0;
                }
            }
            else
            {
                ViewBag.IsFinalized = false;
                ViewBag.ReferenceNumber = "";
                ViewBag.MilestoneCount = 0;
                ViewBag.ChecklistItemCount = 0;
            }

            return View();
        }

        public ActionResult GetDivisions()
        {
            var divisions = db.Divisions.Select(d => new { d.DivisionID, d.DivisionName }).ToList();
            return Json(divisions, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult GetMilestones(int divisionId)
        //{
        //    var milestones = db.PreSetMilestones
        //        .Where(m => m.DivisionID == divisionId)
        //        .Select(m => new
        //        {
        //            m.DivisionID,
        //            m.MilestoneName,
        //            m.Requirements,
        //            m.Approvers
        //        }).ToList();

        //    return Json(milestones, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult CreateChecklist(int divisionId)
        {
            var division = db.Divisions.FirstOrDefault(d => d.DivisionID == divisionId);
            ViewBag.DivisionName = division != null ? division.DivisionName : "Unknown Division";
            ViewBag.DivisionId = divisionId;

            var rawMilestones = (from pm in db.PreSetMilestones
                                 join mr in db.MilestoneRoots on pm.MilestoneID equals mr.id into milestoneJoin
                                 from mr in milestoneJoin.DefaultIfEmpty()
                                 where pm.DivisionID == divisionId
                                 select new
                                 {
                                     pm.MilestoneID,
                                     MilestoneName = mr != null ? mr.milestone_name : "Unknown Milestone",
                                     pm.Requirements,
                                     pm.Approvers,
                                     pm.Sorting,
                                     pm.CreatedDate,
                                     pm.ChecklistNumber,
                                     pm.DivisionCodeNumber
                                 }).ToList();

            var milestones = rawMilestones.Select(m => new MilestoneViewModel
            {
                Id = m.MilestoneID ?? 0,
                MilestoneName = m.MilestoneName, 
                Requirements = !string.IsNullOrEmpty(m.Requirements) ? m.Requirements.Split(';').ToList() : new List<string>(),
                Approvers = !string.IsNullOrEmpty(m.Approvers) ? m.Approvers.Split(',').ToList() : new List<string>(),
                Sorting = m.Sorting ?? 0,
                CreatedDate = m.CreatedDate ?? DateTime.MinValue
            }).ToList();

            return View(milestones);
        }

        public JsonResult GetMilestones(int divisionId)
        {
            var rawMilestones = db.PreSetMilestones
                .Where(m => m.DivisionID == divisionId)
                .ToList();

            var milestones = rawMilestones.Select(m => new
            {
                m.MilestoneID,
                m.MilestoneName,
                Requirements = string.IsNullOrEmpty(m.Requirements)
                    ? new List<string>()
                    : m.Requirements.Split(';').Select(r => r.Trim()).ToList(),

                Approvers = db.PreSetMilestoneApprovers.Where(x => x.task_id == m.ID).Select(x => x.approver_name).ToList()
            }).ToList();

            return Json(milestones, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddMilestones(int? divisionId)
        {
            using (var db = new ProjectManagementDBEntities())
            {
                var division = db.Divisions.FirstOrDefault(d => d.DivisionID == divisionId);
                if (division == null)
                {
                    return HttpNotFound("Division not found.");
                }


                // make the select approvers list limited to division and department head only
                var users = cmdb.AspNetUsers
                  .Where(u => u.JobLevel == 4035 || u.JobLevel == 4034)
                  .Select(u => new SelectListItem
                  {
                      Value = u.Id.ToString(),
                      Text = u.FirstName + " " + u.LastName +
                             (u.JobLevel == 4035 ? " (Division Head)" :
                             u.JobLevel == 4034 ? " (Department Head)" : "")
                  })
                  .ToList();


                var milestoneList = db.MilestoneRoots
                    .Select(m => new SelectListItem
                    {
                        Value = m.id.ToString(),
                        Text = m.milestone_name
                    })
                    .ToList();

                ViewBag.DivisionId = division.DivisionID;
                ViewBag.DivisionName = division.DivisionName;
                ViewBag.Users = users;
                ViewBag.MilestoneList = milestoneList; 

                return View();
            }
        }

        [HttpPost]
        public JsonResult SaveMilestone(int DivisionID, string MilestoneName, List<TaskModel> Tasks)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                Debug.WriteLine($"Received Data - DivisionID: {DivisionID}, MilestoneName: {MilestoneName}, Tasks Count: {Tasks.Count}");
                var division_string = db.Divisions.Where(x => x.DivisionID == DivisionID).Select(x => x.DivisionName).SingleOrDefault();

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
                                       division = kd.Description,
                                       id = u.Id
                                   }).ToList();


                if (Tasks == null || !Tasks.Any())
                {
                    return Json(new { success = false, message = "Error: At least one task is required!" });
                }

                string requirements = string.Join("; ", Tasks.Select(t => t.Requirement).Where(r => !string.IsNullOrWhiteSpace(r)));
                if (string.IsNullOrWhiteSpace(requirements))
                {
                    return Json(new { success = false, message = "Error: Requirements cannot be empty!" });
                }

                string approverIds = string.Join(",", Tasks.SelectMany(t => t.Approvers).Where(a => !string.IsNullOrWhiteSpace(a)));

                int nextSorting = db.PreSetMilestones
                    .Where(m => m.DivisionID == DivisionID)
                    .Select(m => (int?)m.Sorting)
                    .DefaultIfEmpty(0)
                    .Max() ?? 0;
                nextSorting += 1;

                var milestoneId = db.MilestoneRoots
                    .Where(x => x.milestone_name.ToLower() == MilestoneName.ToLower())
                    .Select(x => x.id)
                    .SingleOrDefault();

                if (milestoneId == 0)
                {
                    return Json(new { success = false, message = "Error: Milestone root not found." });
                }

                var divisionCode = db.Divisions
                    .Where(d => d.DivisionID == DivisionID)
                    .Select(d => d.DivisionCode)
                    .FirstOrDefault() ?? "DIV";

                var lastEntry = db.PreSetMilestones
                    .Where(p => p.DivisionCodeNumber.StartsWith(divisionCode))
                    .OrderByDescending(p => p.DivisionCodeNumber)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastEntry != null)
                {
                    string lastNumStr = lastEntry.DivisionCodeNumber.Substring(divisionCode.Length);
                    int.TryParse(lastNumStr, out int parsedNum);
                    nextNumber = parsedNum + 1;
                }
                var divisionCodeNumber = $"{divisionCode}{nextNumber.ToString("D3")}";

                Debug.WriteLine($"DivisionCodeNumber: {divisionCodeNumber}");

                foreach(var task in Tasks)
                {
                    var presetMilestone = new PreSetMilestone
                    {
                        MilestoneID = milestoneId,
                        DivisionID = DivisionID,
                        MilestoneName = MilestoneName,
                        Sorting = nextSorting,
                        Requirements = task.Requirement,
                        CreatedDate = DateTime.Now,
                        ChecklistNumber = "",
                        DivisionCodeNumber = divisionCodeNumber,
                        division_string = db.Divisions.Where(x => x.DivisionID == DivisionID).Select(x => x.DivisionName).FirstOrDefault()
                    };

                    db.PreSetMilestones.Add(presetMilestone);
                    db.SaveChanges();

                    foreach (var approver in task.Approvers)
                    {
                        //save for preset approvers
                        var approver_details = cmdb.AspNetUsers.Where(x => x.Id == approver).SingleOrDefault();
                        var project_list = db.MainTables.Where(x => x.division == division_string).ToList();

                        foreach (var project in project_list)
                        {
                            var approver_container = new PreSetMilestoneApprover
                            {
                                approver_name = approver_details.FirstName + " " + approver_details.LastName,
                                approver_email = approver_details.Email,
                                milestone_id = db.MilestoneRoots.Where(x => x.milestone_name.ToLower() == MilestoneName).Select(x => x.id).SingleOrDefault(),
                                date_added = DateTime.Now,
                                added_by = User.Identity.Name,
                                //division = cmdb.Identity_Keywords.Where(x => x.Id == approver_details.JobId && x.Type == "Divisions").Select(x => x.Description).SingleOrDefault(),
                                division = userDetails.Where(x => x.id == approver_details.Id).Select(x => x.division).FirstOrDefault(),
                                employee_id = Int32.Parse(approver_details.CMId),
                                main_id = project.main_id,
                                task_id = presetMilestone.ID
                            };

                            db.PreSetMilestoneApprovers.Add(approver_container);
                            db.SaveChanges();
                        }
                    }

                    details_container.Add(task.Requirement);
                }

                log.ActivityLog(User.Identity.Name, 2, "Add Checklist Item/s", "N/A", details_container);
                
                //var approver_ids = approverIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                //List<PreSetMilestoneApprover> approver_list = new List<PreSetMilestoneApprover>();

                //foreach (var item in approver_ids)
                //{
                //    var approver_details = cmdb.AspNetUsers.Where(x => x.Id == item).SingleOrDefault();

                //    var project_list = db.MainTables.Where(x => x.division == division_string).ToList();

                //    foreach(var project in project_list)
                //    {
                //        var approver_container = new PreSetMilestoneApprover
                //        {
                //            approver_name = approver_details.FirstName + " " + approver_details.LastName,
                //            approver_email = approver_details.Email,
                //            milestone_id = db.MilestoneRoots.Where(x => x.milestone_name.ToLower() == MilestoneName).Select(x => x.id).SingleOrDefault(),
                //            date_added = DateTime.Now,
                //            added_by = User.Identity.Name,
                //            //division = cmdb.Identity_Keywords.Where(x => x.Id == approver_details.JobId && x.Type == "Divisions").Select(x => x.Description).SingleOrDefault(),
                //            division = userDetails.Where(x => x.id == approver_details.Id).Select(x => x.division).FirstOrDefault(),
                //            employee_id = Int32.Parse(approver_details.CMId),
                //            main_id = project.main_id,
                //            task_id = milestone.ID
                //        };

                //        approver_list.Add(approver_container);
                //    }
                    
                //}

                //db.PreSetMilestoneApprovers.AddRange(approver_list);
                //db.SaveChanges();

                return Json(new { success = true, message = "Milestone saved successfully!" });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException?.Message ?? ex.Message;
                Debug.WriteLine($"Error saving milestone: {errorMessage}");
                return Json(new { success = false, message = "Error saving milestone: " + errorMessage });
            }
        }

        [HttpPost]
        public JsonResult FinalizeChecklist(int divisionId)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                var division = db.Divisions.FirstOrDefault(d => d.DivisionID == divisionId);
                if (division == null || string.IsNullOrEmpty(division.DivisionCode))
                {
                    return Json(new { success = false, message = "Error: Division code not found." });
                }

                var existingReference = db.ChecklistReferences.FirstOrDefault(r => r.DivisionID == divisionId);
                if (existingReference != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"This checklist has already been finalized. " +
                        $"Reference No. {existingReference.ReferenceNumber}"
                });
                }

       
                var milestonesToUpdate = db.PreSetMilestones
                    .Where(m => m.DivisionID == divisionId && string.IsNullOrEmpty(m.ChecklistNumber))
                    .ToList();

                if (!milestonesToUpdate.Any())
                {
                    return Json(new { success = false, message = "No milestones pending checklist assignment." });
                }

                string finalChecklistReference;
                Random rand = new Random();
                do
                {
                    var randNum1 = rand.Next(1000, 9999);
                    var randNum2 = rand.Next(1000, 9999);
                    finalChecklistReference = $"{division.DivisionCode}-{randNum1}-{randNum2}";
                }
                while (db.ChecklistReferences.Any(c => c.ReferenceNumber == finalChecklistReference));
                var checklistReference = new ChecklistReference
                {
                    DivisionID = divisionId,
                    ReferenceNumber = finalChecklistReference,
                    DateFinalized = DateTime.Now
                };
                db.ChecklistReferences.Add(checklistReference);

                foreach (var milestone in milestonesToUpdate)
                {
                    milestone.ChecklistNumber = finalChecklistReference;
                }

                db.SaveChanges();

                details_container.Add(checklistReference.ReferenceNumber);
                log.ActivityLog(User.Identity.Name, 2, "Finalized Checklist", "N/A", details_container);

                return Json(new
                {
                    success = true,
                    message = finalChecklistReference
                });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException?.Message ?? ex.Message;
                Debug.WriteLine($"Error finalizing checklist: {errorMessage}");
                return Json(new { success = false, message = "Error finalizing checklist: " + errorMessage });
            }
        }

        public ActionResult PreviewMilestone(int id)
        {
            var milestone = db.PreSetMilestones.Find(id);
            if (milestone == null) return HttpNotFound();
            return PartialView("_MilestonePreview", milestone);
        }

        [HttpPost]
        public ActionResult DeleteMilestone(int id)
        {
            var milestone = db.PreSetMilestones.Find(id);
            if (milestone == null) return Json(new { success = false, message = "Not found!" });

            db.PreSetMilestones.Remove(milestone);
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult SaveDivision(int divisionId)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                var existingDivision = db.SavedDivisions.FirstOrDefault(d => d.DivisionID == divisionId);

                if (existingDivision != null)
                {
                    
                    existingDivision.IsActive_ = true;
                    details_container.Add(existingDivision.DivisionName);
                }
                else
                {
                    var division = db.Divisions.FirstOrDefault(d => d.DivisionID == divisionId);
                    if (division == null)
                    {
                        return Json(new { success = false, message = "Invalid Division selected." });
                    }

                    var savedDivision = new SavedDivision
                    {
                        DivisionID = division.DivisionID,
                        DivisionName = division.DivisionName,
                        NoOfMilestones = 0, 
                        NoOfRequirements = 0, 
                        IsActive_ = true 
                    };

                    db.SavedDivisions.Add(savedDivision);
                    details_container.Add(division.DivisionName);
                }

                db.SaveChanges();
                log.ActivityLog(User.Identity.Name, 2, "Add/Activate Milestone", "N/A", details_container);

                return Json(new { success = true, message = "Division added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding division: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSavedDivisions()
        {
            try
            {
                var divisions = db.SavedDivisions
                    .Select(d => new
                    {
                        DivisionID = d.DivisionID,
                        DivisionName = d.DivisionName,

                        ChecklistReference = db.ChecklistReferences
                            .Where(c => c.DivisionID == d.DivisionID)
                            .Select(c => c.ReferenceNumber)
                            .FirstOrDefault(),  // null value if not finalized yet

                        Milestones = db.PreSetMilestones
                            .Where(m => m.DivisionID == d.DivisionID)
                            .Select(m => new { m.Requirements })
                            .ToList()
                    }).ToList();

                var result = divisions.Select(d =>
                {
                    bool isFinalized = !string.IsNullOrEmpty(d.ChecklistReference);

                    int numberOfMilestones = isFinalized ? d.Milestones.Count : 0;

                    int numberOfRequirements = isFinalized
                        ? d.Milestones.Sum(m =>
                              string.IsNullOrEmpty(m.Requirements) ? 0 : m.Requirements.Split(';').Length)
                        : 0;

                    return new
                    {
                        d.DivisionID,
                        d.DivisionName,
                        ChecklistReference = d.ChecklistReference ?? "Pending",
                        NumberOfMilestones = numberOfMilestones,
                        NumberOfRequirements = numberOfRequirements
                    };
                }).ToList();

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching divisions: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult HideDivision(int divisionId)
        {
            try
            {
                var division = db.SavedDivisions.FirstOrDefault(d => d.DivisionID == divisionId);
                if (division == null)
                {
                    return Json(new { success = false, message = "Division not found." });
                }
                division.IsActive_ = false;
                db.SaveChanges();

                return Json(new { success = true, message = "Division will be removed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error hiding division: " + ex.Message });
            }
        }

        public JsonResult GetMilestoneSuggestions(string term)
        {
            var suggestions = db.PreSetMilestones
                .Where(m => m.MilestoneName.Contains(term))
                .Select(m => m.MilestoneName)
                .Take(10)
                .ToList();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetChecklistPreview(int divisionId)
        {
            try
            {
                var division = db.Divisions.FirstOrDefault(d => d.DivisionID == divisionId);
                if (division == null)
                {
                    return Json(new { success = false, message = "Division not found." }, JsonRequestBehavior.AllowGet);
                }

                var milestones = db.PreSetMilestones
                    .Where(m => m.DivisionID == divisionId)
                    .Select(m => new
                    {
                        m.MilestoneName,
                        m.Requirements,
                        ApproverIDs = m.Approvers
                    })
                    .ToList();


                //var allApprovers = cmdb.AspNetUsers.ToList(); 
                var allApprovers = cmdb.AspNetUsers
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToList();


                var milestonesWithApprovers = milestones.Select(m => new
                {
                    m.MilestoneName,
                    m.Requirements,
                    Approvers = string.Join(", ", m.ApproverIDs.Split(',')
                        .Select(id =>
                        {
                            var user = allApprovers.FirstOrDefault(u => u.Id == id);
                            return user != null ? user.FirstName + " " + user.LastName : "Unknown";
                        }))
                }).ToList();


                return Json(new { success = true, divisionName = division.DivisionName, data = milestonesWithApprovers }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching checklist: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER, PMS_PROJECT_MANAGER, PMS_USER, PMS_Management")]
        public ActionResult PendingApprovals()
        {
            var userId = User.Identity.GetUserId();
            var userEmail = User.Identity.GetUserName().ToLower().Trim();

            bool isInApproversTbls = db.ApproversTbls.Any(a => a.User_Id == userId && a.IsRemoved_ == false);
            bool isInOptionalApprovers = db.OptionalMilestoneApprovers.Any(a => a.approver_email.ToLower().Trim() == userEmail);
            bool isInPresetApprovers = db.PreSetMilestoneApprovers.Any(a => a.approver_email.ToLower().Trim() == userEmail);

            ViewBag.IsApprover = isInApproversTbls || isInOptionalApprovers || isInPresetApprovers;

            // fetch approvers rec 
            var approverTasks = db.ApproversTbls
                .Where(a => a.User_Id == userId && a.IsRemoved_ == false)
                .ToList();

            var taskIds = approverTasks.Select(a => a.Details_Id).ToList();
            var projectIds = approverTasks.Select(a => a.Main_Id).ToList();

            var tasks = db.DetailsTbls.Where(t => taskIds.Contains(t.details_id)).ToList();
            var projects = db.MainTables.Where(p => projectIds.Contains(p.main_id)).ToList();
            var attachments = db.AttachmentTables.Where(att => taskIds.Contains(att.details_id)).ToList();

            var userIds = approverTasks.Select(a => a.User_Id).Distinct().ToList();
            var users = cmdb.AspNetUsers.Where(u => userIds.Contains(u.Id)).ToList();

            var pendingTasks = approverTasks.Select(a => new ApproverTaskViewModel
            {
                DetailsID = a.Details_Id ?? 0,
                TaskName = tasks.FirstOrDefault(t => t.details_id == a.Details_Id)?.process_title ?? "N/A",
                ProjectTitle = projects.FirstOrDefault(p => p.main_id == a.Main_Id)?.project_title ?? "N/A",
                SubmittedBy = users.FirstOrDefault(u => u.Id == a.User_Id)?.UserName ?? "Unknown",
                SubmittedDate = tasks.FirstOrDefault(t => t.details_id == a.Details_Id)?.created_date ?? DateTime.MinValue,
                AttachmentID = attachments.FirstOrDefault(att => att.details_id == a.Details_Id)?.details_id ?? 0,
                FilePath = attachments.FirstOrDefault(att => att.details_id == a.Details_Id)?.path_file
            }).ToList();

            return View(pendingTasks);
        }

        [HttpGet]
        [Authorize]
        public JsonResult GetApprovals()
        {
            var userEmail = User.Identity.Name.ToLower().Trim();

            // load maintable once to avoid duplicate join 
            var checklistMainIds = db.ChecklistSubmissions
                .Where(s => s.is_removed != true)
                .Select(s => s.main_id)
                .Distinct()
                .ToList();

            var mainTableLookup = db.MainTables
                .Where(m => checklistMainIds.Contains(m.main_id))
                .ToDictionary(m => m.main_id, m => m.project_title);

            List<ApprovalTaskDTO> optionalMileStone = new List<ApprovalTaskDTO>();

            //get all optionalMilestoneApprovers based on userEmail
            var _optional = db.OptionalMilestoneApprovers.Where(x => x.approver_email == userEmail && (x.is_removed != true) && x.task_id.HasValue).ToList();

            foreach (var row in _optional)
            {
                var getTask = db.OptionalMilestones.FirstOrDefault(x => x.id == row.task_id);
                if(getTask != null)
                {
                    var getCheckListSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == row.task_id && x.type == "optional");

                    if(getCheckListSubmission != null)
                    {
                        var getMaintbl = db.MainTables.FirstOrDefault(x => x.main_id == getCheckListSubmission.main_id);

                        if(getMaintbl != null)
                        {
                            var getOptional = new ApprovalTaskDTO
                            {
                                isApproved = row.approved,
                                isRejected = row.rejected,

                                task_id = row.task_id.Value,
                                task_name = getCheckListSubmission.task_name,
                                submitted_by = getCheckListSubmission.submitted_by,
                                submission_date = getCheckListSubmission.submission_date,
                                main_id = getCheckListSubmission.main_id.Value,
                                milestone_id = getCheckListSubmission.milestone_id.Value,
                                project_title = getMaintbl.project_title,
                            };

                            optionalMileStone.Add(getOptional);
                        }

                    }
                    
                }
                
            }

            //Pre-set task
            var _preset = db.PreSetMilestoneApprovers.Where(x => x.approver_email == userEmail && (x.is_removed != true) && x.task_id.HasValue).ToList();

            foreach (var row in _preset)
            {
                var getTask = db.PreSetMilestones.FirstOrDefault(x => x.ID == row.task_id);
                if(getTask != null)
                {
                    var getCheckListSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == row.task_id && x.type == "preset");

                    if(getCheckListSubmission != null)
                    {
                        var getMaintbl = db.MainTables.FirstOrDefault(x => x.main_id == getCheckListSubmission.main_id);

                        if (getMaintbl != null)
                        {
                            var getPreset = new ApprovalTaskDTO
                            {
                                isApproved = row.approved,
                                isRejected = row.rejected,

                                task_id = row.task_id.Value,
                                task_name = getCheckListSubmission.task_name,
                                submitted_by = getCheckListSubmission.submitted_by,
                                submission_date = getCheckListSubmission.submission_date,
                                main_id = getCheckListSubmission.main_id.Value,
                                milestone_id = getCheckListSubmission.milestone_id.Value,
                                project_title = getMaintbl.project_title,
                            };

                            optionalMileStone.Add(getPreset);
                        }
                    }

                }
                
            }

            var pending = optionalMileStone.Where(x => (x.isApproved == null) && (x.isRejected == null || x.isRejected == false));
            var approved = optionalMileStone.Where(x => x.isApproved == true);
            var rejected = optionalMileStone.Where(x => x.isRejected == true);

            return Json(new
            {
                pending = pending.Select(x => new
                {
                    DetailsID = x.task_id,
                    TaskName = x.task_name,
                    ProjectTitle = mainTableLookup.ContainsKey(x.main_id) ? mainTableLookup[x.main_id] : "N/A",
                    ProjectMainId = x.main_id,
                    MilestoneID = x.milestone_id,
                    SubmittedBy = x.submitted_by,
                    SubmittedDate = x.submission_date
                }),
                approved = approved.Select(x => new
                {
                    DetailsID = x.task_id,
                    TaskName = x.task_name,
                    ProjectTitle = mainTableLookup.ContainsKey(x.main_id) ? mainTableLookup[x.main_id] : "N/A",
                    ProjectMainId = x.main_id,
                    MilestoneID = x.milestone_id,
                    SubmittedBy = x.submitted_by,
                    SubmittedDate = x.submission_date
                }),
                rejected = rejected.Select(x => new
                {
                    DetailsID = x.task_id,
                    TaskName = x.task_name,
                    ProjectTitle = mainTableLookup.ContainsKey(x.main_id) ? mainTableLookup[x.main_id] : "N/A",
                    ProjectMainId = x.main_id,
                    MilestoneID = x.milestone_id,
                    SubmittedBy = x.submitted_by,
                    SubmittedDate = x.submission_date
                })
            }, JsonRequestBehavior.AllowGet);
        }

        // OLD
        //[HttpPost]
        //public JsonResult ApproveTask(int taskId)
        //{
        //    try
        //    {
        //        string userEmail = User.Identity.Name.ToLower().Trim();

        //        var optional = db.OptionalMilestoneApprovers.FirstOrDefault(x =>
        //            x.task_id == taskId &&
        //            x.approver_email.ToLower().Trim() == userEmail &&
        //            x.is_removed != true
        //        );

        //        var preset = db.PreSetMilestoneApprovers.FirstOrDefault(x =>
        //            x.task_id == taskId &&
        //            x.approver_email.ToLower().Trim() == userEmail &&
        //            x.is_removed != true
        //        );

        //        if (optional == null && preset == null)
        //        {
        //            return Json(new { success = false, message = "Task not found or not assigned to you." });
        //        }

        //        if (optional != null)
        //        {
        //            optional.approved = true;
        //            optional.rejected = false;
        //            optional.date_approved = DateTime.Now;
        //        }

        //        if (preset != null)
        //        {
        //            preset.approved = true;
        //            preset.rejected = false;
        //            preset.date_approved = DateTime.Now;
        //        }

        //        db.SaveChanges();

        //        return Json(new { success = true, message = "Task approved successfully!" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error: " + ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult ApproveTask(int taskId, int milestoneId)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                string userEmail = User.Identity.Name.ToLower().Trim();

                var submission = db.ChecklistSubmissions
                    .FirstOrDefault(x => x.task_id == taskId && x.milestone_id == milestoneId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                submission.is_approved = true;
                submission.submission_date = DateTime.Now;

                var preset = db.PreSetMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.approver_email.ToLower().Trim() == userEmail && x.is_removed != true);

                var optional = db.OptionalMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.approver_email.ToLower().Trim() == userEmail && x.is_removed != true);

                if (preset != null)
                {
                    preset.approved = true;
                    preset.rejected = false;
                    preset.date_approved = DateTime.Now;
                }

                if (optional != null)
                {
                    optional.approved = true;
                    optional.rejected = false;
                    optional.date_approved = DateTime.Now;
                }

                db.SaveChanges();

                var main_id = submission.main_id;
                var milestone_id = submission.milestone_id;

                var milestone_submissions = db.ChecklistSubmissions.Where(x => x.main_id == main_id && x.milestone_id == milestone_id && (x.is_removed != true || x.is_removed == null)).ToList();

                if (milestone_submissions.Any())
                {
                    var complete = true;

                    foreach (var sub in milestone_submissions)
                    {
                        if (sub.is_approved == null || sub.is_approved == false)
                        {
                            complete = false;
                            break;
                        }
                    }

                    if (complete == true)
                    {
                        var checklistTbl = db.ChecklistTables.Where(x => x.main_id == main_id && x.milestone_id == milestone_id).OrderByDescending(x => x.checklist_id).FirstOrDefault();
                        checklistTbl.is_approved = true;
                        checklistTbl.approval_date = DateTime.Now;

                        var milestoneTbl = db.MilestoneTbls.Where(x => x.main_id == main_id && x.root_id == milestone_id).OrderByDescending(x => x.milestone_id).FirstOrDefault();
                        milestoneTbl.IsCompleted = true;
                        milestoneTbl.actual_completion_date = DateTime.Now;

                        var mainTbl = db.MainTables.Where(x => x.main_id == main_id).SingleOrDefault();
                        var projectTitle = mainTbl.project_title;

                        var milestoneTitle = db.MilestoneRoots.Where(x => x.id == milestone_id).Select(x => x.milestone_name).SingleOrDefault();
                        var membersTbl = db.ProjectMembersTbls.Where(x => x.project_id == main_id).ToList();

                        var project_manager = membersTbl.Where(x => x.role == 1004).ToList();

                        foreach(var row in project_manager)
                        {
                            var taskName = submission.task_name;
                            string approverName = userEmail;
                            using (var cmdb = new CMIdentityDBEntities())
                            {
                                var user = cmdb.AspNetUsers.FirstOrDefault(u => u.Email.ToLower() == userEmail);
                                if (user != null && !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                                {
                                    approverName = user.FirstName + " " + user.LastName;
                                }
                            }

                            var systemEmail = "e-notify@enchantedkingdom.ph";
                            var systemName = "PM SYSTEM";
                            var email = new MimeMessage();

                            email.From.Add(new MailboxAddress(systemName, systemEmail));
                            email.To.Add(new MailboxAddress(row.name, row.email));
                            email.To.Add(new MailboxAddress("Crystal Joyce Benauro", "cbenauro@enchantedkingdom.ph")); // test email

                            email.Subject = "PM System Approval";
                            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                            {
                                Text = $@"
                        <div style='font-family: Poppins, Arial, sans-serif; background-color: #f4f4f9; padding: 0; margin: 0;'>
                            <table align='center' cellpadding='0' cellspacing='0' width='640' style='margin: auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 3px 10px rgba(0,0,0,0.05); border: 1px solid #e0e0e0;'>
                                <tr>
                                    <td style='background-color: #66339A; padding: 24px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center;'>
                                        <h1 style='margin: 0; font-size: 22px; color: #ffffff; font-weight: 600;'>Your Checklist Item Has Been Approved</h1>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='padding: 30px 40px;'>
                                        <p style='font-size: 15px; color: #444; margin-bottom: 24px;'>
                                            This is to notify you that a task has been <strong style='color:#28a745;'>approved</strong> under your project titled <strong>{projectTitle}</strong>.
                                        </p>

                                        <table cellpadding='0' cellspacing='0' width='100%' style='font-size: 14px; color: #333; line-height: 1.6; border-collapse: collapse; margin-top: 10px;'>
                                            <tr style='border-bottom: 1px solid #eee;'>
                                                <td style='padding: 10px; text-align: right; width: 40%;'><strong>Checklist Item:</strong></td>
                                                <td style='padding: 10px; text-align: left;'>{taskName}</td>
                                            </tr>
                                            <tr style='border-bottom: 1px solid #eee;'>
                                                <td style='padding: 10px; text-align: right;'><strong>Milestone Name:</strong></td>
                                                <td style='padding: 10px; text-align: left;'>{milestoneTitle}</td>
                                            </tr>
                                            <tr style='border-bottom: 1px solid #eee;'>
                                                <td style='padding: 10px; text-align: right;'><strong>Approved By:</strong></td>
                                                <td style='padding: 10px; text-align: left;'>{approverName}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 10px; text-align: right;'><strong>Date Approved:</strong></td>
                                                <td style='padding: 10px; text-align: left;'>{DateTime.Now.ToString("MMMM dd, yyyy h:mm tt")}</td>
                                            </tr>
                                        </table>

                                        <div style='text-align: center; margin: 30px 0;'>
                                            <a href='http://localhost:60297/Admin/PendingApprovals'
                                               style='display: inline-block; padding: 14px 40px; background-color: #66339A; color: #fff; text-decoration: none; font-weight: bold; border-radius: 5px; font-size: 16px;'>
                                               View Task
                                            </a>
                                        </div>

                                        <p style='font-size: 13px; color: #888; margin-top: 40px; text-align: center;'>
                                            If you have questions or require assistance, please contact your supervisor or ITS.
                                        </p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='background-color: #f0f0f5; text-align: center; padding: 14px; font-size: 12px; color: #999; border-bottom-left-radius: 8px; border-bottom-right-radius: 8px;'>
                                        <em>This is an automated email from the Project Management System</em>. Do not reply.<br/>Need help? Call <strong>ITS Local 123/132</strong>.
                                    </td>
                                </tr>
                            </table>
                        </div>"
                            };


                            using (var smtp = new SmtpClient())
                            {
                                smtp.Connect("mail.enchantedkingdom.ph", 587, false);
                                smtp.Authenticate("e-notify@enchantedkingdom.ph", "ENCHANTED2024");
                                smtp.Send(email);
                                smtp.Disconnect(true);
                            }
                        }

   

                        db.SaveChanges();

                        details_container.Add(submission.task_name);
                        log.ActivityLog(User.Identity.Name, 9, "Task approval", db.MainTables.Where(x => x.main_id == submission.main_id).OrderByDescending(x => x.main_id).Select(x => x.project_title).FirstOrDefault(), details_container);
                    }
                }

                return Json(new { success = true, message = "Task approved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }



        // OLD
        //[HttpPost]
        //public JsonResult RejectTask(int taskId, string reason)
        //{
        //    try
        //    {
        //        var userId = User.Identity.GetUserId();

        //        var task = db.ApproversTbls.FirstOrDefault(a => a.Details_Id == taskId && a.User_Id == userId);

        //        if (task == null)
        //        {
        //            return Json(new { success = false, message = "Task not found" });
        //        }

        //        task.IsApproved_ = false;
        //        task.IsRejected_ = true;
        //        task.RejectReason = reason;
        //        task.ApprovalDate = DateTime.Now;

        //        db.SaveChanges();

        //        return Json(new { success = true, message = "Task rejected! :( " });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error: " + ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult RejectTask(int taskId, string reason)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                string currentEmail = User.Identity.Name?.ToLower().Trim();
                var submission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                submission.is_approved = false;
                submission.submission_date = DateTime.Now;
                submission.disapproval_reason = reason;

                var preset = db.PreSetMilestoneApprovers.FirstOrDefault(x => x.task_id == taskId && x.is_removed != true && x.approver_email == currentEmail);
                var optional = db.OptionalMilestoneApprovers.FirstOrDefault(x => x.task_id == taskId && x.is_removed != true && x.approver_email == currentEmail);

                if (preset != null)
                {
                    preset.approved = false;
                    preset.rejected = true;
                    preset.date_approved = DateTime.Now;
                }

                if (optional != null)
                {
                    optional.approved = false;
                    optional.rejected = true;
                    optional.date_approved = DateTime.Now;
                }

                db.SaveChanges();

                var main_id = submission.main_id;
                var milestone_id = submission.milestone_id;

                var dbChecklist = db.ChecklistTables.Where(x => x.main_id == main_id && x.milestone_id == milestone_id).OrderByDescending(x => x.checklist_id).FirstOrDefault();
                dbChecklist.disapproval_reason = submission.disapproval_reason;
                dbChecklist.is_approved = false;
                db.SaveChanges();

                var mainTbl = db.MainTables.Where(x => x.main_id == main_id).SingleOrDefault();
                var projectTitle = mainTbl.project_title;

                var milestoneTitle = db.MilestoneRoots.Where(x => x.id == milestone_id).Select(x => x.milestone_name).SingleOrDefault();
                var membersTbl = db.ProjectMembersTbls.Where(x => x.project_id == main_id).ToList();
                var project_manager = membersTbl.Where(x => x.role == 1004).SingleOrDefault();

                var taskName = submission.task_name;
                string approverName = currentEmail;
                using (var cmdb = new CMIdentityDBEntities())
                {
                    var user = cmdb.AspNetUsers.FirstOrDefault(u => u.Email.ToLower() == currentEmail);
                    if (user != null && !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                    {
                        approverName = user.FirstName + " " + user.LastName;
                    }
                }

                var systemEmail = "e-notify@enchantedkingdom.ph";
                var systemName = "PM SYSTEM";
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(systemName, systemEmail));
                email.To.Add(new MailboxAddress(project_manager.name, project_manager.email));
                email.To.Add(new MailboxAddress("Crystal Joyce Benauro", "cbenauro@enchantedkingdom.ph"));
                email.Subject = "PM System Disapproval";

                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = $@"
                <div style='font-family: Poppins, Arial, sans-serif; background-color: #f4f4f9; padding: 0; margin: 0;'>
                    <table align='center' cellpadding='0' cellspacing='0' width='640' style='margin: auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 3px 10px rgba(0,0,0,0.05); border: 1px solid #e0e0e0;'>
                        <tr>
                            <td style='background-color: #66339A; padding: 24px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center;'>
                                <h1 style='margin: 0; font-size: 22px; color: #ffffff; font-weight: 600;'>Your Checklist Item Has Been Disapproved</h1>
                            </td>
                        </tr>

                        <tr>
                            <td style='padding: 30px 40px;'>
                                <p style='font-size: 15px; color: #444; margin-bottom: 24px;'>
                                    This is to notify you that a task has been <strong style='color:#dc3545;'>disapproved</strong> under your project titled <strong>{projectTitle}</strong>.
                                </p>

                                <table cellpadding='0' cellspacing='0' width='100%' style='font-size: 14px; color: #333; line-height: 1.6; border-collapse: collapse; margin-top: 10px;'>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; text-align: right; width: 40%;'><strong>Checklist Item:</strong></td>
                                        <td style='padding: 10px; text-align: left;'>{taskName}</td>
                                    </tr>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; text-align: right;'><strong>Milestone Name:</strong></td>
                                        <td style='padding: 10px; text-align: left;'>{milestoneTitle}</td>
                                    </tr>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; text-align: right;'><strong>Disapproved By:</strong></td>
                                        <td style='padding: 10px; text-align: left;'>{approverName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px; text-align: right;'><strong>Date Disapproved:</strong></td>
                                        <td style='padding: 10px; text-align: left;'>{DateTime.Now.ToString("MMMM dd, yyyy h:mm tt")}</td>
                                    </tr>
                                </table>

                                <div style='margin-top: 30px; padding: 16px 20px; background-color: #fae3e3; border-left: 5px solid #dc3545; border-radius: 6px;'>
                                    <p style='margin: 0; color: #dc3545; font-style: italic;'><strong>Reason:</strong> {reason}</p>
                                </div>

                                <p style='font-size: 13px; color: #888; margin-top: 40px; text-align: center;'>
                                    If you have questions or require assistance, please contact your supervisor or ITS.
                                </p>
                            </td>
                        </tr>

                        <tr>
                            <td style='background-color: #f0f0f5; text-align: center; padding: 14px; font-size: 12px; color: #999; border-bottom-left-radius: 8px; border-bottom-right-radius: 8px;'>
                                <em>This is an automated email from the Project Management System</em>. Do not reply.<br/>Need help? Call <strong>ITS Local 123/132</strong>.
                            </td>
                        </tr>
                    </table>
                </div>"
                };


                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("mail.enchantedkingdom.ph", 587, false);
                    smtp.Authenticate("e-notify@enchantedkingdom.ph", "ENCHANTED2024");
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }

                details_container.Add(submission.task_name);
                log.ActivityLog(User.Identity.Name, 6, "Task Rejection", db.MainTables.Where(x => x.main_id == submission.main_id).OrderByDescending(x => x.main_id).Select(x => x.project_title).FirstOrDefault(), details_container);

                return Json(new { success = true, message = "Task rejected!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult WithdrawTask(int taskId, string reason)
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            try
            {
                var submission = db.ChecklistSubmissions
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                string currentEmail = User.Identity.Name?.ToLower().Trim();
                string withdrawnByName = currentEmail;

                using (var cmdb = new CMIdentityDBEntities())
                {
                    var user = cmdb.AspNetUsers.FirstOrDefault(u => u.Email.ToLower() == currentEmail);

                    if (user != null)
                    {
                        string firstName = user.FirstName?.Trim();
                        string lastName = user.LastName?.Trim();

                        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                        {
                            withdrawnByName = $"{firstName} {lastName}";
                        }
                    }
                }

                submission.is_approved = null;
                submission.submission_date = DateTime.Now;

                var preset = db.PreSetMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true && x.approver_email == currentEmail);

                var optional = db.OptionalMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true && x.approver_email == currentEmail);

                string previousStatus = "Pending";

                if (preset != null)
                {
                    if (preset.approved == true) previousStatus = "Approved";
                    else if (preset.rejected == true) previousStatus = "Rejected";

                    preset.approved = null;
                    preset.rejected = null;
                    preset.date_approved = DateTime.Now;
                    preset.withdraw_status = reason;
                }

                if (optional != null)
                {
                    if (optional.approved == true) previousStatus = "Approved";
                    else if (optional.rejected == true) previousStatus = "Rejected";

                    optional.approved = null;
                    optional.rejected = null;
                    optional.date_approved = DateTime.Now;
                    optional.withdraw_reason = reason;
                }

                db.SaveChanges();

                var main_id = submission.main_id;
                var milestone_id = submission.milestone_id;
                var taskName = submission.task_name;

                var dbChecklist = db.ChecklistTables
                    .Where(x => x.main_id == main_id && x.milestone_id == milestone_id)
                    .OrderByDescending(x => x.checklist_id)
                    .FirstOrDefault();

                if (dbChecklist != null)
                {
                    dbChecklist.disapproval_reason = submission.disapproval_reason;
                    dbChecklist.is_approved = false;
                }

                db.SaveChanges();

                var mainTbl = db.MainTables.FirstOrDefault(x => x.main_id == main_id);
                var projectTitle = mainTbl?.project_title;

                var milestoneTitle = db.MilestoneRoots
                    .Where(x => x.id == milestone_id)
                    .Select(x => x.milestone_name)
                    .FirstOrDefault();

                var project_manager = db.ProjectMembersTbls
                    .FirstOrDefault(x => x.project_id == main_id && x.role == 1004);

                var subjectHeading = previousStatus == "Approved" ? "Approval Withdrawal"
                                 : previousStatus == "Rejected" ? "Rejection Withdrawal"
                                 : "Task Withdrawal";

                // ✅ Send Email
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("PM SYSTEM", "e-notify@enchantedkingdom.ph"));
                email.To.Add(new MailboxAddress(project_manager.name, project_manager.email));
                email.To.Add(new MailboxAddress("Crystal Joyce Benauro", "cbenauro@enchantedkingdom.ph"));

                email.Subject = "PM System " + subjectHeading;
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = $@"
                <div style='font-family: Poppins, Arial, sans-serif; background-color: #f4f4f9; padding: 0; margin: 0;'>
                    <table align='center' cellpadding='0' cellspacing='0' width='640' style='margin: auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 3px 10px rgba(0,0,0,0.05); border: 1px solid #e0e0e0;'>
                        <tr>
                            <td style='background-color: #66339A; padding: 24px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center;'>
                                <h1 style='margin: 0; font-size: 22px; color: #ffffff; font-weight: 600;'>{subjectHeading}</h1>
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 30px 40px;'>
                                <p style='font-size: 15px; color: #444; margin-bottom: 24px;'>
                                    This is to notify you that a task has been <strong>withdrawn</strong> from the checklist under your project title <strong>{projectTitle}</strong>.
                                </p>
                                <p style='font-size: 14px; color: #555; margin-bottom: 24px;'>
                                    You may view this checklist item under your <strong>Pending Approvals</strong> tab.
                                </p>
                                <table cellpadding='0' cellspacing='0' width='100%' style='font-size: 14px; color: #333; line-height: 1.6; border-collapse: collapse; margin-top: 10px;'>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; vertical-align: top;'><strong>Checklist Item:</strong></td>
                                        <td style='padding: 10px;'>{taskName}</td>
                                    </tr>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; vertical-align: top;'><strong>Milestone Name:</strong></td>
                                        <td style='padding: 10px;'>{milestoneTitle}</td>
                                    </tr>
                                    <tr style='border-bottom: 1px solid #eee;'>
                                        <td style='padding: 10px; vertical-align: top;'><strong>Withdrawn By:</strong></td>
                                        <td style='padding: 10px;'>{withdrawnByName}</td>
                                    </tr>   
                                </table>
                                <div style='margin-top: 30px; padding: 16px 20px; background-color: #f7f1fa; border-left: 5px solid #66339A; border-radius: 6px;'>
                                    <p style='margin: 0; color: #66339A; font-style: italic; font-weight: normal;'>Reason: {reason}</p>
                                </div>
                                <p style='font-size: 13px; color: #888; margin-top: 40px; text-align: center; font-weight: normal;'>
                                    If you have questions or require assistance, please contact your supervisor or ITS.
                                </p>
                            </td>
                        </tr>
                        <tr>
                            <td style='background-color: #f0f0f5; text-align: center; padding: 14px; font-size: 12px; color: #999; border-bottom-left-radius: 8px; border-bottom-right-radius: 8px;'>
                                <em>This is an automated email from the Project Management System. Do not reply.<br/>Need help? Call <strong>ITS Local 123/132</strong>.</em>
                            </td>
                        </tr>
                    </table>
                </div>"
                };

                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("mail.enchantedkingdom.ph", 587, false);
                    smtp.Authenticate("e-notify@enchantedkingdom.ph", "ENCHANTED2024");
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }

                // Notification
                if (project_manager != null)
                {
        
                    var notifHelper = new NotificationHelper(db);
                    notifHelper.CreateNotification(
                        userFullName: project_manager.name,
                       message: $"<div style='text-align: justify;'>Task <strong>{taskName}</strong> has been withdrawn from project <strong>{projectTitle}</strong>.</div>",
                        link: $"/checklist/weeklyMilestone?id={main_id}&title={HttpUtility.UrlEncode(projectTitle)}&tab=checklist",
                        type: "task_withdrawal",
                        mainId: main_id,
                        milestoneId: milestone_id,
                        taskId: taskId
                    );

                }

                details_container.Add(submission.task_name);
                log.ActivityLog(User.Identity.Name, 6, "Task Approval/Rejection Withdrawal", db.MainTables.Where(x => x.main_id == submission.main_id).OrderByDescending(x => x.main_id).Select(x => x.project_title).FirstOrDefault(), details_container);

                return Json(new { success = true, message = "Task withdrawn!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }




        [HttpGet]
        public JsonResult GetApproversByTask(int taskId)
        {
            try
            {
                //Console.WriteLine($"Fetching All Users from AspNetUsers for Task ID: {taskId}");

                var allUsers = cmdb.AspNetUsers
                    .Select(user => new
                    {
                        Id = user.Id,
                        FullName = user.FirstName + " " + user.LastName,
                        Email = user.Email
                    })
                    .ToList();

                var assignedApprovers = db.ApproversTbls
                    .Where(a => a.Details_Id == taskId)
                    .Select(a => new { a.User_Id })
                    .ToList();

                var userList = allUsers.Select(user => new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    IsSelected = assignedApprovers.Any(a => a.User_Id == user.Id)
                });
                return Json(userList, JsonRequestBehavior.AllowGet);

            } 
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching users: " + ex.Message);
                return Json(new { success = false, message = "Error fetching users: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public JsonResult GetPendingApprovals()
        {
            try
            {
                string userEmail = User.Identity.Name;

                var pendingOptional = (from s in db.ChecklistSubmissions
                                       where s.approval_enabled == true && s.is_removed != true && s.type == "optional"
                                       join a in db.OptionalMilestoneApprovers.Where(x => x.approver_email == userEmail && x.is_removed != true)
                                           on new { s.main_id, s.milestone_id, s.task_id } equals new { a.main_id, a.milestone_id, a.task_id }
                                       join m in db.MainTables on s.main_id equals m.main_id
                                       select new ApproverTaskViewModel
                                       {
                                           DetailsID = s.task_id.Value,
                                           TaskName = s.task_name,
                                           ProjectTitle = m.project_title,
                                           SubmittedBy = s.submitted_by,
                                           SubmittedDate = s.submission_date ?? DateTime.Now,
                                           ApprovedCount = db.OptionalMilestoneApprovers.Count(x => x.task_id == s.task_id && x.approved == true && x.is_removed != true),
                                           TotalApprovers = db.OptionalMilestoneApprovers.Count(x => x.task_id == s.task_id && x.is_removed != true)
                                       }).ToList();

                var pendingPreset = (from s in db.ChecklistSubmissions
                                     where s.approval_enabled == true && s.is_removed != true && s.type == "preset"
                                     join a in db.PreSetMilestoneApprovers.Where(x => x.approver_email == userEmail && x.is_removed != true)
                                         on new { s.main_id, s.milestone_id, s.task_id } equals new { a.main_id, a.milestone_id, a.task_id }
                                     join m in db.MainTables on s.main_id equals m.main_id
                                     select new ApproverTaskViewModel
                                     {
                                         DetailsID = s.task_id.Value,
                                         TaskName = s.task_name,
                                         ProjectTitle = m.project_title,
                                         SubmittedBy = s.submitted_by,
                                         SubmittedDate = s.submission_date ?? DateTime.Now,
                                         ApprovedCount = db.PreSetMilestoneApprovers.Count(x => x.task_id == s.task_id && x.approved == true && x.is_removed != true),
                                         TotalApprovers = db.PreSetMilestoneApprovers.Count(x => x.task_id == s.task_id && x.is_removed != true)
                                     }).ToList();

                var pendingTasks = new List<ApproverTaskViewModel>();
                pendingTasks.AddRange(pendingOptional);
                pendingTasks.AddRange(pendingPreset);

                return Json(pendingTasks, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading pending approvals" }, JsonRequestBehavior.AllowGet);
            }
        } 

        

        public ActionResult RedirectToChecklist(int id, string title, string projectId, int milestone, int checklistId)
        {
            TempData["FromApproval"] = true;
            TempData["CurrentUserEmail"] = User.Identity.Name; 

            return RedirectToAction("weeklyMilestone", "Checklist", new
            {
                id,
                title,
                projectId,
                milestone,
                tab = "checklist",
                checklistId
            });
        }

        public void CheckApprovedMilestone(int _task_id)
        {
            var checklistSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == _task_id);
            if (checklistSubmission == null)
            {
                return;
            }

            List<dynamic> milestoneApprovers = null;

            switch (checklistSubmission.type)
            {
                case "preset":
                    milestoneApprovers = db.PreSetMilestoneApprovers
                        .Where(x => x.task_id == _task_id)
                        .Select(x => new { x.approved })
                        .ToList<dynamic>();
                    break;

                case "optional":
                    milestoneApprovers = db.OptionalMilestoneApprovers
                        .Where(x => x.task_id == _task_id)
                        .Select(x => new { x.approved })
                        .ToList<dynamic>();
                    break;

                default:
                    return;
            }

            bool allApproversApprove = milestoneApprovers.All(x => x.approved == true);

            if (allApproversApprove)
            {
                // Update checklistSubmission here
            }
        }

    }
}
