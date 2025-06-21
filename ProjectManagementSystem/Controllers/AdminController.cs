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

                Approvers = string.IsNullOrEmpty(m.Approvers)
                    ? new List<string>()
                    : m.Approvers.Split(',')
                        .Select(id => cmdb.AspNetUsers
                            .Where(u => u.Id == id)
                            .Select(u => u.FirstName + " " + u.LastName)
                            .FirstOrDefault() ?? "Unknown Approver")
                        .ToList()
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

                var milestone = new PreSetMilestone
                {
                    MilestoneID = milestoneId,
                    DivisionID = DivisionID,
                    MilestoneName = MilestoneName,
                    Sorting = nextSorting,
                    Requirements = requirements,
                    Approvers = approverIds,
                    CreatedDate = DateTime.Now,
                    ChecklistNumber = "",
                    DivisionCodeNumber = divisionCodeNumber,
                    division_string = db.Divisions.Where(x => x.DivisionID == DivisionID).Select(x => x.DivisionName).FirstOrDefault()
                };

                db.PreSetMilestones.Add(milestone);
                db.SaveChanges();

                var approver_ids = approverIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<PreSetMilestoneApprover> approver_list = new List<PreSetMilestoneApprover>();

                foreach (var item in approver_ids)
                {
                    var approver_details = cmdb.AspNetUsers.Where(x => x.Id == item).SingleOrDefault();

                    var project_list = db.MainTables.Where(x => x.division == division_string).ToList();

                    foreach(var project in project_list)
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
                            task_id = milestone.ID
                        };

                        approver_list.Add(approver_container);
                    }
                    
                }

                db.PreSetMilestoneApprovers.AddRange(approver_list);
                db.SaveChanges();

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
            try
            {
                var existingDivision = db.SavedDivisions.FirstOrDefault(d => d.DivisionID == divisionId);

                if (existingDivision != null)
                {
                    
                    existingDivision.IsActive_ = true;
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
                }

                db.SaveChanges();

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
            var _optional = db.OptionalMilestoneApprovers.Where(x => x.approver_email == userEmail && (x.is_removed == false || x.is_removed == null) && x.task_id.HasValue).ToList();

            foreach (var row in _optional)
            {
                var getTask = db.OptionalMilestones.FirstOrDefault(x => x.id == row.task_id);
                var getCheckListSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == row.task_id);
                var getMaintbl = db.MainTables.FirstOrDefault(x => x.main_id == getCheckListSubmission.main_id);

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

            //Pre-set task
            var _preset = db.PreSetMilestoneApprovers.Where(x => x.approver_email == userEmail && (x.is_removed != true || x.is_removed == null) && x.task_id.HasValue).ToList();

            foreach (var row in _preset)
            {
                var getTask = db.OptionalMilestones.FirstOrDefault(x => x.id == row.task_id);
                var getCheckListSubmission = db.ChecklistSubmissions.FirstOrDefault(x => x.task_id == row.task_id);
                var getMaintbl = db.MainTables.FirstOrDefault(x => x.main_id == getCheckListSubmission.main_id);

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

            // optional
            //var optional = (from s in db.ChecklistSubmissions
            //                join a in db.OptionalMilestoneApprovers
            //                  on new { s.main_id, s.milestone_id, s.task_id }
            //                  equals new { a.main_id, a.milestone_id, a.task_id }
            //                join m in db.MainTables on s.main_id equals m.main_id
            //                where a.approver_email.ToLower().Trim() == userEmail
            //                   && (a.is_removed == false || a.is_removed == null)
            //                   && (s.is_removed == false || s.is_removed == null)
            //                select new ApprovalTaskDTO
            //                {
            //                    task_id = s.task_id.Value,
            //                    task_name = s.task_name,
            //                    submitted_by = s.submitted_by,
            //                    submission_date = s.submission_date,
            //                    main_id = s.main_id.Value,
            //                    milestone_id = s.milestone_id.Value,
            //                    project_title = m.project_title,
            //                    isApproved = a.approved,
            //                    isRejected = a.rejected
            //                }).ToList();

            // preset 
            //var preset = (from s in db.ChecklistSubmissions
            //              join a in db.PreSetMilestoneApprovers
            //                on new { s.main_id, s.milestone_id, s.task_id }
            //                equals new { a.main_id, a.milestone_id, a.task_id }
            //              join m in db.MainTables on s.main_id equals m.main_id
            //              where a.approver_email.ToLower().Trim() == userEmail
            //                 && (a.is_removed == false || a.is_removed == null)
            //                 && (s.is_removed == false || s.is_removed == null)
            //              select new ApprovalTaskDTO
            //              {
            //                  task_id = s.task_id.Value,
            //                  task_name = s.task_name,
            //                  submitted_by = s.submitted_by,
            //                  submission_date = s.submission_date,
            //                  main_id = s.main_id.Value,
            //                  milestone_id = s.milestone_id.Value,
            //                  project_title = m.project_title,
            //                  isApproved = a.approved,
            //                  isRejected = a.rejected
            //              }).ToList();

            // combine then filter
            //var all = optional.Concat(preset).Distinct().ToList();

            var pending = optionalMileStone.Where(x => (x.isApproved == null) && x.isRejected == null);
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
        public JsonResult ApproveTask(int taskId)
        {
            try
            {
                string userEmail = User.Identity.Name.ToLower().Trim();

                // Get the submission task from ChecklistSubmissions
                var submission = db.ChecklistSubmissions
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                // Mark the task as approved
                submission.is_approved = true;
                submission.submission_date = DateTime.Now;

                // Get the corresponding rows from PreSetMilestoneApprovers and OptionalMilestoneApprovers
                var preset = db.PreSetMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.approver_email.ToLower().Trim() == userEmail && x.is_removed != true);

                var optional = db.OptionalMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.approver_email.ToLower().Trim() == userEmail && x.is_removed != true);

                // Update approval status in PreSetMilestoneApprovers
                if (preset != null)
                {
                    preset.approved = true;
                    preset.rejected = false;
                    preset.date_approved = DateTime.Now;
                }

                // Update approval status in OptionalMilestoneApprovers
                if (optional != null)
                {
                    optional.approved = true;
                    optional.rejected = false;
                    optional.date_approved = DateTime.Now;
                }

                db.SaveChanges();

                //Checking for project status. If completely approved, send email.
                var main_id = submission.main_id;
                var milestone_id = submission.milestone_id;

                var milestone_submissions = db.ChecklistSubmissions.Where(x => x.main_id == main_id && x.milestone_id == milestone_id && (x.is_removed != true || x.is_removed == null)).ToList();

                if (milestone_submissions.Any()) {
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

                        //---------------------------------
                        var mainTbl = db.MainTables.Where(x => x.main_id == main_id).SingleOrDefault();
                        var projectTitle = mainTbl.project_title;
     
                        var milestoneTitle = db.MilestoneRoots.Where(x => x.id == milestone_id).Select(x => x.milestone_name).SingleOrDefault();
                        var membersTbl = db.ProjectMembersTbls.Where(x => x.project_id == main_id).ToList();
                        var project_manager = membersTbl.Where(x => x.role == 1004).SingleOrDefault();

                        var systemEmail = "e-notify@enchantedkingdom.ph";
                        var systemName = "PM SYSTEM";
                        var email = new MimeMessage();

                        email.From.Add(new MailboxAddress(systemName, systemEmail));
                        email.To.Add(new MailboxAddress(project_manager.name, project_manager.email));

                        email.Subject = "PM System Approval";
                        email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                        {
                            Text = @"
                            <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                <div style='text-align: center; margin-bottom: 20px;'>
                              
                                    <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                </div>
                                <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                    <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + project_manager.name + @"!</p>
                                    <p style='font-size: 14px; color: #666; margin-top: 10px;'>Your submission for 
                                    <br/>Project: <b>" + projectTitle + "</b>" +
                                        "<br/>Milestone: <b>" + milestoneTitle + "</b>" +
                                        "<br/><br/> <b>has been approved!</b>" + @" .</p>
                                    <p style='font-size: 14px; color: #555;'>
                                        Please see the link below to view the request:
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

                        db.SaveChanges();

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
            try
            {
                var submission = db.ChecklistSubmissions
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                // Mark the task as rejected
                submission.is_approved = false;
                submission.submission_date = DateTime.Now;
                submission.disapproval_reason = reason;

                // Get the corresponding rows from PreSetMilestoneApprovers and OptionalMilestoneApprovers
                var preset = db.PreSetMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                var optional = db.OptionalMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                // Update rejection status in PreSetMilestoneApprovers
                if (preset != null)
                {
                    preset.approved = false;
                    preset.rejected = true;
                    preset.date_approved = DateTime.Now; // or use date_rejected if you prefer
                }

                // Update rejection status in OptionalMilestoneApprovers
                if (optional != null)
                {
                    optional.approved = false;
                    optional.rejected = true;
                    optional.date_approved = DateTime.Now; // or use date_rejected if you prefer
                }

                db.SaveChanges();

                //---------------------------------------
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

                var systemEmail = "e-notify@enchantedkingdom.ph";
                var systemName = "PM SYSTEM";
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(systemName, systemEmail));
                email.To.Add(new MailboxAddress(project_manager.name, project_manager.email));

                email.Subject = "PM System Disapproval";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = @"
                            <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                <div style='text-align: center; margin-bottom: 20px;'>
                              
                                    <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                </div>
                                <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                    <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + project_manager.name + @"!</p>
                                    <p style='font-size: 14px; color: #666; margin-top: 10px;'>Your submission for 
                                    <br/>Project: <b>" + projectTitle + "</b>" +
                                "<br/>Milestone: <b>" + milestoneTitle + "</b>" +
                                "<br/><br/> <b>has been disapproved</b>" + @" .</p>
                                    <p style='font-size: 14px; color: #555;'>
                                        Please see the disapproval reason below:
                                    </p>
                                    <div style='text-align: center; margin: 30px 0;'>
                                        <b>Reason: " + submission.disapproval_reason + @" </b>
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
            try
            {
                var submission = db.ChecklistSubmissions
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                if (submission == null)
                {
                    return Json(new { success = false, message = "Task not found or already removed." });
                }

                // for withdrawn by 
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


                    submission.is_approved = null;
                submission.submission_date = DateTime.Now;

                // Get the corresponding rows from PreSetMilestoneApprovers and OptionalMilestoneApprovers
                var preset = db.PreSetMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                var optional = db.OptionalMilestoneApprovers
                    .FirstOrDefault(x => x.task_id == taskId && x.is_removed != true);

                // Update rejection status in PreSetMilestoneApprovers
                if (preset != null)
                {
                    preset.approved = null;
                    preset.rejected = null;
                    preset.date_approved = DateTime.Now; 
                    preset.withdraw_status = reason;
                }

                // Update rejection status in OptionalMilestoneApprovers
                if (optional != null)
                {
                    optional.approved = null;
                    optional.rejected = null;
                    optional.date_approved = DateTime.Now;
                    optional.withdraw_reason = reason;
                }

                db.SaveChanges();

                //---------------------------------------
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

                var systemEmail = "e-notify@enchantedkingdom.ph";
                var systemName = "PM SYSTEM";
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(systemName, systemEmail));
                email.To.Add(new MailboxAddress("Crystal Joyce Benauro", "cbenauro@enchantedkingdom.ph")); // test only
                //email.To.Add(new MailboxAddress(project_manager.name, project_manager.email));

                email.Subject = "PM System Withdrawal";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = @"
                            <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                <div style='text-align: center; margin-bottom: 20px;'>
                              
                                    <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                </div>
                                <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                    <p style='font-size: 16px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + project_manager.name + @"!</p>
                                    <p style='font-size: 14px; color: #666; margin-top: 10px;'>Your submission for 
                                    <br/>Project: <b>" + projectTitle + "</b>" +
                                "<br/>Milestone: <b>" + milestoneTitle + "</b>" +
                                "<br/><br/> <b>has been withdraw</b>" + @" .</p>

                                    <p style='font-size: 14px; color: #555;'>
                                        Please see the withdraw reason below:
                                    </p>
                                    <div style='text-align: center; margin: 30px 0;'>
                                        <b>Reason: " + submission.disapproval_reason + @" </b>
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

                return Json(new { success = true, message = "Task rejected!" });
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


    }
}
