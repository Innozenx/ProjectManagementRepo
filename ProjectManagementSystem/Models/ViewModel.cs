using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using CsvHelper.Configuration.Attributes;
using System.Web.Mvc;
using ProjectManagementSystem.Models;   

namespace ProjectManagementSystem.Models
{

    public class ViewModel
    {
    }

    public class Checklist
    {
        public string checkListID { get; set; }
        public string title { get; set; }
        public string duration { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public int parent { get; set; }
        public int weeklyReference { get; set; }
        public string project_owner { get; set; }
        public string division { get; set; }
        public string project_name { get; set; }

    }


    public class WeeklyChecklist
    {
        public string weeklyID { get; set; }
        public string weeklyTitle { get; set; }
        public string weeklyDuration { get; set; }
        public string weeklyStart { get; set; }
        public string weeklyTarget { get; set; }
        public string weeklyInYear { get; set; }

    }

    public class DashboardViewModel
    {
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int TotalTasks { get; set; }
        public int CurrentWeek { get; set; }
        public List<WeeklyChecklist> WeeklyChecklists { get; set; }
        public List<dynamic> Tasks { get; set; }
        public List<string> ProjectTitles { get; set; }
        public List<exportCSV> ExportProjects { get; set; }
        public List<Deliverable> UpcomingDeliverables { get; set; } = new List<Deliverable>();
        public DbSet<TaskDetail> DetailsTbl { get; set; }
        public int OverallProgress => TotalTasks > 0 ? (int)((CompletedTasks / (double)TotalTasks) * 100) : 0;
        public IEnumerable<string> UniqueMilestoneNames { get; set; }
        public IEnumerable<ProjectMilestoneViewModel> ProjectsMilestones { get; set; }
        public List<string> MilestoneOrder { get; set; }
        public List<ProjectChecklistGroupViewModel> Projects { get; set; }
    }


    public class ProjectViewModel
    {
        public int MainId { get; set; }
        public string ProjectTitle { get; set; }
    }


    public class TaskDetail
    {
        public string Title { get; set; }
        public DateTime EndDate { get; set; }
    }


    public class Deliverable
    {
        public string Tasks { get; set; }
        public DateTime DueDate { get; set; }
    }



    public class ProjectMilestoneViewModel
    {
        public int MainId { get; set; }
        public string ProjectTitle { get; set; }
        public string MilestoneName { get; set; }
        public string EndDateFormat { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool isCompleted { get; set; }
        public List<TaskViewModel> Tasks { get; set; }
        public int Duration { get; set; }
        public int ProjectYear { get; set; }
        public string Division { get; set; }
        public string Category { get; set; }
        public string ProjectOwner { get; set; }
        public DateTime ProjectStart { get; set; }
        public DateTime ProjectEnd { get; set; }
        public IEnumerable<SelectListItem> Milestones { get; set; }
        public List<ProjectDetailViewModel> ProjectDetails { get; set; } = new List<ProjectDetailViewModel>();
        public string SelectedMilestone { get; set; }
        public string StatusUpdate { get; set; }
        public HttpPostedFileBase FileUpload { get; set; }

        public List<StatusLogsViewModel> StatusLogs { get; set; } = new List<StatusLogsViewModel>();
        public List<ProjectMemberViewModel> ProjectMembers { get; set; }
        public List<ApproverViewModel> Approvers { get; set; }
        public List<ProjectMilestoneViewModel> ProjectList { get; set; }
        public IEnumerable<SelectListItem> TaskTitle { get; set; }
         
        public bool isDelayed { get; set; }
        public int delay { get; set; }
        public DateTime? CompletionDate { get; set; } // Original Completion Date
        public DateTime? CurrentCompletionDate { get; set; } // Current Updated Completion Date

        public UserModel userDetails { get; set; }
        public bool IsProjectManager { get; set; }
        public string ProjectStatus { get; set; }
        public List<ApproverViewModel> Checklist { get; set; }
        public string MilestoneStatus { get; set; }
        public List<string> SelectedMilestones { get; set; }
        public bool AreCompleted { get; set; }
        public bool IsArchived { get; set; }

    }

    public class StatusLogsViewModel
    {
        public int StatusId { get; set; }
        public int? MilestoneId { get; set; }
        public string ProjectOwner { get; set; }
        public string Description { get; set; }
        public string DateUpdated { get; set; }
        public string Attachment { get; set; }
        public int MainId { get; set; }
        public string Username { get; set; }
        public string MilestoneName { get; set; }
        public List<HttpPostedFileBase> FileUploads { get; set; }

    }
    public class ProjectDetailViewModel
    {
        public int Id { get; set; }
        public string ProjectTitle { get; set; }
        public string ProjectStart { get; set; }
        public string ProjectEnd { get; set; }
        public int ProjectDuration { get; set; }
        public int ProjectYear { get; set; }
        public string Division { get; set; }
        public string Category { get; set; }
        public string ProjectOwner { get; set; }
        public int DetailsID { get; set; }
        public string MilestoneName { get; set; }
        public string StatusUpdate { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanProceed { get; set; }
        public int MilestonePosition { get; set; }
        public List<ApproverViewModel> Approvers { get; set; }
        public List<string> Attachments { get; set; }
        

    }

    public class ProjectChecklistGroupViewModel
    {
        public int MainId { get; set; }
        public List<MilestoneViewModel> Milestones { get; set; }
        public string ProjectName { get; set; }
        public string Projects { get; set; }

    }

    public class ApproverViewModel
    {
        public int ApproverId { get; set; }
        public string ApproverName { get; set; }
        public bool Status { get; set; }
        public int TaskId { get; set; }
        public int MilestoneId { get; set; }
        public string UserId { get; set; }
        public bool IsAssigned { get; set; }
        public int DetailsId { get; set; }
        public int Id { get; set; }
        public string TaskName { get; set; }
        public bool IsRemoved { get; set; }

    }


    public class TaskViewModel
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public DateTime? TaskStart { get; set; }
        public int Duration { get; set; }
        public bool IsCompleted { get; set; }
        public bool? IsApproved { get; set; }
        public List<string> Attachments { get; set; }

        public List<ApproverViewModel> AssignedApprovers { get; set; }
        public bool RequiresApproval { get; set; }
    }


    public class ActivityLogViewModel
    {
        public int LogId { get; set; }
        public string Username { get; set; }
        public DateTime? DatetimePerformed { get; set; }
        public int? ActionLevel { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
    }

    public class TaskGantt
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
        public decimal Progress { get; set; }
        public int? ParentId { get; set; }
        public string Type { get; set; }
    }

    public class PMReport
    {
        public int id { get; set; }
        public string division { get; set; }
        public int year { get; set; }
        public int quarter { get; set; }
        public string projectName { get; set; }
        public float targetKPI { get; set; }
        public float actualKPI { get; set; }
        public float completionRate { get; set; }
        public string status { get; set; }
        public string currentTask { get; set; }
        public string nextTask { get; set; }
        public string comments { get; set; }
        public string milestoneActual { get; set; }
        public string milestoneTarget { get; set; }


        /*Extension from checklist to be used for the report*/
        public List<Checklist> listCheckList { get; set; }
    }

    public class ProjectRegister
    {
        public string ProjectName { get; set; }
        public int MainId { get; set; }
        public DateTime DateRegistered { get; set; }
        public string Division { get; set; }
        public string RegisteredBy { get; set; }
        public int Year { get; set; }
        public bool IsCompleted { get; set; }
        public bool Unregistered { get; set; }
        public string UnregisterReason { get; set; }
        public DateTime DateUnregistered { get; set; }
        public bool IsFileUploaded { get; set; }


    }

    public class exportCSV
    {
        public int id { get; set; }
        public string Process { get; set; }
        public string ProjectTitle { get; set; }
        public string ProcessTitle { get; set; }
        public int MainSubTaskID { get; set; }
        public bool IsSubtask { get; set; }
        public string projectStart { get; set; }
        public string projectEnd { get; set; }
        public int task_duration { get; set; }
        public int ProjectYear { get; set; }
        public string division { get; set; }
        public string category { get; set; }
        public string projectOwner { get; set; }
        public string MilestoneName { get; set; }
        public string TaskStart { get; set; } // converted to string
        public string TaskEnd { get; set; }
        public int? Source { get; set; }
        public int? Target { get; set; }
        public int? Parent { get; set; }
        public int Sequence { get; set; }
        public int DetailsID { get; set; }
        public int MilestoneID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ProjectDuration { get; set; }
        public List<exportCSV> ProjectDetails { get; set; }
        public string UserId { get; set; }
        public string Owner { get; set; }

    }

    public class MainTableViewModel
    {
        public int MainId { get; set; }
        public string ProjectTitle { get; set; }
        public string ProjectStart { get; set; }
        public string ProjectEnd { get; set; }
        public string Duration { get; set; }
        public string Year { get; set; }
        public string Division { get; set; }
        public string Category { get; set; }
        public string ProjectOwner { get; set; }
        public List<MilestoneViewModel> Milestones { get; set; }
    }

    public class MilestoneViewModel
    {
        public int Id { get; set; } 
        public string MilestoneName { get; set; } = string.Empty; 
        public DateTime? EndDate { get; set; }
        public bool IsCompleted { get; set; } 
        public string StatusUpdate { get; set; } = string.Empty;
        public List<TaskViewModel> Tasks { get; set; } = new List<TaskViewModel>(); 
        public int? MilestonePosition { get; set; } 
        //public List<ApproverViewModel> Approvers { get; set; } = new List<ApproverViewModel>();
        public DateTime? CurrentTaskEnd { get; set; }
        public List<string> Requirements { get; set; } = new List<string>();
        public List<string> Approvers { get; set; } = new List<string>();
        public int Sorting { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ChecklistNumber { get; set; }
        public string DivisionCodeNumber { get; set; }
    }


    public class DashboardManagementViewModel
    {
        public Dictionary<string, List<MainTableViewModel>> ProjectsByDivision { get; set; }
        public List<string> UniqueMilestoneNames { get; set; }
        public List<ProjectMilestoneViewModel> ProjectsMilestones { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CurrentTaskEnd { get; set; }
        public List<string> Divisions { get; set; }
        public string MilestoneTasks { get; set; }
        public List<MainTableViewModel> IndividualProjects { get; set; }
        public int userRole { get; set; }
        public string userDivision { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int TotalTasks { get; set; }
        public int CurrentWeek { get; set; }
    }
    public class ProjectWithMilestonesViewModel
    {
        public string Division { get; set; }
        public List<MainTableViewModel> Projects { get; set; }
        public List<string> UniqueMilestoneNames { get; set; }
    }

    public class exportCSVHeader
    {
        public string ProcessTitle { get; set; }
        public DateTime StartDate { get; set; }
        public int TaskDuration { get; set; }
        public DateTime EndDate { get; set; }
        public string ProjectTitle { get; set; }
        public DateTime ProjectStart { get; set; }
        public DateTime ProjectEnd { get; set; }
        public int ProjectDuration { get; set; }
        public DateTime ProjectYear { get; set; }
        public string Division { get; set; }
        public string Category { get; set; }
        public string Owner { get; set; }



        public class ProjectMap : ClassMap<exportCSV>
        {
            public ProjectMap()
            {
                Map(x => x.Process).Name("Process");
                Map(x => x.ProcessTitle).Name("Process_Title");
                Map(x => x.TaskStart).Name("Start");
                Map(x => x.task_duration).Name("Duration");
                Map(x => x.Source).Name("Source");
                Map(x => x.Target).Name("Target");
                Map(x => x.Parent).Name("Parent");
                Map(x => x.ProjectTitle).Name("Project_Title");
                Map(x => x.projectStart).Name("Project_Start");
                Map(x => x.ProjectDuration).Name("Project_Duration");
                Map(x => x.projectEnd).Name("Project_End");
                Map(x => x.ProjectYear).Name("Project_Year");
                Map(x => x.division).Name("Division");
                Map(x => x.category).Name("Category");
                Map(x => x.Owner).Name("Owner"); 
                Map(x => x.id).Name("ID");
                Map(x => x.Sequence).Name("Sequence");
                Map(x => x.ProjectDetails).Name("Project_ID"); 
            }
        }
        }
        public class WeeeklyStatus
        {
            public int status_id { get; set; }
            public string description { get; set; }
            public bool attachment { get; set; }
            public string project_name { get; set; }
            public string project_owner { get; set; }
            public DateTime dateInitial { get; set; }
            public DateTime dateFinished { get; set; }
            public string projectType { get; set; }
        }

        public class detailsList
        {
            List<DetailsTbl> TasksList { get; set; }

        }

        public class WeeklyStatus
        {
            public int StatusId { get; set; }
            public string Description { get; set; }
            public int MilestoneId { get; set; }
            public DateTime DateUpdated { get; set; }
            public string Attachment { get; set; }
            public int MainId { get; set; }
            public string UserId { get; set; }
            public string MilestoneName { get; set; }
            
        }
   

    public class UserTypes
    {
        public int type_id { get; set; }
        public string type_name { get; set; }
        public string type_description { get; set; }
    }

    public class Onboarding
    {
        public List<RegistrationTbl> registered_project { get; set; }
        public List<AspNetUser> users { get; set; }
        public List<UserModel> Users { get; set; } 
        public List<RoleModel> Roles { get; set; }
        public List<ProjectModel> Projects { get; set; }
        public List<MemberAccess> CurrentMembers { get; set; }
    }

    public class UserModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int? JobLevel { get; set; }
        public int Role { get; set; }
    }

    public class RoleViewModel
    {
        public List<string> ExistingRoles { get; set; }
    }

    public class RoleModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
    }
    public class ProjectModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class PojectViewModel
    {
        public int ProjectId { get; set; }
        public string Milestones { get; set; }
    }
    public class MemberAccess
    {
        public int MemberId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
    public class ProjectMemberViewModel
    {
        public string Name { get; set; }
        public int Role { get; set; }
        public string Initials { get; set; }
        public string Email { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public int Project_ID { get; set; }
    }

    public class JqueryDatatableParam
    {
        public int draw { get; set; }
        public string searchvalue { get; set; }
        public int length { get; set; }
        public int start { get; set; }
        public int iColumns { get; set; }
        public int iSortCol_0 { get; set; }
        public string sSortDir_0 { get; set; }
        public int iSortingCols { get; set; }
        public string sColumns { get; set; }
    }

    public class DetailsTblVM
    {
        public int details_id { get; set; }
        public int milestone_id { get; set; }
        public int main_id { get; set; }
        public string process_title { get; set; }
        public bool isSubtask { get; set; }
        public DateTime task_start { get; set; }
        public DateTime task_end { get; set; }
        public int task_duration { get; set; }
        public int source { get; set; }
        public int target { get; set; }
        public int parent { get; set; }
        public DateTime created_date { get; set; }
        public string task_status { get; set; }
        public string color_status { get; set; }
        public bool isUnscheduled { get; set; }
        public bool isCompleted { get; set; }
        public string key_person { get; set; }
        public bool isApproved { get; set; }
        public int delay { get; set; }
    }

    public class ChecklistSettingsViewModel
    {
        public int ChecklistId { get; set; }
        public int MainId { get; set; }
        public string ProjectName { get; set; }
        public List<MilestoneViewModel> Milestones { get; set; }
        public Onboarding Onboarding { get; set; }
        //public List<string> Divisions { get; set; }
        public string ChecklistName { get; set; }
       public string Division { get; set; }
        public string Milestone { get; set; }
        public string ChecklistDescription { get; set; }

        public List<TaskViewModel> Tasks { get; set; } = new List<TaskViewModel>();
        public List<DivisionViewModel> Divisions{ get; set; } = new List<DivisionViewModel>();
    } 

    

    public class DivisionViewModel
    {
        public int DivisionID { get; set; }
        public string DivisionName { get; set; }
    }


    public class TaskApproverViewModel
    {
        public int TaskId { get; set; } 
        public string TaskName { get; set; } 
        public List<ApproverViewModel> Approvers { get; set; } 
    }

    public class projectDivisionModel
    {
        public int projectId { get; set; }
        public List<string> division { get; set; }
    }

    public class ChecklistConfigurationViewModel
    {
        public int ClSettId { get; set; } 
        public string ChecklistName { get; set; } 
        public string DateCreated { get; set; } 
        public DateTime? DateRemoved { get; set; } 
        public bool IsActive { get; set; } 
        public string CreatedBy { get; set; } 
        public string Division { get; set; } 
        public string MilestoneId { get; set; } 
        public bool ProjectSpecific { get; set; } 
        public int? MainId { get; set; }
    }

    public class ChecklistSetupModel
    {
        public int setting_id { get; set; }
        public string checklist_name { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_removed { get; set; }
        public bool is_active { get; set; }
        public string created_by { get; set; }
        public string division { get; set; }
        public int milestone_id { get; set; }
        public bool project_specific { get; set; }
        public int main_id { get; set; }
    }

    public class ChecklistSubmissionModel
    {
        public int submission_id { get; set; }
        public string submission_description { get; set; }
        public string task_name { get; set; }
        public int task_id { get; set; }
        public string submitted_by { get; set; }
        public DateTime submission_date { get; set; }
        public bool is_approved { get; set; }
        public string filepath { get; set; }
    }

    public class TaskContainerModel
    {
        public string taskname { get; set; }
        public bool? approved { get; set; }
        public List<string> approvers { get; set; }
        public int? task_id { get; set; }
        public int? milestone_id { get; set; }
        public int? project_id { get; set; }
        public string attachment { get; set; }
        public string reason { get; set; }
        public List<bool?> approver_status { get; set; }
        public bool optFlag { get; set; }
    }

    public class ApproverTaskViewModel
    {
        public int DetailsID { get; set; }
        public string TaskName { get; set; }
        public string ProjectTitle { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime SubmittedDate { get; set; }
        public int? AttachmentID { get; set; }
        public string AttachmentName { get; set; }
        public string FilePath { get; set; }
        public string Status { get; set; }
        public string RejectReason { get; set; }

        public int ApprovedCount { get; set; }
        public int TotalApprovers { get; set; }
        public List<string> ApprovedByUsers { get; set; } = new List<string>();

        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public DateTime? ApprovalDate { get; set; }
    }

    public class ApproverRequest
    {
        public int DetailsId { get; set; } //
        public int MilestoneId { get; set; }
        public int MainId { get; set; }
        public List<string> Approvers { get; set; }
    }

    public class MilestoneDivision
    {
        [Key]
        public int Id { get; set; }
        public int DivisionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string DivisionName { get; set; }
    }

    public class TaskModel
    {
        public string Requirement { get; set; }
        public List<string> Approvers { get; set; }
    }


    public class ChecklistInfo
    {
        public string division { get; set; }
        public int milestone_id { get; set; }
        public List<FixedChecklistTbl> fixedItemList { get; set; }
        public List<UserModel> userList { get; set; }
    }

    public class ChecklistForm
    {
        public string division { get; set; }
        public int milestone_id { get; set; }
        public int project_id { get; set; }
        public List<ChecklistFormItemDetails> item { get; set; }
    }

    public class ChecklistFormItemDetails
    {
        public string title { get; set; }
        public string description { get; set; }
        public bool document { get; set; }
    }

    public class FixedChecklist
    {
        public int milestone_id { get; set; }
        public DateTime date_created { get; set; }
        public int main_id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public bool requires_documents { get; set; }
        public string division { get; set; }
    }

    public class OptionalTaskApprover
    {
        public string approver_name { get; set; }
        public string email { get; set; }
        public int main_id { get; set; }
        public int milestone_id { get; set; }
        public DateTime date_added { get; set; }
        public string added_by { get; set; }
        public string division { get; set; }
        public bool approved { get; set; }
        public string remarks { get; set; }
        public DateTime date_approved { get; set; }
        public bool is_removed { get; set; }
        public DateTime date_removed { get; set; }
        public string removed_by { get; set; }
        public int employee_id { get; set; }
    }
    public class SaveMilestoneRequest
    {
        public int DivisionID { get; set; }
        public string MilestoneName { get; set; }
        public List<TaskModel> Tasks { get; set; }
    }
}

