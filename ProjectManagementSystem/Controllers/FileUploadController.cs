using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class FileUploadController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();

        // GET: FileUpload
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UploadFile()
        {
            ActivityLoggerController log = new ActivityLoggerController();
            List<string> details_container = new List<string>();

            var message = "";
            try
            {
                //if (file.ContentLength > 0)
                //{
                //    string _FileName = Path.GetFileName(file.FileName);
                //    string _path = Path.Combine(Server.MapPath("~/PM_Uploads"), _FileName);
                //    file.SaveAs(_path);

                //    message = "SUCCESS";
                //}
                //ViewBag.Message = "File Uploaded Successfully!!";
                //return Json(new { message = message }, JsonRequestBehavior.AllowGet);

                if (Request.Files.Count > 0)
                {
                    HttpFileCollectionBase files = Request.Files;

                    for (int i = 0; i < files.Count; i++)
                    {
                        HttpPostedFileBase file = files[i];

                        if (file != null && file.ContentLength > 0)
                        {
                            try
                            {
                                // Get only the file name (no path)
                                string originalFileName = Path.GetFileName(file.FileName);

                                // Sanitize file name if needed (basic example)
                                originalFileName = originalFileName.Replace(" ", "_");

                                // Combine with the server path
                                string uploadPath = Server.MapPath("~/Uploads/");
                                string fullPath = Path.Combine(uploadPath, originalFileName);

                                // Ensure the directory exists
                                if (!Directory.Exists(uploadPath))
                                {
                                    Directory.CreateDirectory(uploadPath);
                                }

                                // Save the file to the server
                                file.SaveAs(fullPath);

                                // Get the relative path (e.g., "~/Uploads/file.ext")
                                string relativePath = Url.Content($"~/Uploads/{originalFileName}");

                                // Get values from form
                                string taskName = Request.Form["name"];
                                int taskId = int.Parse(Request.Form["id"]);
                                int milestoneId = int.Parse(Request.Form["milestone_id"]);
                                int projectId = int.Parse(Request.Form["project_id"]);
                                string type = Request.Form["type"];

                                // Create and save the submission
                                var submission = new ChecklistSubmission
                                {
                                    submission_description = "test",
                                    task_name = taskName,
                                    task_id = taskId,
                                    submitted_by = User.Identity.Name,
                                    submission_date = DateTime.Now,
                                    is_approved = false,
                                    filepath = relativePath,
                                    milestone_id = milestoneId,
                                    main_id = projectId,
                                    type = type
                                };

                                db.ChecklistSubmissions.Add(submission);
                                db.SaveChanges();

                                if(type == "optional")
                                {
                                    details_container.Add("Task: " + db.OptionalMilestones.Where(x => x.id == taskId).Select(x => x.description).SingleOrDefault());
                                    log.ActivityLog(User.Identity.Name, 6, "Uploaded File for Optional Task", db.MainTables.Where(x => x.main_id == projectId).OrderByDescending(x => x.main_id).Select(x => x.project_title).FirstOrDefault(), details_container);
                                }

                                else
                                {
                                    details_container.Add("Task: " + db.PreSetMilestones.Where(x => x.ID == taskId).Select(x => x.Requirements).SingleOrDefault());
                                    log.ActivityLog(User.Identity.Name, 6, "Uploaded File for Preset Task", db.MainTables.Where(x => x.main_id == projectId).OrderByDescending(x => x.main_id).Select(x => x.project_title).FirstOrDefault(), details_container);
                                }

                                
                                
                            }
                            catch (Exception ex)
                            {
                                return Json(new { message = "error", detail = ex.Message }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { message = "no_files_uploaded" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = "File upload failed!!";
                Debug.WriteLine(e);
                message = "Sorry, file is required.";
                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}