using System.Linq;
using System.Web.Mvc;
using ProjectManagementSystem.Models; 
namespace ProjectManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var currentEmail = User.Identity.Name;

            using (var db = new ProjectManagementDBEntities())
            {
                bool isApprover = db.PreSetMilestoneApprovers.Any(x => x.approver_email == currentEmail)
                               || db.OptionalMilestoneApprovers.Any(x => x.approver_email == currentEmail);

                ViewBag.IsApprover = isApprover;
            }
        }
    }
}
