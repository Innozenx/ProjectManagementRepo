using System;
using System.Collections.Generic;
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
        public int parentWeeklyReference { get; set; }
        public int parentSubReference { get; set; }
        public string title { get; set; }
        public string duration { get; set; }
        public DateTime startDate { get; set; }
        public DateTime targetDate { get; set; }
    }
}