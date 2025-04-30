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

                if(Request.Files.Count > 0)
                {
                    HttpFileCollectionBase files = Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        HttpPostedFileBase file = files[i];
                        string fname, path;

                        fname = file.FileName;

                        fname = Path.Combine(Server.MapPath("~/Uploads/"), fname);
                        file.SaveAs(fname);

                        path = fname.Replace("c:\\users\\jyparraguirre\\source\\repos\\ProjectManagementSystem\\ProjectManagementSystem", "");
                        var sample = Request.Form.GetValues("name")[0];
                        var submission = new ChecklistSubmission()
                        {
                            submission_description = "test",
                            task_name = Request.Form.GetValues("name")[0],
                            task_id = Int32.Parse(Request.Form.GetValues("id")[0]),
                            submitted_by = User.Identity.Name,
                            submission_date = DateTime.Now,
                            is_approved = false,
                            filepath = path,
                            milestone_id = Int32.Parse(Request.Form.GetValues("milestone_id")[0]),
                            main_id = Int32.Parse(Request.Form.GetValues("project_id")[0]),
                            type = Request.Form.GetValues("type")[0]

                        };

                        db.ChecklistSubmissions.Add(submission);
                        db.SaveChanges();

                    }

                    return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
                }

                else
                {
                    return Json(new { message = "failed" }, JsonRequestBehavior.AllowGet);
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