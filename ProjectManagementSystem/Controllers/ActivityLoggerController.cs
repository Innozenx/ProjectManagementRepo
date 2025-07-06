using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class ActivityLoggerController : Controller
    {

        CMIdentityDBEntities cmdb = new CMIdentityDBEntities();
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();
        
        // GET: ActivityLogger
        public ActionResult Index()
        {
            return View();
        }

        // GET: ActivityLogger/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ActivityLogger/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ActivityLogger/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ActivityLogger/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ActivityLogger/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ActivityLogger/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ActivityLogger/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public JsonResult ActivityLog(string email, int category, string action, string project, List<string> details)
        {
            var details_container = "";
            var userDetails = (from u in cmdb.AspNetUsers.Where(x => x.Email == email)
                               join j in cmdb.Identity_JobDescription on new { jId = u.JobId } equals new { jId = j.Id }
                               join k in cmdb.Identity_Keywords.Where(x => x.Type == "Divisions") on new { division = j.DivisionId } equals new { division = k.Id }
                               join kd in cmdb.Identity_Keywords.Where(x => x.Type == "Departments") on new {department = j.DeptId} equals new {department = kd.Id}
                               select new
                               {
                                   email = u.Email,
                                   name = u.FirstName + " " + u.MiddleName + " " + u.LastName,
                                   emp_id = u.CMId,
                                   division = k.Description,
                                   designation = j.PositionDescription,
                                   department = kd.Description
                               }).FirstOrDefault();

            foreach (var item in details)
            {
                if(details.Count > 1)
                {
                    details_container = details_container + item + ", ";
                }

                else
                {
                    details_container = details_container + item;
                }
            }

            switch (category)
            {
                case 1: // invite teammates
                    Activity_Log inv_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(inv_logs);
                    db.SaveChanges();
                    break;

                case 2: // milestones
                    Activity_Log m_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(m_logs);
                    db.SaveChanges();
                    break;

                case 3: // project registration
                    Activity_Log r_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(r_logs);
                    db.SaveChanges();
                    break;

                case 4: // project onboarding
                    Activity_Log p_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(p_logs);
                    db.SaveChanges();
                    break;

                case 5: // role configuration
                    Activity_Log ro_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(ro_logs);
                    db.SaveChanges();
                    break;

                case 6: // dashboard
                    Activity_Log d_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container + " for project: " + project,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(d_logs);
                    db.SaveChanges();
                    break;

                case 7: // activity logs
                    break;

                case 8: // archived projects
                    break;

                case 9: // approval
                    Activity_Log a_logs = new Activity_Log
                    {
                        username = email,
                        datetime_performed = DateTime.Now,
                        action_level = category,
                        action = action,
                        description = details_container + " for project: " + project,
                        department = userDetails.department,
                        division = userDetails.division
                    };

                    db.Activity_Log.Add(a_logs);
                    db.SaveChanges();
                    break;
            }
            return Json(new { message = "success" }, JsonRequestBehavior.AllowGet);
        }
    }
}
