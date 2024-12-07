using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProjectManagementSystem.Controllers
{
    public class FileUploadController : Controller
    {
        // GET: FileUpload
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UploadFile(HttpPostedFileBase file)
        {
            var message = "";
            try
            {
                if (file.ContentLength > 0)
                {
                    string _FileName = Path.GetFileName(file.FileName);
                    string _path = Path.Combine(Server.MapPath("~/PM_Uploads"), _FileName);
                    file.SaveAs(_path);

                    message = "SUCCESS";
                }
                ViewBag.Message = "File Uploaded Successfully!!";
                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                ViewBag.Message = "File upload failed!!";
                Debug.WriteLine(e);
                message = "FAILED";
                return Json(new { message = message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}