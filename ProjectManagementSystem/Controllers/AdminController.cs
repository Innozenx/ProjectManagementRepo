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

                var users = cmdb.AspNetUsers
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FirstName + " " + u.LastName 
                    }).ToList();

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
                Debug.WriteLine($"Received Data - DivisionID: {DivisionID}, MilestoneName: {MilestoneName}, Tasks Count: {Tasks?.Count ?? 0}");

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
                    DivisionCodeNumber = divisionCodeNumber
                };

                var approver_ids = approverIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<PreSetMilestoneApprover> approver_list = new List<PreSetMilestoneApprover>();

                foreach (var item in approver_ids)
                {
                    var approver_details = cmdb.AspNetUsers.FirstOrDefault(x => x.Id == item);
                    if (approver_details == null)
                    {
                        Debug.WriteLine($"Approver not found for ID: {item}");
                        continue;
                    }

                    int.TryParse(approver_details.CMId, out int employeeId);

                    var divisionName = cmdb.Identity_Keywords
                        .Where(x => x.Id == approver_details.JobLevel && x.Type == "Divisions")
                        .Select(x => x.Description)
                        .FirstOrDefault() ?? "Unknown Division";

                    var approver_container = new PreSetMilestoneApprover
                    {
                        approver_name = approver_details.FirstName + " " + approver_details.LastName,
                        approver_email = approver_details.Email,
                        milestone_id = milestoneId,
                        date_added = DateTime.Now,
                        added_by = User.Identity.Name,
                        division = divisionName,
                        employee_id = employeeId
                    };

                    approver_list.Add(approver_container);
                }

                db.PreSetMilestoneApprovers.AddRange(approver_list);
                db.PreSetMilestones.Add(milestone);
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

        [Authorize(Roles = "PMS_ODCP_ADMIN, PMS_PROJECT_OWNER, PMS_PROJECT_MANAGER")]
        public ActionResult PendingApprovals()
        {
            var userId = User.Identity.Name;

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

        [HttpPost]
        public JsonResult ApproveTask(int taskId)
        {
            try
            {

                var userID = User.Identity.Name;
                var tasks = db.ApproversTbls.FirstOrDefault
                    (a => a.Details_Id == taskId && a.User_Id == userID);

                if (tasks == null)
                {
                    return Json(new { success = false, message = "Task not found" });
                }

                tasks.IsApproved_ = true;
                tasks.IsRejected_ = false;
                tasks.ApprovalDate = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true, message = "Task approved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error" + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult RejectTask(int taskId, string reason)
        {
            try
            {
                var userId = User.Identity.Name;
                var task = db.ApproversTbls.FirstOrDefault
                    (a => a.Details_Id == taskId && a.User_Id == userId);

                if (task == null)
                {
                    return Json(new { success = false, message = "Task not found" });
                }

                task.IsApproved_ = false;
                task.IsRejected_ = true;
                task.RejectReason = reason;
                db.SaveChanges();

                return Json(new { success = true, message = "Task rejected! :( " });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error" + ex.Message });
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

        [HttpGet]
        [Authorize]
        public JsonResult GetPendingApprovals()
        {
            try
            {
                string currentUserId = User.Identity.GetUserId();

                var pendingTasks = db.ApproversTbls
                    .Where(a => a.User_Id == currentUserId && a.IsRemoved_ == false)
                    .Select(a => new ApproverTaskViewModel
                    {
                        DetailsID = a.Details_Id ?? 0,
                        TaskName = db.DetailsTbls
                            .Where(t => t.details_id == a.Details_Id)
                            .Select(t => t.process_title)
                            .FirstOrDefault(),

                        ProjectTitle = db.MainTables
                            .Where(p => p.main_id == a.Main_Id)
                            .Select(p => p.project_title)
                            .FirstOrDefault(),

                        SubmittedBy = cmdb.AspNetUsers
                            .Where(u => u.Id == a.User_Id)
                            .Select(u => u.FirstName + " " + u.LastName)
                            .FirstOrDefault(),
                        SubmittedDate = a.ApprovalDate ?? DateTime.Now
                    }).ToList();

                return Json(pendingTasks, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading pending approvals" }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}
