using ProjectManagementSystem.Models;
using System;
using System.Linq;

namespace ProjectManagementSystem.Controllers
{
    public class NotificationHelper
    {
        private readonly ProjectManagementDBEntities _db;

        public NotificationHelper(ProjectManagementDBEntities db)
        {
            _db = db;
        }

        public void CreateNotification(
       string userFullName,
       string message,
       string link,
       string type,
       int? mainId = null,
       int? milestoneId = null,
       int? taskId = null)
        {
            if (string.IsNullOrEmpty(userFullName) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(type))
                return;

            var notif = new Notification
            {
                cmid_user = userFullName,
                message = message,
                link = link,
                type = type,
                is_read = false,
                main_id = mainId ?? 0,
                milestone_id = milestoneId ?? 0,
                task_id = taskId ?? 0,
                created_date = DateTime.Now
            };

            _db.Notifications.Add(notif);
            _db.SaveChanges();
        }

    }
}
