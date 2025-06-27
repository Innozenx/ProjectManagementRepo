using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using MailKit.Net.Smtp;
using MimeKit;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class AutomatedTaskController : Controller
    {
        ProjectManagementDBEntities db = new ProjectManagementDBEntities();
        CMIdentityDBEntities cmdb = new CMIdentityDBEntities();

        // GET: AutomatedTask
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult StatusNotifier()
        {
            var milestone_tasks = db.MilestoneTbls.Where(x => x.IsCompleted != true).GroupBy(x => x.main_id).Select(x => x.FirstOrDefault()).ToList();
            List<AutomatedWeeklyStatusModel> notifierList = new List<AutomatedWeeklyStatusModel>();

            foreach (var milestone in milestone_tasks)
            {
                AutomatedWeeklyStatusModel notifier = new AutomatedWeeklyStatusModel();
                StringBuilder itemList = new StringBuilder();

                var container = db.DetailsTbls.Where(x => x.milestone_id == milestone.milestone_id && x.main_id == milestone.main_id).ToList();

                notifier.project_title = db.MainTables.Where(x => x.main_id == milestone.main_id && x.isCompleted != false).Select(x => x.project_title).SingleOrDefault();

                var pm_details = db.ProjectMembersTbls.Where(x => x.project_id == milestone.main_id && x.role == 1004 && x.is_removed != true).Select(x => new { name = x.name, email = x.email }).FirstOrDefault();

                var calendar = CultureInfo.InvariantCulture.Calendar;
                var currentWeek = calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

                if (pm_details != null)
                {
                    notifier.pmanager_email = pm_details.email;
                    notifier.pmanager_name = pm_details.name;
                }

                if (notifier.pmanager_email != null && notifier.pmanager_name != null)
                {
                    var dbProjectTitle = notifier.project_title;
                    itemList.Append("<div>" + dbProjectTitle + ": ");

                    foreach (var child in container)
                    {
                        var updateCheck = db.WeeklyStatus.Where(x => x.details_id == child.details_id).OrderByDescending(x => x.status_id).FirstOrDefault();

                        if (child.parent == null && child.isCompleted != true && updateCheck == null)
                        {
                            if (child.current_task_start == null)
                            {

                                if (DateTime.Now > child.task_start)
                                {
                                    itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                                }
                            }

                            else
                            {
                                if (DateTime.Now > child.current_task_start)
                                {
                                    itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                                }
                            }
                        }

                        else if (child.parent == null && child.isCompleted != true && updateCheck != null)
                        {
                            if (currentWeek > calendar.GetWeekOfYear(updateCheck.date_updated.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                            {
                                itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                            }
                        }

                        else
                        {
                            if (container.Where(x => x.excel_id == child.parent).SingleOrDefault() != null && updateCheck == null)
                            {
                                if (container.Where(x => x.excel_id == child.parent).SingleOrDefault().isCompleted == true)
                                {
                                    if (child.current_task_start == null)
                                    {
                                        if (currentWeek > calendar.GetWeekOfYear(child.task_start.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                                        {
                                            itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                                        }
                                    }

                                    else
                                    {
                                        if (currentWeek > calendar.GetWeekOfYear(child.current_task_start.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                                        {
                                            itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                                        }
                                    }
                                }
                            }

                            else if (container.Where(x => x.excel_id == child.parent).SingleOrDefault() != null && updateCheck != null)
                            {
                                if (container.Where(x => x.excel_id == child.parent).SingleOrDefault().isCompleted == true)
                                {
                                    if (currentWeek > calendar.GetWeekOfYear(updateCheck.date_updated.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                                    {
                                        itemList.Append("<br/><span style='color: red'>" + child.process_title + "</span>");
                                    }

                                }

                            }

                            else
                            {
                                //tapon
                            }

                        }

                    }

                    itemList.Append("</div><br/>");
                    var temp_str = itemList.ToString();
                    notifier.content_body = temp_str;
                    notifierList.Add(notifier);
                }

            }

            var emailContent = notifierList.GroupBy(x => x.pmanager_email).Select(x => new { project_manager = x.Key, items = x.ToList() }).ToList();

            foreach (var item in emailContent)
            {
                StringBuilder content_body_list = new StringBuilder();
                AutomatedWeeklyStatusModel emailContainer = new AutomatedWeeklyStatusModel();
                emailContainer.pmanager_email = item.items.FirstOrDefault().pmanager_email;
                emailContainer.pmanager_name = item.items.FirstOrDefault().pmanager_name;

                foreach (var content in item.items)
                {
                    if (content.pmanager_email != null && content.pmanager_name != null)
                    {
                        content_body_list.Append(content.content_body);
                    }

                }

                emailContainer.content_body = content_body_list.ToString();

                //-----EMAIL FOR STATUS UPDATE

                var systemEmail = "e-notify@enchantedkingdom.ph";
                var systemName = "PM SYSTEM";
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(systemName, systemEmail));
                email.To.Add(new MailboxAddress(emailContainer.pmanager_name, emailContainer.pmanager_email));

                email.Subject = "PM System Pending Weekly Status Update";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = @"
                                    <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                        <div style='text-align: center; margin-bottom: 20px;'>

                                            <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                        </div>
                                        <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                            <p style='font-size: 18px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + emailContainer.pmanager_name + @"!</p>
                                            <p style='font-size: 16px; font-weight: 600; color: red; margin-bottom: 10px;'>This serves as a reminder as of: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + @"</p><br/>
                                            <p style='font-size: 16px; font-weight: 400; color: #666; margin-top: 10px;'>It has been a while since you posted an update with these tasks: 
                                            <b>" + emailContainer.project_title + "</b>" +
                                "<br/><b>" + emailContainer.content_body + "</b>" +
                                "<br/> <b>We'd love to know the status of these projects! Please kindly post an update of these task. Thank you!</b>" + @" .</p>
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
            }

            return View();
        }

        public ActionResult ApprovalNotifier()
        {

            var presets = (from s in db.ChecklistSubmissions.Where(x => x.is_removed != true && x.is_approved != true && x.type == "preset" && x.approval_enabled == true && x.submission_date < DateTime.Now)
                           join p in db.PreSetMilestoneApprovers.Where(x => x.approved != true && x.rejected != true && x.is_removed != true && x.datetime_withdraw == null) on new { ptaskID = s.task_id, pmilestoneID = s.milestone_id } equals new { ptaskID = p.task_id, pmilestoneID = p.milestone_id }
                           join pr in db.PreSetMilestones on new { taskID = p.task_id.Value } equals new { taskID = pr.ID }
                           select new
                           {
                               presetApprover = p,
                               presetTasks = pr
                           }).ToList();

            var optionals = (from s in db.ChecklistSubmissions.Where(x => x.is_removed != true && x.is_approved != true && x.type == "optional" && x.approval_enabled == true && x.submission_date < DateTime.Now)
                             join o in db.OptionalMilestoneApprovers.Where(x => x.approved != true && x.rejected != true && x.is_removed != true && x.datetime_withdraw == null) on new { otaskID = s.task_id, omilestoneID = s.milestone_id } equals new { otaskID = o.task_id, omilestoneID = o.milestone_id }
                             join op in db.OptionalMilestones on new { taskID = o.task_id.Value } equals new { taskID = op.id }
                             select new
                             {
                                 optionalApprover = o,
                                 optionalTasks = op
                             }).ToList();

            var groupedPreset = presets.GroupBy(x => x.presetApprover.approver_email).Select(x => x.ToList()).ToList();
            var groupedOptional = optionals.GroupBy(x => x.optionalApprover.approver_email).Select(x => x.ToList()).ToList();
            List<AutomatedApprovalModel> approval_container = new List<AutomatedApprovalModel>();

            foreach (var outer in groupedPreset)
            {
                StringBuilder itemList = new StringBuilder();
                AutomatedApprovalModel approval = new AutomatedApprovalModel();

                var outerTemp = outer.FirstOrDefault();
                var projectTitle = db.MainTables.Where(x => x.main_id == outerTemp.presetApprover.main_id).Select(x => x.project_title).FirstOrDefault();

                itemList.Append("<div>" + projectTitle + ": ");

                foreach (var inner in outer)
                {
                    itemList.Append("<br/><span style='color: red'>" + inner.presetTasks.Requirements + "</span>");
                }

                itemList.Append("<br/></div>");

                approval.approver_email = outerTemp.presetApprover.approver_email;
                approval.approver_name = outerTemp.presetApprover.approver_name;
                approval.content_body = itemList.ToString();
                approval.project_title = projectTitle;

                approval_container.Add(approval);
            }

            foreach (var outer in groupedOptional)
            {
                StringBuilder itemList = new StringBuilder();
                AutomatedApprovalModel approval = new AutomatedApprovalModel();

                var outerTemp = outer.FirstOrDefault();
                var projectTitle = db.MainTables.Where(x => x.main_id == outerTemp.optionalApprover.main_id).Select(x => x.project_title).FirstOrDefault();

                itemList.Append("<div>" + projectTitle + ": ");

                foreach (var inner in outer)
                {
                    itemList.Append("<br/><span style='color: red'>" + inner.optionalTasks.task + "</span>");
                }

                itemList.Append("<br/></div>");

                approval.approver_email = outerTemp.optionalApprover.approver_email;
                approval.approver_name = outerTemp.optionalApprover.approver_name;
                approval.content_body = itemList.ToString();
                approval.project_title = projectTitle;

                approval_container.Add(approval);
            }

            var emailContent = approval_container.GroupBy(x => x.approver_email).Select(x => new { approver = x.Key, items = x.ToList() }).ToList();

            foreach (var item in emailContent)
            {
                StringBuilder content_body_list = new StringBuilder();
                AutomatedApprovalModel emailContainer = new AutomatedApprovalModel();
                emailContainer.approver_email = item.items.FirstOrDefault().approver_email;
                emailContainer.approver_name = item.items.FirstOrDefault().approver_name;

                foreach (var content in item.items)
                {
                    if (content.approver_email != null && content.approver_name != null)
                    {
                        content_body_list.Append(content.content_body);
                    }

                }

                emailContainer.content_body = content_body_list.ToString();

                //-----EMAIL FOR STATUS UPDATE

                var systemEmail = "e-notify@enchantedkingdom.ph";
                var systemName = "PM SYSTEM";
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(systemName, systemEmail));
                email.To.Add(new MailboxAddress(emailContainer.approver_name, emailContainer.approver_email));

                email.Subject = "PM System Pending Approval Update";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = @"
                                    <div style='font-family: Poppins, Arial, sans-serif; font-size: 14px; color: #333; background-color: #f9f9f9; padding: 40px; line-height: 1.8; border-radius: 10px; max-width: 600px; margin: auto; border: 1px solid #ddd;'>
                                        <div style='text-align: center; margin-bottom: 20px;'>

                                            <h1 style='font-size: 26px; color: #66339A; margin: 0;'>Enchanting Day!</h1>
                                        </div>
                                        <div style='background: #fff; padding: 30px; border-radius: 10px; box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1); text-align: center;'>
                                            <p style='font-size: 18px; font-weight: 600; color: #333; margin-bottom: 10px;'>Hello, " + emailContainer.approver_name + @"!</p>
                                            <p style='font-size: 16px; font-weight: 600; color: red; margin-bottom: 10px;'>This serves as a reminder as of: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + @"</p><br/>
                                            <p style='font-size: 16px; font-weight: 400; color: #666; margin-top: 10px;'>It seems like you have not approved any of these requests yet: 
                                            <b>" + emailContainer.project_title + "</b>" +
                                "<br/><b>" + emailContainer.content_body + "</b>" +
                                "<br/> <b>Your approval is integral for the progression of these projects. Please take the time to review these. Thank you!</b>" + @" .</p>
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
            }

            return View();
        }
    }
}