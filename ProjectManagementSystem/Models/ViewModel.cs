using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using CsvHelper.Configuration.Attributes;

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

        //public bool week1 { get; set; }
        //public bool week2 { get; set; }
        //public bool week3 { get; set; }
        //public bool week4 { get; set; }
        //public bool week5 { get; set; }
        //public bool week6 { get; set; }
        //public bool week7 { get; set; }
        //public bool week8 { get; set; }
        //public bool week9 { get; set; }
        //public bool week10 { get; set; }
        //public bool week11 { get; set; }
        //public bool week12 { get; set; }
        //public bool week13 { get; set; }
        //public bool week14 { get; set; }
        //public bool week15 { get; set; }
        //public bool week16 { get; set; }
        //public bool week17 { get; set; }
        //public bool week18 { get; set; }
        //public bool week19 { get; set; }
        //public bool week20 { get; set; }
        //public bool week21 { get; set; }
        //public bool week22 { get; set; }
        //public bool week23 { get; set; }
        //public bool week24 { get; set; }
        //public bool week25 { get; set; }
        //public bool week26 { get; set; }
        //public bool week27 { get; set; }
        //public bool week28 { get; set; }
        //public bool week29 { get; set; }
        //public bool week30 { get; set; }
        //public bool week31 { get; set; }
        //public bool week32 { get; set; }
        //public bool week33 { get; set; }
        //public bool week34 { get; set; }
        //public bool week35 { get; set; }
        //public bool week36 { get; set; }
        //public bool week37 { get; set; }
        //public bool week38 { get; set; }
        //public bool week39 { get; set; }
        //public bool week40 { get; set; }
        //public bool week41 { get; set; }
        //public bool week42 { get; set; }
        //public bool week43 { get; set; }
        //public bool week44 { get; set; }
        //public bool week45 { get; set; }
        //public bool week46 { get; set; }
        //public bool week47 { get; set; }
        //public bool week48 { get; set; }
        //public bool week49 { get; set; }
        //public bool week50 { get; set; }
        //public bool week51 { get; set; }
        //public bool week52 { get; set; }
    }

    //public class MilestoneViewModel
    //{
    //    public string ProjectTitle { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public DateTime EndDate { get; set; }
    //    public string ProjectName { get; set; }
    //    public string Divsion { get; set; }
    //    public byte Completed { get; set; }
    //    public byte Cancelled { get; set; }
    //    public List<MilestoneTbl> MilesetoneTable { get; set; }
    //}




    public class DashboardViewModel
    {
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int TotalTasks { get; set; }
        public int CurrentWeek { get; set; }
        public List<WeeklyChecklist> WeeklyChecklists { get; set; }
        public List<ProjectMilestoneViewModel> ProjectsMilestones { get; set; }
        public List<string> UniqueMilestoneNames { get; set; }
    }


    public class ProjectMilestoneViewModel
    {
        public string ProjectTitle { get; set; }
        public string MilestoneName { get; set; }
        public string EndDateFormat { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    //public class ProjectWithMilestones
    //{
    //    public string ProjectTitle { get; set; }
    //    public List<MilestoneViewModel> Milestones { get; set; }
    //}


    //public class DashboardViewModel
    //{
    //    public int CompletedTasks { get; set; }
    //    public int PendingTasks { get; set; }
    //    public int TotalTasks { get; set; }
    //    public int CurrentWeek { get; set; }
    //    public List<WeeklyChecklist> WeeklyChecklists { get; set; }
    //    public List<MilestoneViewModel> Milestones { get; set; }
    //}

    //public class ProjectViewModel
    //{
    //    public int ProjectId { get; set; }
    //    public string ProjectTitle { get; set; }
    //    public List<MilestoneViewModel> Milestones { get; set; }
    //}


    //public class DashboardViewModel
    //{
    //    public int CompletedTasks { get; set; }
    //    public int PendingTasks { get; set; }
    //    public int TotalTasks { get; set; }
    //    public int CurrentWeek { get; set; }
    //    public List<WeeklyChecklist> WeeklyChecklists { get; set; }
    //    public List<MilestoneViewModel> Milestones { get; set; } 
    //}

    //public class MilestoneViewModel
    //{
    //    public string Title { get; set; }
    //    public DateTime? StartDate { get; set; }
    //    public DateTime? EndDate { get; set; }
    //    public DateTime? Year { get; set; }
    //}

    //public class WeeklyChecklist
    //{
    //    public int weeklyID { get; set; }
    //    public string weeklyTitle { get; set; }
    //    public int weeklyDuration { get; set; }
    //    public DateTime weeklyStart { get; set; }
    //    public DateTime weeklyTarget { get; set; }
    //    public bool? isCompleted { get; set; }
    //}


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

    public class exportCSV
    {
        public int id { get; set; }
        public string Process { get; set; }
        public string ProjectTitle { get; set; }
        public string ProcessTitle { get; set; }
        public int MainSubTaskID { get; set; }
        public bool IsSubtask { get; set; }
        public DateTime projectStart { get; set; }
        public DateTime projectEnd { get; set; }
        public int duration { get; set; }
        public DateTime year { get; set; }
        public string division { get; set; }
        public string category { get; set; }
        public string projectOwner { get; set; }
        public string MilestoneName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? Source { get; set; } 
        public int? Target { get; set; } 
        public int? Parent { get; set; }
        public int Sequence { get; set; }
        public int DetailsID { get; set; }
        public int MilestoneID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Duration { get; set; }
    }

    //public class exportCSV
    //{
    //    public int id { get; set; }
    //    public string projectTitle { get; set; }
    //    public DateTime projectStart { get; set; }
    //    public DateTime projectEnd { get; set; }
    //    public int duration { get; set; }
    //    public DateTime year { get; set; }
    //    public string division { get; set; }
    //    public string category { get; set; }
    //    public string projectOwner { get; set; }

    //    public string MilestoneName { get; set; }
    //    public DateTime CreatedDate { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public DateTime EndDate { get; set; }
    //    public int Source { get; set; }
    //    public int Target { get; set; }
    //    public int Parent { get; set; }  

    //    //public string processTitle { get; set; }
    //    //[Format("MM/dd/yyyy")]
    //    //public DateTime start { get; set; }
    //    //public int duration { get; set; }
    //    //public string source { get; set; }
    //    //public string target { get; set; }
    //    //public int parent { get; set; }
    //    //public string projectTitle { get; set; }
    //    //[Format("MM/dd/yyyy")]
    //    //public DateTime projectStart { get; set; }
    //    //public int projectDuration { get; set; }
    //    //[Format("MM/dd/yyyy")]
    //    //public DateTime projectEnd { get; set; }
    //    //public int projectYear { get; set; }
    //    //public string division { get; set; }
    //    //public string category { get; set; }
    //    //public string owner { get; set; }
    //    //public string id { get; set; }
    //    //public int sequence { get; set; }
    //    //public string projectId { get; set; }

    //}

    public class exportCSVHeader
    { 
        public string ProcessTitle { get; set; }
        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; }
        public string ProjectTitle { get; set; }
        public DateTime ProjectStart { get; set; }
        public DateTime ProjectEnd { get; set; }
        public int ProjectDuration { get; set; }
        public DateTime ProjectYear { get; set; }
        public string Division { get; set; }
        public string Category { get; set; }
        public string Owner { get; set; }


        //public string Process { get; set; }
        //public string ProcessTitle { get; set; }
        //public DateTime StartDate { get; set; }
        //public DateTime EndDate { get; set; }
        //public int Source { get; set; }

        //public string processTitle { get; set; }
        //public int duration { get; set; }
        //public DateTime start { get; set; }
        //public string target { get; set; }
        //public int parent { get; set; }
        //public string title { get; set; }
        //public int projectYear { get; set; }
        //public string division { get; set; }
        //public string category { get; set; }
        //public string owner { get; set; }
        //public string id { get; set; }
        //public int sequence { get; set; }
        //public string projectId { get; set; }


    }



    sealed class ProjectMap : ClassMap<exportCSV>
    {
        public ProjectMap()
        {
            Map(x => x.Process).Name("Process");
            Map(x => x.ProjectTitle).Name("Project_Title");
            Map(x => x.ProcessTitle).Name("Process_Title");
            Map(x => x.projectStart).Name("Project_Start");
            Map(x => x.projectEnd).Name("Project_End");
            Map(x => x.duration).Name("Duration");
            Map(x => x.division).Name("Division");
            Map(x => x.category).Name("Category");
            Map(x => x.projectOwner).Name("Owner");
            Map(x => x.MilestoneName).Name("Process");

            Map(x => x.StartDate).Name("Project_Start");
            Map(x => x.EndDate).Name("Project_End");
            Map(x => x.duration).Name("Project_Duration");
            Map(x => x.Source).Name("Source");
            Map(x => x.Target).Name("Target");
            Map(x => x.Parent).Name("Parent");
            Map(x => x.Sequence).Name("Sequence");
            Map(x => x.id).Name("ID");
        }
    }



    //sealed class ProjectMap : ClassMap<exportCSV>
    //{
    //    public ProjectMap()
    //    {
    //        Map(x => x.projectTitle).Name("Project_Title");
    //        Map(x => x.projectStart).Name("Project_Start");
    //        Map(x => x.projectEnd).Name("Project_End");
    //        Map(x => x.duration).Name("Duration");
    //        Map(x => x.division).Name("Division");
    //        Map(x => x.category).Name("Category");
    //        Map(x => x.projectOwner).Name("Owner");
    //        Map(x => x.MilestoneName).Name("Process");

    //        Map(x => x.StartDate).Name("Project_Start");
    //        Map(x => x.EndDate).Name("Project_End");
    //        Map(x => x.duration).Name("Project_Duration");
    //        Map(x => x.Source).Name("Source");
    //        Map(x => x.Target).Name("Target");
    //        Map(x => x.Parent).Name("Parent");



    //        //Map(x => x.process).Name("Process");
    //        //Map(x => x.processTitle).Name("Process_Title");
    //        //Map(x => x.start).Name("Start");
    //        //Map(x => x.duration).Name("Duration");
    //        //Map(x => x.source).Name("Source");
    //        //Map(x => x.target).Name("Target");
    //        //Map(x => x.parent).Name("Parent");
    //        //Map(x => x.projectTitle).Name("Project_Title");
    //        //Map(x => x.projectStart).Name("Project_Start");
    //        //Map(x => x.projectDuration).Name("Project_Duration");
    //        //Map(x => x.projectEnd).Name("Project_End");
    //        //Map(x => x.projectYear).Name("Project_Year");
    //        //Map(x => x.division).Name("Division");
    //        //Map(x => x.category).Name("Category");
    //        //Map(x => x.owner).Name("Owner");
    //        //Map(x => x.id).Name("ID");
    //        //Map(x => x.sequence).Name("Sequence");
    //        //Map(x => x.projectId).Name("Project_ID");

    //    }
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

   


    //public class listWeekly
    //{
    //    List<WeeklyChecklistTable> ProjectList { get; set; }
    //}

    //public class LinkGantt
    //{
    //    public int Id { get; set; }
    //    public string Type { get; set; }
    //    public int SourceTaskId { get; set; }
    //    public int TargetTaskId { get; set; }

    //}

    //public class GanttJson
    //{
    //    public List<TaskGantt> tasks { get; set; }
    //    public List<LinkGantt> links { get; set; }
    //}

}