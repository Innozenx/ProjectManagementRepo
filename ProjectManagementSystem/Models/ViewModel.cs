﻿using CsvHelper.Configuration;
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

        public int OverallProgress
        {
            get
            {
                return TotalTasks > 0 ? (int)((CompletedTasks / (double)TotalTasks) * 100) : 0;
            }
        }

     

        public IEnumerable<string> UniqueMilestoneNames { get; set; }
        public IEnumerable<ProjectMilestoneViewModel> ProjectsMilestones { get; set; }
        public List<string> MilestoneOrder { get; set; }
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
        public string Milestones { get; set; }
        public List<ProjectDetailViewModel> ProjectDetails { get; set; } = new List<ProjectDetailViewModel>();
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
     
    }


    public class TaskViewModel
    {
        public DateTime? TaskStart { get; set; }
        public int Duration { get; set; } 
        public bool IsCompleted { get; set; } 
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
        public string TaskStart { get; set; }
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

    //public class DetailsTbl
    //{
    //    public int milestone_id { get; set; }
    //    public DateTime? task_start { get; set; }
    //    public int task_duration { get; set; }
    //    public bool isCompleted { get; set; }
    //}



    sealed class ProjectMap : ClassMap<exportCSV>
    {
        public ProjectMap()
        {
            Map(x => x.Process).Name("Process");
            Map(x => x.ProjectTitle).Name("Project_Title");
            Map(x => x.ProcessTitle).Name("Process_Title");
            Map(x => x.TaskStart).Name("Start");
            Map(x => x.projectStart).Name("Project_Start");
            Map(x => x.projectEnd).Name("Project_End");
            Map(x => x.task_duration).Name("Duration");
            Map(x => x.division).Name("Division");
            Map(x => x.category).Name("Category");
            Map(x => x.projectOwner).Name("Owner");
            Map(x => x.MilestoneName).Name("Process");
            Map(x => x.ProjectYear).Name("Project_Year");


            Map(x => x.projectStart).Name("Project_Start");
            Map(x => x.TaskEnd).Name("Project_End");
            Map(x => x.ProjectDuration).Name("Project_Duration");
            Map(x => x.Source).Name("Source");
            Map(x => x.Target).Name("Target");
            Map(x => x.Parent).Name("Parent");
            Map(x => x.Sequence).Name("Sequence");
            Map(x => x.id).Name("ID");
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

    public class projectRegister
    {
        public string project_name { get; set; }
        public int main_id { get; set; }
        public DateTime date_registered { get; set; }
        public string division { get; set; }
        public string registered_by { get; set; }
        public int year { get; set; }
        public bool is_completed { get; set; }
        public bool unregistered { get; set; }
        public string unregister_reason { get; set; }
        public DateTime date_unregistered { get; set; }
        public bool is_file_uploaded { get; set; }

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







