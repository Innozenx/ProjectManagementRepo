using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ProjectManagementSystem.Models
{
    public class ViewModel
    {
    }

    public class Checklist
    {
        public int checkListID { get; set; }
        public string title { get; set; }
        public string duration { get; set; }
        public DateTime startDate { get; set; }
        public int parent { get; set; }
        public int weeklyReference { get; set; }

    }

    public class WeeklyChecklist
    {
        public int weeklyID { get; set; }
        public string weeklyTitle { get; set; }
        public string weeklyDuration { get; set; }
        public string weeklyStart { get; set; }
        public string weeklyTarget { get; set; }
        public string weeklyInYear { get; set; }
        public bool week1 { get; set; }
        public bool week2 { get; set; }
        public bool week3 { get; set; }
        public bool week4 { get; set; }
        public bool week5 { get; set; }
        public bool week6 { get; set; }
        public bool week7 { get; set; }
        public bool week8 { get; set; }
        public bool week9 { get; set; }
        public bool week10 { get; set; }
        public bool week11 { get; set; }
        public bool week12 { get; set; }
        public bool week13 { get; set; }
        public bool week14 { get; set; }
        public bool week15 { get; set; }
        public bool week16 { get; set; }
        public bool week17 { get; set; }
        public bool week18 { get; set; }
        public bool week19 { get; set; }
        public bool week20 { get; set; }
        public bool week21 { get; set; }
        public bool week22 { get; set; }
        public bool week23 { get; set; }
        public bool week24 { get; set; }
        public bool week25 { get; set; }
        public bool week26 { get; set; }
        public bool week27 { get; set; }
        public bool week28 { get; set; }
        public bool week29 { get; set; }
        public bool week30 { get; set; }
        public bool week31 { get; set; }
        public bool week32 { get; set; }
        public bool week33 { get; set; }
        public bool week34 { get; set; }
        public bool week35 { get; set; }
        public bool week36 { get; set; }
        public bool week37 { get; set; }
        public bool week38 { get; set; }
        public bool week39 { get; set; }
        public bool week40 { get; set; }
        public bool week41 { get; set; }
        public bool week42 { get; set; }
        public bool week43 { get; set; }
        public bool week44 { get; set; }
        public bool week45 { get; set; }
        public bool week46 { get; set; }
        public bool week47 { get; set; }
        public bool week48 { get; set; }
        public bool week49 { get; set; }
        public bool week50 { get; set; }
        public bool week51 { get; set; }
        public bool week52 { get; set; }
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

    public class LinkGantt
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int SourceTaskId { get; set; }
        public int TargetTaskId { get; set; }
    }

    public class GanttJson
    {
        public List<TaskGantt> tasks { get; set; }
        public List<LinkGantt> links { get; set; }
    }
}