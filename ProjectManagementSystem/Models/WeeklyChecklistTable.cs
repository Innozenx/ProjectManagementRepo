//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProjectManagementSystem.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class WeeklyChecklistTable
    {
        public int localId { get; set; }
        public string weeklyID { get; set; }
        public Nullable<int> sequenceId { get; set; }
        public string weeklyOwner { get; set; }
        public string weeklyTitle { get; set; }
        public string weeklyDuration { get; set; }
        public System.DateTime weeklyStart { get; set; }
        public System.DateTime weeklyTarget { get; set; }
        public Nullable<int> weeklyInYear { get; set; }
        public Nullable<int> subMain { get; set; }
        public Nullable<int> subSub { get; set; }
        public string division { get; set; }
        public string category { get; set; }
        public Nullable<int> inWeek { get; set; }
        public Nullable<bool> isCancelled { get; set; }
        public Nullable<bool> isDelayed { get; set; }
        public Nullable<int> WeeklyMonth { get; set; }
        public Nullable<int> WeeklyDay { get; set; }
        public Nullable<bool> isCompleted { get; set; }
    }
}
